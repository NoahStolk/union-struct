# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A C# Roslyn incremental source generator that completes C# 15 native unions. The user declares the case types, then a `partial struct` carrying the **BCL** `[System.Runtime.CompilerServices.Union]` with one `partial` constructor per case (the constructor's single parameter type *is* the case type). The generator writes the implementing half: typed payload storage, the constructor bodies, `Value` / `TryGetValue` (the non-boxing union pattern), equality, `ToString` and type-name helpers. The published NuGet package is `NoahStolk.UnionStruct`, version 0.5+, C# 15 / .NET 11 only; 0.4.0 (custom `[Union]` + `static partial` factory methods) stays on NuGet for older code. The solution lives under `src/` and the .NET SDK is pinned via `global.json` (11.0.100, `rollForward: latestPatch` — `latestMinor` would silently move to newer feature bands).

The design and its evidence live in `research/dotnet-11-unions/` (`FINDINGS.md`, `REWRITE-PLAN.md`). The hand-written golden outputs `Samples/S07_GoldenEmission.cs` (overlap branch) and `Samples/S10_InputSurface.cs` (sequential branch) are what the emitter reproduces; the `GoldenOverlap`/`GoldenSequential` snapshot tests use their user halves verbatim.

The generator's distinguishing feature versus OneOf / Dunet / Dusharp — and versus a hand-written native union — is that case data is stored in public fields, so callers can take `ref` to a case's payload (`TryGetValue` only hands out copies). This is intentionally memory-unsafe; do not "fix" it by hiding the fields.

## Common commands

All commands assume `cd src/` (the solution is `src/UnionStruct.slnx`).

- Build: `dotnet build UnionStruct.slnx -c Release`
- Run all tests: `dotnet test --solution UnionStruct.slnx -c Release`
- Run a single test class: `dotnet test --project UnionStruct.Tests/UnionStruct.Tests.csproj -- --treenode-filter "/*/*/UnionStructIncrementalGeneratorTests/*"`
- Run a single test: replace the trailing `*` with the method name (e.g. `"/*/*/UnionStructIncrementalGeneratorTests/GenericUnion"`)

  The tests run on TUnit, which uses Microsoft.Testing.Platform rather than VSTest. Two consequences bite if you
  forget them: `dotnet test` needs `--solution`/`--project` instead of a bare path (a positional path is rejected),
  and VSTest's `--filter "FullyQualifiedName~X"` is **silently ignored** — MTP prints its help and exits reporting
  zero tests, which reads like a pass. Use `--treenode-filter "/<Assembly>/<Namespace>/<Class>/<Test>"`.
  `test.runner` in `global.json` is what puts `dotnet test` into MTP mode; without it nothing runs at all.
- Run the sample app (exercises the generator end-to-end): `dotnet run --project UnionStruct.Sample/UnionStruct.Sample.csproj`
- Pack the NuGet: `dotnet pack -c Release -o ./artifacts -p:Version=<version>`. It builds `UnionStruct.Package` itself; don't reintroduce `GeneratePackageOnBuild` there, which makes `Pack` skip `Build` and ship a stale generator (see the comment in the csproj). When re-running the NuGet integration sequence locally, run `dotnet build-server shutdown` first: the compiler server caches analyzer assemblies by path, and the `0.0.0-temp` path never changes.
- NuGet integration test (validates the packed analyzer): `dotnet pack` first, then `dotnet restore`/`build`/`test` `UnionStruct.Tests.NuGetIntegration` with `--configfile nuget.integration-tests.config` (see `.github/workflows/push.yml` for the full sequence).

## Solution layout

All under `src/`:

- `UnionStruct/` — the source generator itself. Targets `netstandard2.0` (required for analyzers). Entry point is `UnionStructIncrementalGenerator`; the codegen is in `Internals/UnionGenerator.cs`. `Internals/ModelBuilders/UnionModelBuilder.cs` translates Roslyn syntax+symbols into the `Internals/Model/*` records that drive emission, and `Internals/Diagnostics.cs` holds the generator's own diagnostics (US0002–US0011; release-tracked in `AnalyzerReleases.Unshipped.md`). `System/` polyfills attributes (`IsExternalInit`, `RequiredMemberAttribute`, etc.) so the generator can use modern C# features while targeting netstandard2.0.
- The models hold **no symbols or syntax** — only strings, bools and `EquatableArray<T>` — and diagnostics travel as `DiagnosticInfo`/`LocationInfo`, so results compare by value and `ForAttributeWithMetadataName` can cache them. Do not put an `ISymbol`, `SyntaxNode`, `Location` or `Compilation` into a model.
- The library ships **no `[Union]` of its own**: a `UnionStruct.UnionAttribute` would be ambiguous (CS0104) with the BCL one in every file importing `System.Runtime.CompilerServices`. The only library attribute is `[UnionCase(Name = ..., DisplayName = ...)]`. It is **not** a separate project: `Internals/Utils/AttributeSourceUtils.cs` holds its source, emitted via `RegisterPostInitializationOutput`. Post-initialization is mandatory: `UnionModelBuilder.GetUnionCaseArgument` resolves `[UnionCase]` through the semantic model, and only post-initialization sources are visible to it during the generator run.
- `UnionStruct.Package/` — packaging-only project; ships `UnionStruct.dll` as `analyzers/dotnet/cs` and nothing else. `PackageId` and `Version` live here.
  - **The package is analyzer-only on purpose — do not add a `lib/` folder to it.** It sets `DevelopmentDependency=true`, which makes NuGet write `<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>` on install — note the absent `compile`. Anything in `lib/` would be invisible to consumers, who would have to hand-edit `IncludeAssets` after every install. This is why the attributes are generated rather than shipped as a reference assembly.
  - `[UnionCase]` is emitted as `internal`, so each compilation gets its own copy. Compare it by fully qualified name (`GeneratorConstants.UnionCaseAttributeFullName`), never by symbol identity.
