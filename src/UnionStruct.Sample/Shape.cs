using System.Numerics;
using System.Runtime.CompilerServices;

namespace UnionStruct.Sample;

/// <summary>Generic, and one case holds a string: the payloads are laid out sequentially.</summary>
[Union]
internal partial struct Shape<T>
	where T : INumber<T>
{
	public partial Shape(Circle<T> value);

	public partial Shape(Rectangle<T> value);

	[UnionCase(DisplayName = "Text label")]
	public partial Shape(Label value);
}

internal readonly record struct Circle<T>(T Radius);

internal readonly record struct Rectangle<T>(T Width, T Height);

internal readonly record struct Label(string Text);
