using TUnit.Assertions.Enums;
using UnionStruct.Tests.Integration.Unions;

namespace UnionStruct.Tests.Integration;

public sealed class NullTerminatedMemberNamesTests
{
	[Test]
	public async Task NullTerminatedMemberNamesReturnsCorrectResult()
	{
		await Assert.That([.. EnumLikeUnion.NullTerminatedMemberNames]).IsEquivalentTo([.. "Bronze\0Silver\0Gold\0"u8], CollectionOrdering.Matching);

		await Assert.That([.. RotationType.NullTerminatedMemberNames])
			.IsEquivalentTo([.. "None\0RandomRotation\0RandomRotationAroundAxis\0RotationRangeAroundAxis\0CustomRotation\0"u8], CollectionOrdering.Matching);

		await Assert.That([.. CompressedIndex.NullTerminatedMemberNames]).IsEquivalentTo([.. "8-bit\016-bit\032-bit\0"u8], CollectionOrdering.Matching);

		await Assert.That([.. Shape<float>.NullTerminatedMemberNames]).IsEquivalentTo([.. "Circle\0Rectangle\0"u8], CollectionOrdering.Matching);
		await Assert.That([.. Shape<int>.NullTerminatedMemberNames]).IsEquivalentTo([.. "Circle\0Rectangle\0"u8], CollectionOrdering.Matching);

		await Assert.That([.. UnionWithReferenceType.NullTerminatedMemberNames]).IsEquivalentTo([.. "Int32\0String\0"u8], CollectionOrdering.Matching);

		await Assert.That([.. RootUnion.NullTerminatedMemberNames]).IsEquivalentTo([.. "Empty\0Nested\0"u8], CollectionOrdering.Matching);
		await Assert.That([.. NestedUnion.NullTerminatedMemberNames]).IsEquivalentTo([.. "Empty\0Node\0"u8], CollectionOrdering.Matching);
	}
}
