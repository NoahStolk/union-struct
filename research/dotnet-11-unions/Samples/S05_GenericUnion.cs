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
// *** RC 1 BREAKING CHANGE — read this before matching a generic union. ***
//
// You may no longer declare a pattern VARIABLE in a union match when the union's
// own type is still open (contains type parameters) at the match site. This:
//
//     static string Show<T, E>(Result<T, E> r) => r switch
//     {
//         Ok<T> o  => $"Ok({o.Value})",     // error CS8780
//         Err<E> e => $"Err({e.Error})",    // error CS8780
//     };
//
// now fails with CS8780 ("A variable may not be declared within a 'not' or an 'or'
// pattern or a union matching involving matching against either the instance, or its
// underlying value"). It compiled cleanly on preview 4.
//
// The trigger is the UNION being open, not the case type: a generic union with
// entirely non-generic cases is rejected just the same, while a *closed* union
// (`Result<int, string>`) still binds variables fine — see Run() below.
//
// Two workarounds, both zero-alloc and both used here:
//   (a) type-only pattern + read the payload from a PUBLIC FIELD  <- see ShowViaField
//   (b) call TryGetValue explicitly                               <- see ShowViaTryGet
//
// (a) matters for this library specifically: public payload fields are already the
// generator's signature design decision, and they turn out to be the escape hatch
// that keeps generic unions ergonomic under the RC 1 rule.
public static class S05_GenericUnion
{
    public static void Run()
    {
        Console.WriteLine("[S05] Generic non-boxing union (Result<T,E>)");

        Result<int, string> ok = new(new Ok<int>(42));
        Result<int, string> err = new(new Err<string>("boom"));
        Console.WriteLine($"  via public field : {ShowViaField(ok)} / {ShowViaField(err)}");
        Console.WriteLine($"  via TryGetValue  : {ShowViaTryGet(ok)} / {ShowViaTryGet(err)}");

        // A CLOSED union type may still bind pattern variables — this is legal in RC 1.
        long before = GC.GetAllocatedBytesForCurrentThread();
        long acc = 0;
        for (int i = 0; i < 1_000_000; i++)
        {
            Result<int, string> r = new(new Ok<int>(i));
            acc += r switch { Ok<int> o => o.Value, Err<string> => -1 };
        }
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine($"  1,000,000 construct+match allocated {bytes:N0} bytes  (acc={acc})\n");
    }

    // Workaround (a): the pattern carries no variable, so CS8780 does not apply; the
    // payload comes from the public field. Zero copy, zero alloc.
    static string ShowViaField<T, E>(Result<T, E> r) => r switch
    {
        Ok<T> => $"Ok({r.OkData.Value})",
        Err<E> => $"Err({r.ErrData.Error})",
    };

    // Workaround (b): skip patterns entirely and call the non-boxing accessor. This
    // hands back a COPY of the payload (the field route above does not).
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

    // Public, so an open-generic match can read the payload without a pattern
    // variable (see ShowViaField). This mirrors the generator's public-field design.
    public Ok<T> OkData;
    public Err<E> ErrData;

    public Result(Ok<T> ok) { _isOk = true; OkData = ok; ErrData = default; }
    public Result(Err<E> err) { _isOk = false; OkData = default; ErrData = err; }

    public object Value => _isOk ? OkData : ErrData;
    public bool HasValue => true;
    public bool TryGetValue(out Ok<T> value) { value = OkData; return _isOk; }
    public bool TryGetValue(out Err<E> value) { value = ErrData; return !_isOk; }
}
