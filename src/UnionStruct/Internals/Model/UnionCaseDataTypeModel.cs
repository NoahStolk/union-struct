using Microsoft.CodeAnalysis;
using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals.Model;

internal sealed record UnionCaseDataTypeModel(string Name, ITypeSymbol TypeSymbol)
{
	public string Name { get; } = Name;

	public ITypeSymbol TypeSymbol { get; } = TypeSymbol;

	public string FieldName => Name.FirstCharToUpperCase();

	public string FactoryParameterName => SourceBuilderUtils.ToEscapedLocal(Name);

	public string GetFullyQualifiedTypeName()
	{
		return TypeSymbol.ToDisplayString(NullableFlowState.MaybeNull);
	}
}
