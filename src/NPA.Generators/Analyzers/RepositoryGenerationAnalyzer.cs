using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace NPA.Generators.Analyzers;

/// <summary>
/// Analyzer that provides diagnostics for repository generation attributes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RepositoryGenerationAnalyzer : DiagnosticAnalyzer
{
    // Diagnostic IDs
    /// <summary>Diagnostic ID for missing partial keyword on repository class.</summary>
    public const string MissingPartialKeywordId = "NPA001";
    
    /// <summary>Diagnostic ID for invalid entity type (not a class).</summary>
    public const string InvalidEntityTypeId = "NPA002";
    
    /// <summary>Diagnostic ID for missing entity type specification.</summary>
    public const string MissingEntityTypeId = "NPA003";
    
    /// <summary>Diagnostic ID for duplicate repository definitions.</summary>
    public const string DuplicateRepositoryId = "NPA004";

    // Diagnostic descriptors
    private static readonly DiagnosticDescriptor MissingPartialKeywordRule = new(
        id: MissingPartialKeywordId,
        title: "Repository class must be partial",
        messageFormat: "Class '{0}' with [GenerateRepository] attribute must be declared as partial",
        category: "NPA.Repository",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Repository classes using code generation must be marked with the 'partial' keyword to allow the generator to add members.");

    private static readonly DiagnosticDescriptor InvalidEntityTypeRule = new(
        id: InvalidEntityTypeId,
        title: "Invalid entity type for repository",
        messageFormat: "Entity type '{0}' must be a class",
        category: "NPA.Repository",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Repository entity types must be classes, not interfaces, structs, or other types.");

    private static readonly DiagnosticDescriptor MissingEntityTypeRule = new(
        id: MissingEntityTypeId,
        title: "Missing entity type for repository",
        messageFormat: "Repository class '{0}' must specify an entity type",
        category: "NPA.Repository",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Repository classes must specify the entity type they manage, either through IRepository<T> inheritance or [GenerateRepository(typeof(T))].");

    private static readonly DiagnosticDescriptor DuplicateRepositoryRule = new(
        id: DuplicateRepositoryId,
        title: "Duplicate repository for entity type",
        messageFormat: "Repository for entity type '{0}' is already defined",
        category: "NPA.Repository",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Only one repository should be generated for each entity type to avoid conflicts.");

    /// <summary>
    /// Gets the supported diagnostics for this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            MissingPartialKeywordRule,
            InvalidEntityTypeRule,
            MissingEntityTypeRule,
            DuplicateRepositoryRule);

    /// <summary>
    /// Initializes the analyzer by registering actions for syntax analysis.
    /// </summary>
    /// <param name="context">The analysis context for registering analyzer actions.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);

        if (classSymbol == null)
            return;

        // Check if class has [GenerateRepository] attribute
        var hasGenerateRepositoryAttribute = classSymbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name == "GenerateRepositoryAttribute" ||
                        attr.AttributeClass?.Name == "GenerateRepository");

        if (!hasGenerateRepositoryAttribute)
            return;

        // Rule NPA001: Check if class is partial
        if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            var diagnostic = Diagnostic.Create(
                MissingPartialKeywordRule,
                classDeclaration.Identifier.GetLocation(),
                classSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }

        // Rule NPA003: Check if entity type is specified
        var entityType = GetEntityType(classSymbol);
        if (entityType == null)
        {
            var diagnostic = Diagnostic.Create(
                MissingEntityTypeRule,
                classDeclaration.Identifier.GetLocation(),
                classSymbol.Name);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        // Rule NPA002: Check if entity type is valid (must be a class)
        if (entityType.TypeKind != TypeKind.Class)
        {
            var diagnostic = Diagnostic.Create(
                InvalidEntityTypeRule,
                classDeclaration.Identifier.GetLocation(),
                entityType.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static INamedTypeSymbol? GetEntityType(INamedTypeSymbol classSymbol)
    {
        // First, check if class implements IRepository<T>
        foreach (var iface in classSymbol.AllInterfaces)
        {
            if (iface.Name == "IRepository" && iface.IsGenericType && iface.TypeArguments.Length == 1)
            {
                return iface.TypeArguments[0] as INamedTypeSymbol;
            }
        }

        // Second, check if [GenerateRepository(typeof(T))] has an argument
        var generateRepoAttr = classSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "GenerateRepositoryAttribute" ||
                                   attr.AttributeClass?.Name == "GenerateRepository");

        if (generateRepoAttr?.ConstructorArguments.Length > 0)
        {
            var typeArg = generateRepoAttr.ConstructorArguments[0];
            if (typeArg.Kind == TypedConstantKind.Type)
            {
                return typeArg.Value as INamedTypeSymbol;
            }
        }

        return null;
    }
}
