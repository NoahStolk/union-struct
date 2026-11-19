#pragma warning disable CA1303
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using UnionStruct.Sample;
using UnionStruct.Sample.Cases;

Console.WriteLine("\n=== Shape sample (sequential layout) ===");
Console.WriteLine($"Size of Shape<float>: {Unsafe.SizeOf<Shape<float>>()}");
WriteShape<float>(new Circle<float>(1.0f));
WriteShape<float>(new Rectangle<float>(2.0f, 3.0f));
WriteShape<float>(new Label("hello"));

Console.WriteLine("\n=== TestUnion sample (overlapped layout) ===");
WriteSizeOf<TestUnion>();
WriteUnion(default(TestUnion.Empty));
WriteUnion(new Position(new Vector3(1, 2, 3)));
WriteUnion(new PositionRange(new Vector3(1, 2, 3), new Vector3(4, 5, 6)));
WriteUnion(new TestUnion.Motion(new Vector3(1, 2, 3), new Vector3(4, 5, 6)));
WriteUnion(42);

Console.WriteLine("\n=== CaseIndex and member names ===");
for (int i = 0; i < TestUnion.CaseCount; i++)
	Console.WriteLine($"{i}: {TestUnion.GetTypeString(i)}");

static void WriteShape<T>(Shape<T> shape)
	where T : INumber<T>
{
	// Native, exhaustive switch: no default arm, no delegates, no allocation.
	string description = shape switch
	{
		Circle<T> c => $"Circle: radius {c.Radius}",
		Rectangle<T> r => $"Rectangle: width {r.Width} height {r.Height}",
		Label l => $"Label: {l.Text}",
	};

	Console.WriteLine(description);
	Console.WriteLine($"  ToString: {shape}");
}

static void WriteUnion(TestUnion testUnion)
{
	Console.WriteLine(testUnion);
	foreach (byte b in Marshal(testUnion))
		Console.Write(b.ToString("X2", CultureInfo.InvariantCulture));

	string result = testUnion switch
	{
		TestUnion.Empty => "Empty",
		Position pos => $"Position: {pos.Value}",
		PositionRange posRange => $"PositionRange: {posRange.ValueMin} {posRange.ValueMax}",
		TestUnion.Motion motion => $"Motion: {motion.Position} {motion.Velocity}",
		int value => $"Value: {value}",
	};
	Console.WriteLine($" -> {result}");

	if (testUnion is Position)
	{
		// The payload fields are public, so a case can be mutated in place.
		ref Position position = ref testUnion.PositionData;
		position.Value *= 2;
		Console.WriteLine($"Position now multiplied by two: {testUnion.PositionData.Value}");
	}

	Console.WriteLine("---");
}

static unsafe ReadOnlySpan<byte> Marshal<T>(T value)
	where T : unmanaged
{
	return new ReadOnlySpan<byte>(Unsafe.AsPointer(ref value), sizeof(T));
}

static unsafe void WriteSizeOf<T>()
	where T : unmanaged
{
	Console.WriteLine($"Size of {typeof(T).Name}: {sizeof(T)}");
}
