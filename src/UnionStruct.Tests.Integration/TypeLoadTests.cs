using UnionStruct.Tests.Integration.Unions;

namespace UnionStruct.Tests.Integration;

public sealed class TypeLoadTests
{
	[Test]
	public async Task TestTypeLoad()
	{
		UnionWithStructContainingReferenceType union = UnionWithStructContainingReferenceType.Int(1);
		await Assert.That(union.IsInt).IsTrue();
		await Assert.That(union.IsString).IsFalse();
	}
}
