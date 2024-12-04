using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace UnionStruct.Tests.Utils;

internal static class TestHelper
{
	private static readonly VerifySettings _settings = new();

	static TestHelper()
	{
		_settings.UseDirectory(Path.Combine("..", "snapshots"));
	}

	public static Task Verify(string source)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "UnionStruct.Tests",
			syntaxTrees: [syntaxTree],
			references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

		UnionStructIncrementalGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		driver = driver.RunGenerators(compilation);

		GeneratorDriverRunResult runResult = driver.GetRunResult();
		ImmutableArray<Diagnostic> postGeneratorErrors = [..runResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)];
		if (postGeneratorErrors.Length > 0)
			return Task.FromException(new InvalidOperationException($"Post-generator compilation failed ({postGeneratorErrors.Length} errors): {string.Join(Environment.NewLine, postGeneratorErrors)}"));

		return Verifier.Verify(driver, _settings);
	}
}
