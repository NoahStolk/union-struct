using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExhaustiveSwitchAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "US0001";

	internal const string IndexSuffix = "Index";

	private static readonly DiagnosticDescriptor Rule = new(
		id: DiagnosticId,
		title: "Non-exhaustive switch on union case index",
		messageFormat: "Switch on '{0}' does not handle case(s): {1}",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "When switching on a UnionStruct's CaseIndex or Tag, cover every case or include a default arm.");

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(static compilationContext =>
		{
			INamedTypeSymbol? markerAttribute = compilationContext.Compilation.GetTypeByMetadataName($"{GeneratorConstants.RootNamespace}.{GeneratorConstants.MarkerAttributeName}");
			if (markerAttribute is null)
				return;

			compilationContext.RegisterOperationAction(c => AnalyzeSwitch(c, markerAttribute), OperationKind.Switch);
			compilationContext.RegisterOperationAction(c => AnalyzeSwitchExpression(c, markerAttribute), OperationKind.SwitchExpression);
		});
	}

	private static void AnalyzeSwitch(OperationAnalysisContext context, INamedTypeSymbol markerAttribute)
	{
		ISwitchOperation op = (ISwitchOperation)context.Operation;
		if (!TryResolveGoverning(op.Value, markerAttribute, out INamedTypeSymbol? unionType, out bool isTagProperty) || unionType is null)
			return;

		IReadOnlyList<string> expected = GetExpectedCaseNames(unionType);
		if (expected.Count == 0)
			return;

		HashSet<string> matched = new();
		bool hasDefault = false;
		bool hasAnyConstant = false;

		foreach (ISwitchCaseOperation switchCase in op.Cases)
		{
			foreach (ICaseClauseOperation clause in switchCase.Clauses)
			{
				switch (clause)
				{
					case IDefaultCaseClauseOperation:
						hasDefault = true;
						break;
					case IPatternCaseClauseOperation { Guard: not null }:
						break;
					case IPatternCaseClauseOperation patternCase:
						CollectFromPattern(patternCase.Pattern, unionType, isTagProperty, matched, ref hasAnyConstant);
						break;
					case ISingleValueCaseClauseOperation single:
						string? name = ResolveCaseName(single.Value, unionType, isTagProperty);
						if (name != null)
						{
							matched.Add(name);
							hasAnyConstant = true;
						}

						break;
					default:
						break;
				}
			}
		}

		ReportIfMissing(context, op.Syntax.GetLocation(), unionType, isTagProperty, expected, matched, hasDefault, hasAnyConstant);
	}

	private static void AnalyzeSwitchExpression(OperationAnalysisContext context, INamedTypeSymbol markerAttribute)
	{
		ISwitchExpressionOperation op = (ISwitchExpressionOperation)context.Operation;
		if (!TryResolveGoverning(op.Value, markerAttribute, out INamedTypeSymbol? unionType, out bool isTagProperty) || unionType is null)
			return;

		IReadOnlyList<string> expected = GetExpectedCaseNames(unionType);
		if (expected.Count == 0)
			return;

		HashSet<string> matched = new();
		bool hasDiscard = false;
		bool hasAnyConstant = false;

		foreach (ISwitchExpressionArmOperation arm in op.Arms)
		{
			if (arm.Pattern is IDiscardPatternOperation)
			{
				hasDiscard = true;
				continue;
			}

			if (arm.Guard != null)
				continue;

			CollectFromPattern(arm.Pattern, unionType, isTagProperty, matched, ref hasAnyConstant);
		}

		ReportIfMissing(context, op.Syntax.GetLocation(), unionType, isTagProperty, expected, matched, hasDiscard, hasAnyConstant);
	}

	private static void ReportIfMissing(OperationAnalysisContext context, Location location, INamedTypeSymbol unionType, bool isTagProperty, IReadOnlyList<string> expected, HashSet<string> matched, bool hasDefault, bool hasAnyConstant)
	{
		if (!hasAnyConstant || hasDefault)
			return;

		List<string> missing = expected.Where(e => !matched.Contains(e)).ToList();
		if (missing.Count == 0)
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, location, $"{unionType.Name}.{(isTagProperty ? GeneratorConstants.TagPropertyName : GeneratorConstants.CaseIndexFieldName)}", string.Join(", ", missing)));
	}

	internal static bool TryResolveGoverning(IOperation value, INamedTypeSymbol markerAttribute, out INamedTypeSymbol? unionType, out bool isTagProperty)
	{
		unionType = null;
		isTagProperty = false;

		switch (value)
		{
			case IFieldReferenceOperation fieldRef when fieldRef.Field.Name == GeneratorConstants.CaseIndexFieldName:
				unionType = fieldRef.Field.ContainingType?.OriginalDefinition;
				break;
			case IPropertyReferenceOperation propRef when propRef.Property.Name == GeneratorConstants.TagPropertyName:
				unionType = propRef.Property.ContainingType?.OriginalDefinition;
				isTagProperty = true;
				break;
			default:
				return false;
		}

		return unionType is not null && HasMarker(unionType, markerAttribute);
	}

	private static bool HasMarker(INamedTypeSymbol type, INamedTypeSymbol marker)
	{
		foreach (AttributeData attr in type.GetAttributes())
		{
			if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, marker))
				return true;
		}

		return false;
	}

	internal static IReadOnlyList<string> GetExpectedCaseNames(INamedTypeSymbol unionType)
	{
		INamedTypeSymbol? tagEnum = unionType.GetTypeMembers(GeneratorConstants.TagEnumName).FirstOrDefault(t => t.TypeKind == TypeKind.Enum);
		if (tagEnum is null)
			return [];

		List<string> names = new();
		foreach (ISymbol member in tagEnum.GetMembers())
		{
			if (member is IFieldSymbol { IsConst: true } enumField)
				names.Add(enumField.Name);
		}

		return names;
	}

	private static void CollectFromPattern(IPatternOperation pattern, INamedTypeSymbol unionType, bool isTagProperty, HashSet<string> matched, ref bool hasAnyConstant)
	{
		switch (pattern)
		{
			case IConstantPatternOperation cp:
				string? name = ResolveCaseName(cp.Value, unionType, isTagProperty);
				if (name != null)
				{
					matched.Add(name);
					hasAnyConstant = true;
				}

				break;
			case IBinaryPatternOperation { OperatorKind: BinaryOperatorKind.Or } bp:
				CollectFromPattern(bp.LeftPattern, unionType, isTagProperty, matched, ref hasAnyConstant);
				CollectFromPattern(bp.RightPattern, unionType, isTagProperty, matched, ref hasAnyConstant);
				break;
			default:
				break;
		}
	}

	private static string? ResolveCaseName(IOperation value, INamedTypeSymbol unionType, bool isTagProperty)
	{
		while (value is IConversionOperation conv)
			value = conv.Operand;

		if (value is not IFieldReferenceOperation { Field.IsConst: true } fieldRef)
			return null;

		INamedTypeSymbol? container = fieldRef.Field.ContainingType?.OriginalDefinition;
		if (container is null)
			return null;

		if (isTagProperty)
		{
			if (container.TypeKind == TypeKind.Enum
				&& container.Name == GeneratorConstants.TagEnumName
				&& SymbolEqualityComparer.Default.Equals(container.ContainingType?.OriginalDefinition, unionType))
			{
				return fieldRef.Field.Name;
			}
		}
		else
		{
			if (SymbolEqualityComparer.Default.Equals(container, unionType)
				&& fieldRef.Field.Name.Length > IndexSuffix.Length
				&& fieldRef.Field.Name.EndsWith(IndexSuffix, System.StringComparison.Ordinal)
				&& fieldRef.Field.Name != GeneratorConstants.CaseIndexFieldName)
			{
				return fieldRef.Field.Name.Substring(0, fieldRef.Field.Name.Length - IndexSuffix.Length);
			}
		}

		return null;
	}
}
