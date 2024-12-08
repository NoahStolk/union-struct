using FluentAssertions;
using UnionStruct.Tests.Integration.Unions;
using Xunit;

namespace UnionStruct.Tests.Integration;

public sealed class OptionalParameterTests
{
	[Fact]
	public void OptionalParameter()
	{
		UnionWithOptionalParameters union = UnionWithOptionalParameters.Int();
		union.CaseIndex.Should().Be(UnionWithOptionalParameters.IntIndex);
		union.IntData.Should().Be(16);

		union = UnionWithOptionalParameters.Int(32);
		union.CaseIndex.Should().Be(UnionWithOptionalParameters.IntIndex);
		union.IntData.Should().Be(32);

		union = UnionWithOptionalParameters.Text();
		union.CaseIndex.Should().Be(UnionWithOptionalParameters.TextIndex);
		union.TextData.Should().Be("default");

		union = UnionWithOptionalParameters.Text("new");
		union.CaseIndex.Should().Be(UnionWithOptionalParameters.TextIndex);
		union.TextData.Should().Be("new");
	}
}
