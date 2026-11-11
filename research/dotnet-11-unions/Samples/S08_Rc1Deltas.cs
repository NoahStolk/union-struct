using System.Runtime.CompilerServices;

namespace Samples;

// SAMPLE 8 — What changed between preview 4, RC 1 and GA.
//
// This file exists to be RE-RUN on every new SDK. Everything below either compiles (so a
// regression breaks the build) or is commented out with the exact diagnostic the SDK
// raises (so a relaxation shows up as "this compiles now").
//
// Verified by building the same sources against
//   11.0.100-preview.4.26230.115,  11.0.100-rc.1.26425.128  and  11.0.100 (GA, 2026-11-10).
//
// GA vs. RC 1: exactly one change — the CS8780 restriction RC 1 added (DELTA 3) is gone.
// Deltas 1, 2 and 4 hold on GA unchanged, with identical diagnostic text.
public static class S08_Rc1Deltas
{
    public static void Run()
    {
        Console.WriteLine("[S08] preview 4 -> RC 1 -> GA deltas");
        Console.WriteLine($"  TryGetValue without HasValue        : match works -> {D1(new NoHasValue(new Circle(2)))}");
        Console.WriteLine($"  object? Value, no null arm          : no CS8655 -> {D2(new NullableValue(new Circle(3)))}");
        Console.WriteLine($"  open generic union, type-only arms  : {D3(new Boxed<int>(new Circle(4)))}");
        Console.WriteLine($"  open generic union, variable arms   : {D3Vars(new Boxed<int>(new Circle(4)))} / {D3Vars(new Boxed<string>(new Rectangle(5, 6)))}  (CS8780 on RC 1)");
        Console.WriteLine();
    }

    // DELTA 1 — `HasValue` is NOT required for the non-boxing lowering.
    //
    // FINDINGS previously described `HasValue` + `TryGetValue` as a two-part pattern.
    // Measurement says otherwise: `TryGetValue` alone already lowers non-boxing and
    // allocates 0 B, on preview 4, RC 1 *and* GA. The generator need not emit HasValue.
    static double D1(NoHasValue s) => s switch { Circle c => c.Radius, Rectangle r => r.Width };

    // DELTA 2 — the spurious CS8655 null-arm warning is GONE on union switches.
    //
    // On preview 4, declaring `Value` as `object?` produced
    //   warning CS8655: The switch expression does not handle some null inputs
    // on an otherwise-exhaustive union switch. RC 1 and GA no longer report it, so a union
    // may declare `object?` freely. (Note the previously documented cause was wrong:
    // `HasValue => true` never suppressed CS8655 — `Value`'s declared nullability did.)
    static double D2(NullableValue s) => s switch { Circle c => c.Radius, Rectangle r => r.Width };

    // DELTA 3 — pattern variables on an OPEN union: legal on preview 4, banned on RC 1,
    // legal again on GA.
    //
    // Type-only arms have compiled on every build:
    static string D3<T>(Boxed<T> b) => b switch { Circle => "circle", Rectangle => "rect" };

    // Variable arms on an open union. RC 1 rejected both arms with error CS8780:
    //   "A variable may not be declared within a 'not' or an 'or' pattern or a union
    //    matching involving matching against either the instance, or its underlying value."
    // GA (11.0.100) compiles it, binds the correct payload for each case, and still lowers
    // through TryGetValue: 0 B over 100k matches. It is compiled code now, not a comment,
    // so a return of the RC 1 restriction breaks the build.
    //
    // RC 1's trigger was the UNION being open, not the case type — `Boxed<T>`'s cases are
    // both non-generic and were still rejected. The generic-case variant (`Ok<T> o` on
    // `Result<T, E>`) is lifted the same way; see S05.
    static double D3Vars<T>(Boxed<T> b) => b switch
    {
        Circle c    => c.Radius,
        Rectangle r => r.Width,
    };

