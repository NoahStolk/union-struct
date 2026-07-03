# Plan: a new .NET 11-native union library (isolated from `UnionStruct`)

Status: draft design. Depends on C# 15 / .NET 11 (GA ~Nov 2026). Prerequisite reading:
`FINDINGS.md`.

## 0. Framing

Build a **second, standalone library** that emits types implementing the native
`[Union]` / `IUnion` feature with the non-boxing access pattern. It shares neither code
nor package identity with the existing `UnionStruct` generator; the old library stays
as-is for anyone on ≤ C# 14. The new one targets the language feature directly, which
lets us **delete a large fraction of what the current generator does** (pattern-match
plumbing, exhaustiveness analyzer, code fixes) because the compiler now provides it.

Working name (bikeshed later): **`NoahStolk.Unions`** (namespace `NoahStolk.Unions`,
attribute `[Union]`/`[UnionCase]` in that namespace to avoid clashing with the BCL
`System.Runtime.CompilerServices.UnionAttribute`).

## 1. Goals & non-goals

Goals:
- Zero-allocation construction **and** matching for value-type cases (verified pattern:
  typed-field storage + `HasValue` + `TryGetValue`).
- Native `switch`/`is` pattern matching and native exhaustiveness — no custom analyzer.
- Preserve the library's differentiators: `ref` access to payloads (public fields) and
  explicit `[FieldOffset]` overlap for unmanaged cases.
- Keep generating what the language does *not*: `IEquatable<T>`, `==`/`!=`,
  `GetHashCode`, `ToString`, and the UTF-8 / type-name helpers if still wanted.

Non-goals:
- Backward compatibility with the old generator's API. Clean break.
- Supporting pre-C# 15 compilers. That's what the old library is for.

## 2. Authoring model — pure type-union (chosen)

Cases are **types the user declares** (their own top-level `record struct`s). The
generator does **not** synthesize case types; it only fills in the union's storage,
constructors, non-boxing access pattern, and equality. This matches the native feature
one-to-one (a case *is* a type) and keeps the emission small.

The union declares its case set with a repeated generic marker; the generator emits the
constructors (so the compiler sees the case set), storage, and access members:

```csharp
public readonly record struct Angle(float Value);
public readonly record struct Position(Vector3 Value);
public readonly record struct Rotation(Quaternion Value);

[Union]                        // NoahStolk.Unions.UnionAttribute
[UnionCase<Angle>]             // repeated, AllowMultiple generic marker
[UnionCase<Position>]
[UnionCase<Rotation>]
public partial struct Transform;
```

Consumers write native, exhaustive, zero-alloc matches directly on the case types:

```csharp
float scalarish = t switch
{
    Angle a    => a.Value,
    Position p => p.Value.Length(),
    Rotation r => r.Value.W,
};   // no default arm; no US0001; no suppressor
```

Notes / consequences of this model:
- Multi-field cases are just record structs with several members (`record struct
  Segment(Vector3 Start, Vector3 End)`). Empty cases are `record struct None;`.
- **Distinct types required.** Two cases may not be the same type (the compiler reports
  semantic duplication). The old library's "two cases, same payload, different name" is
  expressed by declaring two distinct marker types.
- **Managed case types can't overlap.** `[FieldOffset]` overlap still applies only when
  every case type is unmanaged and the union is non-generic; otherwise sequential fields.

> Input-surface sub-decision (default, revisit in the spike): drive case discovery from
> repeated `[UnionCase<T>]` attributes on an otherwise empty `partial struct`. Alternative
> considered: user hand-writes `public partial Transform(Angle a);` constructors and the
> generator implements them — more explicit, more boilerplate. The attribute marker is
> the leaner default.

## 3. Generated union shape

For the `Transform` example, emit a `[System.Runtime.CompilerServices.Union]` struct
implementing `IUnion`, with:

- **Storage**: `[StructLayout(Explicit)]` + `[FieldOffset]` overlap **iff** all case
  payloads are unmanaged and the union is non-generic (reuse the existing
  `AllowMemoryOverlap` decision logic — it's the one piece of the old generator worth
  porting closely). Otherwise sequential typed fields.
- A `byte`/`int` tag field (keep `CaseIndex` + `CaseTag` — cheap, and the fastest
  hot-path match is still a jump-table `switch (u.CaseIndex)`).
- **Public payload fields** per case (enables `ref u.PositionData` mutation).
- One **public constructor per case type** — this is what registers the case set for the
  compiler. Optionally an `implicit operator` per case type for ergonomic construction.
- **`object Value`** — implement the IUnion member with a **non-nullable** return
  annotation (lazy-boxes only if explicitly read). Emitting `object?` gives `Value` a
  maybe-null state, which re-introduces the `CS8655` null-arm warning on otherwise
  exhaustive switches. Verified in `Samples/S07`.
- **Non-boxing access pattern**: `bool HasValue => true;` and one
  `bool TryGetValue(out <CaseType> value)` per case. `HasValue => true` pins `Value`'s
  null-state to non-null, which removes the spurious `CS8655` null-arm warning.
