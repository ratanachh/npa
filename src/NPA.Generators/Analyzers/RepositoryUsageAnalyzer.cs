using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace NPA.Generators.Analyzers;

/// <summary>
/// Analyzer that provides diagnostics for repository usage patterns.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RepositoryUsageAnalyzer : DiagnosticAnalyzer
{
    // Diagnostic IDs
    /// <summary>Diagnostic ID for missing SaveChanges call after repository modifications.</summary>
    public const string SaveChangesNotCalledId = "NPA100";
    
    /// <summary>Diagnostic ID for invalid primary key type mismatch.</summary>
    public const string InvalidPrimaryKeyTypeId = "NPA101";

    // Diagnostic descriptors
    private static readonly DiagnosticDescriptor SaveChangesNotCalledRule = new(
        id: SaveChangesNotCalledId,
        title: "SaveChanges not called after modification",
        messageFormat: "Consider calling SaveChanges() or SaveChangesAsync() after {0} to persist changes to the database",
        category: "NPA.Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Repository modification methods (Add, Update, Delete) require SaveChanges() to persist data.");

    private static readonly DiagnosticDescriptor InvalidPrimaryKeyTypeRule = new(
        id: InvalidPrimaryKeyTypeId,
        title: "Invalid primary key type",
        messageFormat: "Primary key type '{0}' does not match expected type '{1}'",
        category: "NPA.Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The primary key value type does not match the entity's primary key property type.");

    /// <summary>
    /// Gets the supported diagnostics for this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(SaveChangesNotCalledRule, InvalidPrimaryKeyTypeRule);

    /// <summary>
    /// Initializes the analyzer by registering actions for invocation expression analysis.
    /// </summary>
    /// <param name="context">The analysis context for registering analyzer actions.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        // Get the method being invoked
        var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
        if (memberAccess == null)
            return;

        var methodSymbol = semanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
        if (methodSymbol == null)
            return;

        var containingType = methodSymbol.ContainingType;
        if (containingType == null || !RepositorySymbolHelper.IsGeneratedRepository(containingType))
            return;

        var methodName = methodSymbol.Name;

        // Check for modification methods without SaveChanges
        if (methodName is "Add" or "AddAsync" or "Update" or "UpdateAsync" or "Delete" or "DeleteAsync")
        {
            AnalyzeSaveChangesUsage(context, invocation, methodName);
        }

        // Check for type mismatches in GetById/Delete
        if (methodName is "GetById" or "GetByIdAsync" or "Delete" or "DeleteAsync")
        {
            AnalyzePrimaryKeyType(context, invocation, methodSymbol, containingType);
        }
    }

    private static void AnalyzeSaveChangesUsage(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, string methodName)
    {
        // Look for SaveChanges call in the same method/block
        var containingMethod = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (containingMethod == null)
            return;

        var hasSaveChanges = containingMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(inv =>
            {
                var memberAccess = inv.Expression as MemberAccessExpressionSyntax;
                return memberAccess?.Name.Identifier.Text is "SaveChanges" or "SaveChangesAsync";
            });

        if (!hasSaveChanges)
        {
            var diagnostic = Diagnostic.Create(
                SaveChangesNotCalledRule,
                invocation.GetLocation(),
                methodName);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzePrimaryKeyType(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        IMethodSymbol methodSymbol,
        INamedTypeSymbol repositoryType)
    {
        var entityType = RepositorySymbolHelper.GetEntityType(repositoryType);
        if (entityType == null)
            return;

        var primaryKeyType = RepositorySymbolHelper.GetPrimaryKeyType(entityType);
        if (primaryKeyType == null)
            return;

        // Get the first parameter (id parameter)
        if (methodSymbol.Parameters.Length == 0)
            return;

        var idParameter = methodSymbol.Parameters[0];
        var argumentList = invocation.ArgumentList;

        if (argumentList.Arguments.Count == 0)
            return;

        var firstArgument = argumentList.Arguments[0];
        var argumentType = context.SemanticModel.GetTypeInfo(firstArgument.Expression, context.CancellationToken).Type;

        if (argumentType != null && !SymbolEqualityComparer.Default.Equals(argumentType, primaryKeyType))
        {
            // Check if there's an implicit conversion
            var conversion = context.SemanticModel.Compilation.ClassifyConversion(argumentType, primaryKeyType);
            if (!conversion.Exists || (!conversion.IsIdentity && !conversion.IsImplicit))
            {
                var diagnostic = Diagnostic.Create(
                    InvalidPrimaryKeyTypeRule,
                    firstArgument.GetLocation(),
                    argumentType.ToDisplayString(),
                    primaryKeyType.ToDisplayString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
