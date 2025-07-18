﻿using Microsoft.CodeAnalysis;
using UnionStruct.Internals.Model;
using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals;

internal sealed class UnionGenerator(Compilation compilation, UnionModel unionModel)
{
	private readonly INamedTypeSymbol? _nullableOfTTypeSymbol = compilation.GetTypeByMetadataName("System.Nullable`1");

	public string Generate()
	{
		CodeWriter writer = new();
		writer.WriteLine($"namespace {unionModel.NamespaceName};");
		writer.WriteLine();
		if (unionModel.AllowMemoryOverlap)
			writer.WriteLine("[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Explicit)]");
		writer.WriteLine($"{unionModel.Accessibility} partial struct {unionModel.StructIdentifier} : global::System.IEquatable<{unionModel.StructIdentifier}>");
		writer.StartBlock();
		writer.WriteLine($"public const global::System.Int32 CaseCount = {unionModel.Cases.Count};");
		writer.WriteLine();
		GenerateUnionCaseConstants(writer);
		if (unionModel.AllowMemoryOverlap)
			writer.WriteLine("[global::System.Runtime.InteropServices.FieldOffset(0)]");
		writer.WriteLine("public readonly global::System.Int32 CaseIndex;");
		writer.WriteLine();
		GenerateUnionCaseDataFields(writer);
		GeneratePrivateConstructor(writer);
		GenerateIsProperties(writer);
		GenerateNullTerminatedMemberNames(writer);
		GenerateFactoryMethods(writer);
		GenerateSwitchMethod(writer);
		GenerateMatchMethod(writer);
		GenerateToStringMethod(writer);
		GenerateTypeStringMethods(writer);
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

			if (unionCaseModel.DataTypes.Count == 1 && unionCaseModel.DataTypes[0].TypeSymbol.IsReferenceType && !unionCaseModel.DataTypes[0].IsNullableTypeSyntax && unionCaseModel.DataTypes[0].TypeSymbol.NullableAnnotation != NullableAnnotation.Annotated)
				writer.WriteLine($"public {unionCaseModel.CaseFieldTypeName} {unionCaseModel.CaseFieldName} = null!;");
			else if (unionCaseModel.DataTypes.Count == 1 && !unionCaseModel.DataTypes[0].IsNullableTypeSyntax && unionCaseModel.DataTypes[0].TypeParameterAllowsNullability)
				writer.WriteLine($"public {unionCaseModel.CaseFieldTypeName} {unionCaseModel.CaseFieldName} = default!;");
			else
				writer.WriteLine($"public {unionCaseModel.CaseFieldTypeName} {unionCaseModel.CaseFieldName};");

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

	private void GenerateIsProperties(CodeWriter writer)
	{
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
			writer.WriteLine($"public readonly bool Is{unionCaseModel.CaseName} => CaseIndex == {unionCaseModel.CaseIndexFieldName};");
		writer.WriteLine();
	}

	private void GenerateNullTerminatedMemberNames(CodeWriter writer)
	{
		if (unionModel.Cases.Count == 0)
			return;

		string nullTerminatedMemberNames = string.Concat(unionModel.Cases.Select(kvp => $"{kvp.GetDisplayName()}\\0"));
		writer.WriteLine($"public static global::System.ReadOnlySpan<global::System.Byte> NullTerminatedMemberNames => \"{nullTerminatedMemberNames}\"u8;");
		writer.WriteLine();
	}

	private void GenerateFactoryMethods(CodeWriter writer)
	{
		const string localName = "___factoryReturnValue"; // TODO: Find a better way to avoid naming conflicts with parameter names.

		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
		{
			List<string> parameterDeclarations = unionCaseModel.DataTypes.Select(dt => $"{dt.FullyQualifiedTypeName} {dt.FactoryParameterName}").ToList();

			writer.WriteLine($"public static partial {unionModel.StructIdentifier} {unionCaseModel.CaseName}(");

			writer.StartIndent();
			for (int i = 0; i < parameterDeclarations.Count; i++)
				writer.WriteLine($"{parameterDeclarations[i]}{(i < parameterDeclarations.Count - 1 ? "," : string.Empty)}");
			writer.EndIndent();
			writer.WriteLine(")");

			writer.StartBlock();
			writer.WriteLine($"{unionModel.StructIdentifier} {localName} = new({unionCaseModel.CaseIndexFieldName});");

			if (parameterDeclarations.Count == 1)
				writer.WriteLine($"{localName}.{unionCaseModel.CaseFieldName} = {unionCaseModel.DataTypes[0].FactoryParameterName};");
			else if (parameterDeclarations.Count > 1)
				writer.WriteLine($"{localName}.{unionCaseModel.CaseFieldName} = new({string.Join(", ", unionCaseModel.DataTypes.Select(dt => dt.FactoryParameterName))});");

			writer.WriteLine($"return {localName};");

			writer.EndBlock();
			writer.WriteLine();
		}
	}

	private void GenerateSwitchMethod(CodeWriter writer)
	{
		List<string> parameters = unionModel.Cases.Select(ucm => $"{ucm.ActionTypeName} {ucm.ParameterName}").ToList();

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
			writer.WriteLine($"case {unionCaseModel.CaseIndexFieldName}: {unionCaseModel.ParameterName}.Invoke({unionCaseModel.InvocationParameters}); break;");
		writer.WriteLine("default: throw new global::System.Diagnostics.UnreachableException($\"Invalid case index: {CaseIndex}.\");");
		writer.EndBlock();
		writer.EndBlock();
		writer.WriteLine();
	}

	private void GenerateMatchMethod(CodeWriter writer)
	{
		List<string> parameters = unionModel.Cases.Select(ucm => $"{ucm.FuncTypeName} {ucm.ParameterName}").ToList();

		writer.WriteLine($"public {unionModel.FuncOutTypeParameterName} Match<{unionModel.FuncOutTypeParameterName}>(");

		writer.StartIndent();
		for (int i = 0; i < parameters.Count; i++)
			writer.WriteLine($"{parameters[i]}{(i < parameters.Count - 1 ? "," : string.Empty)}");
		writer.EndIndent();
		writer.WriteLine(")");

		writer.StartBlock();
		writer.WriteLine("return CaseIndex switch");
		writer.StartBlock();
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
			writer.WriteLine($"{unionCaseModel.CaseIndexFieldName} => {unionCaseModel.ParameterName}.Invoke({unionCaseModel.InvocationParameters}),");
		writer.WriteLine("_ => throw new global::System.Diagnostics.UnreachableException($\"Invalid case index: {CaseIndex}.\"),");
		writer.EndBlockWithSemicolon();
		writer.EndBlock();
		writer.WriteLine();
	}

	private void GenerateToStringMethod(CodeWriter writer)
	{
		writer.WriteLine("public override readonly global::System.String ToString()");
		writer.StartBlock();
		writer.WriteLine("return CaseIndex switch");
		writer.StartBlock();
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
			writer.WriteLine($"{unionCaseModel.CaseIndexFieldName} => {unionCaseModel.ToStringReturnValue},");

		writer.WriteLine("_ => throw new global::System.Diagnostics.UnreachableException($\"Invalid case index: {CaseIndex}.\"),");
		writer.EndBlockWithSemicolon();
		writer.EndBlock();
		writer.WriteLine();
	}

	private void GenerateTypeStringMethods(CodeWriter writer)
	{
		writer.WriteLine("public static global::System.String GetTypeString(global::System.Int32 caseIndex)");
		writer.StartBlock();
		writer.WriteLine("return caseIndex switch");
		writer.StartBlock();
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
			writer.WriteLine($"{unionCaseModel.CaseIndexFieldName} => \"{unionCaseModel.GetDisplayName()}\",");

		writer.WriteLine("_ => throw new global::System.Diagnostics.UnreachableException($\"Invalid case index: {caseIndex}.\"),");
		writer.EndBlockWithSemicolon();
		writer.EndBlock();
		writer.WriteLine();

		writer.WriteLine("public readonly global::System.String GetTypeString()");
		writer.StartBlock();
		writer.WriteLine("return GetTypeString(CaseIndex);");
		writer.EndBlock();
		writer.WriteLine();

		writer.WriteLine("public static global::System.ReadOnlySpan<global::System.Byte> GetTypeAsUtf8Span(global::System.Int32 caseIndex)");
		writer.StartBlock();
		writer.WriteLine("return caseIndex switch");
		writer.StartBlock();
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
			writer.WriteLine($"{unionCaseModel.CaseIndexFieldName} => \"{unionCaseModel.GetDisplayName()}\"u8,");

		writer.WriteLine("_ => throw new global::System.Diagnostics.UnreachableException($\"Invalid case index: {caseIndex}.\"),");
		writer.EndBlockWithSemicolon();
		writer.EndBlock();
		writer.WriteLine();

		writer.WriteLine("public readonly global::System.ReadOnlySpan<global::System.Byte> GetTypeAsUtf8Span()");
		writer.StartBlock();
		writer.WriteLine("return GetTypeAsUtf8Span(CaseIndex);");
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

		writer.WriteLine("public override readonly global::System.Int32 GetHashCode()");
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
					string equalityComparer = $"global::System.Collections.Generic.EqualityComparer<{dt.FullyQualifiedTypeName}>.Default";

					bool isNullableOfT = SymbolEqualityComparer.IncludeNullability.Equals(dt.TypeSymbol.OriginalDefinition, _nullableOfTTypeSymbol);
					if (isNullableOfT)
						return $"({fieldName}.HasValue ? {equalityComparer}.GetHashCode({fieldName}.Value) : 0)";

					string getHashCodeCall = $"{equalityComparer}.GetHashCode({fieldName})";
					return dt.IsNullableReferenceType || dt.TypeParameterAllowsNullability ? $"({fieldName} == null ? 0 : {getHashCodeCall})" : getHashCodeCall;
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
		writer.WriteLine("public override readonly global::System.Boolean Equals(global::System.Object? obj)");
		writer.StartBlock();
		writer.WriteLine($"return obj is {unionModel.StructIdentifier} && Equals(({unionModel.StructIdentifier})obj);");
		writer.EndBlock();
		writer.WriteLine();

		writer.WriteLine($"public readonly global::System.Boolean Equals({unionModel.StructIdentifier} other)");
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
					return $"global::System.Collections.Generic.EqualityComparer<{dt.FullyQualifiedTypeName}>.Default.Equals({fieldName}, other.{fieldName})";
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

			writer.WriteLine($"public struct {unionCaseModel.CaseStructTypeIdentifier}");
			writer.StartBlock();
			foreach (UnionCaseDataTypeModel dataType in unionCaseModel.DataTypes)
			{
				writer.WriteLine($"public {dataType.FullyQualifiedTypeName} {dataType.FieldName};");
				writer.WriteLine();
			}

			writer.WriteLine($"public {unionCaseModel.CaseStructTypeIdentifier}(");
			writer.StartIndent();
			for (int i = 0; i < unionCaseModel.DataTypes.Count; i++)
				writer.WriteLine($"{unionCaseModel.DataTypes[i].FullyQualifiedTypeName} {unionCaseModel.DataTypes[i].FactoryParameterName}{(i < unionCaseModel.DataTypes.Count - 1 ? "," : string.Empty)}");
			writer.EndIndent();
			writer.WriteLine(")");

			writer.StartBlock();
			foreach (UnionCaseDataTypeModel dataType in unionCaseModel.DataTypes)
				writer.WriteLine($"{dataType.FieldName} = {dataType.FactoryParameterName};");

			writer.EndBlock();

			writer.EndBlock();
			writer.WriteLine();
		}
	}
}