- **Equality / formatting** (language does not supply these): `IEquatable<Transform>`,
  `==`/`!=`, `GetHashCode`, `ToString`, and — if still wanted — `GetTypeString` /
  `GetTypeAsUtf8Span` / `NullTerminatedMemberNames`.

## 4. What gets deleted vs. the current generator (the simplification win)

| Current concern | Fate in the rewrite |
|---|---|
| `Switch(Action…)` / `Match<T>(Func…)` delegate methods | **Drop** (native `switch` replaces them; they also allocate delegates). Optionally keep an allocation-free `ref`-friendly overload if wanted. |
| `US0001` "missing union cases" analyzer | **Drop** — native exhaustiveness. |
| `CS8509`/`CS8524` `DiagnosticSuppressor` | **Drop** — no longer needed. |
| `UnionStruct.CodeFixes` project | **Drop** entirely. |
| `UnionStruct.Tests.Analyzers` | **Drop** entirely. |
| `CaseTag` / `CaseIndex` int-switch | **Keep** (optional fast path + pre-feature fallback). |
| `[FieldOffset]` overlap + `AllowMemoryOverlap` logic | **Keep / port** — still the memory win. |
| Equality / ToString / UTF-8 helpers | **Keep** — language doesn't provide them. |
| `___factoryReturnValue` / `TMatchOut` naming hacks | Mostly **gone** with delegate methods; keep the factory-local trick only where constructors need it. |

Net: the generator shrinks to *storage + case types + access pattern + equality*, and
two whole projects disappear.

## 5. Isolated project layout

Add under `src/`, referencing none of the existing `UnionStruct*` projects:

```
src/
  NoahStolk.Unions/                 # the incremental generator (netstandard2.0)
  NoahStolk.Unions.Attributes/      # [Union] / [UnionCase] marker attributes (netstandard2.0)
  NoahStolk.Unions.Package/         # packaging-only; new PackageId
  NoahStolk.Unions.Sample/          # net11.0 consumer
  NoahStolk.Unions.Tests/           # Verify snapshot tests of emission
  NoahStolk.Unions.Tests.Integration/  # net11.0 runtime tests (alloc asserts, ref, equality)
```

Decisions:
- **Own solution file** (`src/NoahStolk.Unions.slnx`) or add to the existing one — either
  is fine since code is isolated; a separate slnx keeps CI matrices clean.
- Reuse the same Verify + snapshot workflow (it's independent of the emitted content).
- Add an **allocation regression test** in the integration project asserting
  `GC.GetAllocatedBytesForCurrentThread()` stays 0 across construct + match — the whole
  point of the library, so pin it.
- The generator still targets `netstandard2.0`; the sample/tests target `net11.0`.

## 6. Coexistence & migration

- Both packages can be installed side by side (different PackageId, different namespaces).
- No auto-migration; the authoring surface is close enough that porting a union is a
  mechanical rename (`UnionStruct.[Union]` → `NoahStolk.Unions.[Union]`, adjust
  match sites from `.Match(...)` / `.Tag` switch to native type `switch`).
- Old library keeps shipping for consumers not yet on C# 15.

## 7. Open questions — resolve before implementing

1. **`ref` ergonomics.** `TryGetValue(out T)` yields a copy. Zero-copy mutation stays via
   public field + `ref u.Field` at the call site (a struct member cannot `ref`-return its
   own field — `CS8170`). Confirm this is acceptable, or whether an unsafe
   `ref`-accessor helper is worth it.
2. **Input surface (resolved: pure type-union, user-declared top-level types).** Remaining
   sub-decision: repeated `[UnionCase<T>]` markers (leaner) vs. user-written `partial`
   constructors (more explicit). Default to the markers; confirm in the spike.
3. **Do we still want `Switch`/`Match` at all?** They're convenient but allocate delegates.
   Consider dropping entirely, or offering only where a caller explicitly wants callback
   style.
4. **Preview churn.** The exact `HasValue`/`TryGetValue` overload-resolution rules
   (conversions, inheritance, writable `HasValue`) are still marked open in the spec.
   Re-verify emission against each preview until GA.
5. **Union member providers** (from the full proposal) aren't implemented yet and may
   offer an even simpler emission target — watch for it before finalizing the shape.

## 8. Phased checklist

1. ~~**Spike the target emission by hand**: hand-write the `Transform` union in the
   desired final shape, prove zero-alloc + native exhaustive match + `ref` + equality.~~
   **DONE — see `Samples/S07_GoldenEmission.cs`.** Validated: clean build (no `CS8509`,
   no `CS8655`), `sizeof == 20`, 0 bytes over 1M construct+match, `ref` mutation and
   value equality both work. This is the generator's golden output.
2. Scaffold the six isolated projects; port `AllowMemoryOverlap` and the model builders,
   stripping everything in §4's "Drop" rows.
3. Emit: case types → storage/layout → constructors → `Value`/`HasValue`/`TryGetValue`.
4. Emit: equality + `ToString` + optional UTF-8/type-name helpers.
5. Snapshot tests + allocation regression tests.
6. Package, sample, docs; validate against the latest .NET 11 preview.

**Immediate next step (agreed): prototype the generator's target emission for one sample
union by hand**, so we have the golden output before touching generator code.
