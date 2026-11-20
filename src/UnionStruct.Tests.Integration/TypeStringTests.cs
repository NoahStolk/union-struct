using System.Numerics;
using TUnit.Assertions.Enums;
using UnionStruct.Tests.Integration.Unions;

namespace UnionStruct.Tests.Integration;

public sealed class TypeStringTests
{
	public static IEnumerable<Func<(string Expected, string Actual, byte[] ActualUtf8)>> Cases()
	{
		yield return () => Case("Bronze", new EnumLikeUnion(default(EnumLikeUnion.Bronze)));
		yield return () => Case("Silver", new EnumLikeUnion(default(EnumLikeUnion.Silver)));
		yield return () => Case("Gold", new EnumLikeUnion(default(EnumLikeUnion.Gold)));

		yield return () => Case("None", new RotationType(default(RotationType.None)));
		yield return () => Case("RandomRotation", new RotationType(default(RotationType.RandomRotation)));
		yield return () => Case("RandomRotationAroundAxis", new RotationType(new RandomRotationAroundAxis(Vector3.UnitX)));
		yield return () => Case("RotationRangeAroundAxis", new RotationType(new RotationRangeAroundAxis(Vector3.UnitY, 0.1f, 0.2f)));
		yield return () => Case("CustomRotation", new RotationType(new CustomRotation(Quaternion.Identity)));

		yield return () => Case("8-bit", new CompressedIndex((byte)1));
		yield return () => Case("16-bit", new CompressedIndex((ushort)2));
		yield return () => Case("32-bit", new CompressedIndex(3u));

		yield return () => Case("Circle", new Shape<float>(new Circle<float>(1.5f)));
		yield return () => Case("Rectangle", new Shape<int>(new Rectangle<int>(2, 3)));

		yield return () => Case("Int32", new UnionWithReferenceType(1));
		yield return () => Case("String", new UnionWithReferenceType("1"));

		yield return () => Case("Empty", new RootUnion(default(Empty)));
		yield return () => Case("Nested", new RootUnion(new NestedUnion(new Node(1))));
	}

	[Test]
	[MethodDataSource(nameof(Cases))]
	public async Task GetTypeStringReturnsCorrectResult(string expected, string actual, byte[] actualUtf8)
	{
		await Assert.That(actual).IsEqualTo(expected);
		await Assert.That(actualUtf8).IsEquivalentTo(System.Text.Encoding.UTF8.GetBytes(expected), CollectionOrdering.Matching);
	}

	[Test]
	public async Task StaticOverloadsTakeACaseIndex()
	{
		await Assert.That(CompressedIndex.GetTypeString(CompressedIndex.UInt16Index)).IsEqualTo("16-bit");
		await Assert.That([.. CompressedIndex.GetTypeAsUtf8Span(CompressedIndex.UInt32Index)]).IsEquivalentTo([.. "32-bit"u8], CollectionOrdering.Matching);
		await Assert.That(() => CompressedIndex.GetTypeString(CompressedIndex.CaseCount)).Throws<System.Diagnostics.UnreachableException>();
	}

	private static (string Expected, string Actual, byte[] ActualUtf8) Case(string expected, EnumLikeUnion u)
	{
		return (expected, u.GetTypeString(), [.. u.GetTypeAsUtf8Span()]);
	}

	private static (string Expected, string Actual, byte[] ActualUtf8) Case(string expected, RotationType u)
	{
		return (expected, u.GetTypeString(), [.. u.GetTypeAsUtf8Span()]);
	}

	private static (string Expected, string Actual, byte[] ActualUtf8) Case(string expected, CompressedIndex u)
	{
		return (expected, u.GetTypeString(), [.. u.GetTypeAsUtf8Span()]);
	}

	private static (string Expected, string Actual, byte[] ActualUtf8) Case<T>(string expected, Shape<T> u)
		where T : INumber<T>
	{
		return (expected, u.GetTypeString(), [.. u.GetTypeAsUtf8Span()]);
	}

	private static (string Expected, string Actual, byte[] ActualUtf8) Case(string expected, UnionWithReferenceType u)
	{
		return (expected, u.GetTypeString(), [.. u.GetTypeAsUtf8Span()]);
	}

	private static (string Expected, string Actual, byte[] ActualUtf8) Case(string expected, RootUnion u)
	{
		return (expected, u.GetTypeString(), [.. u.GetTypeAsUtf8Span()]);
	}
}
