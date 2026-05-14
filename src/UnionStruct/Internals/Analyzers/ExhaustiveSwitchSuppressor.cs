using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExhaustiveSwitchSuppressor : DiagnosticSuppressor
{
	private const string Justification = "All UnionStruct cases are covered.";

	private static readonly SuppressionDescriptor SuppressCS8509 = new("USS0001", "CS8509", Justification);
	private static readonly SuppressionDescriptor SuppressCS8524 = new("USS0001", "CS8524", Justification);

	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(SuppressCS8509, SuppressCS8524);

	public override void ReportSuppressions(SuppressionAnalysisContext context)
	{
		INamedTypeSymbol? markerAttribute = context.Compilation.GetTypeByMetadataName($"{GeneratorConstants.RootNamespace}.{GeneratorConstants.MarkerAttributeName}");
		if (markerAttribute is null)
			return;

		foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
		{
			SuppressionDescriptor? descriptor = diagnostic.Id switch
			{
				"CS8509" => SuppressCS8509,
				"CS8524" => SuppressCS8524,
				_ => null,
			};
			if (descriptor is null)
				continue;

			Location location = diagnostic.Location;
			SyntaxNode? root = location.SourceTree?.GetRoot(context.CancellationToken);
			SyntaxNode? node = root?.FindNode(location.SourceSpan, getInnermostNodeForTie: true);
			SwitchExpressionSyntax? switchExpr = node?.FirstAncestorOrSelf<SwitchExpressionSyntax>();
			if (switchExpr is null)
				continue;

			SemanticModel semanticModel = context.GetSemanticModel(switchExpr.SyntaxTree);
			if (semanticModel.GetOperation(switchExpr, context.CancellationToken) is not ISwitchExpressionOperation op)
				continue;

			if (!ExhaustiveSwitchAnalyzer.TryResolveGoverning(op.Value, markerAttribute, out INamedTypeSymbol? unionType, out bool isTagProperty) || unionType is null)
				continue;

			IReadOnlyList<string> expected = ExhaustiveSwitchAnalyzer.GetExpectedCaseNames(unionType);
			if (expected.Count == 0)
				continue;

			HashSet<string> matched = new();
			bool hasDiscard = false;

			foreach (ISwitchExpressionArmOperation arm in op.Arms)
			{
				if (arm.Pattern is IDiscardPatternOperation)
				{
					hasDiscard = true;
					break;
				}

				if (arm.Guard != null)
					continue;

				CollectFromPattern(arm.Pattern, unionType, isTagProperty, matched);
			}

			if (hasDiscard)
				continue;

			if (expected.Any(e => !matched.Contains(e)))
				continue;

			context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
		}
	}

	private static void CollectFromPattern(IPatternOperation pattern, INamedTypeSymbol unionType, bool isTagProperty, HashSet<string> matched)
	{
		switch (pattern)
		{
			case IConstantPatternOperation cp:
				string? name = ResolveCaseName(cp.Value, unionType, isTagProperty);
				if (name != null)
					matched.Add(name);

				break;
			case IBinaryPatternOperation { OperatorKind: BinaryOperatorKind.Or } bp:
				CollectFromPattern(bp.LeftPattern, unionType, isTagProperty, matched);
				CollectFromPattern(bp.RightPattern, unionType, isTagProperty, matched);
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

		const string indexSuffix = "Index";

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
				&& fieldRef.Field.Name.Length > indexSuffix.Length
				&& fieldRef.Field.Name.EndsWith(indexSuffix, System.StringComparison.Ordinal)
				&& fieldRef.Field.Name != GeneratorConstants.CaseIndexFieldName)
			{
				return fieldRef.Field.Name.Substring(0, fieldRef.Field.Name.Length - indexSuffix.Length);
			}
		}

		return null;
	}
}
