# .NET 11 unions vs. the `union-struct` source generator

Exploration of the C# 15 / .NET 11 union feature with the gamedev requirement in
focus: **no boxing, no heap allocation**. All numbers below were measured on this
machine with `11.0.100-rc.1.26425.128` via `GC.GetAllocatedBytesForCurrentThread()`
(see `Samples/` and run `dotnet run -c Release`).

> Preview note: this is a standalone research project (not part of `UnionStruct.slnx`
> and not built by CI). It carries its own `global.json` pinned to the exact SDK build
> with `rollForward: disable`, because the repo root pins the .NET 10 SDK. Run
> `dotnet run -c Release` from `research/dotnet-11-unions/`.
>
> The `disable` is deliberate. This file documents measured numbers against one exact
> build, and the original pin (`rollForward: latestMajor`) silently floated onto a newer
> SDK than the one named in the file — which is how the first round of these findings
> ended up describing a build it was not actually compiled with. If the pinned SDK is
> not installed the build now fails loudly instead.

## Status: RC 1 (`11.0.100-rc.1.26425.128`, 2026-09-08, support phase `go-live`)

Previously written against `11.0.100-preview.4.26230.115`. Previews 5, 6 and 7 landed
in between; every claim below was re-verified against RC 1, and where the two differ
the old claim is called out. GA is expected ~Nov 2026, so treat the RC-1 rules as close
to final but still re-runnable: `Samples/S08_Rc1Deltas.cs` exists to be re-run at RC 2
and GA.

### What changed since preview 4

Four changes, verified by building the *same* sources against both SDKs:

1. **Pattern variables are now banned when the union type is open.** ⚠️ **Breaking.**
   Binding a variable in a union match fails with `CS8780` if the union's own type still
   contains type parameters at the match site. This compiled cleanly on preview 4 and
   is the one change that broke this project's build (`S05`).

   ```csharp
   static string Show<T, E>(Result<T, E> r) => r switch
   {
       Ok<T> o  => $"Ok({o.Value})",     // error CS8780
       Err<E> e => $"Err({e.Error})",    // error CS8780
   };
   ```

   > `error CS8780: A variable may not be declared within a 'not' or an 'or' pattern or
   > a union matching involving matching against either the instance, or its underlying
   > value.`

   The trigger is the **union** being open, not the case type — a generic union whose
   cases are all non-generic is rejected just the same, while a *closed* union
   (`Result<int, string>`) still binds variables fine. Two zero-alloc workarounds, both
   in `S05`:
   - **type-only pattern + read the payload from a public field** — no variable, so the
     rule does not apply, and no copy either;
   - **call `TryGetValue` explicitly** — works, but hands back a copy.

   The first one is worth noting for this library specifically: public payload fields
   are already the generator's signature design decision, and they turn out to be the
   escape hatch that keeps generic unions ergonomic under the new rule.

2. **The spurious `CS8655` null-arm warning is gone.** On preview 4, a union declaring
   `object? Value` produced *"The switch expression does not handle some null inputs"*
   on an otherwise-exhaustive union switch, forcing a dead `_ =>` arm. RC 1 no longer
   reports it, so `Value` may be declared `object?` freely.

