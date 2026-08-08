using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using UnionStruct.Internals.Analyzers;
using Xunit;

namespace UnionStruct.Tests.Analyzers;

public sealed class ExhaustiveSwitchAnalyzerTests
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
			[UnionCase] public static partial U C();
		}
		""";

	private static readonly ImmutableArray<DiagnosticAnalyzer> _analyzer = ImmutableArray.Create<DiagnosticAnalyzer>(new ExhaustiveSwitchAnalyzer());

	[Fact]
	public async Task FiresOnMissingCaseIndexSwitchStatement()
	{
		string code = UnionDecl + """

			internal static class Consumer
			{
				public static int Test(U u)
				{
					switch (u.CaseIndex)
					{
						case U.AIndex: return 1;
						case U.BIndex: return 2;
					}
					return 0;
				}
			}
			""";

		(_, ImmutableArray<Diagnostic> diagnostics) = await AnalyzerTestHelper.CompileWithAnalyzersAsync(code, _analyzer);
		Diagnostic d = Assert.Single(diagnostics, x => x.Id == ExhaustiveSwitchAnalyzer.DiagnosticId);
		Assert.Contains("C", d.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
	}

	[Fact]
	public async Task FiresOnMissingCaseIndexSwitchExpression()
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

		(_, ImmutableArray<Diagnostic> diagnostics) = await AnalyzerTestHelper.CompileWithAnalyzersAsync(code, _analyzer);
		Assert.Single(diagnostics, x => x.Id == ExhaustiveSwitchAnalyzer.DiagnosticId);
	}

	[Fact]
	public async Task FiresOnMissingTagSwitchStatement()
	{
		string code = UnionDecl + """

			internal static class Consumer
			{
				public static int Test(U u)
				{
					switch (u.Tag)
					{
						case U.CaseTag.A: return 1;
						case U.CaseTag.B: return 2;
					}
					return 0;
				}
			}
			""";

		(_, ImmutableArray<Diagnostic> diagnostics) = await AnalyzerTestHelper.CompileWithAnalyzersAsync(code, _analyzer);
		Assert.Single(diagnostics, x => x.Id == ExhaustiveSwitchAnalyzer.DiagnosticId);
	}

	[Fact]
	public async Task FiresOnMissingTagSwitchExpression()
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

		(_, ImmutableArray<Diagnostic> diagnostics) = await AnalyzerTestHelper.CompileWithAnalyzersAsync(code, _analyzer);
		Assert.Single(diagnostics, x => x.Id == ExhaustiveSwitchAnalyzer.DiagnosticId);
	}

	[Fact]
	public async Task DoesNotFireWhenAllCasesPresent()
	{
		string code = UnionDecl + """

			internal static class Consumer
			{
				public static int Test(U u) => u.CaseIndex switch
				{
					U.AIndex => 1,
					U.BIndex => 2,
					U.CIndex => 3,
				};
			}
			""";

		(_, ImmutableArray<Diagnostic> diagnostics) = await AnalyzerTestHelper.CompileWithAnalyzersAsync(code, _analyzer);
		Assert.DoesNotContain(diagnostics, x => x.Id == ExhaustiveSwitchAnalyzer.DiagnosticId);
	}

	[Fact]
	public async Task DoesNotFireWhenDefaultArmPresent()
	{
		string code = UnionDecl + """

			internal static class Consumer
			{
				public static int Test(U u) => u.CaseIndex switch
				{
					U.AIndex => 1,
					_ => 0,
				};
			}
			""";

		(_, ImmutableArray<Diagnostic> diagnostics) = await AnalyzerTestHelper.CompileWithAnalyzersAsync(code, _analyzer);
		Assert.DoesNotContain(diagnostics, x => x.Id == ExhaustiveSwitchAnalyzer.DiagnosticId);
	}

	[Fact]
	public async Task DoesNotFireWhenSwitchedValueIsUnrelated()
	{
		string code =
			"""
			internal static class Consumer
			{
				public static int Test(int x) => x switch
				{
					0 => 1,
					_ => 0,
				};
			}
			""";

		(_, ImmutableArray<Diagnostic> diagnostics) = await AnalyzerTestHelper.CompileWithAnalyzersAsync(code, _analyzer);
		Assert.DoesNotContain(diagnostics, x => x.Id == ExhaustiveSwitchAnalyzer.DiagnosticId);
	}

	[Fact]
	public async Task DoesNotFireOnEmptyUnion()
	{
		const string code =
			"""
			using UnionStruct;
			[Union]
			internal partial struct Empty;
			internal static class Consumer
			{
				public static int Test(Empty e) => e.CaseIndex switch
				{
					0 => 1,
				};
			}
			""";

		(_, ImmutableArray<Diagnostic> diagnostics) = await AnalyzerTestHelper.CompileWithAnalyzersAsync(code, _analyzer);
		Assert.DoesNotContain(diagnostics, x => x.Id == ExhaustiveSwitchAnalyzer.DiagnosticId);
	}

	[Fact]
	public async Task TreatsOrPatternAsCoveringEachConstant()
	{
		string code = UnionDecl + """

			internal static class Consumer
			{
				public static int Test(U u) => u.Tag switch
				{
					U.CaseTag.A or U.CaseTag.B => 1,
					U.CaseTag.C => 3,
				};
			}
			""";

		(_, ImmutableArray<Diagnostic> diagnostics) = await AnalyzerTestHelper.CompileWithAnalyzersAsync(code, _analyzer);
		Assert.DoesNotContain(diagnostics, x => x.Id == ExhaustiveSwitchAnalyzer.DiagnosticId);
	}

	[Fact]
	public async Task DoesNotCreditWhenGuardedArm()
	{
		string code = UnionDecl + """

			internal static class Consumer
			{
				public static int Test(U u, bool flag) => u.Tag switch
				{
					U.CaseTag.A => 1,
					U.CaseTag.B => 2,
					U.CaseTag.C when flag => 3,
				};
			}
			""";

		(_, ImmutableArray<Diagnostic> diagnostics) = await AnalyzerTestHelper.CompileWithAnalyzersAsync(code, _analyzer);
		Assert.Single(diagnostics, x => x.Id == ExhaustiveSwitchAnalyzer.DiagnosticId);
	}

	[Fact]
	public async Task WorksForGenericUnionByOriginalDefinition()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct Shape<T>
				where T : struct
			{
				[UnionCase] public static partial Shape<T> Circle(T radius);
				[UnionCase] public static partial Shape<T> Rectangle(T width, T height);
			}

			internal static class Consumer
			{
				public static int Test(Shape<float> s) => s.Tag switch
				{
					Shape<float>.CaseTag.Circle => 1,
				};
			}
			""";

		(_, ImmutableArray<Diagnostic> diagnostics) = await AnalyzerTestHelper.CompileWithAnalyzersAsync(code, _analyzer);
		Assert.Single(diagnostics, x => x.Id == ExhaustiveSwitchAnalyzer.DiagnosticId);
	}

	[Fact]
	public async Task FiresOnUnionFromReferencedAssembly()
	{
		const string librarySource =
			"""
			using UnionStruct;
			namespace TheLib;
			[Union]
			public partial struct Shape
			{
				[UnionCase] public static partial Shape Circle(float radius);
				[UnionCase] public static partial Shape Square(float side);
			}
			""";

		const string consumerSource =
			"""
			using TheLib;
			namespace Tests;
			internal static class Consumer
			{
				public static int Test(Shape s)
				{
					switch (s.CaseIndex)
					{
						case Shape.CircleIndex: return 1;
					}
					return 0;
				}
			}
			""";

		ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHelper.CompileAcrossAssembliesWithAnalyzersAsync(librarySource, consumerSource, _analyzer);
		Assert.Single(diagnostics, x => x.Id == ExhaustiveSwitchAnalyzer.DiagnosticId);
	}
}
