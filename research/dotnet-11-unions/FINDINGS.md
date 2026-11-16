# .NET 11 unions vs. the UnionStruct source generator

An evaluation of C# 15 / .NET 11 native unions against this library's core requirement:
**no boxing, no heap allocation, `ref` access to case payloads**.

Every number and diagnostic below comes from the samples in `Samples/`, built and run
against the GA SDK **`11.0.100`** (runtime `11.0.0`, released 2026-11-10, STS).
Allocations are measured with `GC.GetAllocatedBytesForCurrentThread()`. To reproduce, run
`dotnet run -c Release` from this directory.

## Summary

- Native unions are **allocation-free only with the opt-in non-boxing pattern**: payloads
  in typed fields, plus one `TryGetValue(out TCase)` per case. The canonical
  `object`-backed union boxes every value-type case (24 B each).
- The compiler validates the case set and **gives native `switch`/`is` with
  exhaustiveness**, but it **synthesizes no bodies**. Storage, `Value`, `TryGetValue`,
  equality and `ToString` are still hand-written. That is a generator's job.
- `ref` access to a payload still requires **public fields**: a struct member cannot
  `ref`-return its own field.
- The native type switch is **as fast as an int switch for small payloads**. For a 64 B
  payload it costs about 0.4 ns more per match, because it copies the payload (`S11`).
- Native exhaustiveness makes the library's **US0001 analyzer, its `CS8509`/`CS8524`
  suppressor and its code fix unnecessary**.
- Cases are declared through **constructors only**. Generic attribute markers such as
  `[UnionCase<Circle<T>>]` cannot express generic unions (`CS8968`). **C# 14 partial
  constructors** can, so that is the generator's input surface (`S10`).

Conclusion: rewrite UnionStruct to **emit native unions**. See `REWRITE-PLAN.md`.

## What ships in .NET 11

- `System.Runtime.CompilerServices.UnionAttribute` and
  `System.Runtime.CompilerServices.IUnion`:
  - `UnionAttribute` is a parameterless marker for classes and structs; it is not
    multiple and not inherited.
  - `IUnion` has a single member, `object? Value { get; }`.
  - Both live in `System.Private.CoreLib`. They are the **entire** BCL surface: there is
    no `UnionCaseAttribute`, and no API for authoring "union member providers".
- C# 15 is the default language version for `net11.0`.
- A type is a union when it carries `[Union]`, has a public instance `Value` property of
  type `object` or `object?`, and declares **one public single-parameter constructor per
  case**. **The constructor parameter types are the case set.**
- The compiler validates the declaration:

  | Diagnostic | Meaning |
  |---|---|
  | `CS9385` | A union type must have at least one union creation member. Only **constructors** count: static factories and implicit operators do not. |
  | `CS9386` | `Value` is missing or has the wrong shape. The compiler keys on the property, not the `IUnion` interface. |

- Pattern matching over a union matches the **case types**, and an expression `switch`
  that covers every case needs no default arm.

## The boxing trap and the non-boxing pattern

The canonical union stores its payload in one `object` field, so every value-type case is
**boxed at construction** (`S01`). Store payloads in typed fields instead and add a
`bool TryGetValue(out TCase value)` per case. The compiler then lowers pattern matching
through `TryGetValue`, strongly typed, without ever touching `Value` (`S02`).
`HasValue` is part of the documented pattern but is **not required**: `TryGetValue` alone
gives the same 0 B (`S08`).

| Scenario (1,000,000 iterations) | Allocated |
|---|---|
| Boxing union: construct + match (`S06`) | **24 B/iter** |
| Non-boxing union: construct + match (`S02`, `S06`) | **0 B** |
| Generic `Result<T,E>`, closed and open (variable arms inside a generic method) (`S05`) | **0 B** |
| Golden emission: unmanaged cases, explicit overlap, `sizeof == 20` (`S07`) | **0 B** |
| Golden emission: generic union with a managed case, sequential layout, `sizeof == 24` (`S10`) | **0 B** |
| Enum-like union, payload-less cases, `sizeof == 1` (`S09`) | **0 B** |

Two related facts:
- Explicit `[FieldOffset]` overlap composes with the pattern: a three-width integer union
  is 8 bytes (`S03`).
- Reading `IUnion.Value` boxes, even for a payload-less case. Don't read it, and don't let
  anything else (`object`-typed APIs, serializers) read it either.

## `ref` access to payloads

`TryGetValue` hands back a **copy**. A struct member cannot `return ref` one of its own
instance fields (`CS8170`), so a `ref`-returning accessor is impossible. Zero-copy
mutation therefore still means a **public payload field**, with the caller taking
`ref u.PositionData` (`S04`, `S07`). The same field also gives the only copy-free read
inside a match: use a type-only pattern, then read the field (`S05` option b, `S11`).

## Declaring the case set (`S10`)

Since only constructors register cases, a generator needs some input it can turn into
constructors. Two candidate surfaces:

