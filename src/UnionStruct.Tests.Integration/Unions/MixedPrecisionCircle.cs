using System.Runtime.CompilerServices;

namespace UnionStruct.Tests.Integration.Unions;

/// <summary>
/// Two instantiations of one generic case type would both be named <c>Circle</c>; <c>[UnionCase(Name = ...)]</c> tells
/// their generated members apart. Not generic and all unmanaged, so the payloads overlap.
/// </summary>
[Union]
internal partial struct MixedPrecisionCircle
{
	[UnionCase(Name = "IntCircle")]
	public partial MixedPrecisionCircle(Circle<int> value);

	[UnionCase(Name = "FloatCircle", DisplayName = "Circle (float)")]
	public partial MixedPrecisionCircle(Circle<float> value);
}
