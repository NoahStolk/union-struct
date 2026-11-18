using System.Runtime.CompilerServices;

namespace UnionStruct.Tests.Integration.Unions;

[Union]
internal readonly partial struct EnumLikeUnion
{
	public partial EnumLikeUnion(Bronze _);

	public partial EnumLikeUnion(Silver _);

	public partial EnumLikeUnion(Gold _);

	public readonly record struct Bronze;

	public readonly record struct Silver;

	public readonly record struct Gold;
}
