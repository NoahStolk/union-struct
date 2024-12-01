using UnionStruct.Sample.Cases;

namespace UnionStruct.Sample;

[Union]
internal partial record struct TestUnion
{
	[UnionCase]
	public static partial TestUnion Empty();

	[UnionCase]
	public static partial TestUnion Pos(Pos pos);

	[UnionCase]
	public static partial TestUnion PosRange(PosRange posRange);
}
