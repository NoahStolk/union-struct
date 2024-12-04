using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnionStruct.Internals.Model;

internal sealed record UnionModel(StructDeclarationSyntax StructDeclarationSyntax, IReadOnlyList<UnionCaseModel> Cases)
{
	public StructDeclarationSyntax StructDeclarationSyntax { get; } = StructDeclarationSyntax;

	public IReadOnlyList<UnionCaseModel> Cases { get; } = Cases;

	public bool AllowMemoryOverlap
	{
		get
		{
			SeparatedSyntaxList<TypeParameterSyntax>? typeParameters = StructDeclarationSyntax.TypeParameterList?.Parameters;
			if (typeParameters is { Count: > 0 })
				return false;

			foreach (UnionCaseModel unionCase in Cases)
			{
				if (unionCase.DataTypes.Any(dt => dt.TypeSymbol.IsReferenceType))
					return false;
			}

			return true;
		}
	}

	/// <summary>
	/// Returns the name of the struct including type parameters if any.
	/// </summary>
	public string StructIdentifier
	{
		get
		{
			SeparatedSyntaxList<TypeParameterSyntax>? typeParameters = StructDeclarationSyntax.TypeParameterList?.Parameters;
			if (typeParameters is { Count: > 0 })
				return $"{StructName}<{string.Join(", ", typeParameters.Value.Select(tp => tp.Identifier.Text))}>";

			return StructName;
		}
	}

	/// <summary>
	/// Returns the name of the struct.
	/// </summary>
	public string StructName => StructDeclarationSyntax.Identifier.Text;
}
