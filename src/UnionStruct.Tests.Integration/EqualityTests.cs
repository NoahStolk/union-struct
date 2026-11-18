using UnionStruct.Tests.Integration.Unions;

namespace UnionStruct.Tests.Integration;

public sealed class EqualityTests
{
	[Test]
	public async Task EqualsReturnsCorrectResult()
	{
		EnumLikeUnion bronze = default(EnumLikeUnion.Bronze);
		await Assert.That(bronze.Equals(default(EnumLikeUnion.Bronze))).IsTrue();
		await Assert.That(bronze.Equals(default(EnumLikeUnion.Silver))).IsFalse();
		await Assert.That(bronze.Equals(null)).IsFalse();

		RotationType none = default(RotationType.None);
		await Assert.That(none.Equals(default(RotationType.None))).IsTrue();
		await Assert.That(none.Equals(default(RotationType.RandomRotation))).IsFalse();
		await Assert.That(none.Equals(null)).IsFalse();

		CompressedIndex index = (byte)1;
		await Assert.That(index.Equals((byte)1)).IsTrue();
		await Assert.That(index.Equals((byte)2)).IsFalse();
		await Assert.That(index.Equals((ushort)1)).IsFalse();
		await Assert.That(index.Equals(null)).IsFalse();

		Shape<float> floatCircle = new Circle<float>(1.5f);
		await Assert.That(floatCircle.Equals(new Circle<float>(1.5f))).IsTrue();
		await Assert.That(floatCircle.Equals(new Circle<float>(2.5f))).IsFalse();
		await Assert.That(floatCircle.Equals(null)).IsFalse();
		await Assert.That(floatCircle.Equals(new Rectangle<float>(1.5f, 1.5f))).IsFalse();

		// ReSharper disable once SuspiciousTypeConversion.Global
		await Assert.That(floatCircle.Equals((object)new Shape<int>(new Circle<int>(1)))).IsFalse();

		UnionWithReferenceType int1 = 1;
		UnionWithReferenceType string1 = "1";
		await Assert.That(int1.Equals(1)).IsTrue();
		await Assert.That(int1.Equals(2)).IsFalse();
		await Assert.That(int1.Equals(string1)).IsFalse();
		await Assert.That(string1.Equals("1")).IsTrue();
		await Assert.That(string1.Equals("2")).IsFalse();
		await Assert.That(string1.Equals(int1)).IsFalse();

		// A bare `null` would bind to Equals(UnionWithReferenceType) through the implicit conversion from string.
		await Assert.That(string1.Equals((object?)null)).IsFalse();

		RootUnion empty = default(Empty);
		RootUnion nestedEmpty = new NestedUnion(default(Empty));
		RootUnion nestedNode = new NestedUnion(new Node(1));
		await Assert.That(empty.Equals(default(Empty))).IsTrue();
		await Assert.That(empty.Equals(nestedEmpty)).IsFalse();
		await Assert.That(nestedEmpty.Equals(new NestedUnion(default(Empty)))).IsTrue();
		await Assert.That(nestedEmpty.Equals(nestedNode)).IsFalse();
		await Assert.That(nestedNode.Equals(new NestedUnion(new Node(1)))).IsTrue();
		await Assert.That(nestedNode.Equals(new NestedUnion(new Node(2)))).IsFalse();
	}

	[Test]
	public async Task EqualityOperatorsAndHashCodeAgreeWithEquals()
	{
		Shape<int> a = new Circle<int>(1);
		Shape<int> b = new Circle<int>(1);
		Shape<int> c = new Rectangle<int>(1, 1);

		await Assert.That(a == b).IsTrue();
		await Assert.That(a != b).IsFalse();
		await Assert.That(a == c).IsFalse();
		await Assert.That(a != c).IsTrue();
		await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
	}

	[Test]
	public async Task EqualsShouldNotUseDataFromInactiveCase()
	{
		// Create a circle and incorrectly set the rectangle data.
		// The memory for the rectangle case is separate because generic types cannot have an explicit layout.
		Shape<int> shape = new Circle<int>(1);
		shape.RectangleData = new Rectangle<int>(1, 1);

		await Assert.That(shape is Circle<int>).IsTrue();
		await Assert.That(shape is Rectangle<int>).IsFalse();
		await Assert.That(shape.CircleData.Radius).IsEqualTo(1);
		await Assert.That(shape.CaseIndex).IsEqualTo(Shape<int>.CircleIndex);
		await Assert.That(shape.Equals(new Circle<int>(1))).IsTrue();
	}
}
