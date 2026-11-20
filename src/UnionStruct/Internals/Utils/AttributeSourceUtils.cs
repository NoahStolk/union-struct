namespace UnionStruct.Internals.Utils;

/// <summary>
/// The attribute is emitted into the consuming compilation instead of being shipped as a reference assembly.
/// This keeps the NuGet package a true development dependency (no <c>lib/</c> folder, so no <c>compile</c> asset is needed)
/// and leaves consumers without any runtime dependency on UnionStruct.
/// </summary>
internal static class AttributeSourceUtils
{
	public const string HintName = "UnionStructAttributes.g.cs";

	/// <summary>
	/// The source of <c>[UnionCase]</c>, the only attribute left: the user marks the union with the BCL
	/// <c>System.Runtime.CompilerServices.UnionAttribute</c>, and cases are the struct's partial constructors.
	/// </summary>
	/// <remarks>
	/// The attribute is <c>internal</c> so that assemblies which both use the generator do not end up exposing
	/// conflicting public types to each other. Everything is fully qualified because the consuming compilation may not
	/// have <c>ImplicitUsings</c> enabled.
	/// </remarks>
	public const string SourceCode =
		$$"""
		  namespace {{GeneratorConstants.RootNamespace}};

		  [global::System.AttributeUsage(global::System.AttributeTargets.Constructor)]
		  internal sealed class {{GeneratorConstants.UnionCaseAttributeName}} : global::System.Attribute
		  {
		  	/// <summary>
		  	/// The case's name: the stem of its generated members (<c>{Name}Index</c>, <c>{Name}Data</c>). Defaults to the
		  	/// case type's name without type arguments. Must be a valid C# identifier.
		  	/// </summary>
		  	public string? {{GeneratorConstants.NamePropertyName}} { get; init; }

		  	/// <summary>
		  	/// The case's name in strings (<c>ToString</c>, <c>GetTypeString</c>, the UTF-8 names). Defaults to the case's
		  	/// name. Never affects generated identifiers.
		  	/// </summary>
		  	public string? {{GeneratorConstants.DisplayNamePropertyName}} { get; init; }
		  }
		  """;
}
