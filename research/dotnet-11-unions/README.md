# Research: .NET 11 / C# 15 native unions

A standalone spike that evaluates C# 15's native unions (`[Union]` / `IUnion`) against
UnionStruct's goals: no boxing, no heap allocation, and `ref` access to payloads. It also
pins down what the rewritten generator should emit. It is not part of `UnionStruct.slnx`
and not built by CI.

## Run it

```bash
cd research/dotnet-11-unions
dotnet run -c Release
```

This needs SDK `11.0.100` exactly. The local `global.json` pins it with
`rollForward: disable`, so a missing SDK fails loudly instead of silently building with a
different one. To validate a newer SDK, install it, bump `version`, and re-run. `S08` is
written so that behaviour changes surface as build errors.

## Documents

- **`FINDINGS.md`** covers:
  - what .NET 11 ships;
  - the boxing trap and the non-boxing pattern, with measured allocations;
  - `ref` access and how cases are declared;
  - match-path cost;
  - a feature comparison with UnionStruct 0.4;
  - whether unions can replace enums;
  - the recommendation.
- **`REWRITE-PLAN.md`** is the design for UnionStruct 0.5: an in-place, .NET 11-only
  rewrite that emits native unions.

## Samples

| | Shows |
|---|---|
| `S01_BoxingUnion` | The canonical `object`-backed union boxes every value-type case (24 B). |
| `S02_NonBoxingUnion` | Typed fields + `TryGetValue` per case: 0 B construct + match. |
| `S03_ExplicitOverlap` | `[FieldOffset]` overlap composes with the non-boxing pattern. |
| `S04_RefAccess` | `ref` to a payload needs a public field (`CS8170` forbids `ref`-returning it). |
| `S05_GenericUnion` | `Result<T,E>`: three ways to read a case from an open generic union, all 0 B. |
| `S06_AllocationBench` | Boxing vs. non-boxing allocations, isolated. |
| `S07_GoldenEmission` | **Generator target**, unmanaged cases with explicit overlap. |
| `S08_Rc1Deltas` | Preview 4 → RC 1 → GA behaviour changes, as re-runnable compiled code. |
| `S09_EnumReplacement` | What a union gains and loses as an enum replacement. |
| `S10_InputSurface` | **Generator input surface**: partial constructors + BCL `[Union]`, plus the target for generic/managed unions. |
| `S11_MatchPerf` | Native type switch vs. `CaseIndex` switch vs. type-only pattern + field read. |

`GeneratedAttributes.cs` stands in for the `[UnionCase]` attribute that the generator
emits.

## Conclusion

Native unions are allocation-free only with typed-field storage and a `TryGetValue` per
case, and the compiler writes none of that. Generating it is exactly what this library is
for. UnionStruct 0.5 therefore emits native unions, which gives users native, exhaustive
`switch`. It keeps the public payload fields (for `ref` access), automatic memory overlap,
equality and the type-name helpers. It drops the custom matching API and the analyzer that
existed to make up for the missing language feature.
