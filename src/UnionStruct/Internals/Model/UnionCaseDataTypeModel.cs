using Microsoft.CodeAnalysis;
using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals.Model;

internal sealed record UnionCaseDataTypeModel(string Name, ITypeSymbol TypeSymbol)
{
	public string Name { get; } = Name;

	public ITypeSymbol TypeSymbol { get; } = TypeSymbol;

	public string FieldName => Name.FirstCharToUpperCase();

	public string FactoryParameterName => SourceBuilderUtils.ToEscapedLocal(Name);

	public string GetFullyQualifiedTypeName(bool includeNullability)
	{
		if (includeNullability)
			return TypeSymbol.ToDisplayString(GetNullableFlowState());

		return TypeSymbol.ToDisplayString();
	}

	public NullableFlowState GetNullableFlowState()
	{
		if (TypeSymbol.IsReferenceType)
			return NullableFlowState.MaybeNull;

		if (TypeSymbol is ITypeParameterSymbol typeParameterSymbol)
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
