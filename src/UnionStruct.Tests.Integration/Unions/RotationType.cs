using System.Numerics;
using System.Runtime.CompilerServices;

namespace UnionStruct.Tests.Integration.Unions;

[Union]
internal partial struct RotationType
{
	public partial RotationType(None value);

	public partial RotationType(RandomRotation value);

	public partial RotationType(RandomRotationAroundAxis value);

	public partial RotationType(RotationRangeAroundAxis value);

	public partial RotationType(CustomRotation value);

	public readonly record struct None;

	public readonly record struct RandomRotation;
}

internal record struct RandomRotationAroundAxis(Vector3 Axis)
{
	public Vector3 Axis = Axis;
}

internal record struct RotationRangeAroundAxis(Vector3 Axis, float AngleMin, float AngleMax)
{
	public Vector3 Axis = Axis;
	public float AngleMin = AngleMin;
	public float AngleMax = AngleMax;
}

internal record struct CustomRotation(Quaternion Rotation)
{
	public Quaternion Rotation = Rotation;
}
