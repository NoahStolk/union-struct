using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using UnionStruct.Sample;
using UnionStruct.Sample.Cases;

unsafe
{
    Console.WriteLine(sizeof(TestUnion));
}

Shape<float> circle = Shape<float>.Circle(1.0f);
Shape<float> rectangle = Shape<float>.Rectangle(2.0f, 3.0f);

WriteShape(circle);
WriteShape(rectangle);

WriteUnion(TestUnion.Empty());
WriteUnion(TestUnion.PositionCase(new Position(new Vector3(1, 2, 3))));
WriteUnion(TestUnion.PositionRangeCase(new PositionRange(new Vector3(1, 2, 3), new Vector3(4, 5, 6))));
WriteUnion(TestUnion.MultiCase(new Vector3(1, 2, 3), new Vector3(4, 5, 6)));

ComplexUnion<int, long> test = ComplexUnion<int, long>.UCaseNested(new ComplexUnion<int, long>.TestGenericNested<byte, short>(1, 2), 3, 4);
Console.WriteLine(test);
Console.WriteLine(test.UCaseNestedData.Value.HasValue ? test.UCaseNestedData.Value.Value.A : 0);

void WriteShape<T>(Shape<T> shape)
	where T : INumber<T>
{
	shape.Switch(
		radius => Console.WriteLine($"Circle: {radius}"),
		(width, height) => Console.WriteLine($"Rectangle: {width} {height}"));

	if (shape.IsCircle)
		Console.WriteLine($"This is a circle: {shape.CircleData}");
}

void WriteUnion(TestUnion testUnion)
{
    Console.WriteLine(testUnion);
    foreach (byte b in Marshal(testUnion))
        Console.Write(b.ToString("X2", CultureInfo.InvariantCulture));

    string result = testUnion.Match(
        static () => "Empty",
        static pos => $"Position: {pos.Value}",
        static posRange => $"PositionRange: {posRange.ValueMin} {posRange.ValueMax}",
        static (position, velocity) => $"MultiCase: {position} {velocity}",
        static value => $"Value: {value}");
    Console.WriteLine($" -> {result}");

    if (testUnion.IsPositionCase)
    {
        Console.WriteLine($"This is a position: {testUnion.PositionCaseData.Value}");

        testUnion.PositionCaseData.Value *= 2;
        Console.WriteLine($"Position now multiplied by two: {testUnion.PositionCaseData.Value}");
    }

    testUnion.Switch(
	    static () => { },
	    static _ => { },
	    static _ => { },
	    static (position, velocity) => Console.WriteLine($"MultiCase: {position} {velocity}"),
	    static _ => { });

    Console.WriteLine("---");
}

static unsafe ReadOnlySpan<byte> Marshal<T>(T value)
    where T : unmanaged
{
    return new ReadOnlySpan<byte>(Unsafe.AsPointer(ref value), sizeof(T));
}
