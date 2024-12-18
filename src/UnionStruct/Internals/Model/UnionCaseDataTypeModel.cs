using Microsoft.CodeAnalysis;

namespace UnionStruct.Internals.Model;

internal sealed record UnionCaseDataTypeModel
{
	public required ITypeSymbol TypeSymbol { get; init; }

	public required string FieldName { get; init; }

	public required string FactoryParameterName { get; init; }

	public required string FullyQualifiedTypeName { get; init; }

	public required string FullyQualifiedTypeNameWithoutNullability { get; init; }

	public required bool IsNullableReferenceType { get; init; }

	public required bool TypeParameterAllowsNullability { get; init; }

	public required bool IsNullableTypeSyntax { get; init; }
}
