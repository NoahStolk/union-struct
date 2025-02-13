using System.Numerics;

namespace UnionStruct.Sample.Cases;

internal record struct PositionRange(Vector3 ValueMin, Vector3 ValueMax)
{
	public Vector3 ValueMin = ValueMin;

	public Vector3 ValueMax = ValueMax;
}
