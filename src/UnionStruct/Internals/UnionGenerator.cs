using Microsoft.CodeAnalysis;
using UnionStruct.Internals.Model;
using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals;

internal sealed class UnionGenerator(Compilation compilation, UnionModel unionModel, string namespaceName, string accessibility)
{
	public string Generate()
	{
		CodeWriter writer = new();
		writer.WriteLine($"namespace {namespaceName};");
		writer.WriteLine();
		if (unionModel.AllowMemoryOverlap)
			writer.WriteLine("[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Explicit)]");
		writer.WriteLine($"{accessibility} partial struct {unionModel.StructIdentifier} : global::System.IEquatable<{unionModel.StructIdentifier}>");
		writer.StartBlock();
		GenerateUnionCaseConstants(writer);
		if (unionModel.AllowMemoryOverlap)
			writer.WriteLine("[global::System.Runtime.InteropServices.FieldOffset(0)]");
		writer.WriteLine("public readonly global::System.Int32 CaseIndex;");
		writer.WriteLine();
		GenerateUnionCaseDataFields(writer);
		GeneratePrivateConstructor(writer);
		GenerateIsMethods(writer);
		GenerateFactoryMethods(writer);
		GenerateSwitchMethod(writer);
		GenerateMatchMethod(writer);
		GenerateToStringMethod(writer);
		GenerateEqualityOperators(writer);
		GenerateGetHashCodeMethod(writer);
		GenerateEqualsMethods(writer);
		GenerateNestedTypes(writer);
		writer.EndBlock();

		return writer.ToString();
	}

	private void GenerateUnionCaseConstants(CodeWriter writer)
	{
		int index = 0;
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
			writer.WriteLine($"public const global::System.Int32 {unionCaseModel.CaseIndexFieldName} = {index++};");
		writer.WriteLine();
	}

	private void GenerateUnionCaseDataFields(CodeWriter writer)
	{
		const int fieldOffset = 4;

		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
		{
			if (unionCaseModel.DataTypes.Count == 0)
				continue;

			if (unionModel.AllowMemoryOverlap)
				writer.WriteLine($"[global::System.Runtime.InteropServices.FieldOffset({fieldOffset})]");
			writer.WriteLine($"public {unionCaseModel.GetCaseTypeName(includeNullability: true)} {unionCaseModel.CaseFieldName};");
			writer.WriteLine();
		}
	}

	private void GeneratePrivateConstructor(CodeWriter writer)
	{
		writer.WriteLine($"private {unionModel.StructName}(global::System.Int32 caseIndex)");
		writer.StartBlock();
		writer.WriteLine("CaseIndex = caseIndex;");
		writer.EndBlock();
		writer.WriteLine();
	}

	private void GenerateIsMethods(CodeWriter writer)
	{
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
			writer.WriteLine($"public bool Is{unionCaseModel.CaseName} => CaseIndex == {unionCaseModel.CaseIndexFieldName};");
		writer.WriteLine();
	}

	private void GenerateFactoryMethods(CodeWriter writer)
	{
		const string localName = "___factoryReturnValue"; // TODO: Find a better way to avoid naming conflicts with parameter names.

		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
		{
			List<string> parameterDeclarations = unionCaseModel.DataTypes.Select(dt => $"{dt.GetFullyQualifiedTypeName(includeNullability: true)} {dt.FactoryParameterName}").ToList();

			writer.WriteLine($"public static partial {unionModel.StructIdentifier} {unionCaseModel.CaseName}(");

			writer.StartIndent();
			for (int i = 0; i < parameterDeclarations.Count; i++)
				writer.WriteLine($"{parameterDeclarations[i]}{(i < parameterDeclarations.Count - 1 ? "," : string.Empty)}");
			writer.EndIndent();
			writer.WriteLine(")");

			writer.StartBlock();
			writer.WriteLine($"{unionModel.StructIdentifier} {localName} = new({unionCaseModel.CaseIndexFieldName});");

			if (parameterDeclarations.Count == 1)
			{
				writer.WriteLine($"{localName}.{unionCaseModel.CaseFieldName} = {unionCaseModel.DataTypes[0].FactoryParameterName};");
			}
			else
			{
				foreach (UnionCaseDataTypeModel dt in unionCaseModel.DataTypes)
					writer.WriteLine($"{localName}.{unionCaseModel.CaseFieldName}.{dt.FieldName} = {dt.FactoryParameterName};");
			}

			writer.WriteLine($"return {localName};");

			writer.EndBlock();
			writer.WriteLine();
		}
	}

