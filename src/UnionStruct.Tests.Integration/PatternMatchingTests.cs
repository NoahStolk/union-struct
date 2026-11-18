using System.Numerics;
using UnionStruct.Tests.Integration.Unions;

namespace UnionStruct.Tests.Integration;

/// <summary>
/// Every switch expression here covers each case and has no default arm. The project treats CS8509 as an error, so
/// these only compile because the compiler sees the generated structs as closed unions.
/// </summary>
public sealed class PatternMatchingTests
{
	[Test]
	public async Task NativeSwitchMatchesCaseTypes()
	{
		await Assert.That(GetPoints(default(EnumLikeUnion.Bronze))).IsEqualTo(1);
		await Assert.That(GetPoints(default(EnumLikeUnion.Silver))).IsEqualTo(2);
		await Assert.That(GetPoints(default(EnumLikeUnion.Gold))).IsEqualTo(3);

		await Assert.That(GetRotation(default(RotationType.None))).IsEqualTo(Quaternion.Identity);
		await Assert.That(GetRotation(default(RotationType.RandomRotation))).IsEqualTo(Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f));
		await Assert.That(GetRotation(new RandomRotationAroundAxis(Vector3.UnitX))).IsEqualTo(Quaternion.CreateFromAxisAngle(Vector3.UnitX, 0.4f));
		await Assert.That(GetRotation(new RotationRangeAroundAxis(Vector3.UnitY, 0.1f, 0.2f))).IsEqualTo(Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.15f));
		await Assert.That(GetRotation(new CustomRotation(Quaternion.CreateFromYawPitchRoll(1, 2, 3)))).IsEqualTo(Quaternion.CreateFromYawPitchRoll(1, 2, 3));

		await Assert.That(GetNode(default(Empty))).IsEqualTo(0);
		await Assert.That(GetNode(new NestedUnion(default(Empty)))).IsEqualTo(0);
		await Assert.That(GetNode(new NestedUnion(new Node(1)))).IsEqualTo(1);

		static int GetPoints(EnumLikeUnion enumLikeUnion) => enumLikeUnion switch
		{
			EnumLikeUnion.Bronze => 1,
			EnumLikeUnion.Silver => 2,
			EnumLikeUnion.Gold => 3,
		};

		static Quaternion GetRotation(RotationType rotationType) => rotationType switch
		{
			RotationType.None => Quaternion.Identity,
			RotationType.RandomRotation => Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f),
			RandomRotationAroundAxis r => Quaternion.CreateFromAxisAngle(r.Axis, 0.4f),
			RotationRangeAroundAxis r => Quaternion.CreateFromAxisAngle(r.Axis, (r.AngleMin + r.AngleMax) / 2f),
			CustomRotation r => r.Rotation,
		};

		static int GetNode(RootUnion rootUnion) => rootUnion switch
		{
			Empty => 0,
			NestedUnion nested => nested switch
			{
				Empty => 0,
				Node node => node.Value,
			},
		};
	}

	[Test]
	public async Task OpenGenericSwitchWithPatternVariables()
	{
		await Assert.That(Area<float>(new Circle<float>(2f))).IsEqualTo(4f);
		await Assert.That(Area<int>(new Rectangle<int>(2, 3))).IsEqualTo(6);

		static T Area<T>(Shape<T> shape)
			where T : INumber<T> => shape switch
		{
			Circle<T> c => c.Radius * c.Radius,
			Rectangle<T> r => r.Width * r.Height,
		};
	}

	[Test]
	public async Task PropertyPatternsAndIs()
	{
		UnionWithReferenceType union = "hello";

		await Assert.That(union is string { Length: 5 }).IsTrue();
		await Assert.That(union is int).IsFalse();
#pragma warning disable CA1508 // The dataflow analysis does not know that a union's type patterns test the active case.
		await Assert.That(union is string s && s == "hello").IsTrue();
#pragma warning restore CA1508
	}

	[Test]
	public async Task TryGetValueReadsTheActiveCaseOnly()
	{
		CompressedIndex index = (ushort)300;

		await Assert.That(index.TryGetValue(out ushort value)).IsTrue();
		await Assert.That(value).IsEqualTo((ushort)300);
		await Assert.That(index.TryGetValue(out byte _)).IsFalse();
		await Assert.That(index.TryGetValue(out uint _)).IsFalse();
	}

	[Test]
	public async Task ValueBoxesTheActivePayload()
	{
		CompressedIndex index = 7u;
		UnionWithReferenceType text = "text";

		await Assert.That(index.Value).IsEqualTo(7u);
		await Assert.That(text.Value).IsEqualTo("text");
	}

	[Test]
	public async Task CaseIndexSwitchWithoutDefaultWorks()
	{
		await Assert.That(PointName(default(EnumLikeUnion.Bronze))).IsEqualTo("Bronze");
		await Assert.That(PointName(default(EnumLikeUnion.Silver))).IsEqualTo("Silver");
		await Assert.That(PointName(default(EnumLikeUnion.Gold))).IsEqualTo("Gold");

		// Unlike the type switch, the compiler does not check this one for exhaustiveness; the discard is required.
		static string PointName(EnumLikeUnion u) => u.CaseIndex switch
		{
			EnumLikeUnion.BronzeIndex => "Bronze",
			EnumLikeUnion.SilverIndex => "Silver",
			EnumLikeUnion.GoldIndex => "Gold",
			_ => throw new InvalidOperationException(),
		};
	}

	[Test]
	public async Task DefaultIsTheFirstCase()
	{
		await Assert.That(default(EnumLikeUnion) is EnumLikeUnion.Bronze).IsTrue();
		await Assert.That(new Circle<int>(0) == default(Shape<int>)).IsTrue();
	}
}
