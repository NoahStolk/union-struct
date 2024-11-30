using Microsoft.CodeAnalysis;
using System.Globalization;

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
		string typeName = Type.Name;
		return typeName.Length switch
		{
			0 => throw new ArgumentException("Type name is empty.", nameof(Type)),
			1 => typeName.ToLower(CultureInfo.InvariantCulture),
			_ => $"{char.ToLower(typeName[0], CultureInfo.InvariantCulture)}{typeName.Substring(1)}",
		};
	}
}
