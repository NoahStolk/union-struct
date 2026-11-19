namespace UnionStruct.Internals.Model;

internal sealed record UnionCaseModel
{
	/// <summary>
	/// The <c>[UnionCase(Name = ...)]</c> override, or else the case type's name without type arguments. The source of
	/// every generated identifier for the case.
	/// </summary>
	public required string Name { get; init; }

	/// <summary>The <c>[UnionCase(DisplayName = ...)]</c> override, or else <see cref="Name"/>.</summary>
	public required string DisplayName { get; init; }

	/// <summary>The fully qualified case type, including nullable annotations.</summary>
	public required string TypeName { get; init; }

	/// <summary>
	/// The accessibility modifiers of the user's partial constructor, which the implementing half must repeat, each
	/// followed by a space.
	/// </summary>
	public required string ConstructorModifiers { get; init; }

	public required string ParameterName { get; init; }

	/// <summary>
	/// <see langword="false"/> for case types a user-defined conversion cannot convert from (interfaces,
	/// <see cref="object"/>, <see cref="ValueType"/>).
	/// </summary>
	public required bool EmitImplicitConversion { get; init; }

	/// <summary>
	/// <see langword="true"/> when the case type is a struct without instance fields. It has a single value, so
	/// <c>ToString</c> prints only the case's name instead of the payload.
	/// </summary>
	public required bool IsFieldless { get; init; }

	public string IndexName => $"{Name}Index";

	public string FieldName => $"{Name}Data";
}
