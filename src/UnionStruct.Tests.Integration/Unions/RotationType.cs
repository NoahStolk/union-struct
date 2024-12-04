using System.Numerics;

namespace UnionStruct.Tests.Integration.Unions;

[Union]
internal partial struct RotationType
{
	[UnionCase]
	public static partial RotationType None();

	[UnionCase]
	public static partial RotationType RandomRotation();

	[UnionCase]
	public static partial RotationType RandomRotationAroundAxis(RandomRotationAroundAxis value);

	[UnionCase]
	public static partial RotationType RotationRangeAroundAxis(RotationRangeAroundAxis value);

	[UnionCase]
	public static partial RotationType CustomRotation(CustomRotation value);
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
