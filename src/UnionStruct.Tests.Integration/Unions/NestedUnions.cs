using System.Runtime.CompilerServices;

namespace UnionStruct.Tests.Integration.Unions;

[Union]
internal partial struct RootUnion
{
	public partial RootUnion(Empty value);

	[UnionCase(DisplayName = "Nested")]
	public partial RootUnion(NestedUnion value);
}

[Union]
internal partial struct NestedUnion
{
	public partial NestedUnion(Empty value);

	public partial NestedUnion(Node value);
}

internal readonly record struct Empty;

internal readonly record struct Node(int Value);
