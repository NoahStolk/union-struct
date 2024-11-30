using UnionStruct.Sample.Cases;

namespace UnionStruct.Sample;

[Union(IncludeEmptyCase: true)]
[UnionCase<Pos>]
[UnionCase<PosRange>]
internal partial record struct TestUnion;
