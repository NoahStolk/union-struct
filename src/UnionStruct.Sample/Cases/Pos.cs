using System.Numerics;

namespace UnionStruct.Sample.Cases;

internal record struct Pos(Vector3 Position)
{
    public Vector3 Position = Position;
}