- `UnionStruct.Sample/` — executable that consumes the generator via `OutputItemType="Analyzer"` ProjectReference. Useful for debugging via `launchSettings.json` in the `UnionStruct` project.
- `UnionStruct.Tests/` — snapshot tests of generator output using `Verify.SourceGenerators` + `Verify.TUnit`. Each test in `UnionStructIncrementalGeneratorTests` is a small union declaration string passed to `TestHelper.Verify`, which also **fails on any diagnostic** in the post-generator compilation (warnings included); output is diffed against `snapshots/*.verified.cs`. `DiagnosticTests` use `TestHelper.VerifyDiagnostics`, which skips that check and snapshots the generator's diagnostics to `*.verified.txt`. These compilations must parse C# 15, which is why `Microsoft.CodeAnalysis.CSharp.Workspaces` (test-only) is 5.12.0 while the generator's own `Microsoft.CodeAnalysis.CSharp` stays at 4.12.0 — see the comments in `Directory.Packages.props`; never raise the generator's past the Roslyn in SDK 11.0.100 (CS9057).
- `UnionStruct.Tests.Integration/` — TUnit tests that consume the generator as an analyzer and exercise the *generated* code at runtime (native exhaustive `switch`, equality, ToString, `ref` mutation, layout/sizes, and `AllocationTests`, the zero-allocation regression test). Test unions live in `Unions/`. The project sets `CS8509` as an error, so a non-exhaustive union switch fails the build.
- `UnionStruct.Tests.NuGetIntegration/` — same idea but consumes the packed `.nupkg` instead of a ProjectReference; only built in CI after `dotnet pack`. It is deliberately **not** in `UnionStruct.slnx`, and it compiles `UnionStruct.Tests.Integration`'s sources by glob — so a change to those test files must keep compiling here too, and you only find out in CI. It deliberately declares the **exact** `PrivateAssets`/`IncludeAssets` that `dotnet add package` writes for a development dependency, i.e. **without** `compile`. Do not add assets to make a build pass — if that project stops compiling, the *package layout* is wrong, not the test.

## How the generator works (the parts that span multiple files)