	private void GenerateSwitchMethod(CodeWriter writer)
	{
		List<string> parameters = unionModel.Cases.Select(ucm => $"{ucm.GetActionType()} {ucm.ParameterName}").ToList();

		writer.WriteLine("public void Switch(");

		writer.StartIndent();
		for (int i = 0; i < parameters.Count; i++)
			writer.WriteLine($"{parameters[i]}{(i < parameters.Count - 1 ? "," : string.Empty)}");
		writer.EndIndent();
		writer.WriteLine(")");

		writer.StartBlock();
		writer.WriteLine("switch (CaseIndex)");
		writer.StartBlock();
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
			writer.WriteLine($"case {unionCaseModel.CaseIndexFieldName}: {unionCaseModel.ParameterName}.Invoke({unionCaseModel.GetInvocationParameters()}); break;");
		writer.WriteLine("default: throw new global::System.Diagnostics.UnreachableException($\"Invalid case index: {CaseIndex}.\");");
		writer.EndBlock();
		writer.EndBlock();
		writer.WriteLine();
	}

	private void GenerateMatchMethod(CodeWriter writer)
	{
		const string typeParameterName = "TMatchOut"; // TODO: Find a better way to avoid naming conflicts with struct type parameter names.

		List<string> parameters = unionModel.Cases.Select(ucm => $"{ucm.GetFuncType(typeParameterName)} {ucm.ParameterName}").ToList();

		writer.WriteLine($"public {typeParameterName} Match<{typeParameterName}>(");

		writer.StartIndent();
		for (int i = 0; i < parameters.Count; i++)
			writer.WriteLine($"{parameters[i]}{(i < parameters.Count - 1 ? "," : string.Empty)}");
		writer.EndIndent();
		writer.WriteLine(")");

		writer.StartBlock();
		writer.WriteLine("return CaseIndex switch");
		writer.StartBlock();
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
			writer.WriteLine($"{unionCaseModel.CaseIndexFieldName} => {unionCaseModel.ParameterName}.Invoke({unionCaseModel.GetInvocationParameters()}),");
		writer.WriteLine("_ => throw new global::System.Diagnostics.UnreachableException($\"Invalid case index: {CaseIndex}.\"),");
		writer.EndBlockWithSemicolon();
		writer.EndBlock();
		writer.WriteLine();
	}

	private void GenerateToStringMethod(CodeWriter writer)
	{
		writer.WriteLine("public override global::System.String ToString()");
		writer.StartBlock();
		writer.WriteLine("return CaseIndex switch");
		writer.StartBlock();
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
			writer.WriteLine($"{unionCaseModel.CaseIndexFieldName} => {unionCaseModel.GetToStringReturnValue()},");

		writer.WriteLine("_ => throw new global::System.Diagnostics.UnreachableException($\"Invalid case index: {CaseIndex}.\"),");
		writer.EndBlockWithSemicolon();
		writer.EndBlock();
		writer.WriteLine();
	}

	private void GenerateEqualityOperators(CodeWriter writer)
	{
		writer.WriteLine($"public static bool operator !=({unionModel.StructIdentifier} left, {unionModel.StructIdentifier} right)");
		writer.StartBlock();
		writer.WriteLine("return !(left == right);");
		writer.EndBlock();
		writer.WriteLine();

		writer.WriteLine($"public static bool operator ==({unionModel.StructIdentifier} left, {unionModel.StructIdentifier} right)");
		writer.StartBlock();
		writer.WriteLine("return left.Equals(right);");
		writer.EndBlock();
		writer.WriteLine();
	}

