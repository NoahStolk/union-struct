# .NET 11 unions vs. the `union-struct` source generator

Exploration of the C# 15 / .NET 11 union feature with the gamedev requirement in
focus: **no boxing, no heap allocation**. All numbers below were measured on this
machine with `11.0.100-preview.4.26230.115` via `GC.GetAllocatedBytesForCurrentThread()`
(see `Samples/` and run `dotnet run -c Release`).

> Preview note: this is a standalone research project (not part of `UnionStruct.slnx`
> and not built by CI). It carries its own `global.json` pinned to the exact preview
> build, because the repo root pins the .NET 10 SDK. Run `dotnet run -c Release` from
> `research/dotnet-11-unions/`.

## What actually ships in preview 4

- `System.Runtime.CompilerServices.UnionAttribute` (parameterless marker) and
  `System.Runtime.CompilerServices.IUnion` (single member: `object? Value { get; }`).
  Both are **in the BCL** in this build — no manual declaration needed (some early
  blog posts say otherwise; that was preview 2).
- A type is a union when it: carries `[Union]`, implements `IUnion`, and exposes **one
  single-parameter public constructor per case type**. The **constructor parameter
  types are the case set** — this is what the compiler pattern-matches against.
  (Factory methods do *not* register cases — verified: `CS8121`.)
- The compiler gives you **union pattern matching** (`x switch { Circle c => … }`) and
  **exhaustiveness**: once every case type is handled, no default arm is required and
  no `CS8509` is raised.
- The compiler does **not** synthesize the body. Omitting `Value` is a hard error
  (`CS0535`). You (or a generator) write the storage, `Value`, and — for performance —
  the non-boxing members. It's a low-level primitive, not codegen.

## The boxing trap (and the escape hatch)

The canonical form stores payloads in a single `object?`. Every value-type case is
**boxed on construction** — 24 B/value on the heap. Fatal for gamedev.

The spec defines an opt-in **non-boxing union access pattern**:

- `bool HasValue { get; }`
- one `bool TryGetValue(out TCase value)` per case type

When present, the compiler **preferentially lowers pattern matching through
`TryGetValue`** (strongly typed, never touches `object Value`). Store the payloads in
typed fields instead of `object`, and both construction and matching are allocation-free.

| Scenario (1,000,000 iters) | Alloc |
|---|---|
| Boxing union — construct + match | **24 B/iter** |
| Non-boxing union — construct + match | **0 B** |
| Non-boxing generic `Result<T,E>` — construct + match | **0 B** |
| Explicit `[FieldOffset]` overlap union | **0 B**, `sizeof == 8` |

So: **the .NET 11 union feature *can* be fully allocation-free** — but only if you
implement `HasValue` + `TryGetValue` and back it with typed fields. That boilerplate
is exactly a source generator's job.

## Feature-by-feature: `union-struct` generator vs. raw `[Union]`

| Capability | `union-struct` (yours) | Raw `[Union]` (preview 4) |
|---|---|---|
| Zero-alloc construction | ✅ public fields | ✅ *if* you hand-write typed-field storage |
| Zero-alloc matching | ✅ `CaseIndex`/`Tag` int switch | ✅ *if* you hand-write `HasValue`+`TryGetValue` |
| `ref` access to payload | ✅ public fields (core differentiator) | ⚠️ only via public field + `ref x.Field` at call site; **can't** `ref`-return from a struct member (`CS8170`) |
| Explicit `[FieldOffset]` overlap for unmanaged cases | ✅ automatic | ✅ but you write the offsets by hand |
| Sequential fallback for generic/managed | ✅ automatic | ✅ manual |
| `switch`/`is` pattern syntax | ❌ (you match on `Tag`/`CaseIndex`) | ✅ native, first-class |
| Compile-time exhaustiveness | ⚠️ custom analyzer `US0001` + `CS8509` suppressor | ✅ **native** — the whole analyzer+suppressor becomes unnecessary |
| `IEquatable`, `==`, `GetHashCode`, `ToString` | ✅ generated | ❌ your job |
| `GetTypeString` / UTF-8 member names | ✅ generated | ❌ your job |
| Ships today, stable | ✅ | ❌ preview; C# 15 GA ~Nov 2026 |

## Recommendation

**Don't replace the generator — evolve it to *emit* the `[Union]` pattern.** The
language feature and your generator are complementary, not competitors:

- Have the generator emit `[Union] : IUnion` with per-case constructors, typed-field
  (or `[FieldOffset]`-overlapped) storage, `Value`, `HasValue`, and `TryGetValue`.
- You then get **native `switch`/`is` syntax and native exhaustiveness for free** —
  which lets you **delete the `US0001` analyzer and the `CS8509`/`CS8524`
  suppressor** entirely, a real maintenance win.
- Keep the parts the language does *not* give you: **public fields for `ref`
  mutation** (your signature feature — still required, `ref`-returning members are
  illegal on structs), automatic overlap decisions, and the generated
  `IEquatable`/`==`/`GetHashCode`/`ToString`/type-name helpers.
- Keep emitting your `Tag`/`CaseIndex` int-switch too: it's the fastest match path
  (a jump table vs. a sequence of `TryGetValue` branches) for hot loops, and it's a
  clean fallback for anyone not yet on C# 15.

Net: the generator's declaration surface (`[UnionCase] static partial` methods) stays;
the emitted code gains language-union interop. Users opt into native pattern matching
where they want ergonomics, and drop to `ref`/`Tag` where they want raw control —
both allocation-free.

### Open questions / things to watch before committing

- **`ref` ergonomics.** `TryGetValue(out T)` returns a copy. Confirm your users are
  fine taking `ref u.PublicField` at the call site (works today) vs. wanting a
  `ref`-returning accessor (impossible on structs).
- **Preview churn.** Exact `HasValue`/`TryGetValue` matching rules (conversions,
  inheritance, read/write `HasValue`) are still marked open in the spec. Re-verify at
  each preview.
- **Union member providers** (from the full proposal) aren't implemented yet — may
  change the recommended emission shape.
- **`Value` nullability.** `HasValue => true` pins `Value`'s null-state to "not null",
  which is what removes the spurious `null`-arm warning (`CS8655`) on exhaustive
  switches. Emit it that way.

## Sources

- [Unions — C# feature specification (preview)](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/unions)
- [Explore union types in C# 15 — .NET Blog](https://devblogs.microsoft.com/dotnet/csharp-15-union-types/)
- [Andrew Lock — .NET 11 gets union types](https://andrewlock.net/exploring-the-dotnet-11-preview-2-dotnet-gets-union-types/)
- [Union type performance: non-boxing custom unions](https://zenn.dev/inuinu/articles/csharp-union-performance?locale=en)
- [C# TypeUnions proposal (csharplang)](https://github.com/dotnet/csharplang/blob/main/proposals/unions.md)