- **Generic attribute markers** (`[UnionCase<Circle>]` on the struct). Lean, but they
  break on generic unions:

  ```
  error CS8968: 'T': an attribute type argument cannot use type parameters
  ```

  `Shape<T>` and `Result<T, E>` are core shapes for this library, so this rules markers out.
- **C# 14 partial constructors.** The user writes `public partial Shape(Circle<T> value);`
  for each case, and the generator writes the implementing half. This works for generic
  unions, and the declaration *is* the native case set. `S10` builds it end to end: an
  open-generic switch with variable arms, 0 B, `ref` mutation, equality, and the
  type-name helpers.

The user should apply the **BCL** `[Union]` directly. If the library shipped its own
`[Union]`, any file that imports both namespaces would hit:

```
error CS0104: 'Union' is an ambiguous reference between 'UnionStruct.UnionAttribute'
and 'System.Runtime.CompilerServices.UnionAttribute'
```

Those files are common, because `System.Runtime.CompilerServices` brings in `Unsafe` and
`MethodImpl`.

## Match-path cost (`S11`)

A five-case union is matched three ways over a 1024-element array. Results are ns/match,
best of 5 rounds, across four runs, with the loops fully optimized:

| Payload | Native type switch | `CaseIndex` int switch + field | Type-only pattern + field |
|---|---|---|---|
| 4 B | 0.73 – 0.76 | 0.66 – 0.67 | 0.61 – 0.62 |
| 64 B | 1.03 – 1.06 | 0.65 – 0.66 | 0.79 – 0.95 |

- With small payloads, the `TryGetValue` chain is as fast as a jump table.
- With large payloads, the native switch pays for copying the payload into the pattern
  variable. A type-only pattern that reads the public field recovers most of that cost.
- `readonly` vs. non-readonly `TryGetValue` on `ref readonly` elements made no measurable
  difference, because the JIT removes the defensive copy. Emit `readonly` anyway: it is
  the correct contract.
- Methodology caveat: under default tiering, one run in three left a loop on its OSR code
  at about 5.5 ns. `S11` therefore uses `AggressiveOptimization`.

So the native type switch can be the default way to match. The int switch is not needed
for speed, except in hot loops over large payloads.

## Feature comparison

| Capability | UnionStruct 0.4 | Raw native union | Native union + rewritten generator |
|---|---|---|---|
| Zero-alloc construction and matching | ✅ | ⚠️ only with hand-written typed fields + `TryGetValue` | ✅ |
| `ref` access to payload | ✅ public fields | ⚠️ hand-written public fields | ✅ |
| `[FieldOffset]` overlap for unmanaged cases | ✅ automatic | ⚠️ hand-written offsets | ✅ automatic |
| `switch`/`is` on case types | ❌ `Tag`/`CaseIndex` or `Switch`/`Match` delegates | ✅ | ✅ |
| Compile-time exhaustiveness | ⚠️ custom `US0001` + suppressor + code fix | ✅ native | ✅ native |
| Generic unions with pattern variables | ✅ | ✅ | ✅ |
| Equality, `==`, `GetHashCode`, `ToString` | ✅ | ❌ hand-written | ✅ |
| `GetTypeString` / UTF-8 names / `DisplayName` | ✅ | ❌ | ✅ |
| Named cases sharing a payload type | ✅ (`Int(int)`, `Count(int)`) | ❌ one distinct type per case | ❌ (declare distinct types) |
| Zero-arity cases | ✅ `Bronze()` | ⚠️ one empty struct per case | ⚠️ one empty struct per case |
| Requires | C# 11 / .NET 7+ (emits `u8` literals, `UnreachableException`) | C# 15 / .NET 11 | C# 15 / .NET 11 |

## Can unions replace enums? (`S09`)

The answer differs for plain enums and for "enums" whose members carry data.

### Closedness is real, and it is the one thing an enum cannot do

Both switches below cover every declared member, and neither has a default arm:

| | Result |
|---|---|
| `enum ClassicColor { Red, Green, Blue }`, all 3 members matched | ⚠️ `CS8524`: *"involving an unnamed enum value. For example, the pattern `(ClassicColor)3` is not covered."* |
| `[Union] Color` with 3 cases, all 3 matched | ✅ clean |

An enum is open by construction: `(ClassicColor)99` is legal and unchecked. So the
compiler can never let you drop the default arm, and **that forced default arm is what
silently swallows members added later**. An enum switch with `_ => "?"` reports nothing,
before or after a member is added.

A union has no such hole. Adding a fourth case (`Alpha`) to `Color` flagged **every**
switch expression over it, naming the missing case each time:

```
warning CS8509: The switch expression does not handle all possible values of its input
type (it is not exhaustive). For example, the pattern 'Samples.Color.Alpha' is not covered.
```

Two caveats:
- It is a **warning**. Consuming projects need `<WarningsAsErrors>CS8509</WarningsAsErrors>`
  (or `TreatWarningsAsErrors`) to turn it into enforcement.
- It applies only to switch **expressions**. A switch *statement* that covers every case
  still needs a trailing `throw` (`CS0161`). That matches enums; it is not a regression.

### Members on the type: yes, trivially