	private void GenerateGetHashCodeMethod(CodeWriter writer)
	{
		const int prime = -1521134295;

		writer.WriteLine("public override global::System.Int32 GetHashCode()");
		writer.StartBlock();

		writer.WriteLine("return CaseIndex switch");
		writer.StartBlock();
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
		{
			if (unionCaseModel.DataTypes.Count == 0)
			{
				writer.WriteLine($"{unionCaseModel.CaseIndexFieldName} => unchecked ( {unionCaseModel.CaseIndexFieldName} ),");
			}
			else
			{
				string fields = string.Join($" * {prime} + ", unionCaseModel.DataTypes.Select(dt =>
				{
					string fieldName = unionCaseModel.DataTypes.Count == 1 ? unionCaseModel.CaseFieldName : $"{unionCaseModel.CaseFieldName}.{dt.FieldName}";
					string equalityComparer = $"global::System.Collections.Generic.EqualityComparer<{dt.GetFullyQualifiedTypeName(includeNullability: true)}>.Default";

					bool isNullableOfT = SymbolEqualityComparer.IncludeNullability.Equals(dt.TypeSymbol.OriginalDefinition, compilation.GetTypeByMetadataName("System.Nullable`1"));
					if (isNullableOfT)
						return $"({fieldName}.HasValue ? {equalityComparer}.GetHashCode({fieldName}.Value) : 0)";

					string getHashCodeCall = $"{equalityComparer}.GetHashCode({fieldName})";
					return dt.GetNullableFlowState() == NullableFlowState.MaybeNull ? $"({fieldName} == null ? 0 : {getHashCodeCall})" : getHashCodeCall;
				}));
				writer.WriteLine($"{unionCaseModel.CaseIndexFieldName} => unchecked ( {unionCaseModel.CaseIndexFieldName} * {prime} + {fields} ),");
			}
		}

		writer.WriteLine($"_ => {unionModel.Cases.Count},");
		writer.EndBlockWithSemicolon();

		writer.EndBlock();
		writer.WriteLine();
	}

	private void GenerateEqualsMethods(CodeWriter writer)
	{
		writer.WriteLine("public override global::System.Boolean Equals(global::System.Object? obj)");
		writer.StartBlock();
		writer.WriteLine($"return obj is {unionModel.StructIdentifier} && Equals(({unionModel.StructIdentifier})obj);");
		writer.EndBlock();
		writer.WriteLine();

		writer.WriteLine($"public global::System.Boolean Equals({unionModel.StructIdentifier} other)");
		writer.StartBlock();
		writer.WriteLine("return CaseIndex == other.CaseIndex && CaseIndex switch");
		writer.StartBlock();
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
		{
			if (unionCaseModel.DataTypes.Count == 0)
			{
				writer.WriteLine($"{unionCaseModel.CaseIndexFieldName} => true,");
			}
			else
			{
				string fields = string.Join(" && ", unionCaseModel.DataTypes.Select(dt =>
				{
					string fieldName = unionCaseModel.DataTypes.Count == 1 ? unionCaseModel.CaseFieldName : $"{unionCaseModel.CaseFieldName}.{dt.FieldName}";
					return $"global::System.Collections.Generic.EqualityComparer<{dt.GetFullyQualifiedTypeName(includeNullability: true)}>.Default.Equals({fieldName}, other.{fieldName})";
				}));
				writer.WriteLine($"{unionCaseModel.CaseIndexFieldName} => {fields},");
			}
		}

		writer.WriteLine("_ => true,");
		writer.EndBlockWithSemicolon();

		writer.EndBlock();
		writer.WriteLine();
	}

	private void GenerateNestedTypes(CodeWriter writer)
	{
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
		{
			if (unionCaseModel.DataTypes.Count <= 1)
				continue;

			writer.WriteLine($"public struct {unionCaseModel.GetCaseTypeName(includeNullability: false)}");
			writer.StartBlock();
			foreach (UnionCaseDataTypeModel dataType in unionCaseModel.DataTypes)
			{
				writer.WriteLine($"public {dataType.GetFullyQualifiedTypeName(includeNullability: true)} {dataType.FieldName};");
				writer.WriteLine();
			}

			writer.EndBlock();
			writer.WriteLine();
		}
	}
}
