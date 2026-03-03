using UnionStruct.Tests.Integration.Unions;
using Xunit;

namespace UnionStruct.Tests.Integration;

public sealed class TypeLoadTests
{
	[Fact]
	public void TestTypeLoad()
	{
		UnionWithStructContainingReferenceType union = UnionWithStructContainingReferenceType.Int(1);
		Assert.True(union.IsInt);
		Assert.False(union.IsString);
	}
}
