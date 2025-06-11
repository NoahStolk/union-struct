namespace UnionStruct.Tests.Integration.Unions;

[Union]
internal partial struct CompressedIndex
{
	[UnionCase(DisplayName = "8-bit")]
	public static partial CompressedIndex Unsigned8(byte value);

	[UnionCase(DisplayName = "16-bit")]
	public static partial CompressedIndex Unsigned16(ushort value);

	[UnionCase(DisplayName = "32-bit")]
	public static partial CompressedIndex Unsigned32(uint value);
}
