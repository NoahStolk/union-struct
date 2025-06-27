# union-struct

**🚧 WORK IN PROGRESS 🚧**

[![NuGet Version](https://img.shields.io/nuget/v/NoahStolk.UnionStruct.svg)](https://www.nuget.org/packages/NoahStolk.UnionStruct/)

Union-struct is an opinionated zero-dependency library containing a source generator that generates memory-efficient union structs in C#. Union structs are value types that can store multiple types in the same memory location.

## Motivation

This library is inspired by the [Type Unions for C# proposal](https://github.com/dotnet/csharplang/blob/main/proposals/TypeUnions.md). At the time of writing, the prototype, implementation, and specification for this proposal have not been started.

It also takes inspiration from:
- [OneOf](https://github.com/mcintyre321/OneOf)
- [Dunet](https://github.com/domn1995/dunet)
- [Dusharp](https://github.com/kolebynov/Dusharp)
- [Intellenum](https://github.com/SteveDunn/Intellenum)

These are all great libraries. If this library doesn't suit your needs, I recommend checking them out.

I have not found a library that supports generating union structs with data that can be passed to `ref` parameters. This was the main motivation for creating this library.

This comes at the cost of memory-safety, as public fields need to be generated to allow this. Changing the data of a union struct can lead to memory corruption if not done correctly.

## Examples

### Basic Usage

```csharp
using UnionStruct;

[Union]
internal partial struct VarIndex
{
	[UnionCase]
	public static partial VarIndex Unsigned8(byte index);

	[UnionCase]
	public static partial VarIndex Unsigned16(ushort index);

	[UnionCase]
	public static partial VarIndex Unsigned32(uint index);
}
```

### Matching

Union structs can be matched using the `Switch` and `Match` methods.

```csharp
VarIndex index = VarIndex.Unsigned8(123);
index.Switch(
	(u8) => Console.WriteLine($"Unsigned 8-bit index: {u8}"),
	(u16) => Console.WriteLine($"Unsigned 16-bit index: {u16}"),
	(u32) => Console.WriteLine($"Unsigned 32-bit index: {u32}"));

int size = index.Match(
	(u8) => sizeof(byte),
	(u16) => sizeof(ushort),
	(u32) => sizeof(uint));
```

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
