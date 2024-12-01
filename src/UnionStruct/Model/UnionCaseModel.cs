using Microsoft.CodeAnalysis;
using System.Globalization;
using UnionStruct.Internals;

namespace UnionStruct.Model;

public sealed record UnionCaseModel(INamedTypeSymbol Type)
{
	public INamedTypeSymbol Type { get; } = Type;

	public string GetFullyQualifiedTypeName()
	{
		string namespaceName = Type.ContainingNamespace.ToDisplayString();
		string typeName = Type.Name;

		return $"{namespaceName}.{typeName}";
	}

	public string GetParameterName()
	{
		return SourceBuilderUtils.ToEscapedLocal(Type.Name);
	}
}
