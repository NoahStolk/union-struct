using Microsoft.CodeAnalysis;

namespace UnionStruct.Model;

public sealed record UnionCaseDataTypeModel(INamedTypeSymbol NamedTypeSymbol)
{
	public INamedTypeSymbol NamedTypeSymbol { get; } = NamedTypeSymbol;

	public string FieldName => NamedTypeSymbol.Name;

	public string GetFullyQualifiedTypeName()
	{
		string namespaceName = NamedTypeSymbol.ContainingNamespace.ToDisplayString();
		return $"{namespaceName}.{NamedTypeSymbol.Name}";
	}
}
