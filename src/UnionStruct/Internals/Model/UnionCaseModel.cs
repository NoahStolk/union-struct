using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals.Model;

internal sealed record UnionCaseModel(string CaseName, IReadOnlyList<UnionCaseDataTypeModel> DataTypes)
{
	public string CaseName { get; } = CaseName;

	public IReadOnlyList<UnionCaseDataTypeModel> DataTypes { get; } = DataTypes;

	public string CaseIndexFieldName => $"{CaseName}Index";

	public string CaseFieldName => $"{CaseName}Data";

	public string ParameterName => SourceBuilderUtils.ToEscapedLocal(CaseName);

	/// <summary>
	/// Returns the name of the generated struct type for this case.
	/// </summary>
	public string CaseTypeName => DataTypes.Count == 1 ? DataTypes[0].GetFullyQualifiedTypeName() : $"{CaseName}Case";

	public string GetActionType()
	{
		return DataTypes.Count switch
		{
			0 => "global::System.Action",
			_ => $"global::System.Action<{string.Join(", ", DataTypes.Select(dt => dt.GetFullyQualifiedTypeName()))}>",
		};
	}

	public string GetFuncType(string funcOutTypeParameterName)
	{
		return DataTypes.Count switch
		{
			0 => $"global::System.Func<{funcOutTypeParameterName}>",
			_ => $"global::System.Func<{string.Join(", ", DataTypes.Select(dt => dt.GetFullyQualifiedTypeName()))}, {funcOutTypeParameterName}>",
		};
	}

	public string GetInvocationParameters()
	{
		return DataTypes.Count switch
		{
			0 => string.Empty,
			1 => CaseFieldName,
			_ => string.Join(", ", DataTypes.Select(dt => $"{CaseFieldName}.{dt.FieldName}")),
		};
	}

	public string GetToStringReturnValue()
	{
		if (DataTypes.Count == 0)
			return $"\"{CaseName}\"";

		if (DataTypes.Count == 1)
			return $"{CaseFieldName}.ToString() ?? string.Empty";

		string fields = string.Join(", ", DataTypes.Select(dt => $"{dt.FieldName} = {{{CaseFieldName}.{dt.FieldName}}}"));
		return
			$$$"""
			   $"{{{CaseName}}} {{ {{{fields}}} }}"
			   """;
	}
}
