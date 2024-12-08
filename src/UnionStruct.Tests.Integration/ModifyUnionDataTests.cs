using FluentAssertions;
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
		rotationType.RandomRotationAroundAxisData.Axis.Should().Be(Vector3.UnitX);

		rotationType.RandomRotationAroundAxisData.Axis = Vector3.UnitY;
		rotationType.RandomRotationAroundAxisData.Axis.Should().Be(Vector3.UnitY);
	}

	[Fact]
	public void ModifyRotationRangeAroundAxisAngles()
	{
		RotationType rotationType = RotationType.RotationRangeAroundAxis(new RotationRangeAroundAxis(Vector3.UnitY, 0.1f, 0.2f));
		rotationType.RotationRangeAroundAxisData.AngleMin.Should().Be(0.1f);
		rotationType.RotationRangeAroundAxisData.AngleMax.Should().Be(0.2f);

		rotationType.RotationRangeAroundAxisData.AngleMin = 0.3f;
		rotationType.RotationRangeAroundAxisData.AngleMax = 0.4f;
		rotationType.RotationRangeAroundAxisData.AngleMin.Should().Be(0.3f);
		rotationType.RotationRangeAroundAxisData.AngleMax.Should().Be(0.4f);

		RotateAngles(ref rotationType.RotationRangeAroundAxisData, 0.1f);
		rotationType.RotationRangeAroundAxisData.AngleMin.Should().Be(0.4f);
		rotationType.RotationRangeAroundAxisData.AngleMax.Should().Be(0.5f);

		static void RotateAngles(ref RotationRangeAroundAxis rotationRangeAroundAxis, float angle)
		{
			rotationRangeAroundAxis.AngleMin += angle;
			rotationRangeAroundAxis.AngleMax += angle;
		}
	}
}
