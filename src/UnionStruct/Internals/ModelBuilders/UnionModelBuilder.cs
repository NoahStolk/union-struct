using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using UnionStruct.Internals.Model;
using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals.ModelBuilders;

/// <summary>
/// Translates a struct carrying the BCL <c>[Union]</c> into a <see cref="UnionModel"/>. The cases are the struct's
/// partial constructor declarations: the constructor parameter types are exactly the case set the compiler matches
/// against, so the user's declaration is the native union declaration and the generator writes the other half.
/// </summary>
internal static class UnionModelBuilder
{
	private static readonly SymbolDisplayFormat _typeFormat = SymbolDisplayFormat.FullyQualifiedFormat
		.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

	public static UnionGenerationResult? Build(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		if (context.TargetSymbol is not INamedTypeSymbol { TypeKind: TypeKind.Struct, IsRecord: false } structSymbol)
			return null;

		List<IMethodSymbol> caseConstructors = [.. structSymbol.InstanceConstructors.Where(IsPartialDefinition)];

		// A [Union] struct without partial constructors is a handwritten union; it is not ours to complete.
		if (caseConstructors.Count == 0)
			return null;

		cancellationToken.ThrowIfCancellationRequested();

		string structDisplayName = structSymbol.ToDisplayString();
		Location structLocation = ((StructDeclarationSyntax)context.TargetNode).Identifier.GetLocation();
		List<DiagnosticInfo> diagnostics = [];

		if (!IsPartialAllTheWayOut(structSymbol))
			return Fail(DiagnosticInfo.Create(Diagnostics.UnionMustBePartial, structLocation, structDisplayName));

		int languageVersion = (int)((CSharpParseOptions)context.TargetNode.SyntaxTree.Options).LanguageVersion;
		if (languageVersion < GeneratorConstants.MinimumLanguageVersion)
			return Fail(DiagnosticInfo.Create(Diagnostics.UnsupportedLanguageVersion, structLocation, structDisplayName, FormatLanguageVersion(languageVersion)));

		if (context.SemanticModel.Compilation.GetTypeByMetadataName(GeneratorConstants.BclUnionInterfaceMetadataName) is null)
			return Fail(DiagnosticInfo.Create(Diagnostics.MissingUnionInterface, structLocation, structDisplayName));

		List<UnionCaseModel> cases = [];
		List<ITypeSymbol> caseTypes = [];
		bool valueIsNullable = false;
		bool hasNonNullableReferenceField = false;
		bool allCasesUnmanaged = true;
		foreach (IMethodSymbol constructor in caseConstructors)
		{
			cancellationToken.ThrowIfCancellationRequested();

			ConstructorDeclarationSyntax? syntax = GetDefiningSyntax(constructor, cancellationToken);
			Location constructorLocation = syntax?.Identifier.GetLocation() ?? structLocation;
			if (constructor.Parameters.Length != 1 || constructor.Parameters[0].RefKind != RefKind.None || constructor.Parameters[0].IsParams)
			{
				diagnostics.Add(DiagnosticInfo.Create(Diagnostics.InvalidCaseConstructor, constructorLocation, constructor.ToDisplayString()));
				continue;
			}

			IParameterSymbol parameter = constructor.Parameters[0];
			ITypeSymbol caseType = parameter.Type;
			if (caseTypes.Contains(caseType, SymbolEqualityComparer.Default))
			{
				diagnostics.Add(DiagnosticInfo.Create(Diagnostics.DuplicateCaseType, constructorLocation, structDisplayName, caseType.ToDisplayString()));
				continue;
			}

			caseTypes.Add(caseType);

			if (!IsSupportedCaseType(caseType))
			{
				diagnostics.Add(DiagnosticInfo.Create(Diagnostics.UnsupportedCaseType, constructorLocation, caseType.ToDisplayString(), structDisplayName));
				continue;
			}

			string? explicitName = GetUnionCaseArgument(constructor, GeneratorConstants.NamePropertyName);
			if (explicitName != null && !SyntaxFacts.IsValidIdentifier(explicitName))
			{
				diagnostics.Add(DiagnosticInfo.Create(Diagnostics.InvalidCaseName, constructorLocation, explicitName, structDisplayName));
				continue;
			}

			string? name = explicitName ?? GetCaseName(caseType);
			if (name is null)
			{
				diagnostics.Add(DiagnosticInfo.Create(Diagnostics.CaseNameRequired, constructorLocation, caseType.ToDisplayString(), structDisplayName));
				continue;
			}

			if (name == "Case")
			{
				diagnostics.Add(DiagnosticInfo.Create(Diagnostics.ReservedCaseName, constructorLocation, structDisplayName, name));
				continue;
			}

			if (cases.Exists(c => c.Name == name))
			{
				diagnostics.Add(DiagnosticInfo.Create(Diagnostics.DuplicateCaseName, constructorLocation, structDisplayName, name));
				continue;
			}

			bool mayBeNull = MayBeNull(caseType);
			valueIsNullable |= mayBeNull;
			hasNonNullableReferenceField |= !mayBeNull && !caseType.IsValueType;
			allCasesUnmanaged &= caseType.IsUnmanagedType;

			cases.Add(new UnionCaseModel
			{
				Name = name,
				DisplayName = GetUnionCaseArgument(constructor, GeneratorConstants.DisplayNamePropertyName) ?? name,
				TypeName = caseType.ToDisplayString(_typeFormat),
				ConstructorModifiers = GetAccessibilityModifiers(syntax),
				ParameterName = SourceBuilderUtils.EscapeIdentifier(parameter.Name),
				EmitImplicitConversion = CanConvertFrom(caseType),
				IsFieldless = IsFieldless(caseType),
			});
		}

		if (diagnostics.Count > 0)
			return new UnionGenerationResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));

		UnionModel model = new()
		{
			NamespaceName = structSymbol.ContainingNamespace.IsGlobalNamespace ? null : structSymbol.ContainingNamespace.ToDisplayString(),
			ContainingTypes = new EquatableArray<ContainingTypeModel>([.. GetContainingTypes(structSymbol)]),
			StructName = SourceBuilderUtils.EscapeIdentifier(structSymbol.Name),
			StructIdentifier = GetIdentifier(structSymbol),
			HintName = GetHintName(structSymbol),
			IsReadOnly = structSymbol.IsReadOnly,
			AllowMemoryOverlap = allCasesUnmanaged && !IsGeneric(structSymbol),
			ValueIsNullable = valueIsNullable,
			HasNonNullableReferenceField = hasNonNullableReferenceField,

			// ReSharper disable once UseCollectionExpression (false positive)
			Cases = new EquatableArray<UnionCaseModel>([.. cases]),
		};

		return new UnionGenerationResult(model, EquatableArray<DiagnosticInfo>.Empty);

		static UnionGenerationResult Fail(DiagnosticInfo diagnostic)
		{
			return new UnionGenerationResult(null, new EquatableArray<DiagnosticInfo>([diagnostic]));
		}
	}

	private static bool IsPartialDefinition(IMethodSymbol constructor)
	{
		// Checked on the syntax rather than through IMethodSymbol.IsPartialDefinition, so it does not depend on how the
		// generator's (older) Roslyn reference models C# 14 partial constructors.
		if (constructor.IsStatic || constructor.IsImplicitlyDeclared)
			return false;

		foreach (SyntaxReference reference in constructor.DeclaringSyntaxReferences)
		{
			if (reference.GetSyntax() is ConstructorDeclarationSyntax { Body: null, ExpressionBody: null } syntax && syntax.Modifiers.Any(SyntaxKind.PartialKeyword))
				return true;
		}

		return false;
	}

	private static ConstructorDeclarationSyntax? GetDefiningSyntax(IMethodSymbol constructor, CancellationToken cancellationToken)
	{
		foreach (SyntaxReference reference in constructor.DeclaringSyntaxReferences)
		{
			if (reference.GetSyntax(cancellationToken) is ConstructorDeclarationSyntax { Body: null, ExpressionBody: null } syntax)
				return syntax;
		}

		return null;
	}

	private static bool IsPartialAllTheWayOut(INamedTypeSymbol structSymbol)
	{
		for (INamedTypeSymbol? type = structSymbol; type != null; type = type.ContainingType)
		{
			foreach (SyntaxReference reference in type.DeclaringSyntaxReferences)
			{
				if (reference.GetSyntax() is TypeDeclarationSyntax syntax && !syntax.Modifiers.Any(SyntaxKind.PartialKeyword))
					return false;
			}
		}

		return true;
	}

	private static bool IsGeneric(INamedTypeSymbol structSymbol)
	{
		for (INamedTypeSymbol? type = structSymbol; type != null; type = type.ContainingType)
		{
			if (type.TypeParameters.Length > 0)
				return true;
		}

		return false;
	}

	/// <summary>
	/// Pointers and function pointers cannot be boxed into <c>Value</c> or used as type arguments, and <c>dynamic</c>
	/// cannot be matched by type.
	/// </summary>
	private static bool IsSupportedCaseType(ITypeSymbol type)
	{
		return type switch
		{
			IPointerTypeSymbol or IFunctionPointerTypeSymbol or IDynamicTypeSymbol => false,
			IArrayTypeSymbol array => IsSupportedCaseType(array.ElementType),
			_ => true,
		};
	}

	/// <summary>
	/// The case type's name without type arguments, or <see langword="null"/> when it has none (tuples), in which case
	/// the user has to name the case. It becomes the stem of the generated <c>{Name}Index</c> and <c>{Name}Data</c>
	/// members, so it must be a valid identifier.
	/// </summary>
	private static string? GetCaseName(ITypeSymbol type)
	{
		return type switch
		{
			INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable => GetCaseName(nullable.TypeArguments[0]) is { } inner ? $"Nullable{inner}" : null,
			INamedTypeSymbol { IsTupleType: true } => null,
			INamedTypeSymbol named => named.Name,
			ITypeParameterSymbol typeParameter => typeParameter.Name,
			IArrayTypeSymbol array => GetCaseName(array.ElementType) is { } element ? $"{element}Array" : null,
			_ => null,
		};
	}

	/// <summary>Reads a string property of the constructor's <c>[UnionCase]</c>.</summary>
	private static string? GetUnionCaseArgument(IMethodSymbol constructor, string propertyName)
	{
		foreach (AttributeData attribute in constructor.GetAttributes())
		{
			if (attribute.AttributeClass?.ToDisplayString() != GeneratorConstants.UnionCaseAttributeFullName)
				continue;

			foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
			{
				if (argument.Key == propertyName && argument.Value.Value is string value)
					return value;
			}
		}

		return null;
	}

	/// <summary>
	/// The implementing half of a partial member must repeat the defining half's accessibility modifiers exactly, so
	/// they are copied from the syntax rather than derived from the symbol.
	/// </summary>
	private static string GetAccessibilityModifiers(ConstructorDeclarationSyntax? syntax)
	{
		if (syntax == null)
			return "public ";

		return string.Concat(syntax.Modifiers
			.Where(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.InternalKeyword) || m.IsKind(SyntaxKind.PrivateKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword))
			.Select(m => $"{m.Text} "));
	}

	private static bool MayBeNull(ITypeSymbol type)
	{
		if (type.NullableAnnotation == NullableAnnotation.Annotated)
			return true;

		if (type is ITypeParameterSymbol typeParameter)
		{
			return !typeParameter.HasValueTypeConstraint
				&& !typeParameter.HasUnmanagedTypeConstraint
				&& !typeParameter.HasNotNullConstraint
				&& !(typeParameter.HasReferenceTypeConstraint && typeParameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.NotAnnotated)
				&& !typeParameter.ConstraintTypes.Any(c => c.NullableAnnotation == NullableAnnotation.NotAnnotated);
		}

		// Oblivious reference types (declared in a #nullable disable context) may be null as well.
		return type.IsReferenceType && type.NullableAnnotation == NullableAnnotation.None;
	}

	/// <summary>
	/// A struct without instance fields has exactly one value. Excluded, because their fields may not be visible here:
	/// <list type="bullet">
	/// <item>partial structs, which source generators may add fields to (every union is one, so a union nested as a case
	/// would otherwise look empty: the generator sees it before its own output exists);</item>
	/// <item>special types, since a primitive's only field may be hidden in a reference assembly.</item>
	/// </list>
	/// Instance events count as storage, because a source type does not list the backing field of a field-like event
	/// among its members (it does list those of auto-properties and captured primary constructor parameters).
	/// </summary>
	private static bool IsFieldless(ITypeSymbol type)
	{
		return type is INamedTypeSymbol { TypeKind: TypeKind.Struct, SpecialType: SpecialType.None } named
			&& !named.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is TypeDeclarationSyntax syntax && syntax.Modifiers.Any(SyntaxKind.PartialKeyword))
			&& !named.GetMembers().Any(m => !m.IsStatic && m is IFieldSymbol or IEventSymbol);
	}

	private static bool CanConvertFrom(ITypeSymbol type)
	{
		return type.TypeKind is not (TypeKind.Interface or TypeKind.Dynamic)
			&& type.SpecialType is not (SpecialType.System_Object or SpecialType.System_ValueType);
	}

	private static Stack<ContainingTypeModel> GetContainingTypes(INamedTypeSymbol structSymbol)
	{
		Stack<ContainingTypeModel> containingTypes = new();
		for (INamedTypeSymbol? type = structSymbol.ContainingType; type != null; type = type.ContainingType)
		{
			string keyword = type switch
			{
				{ TypeKind: TypeKind.Struct, IsRecord: true } => "record struct",
				{ TypeKind: TypeKind.Struct } => "struct",
				{ TypeKind: TypeKind.Interface } => "interface",
				{ IsRecord: true } => "record",
				_ => "class",
			};

			containingTypes.Push(new ContainingTypeModel(keyword, GetIdentifier(type)));
		}

		return containingTypes;
	}

	private static string GetIdentifier(INamedTypeSymbol type)
	{
		string name = SourceBuilderUtils.EscapeIdentifier(type.Name);
		if (type.TypeParameters.Length == 0)
			return name;

		return $"{name}<{string.Join(", ", type.TypeParameters.Select(tp => SourceBuilderUtils.EscapeIdentifier(tp.Name)))}>";
	}

	private static string GetHintName(INamedTypeSymbol structSymbol)
	{
		ImmutableArray<string> names = [.. GetContainingTypes(structSymbol).Select(t => t.Identifier), GetIdentifier(structSymbol)];
		return $"{string.Join(".", names).Replace('<', '(').Replace('>', ')')}.g.cs";
	}

	private static string FormatLanguageVersion(int languageVersion)
	{
		// LanguageVersion encodes C# 1 to 7 as 1 to 7, C# 7.3 as 703 and C# 14 as 1400.
		if (languageVersion < 100)
			return languageVersion.ToString(System.Globalization.CultureInfo.InvariantCulture);

		return languageVersion % 100 == 0 ? (languageVersion / 100).ToString(System.Globalization.CultureInfo.InvariantCulture) : $"{languageVersion / 100}.{languageVersion % 100}";
	}
}
