using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Samples;

// SAMPLE 11 — Is the native type switch fast enough to replace the Tag/CaseIndex switch?
//
// The current library's hot-path advice is `u.Tag switch { ... }` (an int jump table) plus
// the US0001 analyzer for exhaustiveness. The rewrite wants to drop both in favour of the
// native type switch, which the compiler lowers to a chain of TryGetValue calls. This
// measures the three ways to match a 5-case union:
//
//   native   : `u switch { S0 x => x.V, ... }`        — TryGetValue chain, copies the payload
//   index    : `u.CaseIndex switch { 0 => u.S0Data.V }` — int switch + public field, no copy
//   typeonly : `u switch { S0 => u.S0Data.V, ... }`    — TryGetValue chain, field read, no copy
//
// for a small (4 B) and a large (64 B) payload, and — because the elements are read through
// `ref readonly` — with and without `readonly` on the generated TryGetValue. A non-readonly
// member called on a `ref readonly` receiver makes the compiler copy the WHOLE union first
// (a defensive copy), once per TryGetValue call.
//
// Stopwatch timing, best of 5 rounds, Release build; not BenchmarkDotNet. Treat the numbers
// as ratios on this machine, not absolutes. The loops are AggressiveOptimization: each is
// called only a handful of times, and under default tiering one run in three kept the
// "large, native" loop on its OSR code at ~5.5 ns instead of ~1.1 ns. Fully optimizing every
// loop up front makes the comparison between paths fair (at the cost of dynamic PGO).
//
// Measured on 11.0.100 (four runs, ns/match; ranges are across runs):
//
//   payload   native        index         typeonly
//   4 B       0.73 – 0.76   0.66 – 0.67   0.61 – 0.62
//   64 B      1.03 – 1.06   0.65 – 0.66   0.79 – 0.95
//
// * Small payloads: no meaningful difference — the TryGetValue chain is as fast as the
//   int switch.
// * Large payloads: the native switch pays for copying the payload into the pattern
//   variable (~1.6x the index path, ~0.4 ns absolute). A type-only pattern that reads the
//   public field recovers most of it.
// * `readonly` vs non-readonly TryGetValue: no measurable difference; the JIT removes the
//   defensive copy. Emit `readonly` anyway — it is the correct contract, not a speed-up.
//
// Conclusion: the native type switch can replace the Tag/CaseIndex switch as the default
// match. Keep `CaseIndex` public and read-only (the type-name helpers and index-based UI
// need it), so a hot loop over large payloads can still drop to it — without the
// exhaustiveness check that US0001 used to provide.
public static class S11_MatchPerf
{
    const int Count = 1024;          // elements, random case per element
    const int Repeats = 50_000;      // ~51M matches per measurement
    const int Rounds = 5;

    public static void Run()
    {
        Console.WriteLine("[S11] Match-path timing: native type switch vs CaseIndex switch (best of 5, ns/match)");

        Random rng = new(42);
        int[] cases = new int[Count];
        for (int i = 0; i < Count; i++) cases[i] = rng.Next(5);

        SmallU[] small = cases.Select(SmallU.Make).ToArray();
        LargeU[] large = cases.Select(LargeU.Make).ToArray();
        LargeMutU[] largeMut = cases.Select(LargeMutU.Make).ToArray();

        Console.WriteLine($"  sizeof: SmallU={Unsafe.SizeOf<SmallU>()} B, LargeU={Unsafe.SizeOf<LargeU>()} B");
        Report("small, native   ", () => SmallNative(small));
        Report("small, index    ", () => SmallIndex(small));
        Report("small, typeonly ", () => SmallTypeOnly(small));
        Report("large, native   ", () => LargeNative(large));
        Report("large, index    ", () => LargeIndex(large));
        Report("large, typeonly ", () => LargeTypeOnly(large));
        Report("large, native, non-readonly TryGetValue  ", () => LargeMutNative(largeMut));
        Report("large, typeonly, non-readonly TryGetValue", () => LargeMutTypeOnly(largeMut));
        Console.WriteLine();
    }

    static void Report(string label, Func<long> body)
    {
        _ = body();   // warm-up / tier-up
        double best = double.MaxValue;
        long sink = 0;
        for (int r = 0; r < Rounds; r++)
        {
            long t0 = Stopwatch.GetTimestamp();
            sink += body();
            double ns = Stopwatch.GetElapsedTime(t0).TotalNanoseconds / ((double)Count * Repeats);
            best = Math.Min(best, ns);
        }

        Console.WriteLine($"  {label}: {best,6:F2} ns   [sink={sink}]");
    }

