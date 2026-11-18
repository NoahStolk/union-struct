using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals.Model;

/// <param name="Model">The union to emit, or <see langword="null"/> when a diagnostic prevents emission.</param>
internal sealed record UnionGenerationResult(UnionModel? Model, EquatableArray<DiagnosticInfo> Diagnostics);
