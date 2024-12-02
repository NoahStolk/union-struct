using FluentAssertions;
using System.Numerics;
using UnionStruct.Tests.Integration.Unions;
using Xunit;

namespace UnionStruct.Tests.Integration;

public sealed class ToStringTests
{
	[Fact]
	public void ToStringReturnsCorrectResult()
	{
		EnumLikeUnion.Bronze().ToString().Should().Be("Bronze");
		EnumLikeUnion.Silver().ToString().Should().Be("Silver");
		EnumLikeUnion.Gold().ToString().Should().Be("Gold");

		RotationType.None().ToString().Should().Be("None");
		RotationType.RandomRotation().ToString().Should().Be("RandomRotation");
		RotationType.RandomRotationAroundAxis(new RandomRotationAroundAxis(Vector3.UnitX)).ToString().Should().Be("RandomRotationAroundAxis { Axis = <1, 0, 0> }");
		RotationType.RotationRangeAroundAxis(new RotationRangeAroundAxis(Vector3.UnitY, 0.1f, 0.2f)).ToString().Should().Be("RotationRangeAroundAxis { Axis = <0, 1, 0>, AngleMin = 0.1, AngleMax = 0.2 }");
		RotationType.CustomRotation(new CustomRotation(Quaternion.Identity)).ToString().Should().Be("CustomRotation { Rotation = {X:0 Y:0 Z:0 W:1} }");

		CompressedIndex.Unsigned8(1).ToString().Should().Be("1");
		CompressedIndex.Unsigned16(2).ToString().Should().Be("2");
		CompressedIndex.Unsigned32(3).ToString().Should().Be("3");
	}
}
