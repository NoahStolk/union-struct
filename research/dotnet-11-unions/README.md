# Research: .NET 11 / C# 15 native unions

Standalone research spike (not part of `UnionStruct.slnx`, not built by CI) evaluating
the native `[Union]` / `IUnion` feature in .NET 11 preview against this library's
allocation-free goals.

## Run it

Requires the .NET 11 preview SDK (pinned in the local `global.json`):

```bash
cd research/dotnet-11-unions
dotnet run -c Release
```

## Contents

- **`FINDINGS.md`** — the write-up: what ships in preview 4, the boxing trap, the
  non-boxing access pattern (with measured allocation numbers), a feature-by-feature
  comparison against this generator, and the recommendation.
- **`REWRITE-PLAN.md`** — design for a new, isolated .NET 11 library that emits the
  native union pattern.
- **`Samples/`** — six annotated, runnable samples:
  - `S01` boxing union (the trap) · `S02` non-boxing union · `S03` explicit overlap
  - `S04` `ref` payload access · `S05` generic union · `S06` allocation bench

## One-line conclusion

Native unions can be **fully allocation-free** only via the opt-in non-boxing access
pattern (`HasValue` + `TryGetValue`) over typed-field storage — exactly the boilerplate
a source generator should emit. See `REWRITE-PLAN.md`.
