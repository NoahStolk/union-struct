using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnionStruct.Model;

public sealed record UnionModel(RecordDeclarationSyntax RecordDeclarationSyntax, IReadOnlyList<UnionCaseModel> Cases)
{
	public RecordDeclarationSyntax RecordDeclarationSyntax { get; } = RecordDeclarationSyntax;

	public IReadOnlyList<UnionCaseModel> Cases { get; } = Cases;

	public string StructName => RecordDeclarationSyntax.Identifier.Text;
}
