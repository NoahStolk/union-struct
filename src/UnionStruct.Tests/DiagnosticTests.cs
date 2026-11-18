using Microsoft.CodeAnalysis.CSharp;
using UnionStruct.Tests.Utils;

namespace UnionStruct.Tests;

public sealed class DiagnosticTests
{
	[Test]
	public async Task UnionMustBePartial()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			namespace Tests;
			[Union]
			internal struct TestUnion
			{
				public partial TestUnion(int value);
			}
			""";

		await TestHelper.VerifyDiagnostics(code);
	}

	[Test]
	public async Task ContainingTypeMustBePartial()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			namespace Tests;
			internal static class Outer
			{
				[Union]
				internal partial struct TestUnion
				{
					public partial TestUnion(int value);
				}
			}
			""";

		await TestHelper.VerifyDiagnostics(code);
	}

	[Test]
	public async Task InvalidCaseConstructor()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			namespace Tests;
			[Union]
			internal partial struct TestUnion
			{
				public partial TestUnion(int value);
				public partial TestUnion(float x, float y);
				public partial TestUnion(ref long value);
			}
			""";

		await TestHelper.VerifyDiagnostics(code);
	}

	[Test]
	public async Task DuplicateCaseType()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			namespace Tests;
			[Union]
			internal partial struct TestUnion
			{
				public partial TestUnion(string value);
				public partial TestUnion(string? other);
			}
			""";

		await TestHelper.VerifyDiagnostics(code);
	}

	[Test]
	public async Task DuplicateCaseName()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			namespace Tests;
			public readonly record struct Circle<T>(T Radius);
			[Union]
			internal partial struct TestUnion
			{
				public partial TestUnion(Circle<int> value);
				public partial TestUnion(Circle<float> value);
			}
			""";

		await TestHelper.VerifyDiagnostics(code);
	}

	[Test]
	public async Task ReservedCaseName()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			namespace Tests;
			public readonly record struct Case;
			[Union]
			internal partial struct TestUnion
			{
				public partial TestUnion(Case value);
			}
			""";

		await TestHelper.VerifyDiagnostics(code);
	}

	[Test]
	public async Task UnsupportedCaseType()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			namespace Tests;
			[Union]
			internal partial struct TestUnion
			{
				public partial TestUnion((int, int) value);
			}
			""";

		await TestHelper.VerifyDiagnostics(code);
	}

	[Test]
	public async Task UnsupportedLanguageVersion()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			namespace Tests;
			[Union]
			internal partial struct TestUnion
			{
				public partial TestUnion(int value);
			}
			""";

		await TestHelper.VerifyDiagnostics(code, LanguageVersion.CSharp14);
	}
}
