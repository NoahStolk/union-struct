using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnionStruct.Internals.Model;
using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals.ModelBuilders;

internal sealed class UnionCaseDataTypeModelBuilder
{
	private readonly ParameterSyntax _parameterSyntax;
	private readonly ITypeSymbol _parameterType;

	private readonly string _name;

	public UnionCaseDataTypeModelBuilder(ParameterSyntax parameterSyntax, ITypeSymbol parameterType)
	{
		_parameterSyntax = parameterSyntax;
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
			TypeParameterAllowsNullability = TypeParameterAllowsNullability(),
			IsNullableTypeSyntax = _parameterSyntax.Type is NullableTypeSyntax,
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
		if (_parameterType is ITypeParameterSymbol)
			return GetNullableFlowStateFromSyntax();

		if (_parameterType.IsReferenceType)
		{
			return _parameterType.NullableAnnotation switch
			{
				NullableAnnotation.Annotated => NullableFlowState.MaybeNull,
				NullableAnnotation.NotAnnotated => NullableFlowState.NotNull,
				_ => GetNullableFlowStateFromSyntax(),
			};
		}

		return NullableFlowState.NotNull;
	}

	private NullableFlowState GetNullableFlowStateFromSyntax()
	{
		if (_parameterSyntax.Type is NullableTypeSyntax)
			return NullableFlowState.MaybeNull;

		return NullableFlowState.NotNull;
	}

	private bool TypeParameterAllowsNullability()
	{
		if (_parameterType is not ITypeParameterSymbol typeParameterSymbol)
			return false;

		return typeParameterSymbol is { HasUnmanagedTypeConstraint: false, HasValueTypeConstraint: false, HasNotNullConstraint: false };
	}
}
