using System.Numerics;

namespace UnionStruct.Sample;

[Union]
internal partial record struct Shape<T>
	where T : struct, INumber<T>
{
	[UnionCase]
	public static partial Shape<T> Circle(T radius);

	[UnionCase]
	public static partial Shape<T> Rectangle(T width, T height);
}
