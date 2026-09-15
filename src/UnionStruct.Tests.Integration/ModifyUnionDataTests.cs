using System.Numerics;
using UnionStruct.Tests.Integration.Unions;

namespace UnionStruct.Tests.Integration;

public sealed class ModifyUnionDataTests
{
	[Test]
	public async Task ModifyRotationType()
	{
		RotationType rotationType = RotationType.RandomRotationAroundAxis(new RandomRotationAroundAxis(Vector3.UnitX));
		await Assert.That(rotationType.RandomRotationAroundAxisData.Axis).IsEqualTo(Vector3.UnitX);

		rotationType.RandomRotationAroundAxisData.Axis = Vector3.UnitY;
		await Assert.That(rotationType.RandomRotationAroundAxisData.Axis).IsEqualTo(Vector3.UnitY);
	}

	[Test]
	public async Task ModifyRotationRangeAroundAxisAngles()
	{
		RotationType rotationType = RotationType.RotationRangeAroundAxis(new RotationRangeAroundAxis(Vector3.UnitY, 0.1f, 0.2f));
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
}
