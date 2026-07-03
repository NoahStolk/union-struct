using System.Runtime.CompilerServices;

namespace Samples;

// SAMPLE 6 — Clean allocation comparison (no string interpolation to pollute the numbers).
//
// Isolates exactly where the heap allocation comes from:
//   * boxing union     : boxes at construction (object? backing)
//   * non-boxing union : zero, both construction and language-switch matching
public static class S06_AllocationBench
{
    const int N = 1_000_000;

    public static void Run()
    {
        Console.WriteLine("[S06] Allocation bench (numeric only, no string alloc)");

        // Warm up JIT so first-call allocations don't skew the measurement.
        for (int i = 0; i < 1000; i++) { _ = BoxMatch(new Shape(new Circle(i))); _ = NbMatch(new NbShape(new Circle(i))); }

        Measure("  boxing:     construct only  ", () => { double a = 0; for (int i = 0; i < N; i++) { var s = new Shape(new Circle(i)); a += s.GetHashCode() & 1; } return a; });
        Measure("  boxing:     construct+match ", () => { double a = 0; for (int i = 0; i < N; i++) a += BoxMatch(new Shape(new Circle(i))); return a; });
        Measure("  non-boxing: construct only  ", () => { double a = 0; for (int i = 0; i < N; i++) { var s = new NbShape(new Circle(i)); a += s.HasValue ? 1 : 0; } return a; });
        Measure("  non-boxing: construct+match ", () => { double a = 0; for (int i = 0; i < N; i++) a += NbMatch(new NbShape(new Circle(i))); return a; });
        Console.WriteLine();
    }

    static double BoxMatch(Shape s) => s switch { Circle c => c.Radius, Rectangle r => r.Width, _ => 0 };
    static double NbMatch(NbShape s) => s switch { Circle c => c.Radius, Rectangle r => r.Width };

    static void Measure(string label, Func<double> body)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        double sink = body();
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine($"{label}: {bytes,12:N0} bytes  (~{bytes / (double)N,5:F1} B/iter)   [sink={sink:F0}]");
    }
}
