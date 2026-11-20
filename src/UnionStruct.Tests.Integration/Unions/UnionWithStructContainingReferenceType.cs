using System.Runtime.CompilerServices;

namespace UnionStruct.Tests.Integration.Unions;

[Union]
internal partial struct UnionWithStructContainingReferenceType
{
	public partial UnionWithStructContainingReferenceType(int value);

	public partial UnionWithStructContainingReferenceType(StringStruct value);
}

internal readonly record struct StringStruct(string Value);
