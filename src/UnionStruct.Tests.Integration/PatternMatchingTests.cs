using System.Numerics;
using UnionStruct.Tests.Integration.Unions;

namespace UnionStruct.Tests.Integration;

public sealed class PatternMatchingTests
{
	[Test]
	public async Task MatchWorksCorrectly()
	{
		await Assert.That(GetPoints(EnumLikeUnion.Bronze())).IsEqualTo(1);
		await Assert.That(GetPoints(EnumLikeUnion.Silver())).IsEqualTo(2);
		await Assert.That(GetPoints(EnumLikeUnion.Gold())).IsEqualTo(3);

		await Assert.That(GetRotation(RotationType.None())).IsEqualTo(Quaternion.Identity);
		await Assert.That(GetRotation(RotationType.RandomRotation())).IsEqualTo(Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f));
		await Assert.That(GetRotation(RotationType.RandomRotationAroundAxis(new RandomRotationAroundAxis(Vector3.UnitX)))).IsEqualTo(Quaternion.CreateFromAxisAngle(Vector3.UnitX, 0.4f));
		await Assert.That(GetRotation(RotationType.RotationRangeAroundAxis(new RotationRangeAroundAxis(Vector3.UnitY, 0.1f, 0.2f)))).IsEqualTo(Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.15f));
		await Assert.That(GetRotation(RotationType.CustomRotation(new CustomRotation(Quaternion.CreateFromYawPitchRoll(1, 2, 3))))).IsEqualTo(Quaternion.CreateFromYawPitchRoll(1, 2, 3));

		await Assert.That(GetNode(RootUnion.Empty())).IsEqualTo(0);
		await Assert.That(GetNode(RootUnion.NestedCase(NestedUnion.Empty()))).IsEqualTo(0);
		await Assert.That(GetNode(RootUnion.NestedCase(NestedUnion.Node(1)))).IsEqualTo(1);

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

	[Test]
	public async Task SwitchWorksCorrectly()
	{
		int bronzeCount = 0;
		int silverCount = 0;
		int goldCount = 0;

		foreach (EnumLikeUnion enumLikeUnion in new[] { EnumLikeUnion.Bronze(), EnumLikeUnion.Bronze(), EnumLikeUnion.Bronze(), EnumLikeUnion.Silver(), EnumLikeUnion.Silver(), EnumLikeUnion.Gold() })
			enumLikeUnion.Switch(() => bronzeCount++, () => silverCount++, () => goldCount++);

		await Assert.That(bronzeCount).IsEqualTo(3);
		await Assert.That(silverCount).IsEqualTo(2);
		await Assert.That(goldCount).IsEqualTo(1);
	}

	[Test]
	public async Task CaseIndexSwitchWithoutDefaultWorks()
	{
		await Assert.That(PointName(EnumLikeUnion.Bronze())).IsEqualTo("Bronze");
		await Assert.That(PointName(EnumLikeUnion.Silver())).IsEqualTo("Silver");
		await Assert.That(PointName(EnumLikeUnion.Gold())).IsEqualTo("Gold");

		static string PointName(EnumLikeUnion u) => u.CaseIndex switch
		{
			EnumLikeUnion.BronzeIndex => "Bronze",
			EnumLikeUnion.SilverIndex => "Silver",
			EnumLikeUnion.GoldIndex => "Gold",
		};
	}

	[Test]
	public async Task TagSwitchWithoutDiscardArmWorks()
	{
		await Assert.That(Rank(EnumLikeUnion.Bronze())).IsEqualTo(1);
		await Assert.That(Rank(EnumLikeUnion.Silver())).IsEqualTo(2);
		await Assert.That(Rank(EnumLikeUnion.Gold())).IsEqualTo(3);

		static int Rank(EnumLikeUnion u) => u.Tag switch
		{
			EnumLikeUnion.CaseTag.Bronze => 1,
			EnumLikeUnion.CaseTag.Silver => 2,
			EnumLikeUnion.CaseTag.Gold => 3,
		};
	}
}
