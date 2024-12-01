using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using UnionStruct.Internals;
using UnionStruct.Model;

namespace UnionStruct;

[Generator]
public class UnionStructIncrementalGenerator : IIncrementalGenerator
{
	private const string _unionAttributeName = "UnionAttribute";
	private const string _unionCaseAttributeName = "UnionCaseAttribute";

	private const string _unionAttributeSourceCode =
		$"""
		 namespace {GeneratorConstants.RootNamespace};

		 [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
		 public sealed class {_unionAttributeName} : global::System.Attribute;
		 """;

	private const string _unionCaseAttributeSourceCode =
		$"""
		 namespace {GeneratorConstants.RootNamespace};

		 [global::System.AttributeUsage(global::System.AttributeTargets.Method)]
		 public sealed class {_unionCaseAttributeName} : global::System.Attribute;
		 """;

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(ctx => ctx.AddSource($"{_unionAttributeName}.g.cs", SourceText.From(SourceBuilderUtils.Build(_unionAttributeSourceCode), Encoding.UTF8)));
		context.RegisterPostInitializationOutput(ctx => ctx.AddSource($"{_unionCaseAttributeName}.g.cs", SourceText.From(SourceBuilderUtils.Build(_unionCaseAttributeSourceCode), Encoding.UTF8)));

