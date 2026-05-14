using UnionStruct.CodeFixes;
using UnionStruct.Internals.Analyzers;
using Xunit;

namespace UnionStruct.Tests.Analyzers;

public sealed class AddMissingUnionCasesCodeFixTests
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

	[Fact]
	public async Task AddsMissingArmsToSwitchExpressionUsingTagStyle()
	{
		string code = UnionDecl +
			"""
			internal static class Consumer
			{
				public static int Test(U u) => u.Tag switch
				{
					U.CaseTag.A => 1,
				};
			}
			""";

		string fixedCode = await AnalyzerTestHelper.ApplyCodeFixAsync(
			code,
			new ExhaustiveSwitchAnalyzer(),
			new AddMissingUnionCasesCodeFix(),
			ExhaustiveSwitchAnalyzer.DiagnosticId);

		Assert.Contains("U.CaseTag.B => throw new global::System.NotImplementedException()", fixedCode, StringComparison.Ordinal);
		Assert.Contains("U.CaseTag.C => throw new global::System.NotImplementedException()", fixedCode, StringComparison.Ordinal);
	}

	[Fact]
	public async Task AddsMissingArmsToSwitchStatementUsingIndexStyle()
	{
		string code = UnionDecl +
			"""
			internal static class Consumer
			{
				public static int Test(U u)
				{
					switch (u.CaseIndex)
					{
						case U.AIndex: return 1;
					}
					return 0;
				}
			}
			""";

		string fixedCode = await AnalyzerTestHelper.ApplyCodeFixAsync(
			code,
			new ExhaustiveSwitchAnalyzer(),
			new AddMissingUnionCasesCodeFix(),
			ExhaustiveSwitchAnalyzer.DiagnosticId);

		Assert.Contains("case U.BIndex:", fixedCode, StringComparison.Ordinal);
		Assert.Contains("case U.CIndex:", fixedCode, StringComparison.Ordinal);
	}
}
