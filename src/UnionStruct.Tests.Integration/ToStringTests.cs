using System.Numerics;
using UnionStruct.Tests.Integration.Unions;

namespace UnionStruct.Tests.Integration;

public sealed class ToStringTests
{
	[Test]
	public async Task ToStringReturnsCorrectResult()
	{
		await Assert.That(EnumLikeUnion.Bronze().ToString()).IsEqualTo("Bronze");
		await Assert.That(EnumLikeUnion.Silver().ToString()).IsEqualTo("Silver");
		await Assert.That(EnumLikeUnion.Gold().ToString()).IsEqualTo("Gold");

		await Assert.That(RotationType.None().ToString()).IsEqualTo("None");
		await Assert.That(RotationType.RandomRotation().ToString()).IsEqualTo("RandomRotation");
		await Assert.That(RotationType.RandomRotationAroundAxis(new RandomRotationAroundAxis(Vector3.UnitX)).ToString()).IsEqualTo("RandomRotationAroundAxis { Value = RandomRotationAroundAxis { Axis = <1, 0, 0> } }");
		await Assert.That(RotationType.RotationRangeAroundAxis(new RotationRangeAroundAxis(Vector3.UnitY, 0.1f, 0.2f)).ToString()).IsEqualTo("RotationRangeAroundAxis { Value = RotationRangeAroundAxis { Axis = <0, 1, 0>, AngleMin = 0.1, AngleMax = 0.2 } }");
		await Assert.That(RotationType.CustomRotation(new CustomRotation(Quaternion.Identity)).ToString()).IsEqualTo("CustomRotation { Value = CustomRotation { Rotation = {X:0 Y:0 Z:0 W:1} } }");

		await Assert.That(CompressedIndex.Unsigned8(1).ToString()).IsEqualTo("8-bit { Value = 1 }");
		await Assert.That(CompressedIndex.Unsigned16(2).ToString()).IsEqualTo("16-bit { Value = 2 }");
		await Assert.That(CompressedIndex.Unsigned32(3).ToString()).IsEqualTo("32-bit { Value = 3 }");

		await Assert.That(Shape<float>.Circle(1.5f).ToString()).IsEqualTo("Circle { Radius = 1.5 }");
		await Assert.That(Shape<float>.Rectangle(2.5f, 3.5f).ToString()).IsEqualTo("Rectangle { Width = 2.5, Height = 3.5 }");

		await Assert.That(Shape<int>.Circle(1).ToString()).IsEqualTo("Circle { Radius = 1 }");
		await Assert.That(Shape<int>.Rectangle(2, 3).ToString()).IsEqualTo("Rectangle { Width = 2, Height = 3 }");

		await Assert.That(UnionWithReferenceType.Int(1).ToString()).IsEqualTo("Int { Value = 1 }");
		await Assert.That(UnionWithReferenceType.String("1").ToString()).IsEqualTo("String { Value = 1 }");

		await Assert.That(RootUnion.Empty().ToString()).IsEqualTo("Empty");
		await Assert.That(RootUnion.NestedCase(NestedUnion.Empty()).ToString()).IsEqualTo("Nested { Value = Empty }");
		await Assert.That(RootUnion.NestedCase(NestedUnion.Node(1)).ToString()).IsEqualTo("Nested { Value = Node { Value = 1 } }");
	}
}
