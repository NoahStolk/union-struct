using UnionStruct.Internals.Model;
using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals;

/// <summary>
/// Emits the generated half of a union. The shape is pinned by the hand-written golden samples in
/// <c>research/dotnet-11-unions/Samples</c>: <c>S07</c> for the explicit-overlap branch and <c>S10</c> for the
/// sequential branch.
/// </summary>
internal sealed class UnionGenerator(UnionModel unionModel)
{
	private const string _int32 = "global::System.Int32";
	private const string _boolean = "global::System.Boolean";
	private const string _string = "global::System.String";
	private const string _utf8Span = "global::System.ReadOnlySpan<global::System.Byte>";
	private const string _unreachable = "throw new global::System.Diagnostics.UnreachableException($\"Invalid case index: {caseIndex}.\")";
	private const string _fieldOffsetAttribute = "global::System.Runtime.InteropServices.FieldOffset";

	/// <summary>Payloads start after the 4-byte <c>CaseIndex</c>.</summary>
	private const int _payloadOffset = 4;

	private readonly string _self = unionModel.StructIdentifier;

	public string Generate()
	{
		CodeWriter writer = new();
		if (unionModel.NamespaceName != null)
		{
			writer.WriteLine($"namespace {unionModel.NamespaceName};");
			writer.WriteLine();
		}

		foreach (ContainingTypeModel containingType in unionModel.ContainingTypes)
		{
			writer.WriteLine($"partial {containingType.Keyword} {containingType.Identifier}");
			writer.StartBlock();
		}

		if (unionModel.AllowMemoryOverlap)
			writer.WriteLine("[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Explicit)]");

		writer.WriteLine($"partial struct {_self} : global::System.Runtime.CompilerServices.IUnion, global::System.IEquatable<{_self}>");
		writer.StartBlock();
		GenerateConstants(writer);
		GenerateFields(writer);
		GenerateConstructors(writer);
		GenerateImplicitConversions(writer);
		GenerateValueProperty(writer);
		GenerateTryGetValueMethods(writer);
		GenerateEqualityMembers(writer);
		GenerateToString(writer);
		GenerateTypeStringMembers(writer);
		writer.EndBlock();

		for (int i = 0; i < unionModel.ContainingTypes.Count; i++)
			writer.EndBlock();

		return writer.ToString();
	}

	private void GenerateConstants(CodeWriter writer)
	{
		writer.WriteLine($"public const {_int32} CaseCount = {unionModel.Cases.Count};");
		for (int i = 0; i < unionModel.Cases.Count; i++)
			writer.WriteLine($"public const {_int32} {unionModel.Cases[i].IndexName} = {i};");

		writer.WriteLine();
	}

	private void GenerateFields(CodeWriter writer)
	{
		// Public so callers can take `ref` to a payload. When overlapped, writing the wrong case's field corrupts the
		// active one: that is the library's deliberate memory-unsafety.
		string readOnly = unionModel.IsReadOnly ? "readonly " : string.Empty;
		writer.WriteLine($"{FieldOffset(0)}public readonly {_int32} {GeneratorConstants.CaseIndexFieldName};");
		writer.WriteLine();

		foreach (UnionCaseModel unionCase in unionModel.Cases)
			writer.WriteLine($"{FieldOffset(_payloadOffset)}public {readOnly}{unionCase.TypeName} {unionCase.FieldName};");

		writer.WriteLine();
	}

	private string FieldOffset(int offset)
	{
		return unionModel.AllowMemoryOverlap ? $"[{_fieldOffsetAttribute}({offset})] " : string.Empty;
	}

	private void GenerateConstructors(CodeWriter writer)
	{
		// `this = default` leaves every other case's reference-typed payload null, which is what it has always been.
		if (unionModel.HasNonNullableReferenceField)
			writer.WriteLine("#pragma warning disable CS8618 // The inactive cases' payload fields are default.");

		// `this = default` first, because overlapped fields cannot each be definitely assigned. Fields are qualified with
		// `this.` in case the user named the parameter after one of them.
		foreach (UnionCaseModel unionCase in unionModel.Cases)
			writer.WriteLine($"{unionCase.ConstructorModifiers}partial {unionModel.StructName}({unionCase.TypeName} {unionCase.ParameterName}) {{ this = default; this.{GeneratorConstants.CaseIndexFieldName} = {unionCase.IndexName}; this.{unionCase.FieldName} = {unionCase.ParameterName}; }}");

		if (unionModel.HasNonNullableReferenceField)
			writer.WriteLine("#pragma warning restore CS8618");

		writer.WriteLine();
	}

	private void GenerateImplicitConversions(CodeWriter writer)
	{
		// A convenience only: conversions do not register cases, constructors do.
		bool any = false;
		foreach (UnionCaseModel unionCase in unionModel.Cases)
		{
			if (!unionCase.EmitImplicitConversion)
				continue;

			writer.WriteLine($"public static implicit operator {_self}({unionCase.TypeName} value) => new(value);");
			any = true;
		}

		if (any)
			writer.WriteLine();
	}

	private void GenerateValueProperty(CodeWriter writer)
	{
		// The IUnion contract. It boxes, so pattern matching goes through TryGetValue instead and never reads it.
		string type = unionModel.ValueIsNullable ? "global::System.Object?" : "global::System.Object";
		writer.WriteLine($"public readonly {type} Value => {GeneratorConstants.CaseIndexFieldName} switch");
		writer.StartBlock();
		for (int i = 0; i < unionModel.Cases.Count; i++)
		{
			UnionCaseModel unionCase = unionModel.Cases[i];
			string pattern = i == unionModel.Cases.Count - 1 ? "_" : unionCase.IndexName;
			writer.WriteLine($"{pattern} => {unionCase.FieldName},");
		}

		writer.EndBlockWithSemicolon();
		writer.WriteLine();
	}

