using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnionStruct.Internals.Model;
using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals.ModelBuilders;

internal sealed class UnionCaseDataTypeModelBuilder
{
	private readonly ITypeSymbol _parameterType;

	private readonly string _name;

	public UnionCaseDataTypeModelBuilder(ParameterSyntax parameterSyntax, ITypeSymbol parameterType)
	{
		_parameterType = parameterType;

		_name = parameterSyntax.Identifier.Text;
	}

	public UnionCaseDataTypeModel Build()
	{
		return new UnionCaseDataTypeModel
		{
			FieldName = _name.FirstCharToUpperCase(),
			FactoryParameterName = SourceBuilderUtils.ToEscapedLocal(_name),
			FullyQualifiedTypeName = GetFullyQualifiedTypeName(includeNullability: true),
			FullyQualifiedTypeNameWithoutNullability = GetFullyQualifiedTypeName(includeNullability: false),
			TypeSymbol = _parameterType,
			IsNullableReferenceType = GetNullableFlowState() == NullableFlowState.MaybeNull,
		};
	}

	private string GetFullyQualifiedTypeName(bool includeNullability)
	{
		if (includeNullability)
			return _parameterType.ToDisplayString(GetNullableFlowState());

		return _parameterType.ToDisplayString();
	}

	private NullableFlowState GetNullableFlowState()
	{
		if (_parameterType.IsReferenceType)
			return NullableFlowState.MaybeNull;

		if (_parameterType is not ITypeParameterSymbol typeParameterSymbol)
			return NullableFlowState.None;

		if (typeParameterSymbol.HasReferenceTypeConstraint)
			return NullableFlowState.MaybeNull;

		if (typeParameterSymbol.HasNotNullConstraint || typeParameterSymbol.HasValueTypeConstraint || typeParameterSymbol.HasUnmanagedTypeConstraint)
			return NullableFlowState.None;

		return NullableFlowState.MaybeNull;
	}
}
