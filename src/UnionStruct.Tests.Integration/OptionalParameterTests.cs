using UnionStruct.Tests.Integration.Unions;

namespace UnionStruct.Tests.Integration;

public sealed class OptionalParameterTests
{
	[Test]
	public async Task OptionalParameter()
	{
		UnionWithOptionalParameters union = UnionWithOptionalParameters.Int();
		await Assert.That(union.CaseIndex).IsEqualTo(UnionWithOptionalParameters.IntIndex);
		await Assert.That(union.IntData).IsEqualTo(16);

		union = UnionWithOptionalParameters.Int(32);
		await Assert.That(union.CaseIndex).IsEqualTo(UnionWithOptionalParameters.IntIndex);
		await Assert.That(union.IntData).IsEqualTo(32);

		union = UnionWithOptionalParameters.Text();
		await Assert.That(union.CaseIndex).IsEqualTo(UnionWithOptionalParameters.TextIndex);
		await Assert.That(union.TextData).IsEqualTo("default");

		union = UnionWithOptionalParameters.Text("new");
		await Assert.That(union.CaseIndex).IsEqualTo(UnionWithOptionalParameters.TextIndex);
		await Assert.That(union.TextData).IsEqualTo("new");
	}
}