A union is a partial struct, so properties and methods go straight on it. No extension
block is needed:

```csharp
public string Hex => this switch { Red => "#f00", Green => "#0f0", Blue => "#00f" };
public Color Next() => ...;
public static Color RedValue => new(default(Red));   // "enum member" access
```

### What you give up

You lose everything an enum gets from being a named integer:

| Enum capability | Union |
|---|---|
| `const Color c = ...` | ❌ `CS0283` / `CS0133` |
| `[MyAttr(Color.Red)]` | ❌ `CS0181`: not a valid attribute parameter type |
| `case Color.Red:` as a *constant* pattern | ❌ `CS0150`: only the **type** pattern works |
| `(int)c`, `a < b`, `[Flags]` | ❌ `CS0030` / `CS0019` |
| `Enum.GetValues` / `Parse` / `ToString("G")` | ❌ `ArgumentException` |
| `JsonSerializer.Serialize` | ❌ emits `{}` where the enum emits `2` |

Two more costs:
- **`default` is still the first case.** `default(Color)` silently *is* `Red`. Unions close
  the *cast* hole, not the *zero* hole.
- **One distinct type per member.** By hand that is about six lines per member. With the
  rewritten generator it drops to two: a nested `public readonly struct Red;` plus
  `public partial Color(Red _);`.

### Verdict

- **Plain enum** (a fixed set of names you switch on, serialize and persist): **not worth
  it.** You lose serialization, parsing and the name table to gain exhaustiveness.
- **Discriminated enum** (`Loading | Loaded(int) | Failed(string)`, zero-alloc in `S09`):
  **a clear win**, and something an enum cannot express at all.

## Recommendation

Rewrite UnionStruct **in place** as a .NET 11-only generator that emits native unions:

- **Input:**
  - the user's case types;
  - BCL `[Union]`;
  - one partial constructor per case;
  - optional `[UnionCase(DisplayName = …)]`.
- **Output:**
  - typed public payload fields, overlapped when every case is unmanaged and the union is
    non-generic;
  - a public read-only `CaseIndex`;
  - the constructor bodies and implicit conversions;
  - `Value` and a `readonly TryGetValue` per case;
  - equality and `ToString`;
  - the type-name / UTF-8 helpers.
- **Deleted:**
  - `Switch`/`Match` delegates and the static factory methods;
  - the `CaseTag` enum;
  - the US0001 analyzer, its suppressor and code fix, and their two projects.

Version 0.4.x stays on NuGet for code that is not on C# 15.

## Open questions

- **`ToString` format.** The case type is usually a record that already prints its own
  name, so the current `"{Name} {{ Value = {payload} }}"` format reads as
  `Circle { Value = Circle { Radius = 2 } }` (`S10`). One option is to print just the
  payload. But a primitive case such as `byte` would then print as a bare number, losing
  the case name. To decide during the rewrite.
- **Overload-resolution corner cases of `TryGetValue`** (conversions, inheritance between
  case types). Not exercised here; the library only emits exact-type overloads.
- **SDK churn.** Re-run `S08` on new SDKs. A regression breaks the build; a relaxation
  shows up as "this compiles now".

## Appendix: validation history

These findings were first written against `11.0.100-preview.4.26230.115`, re-verified on
RC 1 (`11.0.100-rc.1.26425.128`), and finalized on GA. `Samples/S08_Rc1Deltas.cs` holds
the deltas as compiled code:

1. **Pattern variables on open generic unions.**
   - Preview 4: allowed.
   - RC 1: rejected with `CS8780`, whenever the union type still had type parameters at
     the match site.
   - GA: allowed again, still at 0 B.
2. **Spurious `CS8655`.** Preview 4 warned "does not handle some null inputs" on an
   exhaustive switch over a union whose `Value` is `object?`. RC 1 removed the warning.
3. **Declaration-site validation.** Preview 4 reported only the generic `CS8121` at the
   match site. `CS9385`/`CS9386` arrived in RC 1.
4. **A corrected claim.** The preview 4 write-up called `HasValue => true` load-bearing,
   both for the non-boxing lowering and for suppressing `CS8655`. Both claims were false
   on every build: `TryGetValue` alone drives the lowering, and `Value`'s declared
   nullability drove `CS8655`.

Re-run tip: build declaration-site probes (`CS9385`/`CS9386`) separately from match-site
probes. Roslyn skips method-body diagnostics when there are declaration errors.

## Sources

- [Unions — C# feature specification](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/unions)
- [Explore union types in C# 15 — .NET Blog](https://devblogs.microsoft.com/dotnet/csharp-15-union-types/)
- [Andrew Lock — .NET 11 gets union types](https://andrewlock.net/exploring-the-dotnet-11-preview-2-dotnet-gets-union-types/)
- [Union type performance: non-boxing custom unions](https://zenn.dev/inuinu/articles/csharp-union-performance?locale=en)
- [C# unions proposal (csharplang)](https://github.com/dotnet/csharplang/blob/main/proposals/unions.md)
- [.NET 11 release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/11.0/releases.json)