	private void GenerateTryGetValueMethods(CodeWriter writer)
	{
		// The non-boxing access pattern: the compiler lowers pattern matching through these. `readonly` so a
		// `ref readonly` / `in` receiver never needs a defensive copy.
		foreach (UnionCaseModel unionCase in unionModel.Cases)
			writer.WriteLine($"public readonly {_boolean} TryGetValue(out {unionCase.TypeName} value) {{ value = {unionCase.FieldName}; return {GeneratorConstants.CaseIndexFieldName} == {unionCase.IndexName}; }}");

		writer.WriteLine();
	}

	private void GenerateEqualityMembers(CodeWriter writer)
	{
		writer.WriteLine($"public readonly {_boolean} Equals({_self} other) => {GeneratorConstants.CaseIndexFieldName} == other.{GeneratorConstants.CaseIndexFieldName} && {GeneratorConstants.CaseIndexFieldName} switch");
		writer.StartBlock();
		foreach (UnionCaseModel unionCase in unionModel.Cases)
			writer.WriteLine($"{unionCase.IndexName} => global::System.Collections.Generic.EqualityComparer<{unionCase.TypeName}>.Default.Equals({unionCase.FieldName}, other.{unionCase.FieldName}),");

		writer.WriteLine("_ => true,");
		writer.EndBlockWithSemicolon();
		writer.WriteLine();

		writer.WriteLine($"public override readonly {_boolean} Equals(global::System.Object? obj) => obj is {_self} other && Equals(other);");
		writer.WriteLine();

		writer.WriteLine($"public override readonly {_int32} GetHashCode() => {GeneratorConstants.CaseIndexFieldName} switch");
		writer.StartBlock();
		foreach (UnionCaseModel unionCase in unionModel.Cases)
			writer.WriteLine($"{unionCase.IndexName} => global::System.HashCode.Combine({GeneratorConstants.CaseIndexFieldName}, {unionCase.FieldName}),");

		writer.WriteLine($"_ => {GeneratorConstants.CaseIndexFieldName},");
		writer.EndBlockWithSemicolon();
		writer.WriteLine();

		writer.WriteLine($"public static {_boolean} operator ==({_self} left, {_self} right) => left.Equals(right);");
		writer.WriteLine($"public static {_boolean} operator !=({_self} left, {_self} right) => !left.Equals(right);");
		writer.WriteLine();
	}

	private void GenerateToString(CodeWriter writer)
	{
		writer.WriteLine($"public override readonly {_string} ToString() => {GeneratorConstants.CaseIndexFieldName} switch");
		writer.StartBlock();
		foreach (UnionCaseModel unionCase in unionModel.Cases)
		{
			// A field-less payload has a single value, so the case's name says it all.
			if (unionCase.IsFieldless)
				writer.WriteLine($"{unionCase.IndexName} => \"{SourceBuilderUtils.EscapeStringLiteral(unionCase.DisplayName)}\",");
			else
				writer.WriteLine($"{unionCase.IndexName} => $\"{SourceBuilderUtils.EscapeInterpolatedStringLiteral(unionCase.DisplayName)} {{{{ Value = {{{unionCase.FieldName}}} }}}}\",");
		}

		writer.WriteLine("_ => \"<invalid>\",");
		writer.EndBlockWithSemicolon();
		writer.WriteLine();
	}

	private void GenerateTypeStringMembers(CodeWriter writer)
	{
		// The case type's name, or its DisplayName.
		writer.WriteLine($"public static {_string} GetTypeString({_int32} caseIndex) => caseIndex switch");
		writer.StartBlock();
		foreach (UnionCaseModel unionCase in unionModel.Cases)
			writer.WriteLine($"{unionCase.IndexName} => \"{SourceBuilderUtils.EscapeStringLiteral(unionCase.DisplayName)}\",");

		writer.WriteLine($"_ => {_unreachable},");
		writer.EndBlockWithSemicolon();
		writer.WriteLine();

		writer.WriteLine($"public readonly {_string} GetTypeString() => GetTypeString({GeneratorConstants.CaseIndexFieldName});");
		writer.WriteLine();

		writer.WriteLine($"public static {_utf8Span} GetTypeAsUtf8Span({_int32} caseIndex) => caseIndex switch");
		writer.StartBlock();
		foreach (UnionCaseModel unionCase in unionModel.Cases)
			writer.WriteLine($"{unionCase.IndexName} => \"{SourceBuilderUtils.EscapeStringLiteral(unionCase.DisplayName)}\"u8,");

		writer.WriteLine($"_ => {_unreachable},");
		writer.EndBlockWithSemicolon();
		writer.WriteLine();

		writer.WriteLine($"public readonly {_utf8Span} GetTypeAsUtf8Span() => GetTypeAsUtf8Span({GeneratorConstants.CaseIndexFieldName});");
		writer.WriteLine();

		string names = string.Concat(unionModel.Cases.Select(c => $"{SourceBuilderUtils.EscapeStringLiteral(c.DisplayName)}\\0"));
		writer.WriteLine($"public static {_utf8Span} NullTerminatedMemberNames => \"{names}\"u8;");
	}
}