    // ---- small payload ----
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static long SmallNative(SmallU[] items)
    {
        long acc = 0;
        for (int r = 0; r < Repeats; r++)
            foreach (ref readonly SmallU u in items.AsSpan())
                acc += u switch { S0 x => x.V, S1 x => x.V * 2, S2 x => x.V * 3, S3 x => x.V * 5, S4 x => x.V * 7 };
        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static long SmallIndex(SmallU[] items)
    {
        long acc = 0;
        for (int r = 0; r < Repeats; r++)
            foreach (ref readonly SmallU u in items.AsSpan())
                acc += u.CaseIndex switch { 0 => u.S0Data.V, 1 => u.S1Data.V * 2, 2 => u.S2Data.V * 3, 3 => u.S3Data.V * 5, _ => u.S4Data.V * 7 };
        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static long SmallTypeOnly(SmallU[] items)
    {
        long acc = 0;
        for (int r = 0; r < Repeats; r++)
            foreach (ref readonly SmallU u in items.AsSpan())
                acc += u switch { S0 => u.S0Data.V, S1 => u.S1Data.V * 2, S2 => u.S2Data.V * 3, S3 => u.S3Data.V * 5, S4 => u.S4Data.V * 7 };
        return acc;
    }

    // ---- large payload ----
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static long LargeNative(LargeU[] items)
    {
        long acc = 0;
        for (int r = 0; r < Repeats; r++)
            foreach (ref readonly LargeU u in items.AsSpan())
                acc += u switch { L0 x => x.B[0], L1 x => x.B[3], L2 x => x.B[7], L3 x => x.B[11], L4 x => x.B[15] };
        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static long LargeIndex(LargeU[] items)
    {
        long acc = 0;
        for (int r = 0; r < Repeats; r++)
            foreach (ref readonly LargeU u in items.AsSpan())
                acc += u.CaseIndex switch { 0 => u.L0Data.B[0], 1 => u.L1Data.B[3], 2 => u.L2Data.B[7], 3 => u.L3Data.B[11], _ => u.L4Data.B[15] };
        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static long LargeTypeOnly(LargeU[] items)
    {
        long acc = 0;
        for (int r = 0; r < Repeats; r++)
            foreach (ref readonly LargeU u in items.AsSpan())
                acc += u switch { L0 => u.L0Data.B[0], L1 => u.L1Data.B[3], L2 => u.L2Data.B[7], L3 => u.L3Data.B[11], L4 => u.L4Data.B[15] };
        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static long LargeMutNative(LargeMutU[] items)
    {
        long acc = 0;
        for (int r = 0; r < Repeats; r++)
            foreach (ref readonly LargeMutU u in items.AsSpan())
                acc += u switch { L0 x => x.B[0], L1 x => x.B[3], L2 x => x.B[7], L3 x => x.B[11], L4 x => x.B[15] };
        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static long LargeMutTypeOnly(LargeMutU[] items)
    {
        long acc = 0;
        for (int r = 0; r < Repeats; r++)
            foreach (ref readonly LargeMutU u in items.AsSpan())
                acc += u switch { L0 => u.L0Data.B[0], L1 => u.L1Data.B[3], L2 => u.L2Data.B[7], L3 => u.L3Data.B[11], L4 => u.L4Data.B[15] };
        return acc;
    }
}

// ---- case types ----
public readonly record struct S0(int V);
public readonly record struct S1(int V);
public readonly record struct S2(int V);
public readonly record struct S3(int V);
public readonly record struct S4(int V);

[InlineArray(16)]
public struct Block64 { private int _e0; }   // 64 B

public readonly record struct L0(Block64 B);
public readonly record struct L1(Block64 B);
public readonly record struct L2(Block64 B);
public readonly record struct L3(Block64 B);
public readonly record struct L4(Block64 B);

// ---- unions, in the generator's emission shape (explicit overlap, readonly accessors) ----
[Union]
[StructLayout(LayoutKind.Explicit)]
public struct SmallU : IUnion
{
    [FieldOffset(0)] public readonly int CaseIndex;
    [FieldOffset(4)] public S0 S0Data;
    [FieldOffset(4)] public S1 S1Data;
    [FieldOffset(4)] public S2 S2Data;
    [FieldOffset(4)] public S3 S3Data;
    [FieldOffset(4)] public S4 S4Data;

    public SmallU(S0 v) { this = default; CaseIndex = 0; S0Data = v; }
    public SmallU(S1 v) { this = default; CaseIndex = 1; S1Data = v; }
    public SmallU(S2 v) { this = default; CaseIndex = 2; S2Data = v; }
    public SmallU(S3 v) { this = default; CaseIndex = 3; S3Data = v; }
    public SmallU(S4 v) { this = default; CaseIndex = 4; S4Data = v; }

    public static SmallU Make(int c, int i) => c switch
    {
        0 => new SmallU(new S0(i)), 1 => new SmallU(new S1(i)), 2 => new SmallU(new S2(i)), 3 => new SmallU(new S3(i)), _ => new SmallU(new S4(i)),
    };

    public readonly object Value => CaseIndex switch { 0 => S0Data, 1 => S1Data, 2 => S2Data, 3 => S3Data, _ => S4Data };
    public readonly bool TryGetValue(out S0 value) { value = S0Data; return CaseIndex == 0; }
    public readonly bool TryGetValue(out S1 value) { value = S1Data; return CaseIndex == 1; }
    public readonly bool TryGetValue(out S2 value) { value = S2Data; return CaseIndex == 2; }
    public readonly bool TryGetValue(out S3 value) { value = S3Data; return CaseIndex == 3; }
    public readonly bool TryGetValue(out S4 value) { value = S4Data; return CaseIndex == 4; }
}

[Union]
[StructLayout(LayoutKind.Explicit)]
public struct LargeU : IUnion
{
    [FieldOffset(0)] public readonly int CaseIndex;
    [FieldOffset(4)] public L0 L0Data;
    [FieldOffset(4)] public L1 L1Data;
    [FieldOffset(4)] public L2 L2Data;
    [FieldOffset(4)] public L3 L3Data;
    [FieldOffset(4)] public L4 L4Data;

    public LargeU(L0 v) { this = default; CaseIndex = 0; L0Data = v; }
    public LargeU(L1 v) { this = default; CaseIndex = 1; L1Data = v; }
    public LargeU(L2 v) { this = default; CaseIndex = 2; L2Data = v; }
    public LargeU(L3 v) { this = default; CaseIndex = 3; L3Data = v; }
    public LargeU(L4 v) { this = default; CaseIndex = 4; L4Data = v; }

    public static LargeU Make(int c, int i)
    {
        Block64 b = default;
        for (int k = 0; k < 16; k++) b[k] = i + k;
        return c switch { 0 => new LargeU(new L0(b)), 1 => new LargeU(new L1(b)), 2 => new LargeU(new L2(b)), 3 => new LargeU(new L3(b)), _ => new LargeU(new L4(b)) };
    }

    public readonly object Value => CaseIndex switch { 0 => L0Data, 1 => L1Data, 2 => L2Data, 3 => L3Data, _ => L4Data };
    public readonly bool TryGetValue(out L0 value) { value = L0Data; return CaseIndex == 0; }
    public readonly bool TryGetValue(out L1 value) { value = L1Data; return CaseIndex == 1; }
    public readonly bool TryGetValue(out L2 value) { value = L2Data; return CaseIndex == 2; }
    public readonly bool TryGetValue(out L3 value) { value = L3Data; return CaseIndex == 3; }
    public readonly bool TryGetValue(out L4 value) { value = L4Data; return CaseIndex == 4; }
}

// Identical to LargeU except the accessors are NOT readonly — shows the defensive-copy cost.
[Union]
[StructLayout(LayoutKind.Explicit)]
public struct LargeMutU : IUnion
{
    [FieldOffset(0)] public readonly int CaseIndex;
    [FieldOffset(4)] public L0 L0Data;
    [FieldOffset(4)] public L1 L1Data;
    [FieldOffset(4)] public L2 L2Data;
    [FieldOffset(4)] public L3 L3Data;
    [FieldOffset(4)] public L4 L4Data;

    public LargeMutU(L0 v) { this = default; CaseIndex = 0; L0Data = v; }
    public LargeMutU(L1 v) { this = default; CaseIndex = 1; L1Data = v; }
    public LargeMutU(L2 v) { this = default; CaseIndex = 2; L2Data = v; }
    public LargeMutU(L3 v) { this = default; CaseIndex = 3; L3Data = v; }
    public LargeMutU(L4 v) { this = default; CaseIndex = 4; L4Data = v; }

    public static LargeMutU Make(int c, int i)
    {
        Block64 b = default;
        for (int k = 0; k < 16; k++) b[k] = i + k;
        return c switch { 0 => new LargeMutU(new L0(b)), 1 => new LargeMutU(new L1(b)), 2 => new LargeMutU(new L2(b)), 3 => new LargeMutU(new L3(b)), _ => new LargeMutU(new L4(b)) };
    }

    public object Value => CaseIndex switch { 0 => L0Data, 1 => L1Data, 2 => L2Data, 3 => L3Data, _ => L4Data };
    public bool TryGetValue(out L0 value) { value = L0Data; return CaseIndex == 0; }
    public bool TryGetValue(out L1 value) { value = L1Data; return CaseIndex == 1; }
    public bool TryGetValue(out L2 value) { value = L2Data; return CaseIndex == 2; }
    public bool TryGetValue(out L3 value) { value = L3Data; return CaseIndex == 3; }
    public bool TryGetValue(out L4 value) { value = L4Data; return CaseIndex == 4; }
}
