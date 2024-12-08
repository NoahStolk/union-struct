using UnionStruct.Internals.Model;
using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals.ModelBuilders;

internal sealed class UnionCaseModelBuilder(string caseName, IReadOnlyList<UnionCaseDataTypeModel> dataTypes, string funcOutTypeParameterName)
{
	public UnionCaseModel Build()
	{
		string caseFieldName = $"{caseName}Data";

		return new UnionCaseModel
		{
			CaseName = caseName,
			DataTypes = dataTypes,
			CaseTypeName = GetCaseTypeName(includeNullability: true),
			CaseTypeNameWithoutNullability = GetCaseTypeName(includeNullability: false),
			ActionTypeName = GetActionType(),
			FuncTypeName = GetFuncType(),
			InvocationParameters = GetInvocationParameters(caseFieldName),
			CaseIndexFieldName = $"{caseName}Index",
			CaseFieldName = caseFieldName,
			ParameterName = SourceBuilderUtils.ToEscapedLocal(caseName),
			ToStringReturnValue = GetToStringReturnValue(caseFieldName),
		};
	}

	/// <summary>
	/// Returns the case type name. This is either the only data type in the case, or the generated struct type.
	/// </summary>
	private string GetCaseTypeName(bool includeNullability)
	{
		if (dataTypes.Count != 1)
			return $"{caseName}Case";

		return includeNullability ? dataTypes[0].FullyQualifiedTypeName : dataTypes[0].FullyQualifiedTypeNameWithoutNullability;
	}

	private string GetActionType()
	{
		return dataTypes.Count switch
		{
			0 => "global::System.Action",
			_ => $"global::System.Action<{string.Join(", ", dataTypes.Select(dt => dt.FullyQualifiedTypeName))}>",
		};
	}

	private string GetFuncType()
	{
		return dataTypes.Count switch
		{
			0 => $"global::System.Func<{funcOutTypeParameterName}>",
			_ => $"global::System.Func<{string.Join(", ", dataTypes.Select(dt => dt.FullyQualifiedTypeName))}, {funcOutTypeParameterName}>",
		};
	}

	private string GetInvocationParameters(string caseFieldName)
	{
		return dataTypes.Count switch
		{
			0 => string.Empty,
			1 => caseFieldName,
			_ => string.Join(", ", dataTypes.Select(dt => $"{caseFieldName}.{dt.FieldName}")),
		};
	}

	private string GetToStringReturnValue(string caseFieldName)
	{
		if (dataTypes.Count == 0)
			return $"\"{caseName}\"";

		string fields = string.Join(", ", dataTypes.Select(GetToStringReturnValueForSingleField));
		return
			$$$"""
			   $"{{{caseName}}} {{ {{{fields}}} }}"
			   """;

		string GetToStringReturnValueForSingleField(UnionCaseDataTypeModel dataType)
		{
			if (dataTypes.Count == 1)
				return $"{dataType.FieldName} = {{{caseFieldName}}}";

			return $"{dataType.FieldName} = {{{caseFieldName}.{dataType.FieldName}}}";
		}
	}
}
