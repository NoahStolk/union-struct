// .NET 11 preview union exploration.
//
// Each sample is a self-contained file under Samples/ demonstrating one facet of
// the new `[Union]` / `IUnion` language feature (System.Runtime.CompilerServices),
// with the gamedev requirement front of mind: NO boxing / NO heap allocation.
//
// Run all: `dotnet run -c Release`
// See FINDINGS.md for the write-up and the comparison to the union-struct generator.

Console.WriteLine("=== .NET 11 union exploration ===\n");

Samples.S01_BoxingUnion.Run();
Samples.S02_NonBoxingUnion.Run();
Samples.S03_ExplicitOverlap.Run();
Samples.S04_RefAccess.Run();
Samples.S05_GenericUnion.Run();
Samples.S06_AllocationBench.Run();
Samples.S07_GoldenEmission.Run();
