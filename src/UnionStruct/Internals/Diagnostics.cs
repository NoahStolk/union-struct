using Microsoft.CodeAnalysis;

namespace UnionStruct.Internals;

internal static class Diagnostics
{
	private const string _category = "UnionStruct";

	public static readonly DiagnosticDescriptor UnionMustBePartial = new(
		id: "US0002",
		title: "Union struct must be partial",
		messageFormat: "Union struct '{0}' and all of its containing types must be declared partial",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor InvalidCaseConstructor = new(
		id: "US0003",
		title: "Union case constructor must have exactly one by-value parameter",
		messageFormat: "Partial constructor '{0}' must have exactly one by-value parameter; its type is the case type",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor DuplicateCaseType = new(
		id: "US0004",
		title: "Union cases must have distinct types",
		messageFormat: "Union '{0}' declares more than one case of type '{1}'",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor DuplicateCaseName = new(
		id: "US0005",
		title: "Union case names must be unique",
		messageFormat: "Union '{0}' has more than one case named '{1}'; a case is named after its type without type arguments, so give one a distinct name with [UnionCase(Name = ...)]",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor ReservedCaseName = new(
		id: "US0006",
		title: "Union case name is reserved",
		messageFormat: "Union '{0}' cannot have a case named '{1}', because its generated constant '{1}Index' would collide with the generated CaseIndex field",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor UnsupportedCaseType = new(
		id: "US0007",
		title: "Union case type is not supported",
		messageFormat: "Case type '{0}' of union '{1}' is not supported; pointer, function pointer and dynamic case types cannot be stored in a union",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor UnsupportedLanguageVersion = new(
		id: "US0008",
		title: "UnionStruct requires C# 15",
		messageFormat: "Union '{0}' requires C# 15 or later, but the project uses C# {1}",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor MissingUnionInterface = new(
		id: "US0009",
		title: "UnionStruct requires .NET 11",
		messageFormat: "Union '{0}' requires System.Runtime.CompilerServices.IUnion, which ships with .NET 11",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor InvalidCaseName = new(
		id: "US0010",
		title: "Union case name must be a valid identifier",
		messageFormat: "Case name '{0}' of union '{1}' is not a valid C# identifier; it is the stem of the case's generated members",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor CaseNameRequired = new(
		id: "US0011",
		title: "Union case needs an explicit name",
		messageFormat: "Case type '{0}' of union '{1}' has no name to derive the case's generated members from; name the case with [UnionCase(Name = ...)]",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);
}
