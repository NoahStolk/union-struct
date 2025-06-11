namespace UnionStruct.Tests.Integration.Unions;

using UnionStruct;

[Union]
internal partial struct RootUnion
{
	[UnionCase]
	public static partial RootUnion Empty();

	[UnionCase(DisplayName = "Nested")]
	public static partial RootUnion NestedCase(NestedUnion value);
}

[Union]
internal partial struct NestedUnion
{
	[UnionCase]
	public static partial NestedUnion Empty();

	[UnionCase]
	public static partial NestedUnion Node(int value);
}
