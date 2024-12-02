using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
		return Verifier.Verify(driver, _settings);
	}
}
