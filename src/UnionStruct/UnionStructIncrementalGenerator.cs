using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using UnionStruct.Internals;
using UnionStruct.Internals.Model;
using UnionStruct.Internals.ModelBuilders;
using UnionStruct.Internals.Utils;

namespace UnionStruct;

[Generator]
public sealed class UnionStructIncrementalGenerator : IIncrementalGenerator
{
	private const string _unionAttributeSourceCode =
		$"""
		 namespace {GeneratorConstants.RootNamespace};

		 [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
		 public sealed class {GeneratorConstants.UnionAttributeName} : global::System.Attribute;
		 """;

	private const string _unionCaseAttributeSourceCode =
		$"""
		 namespace {GeneratorConstants.RootNamespace};

		 [global::System.AttributeUsage(global::System.AttributeTargets.Method)]
		 public sealed class {GeneratorConstants.UnionCaseAttributeName} : global::System.Attribute;
		 """;

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(ctx => ctx.AddSource($"{GeneratorConstants.UnionAttributeName}.g.cs", SourceText.From(SourceBuilderUtils.Build(_unionAttributeSourceCode), Encoding.UTF8)));
		context.RegisterPostInitializationOutput(ctx => ctx.AddSource($"{GeneratorConstants.UnionCaseAttributeName}.g.cs", SourceText.From(SourceBuilderUtils.Build(_unionCaseAttributeSourceCode), Encoding.UTF8)));

		// ! LINQ is used to filter out null values.
		IncrementalValuesProvider<UnionModel> unionModelProvider = context.SyntaxProvider
			.CreateSyntaxProvider(
				(sn, _) => sn is StructDeclarationSyntax,
				(ctx, _) => GetUnionModel(ctx))
			.Where(um => um != null)
			.Select((um, _) => um!);

		context.RegisterSourceOutput(
			context.CompilationProvider.Combine(unionModelProvider.Collect()),
			(ctx, t) => GenerateUnionStruct(ctx, t.Left, t.Right));
	}

	private static UnionModel? GetUnionModel(GeneratorSyntaxContext context)
	{
		StructDeclarationSyntax structDeclarationSyntax = (StructDeclarationSyntax)context.Node;

		bool hasUnionAttribute = false;
		foreach (AttributeListSyntax attributeListSyntax in structDeclarationSyntax.AttributeLists)
		{
			foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
			{
				if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
					continue;

				if (attributeSymbol.ContainingType.ToDisplayString() != $"{GeneratorConstants.RootNamespace}.{GeneratorConstants.UnionAttributeName}")
					continue;

				hasUnionAttribute = true;
				break;
			}
		}

		if (!hasUnionAttribute)
			return null;

		UnionModelBuilder builder = new(context.SemanticModel, structDeclarationSyntax);
		return builder.TryCreateUnionModel();
	}

	private static void GenerateUnionStruct(SourceProductionContext context, Compilation compilation, ImmutableArray<UnionModel> unionModels)
	{
		foreach (UnionModel unionModel in unionModels)
		{
			UnionGenerator generator = new(compilation, unionModel);
			string sourceCode = SourceBuilderUtils.Build(generator.Generate());

			context.AddSource($"{unionModel.StructIdentifier.Replace('<', '(').Replace('>', ')')}.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
		}
	}
}
