using System.Runtime.CompilerServices;

namespace Samples;

// SAMPLE 2 — The non-boxing access pattern. THIS is the one that matters for gamedev.
//
// The union spec defines an opt-in "non-boxing union access pattern":
//   * a `bool HasValue { get; }` property, and
//   * one `bool TryGetValue(out TCase value)` method per case type.
//
// When these are present, the compiler PREFERENTIALLY lowers pattern matching
// (switch / is / etc.) through TryGetValue — strongly typed, no trip through
// `object Value`, no box. Combined with storing the payload in typed fields (not
// `object`), construction AND matching are both zero-allocation.
//
// Measured: 0 B on construction, 0 B on the language `switch`. Exhaustive with NO
// default arm and NO null warning (because HasValue == true pins Value's null-state
// to "not null").
public static class S02_NonBoxingUnion
{
    public static void Run()
    {
        Console.WriteLine("[S02] Non-boxing union (HasValue + TryGetValue)");

        NbShape s = new(new Circle(5.0));
        Console.WriteLine($"  area = {Area(s):F2}");

        long before = GC.GetAllocatedBytesForCurrentThread();
        double acc = 0;
        for (int i = 0; i < 1_000_000; i++)
        {
            NbShape x = (i & 1) == 0 ? new NbShape(new Circle(i)) : new NbShape(new Rectangle(i, 2));
            acc += Area(x);
        }
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine($"  1,000,000 construct+match allocated {bytes:N0} bytes  (acc={acc:F0})\n");
    }

    // No `_ =>` arm: a union switch is exhaustive once every case type is handled.
    static double Area(NbShape shape) => shape switch
    {
        Circle c => Math.PI * c.Radius * c.Radius,
        Rectangle r => r.Width * r.Height,
    };
}

[Union]
public struct NbShape : IUnion
{
    private readonly byte _tag;          // 0 = circle, 1 = rectangle
    private readonly Circle _circle;
    private readonly Rectangle _rectangle;

    public NbShape(Circle c) { _tag = 0; _circle = c; }
    public NbShape(Rectangle r) { _tag = 1; _rectangle = r; }

    // IUnion contract. Boxes ONLY if a caller explicitly reads .Value; the language
    // pattern-matching path never touches it once TryGetValue exists.
    public object Value => _tag == 0 ? _circle : _rectangle;

    // ---- non-boxing access pattern ----
    public bool HasValue => true;
    public bool TryGetValue(out Circle value) { value = _circle; return _tag == 0; }
    public bool TryGetValue(out Rectangle value) { value = _rectangle; return _tag == 1; }
}
