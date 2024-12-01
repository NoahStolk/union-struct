﻿using System.Numerics;
using UnionStruct.Sample.Cases;

namespace UnionStruct.Sample;

[Union]
internal partial record struct TestUnion
{
	[UnionCase]
	public static partial TestUnion Empty();

	[UnionCase]
	public static partial TestUnion PositionCase(Position position);

	[UnionCase]
	public static partial TestUnion PositionRangeCase(PositionRange positionRange);

	[UnionCase]
	public static partial TestUnion MultiCase(Vector3 position, Vector3 velocity);
}
