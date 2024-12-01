using Microsoft.CodeAnalysis;
using UnionStruct.Internals.Utils;

namespace UnionStruct.Model;

public sealed record UnionCaseDataTypeModel(string Name, INamedTypeSymbol NamedTypeSymbol)
{
	public string Name { get; } = Name;

	public INamedTypeSymbol NamedTypeSymbol { get; } = NamedTypeSymbol;

	public string FieldName => Name.FirstCharToUpperCase();

	public string ParameterName => SourceBuilderUtils.ToEscapedLocal(Name);

	public string GetFullyQualifiedTypeName()
	{
		string namespaceName = NamedTypeSymbol.ContainingNamespace.ToDisplayString();
		return $"{namespaceName}.{NamedTypeSymbol.Name}";
	}
}
