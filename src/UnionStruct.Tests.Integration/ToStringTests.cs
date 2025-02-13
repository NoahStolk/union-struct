using System.Numerics;
using UnionStruct.Tests.Integration.Unions;
using Xunit;

namespace UnionStruct.Tests.Integration;

public sealed class ToStringTests
{
	[Fact]
	public void ToStringReturnsCorrectResult()
	{
		Assert.Equal("Bronze", EnumLikeUnion.Bronze().ToString());
		Assert.Equal("Silver", EnumLikeUnion.Silver().ToString());
		Assert.Equal("Gold", EnumLikeUnion.Gold().ToString());

		Assert.Equal("None", RotationType.None().ToString());
		Assert.Equal("RandomRotation", RotationType.RandomRotation().ToString());
		Assert.Equal("RandomRotationAroundAxis { Value = RandomRotationAroundAxis { Axis = <1, 0, 0> } }", RotationType.RandomRotationAroundAxis(new RandomRotationAroundAxis(Vector3.UnitX)).ToString());
		Assert.Equal("RotationRangeAroundAxis { Value = RotationRangeAroundAxis { Axis = <0, 1, 0>, AngleMin = 0.1, AngleMax = 0.2 } }", RotationType.RotationRangeAroundAxis(new RotationRangeAroundAxis(Vector3.UnitY, 0.1f, 0.2f)).ToString());
		Assert.Equal("CustomRotation { Value = CustomRotation { Rotation = {X:0 Y:0 Z:0 W:1} } }", RotationType.CustomRotation(new CustomRotation(Quaternion.Identity)).ToString());

		Assert.Equal("Unsigned8 { Value = 1 }", CompressedIndex.Unsigned8(1).ToString());
		Assert.Equal("Unsigned16 { Value = 2 }", CompressedIndex.Unsigned16(2).ToString());
		Assert.Equal("Unsigned32 { Value = 3 }", CompressedIndex.Unsigned32(3).ToString());

		Assert.Equal("Circle { Radius = 1.5 }", Shape<float>.Circle(1.5f).ToString());
		Assert.Equal("Rectangle { Width = 2.5, Height = 3.5 }", Shape<float>.Rectangle(2.5f, 3.5f).ToString());

		Assert.Equal("Circle { Radius = 1 }", Shape<int>.Circle(1).ToString());
		Assert.Equal("Rectangle { Width = 2, Height = 3 }", Shape<int>.Rectangle(2, 3).ToString());

		Assert.Equal("Int { Value = 1 }", UnionWithReferenceType.Int(1).ToString());
		Assert.Equal("String { Value = 1 }", UnionWithReferenceType.String("1").ToString());

		Assert.Equal("Empty", RootUnion.Empty().ToString());
		Assert.Equal("NestedCase { Value = Empty }", RootUnion.NestedCase(NestedUnion.Empty()).ToString());
		Assert.Equal("NestedCase { Value = Node { Value = 1 } }", RootUnion.NestedCase(NestedUnion.Node(1)).ToString());
	}
}
