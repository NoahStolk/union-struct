using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Reflection;

namespace UnionStruct.Tests.Analyzers;

internal static class AnalyzerTestHelper
{
	private static readonly CSharpCompilationOptions _compilationOptions = new(
		outputKind: OutputKind.DynamicallyLinkedLibrary,
		allowUnsafe: false,
		generalDiagnosticOption: ReportDiagnostic.Default,
		nullableContextOptions: NullableContextOptions.Enable);

	public static async Task<(Compilation Compilation, ImmutableArray<Diagnostic> AllDiagnostics)> CompileWithAnalyzersAsync(
		string source,
		ImmutableArray<DiagnosticAnalyzer> analyzers)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		Assembly netstandard = assemblies.Single(a => a.GetName().Name == "netstandard");
		Assembly systemRuntime = assemblies.Single(a => a.GetName().Name == "System.Runtime");

		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "UnionStruct.Tests.Analyzers",
			syntaxTrees: [syntaxTree],
			references:
			[
				MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(UnionAttribute).Assembly.Location),
				MetadataReference.CreateFromFile(netstandard.Location),
				MetadataReference.CreateFromFile(systemRuntime.Location),
			],
			options: _compilationOptions);

		UnionStructIncrementalGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		_ = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out _);

		AnalyzerOptions emptyOptions = new(ImmutableArray<AdditionalText>.Empty);
		CompilationWithAnalyzersOptions options = new(
			options: emptyOptions,
			onAnalyzerException: static (_, _, _) => { },
			concurrentAnalysis: false,
			logAnalyzerExecutionTime: false,
			reportSuppressedDiagnostics: true);
		CompilationWithAnalyzers withAnalyzers = outputCompilation.WithAnalyzers(analyzers, options);
		ImmutableArray<Diagnostic> all = await withAnalyzers.GetAllDiagnosticsAsync().ConfigureAwait(false);
		return (outputCompilation, all);
	}

	public static async Task<string> ApplyCodeFixAsync(
		string source,
		DiagnosticAnalyzer analyzer,
		CodeFixProvider codeFix,
		string fixableDiagnosticId)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		Assembly netstandard = assemblies.Single(a => a.GetName().Name == "netstandard");
		Assembly systemRuntime = assemblies.Single(a => a.GetName().Name == "System.Runtime");

		SyntaxTree userTree = CSharpSyntaxTree.ParseText(source, path: "UserCode.cs");
		CSharpCompilation initial = CSharpCompilation.Create(
			assemblyName: "UnionStruct.Tests.Analyzers",
			syntaxTrees: [userTree],
			references:
			[
				MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(UnionAttribute).Assembly.Location),
				MetadataReference.CreateFromFile(netstandard.Location),
				MetadataReference.CreateFromFile(systemRuntime.Location),
			],
			options: _compilationOptions);

		UnionStructIncrementalGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		_ = driver.RunGeneratorsAndUpdateCompilation(initial, out Compilation generated, out _);

		using AdhocWorkspace workspace = new();
		ProjectId projectId = ProjectId.CreateNewId();
		ProjectInfo projectInfo = ProjectInfo.Create(
			projectId,
			VersionStamp.Default,
			"TestProject",
			"UnionStruct.Tests.Analyzers",
			LanguageNames.CSharp,
			compilationOptions: _compilationOptions,
			metadataReferences: generated.References);

		Solution solution = workspace.CurrentSolution.AddProject(projectInfo);
		DocumentId userDocId = DocumentId.CreateNewId(projectId);
		solution = solution.AddDocument(userDocId, "UserCode.cs", await userTree.GetTextAsync().ConfigureAwait(false), filePath: "UserCode.cs");

		foreach (SyntaxTree tree in generated.SyntaxTrees)
		{
			if (tree == userTree)
				continue;

			DocumentId generatedDocId = DocumentId.CreateNewId(projectId);
			solution = solution.AddDocument(generatedDocId, Path.GetFileName(tree.FilePath) is { Length: > 0 } name ? name : Path.GetRandomFileName() + ".g.cs", await tree.GetTextAsync().ConfigureAwait(false));
		}

		Project augmentedProject = solution.GetProject(projectId) ?? throw new InvalidOperationException("Augmented project missing.");
		Compilation? augmentedCompilation = await augmentedProject.GetCompilationAsync().ConfigureAwait(false);
		if (augmentedCompilation is null)
			throw new InvalidOperationException("Augmented compilation missing.");

		CompilationWithAnalyzers withAnalyzers = augmentedCompilation.WithAnalyzers(ImmutableArray.Create(analyzer));
		ImmutableArray<Diagnostic> diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);

		Document targetDocument = augmentedProject.GetDocument(userDocId) ?? throw new InvalidOperationException("Target document missing.");

		Diagnostic? target = diagnostics.FirstOrDefault(d => d.Id == fixableDiagnosticId && d.Location.SourceTree?.FilePath == "UserCode.cs");
		if (target is null)
			throw new InvalidOperationException($"No '{fixableDiagnosticId}' diagnostic raised on user source. Got: {string.Join(", ", diagnostics.Select(d => d.Id))}");

		CodeAction? action = null;
		CodeFixContext context = new(targetDocument, target, (ca, _) => action = ca, CancellationToken.None);
		await codeFix.RegisterCodeFixesAsync(context).ConfigureAwait(false);

		if (action is null)
			throw new InvalidOperationException("Code fix did not register any action.");

		ImmutableArray<CodeActionOperation> operations = await action.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
		ApplyChangesOperation applyOp = operations.OfType<ApplyChangesOperation>().Single();

		Document? fixedDocument = applyOp.ChangedSolution.GetDocument(userDocId);
		if (fixedDocument is null)
			throw new InvalidOperationException("Code fix removed the document.");

		SourceText fixedText = await fixedDocument.GetTextAsync().ConfigureAwait(false);
		return fixedText.ToString();
	}
}
