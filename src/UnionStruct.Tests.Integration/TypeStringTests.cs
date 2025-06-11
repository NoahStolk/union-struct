using System.Numerics;
using UnionStruct.Tests.Integration.Unions;
using Xunit;

namespace UnionStruct.Tests.Integration;

public sealed class TypeStringTests
{
	[Fact]
	public void GetTypeStringReturnsCorrectResult()
	{
		Assert.Equal("Bronze", EnumLikeUnion.Bronze().GetTypeString());
		Assert.Equal("Silver", EnumLikeUnion.Silver().GetTypeString());
		Assert.Equal("Gold", EnumLikeUnion.Gold().GetTypeString());

		Assert.Equal("None", RotationType.None().GetTypeString());
		Assert.Equal("RandomRotation", RotationType.RandomRotation().GetTypeString());
		Assert.Equal("RandomRotationAroundAxis", RotationType.RandomRotationAroundAxis(new RandomRotationAroundAxis(Vector3.UnitX)).GetTypeString());
		Assert.Equal("RotationRangeAroundAxis", RotationType.RotationRangeAroundAxis(new RotationRangeAroundAxis(Vector3.UnitY, 0.1f, 0.2f)).GetTypeString());
		Assert.Equal("CustomRotation", RotationType.CustomRotation(new CustomRotation(Quaternion.Identity)).GetTypeString());

		Assert.Equal("8-bit", CompressedIndex.Unsigned8(1).GetTypeString());
		Assert.Equal("16-bit", CompressedIndex.Unsigned16(2).GetTypeString());
		Assert.Equal("32-bit", CompressedIndex.Unsigned32(3).GetTypeString());

		Assert.Equal("Circle", Shape<float>.Circle(1.5f).GetTypeString());
		Assert.Equal("Rectangle", Shape<float>.Rectangle(2.5f, 3.5f).GetTypeString());

		Assert.Equal("Circle", Shape<int>.Circle(1).GetTypeString());
		Assert.Equal("Rectangle", Shape<int>.Rectangle(2, 3).GetTypeString());

		Assert.Equal("Int", UnionWithReferenceType.Int(1).GetTypeString());
		Assert.Equal("String", UnionWithReferenceType.String("1").GetTypeString());

		Assert.Equal("Empty", RootUnion.Empty().GetTypeString());
		Assert.Equal("Nested", RootUnion.NestedCase(NestedUnion.Empty()).GetTypeString());
		Assert.Equal("Nested", RootUnion.NestedCase(NestedUnion.Node(1)).GetTypeString());
	}

	[Fact]
	public void GetTypeAsUtf8SpanReturnsCorrectResult()
	{
		Assert.Equal("Bronze"u8, EnumLikeUnion.Bronze().GetTypeAsUtf8Span());
		Assert.Equal("Silver"u8, EnumLikeUnion.Silver().GetTypeAsUtf8Span());
		Assert.Equal("Gold"u8, EnumLikeUnion.Gold().GetTypeAsUtf8Span());

		Assert.Equal("None"u8, RotationType.None().GetTypeAsUtf8Span());
		Assert.Equal("RandomRotation"u8, RotationType.RandomRotation().GetTypeAsUtf8Span());
		Assert.Equal("RandomRotationAroundAxis"u8, RotationType.RandomRotationAroundAxis(new RandomRotationAroundAxis(Vector3.UnitX)).GetTypeAsUtf8Span());
		Assert.Equal("RotationRangeAroundAxis"u8, RotationType.RotationRangeAroundAxis(new RotationRangeAroundAxis(Vector3.UnitY, 0.1f, 0.2f)).GetTypeAsUtf8Span());
		Assert.Equal("CustomRotation"u8, RotationType.CustomRotation(new CustomRotation(Quaternion.Identity)).GetTypeAsUtf8Span());

		Assert.Equal("8-bit"u8, CompressedIndex.Unsigned8(1).GetTypeAsUtf8Span());
		Assert.Equal("16-bit"u8, CompressedIndex.Unsigned16(2).GetTypeAsUtf8Span());
		Assert.Equal("32-bit"u8, CompressedIndex.Unsigned32(3).GetTypeAsUtf8Span());

		Assert.Equal("Circle"u8, Shape<float>.Circle(1.5f).GetTypeAsUtf8Span());
		Assert.Equal("Rectangle"u8, Shape<float>.Rectangle(2.5f, 3.5f).GetTypeAsUtf8Span());

		Assert.Equal("Circle"u8, Shape<int>.Circle(1).GetTypeAsUtf8Span());
		Assert.Equal("Rectangle"u8, Shape<int>.Rectangle(2, 3).GetTypeAsUtf8Span());

		Assert.Equal("Int"u8, UnionWithReferenceType.Int(1).GetTypeAsUtf8Span());
		Assert.Equal("String"u8, UnionWithReferenceType.String("1").GetTypeAsUtf8Span());

		Assert.Equal("Empty"u8, RootUnion.Empty().GetTypeAsUtf8Span());
		Assert.Equal("Nested"u8, RootUnion.NestedCase(NestedUnion.Empty()).GetTypeAsUtf8Span());
		Assert.Equal("Nested"u8, RootUnion.NestedCase(NestedUnion.Node(1)).GetTypeAsUtf8Span());
	}
}
