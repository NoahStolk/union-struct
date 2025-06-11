namespace UnionStruct.Internals.Model;

internal sealed record UnionCaseModel
{
	public required string CaseName { get; init; }

	public required string? CaseDisplayName { get; init; }

	public required IReadOnlyList<UnionCaseDataTypeModel> DataTypes { get; init; }

	public required string CaseFieldTypeName { get; init; }

	public required string CaseStructTypeIdentifier { get; init; }

	public required string ActionTypeName { get; init; }

	public required string FuncTypeName { get; init; }

	public required string InvocationParameters { get; init; }

	public required string CaseIndexFieldName { get; init; }

	public required string CaseFieldName { get; init; }

	public required string ParameterName { get; init; }

	public required string ToStringReturnValue { get; init; }

	public string GetDisplayName()
	{
		return CaseDisplayName ?? CaseName;
	}
}
