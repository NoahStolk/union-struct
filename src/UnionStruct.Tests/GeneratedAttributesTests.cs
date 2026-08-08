using UnionStruct.Tests.Utils;

namespace UnionStruct.Tests;

/// <summary>
/// The attributes are emitted into the consuming compilation rather than shipped as a reference assembly, which makes
/// them part of the generator's output. <see cref="TestHelper.Verify"/> filters them out of the per-union snapshots to
/// avoid duplicating them across every test; this verifies them once.
/// </summary>
public sealed class GeneratedAttributesTests
{
	[Fact]
	public async Task GeneratedAttributes()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct TestUnion
			{
				[UnionCase] public static partial TestUnion Empty();
			}
			""";

		await TestHelper.VerifyIncludingAttributes(code);
	}
}
