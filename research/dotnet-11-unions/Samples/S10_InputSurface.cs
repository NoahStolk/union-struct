using System.Numerics;
using System.Runtime.CompilerServices;
using UnionStruct;

namespace Samples;

// SAMPLE 10 — The generator's INPUT SURFACE, proven on a generic union.
//
// The rewrite is a pure type-union: the user declares the case types, the generator fills
// in everything else. The question this sample answers is HOW the user lists the cases.
//
// The obvious answer — a repeated generic marker, `[UnionCase<Circle<T>>]` — does not work
// for generic unions, because an attribute type argument may not mention a type parameter:
//
//     [UnionCase<Circle<T>>]
//     public partial struct Shape<T>;
//       error CS8968: 'T': an attribute type argument cannot use type parameters
//
// Generic unions (`Shape<T>`, `Result<T, E>`) are first-class in the current library, so
// that surface is out. What works instead — and is what this file proves — is C# 14
// PARTIAL CONSTRUCTORS. The user writes one constructor declaration per case; the generator
// writes the implementing half. The constructor parameter types are literally the case set
// the compiler matches against, so the declaration is the native union declaration.
//
// The user also writes the BCL `[Union]` (System.Runtime.CompilerServices) directly. The
// library therefore has no `[Union]` of its own, which avoids this ambiguity whenever a
// file imports both namespaces (very common: Unsafe, MethodImpl):
//
//     using System.Runtime.CompilerServices;
//     using UnionStruct;
//     [Union] public partial struct Thing;
//       error CS0104: 'Union' is an ambiguous reference between 'UnionStruct.UnionAttribute'
//                     and 'System.Runtime.CompilerServices.UnionAttribute'
//
// The only attribute the library still generates is the optional `[UnionCase]` for a
// display name. Both diagnostics above were built against SDK 11.0.100.
//
// Covered here: generic union, managed (string-carrying) case => sequential layout, open
// generic switch with variable arms, 0 B construct+match, `ref` mutation, equality,
// ToString, and the type-name / UTF-8 helpers.
public static class S10_InputSurface
{
    public static void Run()
    {
        Console.WriteLine("[S10] Input surface: partial constructors + BCL [Union] (generic, managed case)");

        Shape<float> circle = new Circle<float>(2f);
        Shape<float> rect = new Rectangle<float>(2f, 3f);
        Shape<float> label = new Label("hello");

        Console.WriteLine($"  match (open generic, variable arms): {Describe(circle)} | {Describe(rect)} | {Describe(label)}");
        Console.WriteLine($"  ToString: {circle} | {label}");
        Console.WriteLine($"  GetTypeString: {circle.GetTypeString()} / {rect.GetTypeString()} / {label.GetTypeString()}");
        Console.WriteLine($"  GetTypeAsUtf8Span(label) bytes: {label.GetTypeAsUtf8Span().Length}; NullTerminatedMemberNames bytes: {Shape<float>.NullTerminatedMemberNames.Length}");

        // `ref` straight into the union's storage. Case types are readonly records, so the
        // payload is replaced through the ref rather than mutated member-by-member.
        ref Circle<float> slot = ref circle.CircleData;
        slot = slot with { Radius = 9f };
        Console.WriteLine($"  after ref mutate: {circle}; equals new Circle(9): {circle == new Circle<float>(9f)}");

        _ = ConstructAndMatch<float>(1000);   // warm the whole loop method (cf. S09)
        long before = GC.GetAllocatedBytesForCurrentThread();
        float acc = ConstructAndMatch<float>(1_000_000);
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine($"  sizeof(Shape<float>) = {Unsafe.SizeOf<Shape<float>>()} bytes (sequential: managed case); 1M construct+match = {bytes:N0} bytes (acc={acc})\n");
    }

    static string Describe<T>(Shape<T> s) where T : INumber<T> => s switch
    {
        Circle<T> c => $"Circle(r={c.Radius})",
        Rectangle<T> r => $"Rectangle({r.Width}x{r.Height})",
        Label l => $"Label({l.Text})",
    };

    [MethodImpl(MethodImplOptions.NoInlining)]
    static T ConstructAndMatch<T>(int n) where T : INumber<T>
    {
        T acc = T.Zero;
        T one = T.One;
        for (int i = 0; i < n; i++)
        {
            Shape<T> s = (i & 1) == 0 ? new Circle<T>(one) : new Rectangle<T>(one, one);
            acc += s switch
            {
                Circle<T> c => c.Radius,
                Rectangle<T> r => r.Width * r.Height,
                Label => T.Zero,
            };
        }

        return acc;
    }
}

// ---- user-declared case types ----
public readonly record struct Circle<T>(T Radius);
public readonly record struct Rectangle<T>(T Width, T Height);
public readonly record struct Label(string Text);

