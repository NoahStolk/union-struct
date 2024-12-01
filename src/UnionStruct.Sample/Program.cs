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
WriteUnion(TestUnion.Pos(new Pos(new Vector3(1, 2, 3))));
WriteUnion(TestUnion.PosRange(new PosRange(new Vector3(1, 2, 3), new Vector3(4, 5, 6))));

void WriteUnion(TestUnion testUnion)
{
    Console.WriteLine(testUnion);
    foreach (byte b in Marshal(testUnion))
        Console.Write(b.ToString("X2", CultureInfo.InvariantCulture));

    string result = testUnion.Match(
        static _ => "Empty",
        static pos => $"Position: {pos.Position}",
        static posRange => $"PositionRange: {posRange.PositionMin} {posRange.PositionMax}");
    Console.WriteLine($" -> {result}");

    if (testUnion.IsPos)
    {
        Console.WriteLine($"This is a position: {testUnion.PosData.Position}");

        testUnion.PosData.Position = new Vector3(4, 5, 6);
        Console.WriteLine($"This is a position: {testUnion.PosData.Position}");
    }

    Console.WriteLine("---");
}

static unsafe ReadOnlySpan<byte> Marshal<T>(T value)
    where T : unmanaged
{
    return new ReadOnlySpan<byte>(Unsafe.AsPointer(ref value), sizeof(T));
}
