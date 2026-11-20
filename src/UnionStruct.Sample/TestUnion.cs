using System.Numerics;
using System.Runtime.CompilerServices;
using UnionStruct.Sample.Cases;

namespace UnionStruct.Sample;

/// <summary>Every case is unmanaged and the union is not generic: the payloads overlap in memory.</summary>
[Union]
internal partial struct TestUnion
{
	public partial TestUnion(Empty value);

	public partial TestUnion(Position value);

	public partial TestUnion(PositionRange value);

	[UnionCase(DisplayName = "Position and velocity")]
	public partial TestUnion(Motion value);

	public partial TestUnion(int value);

	internal readonly record struct Empty;

	internal readonly record struct Motion(Vector3 Position, Vector3 Velocity);
}
