using System.Runtime.CompilerServices;

namespace Samples;

// SAMPLE 5 — Generic non-boxing union (Result<T, E>-style).
//
// The non-boxing pattern works with type parameters too, so the classic
// Result / Option shapes stay allocation-free. Note: explicit [FieldOffset] overlap
// (S03) is NOT available when a type parameter could be a managed reference — the
// generator falls back to sequential layout for generics for the same reason. Here
// we use sequential typed fields, which is still zero-alloc.
public static class S05_GenericUnion
{
    public static void Run()
    {
        Console.WriteLine("[S05] Generic non-boxing union (Result<T,E>)");

        Result<int, string> ok = new(new Ok<int>(42));
        Result<int, string> err = new(new Err<string>("boom"));
        Console.WriteLine($"  {Show(ok)} / {Show(err)}");

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

    static string Show<T, E>(Result<T, E> r) => r switch
    {
        Ok<T> o => $"Ok({o.Value})",
        Err<E> e => $"Err({e.Error})",
    };
}

public readonly record struct Ok<T>(T Value);
public readonly record struct Err<E>(E Error);

[Union]
public struct Result<T, E> : IUnion
{
    private readonly bool _isOk;
    private readonly Ok<T> _ok;
    private readonly Err<E> _err;

    public Result(Ok<T> ok) { _isOk = true; _ok = ok; _err = default; }
    public Result(Err<E> err) { _isOk = false; _ok = default; _err = err; }

    public object Value => _isOk ? _ok : _err;
    public bool HasValue => true;
    public bool TryGetValue(out Ok<T> value) { value = _ok; return _isOk; }
    public bool TryGetValue(out Err<E> value) { value = _err; return !_isOk; }
}
