# Plan: rewrite UnionStruct on top of .NET 11 native unions

Status: design, ready to implement. Prerequisite reading: `FINDINGS.md`.
Validated against the GA SDK `11.0.100`.

## 0. Framing

The next version of **`NoahStolk.UnionStruct`** is a rewrite **in place**:
- same repository, package ID and project names;
- it targets C# 15 / .NET 11 only;
- it emits the native `[Union]` pattern instead of a custom matching API.

This is a clean break, not a parallel library:

- The package has no known external consumers, and it is a `DevelopmentDependency`, so it
  never flows transitively.
- **`0.4.0` stays on NuGet** (and tagged in git) for code that is not on C# 15. It gets no
  further development.
- Pre-1.0 semver allows the break. The next version is **`0.5.0`**.

## 1. Goals and non-goals

Goals:
- Zero allocation on construction and matching. This is pinned by a regression test.
- Native `switch`/`is` and native exhaustiveness, with no custom analyzer.
- Keep the library's differentiators:
  - public payload fields, for `ref` access;
  - automatic `[FieldOffset]` overlap for unmanaged, non-generic unions.
- Keep what the language does not provide:
  - equality and `ToString`;
  - the type-name / UTF-8 helpers and `DisplayName`.

Non-goals:
- Source compatibility with 0.4. Every union declaration and every match site changes.
- C# ≤ 14 support.
- Synthesizing case types. Users declare them.

## 2. Authoring model: pure type-union, declared with partial constructors

Cases are **types the user declares**. The user writes the BCL `[Union]` and one **C# 14
partial constructor** per case. The generator writes everything else.

```csharp
using System.Numerics;
using System.Runtime.CompilerServices;   // BCL [Union]
using UnionStruct;                       // [UnionCase], generated into the compilation

public readonly record struct Circle<T>(T Radius);
public readonly record struct Rectangle<T>(T Width, T Height);

[Union]
public partial struct Shape<T> where T : INumber<T>
{
    public partial Shape(Circle<T> value);

    [UnionCase(DisplayName = "Rect")]
    public partial Shape(Rectangle<T> value);
}

// consumer
T Area<T>(Shape<T> s) where T : INumber<T> => s switch
{
    Circle<T> c    => c.Radius * c.Radius,
    Rectangle<T> r => r.Width * r.Height,
};   // exhaustive, no default arm, 0 B
```

Why this surface (all verified in `S10`):
- **Generic attribute markers can't express generic unions.** For example,
  `[UnionCase<Circle<T>>]` fails with `CS8968`: an attribute type argument cannot use type
  parameters.
- **Partial constructors are the native case set.** Only constructors register cases
  (`CS9385`). A partial constructor declaration is therefore the union declaration itself,
  and it works for generic unions.
- **The library ships no `[Union]` of its own.** A `UnionStruct.UnionAttribute` would be
  ambiguous with the BCL type (`CS0104`) in every file that imports
  `System.Runtime.CompilerServices`.

Consequences:
- **Multi-field cases** are record structs with several members.
- **Payload-less cases** are empty structs. Enum-like unions nest them:
  `public readonly struct Bronze;` plus `public partial Medal(Bronze _);`. That is two
  lines per member; the generator does the rest.
- **Case types must be distinct.** The 0.4 pattern of two named cases sharing one payload
  type (`Int(int)`, `Count(int)`) now needs a distinct type for each case.
- **Case names** come from the case type's name, without type arguments, unless
  `[UnionCase(Name = …)]` overrides them. A name collision (`Foo.Circle` and
  `Bar.Circle`) is a generator diagnostic that the user resolves with `Name`.
  (`DisplayName` only changes strings, so it cannot resolve a collision.)

## 3. Generated shape

The golden outputs are hand-written in `Samples/`:
- **`S07`** covers the overlap branch (all cases unmanaged, union non-generic).
- **`S10`** covers the sequential branch (generic union and/or managed case).

For each union, the generator emits:

- **Layout:** `[StructLayout(Explicit)]` with every payload at the same `[FieldOffset]`
  **iff** all case types are unmanaged and the struct has no type parameters. Otherwise it
  uses sequential layout. This is the existing `AllowMemoryOverlap` rule, carried over.
