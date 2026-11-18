using UnionStruct.Tests.Utils;

namespace UnionStruct.Tests;

/// <summary>
/// The attributes are emitted into the consuming compilation rather than shipped as a reference assembly, which makes
/// them part of the generator's output. <see cref="TestHelper.Verify"/> filters them out of the per-union snapshots to
/// avoid duplicating them across every test; this verifies them once.
/// </summary>
public sealed class GeneratedAttributesTests
{
	[Test]
	public async Task GeneratedAttributes()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			using UnionStruct;
			namespace Tests;
			public readonly record struct Circle(float Radius);
			[Union]
			internal partial struct TestUnion
			{
				[UnionCase(DisplayName = "Round")] public partial TestUnion(Circle value);
			}
			""";

		await TestHelper.VerifyIncludingAttributes(code);
	}
}
