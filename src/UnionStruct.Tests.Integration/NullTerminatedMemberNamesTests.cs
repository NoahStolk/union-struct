using UnionStruct.Tests.Integration.Unions;
using Xunit;

namespace UnionStruct.Tests.Integration;

public sealed class NullTerminatedMemberNamesTests
{
	[Fact]
	public void NullTerminatedMemberNamesReturnsCorrectResult()
	{
		Assert.Equal("Bronze\0Silver\0Gold\0"u8, EnumLikeUnion.NullTerminatedMemberNames);

		Assert.Equal("None\0RandomRotation\0RandomRotationAroundAxis\0RotationRangeAroundAxis\0CustomRotation\0"u8, RotationType.NullTerminatedMemberNames);

		Assert.Equal("Unsigned8\0Unsigned16\0Unsigned32\0"u8, CompressedIndex.NullTerminatedMemberNames);

		Assert.Equal("Circle\0Rectangle\0"u8, Shape<float>.NullTerminatedMemberNames);
		Assert.Equal("Circle\0Rectangle\0"u8, Shape<int>.NullTerminatedMemberNames);

		Assert.Equal("Int\0String\0"u8, UnionWithReferenceType.NullTerminatedMemberNames);

		Assert.Equal("Empty\0NestedCase\0"u8, RootUnion.NullTerminatedMemberNames);
		Assert.Equal("Empty\0Node\0"u8, NestedUnion.NullTerminatedMemberNames);
	}
}
