using System.Numerics;
using System.Runtime.CompilerServices;

namespace UnionStruct.Tests.Integration.Unions;

/// <summary>Generic, so the layout is sequential even though every case is unmanaged once closed.</summary>
[Union]
internal partial struct Shape<T>
	where T : INumber<T>
{
	public partial Shape(Circle<T> value);

	public partial Shape(Rectangle<T> value);
}

internal readonly record struct Circle<T>(T Radius);

internal readonly record struct Rectangle<T>(T Width, T Height);
