# union-struct

**🚧 WORK IN PROGRESS 🚧**

[![NuGet Version](https://img.shields.io/nuget/v/NoahStolk.UnionStruct.svg)](https://www.nuget.org/packages/NoahStolk.UnionStruct/)

Union-struct is an opinionated zero-dependency source generator that completes C# 15 [union types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/unions) as memory-efficient, allocation-free structs. You declare the case types and the union; the generator writes the storage, the non-boxing pattern-matching support, equality, `ToString`, and a few helpers.

**Requires C# 15 / .NET 11.** Version 0.5 is a rewrite on top of the native union feature and is not source compatible with 0.4. For older language versions, stay on [0.4.0](https://www.nuget.org/packages/NoahStolk.UnionStruct/0.4.0); see [Migrating from 0.4](#migrating-from-04).

## Installation

```sh
dotnet add package NoahStolk.UnionStruct
```

This is an analyzer-only package, so the `PackageReference` it writes needs no further editing:

```xml
<PackageReference Include="NoahStolk.UnionStruct">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

The optional `[UnionCase]` attribute is generated into your compilation as an `internal` type in the `UnionStruct` namespace — there is no assembly to reference, and the package leaves no trace in your build output or in the dependencies of any package you publish. The `[Union]` attribute is the BCL's own `System.Runtime.CompilerServices.UnionAttribute`.

## Motivation

C# 15 unions give native `switch`/`is` and compile-time exhaustiveness, but the compiler synthesizes no bodies. A union that just stores its payload in an `object` boxes every value-type case. Avoiding that means hand-writing typed storage and a `TryGetValue` overload per case, plus equality and `ToString`. This library generates all of that.

It also keeps the reason the library exists: **case data lives in public fields**, so you can pass a payload by `ref` and mutate it in place. When every case type is unmanaged (and the union is not generic), the payloads also overlap in memory, like a C union.

This comes at the cost of memory-safety. Writing to a field that is not the active case's can corrupt the active one.

It takes inspiration from:
- [OneOf](https://github.com/mcintyre321/OneOf)
- [Dunet](https://github.com/domn1995/dunet)
- [Dusharp](https://github.com/kolebynov/Dusharp)
- [Intellenum](https://github.com/SteveDunn/Intellenum)

These are all great libraries. If this library doesn't suit your needs, I recommend checking them out.

## Examples

### Declaring a union

Declare the case types, then a `partial struct` with the BCL `[Union]` attribute and one `partial` constructor per case. The constructor's parameter type is the case type.

```csharp
using System.Numerics;
using System.Runtime.CompilerServices;   // [Union]
using UnionStruct;                       // [UnionCase]

public readonly record struct Circle<T>(T Radius);
public readonly record struct Rectangle<T>(T Width, T Height);

[Union]
public partial struct Shape<T> where T : INumber<T>
{
	public partial Shape(Circle<T> value);

	[UnionCase(DisplayName = "Rect")]
	public partial Shape(Rectangle<T> value);
}
```

Case types must be distinct. A case without data is an empty struct, so enum-like unions nest them:

```csharp
[Union]
public readonly partial struct Medal
{
	public readonly record struct Bronze;
	public readonly record struct Silver;
	public readonly record struct Gold;

	public partial Medal(Bronze _);
	public partial Medal(Silver _);
	public partial Medal(Gold _);
}
```

### Constructing and matching

```csharp
Shape<float> shape = new Circle<float>(2f);   // implicit conversion, or new Shape<float>(new Circle<float>(2f))

float area = shape switch
{
	Circle<float> c => c.Radius * c.Radius,
	Rectangle<float> r => r.Width * r.Height,
};   // exhaustive: no default arm, no allocation
```

Adding a case later turns every switch expression that misses it into warning `CS8509`. Add `<WarningsAsErrors>$(WarningsAsErrors);CS8509</WarningsAsErrors>` to your project to enforce it.

### `ref` access to the payload

Each case's payload is a public field named `<Case>Data`, where `<Case>` is the case type's name without type arguments:

```csharp
ref Circle<float> slot = ref shape.CircleData;
slot = slot with { Radius = 9f };
```

### What is generated

For each union, the generator emits:

- `CaseCount`, a `<Case>Index` constant per case, and a `public readonly int CaseIndex` field;
- a public `<Case>Data` field per case, overlapped with `[FieldOffset]` when every case type is unmanaged and the union is not generic;
- the implementing half of each partial constructor, and an implicit conversion from each case type;
- `Value` (the `IUnion` contract; it boxes, so pattern matching never uses it) and a non-boxing `TryGetValue(out TCase)` per case;
- `IEquatable<T>`, `Equals`, `GetHashCode`, `==` and `!=`;
- `ToString`, formatted as `<Name> { Value = <payload> }`;
- `GetTypeString`, `GetTypeAsUtf8Span` (static by case index, or for the current case) and `NullTerminatedMemberNames`, which use the case type's name or its `DisplayName`.

Generated member names come from the case type's name, so two cases whose types share a name (`Foo.Circle` and `Bar.Circle`, or `Circle<int>` and `Circle<float>`) are an error.

## Migrating from 0.4

1. Declare a case type for each case. Use a record struct for a multi-parameter case and an empty struct for a case without data.
2. Replace `[UnionStruct.Union]` with `[System.Runtime.CompilerServices.Union]`.
3. Replace each `[UnionCase] public static partial U Foo(T value);` with `public partial U(FooType value);`. Keep `DisplayName` if you had one.
4. Construction: `U.Foo(x)` becomes `new U(new FooType(x))`, or relies on the implicit conversion.
5. Matching: `Switch`, `Match`, `Is<Case>`, `Tag` and `CaseTag` are gone. Use `u switch { FooType f => … }` and `u is FooType`.
6. `ref u.FooData` keeps working as long as the case type is named `Foo`.
7. Add `<WarningsAsErrors>$(WarningsAsErrors);CS8509</WarningsAsErrors>`. It replaces the `US0001` analyzer.

## Benchmarks

TODO

## Development

### Debugging the Source Generator

To debug the source generator, use the `launchSettings.json` file in the `UnionStruct` project to run the generator against the `UnionStruct.Sample` project.

You can also debug the generator tests using the `UnionStruct.Tests` project.

### Snapshot Testing

> To simply accept all snapshots immediately, run `./scripts/accept-all.sh src/UnionStruct.Tests/snapshots` from the root of the repository.

To control which diff tool is used for snapshot testing, use the `DiffEngine_ToolOrder` environment variable.

In JetBrains Rider, this can be configured under Build, Execution, Deployment > Unit Testing > Test Runner > Environment variables.

You can also disable DiffEngine by setting the `DiffEngine_Disable` environment variable to `true`.

- [Verify Support plugin for Rider](https://plugins.jetbrains.com/plugin/17240-verify-support)