    // DELTA 4 — union declarations are validated at the DECLARATION site (RC 1, unchanged
    // on GA).
    //
    // Preview 4 only complained at the match site, with the generic
    //   error CS8121: An expression of type 'X' cannot be handled by a pattern of type 'Y'.
    // RC 1 added two dedicated, declaration-site diagnostics:
    //
    //   error CS9385: A union type must have at least one union creation member.
    //   error CS9386: A union member provider type must have an instance 'Value' property
    //                 of type 'object?' or 'object'. The property must have a public get
    //                 accessor.
    //
    // CS9385 confirms that only CONSTRUCTORS register cases. Static factory methods and
    // implicit conversion operators still do not — both of these are rejected:
    //
    // [Union] public struct ByFactory : IUnion            // CS9385
    // {
    //     public static ByFactory Create(Circle c) => default;
    //     public object Value => null!;
    // }
    //
    // [Union] public struct ByOperator : IUnion           // CS9385
    // {
    //     public static implicit operator ByOperator(Circle c) => default;
    //     public object Value => null!;
    // }
    //
    // CS9386 also fires on a `[Union]` type that does NOT implement IUnion, so the
    // `Value` property — not the interface — is what the compiler keys on. The body is
    // still never synthesized (omitting `Value` remains a hard error: CS9386 + CS0535),
    // which is why a generator is still the right tool.
    //
    // Terminology note: "union member provider" and "union creation member" appear in
    // shipped diagnostics. Preview 4's FINDINGS listed member providers as "not
    // implemented yet"; the provider VALIDATION rules are in RC 1 and GA, but GA ships no
    // BCL authoring surface for them (IUnion and UnionAttribute are still the only
    // union types in System.Runtime.CompilerServices).
    //
    // When re-running these: build declaration-site probes (CS9385/CS9386) SEPARATELY from
    // match-site probes (CS8780). Roslyn skips method-body diagnostics when there are
    // declaration errors, so a combined build hides CS8780 even on RC 1.
}

// ---- declarations backing the deltas above ----

[Union]
public struct NoHasValue : IUnion   // DELTA 1: no HasValue member at all
{
    private readonly byte _tag;
    private readonly Circle _circle;
    private readonly Rectangle _rectangle;

    public NoHasValue(Circle c) { _tag = 0; _circle = c; _rectangle = default; }
    public NoHasValue(Rectangle r) { _tag = 1; _circle = default; _rectangle = r; }

    public object Value => _tag == 0 ? _circle : _rectangle;
    public bool TryGetValue(out Circle value) { value = _circle; return _tag == 0; }
    public bool TryGetValue(out Rectangle value) { value = _rectangle; return _tag == 1; }
}

[Union]
public struct NullableValue : IUnion   // DELTA 2: object? Value, matched with no null arm
{
    private readonly byte _tag;
    private readonly Circle _circle;
    private readonly Rectangle _rectangle;

    public NullableValue(Circle c) { _tag = 0; _circle = c; _rectangle = default; }
    public NullableValue(Rectangle r) { _tag = 1; _circle = default; _rectangle = r; }

    public object? Value => _tag == 0 ? _circle : _rectangle;
    public bool TryGetValue(out Circle value) { value = _circle; return _tag == 0; }
    public bool TryGetValue(out Rectangle value) { value = _rectangle; return _tag == 1; }
}

[Union]
public struct Boxed<T> : IUnion   // DELTA 3: generic union, NON-generic case types
{
    private readonly byte _tag;
    private readonly Circle _circle;
    private readonly Rectangle _rectangle;

    public Boxed(Circle c) { _tag = 0; _circle = c; _rectangle = default; }
    public Boxed(Rectangle r) { _tag = 1; _circle = default; _rectangle = r; }

    public object Value => _tag == 0 ? _circle : _rectangle;
    public bool TryGetValue(out Circle value) { value = _circle; return _tag == 0; }
    public bool TryGetValue(out Rectangle value) { value = _rectangle; return _tag == 1; }
}
