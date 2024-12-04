namespace UnionStruct.Tests.Integration.Unions;

[Union]
internal readonly partial struct EnumLikeUnion
{
	[UnionCase]
	public static partial EnumLikeUnion Bronze();

	[UnionCase]
	public static partial EnumLikeUnion Silver();

	[UnionCase]
	public static partial EnumLikeUnion Gold();
}
