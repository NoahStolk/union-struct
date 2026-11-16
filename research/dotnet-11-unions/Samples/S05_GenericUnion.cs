using System.Runtime.CompilerServices;

namespace Samples;

// SAMPLE 5 — Generic non-boxing union (Result<T, E>-style).
//
// The non-boxing pattern works with type parameters too, so the classic
// Result / Option shapes stay allocation-free. Note: explicit [FieldOffset] overlap
// (S03) is NOT available when a type parameter could be a managed reference — the
// generator falls back to sequential layout for generics for the same reason. Here
// we use sequential typed fields, which is still zero-alloc.
//
// Three ways to read a case out of an OPEN generic union (one whose own type still has
// type parameters at the match site, e.g. inside `Show<T, E>`). All three are zero-alloc:
//   (a) pattern VARIABLES, as on any other union          <- see Show
//   (b) type-only pattern + read the PUBLIC FIELD         <- see ShowViaField
//   (c) call TryGetValue explicitly                        <- see ShowViaTryGet
//
// (a) is the natural choice. (b) is the only one that does not copy the payload, which is
// why public payload fields remain the generator's signature design decision (and the
// route to `ref` mutation, S04). (c) suits code that wants no pattern syntax at all.
// (RC 1 briefly rejected (a) with CS8780; GA lifted that. See S08, delta 3.)
public static class S05_GenericUnion
{
    public static void Run()
    {
        Console.WriteLine("[S05] Generic non-boxing union (Result<T,E>)");

        Result<int, string> ok = new(new Ok<int>(42));
        Result<int, string> err = new(new Err<string>("boom"));
        Console.WriteLine($"  via variables    : {Show(ok)} / {Show(err)}");
        Console.WriteLine($"  via public field : {ShowViaField(ok)} / {ShowViaField(err)}");
        Console.WriteLine($"  via TryGetValue  : {ShowViaTryGet(ok)} / {ShowViaTryGet(err)}");

        // CLOSED union: pattern variables have bound fine on every build.
        long before = GC.GetAllocatedBytesForCurrentThread();
        long acc = 0;
        for (int i = 0; i < 1_000_000; i++)
        {
            Result<int, string> r = new(new Ok<int>(i));
            acc += r switch { Ok<int> o => o.Value, Err<string> => -1 };
        }
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine($"  1,000,000 construct+match allocated {bytes:N0} bytes  (acc={acc})  [closed union]");

        // OPEN union: the same match inside a generic method, binding a variable (a).
        // Warm the loop method first so tiered-JIT overhead is not counted (cf. S09).
        _ = OpenConstructAndMatch<int, string>(1000);
        before = GC.GetAllocatedBytesForCurrentThread();
        acc = OpenConstructAndMatch<int, string>(1_000_000);
        bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine($"  1,000,000 construct+match allocated {bytes:N0} bytes  (acc={acc})  [open union, variable arms]\n");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long OpenConstructAndMatch<T, E>(int n) where T : struct
    {
        long acc = 0;
        for (int i = 0; i < n; i++)
        {
            Result<T, E> r = new(new Ok<T>(default));
            acc += r switch { Ok<T> o => o.GetHashCode() + i, Err<E> => -1 };
        }

        return acc;
    }

    // (a): pattern variables on the open union.
    static string Show<T, E>(Result<T, E> r) => r switch
    {
        Ok<T> o  => $"Ok({o.Value})",
        Err<E> e => $"Err({e.Error})",
    };

    // (b): the pattern carries no variable and the payload comes from the public field.
    // Zero copy, zero alloc.
    static string ShowViaField<T, E>(Result<T, E> r) => r switch
    {
        Ok<T> => $"Ok({r.OkData.Value})",
        Err<E> => $"Err({r.ErrData.Error})",
    };

    // (c): skip patterns entirely and call the non-boxing accessor. This hands back a COPY
    // of the payload (the field route above does not).
    static string ShowViaTryGet<T, E>(Result<T, E> r)
    {
        if (r.TryGetValue(out Ok<T> o))
            return $"Ok({o.Value})";
        if (r.TryGetValue(out Err<E> e))
            return $"Err({e.Error})";
        return "<invalid>";
    }
}

public readonly record struct Ok<T>(T Value);
public readonly record struct Err<E>(E Error);

[Union]
public struct Result<T, E> : IUnion
{
    private readonly bool _isOk;

    // Public, so a match can read the payload in place without copying it (see
    // ShowViaField). This mirrors the generator's public-field design.
    public Ok<T> OkData;
    public Err<E> ErrData;

    public Result(Ok<T> ok) { _isOk = true; OkData = ok; ErrData = default; }
    public Result(Err<E> err) { _isOk = false; OkData = default; ErrData = err; }

    public object Value => _isOk ? OkData : ErrData;
    public bool HasValue => true;
    public bool TryGetValue(out Ok<T> value) { value = OkData; return _isOk; }
    public bool TryGetValue(out Err<E> value) { value = ErrData; return !_isOk; }
}
