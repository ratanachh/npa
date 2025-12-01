using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using NPA.Design.Models;
using NPA.Design.Services;
using NPA.Design.Shared;
using NPA.Design.Comparers;
using NPA.Design.Generators.Helpers;
using NPA.Design.Generators.Extractors;
using NPA.Design.Generators.Analyzers;
using NPA.Design.Generators.CodeGenerators;
using NPA.Design.Generators.Builders;

namespace NPA.Design.Generators;

/// <summary>
/// Source generator for creating repository implementations from interfaces marked with [Repository] attribute.
/// </summary>
[Generator]
public class RepositoryGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the incremental generator.
    /// </summary>
    /// <param name="context">The initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register a syntax provider that finds interfaces with Repository attribute
        // Using more specific predicate for better incremental performance
        var repositoryInterfaces = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => RepositoryInfoExtractor.IsRepositoryInterface(node),
                transform: static (ctx, _) => RepositoryInfoExtractor.GetRepositoryInfo(ctx))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!)  // Convert IncrementalValuesProvider<RepositoryInfo?> to IncrementalValuesProvider<RepositoryInfo>
            .WithComparer(new Comparers.RepositoryInfoComparer()); // Enable incremental caching

        // Register the source output for individual repositories
        context.RegisterSourceOutput(repositoryInterfaces, static (spc, source) => GenerateRepository(spc, source));

        // Collect all repositories and generate service collection extension
        var allRepositories = repositoryInterfaces.Collect();
        context.RegisterSourceOutput(allRepositories, static (spc, sources) => GenerateServiceCollectionExtension(spc, sources));
    }

    // Methods moved to EntityAnalyzer and RepositoryInfoExtractor

    private static void GenerateRepository(SourceProductionContext context, RepositoryInfo info)
    {
        var code = GenerateRepositoryCode(info);
        var repositoryName = info.InterfaceName;
        if (repositoryName.StartsWith("I"))
        {
            repositoryName = repositoryName.Substring(1);
        }
        context.AddSource($"{repositoryName}Implementation.g.cs", SourceText.From(code, Encoding.UTF8));

        // Generate partial interface for relationship query methods
        if (info.Relationships.Count > 0)
        {
            var interfaceCode = RelationshipQueryGenerator.GeneratePartialInterface(info);
            context.AddSource($"{repositoryName}Extensions.g.cs", SourceText.From(interfaceCode, Encoding.UTF8));
        }
    }

    private static void GenerateServiceCollectionExtension(SourceProductionContext context, ImmutableArray<RepositoryInfo> repositories)
    {
        if (repositories.IsEmpty)
            return;

        var code = RepositoryCodeGenerator.GenerateServiceCollectionExtension(repositories);
        context.AddSource("NPAServiceCollectionExtensions.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    private static string GenerateRepositoryCode(RepositoryInfo info)
    {
        var sb = new StringBuilder();

        // Generate header (file header, using statements, namespace, class declaration, constructor)
        sb.Append(RepositoryCodeGenerator.GenerateRepositoryHeader(info));

        // Generate method implementations
        foreach (var method in info.Methods)
        {
            sb.AppendLine(MethodGenerator.GenerateMethodImplementation(method, info));
            sb.AppendLine();
        }

        // Generate composite key overloads if entity has composite key
        if (info.HasCompositeKey)
        {
            sb.AppendLine(CompositeKeyGenerator.GenerateCompositeKeyMethods(info));
        }

        // Generate many-to-many relationship methods
        if (info.ManyToManyRelationships.Count > 0)
        {
            sb.AppendLine(ManyToManyGenerator.GenerateManyToManyMethods(info));
        }

        // Generate relationship-aware methods
        if (info.Relationships != null && info.Relationships.Count > 0)
        {
            sb.AppendLine(RelationshipMethodGenerator.GenerateRelationshipAwareMethods(info));
        }

        // Generate eager loading overrides
        if (info.HasEagerRelationships)
        {
            sb.AppendLine(EagerLoadingGenerator.GenerateEagerLoadingOverrides(info));
        }

        // Generate cascade operation overrides
        if (info.HasCascadeRelationships)
        {
            sb.AppendLine(CascadeOperationGenerator.GenerateCascadeOperationOverrides(info));
        }

        // Generate orphan removal override for UpdateAsync
        if (info.HasOrphanRemovalRelationships)
        {
            sb.AppendLine(OrphanRemovalGenerator.GenerateOrphanRemovalUpdateOverride(info));
        }

        // Generate property-to-column mapping helper for sorting
        sb.AppendLine(PropertyColumnMappingGenerator.GeneratePropertyColumnMapping(info));

        // Generate relationship query methods
        if (info.Relationships is { Count: > 0 })
        {
            sb.AppendLine(RelationshipQueryGenerator.GenerateRelationshipQueryMethods(info));
        }

        // Generate footer (closing braces)
        sb.Append(RepositoryCodeGenerator.GenerateRepositoryFooter());

        return sb.ToString();
    }

    // Methods moved to CompositeKeyGenerator and ManyToManyGenerator


    // Methods moved to MethodGenerator

    // Methods moved to QueryMethodGenerator

    // Methods moved to StoredProcedureGenerator and BulkOperationGenerator

    // Methods moved to ConventionBasedQueryGenerator

    // Methods moved to SqlQueryBuilder (duplicates removed)

    // Methods moved to RelationshipQueryGenerator
}
