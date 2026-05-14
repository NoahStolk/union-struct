using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnionStruct.Internals.Analyzers;
using UnionStruct.Internals.Utils;

namespace UnionStruct.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddMissingUnionCasesCodeFix))]
[Shared]
public sealed class AddMissingUnionCasesCodeFix : CodeFixProvider
{
	private const string IndexSuffix = "Index";

	public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ExhaustiveSwitchAnalyzer.DiagnosticId);

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
			return;

		foreach (Diagnostic diagnostic in context.Diagnostics)
		{
			SyntaxNode? node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
			SyntaxNode? switchNode = node?.FirstAncestorOrSelf<SwitchStatementSyntax>() ?? (SyntaxNode?)node?.FirstAncestorOrSelf<SwitchExpressionSyntax>();
			if (switchNode is null)
				continue;

			context.RegisterCodeFix(
				CodeAction.Create(
					title: "Add missing union cases",
					createChangedDocument: ct => AddMissingCasesAsync(context.Document, switchNode, ct),
					equivalenceKey: "AddMissingUnionCases"),
				diagnostic);
		}
	}

	private static async Task<Document> AddMissingCasesAsync(Document document, SyntaxNode switchNode, CancellationToken cancellationToken)
	{
		SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		if (semanticModel is null)
			return document;

		INamedTypeSymbol? markerAttribute = semanticModel.Compilation.GetTypeByMetadataName($"{GeneratorConstants.RootNamespace}.{GeneratorConstants.MarkerAttributeName}");
		if (markerAttribute is null)
			return document;

		IOperation? maybeOp = semanticModel.GetOperation(switchNode, cancellationToken);
		if (maybeOp is not ISwitchOperation and not ISwitchExpressionOperation)
			return document;

		IOperation op = maybeOp;
		IOperation governing = op switch
		{
			ISwitchOperation switchOp => switchOp.Value,
			ISwitchExpressionOperation switchExpr => switchExpr.Value,
			_ => throw new global::System.InvalidOperationException(),
		};

		if (!ExhaustiveSwitchAnalyzer.TryResolveGoverning(governing, markerAttribute, out INamedTypeSymbol? unionType, out bool isTagProperty) || unionType is null)
			return document;

		IReadOnlyList<string> expected = ExhaustiveSwitchAnalyzer.GetExpectedCaseNames(unionType);
		HashSet<string> matched = CollectMatched(op, unionType, isTagProperty);

		List<string> missing = expected.Where(e => !matched.Contains(e)).ToList();
		if (missing.Count == 0)
			return document;

		SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
			return document;

		string unionDisplay = unionType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
		SyntaxNode newSwitch = switchNode switch
		{
			SwitchStatementSyntax stmt => InsertStatementArms(stmt, unionDisplay, isTagProperty, missing),
			SwitchExpressionSyntax expr => InsertExpressionArms(expr, unionDisplay, isTagProperty, missing),
			_ => switchNode,
		};

		return document.WithSyntaxRoot(root.ReplaceNode(switchNode, newSwitch));
	}

	private static HashSet<string> CollectMatched(IOperation op, INamedTypeSymbol unionType, bool isTagProperty)
	{
		HashSet<string> matched = new();
		switch (op)
		{
			case ISwitchOperation switchOp:
				foreach (ISwitchCaseOperation switchCase in switchOp.Cases)
				{
					foreach (ICaseClauseOperation clause in switchCase.Clauses)
					{
						switch (clause)
						{
							case IPatternCaseClauseOperation { Guard: not null }:
								break;
							case IPatternCaseClauseOperation patternCase:
								CollectFromPattern(patternCase.Pattern, unionType, isTagProperty, matched);
								break;
							case ISingleValueCaseClauseOperation single:
								string? name = ResolveCaseName(single.Value, unionType, isTagProperty);
								if (name != null)
									matched.Add(name);

								break;
							default:
								break;
						}
					}
				}

				break;
			case ISwitchExpressionOperation switchExpr:
				foreach (ISwitchExpressionArmOperation arm in switchExpr.Arms)
				{
					if (arm.Guard != null)
						continue;

					CollectFromPattern(arm.Pattern, unionType, isTagProperty, matched);
				}

				break;
			default:
				break;
		}

		return matched;
	}

	private static SwitchStatementSyntax InsertStatementArms(SwitchStatementSyntax stmt, string unionDisplay, bool isTagProperty, List<string> missing)
	{
		List<SwitchSectionSyntax> additions = new();
		foreach (string caseName in missing)
		{
			string label = isTagProperty
				? $"{unionDisplay}.{GeneratorConstants.TagEnumName}.{caseName}"
				: $"{unionDisplay}.{caseName}{IndexSuffix}";

			SwitchSectionSyntax section = SyntaxFactory.SwitchSection()
				.AddLabels(SyntaxFactory.CaseSwitchLabel(SyntaxFactory.ParseExpression(label)))
				.AddStatements(SyntaxFactory.BreakStatement());
			additions.Add(section);
		}

		int insertAt = FindStatementInsertIndex(stmt);
		return stmt.WithSections(stmt.Sections.InsertRange(insertAt, additions));
	}

	private static int FindStatementInsertIndex(SwitchStatementSyntax stmt)
	{
		for (int i = 0; i < stmt.Sections.Count; i++)
		{
			foreach (SwitchLabelSyntax label in stmt.Sections[i].Labels)
			{
				if (label is DefaultSwitchLabelSyntax)
					return i;
			}
		}

		return stmt.Sections.Count;
	}

	private static SwitchExpressionSyntax InsertExpressionArms(SwitchExpressionSyntax expr, string unionDisplay, bool isTagProperty, List<string> missing)
	{
		List<SwitchExpressionArmSyntax> additions = new();
		foreach (string caseName in missing)
		{
			string label = isTagProperty
				? $"{unionDisplay}.{GeneratorConstants.TagEnumName}.{caseName}"
				: $"{unionDisplay}.{caseName}{IndexSuffix}";

			SwitchExpressionArmSyntax arm = SyntaxFactory.SwitchExpressionArm(
				SyntaxFactory.ConstantPattern(SyntaxFactory.ParseExpression(label)),
				SyntaxFactory.ParseExpression("throw new global::System.NotImplementedException()"));
			additions.Add(arm);
		}

		int insertAt = FindExpressionInsertIndex(expr);
		return expr.WithArms(expr.Arms.InsertRange(insertAt, additions));
	}

	private static int FindExpressionInsertIndex(SwitchExpressionSyntax expr)
	{
		for (int i = 0; i < expr.Arms.Count; i++)
		{
			if (expr.Arms[i].Pattern is DiscardPatternSyntax)
				return i;
		}

		return expr.Arms.Count;
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
