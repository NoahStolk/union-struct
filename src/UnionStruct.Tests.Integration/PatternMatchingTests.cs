using FluentAssertions;
using System.Numerics;
using UnionStruct.Tests.Integration.Unions;
using Xunit;

namespace UnionStruct.Tests.Integration;

public sealed class PatternMatchingTests
{
	[Fact]
	public void MatchWorksCorrectly()
	{
		GetPoints(EnumLikeUnion.Bronze()).Should().Be(1);
		GetPoints(EnumLikeUnion.Silver()).Should().Be(2);
		GetPoints(EnumLikeUnion.Gold()).Should().Be(3);

		GetRotation(RotationType.None()).Should().Be(Quaternion.Identity);
		GetRotation(RotationType.RandomRotation()).Should().Be(Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f));
		GetRotation(RotationType.RandomRotationAroundAxis(new RandomRotationAroundAxis(Vector3.UnitX))).Should().Be(Quaternion.CreateFromAxisAngle(Vector3.UnitX, 0.4f));
		GetRotation(RotationType.RotationRangeAroundAxis(new RotationRangeAroundAxis(Vector3.UnitY, 0.1f, 0.2f))).Should().Be(Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.15f));
		GetRotation(RotationType.CustomRotation(new CustomRotation(Quaternion.CreateFromYawPitchRoll(1, 2, 3)))).Should().Be(Quaternion.CreateFromYawPitchRoll(1, 2, 3));

		GetNode(RootUnion.Empty()).Should().Be(0);
		GetNode(RootUnion.NestedCase(NestedUnion.Empty())).Should().Be(0);
		GetNode(RootUnion.NestedCase(NestedUnion.Node(1))).Should().Be(1);

		static int GetPoints(EnumLikeUnion enumLikeUnion)
		{
			return enumLikeUnion.Match(() => 1, () => 2, () => 3);
		}

		static Quaternion GetRotation(RotationType rotationType)
		{
			return rotationType.Match(
				() => Quaternion.Identity,
				() => Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f), // Random rotation
				randomRotationAroundAxis => Quaternion.CreateFromAxisAngle(randomRotationAroundAxis.Axis, 0.4f),
				rotationRangeAroundAxis => Quaternion.CreateFromAxisAngle(rotationRangeAroundAxis.Axis, (rotationRangeAroundAxis.AngleMin + rotationRangeAroundAxis.AngleMax) / 2f),
				customRotation => customRotation.Rotation);
		}

		static int GetNode(RootUnion rootUnion)
		{
			return rootUnion.Match(
				() => 0,
				nestedUnion => nestedUnion.Match(
					() => 0,
					node => node));
		}
	}

	[Fact]
	public void SwitchWorksCorrectly()
	{
		int bronzeCount = 0;
		int silverCount = 0;
		int goldCount = 0;

		foreach (EnumLikeUnion enumLikeUnion in new[] { EnumLikeUnion.Bronze(), EnumLikeUnion.Bronze(), EnumLikeUnion.Bronze(), EnumLikeUnion.Silver(), EnumLikeUnion.Silver(), EnumLikeUnion.Gold() })
			enumLikeUnion.Switch(() => bronzeCount++, () => silverCount++, () => goldCount++);

		bronzeCount.Should().Be(3);
		silverCount.Should().Be(2);
		goldCount.Should().Be(1);
	}
}
