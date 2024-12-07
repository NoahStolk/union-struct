using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals.Model;

// TODO: Refactor to UnionCaseModelBuilder.
internal sealed record UnionCaseModel(string CaseName, IReadOnlyList<UnionCaseDataTypeModel> DataTypes)
{
	public string CaseName { get; } = CaseName;

	public IReadOnlyList<UnionCaseDataTypeModel> DataTypes { get; } = DataTypes;

	public string CaseIndexFieldName => $"{CaseName}Index";

	public string CaseFieldName => $"{CaseName}Data";

	public string ParameterName => SourceBuilderUtils.ToEscapedLocal(CaseName);

	/// <summary>
	/// Returns the case type name. This is either the only data type in the case, or the generated struct type.
	/// </summary>
	public string GetCaseTypeName(bool includeNullability)
	{
		if (DataTypes.Count != 1)
			return $"{CaseName}Case";

		return includeNullability ? DataTypes[0].FullyQualifiedTypeName : DataTypes[0].FullyQualifiedTypeNameWithoutNullability;
	}

	public string GetActionType()
	{
		return DataTypes.Count switch
		{
			0 => "global::System.Action",
			_ => $"global::System.Action<{string.Join(", ", DataTypes.Select(dt => dt.FullyQualifiedTypeName))}>",
		};
	}

	public string GetFuncType(string funcOutTypeParameterName)
	{
		return DataTypes.Count switch
		{
			0 => $"global::System.Func<{funcOutTypeParameterName}>",
			_ => $"global::System.Func<{string.Join(", ", DataTypes.Select(dt => dt.FullyQualifiedTypeName))}, {funcOutTypeParameterName}>",
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

		string fields = string.Join(", ", DataTypes.Select(GetToStringReturnValueForSingleField));
		return
			$$$"""
			   $"{{{CaseName}}} {{ {{{fields}}} }}"
			   """;

		string GetToStringReturnValueForSingleField(UnionCaseDataTypeModel dataType)
		{
			if (DataTypes.Count == 1)
				return $"{dataType.FieldName} = {{{CaseFieldName}}}";

			return $"{dataType.FieldName} = {{{CaseFieldName}.{dataType.FieldName}}}";
		}
	}
}