		// ! LINQ is used to filter out null values.
		IncrementalValuesProvider<UnionModel> provider = context.SyntaxProvider
			.CreateSyntaxProvider(
				(sn, _) => sn is RecordDeclarationSyntax recordDeclarationSyntax && recordDeclarationSyntax.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword),
				(ctx, _) => GetUnionModel(ctx))
			.Where(um => um != null)
			.Select((um, _) => um!);

		context.RegisterSourceOutput(
			context.CompilationProvider.Combine(provider.Collect()),
			(ctx, t) => GenerateUnionStruct(ctx, t.Left, t.Right));
	}

	private static UnionModel? GetUnionModel(GeneratorSyntaxContext context)
	{
		RecordDeclarationSyntax recordDeclarationSyntax = (RecordDeclarationSyntax)context.Node;

		bool isUnion = false;
		foreach (AttributeListSyntax attributeListSyntax in recordDeclarationSyntax.AttributeLists)
		{
			foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
			{
				if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
					continue;

				string attributeName = attributeSymbol.ContainingType.ToDisplayString();

				if (attributeName == $"{GeneratorConstants.RootNamespace}.{_unionAttributeName}")
				{
					isUnion = true;
					break;
				}
			}
		}

		if (!isUnion)
			return null;

		List<UnionCaseModel> cases = [];
		foreach (MethodDeclarationSyntax methodDeclarationSyntax in recordDeclarationSyntax.Members.OfType<MethodDeclarationSyntax>())
		{
			foreach (AttributeListSyntax attributeListSyntax in methodDeclarationSyntax.AttributeLists)
			{
				foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
				{
					if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
						continue;

					string attributeName = attributeSymbol.ContainingType.ToDisplayString();

					if (attributeName == $"{GeneratorConstants.RootNamespace}.{_unionCaseAttributeName}")
					{
						List<UnionCaseDataTypeModel> dataTypes = [];
						foreach (ParameterSyntax parameterSyntax in methodDeclarationSyntax.ParameterList.Parameters)
						{
							if (parameterSyntax.Type == null)
								continue;

							if (ModelExtensions.GetTypeInfo(context.SemanticModel, parameterSyntax.Type).Type is INamedTypeSymbol namedTypeSymbol)
								dataTypes.Add(new UnionCaseDataTypeModel(namedTypeSymbol));
						}

						cases.Add(new UnionCaseModel(methodDeclarationSyntax.Identifier.Text, dataTypes));
					}
				}
			}
		}

		return new UnionModel(recordDeclarationSyntax, cases);
	}

	private static void GenerateUnionStruct(SourceProductionContext context, Compilation compilation, ImmutableArray<UnionModel> unionModels)
	{
		foreach (UnionModel unionModel in unionModels)
		{
			SemanticModel semanticModel = compilation.GetSemanticModel(unionModel.RecordDeclarationSyntax.SyntaxTree);
			if (ModelExtensions.GetDeclaredSymbol(semanticModel, unionModel.RecordDeclarationSyntax) is not INamedTypeSymbol structSymbol)
				continue;

			string namespaceName = structSymbol.ContainingNamespace.ToDisplayString();
			string structName = unionModel.StructName;
			string accessibility = structSymbol.DeclaredAccessibility.ToString().ToLowerInvariant();

			CodeWriter writer = new();
			writer.WriteLine($"namespace {namespaceName};");
			writer.WriteLine();
			writer.WriteLine("[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Explicit)]");
			writer.WriteLine($"{accessibility} partial record struct {structName}");
			writer.StartBlock();
			GenerateUnionCaseConstants(writer, unionModel);
			writer.WriteLine("[global::System.Runtime.InteropServices.FieldOffset(0)]");
			writer.WriteLine("public readonly global::System.Int32 CaseIndex;");
			writer.WriteLine();
			GenerateUnionCaseDataFields(writer, unionModel);
			GeneratePrivateConstructor(writer, structName);
			GenerateIsMethods(writer, unionModel);
			GenerateFactoryMethods(writer, unionModel);
			GenerateSwitchMethod(writer, unionModel);
			GenerateMatchMethod(writer, unionModel);
			GenerateToStringMethod(writer, unionModel);
			GenerateNestedTypes(writer, unionModel);
			writer.EndBlock();

			context.AddSource($"{structName}.g.cs", SourceText.From(SourceBuilderUtils.Build(writer), Encoding.UTF8));
		}
	}

	private static void GenerateUnionCaseConstants(CodeWriter writer, UnionModel unionModel)
	{
		int index = 0;
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
			writer.WriteLine($"public const global::System.Int32 {unionCaseModel.CaseIndexFieldName} = {index++};");
		writer.WriteLine();
	}

	private static void GenerateUnionCaseDataFields(CodeWriter writer, UnionModel unionModel)
	{
		const int fieldOffset = 4;

		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
		{
			writer.WriteLine($"[global::System.Runtime.InteropServices.FieldOffset({fieldOffset})]");
			writer.WriteLine($"public {unionCaseModel.GetCaseTypeName()} {unionCaseModel.CaseFieldName};");
			writer.WriteLine();
		}
	}

	private static void GeneratePrivateConstructor(CodeWriter writer, string structName)
	{
		writer.WriteLine($"private {structName}(global::System.Int32 caseIndex)");
		writer.StartBlock();
		writer.WriteLine("CaseIndex = caseIndex;");
		writer.EndBlock();
		writer.WriteLine();
	}

	private static void GenerateIsMethods(CodeWriter writer, UnionModel unionModel)
	{
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
			writer.WriteLine($"public bool Is{unionCaseModel.CaseName} => CaseIndex == {unionCaseModel.CaseIndexFieldName};");
		writer.WriteLine();
	}

	private static void GenerateFactoryMethods(CodeWriter writer, UnionModel unionModel)
	{
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
		{
			List<string> parameterDeclarations = unionCaseModel.DataTypes.Select(dt => $"{dt.GetFullyQualifiedTypeName()} {SourceBuilderUtils.ToEscapedLocal(dt.NamedTypeSymbol.Name)}").ToList();

			writer.WriteLine($"public static partial {unionModel.StructName} {unionCaseModel.CaseName}(");

			writer.StartIndent();
			for (int i = 0; i < parameterDeclarations.Count; i++)
				writer.WriteLine($"{parameterDeclarations[i]}{(i < parameterDeclarations.Count - 1 ? "," : string.Empty)}");
			writer.EndIndent();
			writer.WriteLine(")");

			writer.StartBlock();
			writer.WriteLine($"return new {unionModel.StructName}({unionCaseModel.CaseIndexFieldName})");

			writer.StartBlock();
			if (parameterDeclarations.Count == 1)
			{
				writer.WriteLine($"{unionCaseModel.CaseFieldName} = {unionCaseModel.ParameterName},");
			}
			else
			{
				for (int i = 0; i < parameterDeclarations.Count; i++)
					writer.WriteLine($"{unionCaseModel.CaseFieldName}.{unionCaseModel.DataTypes[i].FieldName} = {parameterDeclarations[i]};");
			}

			writer.EndBlockWithSemicolon();

			writer.EndBlock();
			writer.WriteLine();
		}
	}

	private static void GenerateSwitchMethod(CodeWriter writer, UnionModel unionModel)
	{
		List<string> parameters = unionModel.Cases.Select(ucm => $"{ucm.ActionType} {ucm.ParameterName}").ToList();

		writer.WriteLine("public void Switch(");

		writer.StartIndent();
		for (int i = 0; i < parameters.Count; i++)
			writer.WriteLine($"{parameters[i]}{(i < parameters.Count - 1 ? "," : string.Empty)}");
		writer.EndIndent();
		writer.WriteLine(")");

		writer.StartBlock();
		writer.WriteLine("switch (CaseIndex)");
		writer.StartBlock();
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
			writer.WriteLine($"case {unionCaseModel.CaseIndexFieldName}: {unionCaseModel.ParameterName}.Invoke({unionCaseModel.CaseFieldName}); break;");
		writer.WriteLine("default: throw new global::System.Diagnostics.UnreachableException($\"Invalid case index: {CaseIndex}.\");");
		writer.EndBlock();
		writer.EndBlock();
		writer.WriteLine();
	}

	private static void GenerateMatchMethod(CodeWriter writer, UnionModel unionModel)
	{
		List<string> parameters = unionModel.Cases.Select(ucm => $"{ucm.FuncType} {ucm.ParameterName}").ToList();

		writer.WriteLine("public T Match<T>(");

		writer.StartIndent();
		for (int i = 0; i < parameters.Count; i++)
			writer.WriteLine($"{parameters[i]}{(i < parameters.Count - 1 ? "," : string.Empty)}");
		writer.EndIndent();
		writer.WriteLine(")");

		writer.StartBlock();
		writer.WriteLine("return CaseIndex switch");
		writer.StartBlock();
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
			writer.WriteLine($"{unionCaseModel.CaseIndexFieldName} => {unionCaseModel.ParameterName}.Invoke({unionCaseModel.CaseFieldName}),");
		writer.WriteLine("_ => throw new global::System.Diagnostics.UnreachableException($\"Invalid case index: {CaseIndex}.\")");
		writer.EndBlockWithSemicolon();
		writer.EndBlock();
		writer.WriteLine();
	}

	private static void GenerateToStringMethod(CodeWriter writer, UnionModel unionModel)
	{
		writer.WriteLine("public override global::System.String ToString()");
		writer.StartBlock();
		writer.WriteLine("return CaseIndex switch");
		writer.StartBlock();
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
		{
			if (unionCaseModel.DataTypes.Count == 0)
				writer.WriteLine($"{unionCaseModel.CaseIndexFieldName} => \"{unionCaseModel.CaseName}\",");
			else
				writer.WriteLine($"{unionCaseModel.CaseIndexFieldName} => {unionCaseModel.CaseFieldName}.ToString(),");
		}

		writer.WriteLine("_ => throw new global::System.Diagnostics.UnreachableException($\"Invalid case index: {CaseIndex}.\")");
		writer.EndBlockWithSemicolon();
		writer.EndBlock();
		writer.WriteLine();
	}

	private static void GenerateNestedTypes(CodeWriter writer, UnionModel unionModel)
	{
		foreach (UnionCaseModel unionCaseModel in unionModel.Cases)
		{
			if (unionCaseModel.DataTypes.Count == 1)
				continue;

			writer.WriteLine($"public struct {unionCaseModel.GetCaseTypeName()}");
			writer.StartBlock();
			foreach (UnionCaseDataTypeModel dataType in unionCaseModel.DataTypes)
			{
				writer.WriteLine($"public {dataType.GetFullyQualifiedTypeName()} {dataType.FieldName};");
				writer.WriteLine();
			}

			writer.EndBlock();
			writer.WriteLine();
		}
	}
}
