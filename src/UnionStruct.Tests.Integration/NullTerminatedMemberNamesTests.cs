using TUnit.Assertions.Enums;
using UnionStruct.Tests.Integration.Unions;

namespace UnionStruct.Tests.Integration;

public sealed class NullTerminatedMemberNamesTests
{
	[Test]
	public async Task NullTerminatedMemberNamesReturnsCorrectResult()
	{
		await Assert.That(EnumLikeUnion.NullTerminatedMemberNames.ToArray()).IsEquivalentTo("Bronze\0Silver\0Gold\0"u8.ToArray(), CollectionOrdering.Matching);

		await Assert.That(RotationType.NullTerminatedMemberNames.ToArray()).IsEquivalentTo("None\0RandomRotation\0RandomRotationAroundAxis\0RotationRangeAroundAxis\0CustomRotation\0"u8.ToArray(), CollectionOrdering.Matching);

		await Assert.That(CompressedIndex.NullTerminatedMemberNames.ToArray()).IsEquivalentTo("8-bit\016-bit\032-bit\0"u8.ToArray(), CollectionOrdering.Matching);

		await Assert.That(Shape<float>.NullTerminatedMemberNames.ToArray()).IsEquivalentTo("Circle\0Rectangle\0"u8.ToArray(), CollectionOrdering.Matching);
		await Assert.That(Shape<int>.NullTerminatedMemberNames.ToArray()).IsEquivalentTo("Circle\0Rectangle\0"u8.ToArray(), CollectionOrdering.Matching);

		await Assert.That(UnionWithReferenceType.NullTerminatedMemberNames.ToArray()).IsEquivalentTo("Int\0String\0"u8.ToArray(), CollectionOrdering.Matching);

		await Assert.That(RootUnion.NullTerminatedMemberNames.ToArray()).IsEquivalentTo("Empty\0Nested\0"u8.ToArray(), CollectionOrdering.Matching);
		await Assert.That(NestedUnion.NullTerminatedMemberNames.ToArray()).IsEquivalentTo("Empty\0Node\0"u8.ToArray(), CollectionOrdering.Matching);
	}
}
