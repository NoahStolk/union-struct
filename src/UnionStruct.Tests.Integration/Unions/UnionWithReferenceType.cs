using System.Runtime.CompilerServices;

namespace UnionStruct.Tests.Integration.Unions;

[Union]
internal partial struct UnionWithReferenceType
{
	public partial UnionWithReferenceType(int value);

	public partial UnionWithReferenceType(string value);
}
