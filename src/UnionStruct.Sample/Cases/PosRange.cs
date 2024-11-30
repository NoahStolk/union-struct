using System.Numerics;

namespace UnionStruct.Sample.Cases;

internal record struct PosRange(Vector3 PositionMin, Vector3 PositionMax)
{
    public Vector3 PositionMin = PositionMin;

    public Vector3 PositionMax = PositionMax;
}
