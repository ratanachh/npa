using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;

namespace NPA.Generators.Analyzers;

/// <summary>
/// Code fix provider that automatically fixes repository generation issues.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RepositoryCodeFixProvider)), Shared]
public class RepositoryCodeFixProvider : CodeFixProvider
{
    /// <summary>
    /// Gets the diagnostic IDs that this code fix provider can fix.
    /// </summary>
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(RepositoryGenerationAnalyzer.MissingPartialKeywordId);

    /// <summary>
    /// Gets the fix all provider for batch fixing multiple instances.
    /// </summary>
    /// <returns>The fix all provider.</returns>
    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    /// <summary>
    /// Registers code fixes for the specified code fix context.
    /// </summary>
    /// <param name="context">The code fixes context containing diagnostic information.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the class declaration identified by the diagnostic
        var declaration = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<ClassDeclarationSyntax>()
            .First();

        if (declaration == null)
            return;

        // Register a code action that will invoke the fix
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add 'partial' keyword",
                createChangedDocument: c => AddPartialKeywordAsync(context.Document, declaration, c),
                equivalenceKey: nameof(RepositoryCodeFixProvider)),
            diagnostic);
    }

    private async Task<Document> AddPartialKeywordAsync(
        Document document,
        ClassDeclarationSyntax classDeclaration,
        CancellationToken cancellationToken)
    {
        // Get the current modifiers
        var modifiers = classDeclaration.Modifiers;

        // Add the 'partial' keyword if it's not already present
        if (!modifiers.Any(SyntaxKind.PartialKeyword))
        {
            // Insert 'partial' before the class keyword
            var partialModifier = SyntaxFactory.Token(SyntaxKind.PartialKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space);

            var newModifiers = modifiers.Add(partialModifier);
            var newClassDeclaration = classDeclaration.WithModifiers(newModifiers);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }

        return document;
    }
}
