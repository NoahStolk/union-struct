using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Samples;

// SAMPLE 3 — Non-boxing PLUS explicit memory overlap for unmanaged cases.
//
// When every case payload is unmanaged, you can lay the fields out with
// [StructLayout(Explicit)] + [FieldOffset] so all payloads share the same bytes —
// exactly what the union-struct generator does for unmanaged, non-generic unions.
// The union is then sizeof(largest case) + tag, not the sum of all cases.
//
// This composes cleanly with the non-boxing access pattern, so you get: minimal
// footprint, zero-alloc construction, and zero-alloc exhaustive matching.
public static class S03_ExplicitOverlap
{
    public static void Run()
    {
        Console.WriteLine("[S03] Explicit overlap + non-boxing (unmanaged cases)");

        var v = new VarIndex(1000u);
        Console.WriteLine($"  sizeof(VarIndex) = {Unsafe.SizeOf<VarIndex>()} bytes (payloads overlap; tag shares the struct)");
        Console.WriteLine($"  describe = {Describe(v)}");
    }

    static string Describe(VarIndex x) => x switch
    {
        byte u8 => $"u8:{u8}",
        ushort u16 => $"u16:{u16}",
        uint u32 => $"u32:{u32}",
    };
}

// A tagged union of three unsigned integer widths, all sharing offset 1.
[Union]
[StructLayout(LayoutKind.Explicit)]
public struct VarIndex : IUnion
{
    [FieldOffset(0)] private byte _tag;   // 0 = u8, 1 = u16, 2 = u32
    [FieldOffset(1)] public byte U8;
    [FieldOffset(1)] public ushort U16;
    [FieldOffset(1)] public uint U32;

    // `this = default` first so the compiler is satisfied all fields are assigned
    // (overlap means we can't touch each named field individually).
    public VarIndex(byte v) { this = default; _tag = 0; U8 = v; }
    public VarIndex(ushort v) { this = default; _tag = 1; U16 = v; }
    public VarIndex(uint v) { this = default; _tag = 2; U32 = v; }

    public object Value => _tag switch { 0 => U8, 1 => U16, _ => U32 };
    public bool HasValue => true;
    public bool TryGetValue(out byte value) { value = U8; return _tag == 0; }
    public bool TryGetValue(out ushort value) { value = U16; return _tag == 1; }
    public bool TryGetValue(out uint value) { value = U32; return _tag == 2; }
}
