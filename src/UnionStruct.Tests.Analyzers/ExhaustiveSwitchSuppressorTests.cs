using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using UnionStruct.Internals.Analyzers;
using Xunit;

namespace UnionStruct.Tests.Analyzers;

public sealed class ExhaustiveSwitchSuppressorTests
{
	private const string UnionDecl =
		"""
		using UnionStruct;
		namespace Tests;
		[Union]
		internal partial struct U
		{
			[UnionCase] public static partial U A(int value);
			[UnionCase] public static partial U B(long value);
		}
		""";

	private static readonly ImmutableArray<DiagnosticAnalyzer> _suppressor = ImmutableArray.Create<DiagnosticAnalyzer>(new ExhaustiveSwitchSuppressor());

	[Fact]
	public async Task SuppressesCS8509WhenAllCasesCovered()
	{
		string code = UnionDecl + """

			internal static class Consumer
			{
				public static int Test(U u) => u.CaseIndex switch
				{
					U.AIndex => 1,
					U.BIndex => 2,
				};
			}
			""";

		(_, ImmutableArray<Diagnostic> diagnostics) = await AnalyzerTestHelper.CompileWithAnalyzersAsync(code, _suppressor);
		Diagnostic cs8509 = Assert.Single(diagnostics, x => x.Id == "CS8509");
		Assert.True(cs8509.IsSuppressed, "CS8509 should be suppressed by USS0001.");
	}

	[Fact]
	public async Task SuppressesCS8524ForTagEnumWhenAllCasesCovered()
	{
		string code = UnionDecl + """

			internal static class Consumer
			{
				public static int Test(U u) => u.Tag switch
				{
					U.CaseTag.A => 1,
					U.CaseTag.B => 2,
				};
			}
			""";

		(_, ImmutableArray<Diagnostic> diagnostics) = await AnalyzerTestHelper.CompileWithAnalyzersAsync(code, _suppressor);
		Diagnostic? compilerDiag = diagnostics.FirstOrDefault(x => x.Id is "CS8509" or "CS8524");
		Assert.NotNull(compilerDiag);
		Assert.True(compilerDiag.IsSuppressed, $"{compilerDiag.Id} should be suppressed.");
	}

	[Fact]
	public async Task DoesNotSuppressWhenDiscardArmPresent()
	{
		string code = UnionDecl + """

			internal static class Consumer
			{
				public static int Test(U u) => u.CaseIndex switch
				{
					U.AIndex => 1,
					U.BIndex => 2,
					_ => 0,
				};
			}
			""";

		(_, ImmutableArray<Diagnostic> diagnostics) = await AnalyzerTestHelper.CompileWithAnalyzersAsync(code, _suppressor);
		Assert.DoesNotContain(diagnostics, x => x.Id == "CS8509");
	}

	[Fact]
	public async Task DoesNotSuppressWhenCaseMissing()
	{
		string code = UnionDecl + """

			internal static class Consumer
			{
				public static int Test(U u) => u.CaseIndex switch
				{
					U.AIndex => 1,
				};
			}
			""";

		(_, ImmutableArray<Diagnostic> diagnostics) = await AnalyzerTestHelper.CompileWithAnalyzersAsync(code, _suppressor);
		Diagnostic cs8509 = Assert.Single(diagnostics, x => x.Id == "CS8509");
		Assert.False(cs8509.IsSuppressed, "CS8509 must remain visible when cases are missing.");
	}

	[Fact]
	public async Task DoesNotSuppressWhenGuardedArmIsTheOnlyCoverage()
	{
		string code = UnionDecl + """

			internal static class Consumer
			{
				public static int Test(U u, bool flag) => u.CaseIndex switch
				{
					U.AIndex => 1,
					U.BIndex when flag => 2,
				};
			}
			""";

		(_, ImmutableArray<Diagnostic> diagnostics) = await AnalyzerTestHelper.CompileWithAnalyzersAsync(code, _suppressor);
		Diagnostic cs8509 = Assert.Single(diagnostics, x => x.Id == "CS8509");
		Assert.False(cs8509.IsSuppressed);
	}
}
