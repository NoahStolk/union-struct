using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace UnionStruct.Tests.Utils;

internal static class TestHelper
{
	private static readonly CSharpCompilationOptions _compilationOptions = new(
		outputKind: OutputKind.DynamicallyLinkedLibrary,
		generalDiagnosticOption: ReportDiagnostic.Error,
		nullableContextOptions: NullableContextOptions.Enable);

	public static Task Verify(string source)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "UnionStruct.Tests",
			syntaxTrees: [syntaxTree],
			references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
			options: _compilationOptions);

		UnionStructIncrementalGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out _);

		ImmutableArray<Diagnostic> diagnostics = outputCompilation.GetDiagnostics();
		if (diagnostics.Length > 0)
			return Task.FromException(new InvalidOperationException($"Post-generator compilation failed ({diagnostics.Length} errors):\n{string.Join(Environment.NewLine, diagnostics)}"));

		return Verifier.Verify(driver).UseDirectory(Path.Combine("..", "snapshots"));
	}
}
