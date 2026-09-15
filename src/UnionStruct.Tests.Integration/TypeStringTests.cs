using System.Numerics;
using TUnit.Assertions.Enums;
using UnionStruct.Tests.Integration.Unions;

namespace UnionStruct.Tests.Integration;

public sealed class TypeStringTests
{
	[Test]
	public async Task GetTypeStringReturnsCorrectResult()
	{
		await Assert.That(EnumLikeUnion.Bronze().GetTypeString()).IsEqualTo("Bronze");
		await Assert.That(EnumLikeUnion.Silver().GetTypeString()).IsEqualTo("Silver");
		await Assert.That(EnumLikeUnion.Gold().GetTypeString()).IsEqualTo("Gold");

		await Assert.That(RotationType.None().GetTypeString()).IsEqualTo("None");
		await Assert.That(RotationType.RandomRotation().GetTypeString()).IsEqualTo("RandomRotation");
		await Assert.That(RotationType.RandomRotationAroundAxis(new RandomRotationAroundAxis(Vector3.UnitX)).GetTypeString()).IsEqualTo("RandomRotationAroundAxis");
		await Assert.That(RotationType.RotationRangeAroundAxis(new RotationRangeAroundAxis(Vector3.UnitY, 0.1f, 0.2f)).GetTypeString()).IsEqualTo("RotationRangeAroundAxis");
		await Assert.That(RotationType.CustomRotation(new CustomRotation(Quaternion.Identity)).GetTypeString()).IsEqualTo("CustomRotation");

		await Assert.That(CompressedIndex.Unsigned8(1).GetTypeString()).IsEqualTo("8-bit");
		await Assert.That(CompressedIndex.Unsigned16(2).GetTypeString()).IsEqualTo("16-bit");
		await Assert.That(CompressedIndex.Unsigned32(3).GetTypeString()).IsEqualTo("32-bit");

		await Assert.That(Shape<float>.Circle(1.5f).GetTypeString()).IsEqualTo("Circle");
		await Assert.That(Shape<float>.Rectangle(2.5f, 3.5f).GetTypeString()).IsEqualTo("Rectangle");

		await Assert.That(Shape<int>.Circle(1).GetTypeString()).IsEqualTo("Circle");
		await Assert.That(Shape<int>.Rectangle(2, 3).GetTypeString()).IsEqualTo("Rectangle");

		await Assert.That(UnionWithReferenceType.Int(1).GetTypeString()).IsEqualTo("Int");
		await Assert.That(UnionWithReferenceType.String("1").GetTypeString()).IsEqualTo("String");

		await Assert.That(RootUnion.Empty().GetTypeString()).IsEqualTo("Empty");
		await Assert.That(RootUnion.NestedCase(NestedUnion.Empty()).GetTypeString()).IsEqualTo("Nested");
		await Assert.That(RootUnion.NestedCase(NestedUnion.Node(1)).GetTypeString()).IsEqualTo("Nested");
	}

	[Test]
	public async Task GetTypeAsUtf8SpanReturnsCorrectResult()
	{
		await Assert.That(EnumLikeUnion.Bronze().GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("Bronze"u8.ToArray(), CollectionOrdering.Matching);
		await Assert.That(EnumLikeUnion.Silver().GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("Silver"u8.ToArray(), CollectionOrdering.Matching);
		await Assert.That(EnumLikeUnion.Gold().GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("Gold"u8.ToArray(), CollectionOrdering.Matching);

		await Assert.That(RotationType.None().GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("None"u8.ToArray(), CollectionOrdering.Matching);
		await Assert.That(RotationType.RandomRotation().GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("RandomRotation"u8.ToArray(), CollectionOrdering.Matching);
		await Assert.That(RotationType.RandomRotationAroundAxis(new RandomRotationAroundAxis(Vector3.UnitX)).GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("RandomRotationAroundAxis"u8.ToArray(), CollectionOrdering.Matching);
		await Assert.That(RotationType.RotationRangeAroundAxis(new RotationRangeAroundAxis(Vector3.UnitY, 0.1f, 0.2f)).GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("RotationRangeAroundAxis"u8.ToArray(), CollectionOrdering.Matching);
		await Assert.That(RotationType.CustomRotation(new CustomRotation(Quaternion.Identity)).GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("CustomRotation"u8.ToArray(), CollectionOrdering.Matching);

		await Assert.That(CompressedIndex.Unsigned8(1).GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("8-bit"u8.ToArray(), CollectionOrdering.Matching);
		await Assert.That(CompressedIndex.Unsigned16(2).GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("16-bit"u8.ToArray(), CollectionOrdering.Matching);
		await Assert.That(CompressedIndex.Unsigned32(3).GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("32-bit"u8.ToArray(), CollectionOrdering.Matching);

		await Assert.That(Shape<float>.Circle(1.5f).GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("Circle"u8.ToArray(), CollectionOrdering.Matching);
		await Assert.That(Shape<float>.Rectangle(2.5f, 3.5f).GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("Rectangle"u8.ToArray(), CollectionOrdering.Matching);

		await Assert.That(Shape<int>.Circle(1).GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("Circle"u8.ToArray(), CollectionOrdering.Matching);
		await Assert.That(Shape<int>.Rectangle(2, 3).GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("Rectangle"u8.ToArray(), CollectionOrdering.Matching);

		await Assert.That(UnionWithReferenceType.Int(1).GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("Int"u8.ToArray(), CollectionOrdering.Matching);
		await Assert.That(UnionWithReferenceType.String("1").GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("String"u8.ToArray(), CollectionOrdering.Matching);

		await Assert.That(RootUnion.Empty().GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("Empty"u8.ToArray(), CollectionOrdering.Matching);
		await Assert.That(RootUnion.NestedCase(NestedUnion.Empty()).GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("Nested"u8.ToArray(), CollectionOrdering.Matching);
		await Assert.That(RootUnion.NestedCase(NestedUnion.Node(1)).GetTypeAsUtf8Span().ToArray()).IsEquivalentTo("Nested"u8.ToArray(), CollectionOrdering.Matching);
	}
}
