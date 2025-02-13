using System.Numerics;
using UnionStruct.Tests.Integration.Unions;
using Xunit;

namespace UnionStruct.Tests.Integration;

public sealed class ModifyUnionDataTests
{
	[Fact]
	public void ModifyRotationType()
	{
		RotationType rotationType = RotationType.RandomRotationAroundAxis(new RandomRotationAroundAxis(Vector3.UnitX));
		Assert.Equal(Vector3.UnitX, rotationType.RandomRotationAroundAxisData.Axis);

		rotationType.RandomRotationAroundAxisData.Axis = Vector3.UnitY;
		Assert.Equal(Vector3.UnitY, rotationType.RandomRotationAroundAxisData.Axis);
	}

	[Fact]
	public void ModifyRotationRangeAroundAxisAngles()
	{
		RotationType rotationType = RotationType.RotationRangeAroundAxis(new RotationRangeAroundAxis(Vector3.UnitY, 0.1f, 0.2f));
		Assert.Equal(0.1f, rotationType.RotationRangeAroundAxisData.AngleMin);
		Assert.Equal(0.2f, rotationType.RotationRangeAroundAxisData.AngleMax);

		rotationType.RotationRangeAroundAxisData.AngleMin = 0.3f;
		rotationType.RotationRangeAroundAxisData.AngleMax = 0.4f;
		Assert.Equal(0.3f, rotationType.RotationRangeAroundAxisData.AngleMin);
		Assert.Equal(0.4f, rotationType.RotationRangeAroundAxisData.AngleMax);

		RotateAngles(ref rotationType.RotationRangeAroundAxisData, 0.1f);
		Assert.Equal(0.4f, rotationType.RotationRangeAroundAxisData.AngleMin);
		Assert.Equal(0.5f, rotationType.RotationRangeAroundAxisData.AngleMax);

		static void RotateAngles(ref RotationRangeAroundAxis rotationRangeAroundAxis, float angle)
		{
			rotationRangeAroundAxis.AngleMin += angle;
			rotationRangeAroundAxis.AngleMax += angle;
		}
	}
}
