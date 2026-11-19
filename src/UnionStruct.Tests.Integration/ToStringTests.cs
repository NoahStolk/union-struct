using System.Numerics;
using UnionStruct.Tests.Integration.Unions;

namespace UnionStruct.Tests.Integration;

public sealed class ToStringTests
{
	[Test]
	public async Task ToStringReturnsCorrectResult()
	{
		await Assert.That(new EnumLikeUnion(default(EnumLikeUnion.Bronze)).ToString()).IsEqualTo("Bronze");
		await Assert.That(new EnumLikeUnion(default(EnumLikeUnion.Gold)).ToString()).IsEqualTo("Gold");

		await Assert.That(new RotationType(default(RotationType.None)).ToString()).IsEqualTo("None");
		await Assert.That(new RotationType(new RandomRotationAroundAxis(Vector3.UnitX)).ToString()).IsEqualTo("RandomRotationAroundAxis { Value = RandomRotationAroundAxis { Axis = <1, 0, 0> } }");
		await Assert.That(new RotationType(new RotationRangeAroundAxis(Vector3.UnitY, 0.1f, 0.2f)).ToString())
			.IsEqualTo("RotationRangeAroundAxis { Value = RotationRangeAroundAxis { Axis = <0, 1, 0>, AngleMin = 0.1, AngleMax = 0.2 } }");
		await Assert.That(new RotationType(new CustomRotation(Quaternion.Identity)).ToString()).IsEqualTo("CustomRotation { Value = CustomRotation { Rotation = {X:0 Y:0 Z:0 W:1} } }");

		await Assert.That(new CompressedIndex((byte)1).ToString()).IsEqualTo("8-bit { Value = 1 }");
		await Assert.That(new CompressedIndex((ushort)2).ToString()).IsEqualTo("16-bit { Value = 2 }");
		await Assert.That(new CompressedIndex(3u).ToString()).IsEqualTo("32-bit { Value = 3 }");

		await Assert.That(new Shape<float>(new Circle<float>(1.5f)).ToString()).IsEqualTo("Circle { Value = Circle { Radius = 1.5 } }");
		await Assert.That(new Shape<int>(new Rectangle<int>(2, 3)).ToString()).IsEqualTo("Rectangle { Value = Rectangle { Width = 2, Height = 3 } }");

		await Assert.That(new UnionWithReferenceType(1).ToString()).IsEqualTo("Int32 { Value = 1 }");
		await Assert.That(new UnionWithReferenceType("1").ToString()).IsEqualTo("String { Value = 1 }");

		await Assert.That(new RootUnion(default(Empty)).ToString()).IsEqualTo("Empty");
		await Assert.That(new RootUnion(new NestedUnion(new Node(1))).ToString()).IsEqualTo("Nested { Value = Node { Value = Node { Value = 1 } } }");
		await Assert.That(new RootUnion(new NestedUnion(default(Empty))).ToString()).IsEqualTo("Nested { Value = Empty }");
	}

	[Test]
	public async Task FieldlessCasePrintsOnlyItsName()
	{
		// A plain empty struct's own ToString would print its full type name.
		await Assert.That(new Signal(default(Signal.Idle)).ToString()).IsEqualTo("Idle");
		await Assert.That(default(Signal.Idle).ToString()).IsEqualTo("UnionStruct.Tests.Integration.Unions.Signal+Idle");
	}

	[Test]
	public async Task StructWithOnlyPrivateFieldsFromAReferenceAssemblyPrintsItsPayload()
	{
		await Assert.That(new Signal(CancellationToken.None).ToString()).IsEqualTo("CancellationToken { Value = System.Threading.CancellationToken }");
	}
}
