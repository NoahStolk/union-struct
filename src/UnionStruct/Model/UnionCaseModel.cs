using UnionStruct.Internals;

namespace UnionStruct.Model;

public sealed record UnionCaseModel(string CaseName, IReadOnlyList<UnionCaseDataTypeModel> DataTypes)
{
	public string CaseName { get; } = CaseName;

	public IReadOnlyList<UnionCaseDataTypeModel> DataTypes { get; } = DataTypes;

	public string CaseIndexFieldName => $"{CaseName}Index";

	public string CaseFieldName => $"{CaseName}Data";

	public string ActionType => $"global::System.Action<{GetCaseTypeName()}>";

	public string FuncType => $"global::System.Func<{GetCaseTypeName()}, T>";

	public string ParameterName => SourceBuilderUtils.ToEscapedLocal(CaseName);

	/// <summary>
	/// Returns the name of the generated struct type for this case.
	/// </summary>
	public string GetCaseTypeName()
	{
		if (DataTypes.Count != 1)
			return $"{CaseName}Case";

		return DataTypes[0].GetFullyQualifiedTypeName();
	}
}
