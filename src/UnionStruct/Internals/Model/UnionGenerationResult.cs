using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals.Model;

/// <summary>The outcome of building one union's model: what to emit and what to report.</summary>
/// <param name="Model">The union to emit, or <see langword="null"/> when a diagnostic prevents emission.</param>
/// <param name="Diagnostics">The diagnostics to report for the union.</param>
internal sealed record UnionGenerationResult(UnionModel? Model, EquatableArray<DiagnosticInfo> Diagnostics);
