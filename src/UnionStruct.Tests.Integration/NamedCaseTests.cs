using TUnit.Assertions.Enums;
using UnionStruct.Tests.Integration.Unions;

namespace UnionStruct.Tests.Integration;

public sealed class NamedCaseTests
{
	[Test]
	public async Task NameDrivesGeneratedMembers()
	{
		MixedPrecisionCircle intCircle = new Circle<int>(2);
		MixedPrecisionCircle floatCircle = new Circle<float>(1.5f);

		await Assert.That(intCircle.CaseIndex).IsEqualTo(MixedPrecisionCircle.IntCircleIndex);
		await Assert.That(floatCircle.CaseIndex).IsEqualTo(MixedPrecisionCircle.FloatCircleIndex);
		await Assert.That(intCircle.IntCircleData.Radius).IsEqualTo(2);
		await Assert.That(floatCircle.FloatCircleData.Radius).IsEqualTo(1.5f);
	}

	[Test]
	public async Task NamedCasesMatchByType()
	{
		await Assert.That(Describe(new Circle<int>(2))).IsEqualTo("int 2");
		await Assert.That(Describe(new Circle<float>(1.5f))).IsEqualTo("float 1.5");

		static string Describe(MixedPrecisionCircle circle) => circle switch
		{
			Circle<int> c => $"int {c.Radius}",
			Circle<float> c => $"float {c.Radius.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
		};
	}

	[Test]
	public async Task NameIsTheDefaultDisplayName()
	{
		MixedPrecisionCircle intCircle = new Circle<int>(2);
		MixedPrecisionCircle floatCircle = new Circle<float>(1.5f);

		await Assert.That(intCircle.ToString()).IsEqualTo("IntCircle { Value = Circle { Radius = 2 } }");
		await Assert.That(floatCircle.ToString()).IsEqualTo("Circle (float) { Value = Circle { Radius = 1.5 } }");
		await Assert.That(intCircle.GetTypeString()).IsEqualTo("IntCircle");
		await Assert.That([.. floatCircle.GetTypeAsUtf8Span()]).IsEquivalentTo([.. "Circle (float)"u8], CollectionOrdering.Matching);
		await Assert.That([.. MixedPrecisionCircle.NullTerminatedMemberNames]).IsEquivalentTo([.. "IntCircle\0Circle (float)\0"u8], CollectionOrdering.Matching);
	}

	[Test]
	public async Task NamedPayloadIsMutableThroughRef()
	{
		MixedPrecisionCircle circle = new Circle<float>(1f);
		ref Circle<float> slot = ref circle.FloatCircleData;
		slot = slot with { Radius = 4f };

		await Assert.That(circle == new Circle<float>(4f)).IsTrue();
	}
}