// ---- USER HALF: this is everything the user writes ----
[Union]
public partial struct Shape<T> where T : INumber<T>
{
    public partial Shape(Circle<T> value);

    public partial Shape(Rectangle<T> value);

    [UnionCase(DisplayName = "Text label")]
    public partial Shape(Label value);
}

#region generated by UnionStruct (target)
public partial struct Shape<T> : IUnion, IEquatable<Shape<T>>
{
    public const int CaseCount = 3;
    public const int CircleIndex = 0;
    public const int RectangleIndex = 1;
    public const int LabelIndex = 2;

    // Read-only and public: the index feeds the type-name helpers and index-based UI (e.g. a
    // combo box over NullTerminatedMemberNames). Matching should use the native type switch.
    public readonly int CaseIndex;

    // A type parameter or a managed case rules out [FieldOffset] overlap => sequential.
    // Public so callers can take `ref` to a payload.
    public Circle<T> CircleData;
    public Rectangle<T> RectangleData;
    public Label LabelData;

    public partial Shape(Circle<T> value) { this = default; CaseIndex = CircleIndex; CircleData = value; }
    public partial Shape(Rectangle<T> value) { this = default; CaseIndex = RectangleIndex; RectangleData = value; }
    public partial Shape(Label value) { this = default; CaseIndex = LabelIndex; LabelData = value; }

    public static implicit operator Shape<T>(Circle<T> value) => new(value);
    public static implicit operator Shape<T>(Rectangle<T> value) => new(value);
    public static implicit operator Shape<T>(Label value) => new(value);

    // IUnion contract. Boxes only if a caller reads it; pattern matching uses TryGetValue.
    public readonly object Value => CaseIndex switch
    {
        CircleIndex => CircleData,
        RectangleIndex => RectangleData,
        _ => LabelData,
    };

    public readonly bool TryGetValue(out Circle<T> value) { value = CircleData; return CaseIndex == CircleIndex; }
    public readonly bool TryGetValue(out Rectangle<T> value) { value = RectangleData; return CaseIndex == RectangleIndex; }
    public readonly bool TryGetValue(out Label value) { value = LabelData; return CaseIndex == LabelIndex; }

    public readonly bool Equals(Shape<T> other) => CaseIndex == other.CaseIndex && CaseIndex switch
    {
        CircleIndex => EqualityComparer<Circle<T>>.Default.Equals(CircleData, other.CircleData),
        RectangleIndex => EqualityComparer<Rectangle<T>>.Default.Equals(RectangleData, other.RectangleData),
        LabelIndex => EqualityComparer<Label>.Default.Equals(LabelData, other.LabelData),
        _ => true,
    };

    public override readonly bool Equals(object? obj) => obj is Shape<T> other && Equals(other);

    public override readonly int GetHashCode() => CaseIndex switch
    {
        CircleIndex => HashCode.Combine(CaseIndex, CircleData),
        RectangleIndex => HashCode.Combine(CaseIndex, RectangleData),
        LabelIndex => HashCode.Combine(CaseIndex, LabelData),
        _ => CaseIndex,
    };

    public static bool operator ==(Shape<T> left, Shape<T> right) => left.Equals(right);
    public static bool operator !=(Shape<T> left, Shape<T> right) => !left.Equals(right);

    public override readonly string ToString() => CaseIndex switch
    {
        CircleIndex => $"Circle {{ Value = {CircleData} }}",
        RectangleIndex => $"Rectangle {{ Value = {RectangleData} }}",
        LabelIndex => $"Text label {{ Value = {LabelData} }}",
        _ => "<invalid>",
    };

    // Case names: the case type's name without type arguments, unless DisplayName overrides it.
    public static string GetTypeString(int caseIndex) => caseIndex switch
    {
        CircleIndex => "Circle",
        RectangleIndex => "Rectangle",
        LabelIndex => "Text label",
        _ => throw new System.Diagnostics.UnreachableException($"Invalid case index: {caseIndex}."),
    };

    public readonly string GetTypeString() => GetTypeString(CaseIndex);

    public static ReadOnlySpan<byte> GetTypeAsUtf8Span(int caseIndex) => caseIndex switch
    {
        CircleIndex => "Circle"u8,
        RectangleIndex => "Rectangle"u8,
        LabelIndex => "Text label"u8,
        _ => throw new System.Diagnostics.UnreachableException($"Invalid case index: {caseIndex}."),
    };

    public readonly ReadOnlySpan<byte> GetTypeAsUtf8Span() => GetTypeAsUtf8Span(CaseIndex);

    public static ReadOnlySpan<byte> NullTerminatedMemberNames => "Circle\0Rectangle\0Text label\0"u8;
}
#endregion
