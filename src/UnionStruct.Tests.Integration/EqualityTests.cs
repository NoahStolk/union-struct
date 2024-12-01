﻿using FluentAssertions;
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
	}
}
