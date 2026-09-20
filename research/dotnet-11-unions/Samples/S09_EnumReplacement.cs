using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Samples;

// SAMPLE 9 — Can native unions replace ENUMS?
//
// The motivating want: a *closed* enum (the compiler proves every case is handled, and
// adding a case breaks every site that does not handle it) plus instance methods and
// properties declared directly on the type, without an extension block.
//
// Verdict, measured below:
//   * Closedness  — YES, and it is the one thing an enum fundamentally cannot do.
//   * Members     — YES, trivially; a union is just a partial struct.
//   * Cost        — one distinct TYPE per member, plus everything an enum gets for free
//                   from being a named integer (const, attribute args, case labels,
//                   numeric conversion, ordering, [Flags], the name table, JSON).
//
// Same doctrine as S08: everything here either compiles (so a regression breaks the
// build) or is commented out with the exact diagnostic RC 1 raises (so a relaxation
// shows up as "this compiles now"). Re-run at RC 2 / GA.
public static class S09_EnumReplacement
{
    const int N = 1_000_000;

    public static void Run()
    {
        Console.WriteLine("[S09] Enum replacement");

        // Instance members on the union itself — no extension type needed.
        Console.WriteLine($"  sizeof(Color)            : {Unsafe.SizeOf<Color>()} byte (tag only; payload-less cases need no storage)");
        Console.WriteLine($"  Blue.Hex                 : {Color.BlueValue.Hex}");
        Console.WriteLine($"  Green.Next()             : {Color.GreenValue.Next()}");

        // NOT fixed: `default` is still silently the first case, exactly as for an enum
        // whose first member is 0. Unions close the *cast* hole, not the *zero* hole.
        Console.WriteLine($"  default(Color)           : {default(Color)}  <-- silently the first case");

        // Equality/GetHashCode are hand-written (see the declaration) — the language
        // synthesizes nothing for a union.
        Dictionary<Color, int> byColor = new() { [Color.RedValue] = 1, [Color.BlueValue] = 3 };
        Console.WriteLine($"  usable as dictionary key : byColor[Blue] = {byColor[Color.BlueValue]}");

        // Zero-alloc construct + exhaustive match (numeric only, so no string alloc
        // pollutes the number — cf. S06). The whole loop METHOD is warmed, not just
        // Score: measuring its first call reports ~536 B of tiered-JIT/OSR overhead that
        // has nothing to do with the union. Steady state is exactly 0.
        _ = ConstructAndMatch();
        long before = GC.GetAllocatedBytesForCurrentThread();
        int acc = ConstructAndMatch();
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine($"  {N:N0} construct+match  : {bytes:N0} bytes  [sink={acc}]");

        // Reading IUnion.Value boxes even a payload-less case — so don't, and don't let
        // anything else (ToString on object, serializers, LINQ over object) reach it.
        Color blue = Color.BlueValue;
        long b0 = GC.GetAllocatedBytesForCurrentThread();
        object boxed = blue.Value;
        Console.WriteLine($"  reading .Value           : {GC.GetAllocatedBytesForCurrentThread() - b0} bytes -> {boxed.GetType().Name} (boxes; the non-boxing path is TryGetValue)");

        // What an enum gets from the BCL and a union does not.
        Console.WriteLine($"  JsonSerializer (enum)    : {JsonSerializer.Serialize(ClassicColor.Blue)}");
        Console.WriteLine($"  JsonSerializer (union)   : {JsonSerializer.Serialize(Color.BlueValue)}  <-- no public members to serialize");
        Console.WriteLine($"  Enum.GetValues(Color)    : {GetValuesOrThrow()}");

        // Grouping and negation over cases — the usual enum ergonomics survive.
        Console.WriteLine($"  or / not patterns        : IsWarm(Green)={IsWarm(Color.GreenValue)}, IsNotRed(Green)={IsNotRed(Color.GreenValue)}");
        Console.WriteLine($"  switch STATEMENT         : {DescribeStatement(Color.GreenValue)} (needs a trailing throw; CS0161 without it)");
        Console.WriteLine($"  enum + default arm       : {EnumWithDefault(ClassicColor.Blue)} (compiles clean, and silently swallows members added later)");

        // The case unions actually win outright: members that carry data.
        Console.WriteLine($"  mixed union              : {Render(new UiState(new Loaded(42)))} | {Render(new UiState(new Loading()))} | {Render(new UiState(new Failed("io")))}");
        Console.WriteLine();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ConstructAndMatch()
    {
        int acc = 0;
        for (int i = 0; i < N; i++)
        {
            Color c = (i % 3) switch { 0 => Color.RedValue, 1 => Color.GreenValue, _ => Color.BlueValue };
            acc += Score(c);
        }

        return acc;
    }

    static string GetValuesOrThrow()
    {
        try { return $"{Enum.GetValues(typeof(Color)).Length}"; }
        catch (ArgumentException) { return "ArgumentException — no name table, no GetValues/Parse/ToString(\"G\")"; }
    }

    // ---- (1) EXHAUSTIVENESS: the whole point -------------------------------------
    //
    // Covers every case, NO default arm, compiles clean. Type-only patterns; payload-less
    // cases have nothing to bind anyway, so CS8780 (S05) never comes up for enum-like
    // unions even when they are generic.
    static int Score(Color c) => c switch
    {
        Color.Red => 1,
        Color.Green => 2,
        Color.Blue => 3,
    };

    // Grouping and negation work, so the usual enum ergonomics survive.
    static bool IsWarm(Color c) => c is Color.Red or Color.Green;
    static bool IsNotRed(Color c) => c is not Color.Red;

    // MISSING an arm names the missing case:
    //   warning CS8509: The switch expression does not handle all possible values of its
    //   input type (it is not exhaustive). For example, the pattern 'Color.Blue' is not
    //   covered.
    //
    // static int ScoreMissing(Color c) => c switch
    // {
    //     Color.Red => 1,
    //     Color.Green => 2,
    // };
    //
    // NOTE it is a WARNING. For this to be enforcement rather than advice the consuming
    // project needs `<WarningsAsErrors>CS8509</WarningsAsErrors>` (or TreatWarningsAsErrors).

    // ---- (2) THE ENUM COMPARISON: why an enum can never be closed -----------------
    //
    // This enum switch covers every DECLARED member and still does not compile clean:
    //
    //   warning CS8524: The switch expression does not handle some values of its input
    //   type (it is not exhaustive) involving an unnamed enum value. For example, the
    //   pattern '(ClassicColor)3' is not covered.
    //
    // static string EnumAllMembers(ClassicColor c) => c switch
    // {
    //     ClassicColor.Red => "red",
    //     ClassicColor.Green => "green",
    //     ClassicColor.Blue => "blue",
    // };
    //
    // An enum is open by construction — `(ClassicColor)99` is legal and unchecked — so the
    // compiler can never let you drop the default arm. And that forced default arm is
    // exactly what silently swallows members added later: the version below compiles with
    // NO diagnostic at all, before or after a member is added. That is the failure mode
    // unions remove.
    static string EnumWithDefault(ClassicColor c) => c switch
    {
        ClassicColor.Red => "red",
        ClassicColor.Green => "green",
        ClassicColor.Blue => "blue",
        _ => "?",
    };

    // Verified by adding a 4th case to `Color`: EVERY match site is flagged by name,
    // including `Color.Hex` and `Color.ToString` inside the declaration itself —
    //   warning CS8509: ... For example, the pattern 'Color.Alpha' is not covered.
    // at each of them. That is the "adding a case breaks the build" property.

    // ---- (3) EXHAUSTIVENESS IS A SWITCH-*EXPRESSION* FEATURE ----------------------
    //
    // A switch STATEMENT gets no exhaustiveness: without the trailing throw this is
    //   error CS0161: not all code paths return a value.
    // Same as for enums, so it is parity rather than a regression — but it means the
    // guarantee only holds where you write switch expressions.
    static string DescribeStatement(Color c)
    {
        switch (c)
        {
            case Color.Red: return "red";
            case Color.Green: return "green";
            case Color.Blue: return "blue";
        }

        throw new InvalidOperationException();
    }

    // ---- (4) WHAT AN ENUM DOES THAT A UNION CANNOT --------------------------------
    //
    // Every one of these was compiled; the diagnostic is the RC 1 text.
    //
    //   const Color C = Color.RedValue;
    //     error CS0283: The type 'Color' cannot be declared const
    //     error CS0133: The expression being assigned to 'C' must be constant
    //
    //   [SomeAttribute(Color.RedValue)]
    //     error CS0181: Attribute constructor parameter 'c' has type 'Color', which is
    //                   not a valid attribute parameter type
    //
    //   c switch { Color.RedValue => 1, _ => 0 }        // a *constant* pattern
    //     error CS0150: A constant value is expected
    //     (only the TYPE pattern `Color.Red` works, never the value `Color.RedValue`)
    //
    //   (int)c
    //     error CS0030: Cannot convert type 'Color' to 'int'
    //
    //   a < b
    //     error CS0019: Operator '<' cannot be applied to operands of type 'Color' and 'Color'
    //     (so no ordering, and no [Flags]/bitwise combination either)
    //
    // Runtime-side, measured in Run(): Enum.GetValues/Parse/ToString("G") have no
    // equivalent, and System.Text.Json emits `{}` where the enum emits `2`.

    // ---- (5) THE CASE UNIONS WIN OUTRIGHT -----------------------------------------
    //
    // An enum cannot express "some members carry data". This can, and stays zero-alloc.
    static string Render(UiState s) => s switch
    {
        Loading => "spinner",
        Loaded x => $"data={x.Items}",
        Failed x => $"error={x.Message}",
    };
}

// ---- the enum-like union ---------------------------------------------------------
//
// The declaration cost: ONE DISTINCT TYPE PER MEMBER. The case set *is* the set of
// constructor parameter types (S08, delta 4), so two members cannot share a type and
// there is no way to declare a payload-less case more cheaply than an empty struct.
// Nesting them keeps the call sites reading like enum members (`Color.Red`).
[Union]
public readonly partial struct Color : IUnion, IEquatable<Color>
{
    public readonly struct Red;
    public readonly struct Green;
    public readonly struct Blue;

    private readonly byte _tag;

    public Color(Red _) => _tag = 0;
    public Color(Green _) => _tag = 1;
    public Color(Blue _) => _tag = 2;

    public object Value => _tag switch { 0 => default(Red), 1 => default(Green), _ => default(Blue) };

    public bool TryGetValue(out Red value) { value = default; return _tag == 0; }
    public bool TryGetValue(out Green value) { value = default; return _tag == 1; }
    public bool TryGetValue(out Blue value) { value = default; return _tag == 2; }

    // The members you would otherwise need an extension block for.
    public string Hex => this switch { Red => "#f00", Green => "#0f0", Blue => "#00f" };

    public Color Next() => _tag switch { 0 => new Color(default(Green)), 1 => new Color(default(Blue)), _ => new Color(default(Red)) };

    // "Enum member" access. NOT constants — see (4) above — just static properties.
    public static Color RedValue => new(default(Red));
    public static Color GreenValue => new(default(Green));
    public static Color BlueValue => new(default(Blue));

    // All hand-written; the language synthesizes none of this for a union.
    public bool Equals(Color other) => _tag == other._tag;
    public override bool Equals(object? obj) => obj is Color other && Equals(other);
    public override int GetHashCode() => _tag;
    public static bool operator ==(Color left, Color right) => left.Equals(right);
    public static bool operator !=(Color left, Color right) => !left.Equals(right);
    public override string ToString() => this switch { Red => "Red", Green => "Green", Blue => "Blue" };
}

// The enum the comparison is against.
public enum ClassicColor { Red, Green, Blue }

// ---- the mixed union (what an enum cannot express at all) -------------------------
public readonly record struct Loading;
public readonly record struct Loaded(int Items);
public readonly record struct Failed(string Message);

[Union]
public readonly partial struct UiState : IUnion
{
    private readonly byte _tag;
    private readonly int _items;
    private readonly string? _message;

    public UiState(Loading _) { _tag = 0; _items = 0; _message = null; }
    public UiState(Loaded value) { _tag = 1; _items = value.Items; _message = null; }
    public UiState(Failed value) { _tag = 2; _items = 0; _message = value.Message; }

    public object Value => _tag switch { 0 => default(Loading), 1 => new Loaded(_items), _ => new Failed(_message!) };

    public bool TryGetValue(out Loading value) { value = default; return _tag == 0; }
    public bool TryGetValue(out Loaded value) { value = new Loaded(_items); return _tag == 1; }
    public bool TryGetValue(out Failed value) { value = new Failed(_message!); return _tag == 2; }
}