- **`: IUnion, IEquatable<TSelf>`**. Do not repeat `[Union]`: the user's attribute is the
  generator's trigger, and `UnionAttribute` is not `AllowMultiple`.
- **`CaseCount` and `<Case>Index` constants, plus `public readonly int CaseIndex`.** These
  feed the type-name helpers and index-based UI, and give the fastest match for large
  payloads in hot loops (`S11`). There is no `CaseTag` enum.
- **Public payload fields** named `<Case>Data`, one per case, for `ref` access.
- **The implementing half of each partial constructor**: `this = default`, then set the
  index and the field.
- **An implicit conversion from each case type.** This is a convenience only; it does not
  register a case.
- **`readonly object Value`**, typed `object` (non-nullable).
- **One `readonly bool TryGetValue(out TCase value)` per case.** No `HasValue`.
- **Equality:** `Equals(TSelf)`, `Equals(object?)`, `GetHashCode`, `==`, `!=`. The
  per-case comparison uses `EqualityComparer<TCase>.Default`, which is safe for generics
  and does not box.
- **`ToString`** (format: see open question 1).
- **Type-name helpers:**
  - `GetTypeString(int)` and `GetTypeString()`;
  - `GetTypeAsUtf8Span(int)` and `GetTypeAsUtf8Span()`;
  - `NullTerminatedMemberNames`.
- **Conventions from 0.4 that stay:** `global::`-qualified names everywhere,
  `#nullable enable`, `<auto-generated>`, and the hint name `{Struct}.g.cs` with `<>`
  replaced by `()`.

Generator diagnostics (new):
- the `[Union]` struct is not `partial`;
- a partial constructor has anything other than exactly one parameter;
- two cases have the same type;
- two cases resolve to the same name;
- the compilation is not C# 15, or `IUnion` is missing.

## 4. Keep / drop

| 0.4 concern | In 0.5 |
|---|---|
| `[Union]` (ours) + `[UnionCase] static partial` factory methods | **Replaced** by the BCL `[Union]` + partial constructors |
| `[UnionCase(DisplayName)]` | **Kept**, now targets constructors |
| `[GeneratedUnion]` marker | **Dropped**. Only the analyzer used it. |
| `Switch(Action…)` / `Match<T>(Func…)` | **Dropped**. They allocate delegates; use the native `switch`. |
| `Is<Case>` properties | **Dropped**: `u is Circle<T>` is native |
| `CaseTag` enum | **Dropped**. Match on types; the int path is `CaseIndex`. |
| `CaseIndex`, `<Case>Index`, `CaseCount` | **Kept**. Needed by the helpers and index-based UI. |
| US0001 analyzer, `CS8509`/`CS8524` suppressor | **Dropped**. Native exhaustiveness replaces them. |
| `UnionStruct.CodeFixes` project | **Dropped** |
| `UnionStruct.Tests.Analyzers` project | **Dropped** |
| Public payload fields | **Kept** |
| `AllowMemoryOverlap` + `[FieldOffset]` emission | **Kept** |
| Equality / `ToString` / type-name / UTF-8 helpers | **Kept** |
| `___factoryReturnValue` / `TMatchOut` naming hacks | **Gone**, along with factories and `Match` |
| Synthesized per-case `struct` for multi-parameter cases | **Gone**. The user's case type is the payload. |

Some `S11` numbers show the cost of losing `CaseTag`. A native switch matches as fast as
an int switch for small payloads. For 64 B payloads it is about 0.4 ns slower per match,
because it copies the payload. Hot loops can still use `CaseIndex` or a type-only pattern
plus a field read. The trade-off is that a `CaseIndex` switch is no longer checked for
exhaustiveness.

## 5. Project layout (in place, under `src/`)

```
UnionStruct/                        generator (netstandard2.0); Internals/Analyzers/ deleted
UnionStruct.Package/                packs only UnionStruct.dll into analyzers/dotnet/cs
UnionStruct.Sample/                 net11.0
UnionStruct.Tests/                  net11.0, Verify snapshots
UnionStruct.Tests.Integration/      net11.0, runtime tests + allocation regression test
UnionStruct.Tests.NuGetIntegration/ net11.0, still outside the slnx, still glob-compiles Integration
```

Deleted: `UnionStruct.CodeFixes/`, `UnionStruct.Tests.Analyzers/`.

