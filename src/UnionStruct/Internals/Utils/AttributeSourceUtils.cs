namespace UnionStruct.Internals.Utils;

/// <summary>
/// The attributes are emitted into the consuming compilation instead of being shipped as a reference assembly.
/// This keeps the NuGet package a true development dependency (no <c>lib/</c> folder, so no <c>compile</c> asset is needed)
/// and leaves consumers without any runtime dependency on UnionStruct.
/// </summary>
internal static class AttributeSourceUtils
{
	public const string HintName = "UnionStructAttributes.g.cs";

	/// <remarks>
	/// The attributes are <c>internal</c> so that assemblies which both use the generator do not end up exposing
	/// conflicting public types to each other. Everything is fully qualified because the consuming compilation may not
	/// have <c>ImplicitUsings</c> enabled, and the bodies are written as <c>{ }</c> rather than <c>;</c> so the source
	/// does not require C# 12.
	/// </remarks>
	public const string SourceCode =
		$$"""
		  namespace {{GeneratorConstants.RootNamespace}};

		  [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
		  internal sealed class {{GeneratorConstants.UnionAttributeName}} : global::System.Attribute
		  {
		  }

		  [global::System.AttributeUsage(global::System.AttributeTargets.Method)]
		  internal sealed class {{GeneratorConstants.UnionCaseAttributeName}} : global::System.Attribute
		  {
		  	public string? {{GeneratorConstants.DisplayNamePropertyName}} { get; set; }
		  }

		  [global::System.AttributeUsage(global::System.AttributeTargets.Struct, Inherited = false)]
		  internal sealed class {{GeneratorConstants.MarkerAttributeName}} : global::System.Attribute
		  {
		  }
		  """;
}
