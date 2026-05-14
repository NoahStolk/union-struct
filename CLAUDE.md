# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A C# Roslyn incremental source generator that emits union-struct value types from a `[Union]`-decorated partial struct whose `[UnionCase]`-decorated `static partial` methods declare each case. The published NuGet package is `NoahStolk.UnionStruct`. The solution lives under `src/` and the .NET SDK is pinned via `global.json` (10.0.100).

The generator's distinguishing feature versus OneOf / Dunet / Dusharp is that case data is stored in public fields, so callers can take `ref` to a case's payload. This is intentionally memory-unsafe; do not "fix" it by hiding the fields.

## Common commands

All commands assume `cd src/` (the solution is `src/UnionStruct.slnx`).

- Build: `dotnet build UnionStruct.slnx -c Release`
- Run all tests: `dotnet test UnionStruct.slnx -c Release`
- Run a single test class: `dotnet test UnionStruct.Tests/UnionStruct.Tests.csproj --filter "FullyQualifiedName~UnionStructIncrementalGeneratorTests"`
- Run a single test: append `.MethodName` to the filter (e.g. `--filter "FullyQualifiedName~UnionStructIncrementalGeneratorTests.GenericUnion"`)
- Run the sample app (exercises the generator end-to-end): `dotnet run --project UnionStruct.Sample/UnionStruct.Sample.csproj`
- Pack the NuGet: `dotnet pack -c Release -o ./artifacts -p:Version=<version>`
- NuGet integration test (validates the packed analyzer): `dotnet pack` first, then `dotnet restore`/`build`/`test` `UnionStruct.Tests.NuGetIntegration` with `--configfile nuget.integration-tests.config` (see `.github/workflows/push.yml` for the full sequence).

## Solution layout

Six projects, all under `src/`:

- `UnionStruct/` — the source generator itself. Targets `netstandard2.0` (required for analyzers) and `LangVersion 14.0`. Entry point is `UnionStructIncrementalGenerator`; the bulk of the codegen is in `Internals/UnionGenerator.cs`. `Internals/ModelBuilders/*` translate Roslyn syntax+symbols into the immutable `Internals/Model/*` records that drive emission. `System/` polyfills attributes (`IsExternalInit`, `RequiredMemberAttribute`, etc.) so the generator can use modern C# features while targeting netstandard2.0.
- `UnionStruct.Attributes/` — the runtime-facing `[Union]` and `[UnionCase(DisplayName=...)]` attributes consumed by user code. Also netstandard2.0.
- `UnionStruct.Package/` — packaging-only project; ships both DLLs as `analyzers/dotnet/cs` and the attributes DLL as `lib/netstandard2.0`. `PackageId` and `Version` live here.
- `UnionStruct.Sample/` — executable that consumes the generator via `OutputItemType="Analyzer"` ProjectReference. Useful for debugging via `launchSettings.json` in the `UnionStruct` project.
- `UnionStruct.Tests/` — snapshot tests of generator output using `Verify.SourceGenerators` + `Verify.XunitV3`. Each test in `UnionStructIncrementalGeneratorTests` is a small union declaration string passed to `TestHelper.Verify`; output is diffed against `snapshots/*.verified.cs`.
- `UnionStruct.Tests.Integration/` — xUnit tests that consume the generator as an analyzer and exercise the *generated* code at runtime (equality, ToString, pattern matching, `ref` mutation, etc.). Test unions live in `Unions/`.
- `UnionStruct.Tests.NuGetIntegration/` — same idea but consumes the packed `.nupkg` instead of a ProjectReference; only built in CI after `dotnet pack`.

## How the generator works (the parts that span multiple files)

1. `UnionStructIncrementalGenerator.Initialize` filters every `StructDeclarationSyntax` to ones bearing `[Union]`, then hands off to `UnionModelBuilder`.
2. `UnionModelBuilder` walks `[UnionCase]`-attributed `MethodDeclarationSyntax` members, delegates per-case work to `UnionCaseModelBuilder` → `UnionCaseDataTypeModelBuilder`, and computes whether the struct can use `[StructLayout(Explicit)]` with `[FieldOffset]` overlap. **Memory overlap is only allowed when all case-data types are unmanaged AND the struct has no type parameters** (`AllowMemoryOverlap` in `UnionModelBuilder.cs`). Anything generic falls back to sequential layout.
3. `UnionGenerator.Generate` emits the partial struct: per-case `CaseIndex` constants, public payload fields (one per case; nested `struct` per case when arity > 1), `Is<Case>` properties, factory methods matching the `partial` signatures, `Switch`/`Match`, `ToString`, `GetTypeString`/`GetTypeAsUtf8Span`, full `IEquatable<T>` + `==`/`!=` + `GetHashCode`, and a `NullTerminatedMemberNames` UTF-8 span.
4. Generated files are added as `{StructIdentifier}.g.cs` with `<`/`>` replaced by `(`/`)` for filesystem safety — this is why snapshot files look like `Shape(T).g.verified.cs`.

When changing emission, also expect to update snapshots — see below.

## Snapshot testing workflow

Generator output is verified via Verify. On mismatch, Verify writes `*.received.cs` alongside the `*.verified.cs` and (by default) opens a diff tool.

- Accept all pending diffs in bulk: `./scripts/accept-all.sh src/UnionStruct.Tests/snapshots` from the repo root.
- Disable the auto-diff popup: `DiffEngine_Disable=true`.
- Pick a different diff tool: `DiffEngine_ToolOrder=<tool>` (configurable in Rider under Build, Execution, Deployment → Unit Testing → Test Runner → Environment variables).

When you add a generator feature, add a new `[Fact]` in `UnionStructIncrementalGeneratorTests` and a matching integration test in `UnionStruct.Tests.Integration` if it has runtime behavior worth pinning down.

## Code style and analyzers

`src/Directory.Build.props` enables `AnalysisMode=All`, `Features=strict`, `WarningsAsErrors=nullable`, and pulls in BannedApiAnalyzers, Nullable.Extended, Roslynator, SonarAnalyzer, and StyleCop. `src/.globalconfig` tunes severities. Expect a noisy analyzer baseline; new code should compile clean against it.

- `LangVersion` is 14.0 across the solution; the generator project additionally sets `EnforceExtendedAnalyzerRules=true` and `IsRoslynComponent=true` (any change that pulls in a non-netstandard2.0 API in `UnionStruct/` will break the analyzer load).
- `AllowUnsafeBlocks=true` — the sample uses `Unsafe.AsPointer` to demonstrate struct layout.

## Things that look like bugs but aren't

- The generator emits `global::`-qualified names everywhere on purpose, including for `System.Int32` etc. — don't "simplify" to `int` in emitted strings.
- Factory methods use a local named `___factoryReturnValue` to dodge collisions with user parameter names (see the TODO comment in `UnionGenerator.GenerateFactoryMethods`). The matching `funcOutTypeParameterName = "TMatchOut"` plays the same role for `Match<T>` and has the same TODO.
- `UnionStruct.Package` has `IncludeBuildOutput=false` and only exists to assemble the NuGet (manually wires both DLLs into `analyzers/dotnet/cs`) — don't add code to it.
