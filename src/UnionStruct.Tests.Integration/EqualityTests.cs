using FluentAssertions;
using UnionStruct.Tests.Integration.Unions;
using Xunit;

namespace UnionStruct.Tests.Integration;

public sealed class EqualityTests
{
	[Fact]
	public void EqualsReturnsCorrectResult()
	{
		EnumLikeUnion.Bronze().Equals(EnumLikeUnion.Bronze()).Should().BeTrue();
		EnumLikeUnion.Bronze().Equals(EnumLikeUnion.Silver()).Should().BeFalse();
		EnumLikeUnion.Bronze().Equals(null).Should().BeFalse();

		RotationType.None().Equals(RotationType.None()).Should().BeTrue();
		RotationType.None().Equals(RotationType.RandomRotation()).Should().BeFalse();
		RotationType.None().Equals(null).Should().BeFalse();

		CompressedIndex.Unsigned8(1).Equals(CompressedIndex.Unsigned8(1)).Should().BeTrue();
		CompressedIndex.Unsigned8(1).Equals(CompressedIndex.Unsigned8(2)).Should().BeFalse();
		CompressedIndex.Unsigned8(1).Equals(null).Should().BeFalse();

		Shape<float>.Circle(1.5f).Equals(Shape<float>.Circle(1.5f)).Should().BeTrue();
		Shape<float>.Circle(1.5f).Equals(Shape<float>.Circle(2.5f)).Should().BeFalse();
		Shape<float>.Circle(1.5f).Equals(null).Should().BeFalse();
		Shape<float>.Circle(1).Equals(Shape<float>.Rectangle(1, 1)).Should().BeFalse();

		// ReSharper disable once SuspiciousTypeConversion.Global
		Shape<float>.Circle(1).Equals(Shape<int>.Circle(1)).Should().BeFalse();

		Shape<int>.Circle(1).Equals(Shape<int>.Circle(1)).Should().BeTrue();
		Shape<int>.Circle(1).Equals(Shape<int>.Circle(2)).Should().BeFalse();
		Shape<int>.Circle(1).Equals(null).Should().BeFalse();

		UnionWithReferenceType.Int(1).Equals(UnionWithReferenceType.Int(1)).Should().BeTrue();
		UnionWithReferenceType.Int(1).Equals(UnionWithReferenceType.Int(2)).Should().BeFalse();
		UnionWithReferenceType.Int(1).Equals(null).Should().BeFalse();
		UnionWithReferenceType.Int(1).Equals(UnionWithReferenceType.String("1")).Should().BeFalse();
		UnionWithReferenceType.String("1").Equals(UnionWithReferenceType.String("1")).Should().BeTrue();
		UnionWithReferenceType.String("1").Equals(UnionWithReferenceType.String("2")).Should().BeFalse();
		UnionWithReferenceType.String("1").Equals(null).Should().BeFalse();
		UnionWithReferenceType.String("1").Equals(UnionWithReferenceType.Int(1)).Should().BeFalse();

		RootUnion.Empty().Equals(RootUnion.Empty()).Should().BeTrue();
		RootUnion.Empty().Equals(RootUnion.NestedCase(NestedUnion.Empty())).Should().BeFalse();
		RootUnion.Empty().Equals(null).Should().BeFalse();

		RootUnion.NestedCase(NestedUnion.Empty()).Equals(RootUnion.NestedCase(NestedUnion.Empty())).Should().BeTrue();
		RootUnion.NestedCase(NestedUnion.Empty()).Equals(RootUnion.NestedCase(NestedUnion.Node(1))).Should().BeFalse();
		RootUnion.NestedCase(NestedUnion.Empty()).Equals(null).Should().BeFalse();

		RootUnion.NestedCase(NestedUnion.Node(1)).Equals(RootUnion.NestedCase(NestedUnion.Node(1))).Should().BeTrue();
		RootUnion.NestedCase(NestedUnion.Node(1)).Equals(RootUnion.NestedCase(NestedUnion.Node(2))).Should().BeFalse();
		RootUnion.NestedCase(NestedUnion.Node(1)).Equals(RootUnion.NestedCase(NestedUnion.Empty())).Should().BeFalse();
		RootUnion.NestedCase(NestedUnion.Node(1)).Equals(null).Should().BeFalse();
	}

	[Fact]
	public void EqualsShouldNotUseDataFromInactiveCase()
	{
		// Create a circle and incorrectly set the rectangle data.
		// The memory for the rectangle case is separated because generic types cannot have an explicit layout.
		Shape<int> shape = Shape<int>.Circle(1);
		shape.RectangleData = new Shape<int>.RectangleCase { Width = 1, Height = 1 };

		shape.IsCircle.Should().BeTrue();
		shape.IsRectangle.Should().BeFalse();
		shape.CircleData.Should().Be(1);
		shape.CaseIndex.Should().Be(Shape<int>.CircleIndex);
		shape.Equals(Shape<int>.Circle(1)).Should().BeTrue();
	}
}
