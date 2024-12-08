﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnionStruct.Internals.Model;
using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals.ModelBuilders;

internal sealed class UnionModelBuilder
{
	private readonly SemanticModel _semanticModel;
	private readonly StructDeclarationSyntax _structDeclarationSyntax;

	private readonly string _structName;
	private readonly string _structIdentifier;
	private readonly string _namespaceName;
	private readonly string _accessibility;

	public UnionModelBuilder(SemanticModel semanticModel, StructDeclarationSyntax structDeclarationSyntax, INamedTypeSymbol structSymbol)
	{
		_semanticModel = semanticModel;
		_structDeclarationSyntax = structDeclarationSyntax;

		_structName = _structDeclarationSyntax.Identifier.Text;
		_structIdentifier = GetStructIdentifier(_structName);
		_namespaceName = structSymbol.ContainingNamespace.ToDisplayString();
		_accessibility = structSymbol.DeclaredAccessibility.ToString().ToLowerInvariant();
	}

	public UnionModel Build()
	{
		// TODO: Find a better way to avoid naming conflicts with struct type parameter names.
		const string funcOutTypeParameterName = "TMatchOut";

		IReadOnlyList<UnionCaseModel> cases = GetUnionCases(funcOutTypeParameterName);

		return new UnionModel
		{
			Cases = cases,
			StructName = _structName,
			StructIdentifier = _structIdentifier,
			NamespaceName = _namespaceName,
			Accessibility = _accessibility,
			AllowMemoryOverlap = AllowMemoryOverlap(cases),
			FuncOutTypeParameterName = funcOutTypeParameterName,
		};
	}

	private string GetStructIdentifier(string structName)
	{
		SeparatedSyntaxList<TypeParameterSyntax>? typeParameters = _structDeclarationSyntax.TypeParameterList?.Parameters;
		if (typeParameters is { Count: > 0 })
			return $"{structName}<{string.Join(", ", typeParameters.Value.Select(tp => tp.Identifier.Text))}>";

		return structName;
	}

	private bool AllowMemoryOverlap(IReadOnlyList<UnionCaseModel> cases)
	{
		SeparatedSyntaxList<TypeParameterSyntax>? typeParameters = _structDeclarationSyntax.TypeParameterList?.Parameters;
		if (typeParameters is { Count: > 0 })
			return false;

		foreach (UnionCaseModel unionCase in cases)
		{
			if (unionCase.DataTypes.Any(dt => dt.TypeSymbol.IsReferenceType))
				return false;
		}

		return true;
	}

	private List<UnionCaseModel> GetUnionCases(string funcOutTypeParameterName)
	{
		List<UnionCaseModel> cases = [];
		foreach (MethodDeclarationSyntax methodDeclarationSyntax in _structDeclarationSyntax.Members.OfType<MethodDeclarationSyntax>())
		{
			foreach (AttributeListSyntax attributeListSyntax in methodDeclarationSyntax.AttributeLists)
			{
				foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
				{
					if (_semanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
						continue;

					string attributeName = attributeSymbol.ContainingType.ToDisplayString();
					if (attributeName != $"{GeneratorConstants.RootNamespace}.{GeneratorConstants.UnionCaseAttributeName}")
						continue;

					UnionCaseModelBuilder caseBuilder = new(_semanticModel, methodDeclarationSyntax, funcOutTypeParameterName);
					cases.Add(caseBuilder.Build());
				}
			}
		}

		return cases;
	}
}