1. `UnionStructIncrementalGenerator.Initialize` uses `ForAttributeWithMetadataName("System.Runtime.CompilerServices.UnionAttribute")` on `StructDeclarationSyntax`, then hands off to `UnionModelBuilder.Build`.
2. `UnionModelBuilder` collects the struct's **partial constructor definitions** (found on syntax: `partial` modifier, no body). A `[Union]` struct with none is a hand-written union and is ignored silently. Each must have exactly one by-value parameter; its type is the case. The case *name* is `[UnionCase(Name)]` if set, else the case type's name without type arguments (`Nullable<int>` → `NullableInt32`, `int[]` → `Int32Array`; tuples have none, so they need `Name` — US0011), and is the stem of every generated identifier (`<Name>Index`, `<Name>Data`), so it must be a valid identifier (US0010). `[UnionCase(DisplayName)]` only changes strings (`ToString`, `GetTypeString`, UTF-8 names), never identifiers, and defaults to the name. Any diagnostic (not partial, wrong parameter shape, duplicate case type or name, reserved name `Case`, invalid or missing name, unsupported case type such as a pointer, C# < 15, missing `IUnion`) suppresses emission for that union entirely. **Memory overlap is only allowed when all case types are unmanaged AND neither the struct nor any containing type has type parameters** (`AllowMemoryOverlap`). Anything else falls back to sequential layout.
3. `UnionGenerator.Generate` emits, in S07/S10 order: `CaseCount` and `<Name>Index` constants, `CaseIndex`, the public `<Name>Data` fields (`readonly` if the user's struct is `readonly`), the partial constructor bodies, implicit conversions (skipped for interface/`object`/`ValueType` case types, which C# forbids), `Value` (`object?` iff some case may be null), a `readonly TryGetValue(out TCase)` per case, `IEquatable<T>` + `Equals`/`GetHashCode`/`==`/`!=`, `ToString` (`<Display> { Value = <payload> }`, or just `<Display>` for a field-less case — `UnionCaseModel.IsFieldless`; partial structs never count, since a generator may add their fields, which is exactly what happens to a union nested as a case), `GetTypeString`/`GetTypeAsUtf8Span`, and `NullTerminatedMemberNames`. Containing types are re-declared as `partial` (so they must be partial too — US0002).
4. Generated files are added as `{Containing.Types.}{StructIdentifier}.g.cs` with `<`/`>` replaced by `(`/`)` for filesystem safety — this is why snapshot files look like `Shape(T).g.verified.cs`.

When changing emission, also expect to update snapshots — see below.

## Snapshot testing workflow

Generator output is verified via Verify. On mismatch, Verify writes `*.received.cs` alongside the `*.verified.cs` and (by default) opens a diff tool.

- Accept all pending diffs in bulk (both `.cs` sources and `.txt` diagnostics): `./scripts/accept-all.sh src/UnionStruct.Tests/snapshots` from the repo root.
- Disable the auto-diff popup: `DiffEngine_Disable=true`.
- Encoding: the repo is UTF-8 **without** BOM (`.editorconfig` `charset = utf-8`), except snapshots. Verify always writes `*.verified.*` as UTF-8 **with** BOM and LF, so `.editorconfig` has a final `[*.{received,verified}.*]` section (Verify's recommended settings) and `.gitattributes` pins them to LF. Don't strip their BOMs or reformat them.
- Pick a different diff tool: `DiffEngine_ToolOrder=<tool>` (configurable in Rider under Build, Execution, Deployment → Unit Testing → Test Runner → Environment variables).

When you add a generator feature, add a new `[Test]` in `UnionStructIncrementalGeneratorTests` and a matching integration test in `UnionStruct.Tests.Integration` if it has runtime behavior worth pinning down.

## Test framework and the Verify maintenance fee

Tests are TUnit (`[Test]`, `await Assert.That(x).IsEqualTo(y)` — assertions are async, so test methods are
`async Task`). `Microsoft.NET.Test.Sdk` and `coverlet.*` must **not** come back: they pull in the VSTest host and
break TUnit's test discovery. Each test project is `OutputType=Exe` because MTP generates the entry point;
`src/Directory.Build.targets` mutes CA1515 and CA2007 for test projects, which is fallout from that, not style drift.

`ReadOnlySpan<byte>` has no TUnit assertion — a `ref struct` cannot be a generic type argument — so the `u8`
comparisons in `NullTerminatedMemberNamesTests` and `TypeStringTests` go through `.ToArray()` and
`IsEquivalentTo(..., CollectionOrdering.Matching)`. Keep the `CollectionOrdering.Matching`: `IsEquivalentTo`
ignores order by default, which would let a permutation pass.

Verify v33 charges an [Open Source Maintenance Fee](https://github.com/VerifyTests/Verify/blob/main/docs/maintenance-fee.md)
to revenue-generating organizations, enforced at build time. A build referencing any Verify package that declares
nothing fails with **SC021**. This project claims the free `OpenSource` exemption in `src/Directory.Build.props`.
The claim is time-bounded and the build starts failing once `Verify_SponsorshipExemptionUntil` has passed — bump it
yearly; it is not dead config.

Rider needs *Settings → Build, Execution, Deployment → Unit Testing → Testing Platform → "Enable Testing Platform
support"* before it will discover any of these tests.

## Code style and analyzers

`src/Directory.Build.props` enables `AnalysisMode=All`, `Features=strict`, `WarningsAsErrors=nullable`, and pulls in BannedApiAnalyzers, Nullable.Extended, Roslynator, SonarAnalyzer, and StyleCop. `src/.globalconfig` tunes severities. Expect a noisy analyzer baseline; new code should compile clean against it.

- `LangVersion` is 15.0 across the solution; the generator project additionally sets `EnforceExtendedAnalyzerRules=true` and `IsRoslynComponent=true` (any change that pulls in a non-netstandard2.0 API in `UnionStruct/` will break the analyzer load).
- `AllowUnsafeBlocks=true` — the sample uses `Unsafe.AsPointer` to demonstrate struct layout.

## Things that look like bugs but aren't

- The generator emits `global::`-qualified names everywhere on purpose, including for `System.Int32` etc. — don't "simplify" to `int` in emitted strings.
- The generated constructors start with `this = default` (overlapped fields cannot each be definitely assigned) and qualify fields with `this.` (the user may name the parameter after a generated field). They keep the user's parameter name and accessibility modifiers verbatim: the two halves of a partial member must match.
- `#pragma warning disable CS8618` around the constructors is deliberate: `this = default` leaves the inactive cases' non-nullable reference payloads null, which is inherent to the design. It is only emitted when such a field exists (`HasNonNullableReferenceField`).
- There is no `Switch`/`Match`, `Is<Case>`, `Tag`/`CaseTag`, factory method, US0001 analyzer, suppressor or code fix any more — all dropped in 0.5 in favour of native `switch`/`is` and exhaustiveness. Don't reintroduce them.
- Roslyn analyzers that predate unions misread them: CA1508 reports a union type pattern as "always true", and `u.Equals(null)` binds to `Equals(TUnion)` through the implicit conversion from a reference-type case. The integration tests work around both locally.
- `UnionStruct.Package` has `IncludeBuildOutput=false` and only exists to assemble the NuGet (manually wires both DLLs into `analyzers/dotnet/cs`) — don't add code to it.
