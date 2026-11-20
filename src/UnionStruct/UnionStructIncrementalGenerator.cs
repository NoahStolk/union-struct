using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using UnionStruct.Internals;
using UnionStruct.Internals.Model;
using UnionStruct.Internals.ModelBuilders;
using UnionStruct.Internals.Utils;

namespace UnionStruct;

[Generator]
public sealed class UnionStructIncrementalGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// [UnionCase] must be added during post-initialization: the model builder resolves it through the semantic
		// model, which only sees post-initialization sources when it runs.
		context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
			AttributeSourceUtils.HintName,
			SourceText.From(SourceBuilderUtils.Build(AttributeSourceUtils.SourceCode), Encoding.UTF8)));

		// ! LINQ is used to filter out null values.
		IncrementalValuesProvider<UnionGenerationResult> results = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				GeneratorConstants.BclUnionAttributeMetadataName,
				static (node, _) => node is StructDeclarationSyntax,
				UnionModelBuilder.Build)
			.Where(static r => r != null)
			.Select(static (r, _) => r!);

		context.RegisterSourceOutput(results, static (ctx, result) =>
		{
			foreach (DiagnosticInfo diagnostic in result.Diagnostics)
				ctx.ReportDiagnostic(diagnostic.ToDiagnostic());

			if (result.Model == null)
				return;

			UnionGenerator generator = new(result.Model);
			ctx.AddSource(result.Model.HintName, SourceText.From(SourceBuilderUtils.Build(generator.Generate()), Encoding.UTF8));
		});
	}
}
