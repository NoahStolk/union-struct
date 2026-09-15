# Research: .NET 11 / C# 15 native unions

Standalone research spike (not part of `UnionStruct.slnx`, not built by CI) evaluating
the native `[Union]` / `IUnion` feature in .NET 11 against this library's
allocation-free goals.

Currently validated against **SDK `11.0.100-rc.1.26425.128`** (RC 1, 2026-09-08,
support phase `go-live`). Originally written against preview 4; see the
"What changed since preview 4" section of `FINDINGS.md` for the delta.

## Run it

Requires that exact .NET 11 SDK (pinned in the local `global.json` with
`rollForward: disable`, so a missing SDK fails loudly rather than silently floating
onto another build):

```bash
cd research/dotnet-11-unions
dotnet run -c Release
```

To move to a newer build, install it and bump the `version` in `global.json`, then
re-run — `S08` is written so that RC-1 behaviour changes surface as build errors.

## Contents

- **`FINDINGS.md`** — the write-up: the preview 4 → RC 1 delta, what ships in RC 1, the
  boxing trap, the non-boxing access pattern (with measured allocation numbers), a
  feature-by-feature comparison against this generator, and the recommendation.
- **`REWRITE-PLAN.md`** — design for a new, isolated .NET 11 library that emits the
  native union pattern. Details that RC 1 changed are marked **[RC 1]**.
- **`Samples/`** — eight annotated, runnable samples:
  - `S01` boxing union (the trap) · `S02` non-boxing union · `S03` explicit overlap
  - `S04` `ref` payload access · `S05` generic union · `S06` allocation bench
  - `S07` golden emission (the generator's target output)
  - `S08` preview 4 → RC 1 deltas (re-run this at RC 2 / GA)

## One-line conclusion

Native unions can be **fully allocation-free** only via the opt-in non-boxing access
pattern (`TryGetValue` per case) over typed-field storage — exactly the boilerplate a
source generator should emit. RC 1 does not change that, and its new `CS8780`
restriction on generic unions makes this library's public fields and `CaseIndex`
switch *more* valuable, not less. See `REWRITE-PLAN.md`.
