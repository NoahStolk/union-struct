namespace UnionStruct.Sample;

[Union]
internal partial record struct ComplexUnion<T1, T2>
{
	[UnionCase]
	public static partial ComplexUnion<T1, T2> Int(int? value);

	[UnionCase]
	public static partial ComplexUnion<T1, T2> Long(long? value);

	[UnionCase]
	public static partial ComplexUnion<T1, T2> TCase(T1? value);

	[UnionCase]
	public static partial ComplexUnion<T1, T2> UCase(TestGeneric<byte, short>? value, T1 a, T2 b);

	[UnionCase]
	public static partial ComplexUnion<T1, T2> UCaseNested(TestGenericNested<byte, short>? value, T1 a, T2 b);

	internal record struct TestGenericNested<T3, T4>(T3 A, T4 B);
}

internal record struct TestGeneric<T3, T4>(T3 A, T4 B);
