using System.Runtime.CompilerServices;

namespace UnionStruct.Tests.Integration.Unions;

/// <summary>Every case is unmanaged and the union is not generic, so the payloads overlap.</summary>
[Union]
internal partial struct CompressedIndex
{
	[UnionCase(DisplayName = "8-bit")]
	public partial CompressedIndex(byte value);

	[UnionCase(DisplayName = "16-bit")]
	public partial CompressedIndex(ushort value);

	[UnionCase(DisplayName = "32-bit")]
	public partial CompressedIndex(uint value);
}
