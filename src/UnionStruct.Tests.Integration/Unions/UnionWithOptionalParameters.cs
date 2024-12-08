namespace UnionStruct.Tests.Integration.Unions;

[Union]
internal partial struct UnionWithOptionalParameters
{
	[UnionCase]
	public static partial UnionWithOptionalParameters Int(int value = 16);

	[UnionCase]
	public static partial UnionWithOptionalParameters Text(string b = "default");
}
