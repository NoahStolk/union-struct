using UnionStruct.Tests.Integration.Unions;
using Xunit;

namespace UnionStruct.Tests.Integration;

public sealed class EqualityTests
{
	[Fact]
	public void EqualsReturnsCorrectResult()
	{
		Assert.True(EnumLikeUnion.Bronze().Equals(EnumLikeUnion.Bronze()));
		Assert.False(EnumLikeUnion.Bronze().Equals(EnumLikeUnion.Silver()));
		Assert.False(EnumLikeUnion.Bronze().Equals(null));

		Assert.True(RotationType.None().Equals(RotationType.None()));
		Assert.False(RotationType.None().Equals(RotationType.RandomRotation()));
		Assert.False(RotationType.None().Equals(null));

		Assert.True(CompressedIndex.Unsigned8(1).Equals(CompressedIndex.Unsigned8(1)));
		Assert.False(CompressedIndex.Unsigned8(1).Equals(CompressedIndex.Unsigned8(2)));
		Assert.False(CompressedIndex.Unsigned8(1).Equals(null));

		Assert.True(Shape<float>.Circle(1.5f).Equals(Shape<float>.Circle(1.5f)));
		Assert.False(Shape<float>.Circle(1.5f).Equals(Shape<float>.Circle(2.5f)));
		Assert.False(Shape<float>.Circle(1.5f).Equals(null));
		Assert.False(Shape<float>.Circle(1).Equals(Shape<float>.Rectangle(1, 1)));

		// ReSharper disable once SuspiciousTypeConversion.Global
		Assert.False(Shape<float>.Circle(1).Equals(Shape<int>.Circle(1)));

		Assert.True(Shape<int>.Circle(1).Equals(Shape<int>.Circle(1)));
		Assert.False(Shape<int>.Circle(1).Equals(Shape<int>.Circle(2)));
		Assert.False(Shape<int>.Circle(1).Equals(null));

		Assert.True(UnionWithReferenceType.Int(1).Equals(UnionWithReferenceType.Int(1)));
		Assert.False(UnionWithReferenceType.Int(1).Equals(UnionWithReferenceType.Int(2)));
		Assert.False(UnionWithReferenceType.Int(1).Equals(null));
		Assert.False(UnionWithReferenceType.Int(1).Equals(UnionWithReferenceType.String("1")));
		Assert.True(UnionWithReferenceType.String("1").Equals(UnionWithReferenceType.String("1")));
		Assert.False(UnionWithReferenceType.String("1").Equals(UnionWithReferenceType.String("2")));
		Assert.False(UnionWithReferenceType.String("1").Equals(null));
		Assert.False(UnionWithReferenceType.String("1").Equals(UnionWithReferenceType.Int(1)));

		Assert.True(RootUnion.Empty().Equals(RootUnion.Empty()));
		Assert.False(RootUnion.Empty().Equals(RootUnion.NestedCase(NestedUnion.Empty())));
		Assert.False(RootUnion.Empty().Equals(null));

		Assert.True(RootUnion.NestedCase(NestedUnion.Empty()).Equals(RootUnion.NestedCase(NestedUnion.Empty())));
		Assert.False(RootUnion.NestedCase(NestedUnion.Empty()).Equals(RootUnion.NestedCase(NestedUnion.Node(1))));
		Assert.False(RootUnion.NestedCase(NestedUnion.Empty()).Equals(null));

		Assert.True(RootUnion.NestedCase(NestedUnion.Node(1)).Equals(RootUnion.NestedCase(NestedUnion.Node(1))));
		Assert.False(RootUnion.NestedCase(NestedUnion.Node(1)).Equals(RootUnion.NestedCase(NestedUnion.Node(2))));
		Assert.False(RootUnion.NestedCase(NestedUnion.Node(1)).Equals(RootUnion.NestedCase(NestedUnion.Empty())));
		Assert.False(RootUnion.NestedCase(NestedUnion.Node(1)).Equals(null));
	}

	[Fact]
	public void EqualsShouldNotUseDataFromInactiveCase()
	{
		// Create a circle and incorrectly set the rectangle data.
		// The memory for the rectangle case is separated because generic types cannot have an explicit layout.
		Shape<int> shape = Shape<int>.Circle(1);
		shape.RectangleData = new Shape<int>.RectangleCase { Width = 1, Height = 1 };

		Assert.True(shape.IsCircle);
		Assert.False(shape.IsRectangle);
		Assert.Equal(1, shape.CircleData);
		Assert.Equal(Shape<int>.CircleIndex, shape.CaseIndex);
		Assert.True(shape.Equals(Shape<int>.Circle(1)));
	}
}
