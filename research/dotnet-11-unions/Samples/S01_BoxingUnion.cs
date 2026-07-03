using System.Runtime.CompilerServices;

namespace Samples;

// SAMPLE 1 — The naive / canonical union (what the tooling generates by default).
//
// This is the shape shown in every C# 15 union intro: a single `object?` backing
// field. Cases are inferred from the single-parameter constructors. It is dead
// simple, but for value-type cases it BOXES on construction — 24 bytes on the heap
// per union value. For gamedev this is a non-starter.
//
// Measured: ~24 B/alloc per construction (see S06 bench).
public static class S01_BoxingUnion
{
    public static void Run()
    {
        Console.WriteLine("[S01] Boxing union (object? backing)");

        Shape s = new(new Circle(5.0));
        Console.WriteLine($"  area = {Area(s):F2}");

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) _ = new Shape(new Circle(i));
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine($"  1000 constructions allocated {bytes:N0} bytes  (~{bytes / 1000.0:F0} B each — BOXED)\n");
    }

    // Language switch works; lowered through `object? Value` → box already happened at ctor.
    static double Area(Shape shape) => shape switch
    {
        Circle c => Math.PI * c.Radius * c.Radius,
        Rectangle r => r.Width * r.Height,
        _ => 0, // needed: Value is `object?` (maybe-null), so compiler wants a null/default arm
    };
}

[Union]
public struct BoxingShape : IUnion
{
    private readonly object? _value;
    public BoxingShape(Circle value) => _value = value;   // struct -> object : BOX
    public BoxingShape(Rectangle value) => _value = value; // struct -> object : BOX
    public object? Value => _value;
}

// Kept named `Shape` for the sample above; same story.
[Union]
public struct Shape : IUnion
{
    private readonly object? _value;
    public Shape(Circle value) => _value = value;
    public Shape(Rectangle value) => _value = value;
    public object? Value => _value;
}

public readonly record struct Circle(double Radius);
public readonly record struct Rectangle(double Width, double Height);
