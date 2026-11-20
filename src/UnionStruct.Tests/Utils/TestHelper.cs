using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using UnionStruct.Internals.Utils;

namespace UnionStruct.Tests.Utils;

internal static class TestHelper
{
	private static readonly CSharpCompilationOptions _compilationOptions = new(
		outputKind: OutputKind.DynamicallyLinkedLibrary,
		allowUnsafe: false,
		generalDiagnosticOption: ReportDiagnostic.Warn,
		nullableContextOptions: NullableContextOptions.Enable);

	/// <summary>
	/// Verifies the union generated for <paramref name="source"/>, and that the result compiles without a single
	/// diagnostic. The attributes the generator adds during post-initialization are excluded, since they are identical
	/// for every compilation; <see cref="VerifyIncludingAttributes"/> covers those.
	/// </summary>
	public static Task Verify(string source)
	{
		return Verify(source, LanguageVersion.CSharp15, includeAttributes: false, requireCleanCompilation: true);
	}

	/// <summary>Verifies the attributes the generator emits into the consuming compilation.</summary>
	public static Task VerifyIncludingAttributes(string source)
	{
		return Verify(source, LanguageVersion.CSharp15, includeAttributes: true, requireCleanCompilation: true);
	}

	/// <summary>
	/// Verifies the generator's output and diagnostics for <paramref name="source"/> without requiring the result to
	/// compile. Used for the generator's own diagnostics, which usually come with compiler errors of their own.
	/// </summary>
	public static Task VerifyDiagnostics(string source, LanguageVersion languageVersion = LanguageVersion.CSharp15)
	{
		return Verify(source, languageVersion, includeAttributes: false, requireCleanCompilation: false);
	}

	private static Task Verify(string source, LanguageVersion languageVersion, bool includeAttributes, bool requireCleanCompilation)
	{
		// The test host runs on net11.0, so its own System.Private.CoreLib carries the BCL [Union] and IUnion.
		// ! The location of a loaded framework assembly always has a directory.
		string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
		MetadataReference[] references =
		[
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "netstandard.dll")),
			MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Runtime.dll")),
			MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Numerics.Vectors.dll")),
			MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Collections.dll")),
		];

		CSharpParseOptions parseOptions = new(languageVersion);
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "UnionStruct.Tests",
			syntaxTrees: [syntaxTree],
			references: references,
			options: _compilationOptions);

		UnionStructIncrementalGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()], parseOptions: parseOptions);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out _);

		if (requireCleanCompilation)
		{
			ImmutableArray<Diagnostic> diagnostics = outputCompilation.GetDiagnostics();
			if (diagnostics.Length > 0)
				return Task.FromException(new InvalidOperationException($"Post-generator compilation failed ({diagnostics.Length} diagnostics):\n{string.Join(Environment.NewLine, diagnostics)}"));
		}

		SettingsTask settingsTask = Verifier.Verify(driver).UseDirectory(Path.Combine("..", "snapshots"));
		if (!includeAttributes)
			settingsTask = settingsTask.IgnoreGeneratedResult(r => r.HintName == AttributeSourceUtils.HintName);

		return settingsTask;
	}
}
