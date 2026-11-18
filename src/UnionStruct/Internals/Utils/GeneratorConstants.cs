namespace UnionStruct.Internals.Utils;

internal static class GeneratorConstants
{
	public const char NewLine = '\n';

	public const string RootNamespace = "UnionStruct";

	public const string UnionCaseAttributeName = "UnionCaseAttribute";

	public const string UnionCaseAttributeFullName = $"{RootNamespace}.{UnionCaseAttributeName}";

	public const string DisplayNamePropertyName = "DisplayName";

	/// <summary>
	/// The BCL attribute the user applies. The library deliberately ships no <c>[Union]</c> of its own: it would be
	/// ambiguous (CS0104) with this one in every file that imports <c>System.Runtime.CompilerServices</c>.
	/// </summary>
	public const string BclUnionAttributeMetadataName = "System.Runtime.CompilerServices.UnionAttribute";

	public const string BclUnionInterfaceMetadataName = "System.Runtime.CompilerServices.IUnion";

	public const string CaseIndexFieldName = "CaseIndex";

	/// <summary>
	/// <c>LanguageVersion.CSharp15</c>. Spelled as a number because the generator compiles against a Roslyn that
	/// predates the enum member.
	/// </summary>
	public const int MinimumLanguageVersion = 1500;
}
