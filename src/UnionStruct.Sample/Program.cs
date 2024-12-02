using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using UnionStruct.Sample;
using UnionStruct.Sample.Cases;

unsafe
{
    Console.WriteLine(sizeof(TestUnion));
}

WriteUnion(TestUnion.Empty());
WriteUnion(TestUnion.PositionCase(new Position(new Vector3(1, 2, 3))));
WriteUnion(TestUnion.PositionRangeCase(new PositionRange(new Vector3(1, 2, 3), new Vector3(4, 5, 6))));
WriteUnion(TestUnion.MultiCase(new Vector3(1, 2, 3), new Vector3(4, 5, 6)));

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
