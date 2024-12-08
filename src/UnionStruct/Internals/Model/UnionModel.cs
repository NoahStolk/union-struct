namespace UnionStruct.Internals.Model;

internal sealed record UnionModel
{
	public required IReadOnlyList<UnionCaseModel> Cases { get; init; }

	public required bool AllowMemoryOverlap { get; init; }

	/// <summary>
	/// Returns the name of the struct including type parameters if any.
	/// </summary>
	public required string StructIdentifier { get; init; }

	/// <summary>
	/// Returns the name of the struct without type parameters.
	/// </summary>
	public required string StructName { get; init; }

	public required string NamespaceName { get; init; }

	public required string Accessibility { get; init; }

	public required string FuncOutTypeParameterName { get; init; }
}
