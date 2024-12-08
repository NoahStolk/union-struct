using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnionStruct.Internals.Model;
using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals.ModelBuilders;

internal sealed class UnionCaseDataTypeModelBuilder(ParameterSyntax parameterSyntax, ITypeSymbol parameterType)
{
	public UnionCaseDataTypeModel Build()
	{
		string name = parameterSyntax.Identifier.Text;
		return new UnionCaseDataTypeModel
		{
			FieldName = name.FirstCharToUpperCase(),
			FactoryParameterName = SourceBuilderUtils.ToEscapedLocal(name),
			FullyQualifiedTypeName = GetFullyQualifiedTypeName(includeNullability: true),
			FullyQualifiedTypeNameWithoutNullability = GetFullyQualifiedTypeName(includeNullability: false),
			TypeSymbol = parameterType,
			IsNullable = GetNullableFlowState() == NullableFlowState.MaybeNull,
		};
	}

	private string GetFullyQualifiedTypeName(bool includeNullability)
	{
		if (includeNullability)
			return parameterType.ToDisplayString(GetNullableFlowState());

		return parameterType.ToDisplayString();
	}

	private NullableFlowState GetNullableFlowState()
	{
		if (parameterType.IsReferenceType)
			return NullableFlowState.MaybeNull;

		if (parameterType is ITypeParameterSymbol typeParameterSymbol)
		{
			if (typeParameterSymbol.HasReferenceTypeConstraint)
				return NullableFlowState.MaybeNull;

			if (typeParameterSymbol.HasNotNullConstraint || typeParameterSymbol.HasValueTypeConstraint || typeParameterSymbol.HasUnmanagedTypeConstraint)
				return NullableFlowState.None;

			return NullableFlowState.MaybeNull;
		}

		return NullableFlowState.None;
	}
}
