using UnionStruct.Tests.Integration.Unions;

namespace UnionStruct.Tests.Integration;

public sealed class EqualityTests
{
	[Test]
	public async Task EqualsReturnsCorrectResult()
	{
		await Assert.That(EnumLikeUnion.Bronze().Equals(EnumLikeUnion.Bronze())).IsTrue();
		await Assert.That(EnumLikeUnion.Bronze().Equals(EnumLikeUnion.Silver())).IsFalse();
		await Assert.That(EnumLikeUnion.Bronze().Equals(null)).IsFalse();

		await Assert.That(RotationType.None().Equals(RotationType.None())).IsTrue();
		await Assert.That(RotationType.None().Equals(RotationType.RandomRotation())).IsFalse();
		await Assert.That(RotationType.None().Equals(null)).IsFalse();

		await Assert.That(CompressedIndex.Unsigned8(1).Equals(CompressedIndex.Unsigned8(1))).IsTrue();
		await Assert.That(CompressedIndex.Unsigned8(1).Equals(CompressedIndex.Unsigned8(2))).IsFalse();
		await Assert.That(CompressedIndex.Unsigned8(1).Equals(null)).IsFalse();

		await Assert.That(Shape<float>.Circle(1.5f).Equals(Shape<float>.Circle(1.5f))).IsTrue();
		await Assert.That(Shape<float>.Circle(1.5f).Equals(Shape<float>.Circle(2.5f))).IsFalse();
		await Assert.That(Shape<float>.Circle(1.5f).Equals(null)).IsFalse();
		await Assert.That(Shape<float>.Circle(1).Equals(Shape<float>.Rectangle(1, 1))).IsFalse();

		// ReSharper disable once SuspiciousTypeConversion.Global
		await Assert.That(Shape<float>.Circle(1).Equals(Shape<int>.Circle(1))).IsFalse();

		await Assert.That(Shape<int>.Circle(1).Equals(Shape<int>.Circle(1))).IsTrue();
		await Assert.That(Shape<int>.Circle(1).Equals(Shape<int>.Circle(2))).IsFalse();
		await Assert.That(Shape<int>.Circle(1).Equals(null)).IsFalse();

		await Assert.That(UnionWithReferenceType.Int(1).Equals(UnionWithReferenceType.Int(1))).IsTrue();
		await Assert.That(UnionWithReferenceType.Int(1).Equals(UnionWithReferenceType.Int(2))).IsFalse();
		await Assert.That(UnionWithReferenceType.Int(1).Equals(null)).IsFalse();
		await Assert.That(UnionWithReferenceType.Int(1).Equals(UnionWithReferenceType.String("1"))).IsFalse();
		await Assert.That(UnionWithReferenceType.String("1").Equals(UnionWithReferenceType.String("1"))).IsTrue();
		await Assert.That(UnionWithReferenceType.String("1").Equals(UnionWithReferenceType.String("2"))).IsFalse();
		await Assert.That(UnionWithReferenceType.String("1").Equals(null)).IsFalse();
		await Assert.That(UnionWithReferenceType.String("1").Equals(UnionWithReferenceType.Int(1))).IsFalse();

		await Assert.That(RootUnion.Empty().Equals(RootUnion.Empty())).IsTrue();
		await Assert.That(RootUnion.Empty().Equals(RootUnion.NestedCase(NestedUnion.Empty()))).IsFalse();
		await Assert.That(RootUnion.Empty().Equals(null)).IsFalse();

		await Assert.That(RootUnion.NestedCase(NestedUnion.Empty()).Equals(RootUnion.NestedCase(NestedUnion.Empty()))).IsTrue();
		await Assert.That(RootUnion.NestedCase(NestedUnion.Empty()).Equals(RootUnion.NestedCase(NestedUnion.Node(1)))).IsFalse();
		await Assert.That(RootUnion.NestedCase(NestedUnion.Empty()).Equals(null)).IsFalse();

		await Assert.That(RootUnion.NestedCase(NestedUnion.Node(1)).Equals(RootUnion.NestedCase(NestedUnion.Node(1)))).IsTrue();
		await Assert.That(RootUnion.NestedCase(NestedUnion.Node(1)).Equals(RootUnion.NestedCase(NestedUnion.Node(2)))).IsFalse();
		await Assert.That(RootUnion.NestedCase(NestedUnion.Node(1)).Equals(RootUnion.NestedCase(NestedUnion.Empty()))).IsFalse();
		await Assert.That(RootUnion.NestedCase(NestedUnion.Node(1)).Equals(null)).IsFalse();
	}

	[Test]
	public async Task EqualsShouldNotUseDataFromInactiveCase()
	{
		// Create a circle and incorrectly set the rectangle data.
		// The memory for the rectangle case is separated because generic types cannot have an explicit layout.
		Shape<int> shape = Shape<int>.Circle(1);
		shape.RectangleData = new Shape<int>.RectangleCase { Width = 1, Height = 1 };

		await Assert.That(shape.IsCircle).IsTrue();
		await Assert.That(shape.IsRectangle).IsFalse();
		await Assert.That(shape.CircleData).IsEqualTo(1);
		await Assert.That(shape.CaseIndex).IsEqualTo(Shape<int>.CircleIndex);
		await Assert.That(shape.Equals(Shape<int>.Circle(1))).IsTrue();
	}
}
