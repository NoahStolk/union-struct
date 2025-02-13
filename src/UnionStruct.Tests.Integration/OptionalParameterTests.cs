using UnionStruct.Tests.Integration.Unions;
using Xunit;

namespace UnionStruct.Tests.Integration;

public sealed class OptionalParameterTests
{
	[Fact]
	public void OptionalParameter()
	{
		UnionWithOptionalParameters union = UnionWithOptionalParameters.Int();
		Assert.Equal(UnionWithOptionalParameters.IntIndex, union.CaseIndex);
		Assert.Equal(16, union.IntData);

		union = UnionWithOptionalParameters.Int(32);
		Assert.Equal(UnionWithOptionalParameters.IntIndex, union.CaseIndex);
		Assert.Equal(32, union.IntData);

		union = UnionWithOptionalParameters.Text();
		Assert.Equal(UnionWithOptionalParameters.TextIndex, union.CaseIndex);
		Assert.Equal("default", union.TextData);

		union = UnionWithOptionalParameters.Text("new");
		Assert.Equal(UnionWithOptionalParameters.TextIndex, union.CaseIndex);
		Assert.Equal("new", union.TextData);
	}
}
