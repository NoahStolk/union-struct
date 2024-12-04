namespace UnionStruct.Tests.Integration.Unions;

[Union]
internal partial struct CompressedIndex
{
	[UnionCase]
	public static partial CompressedIndex Unsigned8(byte value);

	[UnionCase]
	public static partial CompressedIndex Unsigned16(ushort value);

	[UnionCase]
	public static partial CompressedIndex Unsigned32(uint value);
}
