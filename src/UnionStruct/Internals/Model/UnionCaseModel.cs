namespace UnionStruct.Internals.Model;

internal sealed record UnionCaseModel
{
	public required string CaseName { get; init; }

	public required IReadOnlyList<UnionCaseDataTypeModel> DataTypes { get; init; }

	public required string CaseTypeName { get; init; }

	public required string CaseTypeNameWithoutNullability { get; init; }

	public required string ActionTypeName { get; init; }

	public required string FuncTypeName { get; init; }

	public required string InvocationParameters { get; init; }

	public required string CaseIndexFieldName { get; init; }

	public required string CaseFieldName { get; init; }

	public required string ParameterName { get; init; }

	public required string ToStringReturnValue { get; init; }
}
