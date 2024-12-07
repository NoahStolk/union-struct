using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnionStruct.Internals.Model;
using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals.ModelBuilders;

internal sealed class UnionModelBuilder(SemanticModel semanticModel, StructDeclarationSyntax structDeclarationSyntax)
{
	public UnionModel? TryCreateUnionModel()
	{
		if (semanticModel.GetDeclaredSymbol(structDeclarationSyntax) is not INamedTypeSymbol structSymbol)
			return null;

		IReadOnlyList<UnionCaseModel> cases = GetUnionCases();

		string structName = structDeclarationSyntax.Identifier.Text;
		string structIdentifier = GetStructIdentifier(structName);
		bool allowMemoryOverlap = AllowMemoryOverlap(cases);
		string namespaceName = structSymbol.ContainingNamespace.ToDisplayString();
		string accessibility = structSymbol.DeclaredAccessibility.ToString().ToLowerInvariant();

		return new UnionModel
		{
			Cases = cases,
			StructName = structName,
			StructIdentifier = structIdentifier,
			NamespaceName = namespaceName,
			Accessibility = accessibility,
			AllowMemoryOverlap = allowMemoryOverlap,
		};
	}

	private string GetStructIdentifier(string structName)
	{
		SeparatedSyntaxList<TypeParameterSyntax>? typeParameters = structDeclarationSyntax.TypeParameterList?.Parameters;
		if (typeParameters is { Count: > 0 })
			return $"{structName}<{string.Join(", ", typeParameters.Value.Select(tp => tp.Identifier.Text))}>";

		return structName;
	}

	private bool AllowMemoryOverlap(IReadOnlyList<UnionCaseModel> cases)
	{
		SeparatedSyntaxList<TypeParameterSyntax>? typeParameters = structDeclarationSyntax.TypeParameterList?.Parameters;
		if (typeParameters is { Count: > 0 })
			return false;

		foreach (UnionCaseModel unionCase in cases)
		{
			if (unionCase.DataTypes.Any(dt => dt.TypeSymbol.IsReferenceType))
				return false;
		}

		return true;
	}

	private List<UnionCaseModel> GetUnionCases()
	{
		List<UnionCaseModel> cases = [];
		foreach (MethodDeclarationSyntax methodDeclarationSyntax in structDeclarationSyntax.Members.OfType<MethodDeclarationSyntax>())
		{
			foreach (AttributeListSyntax attributeListSyntax in methodDeclarationSyntax.AttributeLists)
			{
				foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
				{
					if (semanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
						continue;

					string attributeName = attributeSymbol.ContainingType.ToDisplayString();

					if (attributeName == $"{GeneratorConstants.RootNamespace}.{GeneratorConstants.UnionCaseAttributeName}")
					{
						List<UnionCaseDataTypeModel> dataTypes = [];
						foreach (ParameterSyntax parameterSyntax in methodDeclarationSyntax.ParameterList.Parameters)
						{
							if (parameterSyntax.Type == null)
								continue;

							ITypeSymbol? parameterType = semanticModel.GetTypeInfo(parameterSyntax.Type).Type;
							if (parameterType != null)
							{
								UnionCaseDataTypeModelBuilder builder = new(parameterSyntax, parameterType);
								dataTypes.Add(builder.CreateUnionCaseDataTypeModel());
							}
						}

						cases.Add(new UnionCaseModel(methodDeclarationSyntax.Identifier.Text, dataTypes));
					}
				}
			}
		}

		return cases;
	}
}
