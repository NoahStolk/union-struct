using UnionStruct.Tests.Integration.Unions;

namespace UnionStruct.Tests.Integration;

public sealed class TypeLoadTests
{
	/// <summary>
	/// A struct case holding a reference is managed, so the generator must fall back to sequential layout; overlapping
	/// a reference with an int fails at type load.
	/// </summary>
	[Test]
	public async Task TestTypeLoad()
	{
		UnionWithStructContainingReferenceType union = 1;
		await Assert.That(union is int).IsTrue();
		await Assert.That(union is StringStruct).IsFalse();
	}
}
