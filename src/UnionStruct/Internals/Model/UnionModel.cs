using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals.Model;

/// <summary>
/// Everything needed to emit one union. It holds no symbols or syntax, so it compares by value and the incremental
/// pipeline can skip emission when nothing relevant changed.
/// </summary>
internal sealed record UnionModel
{
	/// <summary>The containing namespace, or <see langword="null"/> for the global namespace.</summary>
	public required string? NamespaceName { get; init; }

	/// <summary>The types the union is nested in, outermost first.</summary>
	public required EquatableArray<ContainingTypeModel> ContainingTypes { get; init; }

	/// <summary>The name of the struct without type parameters, escaped if it is a keyword.</summary>
	public required string StructName { get; init; }

	/// <summary>The name of the struct including type parameters if any, e.g. <c>Shape&lt;T&gt;</c>.</summary>
	public required string StructIdentifier { get; init; }

	public required string HintName { get; init; }

	public required bool IsReadOnly { get; init; }

	/// <summary>
	/// <see langword="true"/> when every case type is unmanaged and neither the struct nor any containing type is
	/// generic. Only then are the payloads overlapped with <c>[FieldOffset]</c>; generic types cannot have an explicit
	/// layout, and managed fields cannot overlap.
	/// </summary>
	public required bool AllowMemoryOverlap { get; init; }

	/// <summary>Whether any case payload can be <see langword="null"/>, which makes <c>Value</c> <c>object?</c>.</summary>
	public required bool ValueIsNullable { get; init; }

	/// <summary>
	/// Whether any payload field is declared non-nullable but may hold a reference, which <c>this = default</c> leaves null.
	/// </summary>
	public required bool HasNonNullableReferenceField { get; init; }

	public required EquatableArray<UnionCaseModel> Cases { get; init; }
}
