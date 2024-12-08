using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnionStruct.Internals.Model;
using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals.ModelBuilders;

internal sealed class UnionCaseModelBuilder
{
	private readonly SemanticModel _semanticModel;
	private readonly MethodDeclarationSyntax _methodDeclarationSyntax;
	private readonly string _funcOutTypeParameterName;

	private readonly string _caseName;
	private readonly string _caseFieldName;

	public UnionCaseModelBuilder(SemanticModel semanticModel, MethodDeclarationSyntax methodDeclarationSyntax, string funcOutTypeParameterName)
	{
		_semanticModel = semanticModel;
		_methodDeclarationSyntax = methodDeclarationSyntax;
		_funcOutTypeParameterName = funcOutTypeParameterName;

		_caseName = _methodDeclarationSyntax.Identifier.Text;
		_caseFieldName = $"{_caseName}Data";
	}

	public UnionCaseModel Build()
	{
		List<UnionCaseDataTypeModel> dataTypes = [];
		foreach (ParameterSyntax parameterSyntax in _methodDeclarationSyntax.ParameterList.Parameters)
		{
			if (parameterSyntax.Type == null)
				continue;

			ITypeSymbol? parameterType = _semanticModel.GetTypeInfo(parameterSyntax.Type).Type;
			if (parameterType == null)
				continue;

			UnionCaseDataTypeModelBuilder builder = new(parameterSyntax, parameterType);
			dataTypes.Add(builder.Build());
		}

		return new UnionCaseModel
		{
			CaseName = _caseName,
			DataTypes = dataTypes,
			CaseFieldTypeName = GetCaseTypeName(dataTypes, includeNullability: true),
			CaseStructTypeIdentifier = GetCaseTypeName(dataTypes, includeNullability: false),
			ActionTypeName = GetActionType(dataTypes),
			FuncTypeName = GetFuncType(dataTypes),
			InvocationParameters = GetInvocationParameters(dataTypes),
			CaseIndexFieldName = $"{_caseName}Index",
			CaseFieldName = _caseFieldName,
			ParameterName = SourceBuilderUtils.ToEscapedLocal(_caseName),
			ToStringReturnValue = GetToStringReturnValue(dataTypes),
		};
	}

	/// <summary>
	/// Returns the case type name. This is either the only data type in the case, or the generated struct type.
	/// </summary>
	private string GetCaseTypeName(List<UnionCaseDataTypeModel> dataTypes, bool includeNullability)
	{
		if (dataTypes.Count != 1)
			return $"{_caseName}Case";

		return includeNullability ? dataTypes[0].FullyQualifiedTypeName : dataTypes[0].FullyQualifiedTypeNameWithoutNullability;
	}

	private static string GetActionType(List<UnionCaseDataTypeModel> dataTypes)
	{
		return dataTypes.Count switch
		{
			0 => "global::System.Action",
			_ => $"global::System.Action<{string.Join(", ", dataTypes.Select(dt => dt.FullyQualifiedTypeName))}>",
		};
	}

	private string GetFuncType(List<UnionCaseDataTypeModel> dataTypes)
	{
		return dataTypes.Count switch
		{
			0 => $"global::System.Func<{_funcOutTypeParameterName}>",
			_ => $"global::System.Func<{string.Join(", ", dataTypes.Select(dt => dt.FullyQualifiedTypeName))}, {_funcOutTypeParameterName}>",
		};
	}

	private string GetInvocationParameters(List<UnionCaseDataTypeModel> dataTypes)
	{
		return dataTypes.Count switch
		{
			0 => string.Empty,
			1 => _caseFieldName,
			_ => string.Join(", ", dataTypes.Select(dt => $"{_caseFieldName}.{dt.FieldName}")),
		};
	}

	private string GetToStringReturnValue(List<UnionCaseDataTypeModel> dataTypes)
	{
		if (dataTypes.Count == 0)
			return $"\"{_caseName}\"";

		string fields = string.Join(", ", dataTypes.Select(GetToStringReturnValueForSingleField));
		return
			$$$"""
			   $"{{{_caseName}}} {{ {{{fields}}} }}"
			   """;

		string GetToStringReturnValueForSingleField(UnionCaseDataTypeModel dataType)
		{
			if (dataTypes.Count == 1)
				return $"{dataType.FieldName} = {{{_caseFieldName}}}";

			return $"{dataType.FieldName} = {{{_caseFieldName}.{dataType.FieldName}}}";
		}
	}
}
