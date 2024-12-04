using System.Numerics;

namespace UnionStruct.Sample;

[Union]
internal partial struct Shape<T>
	where T : INumber<T>
{
	[UnionCase]
	public static partial Shape<T> Circle(T radius);

	[UnionCase]
	public static partial Shape<T> Rectangle(T width, T height);
}
