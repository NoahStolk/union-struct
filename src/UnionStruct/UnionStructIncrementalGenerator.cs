using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using UnionStruct.Internals;
using UnionStruct.Internals.Model;
using UnionStruct.Internals.Utils;

namespace UnionStruct;

[Generator]
public sealed class UnionStructIncrementalGenerator : IIncrementalGenerator
{
	private const string _unionAttributeName = "UnionAttribute";
	private const string _unionCaseAttributeName = "UnionCaseAttribute";

	private const string _unionAttributeSourceCode =
		$"""
		 namespace {GeneratorConstants.RootNamespace};

		 [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
		 public sealed class {_unionAttributeName} : global::System.Attribute;
		 """;

	private const string _unionCaseAttributeSourceCode =
		$"""
		 namespace {GeneratorConstants.RootNamespace};

		 [global::System.AttributeUsage(global::System.AttributeTargets.Method)]
		 public sealed class {_unionCaseAttributeName} : global::System.Attribute;
		 """;

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(ctx => ctx.AddSource($"{_unionAttributeName}.g.cs", SourceText.From(SourceBuilderUtils.Build(_unionAttributeSourceCode), Encoding.UTF8)));
		context.RegisterPostInitializationOutput(ctx => ctx.AddSource($"{_unionCaseAttributeName}.g.cs", SourceText.From(SourceBuilderUtils.Build(_unionCaseAttributeSourceCode), Encoding.UTF8)));

		// ! LINQ is used to filter out null values.
		IncrementalValuesProvider<UnionModel> provider = context.SyntaxProvider
			.CreateSyntaxProvider(
				(sn, _) => sn is StructDeclarationSyntax,
				(ctx, _) => GetUnionModel(ctx))
			.Where(um => um != null)
			.Select((um, _) => um!);

		context.RegisterSourceOutput(
			context.CompilationProvider.Combine(provider.Collect()),
			(ctx, t) => GenerateUnionStruct(ctx, t.Left, t.Right));
	}

	private static UnionModel? GetUnionModel(GeneratorSyntaxContext context)
	{
		StructDeclarationSyntax structDeclarationSyntax = (StructDeclarationSyntax)context.Node;

		bool isUnion = false;
		foreach (AttributeListSyntax attributeListSyntax in structDeclarationSyntax.AttributeLists)
		{
			foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
			{
				if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
					continue;

				if (attributeSymbol.ContainingType.ToDisplayString() != $"{GeneratorConstants.RootNamespace}.{_unionAttributeName}")
					continue;

				isUnion = true;
				break;
			}
		}

		if (!isUnion)
			return null;

		List<UnionCaseModel> cases = [];
		foreach (MethodDeclarationSyntax methodDeclarationSyntax in structDeclarationSyntax.Members.OfType<MethodDeclarationSyntax>())
		{
			foreach (AttributeListSyntax attributeListSyntax in methodDeclarationSyntax.AttributeLists)
			{
				foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
				{
					if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
						continue;

					string attributeName = attributeSymbol.ContainingType.ToDisplayString();

					if (attributeName == $"{GeneratorConstants.RootNamespace}.{_unionCaseAttributeName}")
					{
						List<UnionCaseDataTypeModel> dataTypes = [];
						foreach (ParameterSyntax parameterSyntax in methodDeclarationSyntax.ParameterList.Parameters)
						{
							if (parameterSyntax.Type == null)
								continue;

							ITypeSymbol? parameterType = ModelExtensions.GetTypeInfo(context.SemanticModel, parameterSyntax.Type).Type;
							if (parameterType != null)
								dataTypes.Add(new UnionCaseDataTypeModel(parameterSyntax.Identifier.Text, parameterType));
						}

						cases.Add(new UnionCaseModel(methodDeclarationSyntax.Identifier.Text, dataTypes));
					}
				}
			}
		}

		return new UnionModel(structDeclarationSyntax, cases);
	}

	private static void GenerateUnionStruct(SourceProductionContext context, Compilation compilation, ImmutableArray<UnionModel> unionModels)
	{
		foreach (UnionModel unionModel in unionModels)
		{
			SemanticModel semanticModel = compilation.GetSemanticModel(unionModel.StructDeclarationSyntax.SyntaxTree);
			if (ModelExtensions.GetDeclaredSymbol(semanticModel, unionModel.StructDeclarationSyntax) is not INamedTypeSymbol structSymbol)
				continue;

			string namespaceName = structSymbol.ContainingNamespace.ToDisplayString();
			string accessibility = structSymbol.DeclaredAccessibility.ToString().ToLowerInvariant();

			UnionGenerator generator = new(compilation, unionModel, namespaceName, accessibility);
			string sourceCode = SourceBuilderUtils.Build(generator.Generate());

			context.AddSource($"{unionModel.StructIdentifier.Replace('<', '(').Replace('>', ')')}.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
		}
	}
}