3. **Union declarations are validated at the declaration site.** Preview 4 only
   complained at the *match* site with the generic `CS8121` ("cannot be handled by a
   pattern of type X"). RC 1 adds two dedicated diagnostics on the declaration:

   | Diagnostic | Meaning |
   |---|---|
   | `CS9385` | A union type must have at least one union creation member. |
   | `CS9386` | A union member provider type must have an instance `Value` property of type `object?` or `object`, with a public get accessor. |

   `CS9385` re-confirms that **only constructors register cases** — static factory
   methods and implicit conversion operators still do not. `CS9386` also fires on a
   `[Union]` type that does *not* implement `IUnion`, so the compiler keys on the
   `Value` **property**, not the interface.

4. **"Union member providers" now exist in shipped diagnostics.** Preview 4's findings
   listed them as not implemented; the provider *validation* rules are in RC 1 (the
   `CS9386` wording). No new BCL surface appeared for them, though — see below.

### One earlier claim was simply wrong

The preview-4 write-up said `HasValue => true` "pins `Value`'s null-state to not null,
which is what removes the spurious `CS8655`". Both halves are false, and were false on
preview 4 too:

- **`HasValue` is not required for the non-boxing lowering.** A union carrying only
  `TryGetValue` allocates **0 B** on construct + match, on both SDKs.
- **`HasValue` never suppressed `CS8655`.** `Value`'s *declared nullability* did: on
  preview 4, `object? Value` warned even with `HasValue => true`, and `object Value`
  did not warn even with no `HasValue` at all.

So the non-boxing access pattern the generator must emit is **`TryGetValue` per case**.
`HasValue` is optional; the samples keep it only because it is cheap and part of the
documented pattern.

## What ships in RC 1

- `System.Runtime.CompilerServices.UnionAttribute` (parameterless marker,
  `AttributeTargets.Class | Struct`, not multiple, not inherited) and
  `System.Runtime.CompilerServices.IUnion` (single member: `object Value { get; }`).
  Both are in `System.Private.CoreLib` — no manual declaration needed.
- There is still **no `UnionCaseAttribute`** and no other union type in
  `System.Runtime.CompilerServices`; `IUnion` and `UnionAttribute` are the whole BCL
  surface. (Reflected over the RC 1 runtime directly.)
- A type is a union when it carries `[Union]`, exposes a `Value` property per `CS9386`,
  and declares **one single-parameter public constructor per case type**. The
  **constructor parameter types are the case set** — this is what the compiler
  pattern-matches against.
- The compiler gives you **union pattern matching** (`x switch { Circle c => … }`) and
  **exhaustiveness**: once every case type is handled, no default arm is required and
  no `CS8509` is raised.
- The compiler still does **not** synthesize the body. Omitting `Value` remains a hard
  error (`CS0535` + `CS9386`). You (or a generator) write the storage, `Value`, and —
  for performance — the non-boxing members. It is a low-level primitive, not codegen.
  **This is why a generator is still the right tool at GA.**

## The boxing trap (and the escape hatch)

The canonical form stores payloads in a single `object?`. Every value-type case is
**boxed on construction** — 24 B/value on the heap. Fatal for gamedev.

Store the payloads in **typed fields** and add one **`bool TryGetValue(out TCase value)`
per case type**, and the compiler preferentially lowers pattern matching through
`TryGetValue` — strongly typed, never touching `object Value`. Both construction and
matching are then allocation-free.

| Scenario (1,000,000 iters) | Alloc | vs. preview 4 |
|---|---|---|
| Boxing union — construct + match | **24 B/iter** | unchanged |
| Non-boxing (`TryGetValue` + `HasValue`) — construct + match | **0 B** | unchanged |
| Non-boxing (`TryGetValue` only, no `HasValue`) — construct + match | **0 B** | unchanged |
| Non-boxing with `object?` `Value` — construct + match | **0 B** | unchanged (warning gone) |
| Non-boxing generic `Result<T,E>` — construct + match | **0 B** | unchanged |
| Explicit `[FieldOffset]` overlap union | **0 B**, `sizeof == 8` | unchanged |
| Golden emission `Transform` (`S07`) | **0 B**, `sizeof == 20` | unchanged |
| Enum-like union, payload-less cases (`S09`) | **0 B**, `sizeof == 1` | new in this round |

So: **the .NET 11 union feature can be fully allocation-free** — but only if you
implement `TryGetValue` and back it with typed fields. That boilerplate is exactly a
source generator's job.

## Feature-by-feature: `union-struct` generator vs. raw `[Union]`

| Capability | `union-struct` (yours) | Raw `[Union]` (RC 1) |
|---|---|---|
| Zero-alloc construction | ✅ public fields | ✅ *if* you hand-write typed-field storage |
| Zero-alloc matching | ✅ `CaseIndex`/`Tag` int switch | ✅ *if* you hand-write `TryGetValue` |
| `ref` access to payload | ✅ public fields (core differentiator) | ⚠️ only via public field + `ref x.Field` at call site; **can't** `ref`-return from a struct member (`CS8170`) |
| Explicit `[FieldOffset]` overlap for unmanaged cases | ✅ automatic | ✅ but you write the offsets by hand |
| Sequential fallback for generic/managed | ✅ automatic | ✅ manual |
| `switch`/`is` pattern syntax | ❌ (you match on `Tag`/`CaseIndex`) | ✅ native, first-class |
| Pattern variables on a **generic** union | ✅ unaffected | ❌ **`CS8780`** — type-only patterns or `TryGetValue` only |
| Compile-time exhaustiveness | ⚠️ custom analyzer `US0001` + `CS8509` suppressor | ✅ **native** — the whole analyzer+suppressor becomes unnecessary |
| Declaration-site validation of the case set | ✅ your own diagnostics | ✅ new in RC 1 (`CS9385`/`CS9386`) |
| `IEquatable`, `==`, `GetHashCode`, `ToString` | ✅ generated | ❌ your job |
| `GetTypeString` / UTF-8 member names | ✅ generated | ❌ your job |
| Ships today, stable | ✅ | ⚠️ RC 1, `go-live` support; C# 15 GA ~Nov 2026 |

## Can they replace enums? (`S09`)

A separate question from "can they replace the generator", and the answer splits. Measured
on RC 1 in `Samples/S09_EnumReplacement.cs`.

### The closedness is real — and it is the one thing an enum cannot do

Both of these switches cover every declared member and neither has a default arm:

| | Result |
|---|---|
| `enum ClassicColor { Red, Green, Blue }`, all 3 members matched | ⚠️ `CS8524` — *"involving an unnamed enum value. For example, the pattern `(ClassicColor)3` is not covered."* |
| `[Union] Color` with 3 cases, all 3 matched | ✅ clean |

An enum is open by construction — `(ClassicColor)99` is legal and unchecked — so the
compiler can never let you drop the default arm, and **that forced default arm is exactly
what silently swallows members added later**. Verified: an enum switch with `_ => "?"`
reports nothing at all, before or after a member is added.

A union has no such hole. Adding a 4th case (`Alpha`) to `Color` flagged **every** switch
expression over it — all four, by name — including the two inside the union's own
declaration (`Color.Hex` and `Color.ToString`):

```
warning CS8509: The switch expression does not handle all possible values of its input
type (it is not exhaustive). For example, the pattern 'Color.Alpha' is not covered.
```

Two caveats on the guarantee:

- It is a **warning**. Consuming projects need `<WarningsAsErrors>CS8509</WarningsAsErrors>`
  (or `TreatWarningsAsErrors`) for it to be enforcement rather than advice.
- It only applies to switch **expressions**. A switch *statement* covering every case
  still fails with `CS0161: not all code paths return a value` and needs a trailing
  `throw`. That is parity with enums, not a regression, but it bounds where the guarantee
  holds.

### Members on the type: yes, trivially

A union is just a partial struct, so instance properties and methods go straight on it —
no extension block, no `static class ...Extensions`:

```csharp
public string Hex => this switch { Red => "#f00", Green => "#0f0", Blue => "#00f" };
public Color Next() => ...;
public static Color RedValue => new(default(Red));   // "enum member" access
```

`sizeof(Color) == 1` (payload-less cases need no storage beyond the tag) and 1M
construct+match allocates **0 B**.

### What you give up

Everything an enum gets from being a named integer. Each was compiled; diagnostics are the
RC 1 text:

| Enum capability | Union |
|---|---|
| `const Color c = ...` | ❌ `CS0283` / `CS0133` |
| `[MyAttr(Color.Red)]` | ❌ `CS0181` — not a valid attribute parameter type |
| `case Color.Red:` as a *constant* pattern | ❌ `CS0150` — only the **type** pattern `Color.Red` works, never the value |
| `(int)c`, `a < b`, `[Flags]` | ❌ `CS0030` / `CS0019` — no conversion, no ordering, no bitwise |
| `Enum.GetValues` / `Parse` / `ToString("G")` | ❌ `ArgumentException` — no name table, no equivalent |
| `JsonSerializer.Serialize` | ❌ emits `{}` where the enum emits `2` |
| `Equals` / `GetHashCode` / `ToString` | ❌ hand-written; the language synthesizes nothing |

Two more costs:

- **`default` is still the first case.** `default(Color)` silently *is* `Red`, exactly like
  an enum whose first member is 0. Unions close the *cast* hole, not the *zero* hole.
- **One distinct type per member.** The case set *is* the set of constructor parameter
  types, so two members cannot share a type and a payload-less case cannot be declared
  more cheaply than an empty struct. Roughly six lines per member (case type, ctor,
  `TryGetValue`, `Value` arm, static accessor, `ToString` arm) against one.

### Verdict

- **Plain enum** — a fixed set of names you switch on, serialize and persist: **not worth
  it.** You trade a one-line declaration for per-member boilerplate and lose serialization,
  parsing and the name table, to buy exhaustiveness you can largely approximate with
  `WarningsAsErrors` on a switch that already needs its default arm.
- **Discriminated enum** — where some members carry data (`Loading | Loaded(int) |
  Failed(string)`, also measured zero-alloc in `S09`): **clear win**, and the case an enum
  cannot express at all.

Note that the *current* generator already covers the enum-like shape with a much better
declaration surface than the native feature: `UnionStruct.Tests.Integration/Unions/EnumLikeUnion.cs`
declares `Bronze()/Silver()/Gold()` as zero-arity cases with no case types at all, and
`US0001` supplies the exhaustiveness. Closed enums are available on .NET 10 today. This is
a design input for `REWRITE-PLAN.md` — see its open question 7.

## Recommendation

**Unchanged by RC 1: don't replace the generator — evolve it to *emit* the `[Union]`
pattern.** The language feature and your generator are complementary, not competitors:

- Have the generator emit `[Union] : IUnion` with per-case constructors, typed-field
  (or `[FieldOffset]`-overlapped) storage, `Value`, and `TryGetValue`.
- You then get **native `switch`/`is` syntax and native exhaustiveness for free** —
  which lets you **delete the `US0001` analyzer and the `CS8509`/`CS8524`
  suppressor** entirely, a real maintenance win.
- Keep the parts the language does *not* give you: **public fields for `ref`
  mutation** (your signature feature — still required, `ref`-returning members are
  illegal on structs, and now *doubly* useful as the `CS8780` workaround for generic
  unions), automatic overlap decisions, and the generated
  `IEquatable`/`==`/`GetHashCode`/`ToString`/type-name helpers.
- Keep emitting your `Tag`/`CaseIndex` int-switch too: it's the fastest match path
  (a jump table vs. a sequence of `TryGetValue` branches) for hot loops, it's a
  clean fallback for anyone not yet on C# 15, **and it is the only ergonomic match
  path left for generic unions under `CS8780`**.

Net: the generator's declaration surface (`[UnionCase] static partial` methods) stays;
the emitted code gains language-union interop. Users opt into native pattern matching
where they want ergonomics, and drop to `ref`/`Tag` where they want raw control —
both allocation-free.

RC 1 moves the balance slightly **towards** keeping the generator: generic unions lost
pattern-variable ergonomics, which is precisely the gap `Tag`/`CaseIndex` + public
fields fills.

### Open questions / things to watch before committing

- **`CS8780` on generic unions.** Is the open-union restriction intended for GA, or a
  conservative RC-era guard? It is the one change that would reshape the emission plan
  if relaxed. Worth tracking upstream before finalizing.
- **`ref` ergonomics.** `TryGetValue(out T)` returns a copy. Confirm your users are
  fine taking `ref u.PublicField` at the call site (works today) vs. wanting a
  `ref`-returning accessor (impossible on structs).
- **Union member providers.** Validation wording is in RC 1 diagnostics but no BCL
  surface appeared. If the authoring half lands before GA it may offer a simpler
  emission target — re-check at RC 2.
- ~~**`Value` nullability.**~~ Resolved: no longer matters, `CS8655` is gone (delta 2).
- ~~**`HasValue` required?**~~ Resolved: it is not, and never was (see above).
- **Remaining preview churn.** Re-run `Samples/S08_Rc1Deltas.cs` at RC 2 and GA; it is
  written so that a regression breaks the build and a relaxation shows up as
  "this compiles now".

## Sources

- [Unions — C# feature specification (preview)](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/unions)
- [Explore union types in C# 15 — .NET Blog](https://devblogs.microsoft.com/dotnet/csharp-15-union-types/)
- [Andrew Lock — .NET 11 gets union types](https://andrewlock.net/exploring-the-dotnet-11-preview-2-dotnet-gets-union-types/)
- [Union type performance: non-boxing custom unions](https://zenn.dev/inuinu/articles/csharp-union-performance?locale=en)
- [C# TypeUnions proposal (csharplang)](https://github.com/dotnet/csharplang/blob/main/proposals/unions.md)
- [.NET 11 release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/11.0/releases.json) — the channel's current SDK/runtime build numbers.