Unchanged constraints from CLAUDE.md:
- The attributes are still **generated** through `RegisterPostInitializationOutput`. There
  is no attributes assembly and no `lib/` folder, because the package is analyzer-only.
- The generator still targets `netstandard2.0`.

Tooling:
- Root `global.json` → `11.0.100`. Choose `rollForward` deliberately: `latestMinor`
  silently moves to newer feature bands.
- `Microsoft.CodeAnalysis.CSharp` (currently `4.12.0`) must be raised for the **test**
  projects, whose compilations have to parse C# 14 partial constructors and C# 15 unions.
  The generator's own reference must not exceed the compiler in the oldest supported SDK
  (`11.0.100`).
- Snapshot workflow is unchanged (`./scripts/accept-all.sh src/UnionStruct.Tests/snapshots`).

## 6. Migrating a 0.4 union

1. Declare a case type for each case. Reuse existing payload records where possible:
   - for a multi-parameter case, write a record struct;
   - for a zero-arity case, write a nested empty struct.
2. Replace `[UnionStruct.Union]` with `[System.Runtime.CompilerServices.Union]`.
3. Replace each `[UnionCase] public static partial U Foo(T value);` with
   `public partial U(FooType value);`. Keep `DisplayName` if you had one.
4. Update construction: `U.Foo(x)` becomes `new U(new FooType(x))`, or rely on the
   implicit conversion.
5. Update matching: a `u.Tag switch { U.CaseTag.Foo => … }` or `Match(...)` becomes
   `u switch { FooType f => … }`. A `ref u.FooData` keeps working as long as the field
   name is unchanged.
6. Add `<WarningsAsErrors>CS8509</WarningsAsErrors>` so exhaustiveness is enforced.

## 7. Open questions

1. **`ToString` format.** `"{Name} {{ Value = {payload} }}"` nests awkwardly when the
   payload is a record: `Circle { Value = Circle { Radius = 2 } }`. Printing only the
   payload loses the case name for primitive case types. Pick one in the snapshot-test
   phase.
2. ✅ **Same-name case types.** A collision is an error (US0005) that points to
   `[UnionCase(Name = …)]`, which overrides the stem of the generated members.
   Disambiguating automatically was rejected: adding a second `Circle<T>` case would
   silently rename the first case's public fields and break `ref` call sites. Static
   factory methods (method name = case name, as in 0.4) were reconsidered and rejected:
   they don't register cases (`CS9385`), so the generator would have to emit the
   constructors, adding a second construction path and suggesting named cases where
   matching is by type.
3. **Struct-level `readonly`.** If the user declares `readonly partial struct`, `ref`
   mutation of payloads is impossible. Should the generator allow it (enum-like unions
   want it) or warn? Default: allow it, and make the fields `readonly`.

## 8. Phases

1. ✅ **Golden emission, hand-written and measured:** `S07` (overlap) and `S10` (sequential,
   generic, input surface). `S11` covers match cost.
2. ✅ Retarget to .NET 11, delete the two projects and `Internals/Analyzers/`, and strip the
   CodeFixes DLL from `UnionStruct.Package.csproj`.
3. ✅ Detection: `ForAttributeWithMetadataName("System.Runtime.CompilerServices.UnionAttribute")`,
   filtered to `partial struct`s with partial constructor declarations. Then shrink
   `AttributeSourceUtils` to `[UnionCase]`.
4. ✅ Model builders: build cases from partial constructors (case type, name, unmanaged-ness),
   keeping `AllowMemoryOverlap`.
5. ✅ Emission matching `S07`/`S10`, plus the new diagnostics (US0002–US0009).
6. ✅ Tests: rewritten snapshot tests, `Unions/` ported for integration, and the allocation
   regression test. Keep NuGetIntegration compiling.
7. ✅ Root README, CLAUDE.md, `Version` 0.5.0. Pack, and run the NuGetIntegration sequence
   from `.github/workflows/push.yml` (run locally; not yet tagged or published).

Phases 2–7 were implemented on branch `rewrite/dotnet-11-unions`. Open questions 1–3 were
implemented with their defaults (golden `ToString` format, collision is an error, `readonly`
structs get `readonly` fields); question 2 was later resolved with `[UnionCase(Name)]`
(US0010/US0011). Question 1 still needs a decision before release.
