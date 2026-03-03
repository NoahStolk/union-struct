namespace UnionStruct.Tests.Integration.Unions;

[Union]
internal partial struct UnionWithStructContainingReferenceType
{
	[UnionCase]
	public static partial UnionWithStructContainingReferenceType Int(int value);

	[UnionCase]
	public static partial UnionWithStructContainingReferenceType String(StringStruct value);
}

internal record struct StringStruct(string Value);
