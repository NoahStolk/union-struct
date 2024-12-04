namespace UnionStruct.Tests.Integration.Unions;

[Union]
public partial struct UnionWithReferenceType
{
	[UnionCase]
	public static partial UnionWithReferenceType Int(int value);

	[UnionCase]
	public static partial UnionWithReferenceType String(string value);
}
