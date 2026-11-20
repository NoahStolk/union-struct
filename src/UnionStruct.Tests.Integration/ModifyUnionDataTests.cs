using System.Numerics;
using UnionStruct.Tests.Integration.Unions;

namespace UnionStruct.Tests.Integration;

/// <summary>
/// The payload fields are public so callers can take <c>ref</c> to them and mutate a case in place; the native union
/// pattern alone only hands out copies through <c>TryGetValue</c>.
/// </summary>
public sealed class ModifyUnionDataTests
{
	[Test]
	public async Task ModifyRotationType()
	{
		RotationType rotationType = new RandomRotationAroundAxis(Vector3.UnitX);
		await Assert.That(rotationType.RandomRotationAroundAxisData.Axis).IsEqualTo(Vector3.UnitX);

		rotationType.RandomRotationAroundAxisData.Axis = Vector3.UnitY;
		await Assert.That(rotationType.RandomRotationAroundAxisData.Axis).IsEqualTo(Vector3.UnitY);
		await Assert.That(rotationType is RandomRotationAroundAxis { Axis.Y: 1 }).IsTrue();
	}

	[Test]
	public async Task ModifyRotationRangeAroundAxisAngles()
	{
		RotationType rotationType = new RotationRangeAroundAxis(Vector3.UnitY, 0.1f, 0.2f);
		await Assert.That(rotationType.RotationRangeAroundAxisData.AngleMin).IsEqualTo(0.1f);
		await Assert.That(rotationType.RotationRangeAroundAxisData.AngleMax).IsEqualTo(0.2f);

		rotationType.RotationRangeAroundAxisData.AngleMin = 0.3f;
		rotationType.RotationRangeAroundAxisData.AngleMax = 0.4f;
		await Assert.That(rotationType.RotationRangeAroundAxisData.AngleMin).IsEqualTo(0.3f);
		await Assert.That(rotationType.RotationRangeAroundAxisData.AngleMax).IsEqualTo(0.4f);

		RotateAngles(ref rotationType.RotationRangeAroundAxisData, 0.1f);
		await Assert.That(rotationType.RotationRangeAroundAxisData.AngleMin).IsEqualTo(0.4f);
		await Assert.That(rotationType.RotationRangeAroundAxisData.AngleMax).IsEqualTo(0.5f);

		static void RotateAngles(ref RotationRangeAroundAxis rotationRangeAroundAxis, float angle)
		{
			rotationRangeAroundAxis.AngleMin += angle;
			rotationRangeAroundAxis.AngleMax += angle;
		}
	}

	[Test]
	public async Task ReplaceReadOnlyPayloadThroughRef()
	{
		// Case types are often readonly records; the payload is then replaced through the ref instead.
		Shape<float> shape = new Circle<float>(2f);
		ref Circle<float> slot = ref shape.CircleData;
		slot = slot with { Radius = 9f };

		await Assert.That(shape == new Circle<float>(9f)).IsTrue();
	}
}
