using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using NPA.Generators.Shared;

namespace NPA.Generators;

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
                predicate: static (node, _) => IsRepositoryInterface(node),
                transform: static (ctx, _) => GetRepositoryInfo(ctx))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!)  // Convert IncrementalValuesProvider<RepositoryInfo?> to IncrementalValuesProvider<RepositoryInfo>
            .WithComparer(new RepositoryInfoComparer()); // Enable incremental caching

        // Register the source output for individual repositories
        context.RegisterSourceOutput(repositoryInterfaces, static (spc, source) => GenerateRepository(spc, source));

        // Collect all repositories and generate service collection extension
        var allRepositories = repositoryInterfaces.Collect();
        context.RegisterSourceOutput(allRepositories, static (spc, sources) => GenerateServiceCollectionExtension(spc, sources));
    }

    private static bool IsRepositoryInterface(SyntaxNode node)
    {
        // Optimized predicate: Only consider interface declarations with attributes
        if (node is not InterfaceDeclarationSyntax interfaceDecl)
            return false;

        // Quick check: Must have attributes and name should contain "Repository"
        // This reduces the number of nodes passed to the expensive semantic analysis
        return interfaceDecl.AttributeLists.Count > 0 &&
               interfaceDecl.Identifier.Text.Contains("Repository");
    }

    private static RepositoryInfo? GetRepositoryInfo(GeneratorSyntaxContext context)
    {
        var interfaceDecl = (InterfaceDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDecl) as INamedTypeSymbol;

        if (interfaceSymbol == null)
            return null;

        // Skip nested types (for demonstration/sample code)
        if (interfaceSymbol.ContainingType != null)
            return null;

        // Check if it has the Repository attribute
        var repositoryAttribute = interfaceSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "RepositoryAttribute");

        if (repositoryAttribute == null)
            return null;

        // Extract entity and key types from IRepository<TEntity, TKey> inheritance
        var (entityType, keyType) = ExtractRepositoryTypes(interfaceSymbol);
        if (entityType == null || keyType == null)
            return null;

        var methods = interfaceSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Select(m => new MethodInfo
            {
                Name = m.Name,
                ReturnType = m.ReturnType.ToDisplayString(),
                Parameters = m.Parameters.Select(p => new ParameterInfo
                {
                    Name = p.Name,
                    Type = p.Type.ToDisplayString()
                }).ToList(),
                Attributes = ExtractMethodAttributes(m),
                Symbol = m
            })
            .ToList();

        // Detect composite keys by analyzing the entity type
        var (hasCompositeKey, compositeKeyProps) = DetectCompositeKey(semanticModel.Compilation, entityType);

        // Detect many-to-many relationships
        var manyToManyRelationships = DetectManyToManyRelationships(semanticModel.Compilation, entityType);

        // Detect multi-tenancy
        var multiTenantInfo = DetectMultiTenancy(semanticModel.Compilation, entityType);

        // Extract entity metadata (table name and column mappings)
        var entityMetadata = ExtractEntityMetadata(semanticModel.Compilation, entityType);

        // Build dictionary of all entity metadata (main + related entities)
        var entitiesMetadata = BuildEntityMetadataDictionary(semanticModel.Compilation, entityMetadata);

        // Extract relationship metadata
        var relationships = ExtractRelationships(semanticModel.Compilation, entityType);

        return new RepositoryInfo
        {
            InterfaceName = interfaceSymbol.Name,
            FullInterfaceName = interfaceSymbol.ToDisplayString(),
            Namespace = interfaceSymbol.ContainingNamespace.ToDisplayString(),
            EntityType = entityType,
            KeyType = keyType,
            Methods = methods,
            HasCompositeKey = hasCompositeKey,
            CompositeKeyProperties = compositeKeyProps,
            ManyToManyRelationships = manyToManyRelationships,
            MultiTenantInfo = multiTenantInfo,
            EntityMetadata = entityMetadata,
            EntitiesMetadata = entitiesMetadata,
            Relationships = relationships,
            Compilation = semanticModel.Compilation
        };
    }

    private static (bool hasCompositeKey, List<string> keyProperties) DetectCompositeKey(Compilation compilation, string entityTypeName)
    {
        var keyProperties = new List<string>();

        // Find the entity type symbol
        var entityType = compilation.GetTypeByMetadataName(entityTypeName);
        if (entityType == null)
        {
            // Try to find it without full namespace
            entityType = compilation.GetSymbolsWithName(entityTypeName.Split('.').Last(), SymbolFilter.Type)
                .OfType<INamedTypeSymbol>()
                .FirstOrDefault();
        }

        if (entityType == null)
            return (false, keyProperties);

        // Find all properties with [Id] attribute
        var idProperties = entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.GetAttributes().Any(a => a.AttributeClass?.Name == "IdAttribute"))
            .Select(p => p.Name)
            .ToList();

        if (idProperties.Count > 1)
        {
            return (true, idProperties);
        }

        return (false, keyProperties);
    }

    private static List<ManyToManyRelationshipInfo> DetectManyToManyRelationships(Compilation compilation, string entityTypeName)
    {
        var relationships = new List<ManyToManyRelationshipInfo>();

        // Find the entity type symbol
        var entityType = compilation.GetTypeByMetadataName(entityTypeName);
        if (entityType == null)
        {
            // Try to find it without full namespace
            entityType = compilation.GetSymbolsWithName(entityTypeName.Split('.').Last(), SymbolFilter.Type)
                .OfType<INamedTypeSymbol>()
                .FirstOrDefault();
        }

        if (entityType == null)
            return relationships;

        // Find all properties with [ManyToMany] attribute
        var manyToManyProperties = entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.GetAttributes().Any(a => a.AttributeClass?.Name == "ManyToManyAttribute"))
            .ToList();

        foreach (var property in manyToManyProperties)
        {
            var manyToManyAttr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "ManyToManyAttribute");

            var joinTableAttr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "JoinTableAttribute");

            if (manyToManyAttr == null || joinTableAttr == null)
                continue;

            // Extract collection element type
            var collectionElementType = string.Empty;
            if (property.Type is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
            {
                collectionElementType = namedType.TypeArguments[0].ToDisplayString();
            }

            // Extract ManyToMany attribute properties
            var mappedBy = string.Empty;
            var mappedByArg = manyToManyAttr.NamedArguments.FirstOrDefault(a => a.Key == "MappedBy");
            if (mappedByArg.Value.Value is string mappedByValue)
            {
                mappedBy = mappedByValue;
            }

            // Extract JoinTable attribute properties
            var joinTableName = string.Empty;
            var joinTableSchema = string.Empty;
            var joinColumns = Array.Empty<string>();
            var inverseJoinColumns = Array.Empty<string>();

            // Name is usually the first constructor argument
            if (joinTableAttr.ConstructorArguments.Length > 0 && joinTableAttr.ConstructorArguments[0].Value is string tableName)
            {
                joinTableName = tableName;
            }

            // Extract named arguments
            foreach (var namedArg in joinTableAttr.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "Schema":
                        if (namedArg.Value.Value is string schema)
                            joinTableSchema = schema;
                        break;
                    case "JoinColumns":
                        if (namedArg.Value.Kind == TypedConstantKind.Array && namedArg.Value.Values.Length > 0)
                        {
                            joinColumns = namedArg.Value.Values
                                .Select(v => v.Value?.ToString() ?? string.Empty)
                                .Where(s => !string.IsNullOrEmpty(s))
                                .ToArray();
                        }
                        break;
                    case "InverseJoinColumns":
                        if (namedArg.Value.Kind == TypedConstantKind.Array && namedArg.Value.Values.Length > 0)
                        {
                            inverseJoinColumns = namedArg.Value.Values
                                .Select(v => v.Value?.ToString() ?? string.Empty)
                                .Where(s => !string.IsNullOrEmpty(s))
                                .ToArray();
                        }
                        break;
                }
            }

            if (!string.IsNullOrEmpty(joinTableName) && !string.IsNullOrEmpty(collectionElementType))
            {
                relationships.Add(new ManyToManyRelationshipInfo
                {
                    PropertyName = property.Name,
                    PropertyType = property.Type.ToDisplayString(),
                    CollectionElementType = collectionElementType,
                    JoinTableName = joinTableName,
                    JoinTableSchema = joinTableSchema,
                    JoinColumns = joinColumns,
                    InverseJoinColumns = inverseJoinColumns,
                    MappedBy = mappedBy
                });
            }
        }

        return relationships;
    }

    private static MultiTenantInfo? DetectMultiTenancy(Compilation compilation, string entityTypeName)
    {
        // Find the entity type symbol
        var entityType = compilation.GetTypeByMetadataName(entityTypeName);
        if (entityType == null)
        {
            // Try to find it without full namespace
            entityType = compilation.GetSymbolsWithName(entityTypeName.Split('.').Last(), SymbolFilter.Type)
                .OfType<INamedTypeSymbol>()
                .FirstOrDefault();
        }

        if (entityType == null)
            return null;

        // Find [MultiTenant] attribute
        var multiTenantAttr = entityType.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "MultiTenantAttribute");

        if (multiTenantAttr == null)
            return null;

        // Extract attribute properties with defaults
        var tenantIdProperty = "TenantId";
        var enforceTenantIsolation = true;
        var allowCrossTenantQueries = false;

        // Check constructor arguments first (positional parameter like [MultiTenant("OrganizationId")])
        if (multiTenantAttr.ConstructorArguments.Length > 0)
        {
            var firstArg = multiTenantAttr.ConstructorArguments[0];
            if (firstArg.Value is string constructorProp && !string.IsNullOrEmpty(constructorProp))
            {
                tenantIdProperty = constructorProp;
            }
        }

        // Named arguments (both named constructor parameters like tenantIdProperty: and property setters)
        // Note: Named constructor parameters appear as property names in Roslyn's NamedArguments
        foreach (var namedArg in multiTenantAttr.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "TenantIdProperty": // Property name used by Roslyn even for constructor parameter
                    if (namedArg.Value.Value is string prop && !string.IsNullOrEmpty(prop))
                        tenantIdProperty = prop;
                    break;
                case "EnforceTenantIsolation":
                    if (namedArg.Value.Value is bool enforce)
                        enforceTenantIsolation = enforce;
                    break;
                case "AllowCrossTenantQueries":
                    if (namedArg.Value.Value is bool allow)
                        allowCrossTenantQueries = allow;
                    break;
            }
        }

        return new MultiTenantInfo
        {
            IsMultiTenant = true,
            TenantIdProperty = tenantIdProperty,
            EnforceTenantIsolation = enforceTenantIsolation,
            AllowCrossTenantQueries = allowCrossTenantQueries
        };
    }

    private static EntityMetadataInfo? ExtractEntityMetadata(Compilation compilation, string entityTypeName)
    {
        // Find the entity type symbol
        var entityType = compilation.GetTypeByMetadataName(entityTypeName);
        if (entityType == null)
        {
            // Try to find it without full namespace
            entityType = compilation.GetSymbolsWithName(entityTypeName.Split('.').Last(), SymbolFilter.Type)
                .OfType<INamedTypeSymbol>()
                .FirstOrDefault();
        }

        if (entityType == null)
            return null;

        // Use shared metadata extractor for consistency
        return MetadataExtractor.ExtractEntityMetadata(entityType);
    }

    /// <summary>
    /// Builds a dictionary of entity metadata including the main entity and all related entities
    /// </summary>
    private static Dictionary<string, EntityMetadataInfo> BuildEntityMetadataDictionary(
        Compilation compilation,
        EntityMetadataInfo? mainEntityMetadata)
    {
        var dictionary = new Dictionary<string, EntityMetadataInfo>();

        if (mainEntityMetadata == null)
            return dictionary;

        // Add the main entity
        dictionary[mainEntityMetadata.Name] = mainEntityMetadata;

        // Add related entities from relationships
        foreach (var relationship in mainEntityMetadata.Relationships)
        {
            if (!dictionary.ContainsKey(relationship.TargetEntity))
            {
                var relatedMetadata = ExtractEntityMetadata(compilation, relationship.TargetEntity);
                if (relatedMetadata != null)
                {
                    dictionary[relatedMetadata.Name] = relatedMetadata;
                }
            }
        }

        return dictionary;
    }

    private static (string? entityType, string? keyType) ExtractRepositoryTypes(INamedTypeSymbol interfaceSymbol)
    {
        // Look for IRepository<TEntity, TKey> in the interface hierarchy
        foreach (var baseInterface in interfaceSymbol.AllInterfaces)
        {
            if (baseInterface.Name == "IRepository" && baseInterface.TypeArguments.Length >= 2)
            {
                var entityType = baseInterface.TypeArguments[0].ToDisplayString();
                var keyType = baseInterface.TypeArguments[1].ToDisplayString();
                return (entityType, keyType);
            }
        }

        // If no IRepository<TEntity, TKey> is found, try to infer from interface name
        var interfaceName = interfaceSymbol.Name;
        if (interfaceName.StartsWith("I") && interfaceName.EndsWith("Repository"))
        {
            var entityType = interfaceName.Substring(1, interfaceName.Length - 11); // Remove I prefix and Repository suffix
            return (entityType, "object"); // Default key type
        }

        return (null, null);
    }

    private static MethodAttributeInfo ExtractMethodAttributes(IMethodSymbol method)
    {
        var attributes = method.GetAttributes();
        var info = new MethodAttributeInfo();

        foreach (var attr in attributes)
        {
            var attrName = attr.AttributeClass?.Name;
            var attrFullName = attr.AttributeClass?.ToDisplayString();

            if (attrName == "QueryAttribute")
            {
                info.HasQuery = true;
                // Try to get SQL from constructor arguments
                if (attr.ConstructorArguments.Length > 0)
                {
                    var ctorArg = attr.ConstructorArguments[0];
                    info.QuerySql = ctorArg.Value?.ToString() ?? string.Empty;
                }
                else
                {
                    // Fallback: Try to get from attribute syntax directly
                    var attributeSyntax = attr.ApplicationSyntaxReference?.GetSyntax();
                    if (attributeSyntax is Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax attrSyntax &&
                        attrSyntax.ArgumentList?.Arguments.Count > 0)
                    {
                        var firstArg = attrSyntax.ArgumentList.Arguments[0];
                        if (firstArg.Expression is Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax literal)
                        {
                            info.QuerySql = literal.Token.ValueText;
                        }
                    }
                }

                info.CommandTimeout = GetNamedArgument<int?>(attr, "CommandTimeout");
                var buffered = GetNamedArgument<bool?>(attr, "Buffered");
                info.Buffered = buffered ?? true;
                var nativeQuery = GetNamedArgument<bool?>(attr, "NativeQuery");
                info.NativeQuery = nativeQuery ?? false;
            }
            else if (attrName == "NamedQueryAttribute" || attrName == "NamedQuery")
            {
                info.HasNamedQuery = true;
                // Get the query name from constructor argument
                if (attr.ConstructorArguments.Length > 0)
                {
                    info.NamedQueryName = attr.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                }
                // Note: The actual query will be looked up from entity metadata during code generation
            }
            else if (attrName == "StoredProcedureAttribute")
            {
                info.HasStoredProcedure = true;
                // Try to get procedure name from constructor arguments
                if (attr.ConstructorArguments.Length > 0)
                {
                    info.ProcedureName = attr.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                }
                else
                {
                    // Fallback: Try to get from attribute syntax directly
                    var attributeSyntax = attr.ApplicationSyntaxReference?.GetSyntax();
                    if (attributeSyntax is Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax attrSyntax &&
                        attrSyntax.ArgumentList?.Arguments.Count > 0)
                    {
                        var firstArg = attrSyntax.ArgumentList.Arguments[0];
                        if (firstArg.Expression is Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax literal)
                        {
                            info.ProcedureName = literal.Token.ValueText;
                        }
                    }
                }

                info.CommandTimeout = GetNamedArgument<int?>(attr, "CommandTimeout");
                info.Schema = GetNamedArgument<string>(attr, "Schema");
            }
            else if (attrName == "MultiMappingAttribute")
            {
                info.HasMultiMapping = true;
                if (attr.ConstructorArguments.Length > 0)
                {
                    info.KeyProperty = attr.ConstructorArguments[0].Value?.ToString();
                }
                info.SplitOn = GetNamedArgument<string>(attr, "SplitOn");
            }
            else if (attrName == "BulkOperationAttribute")
            {
                info.HasBulkOperation = true;
                var batchSize = GetNamedArgument<int?>(attr, "BatchSize");
                info.BatchSize = batchSize ?? 1000;
                var useTransaction = GetNamedArgument<bool?>(attr, "UseTransaction");
                info.UseTransaction = useTransaction ?? true;
                info.CommandTimeout = GetNamedArgument<int?>(attr, "CommandTimeout");
            }
            else if (attrName == "GeneratedMethodAttribute")
            {
                info.HasGeneratedMethod = true;
                info.IncludeNullCheck = GetNamedArgument<bool?>(attr, "IncludeNullCheck") ?? true;
                info.GenerateAsync = GetNamedArgument<bool?>(attr, "GenerateAsync") ?? false;
                info.GenerateSync = GetNamedArgument<bool?>(attr, "GenerateSync") ?? false;
                info.CustomSql = GetNamedArgument<string>(attr, "CustomSql");
                info.IncludeLogging = GetNamedArgument<bool?>(attr, "IncludeLogging") ?? false;
                info.IncludeErrorHandling = GetNamedArgument<bool?>(attr, "IncludeErrorHandling") ?? false;
                info.MethodDescription = GetNamedArgument<string>(attr, "Description");
            }
            else if (attrName == "IgnoreInGenerationAttribute")
            {
                info.IgnoreInGeneration = true;
                if (attr.ConstructorArguments.Length > 0)
                {
                    info.IgnoreReason = attr.ConstructorArguments[0].Value?.ToString();
                }
                else
                {
                    info.IgnoreReason = GetNamedArgument<string>(attr, "Reason");
                }
            }
            else if (attrName == "CustomImplementationAttribute")
            {
                info.HasCustomImplementation = true;
                if (attr.ConstructorArguments.Length > 0)
                {
                    info.ImplementationHint = attr.ConstructorArguments[0].Value?.ToString();
                }
                info.GeneratePartialStub = GetNamedArgument<bool?>(attr, "GeneratePartialStub") ?? true;
                info.ImplementationHint = info.ImplementationHint ?? GetNamedArgument<string>(attr, "ImplementationHint");
                info.CustomImplementationRequired = GetNamedArgument<bool?>(attr, "Required") ?? true;
            }
            else if (attrName == "CacheResultAttribute")
            {
                info.HasCacheResult = true;
                if (attr.ConstructorArguments.Length > 0)
                {
                    info.CacheDuration = (int)(attr.ConstructorArguments[0].Value ?? 300);
                }
                else
                {
                    info.CacheDuration = GetNamedArgument<int?>(attr, "Duration") ?? 300;
                }
                info.CacheKeyPattern = GetNamedArgument<string>(attr, "KeyPattern");
                info.CacheRegion = GetNamedArgument<string>(attr, "Region");
                info.CacheNulls = GetNamedArgument<bool?>(attr, "CacheNulls") ?? false;
                info.CachePriority = GetNamedArgument<int?>(attr, "Priority") ?? 0;
                info.CacheSlidingExpiration = GetNamedArgument<bool?>(attr, "SlidingExpiration") ?? false;
            }
            else if (attrName == "ValidateParametersAttribute")
            {
                info.HasValidateParameters = true;
                info.ThrowOnNull = GetNamedArgument<bool?>(attr, "ThrowOnNull") ?? true;
                info.ValidateStringsNotEmpty = GetNamedArgument<bool?>(attr, "ValidateStringsNotEmpty") ?? false;
                info.ValidateCollectionsNotEmpty = GetNamedArgument<bool?>(attr, "ValidateCollectionsNotEmpty") ?? false;
                info.ValidatePositive = GetNamedArgument<bool?>(attr, "ValidatePositive") ?? false;
                info.ValidationErrorMessage = GetNamedArgument<string>(attr, "ErrorMessage");
            }
            else if (attrName == "RetryOnFailureAttribute")
            {
                info.HasRetryOnFailure = true;
                if (attr.ConstructorArguments.Length > 0)
                {
                    info.MaxRetryAttempts = (int)(attr.ConstructorArguments[0].Value ?? 3);
                }
                else
                {
                    info.MaxRetryAttempts = GetNamedArgument<int?>(attr, "MaxAttempts") ?? 3;
                }
                info.RetryDelayMilliseconds = GetNamedArgument<int?>(attr, "DelayMilliseconds") ?? 100;
                info.RetryExponentialBackoff = GetNamedArgument<bool?>(attr, "ExponentialBackoff") ?? true;
                info.RetryMaxDelayMilliseconds = GetNamedArgument<int?>(attr, "MaxDelayMilliseconds") ?? 30000;
                info.LogRetries = GetNamedArgument<bool?>(attr, "LogRetries") ?? true;
            }
            else if (attrName == "TransactionScopeAttribute")
            {
                info.HasTransactionScope = true;
                if (attr.ConstructorArguments.Length > 0)
                {
                    // IsolationLevel enum value
                    var isolationLevel = attr.ConstructorArguments[0].Value;
                    info.TransactionIsolationLevel = isolationLevel?.ToString() ?? "ReadCommitted";
                }
                info.TransactionRequired = GetNamedArgument<bool?>(attr, "Required") ?? true;
                var isolationFromNamed = GetNamedArgument<int?>(attr, "IsolationLevel");
                if (isolationFromNamed.HasValue)
                {
                    info.TransactionIsolationLevel = ((System.Data.IsolationLevel)isolationFromNamed.Value).ToString();
                }
                info.TransactionTimeoutSeconds = GetNamedArgument<int?>(attr, "TimeoutSeconds") ?? 30;
                info.TransactionAutoRollback = GetNamedArgument<bool?>(attr, "AutoRollbackOnError") ?? true;
                info.TransactionJoinAmbient = GetNamedArgument<bool?>(attr, "JoinAmbientTransaction") ?? true;
            }
            else if (attrName == "PerformanceMonitorAttribute")
            {
                info.HasPerformanceMonitor = true;
                if (attr.ConstructorArguments.Length > 0)
                {
                    info.WarnThresholdMs = (int)(attr.ConstructorArguments[0].Value ?? 0);
                }
                else
                {
                    info.WarnThresholdMs = GetNamedArgument<int?>(attr, "WarnThresholdMs") ?? 0;
                }
                info.IncludeParameters = GetNamedArgument<bool?>(attr, "IncludeParameters") ?? false;
                info.MetricCategory = GetNamedArgument<string>(attr, "Category");
                info.TrackMemory = GetNamedArgument<bool?>(attr, "TrackMemory") ?? false;
                info.TrackQueryCount = GetNamedArgument<bool?>(attr, "TrackQueryCount") ?? false;
                info.MetricName = GetNamedArgument<string>(attr, "MetricName");
            }
            else if (attrName == "AuditAttribute")
            {
                info.HasAudit = true;
                if (attr.ConstructorArguments.Length > 0)
                {
                    info.AuditCategory = attr.ConstructorArguments[0].Value?.ToString() ?? "Data";
                }
                else
                {
                    info.AuditCategory = GetNamedArgument<string>(attr, "Category") ?? "Data";
                }
                info.AuditIncludeOldValue = GetNamedArgument<bool?>(attr, "IncludeOldValue") ?? false;
                info.AuditIncludeNewValue = GetNamedArgument<bool?>(attr, "IncludeNewValue") ?? true;

                // Handle AuditSeverity enum
                var severityValue = GetNamedArgument<int?>(attr, "Severity");
                if (severityValue.HasValue)
                {
                    info.AuditSeverity = severityValue.Value switch
                    {
                        0 => "Low",
                        1 => "Normal",
                        2 => "High",
                        3 => "Critical",
                        _ => "Normal"
                    };
                }
                else
                {
                    info.AuditSeverity = "Normal";
                }

                info.AuditIncludeParameters = GetNamedArgument<bool?>(attr, "IncludeParameters") ?? true;
                info.AuditCaptureUser = GetNamedArgument<bool?>(attr, "CaptureUser") ?? true;
                info.AuditDescription = GetNamedArgument<string>(attr, "Description");
                info.AuditCaptureIpAddress = GetNamedArgument<bool?>(attr, "CaptureIpAddress") ?? false;
            }
        }

        return info;
    }

    private static T? GetNamedArgument<T>(AttributeData attr, string name)
    {
        var namedArg = attr.NamedArguments.FirstOrDefault(arg => arg.Key == name);
        if (namedArg.Value.Value is T value)
        {
            return value;
        }
        return default;
    }

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
            var interfaceCode = GeneratePartialInterface(info);
            context.AddSource($"{repositoryName}Extensions.g.cs", SourceText.From(interfaceCode, Encoding.UTF8));
        }
    }

    private static void GenerateServiceCollectionExtension(SourceProductionContext context, ImmutableArray<RepositoryInfo> repositories)
    {
        if (repositories.IsEmpty)
            return;

        // Group repositories by namespace to organize the output
        var firstNamespace = repositories.FirstOrDefault()?.Namespace ?? "NPA.Generated";

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// This code was generated by NPA.Generators.RepositoryGenerator");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using NPA.Core.Core;");
        sb.AppendLine("using NPA.Core.Repositories;");
        sb.AppendLine();

        // Add namespaces from all repositories
        var namespaces = repositories.Select(r => r.Namespace).Distinct().OrderBy(n => n);
        foreach (var ns in namespaces)
        {
            sb.AppendLine($"using {ns};");
        }

        sb.AppendLine();
        sb.AppendLine("namespace Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Service collection extensions for NPA repositories.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class NPAServiceCollectionExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Adds all NPA-generated repositories to the service collection.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <param name=\"services\">The service collection.</param>");
        sb.AppendLine("    /// <returns>The service collection for chaining.</returns>");
        sb.AppendLine("    public static IServiceCollection AddNPA(this IServiceCollection services)");
        sb.AppendLine("    {");

        // Register EntityManager and BaseRepository
        sb.AppendLine("        // Register core NPA services");
        sb.AppendLine("        services.AddScoped<IEntityManager, EntityManager>();");
        sb.AppendLine();
        sb.AppendLine("        // Register all generated repositories");

        foreach (var repo in repositories.OrderBy(r => r.InterfaceName))
        {
            var implName = GetImplementationName(repo.InterfaceName);
            sb.AppendLine($"        services.AddScoped<{repo.InterfaceName}, {implName}>();");
        }

        sb.AppendLine();
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("NPAServiceCollectionExtensions.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static string GenerateRepositoryCode(RepositoryInfo info)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// This code was generated by NPA.Generators.RepositoryGenerator");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Data;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Dapper;");
        sb.AppendLine("using NPA.Core.Core;");
        sb.AppendLine("using NPA.Core.Repositories;");
        sb.AppendLine("using NPA.Core.Metadata;");

        // Add multi-tenancy using if needed
        if (info.MultiTenantInfo?.IsMultiTenant == true)
        {
            sb.AppendLine("using NPA.Core.MultiTenancy;");
        }

        sb.AppendLine();

        sb.AppendLine($"namespace {info.Namespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Generated implementation of {info.InterfaceName}.");

        // Add multi-tenant documentation if applicable
        if (info.MultiTenantInfo?.IsMultiTenant == true)
        {
            sb.AppendLine($"    /// This repository supports multi-tenancy with automatic tenant filtering.");
            sb.AppendLine($"    /// Tenant property: {info.MultiTenantInfo.TenantIdProperty}");
            if (info.MultiTenantInfo.AllowCrossTenantQueries)
            {
                sb.AppendLine($"    /// Cross-tenant queries: Allowed (use WithoutTenantFilterAsync for admin operations)");
            }
            else
            {
                sb.AppendLine($"    /// Cross-tenant queries: Not allowed");
            }
        }

        sb.AppendLine($"    /// </summary>");

        // Build the interface list - add Partial interface if relationships exist
        var interfaces = info.FullInterfaceName;
        if (info.Relationships.Count > 0)
        {
            var partialInterfaceName = info.InterfaceName + "Partial";
            interfaces = $"{info.FullInterfaceName}, {partialInterfaceName}";
        }

        sb.AppendLine($"    public class {GetImplementationName(info.InterfaceName)} : BaseRepository<{info.EntityType}, {info.KeyType}>, {interfaces}");
        sb.AppendLine("    {");

        // Generate constructor
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Initializes a new instance of the {GetImplementationName(info.InterfaceName)} class.");
        sb.AppendLine($"        /// </summary>");

        if (info.MultiTenantInfo?.IsMultiTenant == true)
        {
            // Constructor with ITenantProvider
            sb.AppendLine("        public " + GetImplementationName(info.InterfaceName) + "(IDbConnection connection, IEntityManager entityManager, IMetadataProvider metadataProvider, ITenantProvider? tenantProvider = null)");
            sb.AppendLine("            : base(connection, entityManager, metadataProvider, tenantProvider)");
        }
        else
        {
            // Constructor without ITenantProvider
            sb.AppendLine("        public " + GetImplementationName(info.InterfaceName) + "(IDbConnection connection, IEntityManager entityManager, IMetadataProvider metadataProvider)");
            sb.AppendLine("            : base(connection, entityManager, metadataProvider)");
        }

        sb.AppendLine("        {");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate method implementations
        foreach (var method in info.Methods)
        {
            sb.AppendLine(GenerateMethodImplementation(method, info));
            sb.AppendLine();
        }

        // Generate composite key overloads if entity has composite key
        if (info.HasCompositeKey)
        {
            sb.AppendLine(GenerateCompositeKeyMethods(info));
        }

        // Generate many-to-many relationship methods
        if (info.ManyToManyRelationships.Count > 0)
        {
            sb.AppendLine(GenerateManyToManyMethods(info));
        }

        // Generate relationship-aware methods
        if (info.Relationships != null && info.Relationships.Count > 0)
        {
            sb.AppendLine(GenerateRelationshipAwareMethods(info));
        }

        // Generate eager loading overrides
        if (info.HasEagerRelationships)
        {
            sb.AppendLine(GenerateEagerLoadingOverrides(info));
        }

        // Generate cascade operation overrides
        if (info.HasCascadeRelationships)
        {
            sb.AppendLine(GenerateCascadeOperationOverrides(info));
        }

        // Generate orphan removal override for UpdateAsync
        if (info.HasOrphanRemovalRelationships)
        {
            sb.AppendLine(GenerateOrphanRemovalUpdateOverride(info));
        }

        // Generate property-to-column mapping helper for sorting
        sb.AppendLine(GeneratePropertyColumnMapping(info));

        // Generate relationship query methods
        if (info.Relationships is { Count: > 0 })
        {
            sb.AppendLine(GenerateRelationshipQueryMethods(info));
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateCompositeKeyMethods(RepositoryInfo info)
    {
        var sb = new StringBuilder();
        var entityType = info.EntityType;

        sb.AppendLine("        #region Composite Key Methods");
        sb.AppendLine();

        // GetByIdAsync(CompositeKey)
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Gets an entity by its composite key asynchronously.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <param name=\"key\">The composite key.</param>");
        sb.AppendLine("        /// <returns>The entity if found; otherwise, null.</returns>");
        sb.AppendLine($"        public async Task<{entityType}?> GetByIdAsync(NPA.Core.Core.CompositeKey key)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (key == null) throw new ArgumentNullException(nameof(key));");
        sb.AppendLine($"            return await _entityManager.FindAsync<{entityType}>(key);");
        sb.AppendLine("        }");
        sb.AppendLine();

        // DeleteAsync(CompositeKey)
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Deletes an entity by its composite key asynchronously.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <param name=\"key\">The composite key.</param>");
        sb.AppendLine($"        public async Task DeleteAsync(NPA.Core.Core.CompositeKey key)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (key == null) throw new ArgumentNullException(nameof(key));");
        sb.AppendLine($"            await _entityManager.RemoveAsync<{entityType}>(key);");
        sb.AppendLine("        }");
        sb.AppendLine();

        // ExistsAsync(CompositeKey)
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Checks if an entity exists by its composite key asynchronously.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <param name=\"key\">The composite key.");
        sb.AppendLine("        /// <returns>True if the entity exists; otherwise, false.</returns>");
        sb.AppendLine($"        public async Task<bool> ExistsAsync(NPA.Core.Core.CompositeKey key)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (key == null) throw new ArgumentNullException(nameof(key));");
        sb.AppendLine("            var entity = await GetByIdAsync(key);");
        sb.AppendLine("            return entity != null;");
        sb.AppendLine("        }");
        sb.AppendLine();

        // FindByCompositeKey (individual parameters)
        var keyParams = string.Join(", ", info.CompositeKeyProperties.Select((prop, i) => $"object {ToCamelCase(prop)}"));
        var keySetters = string.Join("\n            ", info.CompositeKeyProperties.Select(prop => $"key.SetValue(\"{prop}\", {ToCamelCase(prop)});"));

        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Finds an entity by its composite key components asynchronously.");
        sb.AppendLine("        /// </summary>");
        foreach (var prop in info.CompositeKeyProperties)
        {
            sb.AppendLine($"        /// <param name=\"{ToCamelCase(prop)}\">The {prop} component of the composite key.</param>");
        }
        sb.AppendLine("        /// <returns>The entity if found; otherwise, null.</returns>");
        sb.AppendLine($"        public async Task<{entityType}?> FindByCompositeKeyAsync({keyParams})");
        sb.AppendLine("        {");
        sb.AppendLine("            var key = new NPA.Core.Core.CompositeKey();");
        sb.AppendLine($"            {keySetters}");
        sb.AppendLine("            return await GetByIdAsync(key);");
        sb.AppendLine("        }");
        sb.AppendLine();

        sb.AppendLine("        #endregion");

        return sb.ToString();
    }

    private static string GenerateManyToManyMethods(RepositoryInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("        #region Many-to-Many Relationship Methods");
        sb.AppendLine();

        foreach (var relationship in info.ManyToManyRelationships)
        {
            var entityName = info.EntityType.Split('.').Last();
            var relatedName = relationship.CollectionElementType.Split('.').Last();
            var joinTable = string.IsNullOrEmpty(relationship.JoinTableSchema)
                ? relationship.JoinTableName
                : $"{relationship.JoinTableSchema}.{relationship.JoinTableName}";

            // Determine key columns with defaults if not specified
            var ownerKeyColumn = relationship.JoinColumns.FirstOrDefault() ?? $"{entityName}Id";
            var targetKeyColumn = relationship.InverseJoinColumns.FirstOrDefault() ?? $"{relatedName}Id";

            // Get{Related}Async method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets all {relationship.PropertyName} for a {entityName} asynchronously.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{ToCamelCase(entityName)}Id\">The {entityName} identifier.</param>");
            sb.AppendLine($"        /// <returns>A collection of {relatedName} entities.</returns>");
            sb.AppendLine($"        public async Task<IEnumerable<{relationship.CollectionElementType}>> Get{relationship.PropertyName}Async({info.KeyType} {ToCamelCase(entityName)}Id)");
            sb.AppendLine("        {");
            // Get the related entity's key property name from metadata
            // CollectionElementType is the full type name (e.g., "Phase7Demo.Tag")
            // Extract just the type name (e.g., "Tag")
            var relatedEntityTypeName = relationship.CollectionElementType.Split('.').Last();
            var relatedKeyPropertyName = GetKeyPropertyName(info, relatedEntityTypeName);
            sb.AppendLine($"            var sql = @\"");
            sb.AppendLine($"                SELECT r.*");
            sb.AppendLine($"                FROM {joinTable} jt");
            sb.AppendLine($"                INNER JOIN {relatedName} r ON jt.{targetKeyColumn} = r.{relatedKeyPropertyName}");
            sb.AppendLine($"                WHERE jt.{ownerKeyColumn} = @{entityName}Id\";");
            sb.AppendLine();
            sb.AppendLine($"            return await _connection.QueryAsync<{relationship.CollectionElementType}>(");
            sb.AppendLine($"                sql,");
            sb.AppendLine($"                new {{ {entityName}Id = {ToCamelCase(entityName)}Id }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Add{Related}Async method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Adds a relationship between a {entityName} and a {relatedName} asynchronously.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{ToCamelCase(entityName)}Id\">The {entityName} identifier.</param>");
            sb.AppendLine($"        /// <param name=\"{ToCamelCase(relatedName)}Id\">The {relatedName} identifier.</param>");
            sb.AppendLine($"        public async Task Add{relatedName}Async({info.KeyType} {ToCamelCase(entityName)}Id, {info.KeyType} {ToCamelCase(relatedName)}Id)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"INSERT INTO {joinTable} ({ownerKeyColumn}, {targetKeyColumn}) VALUES (@{entityName}Id, @{relatedName}Id)\";");
            sb.AppendLine();
            sb.AppendLine("            await _connection.ExecuteAsync(");
            sb.AppendLine("                sql,");
            sb.AppendLine($"                new {{ {entityName}Id = {ToCamelCase(entityName)}Id, {relatedName}Id = {ToCamelCase(relatedName)}Id }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Remove{Related}Async method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Removes a relationship between a {entityName} and a {relatedName} asynchronously.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{ToCamelCase(entityName)}Id\">The {entityName} identifier.</param>");
            sb.AppendLine($"        /// <param name=\"{ToCamelCase(relatedName)}Id\">The {relatedName} identifier.</param>");
            sb.AppendLine($"        public async Task Remove{relatedName}Async({info.KeyType} {ToCamelCase(entityName)}Id, {info.KeyType} {ToCamelCase(relatedName)}Id)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"DELETE FROM {joinTable} WHERE {ownerKeyColumn} = @{entityName}Id AND {targetKeyColumn} = @{relatedName}Id\";");
            sb.AppendLine();
            sb.AppendLine("            await _connection.ExecuteAsync(");
            sb.AppendLine("                sql,");
            sb.AppendLine($"                new {{ {entityName}Id = {ToCamelCase(entityName)}Id, {relatedName}Id = {ToCamelCase(relatedName)}Id }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Has{Related}Async method (existence check)
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Checks if a relationship exists between a {entityName} and a {relatedName} asynchronously.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{ToCamelCase(entityName)}Id\">The {entityName} identifier.</param>");
            sb.AppendLine($"        /// <param name=\"{ToCamelCase(relatedName)}Id\">The {relatedName} identifier.</param>");
            sb.AppendLine($"        /// <returns>True if the relationship exists; otherwise, false.</returns>");
            sb.AppendLine($"        public async Task<bool> Has{relatedName}Async({info.KeyType} {ToCamelCase(entityName)}Id, {info.KeyType} {ToCamelCase(relatedName)}Id)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"SELECT COUNT(1) FROM {joinTable} WHERE {ownerKeyColumn} = @{entityName}Id AND {targetKeyColumn} = @{relatedName}Id\";");
            sb.AppendLine();
            sb.AppendLine("            var count = await _connection.ExecuteScalarAsync<int>(");
            sb.AppendLine("                sql,");
            sb.AppendLine($"                new {{ {entityName}Id = {ToCamelCase(entityName)}Id, {relatedName}Id = {ToCamelCase(relatedName)}Id }});");
            sb.AppendLine();
            sb.AppendLine("            return count > 0;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        sb.AppendLine("        #endregion");

        return sb.ToString();
    }

    private static string ToCamelCase(string pascalCase)
    {
        if (string.IsNullOrEmpty(pascalCase) || char.IsLower(pascalCase[0]))
            return pascalCase;

        return char.ToLower(pascalCase[0]) + pascalCase.Substring(1);
    }

    private static string GetImplementationName(string interfaceName)
    {
        // IUserRepository -> UserRepositoryImplementation
        if (interfaceName.StartsWith("I") && interfaceName.Length > 1 && char.IsUpper(interfaceName[1]))
            return interfaceName.Substring(1) + "Implementation";

        return interfaceName + "Implementation";
    }

    private static string GenerateMethodImplementation(MethodInfo method, RepositoryInfo info)
    {
        var sb = new StringBuilder();

        // Add XML documentation
        sb.AppendLine("        /// <inheritdoc />");

        // Method signature - add async if return type is Task
        var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"));
        var isAsync = method.ReturnType.StartsWith("System.Threading.Tasks.Task");
        var asyncModifier = isAsync ? "async " : "";

        sb.AppendLine($"        public {asyncModifier}{method.ReturnType} {method.Name}({parameters})");
        sb.AppendLine("        {");

        // Generate implementation based on attributes or conventions
        var implementation = GenerateMethodBody(method, info);
        sb.Append(implementation);

        sb.AppendLine("        }");

        return sb.ToString();
    }

    private static string GenerateMethodBody(MethodInfo method, RepositoryInfo info)
    {
        var sb = new StringBuilder();
        var attrs = method.Attributes;

        // Priority 1: Check if method name matches a NamedQuery (auto-detection)
        var namedQueryName = TryFindMatchingNamedQuery(method, info);
        if (namedQueryName != null)
        {
            // Use the matched named query (highest priority)
            sb.Append(GenerateNamedQueryMethodBody(method, info, namedQueryName));
        }
        // Priority 2: Explicit [NamedQuery] attribute on method
        else if (attrs.HasNamedQuery && !string.IsNullOrEmpty(attrs.NamedQueryName))
        {
            sb.Append(GenerateNamedQueryMethodBody(method, info, attrs.NamedQueryName!));
        }
        // Priority 3: [Query] attribute
        else if (attrs.HasQuery)
        {
            sb.Append(GenerateQueryMethodBody(method, info, attrs));
        }
        // Priority 4: [StoredProcedure] attribute
        else if (attrs.HasStoredProcedure)
        {
            sb.Append(GenerateStoredProcedureMethodBody(method, info.EntityType, attrs));
        }
        // Priority 5: [BulkOperation] attribute
        else if (attrs.HasBulkOperation)
        {
            sb.Append(GenerateBulkOperationMethodBody(method, info.EntityType, attrs));
        }
        // Priority 6: Convention-based generation
        else if (method.Symbol != null)
        {
            // Use convention-based generation
            var convention = MethodConventionAnalyzer.AnalyzeMethod(method.Symbol);
            sb.Append(GenerateConventionBasedMethodBody(method, info, convention));
        }
        else
        {
            // Fallback to simple conventions
            sb.Append(GenerateSimpleConventionBody(method, info));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Tries to find a matching named query based on method name conventions.
    /// Looks for patterns like: EntityName.MethodName or just MethodName
    /// </summary>
    private static string? TryFindMatchingNamedQuery(MethodInfo method, RepositoryInfo info)
    {
        // Get entity metadata to access named queries
        var entityMetadata = info.EntityMetadata;
        if (entityMetadata == null || entityMetadata.NamedQueries == null || !entityMetadata.NamedQueries.Any())
        {
            return null;
        }

        var methodName = method.Name;
        var entityName = info.EntityType;

        // Extract simple entity name without namespace
        var simpleEntityName = entityName.Contains(".")
            ? entityName.Substring(entityName.LastIndexOf('.') + 1)
            : entityName;

        // Try different naming conventions:
        // 1. EntityName.MethodName (e.g., "Order.FindRecentOrdersAsync")
        var fullName = $"{simpleEntityName}.{methodName}";
        if (entityMetadata.NamedQueries.Any(nq => nq.Name == fullName))
        {
            return fullName;
        }

        // 2. Just MethodName (e.g., "FindRecentOrdersAsync")
        if (entityMetadata.NamedQueries.Any(nq => nq.Name == methodName))
        {
            return methodName;
        }

        // 3. Try without "Async" suffix if present
        if (methodName.EndsWith("Async"))
        {
            var nameWithoutAsync = methodName.Substring(0, methodName.Length - 5);

            // EntityName.MethodNameWithoutAsync
            var fullNameWithoutAsync = $"{simpleEntityName}.{nameWithoutAsync}";
            if (entityMetadata.NamedQueries.Any(nq => nq.Name == fullNameWithoutAsync))
            {
                return fullNameWithoutAsync;
            }

            // Just MethodNameWithoutAsync
            if (entityMetadata.NamedQueries.Any(nq => nq.Name == nameWithoutAsync))
            {
                return nameWithoutAsync;
            }
        }

        return null;
    }

    private static string GenerateQueryMethodBody(MethodInfo method, RepositoryInfo info, MethodAttributeInfo attrs)
    {
        var sb = new StringBuilder();
        var isAsync = method.ReturnType.StartsWith("System.Threading.Tasks.Task");
        var paramObj = GenerateParameterObject(method.Parameters);

        // Use native SQL if NativeQuery is true, otherwise convert CPQL to SQL
        string sql;
        if (attrs.NativeQuery)
        {
            // Native SQL - use as-is without conversion
            sql = attrs.QuerySql ?? string.Empty;
        }
        else
        {
            // Convert CPQL to SQL using entity metadata dictionary
            sql = info.EntitiesMetadata.Count > 0
                ? CpqlToSqlConverter.ConvertToSql(attrs.QuerySql ?? string.Empty, info.EntitiesMetadata)
                : CpqlToSqlConverter.ConvertToSql(attrs.QuerySql ?? string.Empty);
        }

        if (attrs.HasMultiMapping)
        {
            // Multi-mapping query using Dapper
            var innerType = GetInnerType(method.ReturnType);
            var isCollection = method.ReturnType.Contains("IEnumerable") || method.ReturnType.Contains("List") ||
                             method.ReturnType.Contains("ICollection") || method.ReturnType.Contains("[]");

            sb.AppendLine($"            var sql = @\"{sql}\";");
            sb.AppendLine($"            var splitOn = \"{attrs.SplitOn ?? "Id"}\";");
            sb.AppendLine();

            if (isCollection)
            {
                // Collection result with multi-mapping
                sb.AppendLine($"            var lookup = new Dictionary<object, {innerType}>();");
                sb.AppendLine();

                if (isAsync)
                {
                    sb.AppendLine($"            await _connection.QueryAsync<{innerType}, dynamic, {innerType}>(");
                    sb.AppendLine($"                sql,");
                    sb.AppendLine($"                (main, related) => {{");
                    sb.AppendLine($"                    var key = main.{attrs.KeyProperty ?? "Id"};");
                    sb.AppendLine($"                    if (!lookup.TryGetValue(key, out var existing))");
                    sb.AppendLine($"                    {{");
                    sb.AppendLine($"                        lookup[key] = main;");
                    sb.AppendLine($"                    }}");
                    sb.AppendLine($"                    // Note: Relationship population should be customized based on your entities");
                    sb.AppendLine($"                    return main;");
                    sb.AppendLine($"                }},");
                    sb.AppendLine($"                {paramObj},");
                    sb.AppendLine($"                splitOn: splitOn);");
                    sb.AppendLine();

                    var conversion = GetCollectionConversion(method.ReturnType);
                    if (!string.IsNullOrEmpty(conversion))
                    {
                        sb.AppendLine($"            return lookup.Values.{conversion};");
                    }
                    else
                    {
                        sb.AppendLine($"            return lookup.Values;");
                    }
                }
                else
                {
                    sb.AppendLine($"            _connection.Query<{innerType}, dynamic, {innerType}>(");
                    sb.AppendLine($"                sql,");
                    sb.AppendLine($"                (main, related) => {{");
                    sb.AppendLine($"                    var key = main.{attrs.KeyProperty ?? "Id"};");
                    sb.AppendLine($"                    if (!lookup.TryGetValue(key, out var existing))");
                    sb.AppendLine($"                    {{");
                    sb.AppendLine($"                        lookup[key] = main;");
                    sb.AppendLine($"                    }}");
                    sb.AppendLine($"                    // Note: Relationship population should be customized based on your entities");
                    sb.AppendLine($"                    return main;");
                    sb.AppendLine($"                }},");
                    sb.AppendLine($"                {paramObj},");
                    sb.AppendLine($"                splitOn: splitOn);");
                    sb.AppendLine();

                    var conversion = GetCollectionConversion(method.ReturnType);
                    if (!string.IsNullOrEmpty(conversion))
                    {
                        sb.AppendLine($"            return lookup.Values.{conversion};");
                    }
                    else
                    {
                        sb.AppendLine($"            return lookup.Values;");
                    }
                }
            }
            else
            {
                // Single result with multi-mapping
                if (isAsync)
                {
                    sb.AppendLine($"            var result = await _connection.QueryAsync<{innerType}, dynamic, {innerType}>(");
                    sb.AppendLine($"                sql,");
                    sb.AppendLine($"                (main, related) => {{");
                    sb.AppendLine($"                    // Note: Relationship population should be customized based on your entities");
                    sb.AppendLine($"                    return main;");
                    sb.AppendLine($"                }},");
                    sb.AppendLine($"                {paramObj},");
                    sb.AppendLine($"                splitOn: splitOn);");
                    sb.AppendLine();
                    sb.AppendLine($"            return result.FirstOrDefault();");
                }
                else
                {
                    sb.AppendLine($"            var result = _connection.Query<{innerType}, dynamic, {innerType}>(");
                    sb.AppendLine($"                sql,");
                    sb.AppendLine($"                (main, related) => {{");
                    sb.AppendLine($"                    // Note: Relationship population should be customized based on your entities");
                    sb.AppendLine($"                    return main;");
                    sb.AppendLine($"                }},");
                    sb.AppendLine($"                {paramObj},");
                    sb.AppendLine($"                splitOn: splitOn);");
                    sb.AppendLine();
                    sb.AppendLine($"            return result.FirstOrDefault();");
                }
            }
        }
        else if (method.ReturnType.Contains("IEnumerable") || method.ReturnType.Contains("ICollection") ||
                 method.ReturnType.Contains("List") || method.ReturnType.Contains("[]") ||
                 method.ReturnType.Contains("HashSet") || method.ReturnType.Contains("ISet") ||
                 method.ReturnType.Contains("IReadOnly"))
        {
            // Returns collection
            sb.AppendLine($"            var sql = @\"{sql}\";");
            var conversion = GetCollectionConversion(method.ReturnType);

            if (isAsync)
            {
                if (!string.IsNullOrEmpty(conversion))
                {
                    sb.AppendLine($"            var result = await _connection.QueryAsync<{GetInnerType(method.ReturnType)}>(sql, {paramObj});");
                    sb.AppendLine($"            return result.{conversion};");
                }
                else
                {
                    sb.AppendLine($"            return await _connection.QueryAsync<{GetInnerType(method.ReturnType)}>(sql, {paramObj});");
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(conversion))
                {
                    sb.AppendLine($"            var result = _connection.Query<{GetInnerType(method.ReturnType)}>(sql, {paramObj});");
                    sb.AppendLine($"            return result.{conversion};");
                }
                else
                {
                    sb.AppendLine($"            return _connection.Query<{GetInnerType(method.ReturnType)}>(sql, {paramObj});");
                }
            }
        }
        else if (method.ReturnType.Contains("int") || method.ReturnType.Contains("long"))
        {
            // Returns scalar (count, affected rows, etc.)
            // Detect if it's INSERT/UPDATE/DELETE based on SQL query
            sb.AppendLine($"            var sql = @\"{sql}\";");

            var isModification = sql.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) ||
                                sql.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) ||
                                sql.TrimStart().StartsWith("DELETE", StringComparison.OrdinalIgnoreCase);

            if (isModification)
            {
                // INSERT/UPDATE/DELETE - returns affected row count
                if (isAsync)
                {
                    sb.AppendLine($"            return await _connection.ExecuteAsync(sql, {paramObj});");
                }
                else
                {
                    sb.AppendLine($"            return _connection.Execute(sql, {paramObj});");
                }
            }
            else
            {
                // SELECT COUNT, SUM, etc. - returns scalar value
                if (isAsync)
                {
                    sb.AppendLine($"            return await _connection.ExecuteScalarAsync<{GetInnerType(method.ReturnType)}>(sql, {paramObj});");
                }
                else
                {
                    sb.AppendLine($"            return _connection.ExecuteScalar<{GetInnerType(method.ReturnType)}>(sql, {paramObj});");
                }
            }
        }
        else if (method.ReturnType.Contains("bool"))
        {
            // Returns boolean (exists check)
            sb.AppendLine($"            var sql = @\"{sql}\";");
            if (isAsync)
            {
                sb.AppendLine($"            var result = await _connection.ExecuteScalarAsync<int>(sql, {paramObj});");
            }
            else
            {
                sb.AppendLine($"            var result = _connection.ExecuteScalar<int>(sql, {paramObj});");
            }
            sb.AppendLine($"            return result > 0;");
        }
        else
        {
            // Returns single entity or nullable
            sb.AppendLine($"            var sql = @\"{sql}\";");
            if (isAsync)
            {
                sb.AppendLine($"            return await _connection.QueryFirstOrDefaultAsync<{GetInnerType(method.ReturnType)}>(sql, {paramObj});");
            }
            else
            {
                sb.AppendLine($"            return _connection.QueryFirstOrDefault<{GetInnerType(method.ReturnType)}>(sql, {paramObj});");
            }
        }

        return sb.ToString();
    }

    private static string GenerateNamedQueryMethodBody(MethodInfo method, RepositoryInfo info, string namedQueryName)
    {
        // Find the named query from entity metadata
        var namedQuery = info.EntityMetadata?.NamedQueries
            ?.FirstOrDefault(nq => nq.Name == namedQueryName);

        if (namedQuery == null)
        {
            // Fallback to convention-based if named query not found
            // This shouldn't happen but provides a safety net
            return GenerateSimpleConventionBody(method, info);
        }

        var sb = new StringBuilder();

        // Generate comment indicating this uses a named query
        sb.AppendLine($"            // Using named query: {namedQueryName}");
        sb.AppendLine();

        // Determine the SQL to use
        string sql;
        if (namedQuery.NativeQuery)
        {
            // Native SQL - use as-is without conversion
            sql = namedQuery.Query;
        }
        else
        {
            // Convert CPQL to SQL using entity metadata dictionary
            sql = info.EntitiesMetadata.Count > 0
                ? CpqlToSqlConverter.ConvertToSql(namedQuery.Query, info.EntitiesMetadata)
                : CpqlToSqlConverter.ConvertToSql(namedQuery.Query);
        }

        // Generate parameter object
        var paramObj = GenerateParameterObject(method.Parameters);

        // Execute query based on return type (same logic as GenerateQueryMethodBody)
        var isAsync = method.ReturnType.StartsWith("System.Threading.Tasks.Task");

        if (method.ReturnType.Contains("IEnumerable") || method.ReturnType.Contains("ICollection") ||
            method.ReturnType.Contains("List") || method.ReturnType.Contains("[]") ||
            method.ReturnType.Contains("HashSet") || method.ReturnType.Contains("ISet") ||
            method.ReturnType.Contains("IReadOnly"))
        {
            // Returns collection
            sb.AppendLine($"            var sql = @\"{sql}\";");
            if (isAsync)
            {
                sb.AppendLine($"            return await _connection.QueryAsync<{GetInnerType(method.ReturnType)}>(sql, {paramObj});");
            }
            else
            {
                sb.AppendLine($"            return _connection.Query<{GetInnerType(method.ReturnType)}>(sql, {paramObj});");
            }
        }
        else if (method.ReturnType.Contains(info.EntityType) || method.ReturnType.EndsWith("?"))
        {
            // Returns single entity
            sb.AppendLine($"            var sql = @\"{sql}\";");
            if (isAsync)
            {
                sb.AppendLine($"            return await _connection.QueryFirstOrDefaultAsync<{GetInnerType(method.ReturnType)}>(sql, {paramObj});");
            }
            else
            {
                sb.AppendLine($"            return _connection.QueryFirstOrDefault<{GetInnerType(method.ReturnType)}>(sql, {paramObj});");
            }
        }
        else if (method.ReturnType.Contains("int") || method.ReturnType.Contains("long"))
        {
            // Returns scalar (count, affected rows, etc.)
            sb.AppendLine($"            var sql = @\"{sql}\";");

            var isModification = sql.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) ||
                                sql.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) ||
                                sql.TrimStart().StartsWith("DELETE", StringComparison.OrdinalIgnoreCase);

            if (isModification)
            {
                // INSERT/UPDATE/DELETE - returns affected row count
                if (isAsync)
                {
                    sb.AppendLine($"            return await _connection.ExecuteAsync(sql, {paramObj});");
                }
                else
                {
                    sb.AppendLine($"            return _connection.Execute(sql, {paramObj});");
                }
            }
            else
            {
                // SELECT COUNT, SUM, etc. - returns scalar value
                if (isAsync)
                {
                    sb.AppendLine($"            return await _connection.ExecuteScalarAsync<{GetInnerType(method.ReturnType)}>(sql, {paramObj});");
                }
                else
                {
                    sb.AppendLine($"            return _connection.ExecuteScalar<{GetInnerType(method.ReturnType)}>(sql, {paramObj});");
                }
            }
        }
        else if (method.ReturnType.Contains("bool"))
        {
            // Returns boolean (exists check)
            sb.AppendLine($"            var sql = @\"{sql}\";");
            if (isAsync)
            {
                sb.AppendLine($"            var result = await _connection.ExecuteScalarAsync<int>(sql, {paramObj});");
            }
            else
            {
                sb.AppendLine($"            var result = _connection.ExecuteScalar<int>(sql, {paramObj});");
            }
            sb.AppendLine($"            return result > 0;");
        }
        else
        {
            // Returns single entity or nullable
            sb.AppendLine($"            var sql = @\"{sql}\";");
            if (isAsync)
            {
                sb.AppendLine($"            return await _connection.QueryFirstOrDefaultAsync<{GetInnerType(method.ReturnType)}>(sql, {paramObj});");
            }
            else
            {
                sb.AppendLine($"            return _connection.QueryFirstOrDefault<{GetInnerType(method.ReturnType)}>(sql, {paramObj});");
            }
        }

        return sb.ToString();
    }

    private static string GenerateStoredProcedureMethodBody(MethodInfo method, string entityType, MethodAttributeInfo attrs)
    {
        var sb = new StringBuilder();
        var isAsync = method.ReturnType.StartsWith("System.Threading.Tasks.Task");
        var procName = attrs.Schema != null ? $"{attrs.Schema}.{attrs.ProcedureName}" : attrs.ProcedureName;
        var paramObj = GenerateParameterObject(method.Parameters);

        if (method.ReturnType.Contains("IEnumerable") || method.ReturnType.Contains("ICollection") ||
            method.ReturnType.Contains("List") || method.ReturnType.Contains("[]") ||
            method.ReturnType.Contains("HashSet") || method.ReturnType.Contains("ISet") ||
            method.ReturnType.Contains("IReadOnly"))
        {
            // Returns collection
            var conversion = GetCollectionConversion(method.ReturnType);

            if (isAsync)
            {
                if (!string.IsNullOrEmpty(conversion))
                {
                    sb.AppendLine($"            var result = await _connection.QueryAsync<{GetInnerType(method.ReturnType)}>(\"{procName}\", {paramObj}, commandType: CommandType.StoredProcedure);");
                    sb.AppendLine($"            return result.{conversion};");
                }
                else
                {
                    sb.AppendLine($"            return await _connection.QueryAsync<{GetInnerType(method.ReturnType)}>(\"{procName}\", {paramObj}, commandType: CommandType.StoredProcedure);");
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(conversion))
                {
                    sb.AppendLine($"            var result = _connection.Query<{GetInnerType(method.ReturnType)}>(\"{procName}\", {paramObj}, commandType: CommandType.StoredProcedure);");
                    sb.AppendLine($"            return result.{conversion};");
                }
                else
                {
                    sb.AppendLine($"            return _connection.Query<{GetInnerType(method.ReturnType)}>(\"{procName}\", {paramObj}, commandType: CommandType.StoredProcedure);");
                }
            }
        }
        else if (method.ReturnType.Contains("int") || method.ReturnType.Contains("long") || method.ReturnType.Contains("bool"))
        {
            // Returns scalar or execution result
            if (isAsync)
            {
                sb.AppendLine($"            return await _connection.ExecuteAsync(\"{procName}\", {paramObj}, commandType: CommandType.StoredProcedure);");
            }
            else
            {
                sb.AppendLine($"            return _connection.Execute(\"{procName}\", {paramObj}, commandType: CommandType.StoredProcedure);");
            }
        }
        else
        {
            // Returns single entity
            if (isAsync)
            {
                sb.AppendLine($"            return await _connection.QueryFirstOrDefaultAsync<{GetInnerType(method.ReturnType)}>(\"{procName}\", {paramObj}, commandType: CommandType.StoredProcedure);");
            }
            else
            {
                sb.AppendLine($"            return _connection.QueryFirstOrDefault<{GetInnerType(method.ReturnType)}>(\"{procName}\", {paramObj}, commandType: CommandType.StoredProcedure);");
            }
        }

        return sb.ToString();
    }

    private static string GenerateBulkOperationMethodBody(MethodInfo method, string entityType, MethodAttributeInfo attrs)
    {
        var sb = new StringBuilder();
        var isAsync = method.ReturnType.StartsWith("System.Threading.Tasks.Task");
        var entityParam = method.Parameters.FirstOrDefault(p => p.Type.Contains("IEnumerable"));

        if (entityParam != null)
        {
            var collectionType = GetInnerType(entityParam.Type);

            if (method.Name.Contains("Insert") || method.Name.Contains("Add") || method.Name.Contains("Create"))
            {
                if (isAsync)
                {
                    sb.AppendLine($"            return await _entityManager.BulkInsertAsync({entityParam.Name});");
                }
                else
                {
                    sb.AppendLine($"            return _entityManager.BulkInsert({entityParam.Name});");
                }
            }
            else if (method.Name.Contains("Update") || method.Name.Contains("Modify"))
            {
                if (isAsync)
                {
                    sb.AppendLine($"            return await _entityManager.BulkUpdateAsync({entityParam.Name});");
                }
                else
                {
                    sb.AppendLine($"            return _entityManager.BulkUpdate({entityParam.Name});");
                }
            }
            else if (method.Name.Contains("Delete") || method.Name.Contains("Remove"))
            {
                if (isAsync)
                {
                    sb.AppendLine($"            return await _entityManager.BulkDeleteAsync({entityParam.Name});");
                }
                else
                {
                    sb.AppendLine($"            return _entityManager.BulkDelete({entityParam.Name});");
                }
            }
            else
            {
                sb.AppendLine($"            throw new NotImplementedException(\"Bulk operation type not recognized from method name\");");
            }
        }
        else
        {
            sb.AppendLine($"            throw new NotImplementedException(\"Bulk operation requires an IEnumerable parameter\");");
        }

        return sb.ToString();
    }

    private static string GenerateConventionBasedMethodBody(MethodInfo method, RepositoryInfo info, MethodConvention convention)
    {
        var sb = new StringBuilder();
        var isAsync = method.ReturnType.StartsWith("System.Threading.Tasks.Task");
        var tableName = GetTableName(info.EntityType);

        switch (convention.QueryType)
        {
            case QueryType.Select:
                sb.Append(GenerateSelectQuery(method, info, tableName, convention, isAsync));
                break;
            case QueryType.Count:
                sb.Append(GenerateCountQuery(method, info, tableName, convention, isAsync));
                break;
            case QueryType.Exists:
                sb.Append(GenerateExistsQuery(method, info, tableName, convention, isAsync));
                break;
            case QueryType.Delete:
                sb.Append(GenerateDeleteQuery(method, info, tableName, convention, isAsync));
                break;
            case QueryType.Update:
            case QueryType.Insert:
                sb.Append(GenerateModificationQuery(method, info.EntityType, convention, isAsync));
                break;
            default:
                sb.AppendLine($"            throw new NotImplementedException(\"Method {method.Name} requires manual implementation\");");
                break;
        }

        return sb.ToString();
    }

    private static string GenerateSelectQuery(MethodInfo method, RepositoryInfo info, string tableName, MethodConvention convention, bool isAsync)
    {
        var sb = new StringBuilder();

        // Determine the actual return type - use method return type if different from entity
        var returnType = GetInnerType(method.ReturnType);
        var queryType = string.IsNullOrEmpty(returnType) ? info.EntityType : returnType;

        var whereClause = BuildWhereClause(convention.PropertyNames, convention.PropertySeparators, convention.Parameters, info.EntityMetadata);
        var orderByClause = BuildOrderByClause(convention.OrderByProperties, info.EntityMetadata);
        var paramObj = GenerateParameterObject(convention.Parameters);
        var hasParameters = !string.IsNullOrEmpty(paramObj) && paramObj != "null";

        // Build the full SQL query with optional DISTINCT and LIMIT
        var columnList = BuildColumnList(info.EntityMetadata);
        var selectClause = convention.HasDistinct ? $"SELECT DISTINCT {columnList}" : $"SELECT {columnList}";
        var sqlBuilder = new StringBuilder($"{selectClause} FROM {tableName}");

        if (!string.IsNullOrEmpty(whereClause))
        {
            sqlBuilder.Append($" WHERE {whereClause}");
        }

        if (!string.IsNullOrEmpty(orderByClause))
        {
            sqlBuilder.Append($" ORDER BY {orderByClause}");
        }

        // Add LIMIT clause if specified
        // Using ANSI SQL FETCH FIRST syntax for maximum compatibility
        if (convention.Limit.HasValue)
        {
            sqlBuilder.Append($" FETCH FIRST {convention.Limit.Value} ROWS ONLY");
        }

        var fullSql = sqlBuilder.ToString();
        sb.AppendLine($"            var sql = \"{fullSql}\";");

        if (!hasParameters)
        {
            // No parameters
            if (convention.ReturnsCollection)
            {
                if (isAsync)
                {
                    sb.AppendLine($"            return await _connection.QueryAsync<{queryType}>(sql);");
                }
                else
                {
                    sb.AppendLine($"            return _connection.Query<{queryType}>(sql);");
                }
            }
            else
            {
                if (isAsync)
                {
                    sb.AppendLine($"            return await _connection.QueryFirstOrDefaultAsync<{queryType}>(sql);");
                }
                else
                {
                    sb.AppendLine($"            return _connection.QueryFirstOrDefault<{queryType}>(sql);");
                }
            }
        }
        else
        {
            // With parameters
            if (convention.ReturnsCollection)
            {
                if (isAsync)
                {
                    sb.AppendLine($"            return await _connection.QueryAsync<{queryType}>(sql, {paramObj});");
                }
                else
                {
                    sb.AppendLine($"            return _connection.Query<{queryType}>(sql, {paramObj});");
                }
            }
            else
            {
                if (isAsync)
                {
                    sb.AppendLine($"            return await _connection.QueryFirstOrDefaultAsync<{queryType}>(sql, {paramObj});");
                }
                else
                {
                    sb.AppendLine($"            return _connection.QueryFirstOrDefault<{queryType}>(sql, {paramObj});");
                }
            }
        }

        return sb.ToString();
    }

    private static string GenerateCountQuery(MethodInfo method, RepositoryInfo info, string tableName, MethodConvention convention, bool isAsync)
    {
        var sb = new StringBuilder();
        var whereClause = BuildWhereClause(convention.PropertyNames, convention.PropertySeparators, convention.Parameters, info.EntityMetadata);
        var paramObj = GenerateParameterObject(convention.Parameters);

        // Handle DISTINCT for COUNT queries
        var countExpression = convention.HasDistinct ? "COUNT(DISTINCT *)" : "COUNT(*)";

        if (string.IsNullOrEmpty(whereClause))
        {
            sb.AppendLine($"            var sql = \"SELECT {countExpression} FROM {tableName}\";");
            if (isAsync)
            {
                sb.AppendLine($"            return await _connection.ExecuteScalarAsync<int>(sql);");
            }
            else
            {
                sb.AppendLine($"            return _connection.ExecuteScalar<int>(sql);");
            }
        }
        else
        {
            sb.AppendLine($"            var sql = \"SELECT {countExpression} FROM {tableName} WHERE {whereClause}\";");
            if (isAsync)
            {
                sb.AppendLine($"            return await _connection.ExecuteScalarAsync<int>(sql, {paramObj});");
            }
            else
            {
                sb.AppendLine($"            return _connection.ExecuteScalar<int>(sql, {paramObj});");
            }
        }

        return sb.ToString();
    }

    private static string GenerateExistsQuery(MethodInfo method, RepositoryInfo info, string tableName, MethodConvention convention, bool isAsync)
    {
        var sb = new StringBuilder();
        var whereClause = BuildWhereClause(convention.PropertyNames, convention.PropertySeparators, convention.Parameters, info.EntityMetadata);
        var paramObj = GenerateParameterObject(convention.Parameters);

        sb.AppendLine($"            var sql = \"SELECT COUNT(*) FROM {tableName} WHERE {whereClause}\";");
        if (isAsync)
        {
            sb.AppendLine($"            var count = await _connection.ExecuteScalarAsync<int>(sql, {paramObj});");
        }
        else
        {
            sb.AppendLine($"            var count = _connection.ExecuteScalar<int>(sql, {paramObj});");
        }
        sb.AppendLine($"            return count > 0;");

        return sb.ToString();
    }

    private static string GenerateDeleteQuery(MethodInfo method, RepositoryInfo info, string tableName, MethodConvention convention, bool isAsync)
    {
        var sb = new StringBuilder();
        var whereClause = BuildWhereClause(convention.PropertyNames, convention.PropertySeparators, convention.Parameters, info.EntityMetadata);
        var paramObj = GenerateParameterObject(convention.Parameters);

        // Special handling for id parameter when convention doesn't extract it
        if (string.IsNullOrEmpty(whereClause) && convention.Parameters.Count > 0)
        {
            // Check if there's an 'id' parameter (case-insensitive)
            var idParam = convention.Parameters.FirstOrDefault(p =>
                p.Name.Equals("id", StringComparison.OrdinalIgnoreCase));

            if (idParam != null)
            {
                whereClause = $"id = @{idParam.Name}";
                paramObj = $"new {{ {idParam.Name} }}";
            }
        }

        if (string.IsNullOrEmpty(whereClause))
        {
            sb.AppendLine($"            throw new InvalidOperationException(\"Delete without WHERE clause is not allowed\");");
        }
        else
        {
            sb.AppendLine($"            var sql = \"DELETE FROM {tableName} WHERE {whereClause}\";");
            if (isAsync)
            {
                sb.AppendLine($"            await _connection.ExecuteAsync(sql, {paramObj});");
            }
            else
            {
                sb.AppendLine($"            _connection.Execute(sql, {paramObj});");
            }
        }

        return sb.ToString();
    }

    private static string GenerateModificationQuery(MethodInfo method, string entityType, MethodConvention convention, bool isAsync)
    {
        var sb = new StringBuilder();
        var entityParam = convention.Parameters.FirstOrDefault();

        if (entityParam != null && entityParam.Type.Contains(entityType.Split('.').Last()))
        {
            if (convention.QueryType == QueryType.Insert)
            {
                if (isAsync)
                {
                    sb.AppendLine($"            await _entityManager.PersistAsync({entityParam.Name});");
                }
                else
                {
                    sb.AppendLine($"            _entityManager.Persist({entityParam.Name});");
                }
            }
            else
            {
                if (isAsync)
                {
                    sb.AppendLine($"            await _entityManager.MergeAsync({entityParam.Name});");
                }
                else
                {
                    sb.AppendLine($"            _entityManager.Merge({entityParam.Name});");
                }
            }
        }
        else
        {
            sb.AppendLine($"            throw new NotImplementedException(\"Method {method.Name} requires manual implementation\");");
        }

        return sb.ToString();
    }

    private static string GetColumnNameForProperty(string propertyName, EntityMetadataInfo? entityMetadata)
    {
        // Check if we have metadata and the property exists
        if (entityMetadata != null)
        {
            var propertyMetadata = entityMetadata.Properties
                .FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

            if (propertyMetadata != null && !string.IsNullOrEmpty(propertyMetadata.ColumnName))
            {
                // Return the column name from metadata (either from [Column] attribute or property name as-is)
                return propertyMetadata.ColumnName;
            }
        }

        // No metadata found: use property name as-is (preserve exact casing)
        return propertyName;
    }

    private static string BuildWhereClause(List<string> propertyNames, List<string> separators, List<ParameterInfo> parameters, EntityMetadataInfo? entityMetadata)
    {
        if (propertyNames.Count == 0)
            return string.Empty;

        var clauses = new List<string>();
        var paramIndex = 0;

        for (int i = 0; i < propertyNames.Count; i++)
        {
            var propExpression = propertyNames[i];

            // Check if property has a keyword (format: "Property:Keyword")
            if (propExpression.Contains(":"))
            {
                var parts = propExpression.Split(':');
                var propertyName = parts[0];
                var keyword = parts[1];
                var columnName = GetColumnNameForProperty(propertyName, entityMetadata);

                switch (keyword)
                {
                    case "GreaterThan":
                    case "IsGreaterThan":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} > @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "GreaterThanEqual":
                    case "IsGreaterThanEqual":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} >= @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "LessThan":
                    case "IsLessThan":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} < @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "LessThanEqual":
                    case "IsLessThanEqual":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} <= @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "Between":
                    case "IsBetween":
                        if (paramIndex + 1 < parameters.Count)
                        {
                            clauses.Add($"{columnName} BETWEEN @{parameters[paramIndex].Name} AND @{parameters[paramIndex + 1].Name}");
                            paramIndex += 2;
                        }
                        break;
                    case "Like":
                    case "IsLike":
                    case "Containing":
                    case "IsContaining":
                    case "Contains":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} LIKE CONCAT('%', @{parameters[paramIndex].Name}, '%')");
                            paramIndex++;
                        }
                        break;
                    case "NotLike":
                    case "IsNotLike":
                    case "NotContaining":
                    case "IsNotContaining":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} NOT LIKE CONCAT('%', @{parameters[paramIndex].Name}, '%')");
                            paramIndex++;
                        }
                        break;
                    case "Regex":
                    case "Matches":
                    case "IsMatches":
                    case "MatchesRegex":
                        if (paramIndex < parameters.Count)
                        {
                            // MySQL uses REGEXP, PostgreSQL uses ~, SQL Server doesn't have native regex
                            // Using MySQL syntax by default - providers can override
                            clauses.Add($"{columnName} REGEXP @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "StartingWith":
                    case "IsStartingWith":
                    case "StartsWith":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} LIKE CONCAT(@{parameters[paramIndex].Name}, '%')");
                            paramIndex++;
                        }
                        break;
                    case "EndingWith":
                    case "IsEndingWith":
                    case "EndsWith":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} LIKE CONCAT('%', @{parameters[paramIndex].Name})");
                            paramIndex++;
                        }
                        break;
                    case "In":
                    case "IsIn":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} IN @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "NotIn":
                    case "IsNotIn":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} NOT IN @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "IsNull":
                        clauses.Add($"{columnName} IS NULL");
                        // No parameter consumed
                        break;
                    case "Null":
                        // Shorthand for IsNull
                        clauses.Add($"{columnName} IS NULL");
                        // No parameter consumed
                        break;
                    case "IsNotNull":
                        clauses.Add($"{columnName} IS NOT NULL");
                        // No parameter consumed
                        break;
                    case "NotNull":
                        // Shorthand for IsNotNull
                        clauses.Add($"{columnName} IS NOT NULL");
                        // No parameter consumed
                        break;
                    case "Is":
                    case "Equals":
                        // Synonyms for equality - handle NULL specially
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} = @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "Not":
                    case "IsNot":
                        // Inequality operator
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} <> @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "True":
                    case "IsTrue":
                        clauses.Add($"{columnName} = TRUE");
                        // No parameter consumed
                        break;
                    case "False":
                    case "IsFalse":
                        clauses.Add($"{columnName} = FALSE");
                        // No parameter consumed
                        break;
                    case "Before":
                    case "IsBefore":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} < @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "After":
                    case "IsAfter":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} > @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "IgnoreCase":
                    case "IgnoringCase":
                        // Apply to the previous clause if exists
                        if (clauses.Count > 0 && paramIndex > 0)
                        {
                            var lastClause = clauses[clauses.Count - 1];
                            clauses[clauses.Count - 1] = $"LOWER({columnName}) = LOWER(@{parameters[paramIndex - 1].Name})";
                        }
                        break;
                    case "AllIgnoreCase":
                    case "AllIgnoringCase":
                        // This would require tracking all properties and applying LOWER to all comparisons
                        // For now, treat same as IgnoreCase
                        if (clauses.Count > 0 && paramIndex > 0)
                        {
                            var lastClause = clauses[clauses.Count - 1];
                            clauses[clauses.Count - 1] = $"LOWER({columnName}) = LOWER(@{parameters[paramIndex - 1].Name})";
                        }
                        break;
                    default:
                        // Default to equality
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} = @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                }
            }
            else
            {
                // Simple property without keyword - use equality
                var columnName = GetColumnNameForProperty(propExpression, entityMetadata);
                if (paramIndex < parameters.Count)
                {
                    clauses.Add($"{columnName} = @{parameters[paramIndex].Name}");
                    paramIndex++;
                }
            }
        }

        // Join clauses with appropriate separators (AND or OR)
        if (clauses.Count == 0)
            return string.Empty;
        if (clauses.Count == 1)
            return clauses[0];

        var result = new StringBuilder();
        result.Append(clauses[0]);

        for (int i = 1; i < clauses.Count; i++)
        {
            // Use separator if available, otherwise default to AND
            var separator = i - 1 < separators.Count ? separators[i - 1].ToUpper() : "AND";
            result.Append($" {separator} {clauses[i]}");
        }

        return result.ToString();
    }

    private static string BuildOrderByClause(List<OrderByInfo> orderByProperties, EntityMetadataInfo? entityMetadata)
    {
        if (orderByProperties.Count == 0)
            return string.Empty;

        var clauses = new List<string>();
        foreach (var orderBy in orderByProperties)
        {
            var columnName = GetColumnNameForProperty(orderBy.PropertyName, entityMetadata);
            var direction = orderBy.Direction.Equals("Desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            clauses.Add($"{columnName} {direction}");
        }

        return string.Join(", ", clauses);
    }

    private static string GenerateParameterObject(List<ParameterInfo> parameters)
    {
        if (parameters.Count == 0)
            return "null";

        var props = string.Join(", ", parameters.Select(p => p.Name));
        return $"new {{ {props} }}";
    }

    private static string GetInnerType(string typeString)
    {
        // Handle Task<T> first
        if (typeString.StartsWith("System.Threading.Tasks.Task<"))
        {
            var taskInner = ExtractFirstGenericArgument(typeString.Substring("System.Threading.Tasks.Task".Length));
            return GetInnerType(taskInner); // Recursively handle nested generics
        }

        // Handle arrays (T[] or T?[])
        if (typeString.Contains("[]"))
        {
            return typeString.Replace("[]", "");
        }

        // Handle IEnumerable<T>, ICollection<T>, List<T>, HashSet<T>, ISet<T>, etc.
        if (typeString.Contains("IEnumerable<") || typeString.Contains("ICollection<") ||
            typeString.Contains("IList<") || typeString.Contains("List<") ||
            typeString.Contains("HashSet<") || typeString.Contains("ISet<") ||
            typeString.Contains("IReadOnlyCollection<") || typeString.Contains("IReadOnlyList<"))
        {
            var collectionStart = typeString.IndexOf('<');
            if (collectionStart >= 0)
            {
                var innerType = ExtractFirstGenericArgument(typeString.Substring(collectionStart));
                // Don't trim '?' - preserve nullability of the element type
                return innerType;
            }
        }

        // No generic type found, return as is (preserve nullability)
        return typeString;
    }

    private static string GetCollectionConversion(string returnType)
    {
        // Determine what conversion method to use based on return type
        // Returns: empty string (no conversion), "ToList()", "ToArray()", "ToHashSet()"

        if (returnType.Contains("[]"))
            return "ToArray()";

        // List<T>, IList<T>, IReadOnlyList<T> all need ToList()
        if (returnType.Contains("List<") || returnType.Contains("System.Collections.Generic.List<") ||
            returnType.Contains("IList<") || returnType.Contains("System.Collections.Generic.IList<") ||
            returnType.Contains("IReadOnlyList<") || returnType.Contains("System.Collections.Generic.IReadOnlyList<"))
            return "ToList()";

        // IReadOnlyCollection<T> also needs ToList() (can't use ToHashSet for this)
        if (returnType.Contains("IReadOnlyCollection<") || returnType.Contains("System.Collections.Generic.IReadOnlyCollection<"))
            return "ToList()";

        if (returnType.Contains("HashSet<") || returnType.Contains("System.Collections.Generic.HashSet<"))
            return "ToHashSet()";

        if (returnType.Contains("ISet<") || returnType.Contains("System.Collections.Generic.ISet<"))
            return "ToHashSet()";

        // IEnumerable, ICollection - no conversion needed (QueryAsync returns IEnumerable)
        return string.Empty;
    }

    private static string ExtractFirstGenericArgument(string text)
    {
        // Find the first < and matching >
        var startIndex = text.IndexOf('<');
        if (startIndex < 0)
            return text;

        var depth = 0;
        for (int i = startIndex; i < text.Length; i++)
        {
            if (text[i] == '<')
                depth++;
            else if (text[i] == '>')
            {
                depth--;
                if (depth == 0)
                {
                    return text.Substring(startIndex + 1, i - startIndex - 1);
                }
            }
        }

        return text;
    }

    private static string GenerateSimpleConventionBody(MethodInfo method, RepositoryInfo info)
    {
        var sb = new StringBuilder();

        // Determine the actual return type - use method return type if different from entity
        var returnType = GetInnerType(method.ReturnType);
        var queryType = string.IsNullOrEmpty(returnType) ? info.EntityType : returnType;

        // Simple convention analysis (fallback)
        if (method.Name.StartsWith("GetAll") || method.Name.StartsWith("FindAll"))
        {
            var columnList = BuildColumnList(info.EntityMetadata);
            sb.AppendLine($"            var sql = \"SELECT {columnList} FROM {GetTableName(info.EntityType)}\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{queryType}>(sql);");
        }
        else if (method.Name.StartsWith("GetById") || method.Name.StartsWith("FindById"))
        {
            var columnList = BuildColumnList(info.EntityMetadata);
            sb.AppendLine($"            var sql = \"SELECT {columnList} FROM {GetTableName(info.EntityType)} WHERE id = @id\";");
            sb.AppendLine($"            return await _connection.QueryFirstOrDefaultAsync<{queryType}>(sql, new {{ id }});");
        }
        else
        {
            // Default implementation - throw not implemented
            sb.AppendLine($"            throw new NotImplementedException(\"Method {method.Name} requires manual implementation or a custom attribute\");");
        }

        return sb.ToString();
    }

    private static string GetTableName(string entityType)
    {
        // Simple pluralization: User -> users
        var simpleName = entityType.Split('.').Last();
        return MethodConventionAnalyzer.ToSnakeCase(simpleName) + "s";
    }

    private static string? GetTableNameFromMetadata(RepositoryInfo info, string entityType)
    {
        // Try to find the entity in the metadata dictionary
        var simpleName = entityType.Split('.').Last();
        if (info.EntitiesMetadata != null && info.EntitiesMetadata.TryGetValue(simpleName, out var metadata))
        {
            return metadata.TableName;
        }
        return null;
    }

    private static string BuildColumnList(EntityMetadataInfo? metadata)
    {
        if (metadata == null || metadata.Properties == null || metadata.Properties.Count == 0)
        {
            return "*";
        }

        var columns = metadata.Properties
            .Select(p => p.ColumnName)
            .Where(c => !string.IsNullOrEmpty(c));

        return string.Join(", ", columns);
    }

    // Relationship extraction and code generation
    private static List<Models.RelationshipMetadata> ExtractRelationships(Compilation compilation, string entityTypeName)
    {
        var relationships = new List<Models.RelationshipMetadata>();

        // Find the entity type symbol
        var entityType = compilation.GetTypeByMetadataName(entityTypeName);
        if (entityType == null)
        {
            entityType = compilation.GetSymbolsWithName(entityTypeName.Split('.').Last(), SymbolFilter.Type)
                .OfType<INamedTypeSymbol>()
                .FirstOrDefault();
        }

        if (entityType == null)
            return relationships;

        // Extract relationships from all properties
        foreach (var member in entityType.GetMembers().OfType<IPropertySymbol>())
        {
            var relationshipMetadata = Shared.RelationshipExtractor.ExtractRelationshipMetadata(member);
            if (relationshipMetadata != null)
            {
                relationships.Add(relationshipMetadata);
            }
        }

        return relationships;
    }

    private static string GenerateRelationshipAwareMethods(RepositoryInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("        #region Relationship-Aware Methods");
        sb.AppendLine();

        var entityName = info.EntityType.Split('.').Last();

        foreach (var relationship in info.Relationships)
        {
            var relatedTypeName = relationship.TargetEntityType;
            var relatedTypeFullName = relationship.TargetEntityFullType;
            var propertyName = relationship.PropertyName;

            // Generate GetByIdWith{Property}Async method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets a {entityName} by its ID with {propertyName} loaded asynchronously.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"id\">The {entityName} identifier.</param>");
            sb.AppendLine($"        /// <returns>The {entityName} with {propertyName} loaded if found; otherwise, null.</returns>");
            sb.AppendLine($"        public async Task<{info.EntityType}?> GetByIdWith{propertyName}Async({info.KeyType} id)");
            sb.AppendLine("        {");

            if (relationship.IsCollection)
            {
                GenerateOneToManyLoadSQL(sb, info, relationship, entityName, relatedTypeName);
            }
            else if (relationship.Type == Models.RelationshipType.ManyToOne)
            {
                GenerateManyToOneLoadSQL(sb, info, relationship, entityName, relatedTypeName);
            }
            else if (relationship.Type == Models.RelationshipType.OneToOne)
            {
                GenerateOneToOneLoadSQL(sb, info, relationship, entityName, relatedTypeName);
            }

            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate Load{Property}Async for lazy loading
            if (relationship.FetchType == Models.FetchType.Lazy)
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Loads {propertyName} for an existing {entityName} entity asynchronously.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        /// <param name=\"entity\">The {entityName} entity.</param>");

                if (relationship.IsCollection)
                {
                    sb.AppendLine($"        /// <returns>A collection of {relatedTypeName} entities.</returns>");
                    sb.AppendLine($"        public async Task<IEnumerable<{relatedTypeFullName}>> Load{propertyName}Async({info.EntityType} entity)");
                }
                else
                {
                    sb.AppendLine($"        /// <returns>The loaded {relatedTypeName} entity if found; otherwise, null.</returns>");
                    // Remove trailing ? if already present to avoid double nullable marker
                    var returnType = relatedTypeFullName.TrimEnd('?');
                    sb.AppendLine($"        public async Task<{returnType}?> Load{propertyName}Async({info.EntityType} entity)");
                }

                sb.AppendLine("        {");
                sb.AppendLine("            if (entity == null) throw new ArgumentNullException(nameof(entity));");
                sb.AppendLine();

                if (relationship.IsCollection)
                {
                    GenerateLazyLoadCollectionSQL(sb, info, relationship, relatedTypeName);
                }
                else
                {
                    GenerateLazyLoadSingleSQL(sb, info, relationship, relatedTypeName);
                }

                sb.AppendLine("        }");
                sb.AppendLine();
            }
        }

        sb.AppendLine("        #endregion");
        return sb.ToString();
    }

    private static void GenerateManyToOneLoadSQL(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string entityName, string relatedTypeName)
    {
        var foreignKeyColumn = relationship.JoinColumn?.Name ?? $"{relatedTypeName}Id";

        // Get actual table names from metadata
        var entityTableName = info.EntityMetadata?.TableName ?? entityName;
        var relatedTableName = GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relatedTypeName;
        var keyPropertyName = GetKeyPropertyName(info);
        var relatedKeyPropertyName = GetKeyPropertyName(info, relationship.TargetEntityType);

        sb.AppendLine($"            var sql = @\"SELECT e.*, r.* FROM {entityTableName} e LEFT JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyPropertyName} WHERE e.{keyPropertyName} = @Id\";");
        sb.AppendLine($"            var result = await _connection.QueryAsync<{info.EntityType}, {relationship.TargetEntityFullType}, {info.EntityType}>(sql, (entity, related) => {{ entity.{relationship.PropertyName} = related; return entity; }}, new {{ Id = id }}, splitOn: \"{relatedKeyPropertyName}\");");
        sb.AppendLine("            return result.FirstOrDefault();");
    }

    private static void GenerateOneToOneLoadSQL(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string entityName, string relatedTypeName)
    {
        var foreignKeyColumn = relationship.JoinColumn?.Name ?? $"{relatedTypeName}Id";

        // Get actual table names from metadata
        var entityTableName = info.EntityMetadata?.TableName ?? entityName;
        var relatedTableName = GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relatedTypeName;
        var keyPropertyName = GetKeyPropertyName(info);
        var relatedKeyPropertyName = GetKeyPropertyName(info, relationship.TargetEntityType);

        sb.AppendLine($"            var sql = @\"SELECT e.*, r.* FROM {entityTableName} e LEFT JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyPropertyName} WHERE e.{keyPropertyName} = @Id\";");
        sb.AppendLine($"            var result = await _connection.QueryAsync<{info.EntityType}, {relationship.TargetEntityFullType}, {info.EntityType}>(sql, (entity, related) => {{ entity.{relationship.PropertyName} = related; return entity; }}, new {{ Id = id }}, splitOn: \"{relatedKeyPropertyName}\");");
        sb.AppendLine("            return result.FirstOrDefault();");
    }

    private static void GenerateOneToManyLoadSQL(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string entityName, string relatedTypeName)
    {
        var foreignKeyColumn = relationship.JoinColumn?.Name ?? $"{entityName}Id";

        // Get actual table names from metadata
        var entityTableName = info.EntityMetadata?.TableName ?? entityName;
        var relatedTableName = GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relatedTypeName;

        var keyPropertyName = GetKeyPropertyName(info);
        sb.AppendLine($"            var entityDict = new Dictionary<{info.KeyType}, {info.EntityType}>();");
        sb.AppendLine($"            var sql = @\"SELECT e.*, r.* FROM {entityTableName} e LEFT JOIN {relatedTableName} r ON e.{keyPropertyName} = r.{foreignKeyColumn} WHERE e.{keyPropertyName} = @Id\";");
        sb.AppendLine($"            await _connection.QueryAsync<{info.EntityType}, {relationship.TargetEntityFullType}, {info.EntityType}>(sql, (entity, related) => {{");
        sb.AppendLine($"                if (!entityDict.TryGetValue(entity.{keyPropertyName}, out var existingEntity)) {{");
        sb.AppendLine($"                    existingEntity = entity;");
        sb.AppendLine($"                    existingEntity.{relationship.PropertyName} = new List<{relationship.TargetEntityFullType}>();");
        sb.AppendLine($"                    entityDict[entity.{keyPropertyName}] = existingEntity;");
        sb.AppendLine($"                }}");
        sb.AppendLine($"                if (related != null) ((List<{relationship.TargetEntityFullType}>)existingEntity.{relationship.PropertyName}).Add(related);");
        sb.AppendLine($"                return existingEntity;");
        sb.AppendLine($"            }}, new {{ Id = id }}, splitOn: \"{keyPropertyName}\");");
        sb.AppendLine("            return entityDict.Values.FirstOrDefault();");
    }

    private static void GenerateLazyLoadCollectionSQL(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string relatedTypeName)
    {
        // Get actual table name from metadata
        var relatedTableName = GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relatedTypeName;
        var keyPropertyName = GetKeyPropertyName(info);
        var fkColumnName = relationship.JoinColumn?.Name ?? $"{info.EntityType.Split('.').Last()}Id";

        sb.AppendLine($"            var sql = @\"SELECT * FROM {relatedTableName} WHERE {fkColumnName} = @Id\";");
        sb.AppendLine($"            return await _connection.QueryAsync<{relationship.TargetEntityFullType}>(sql, new {{ Id = entity.{keyPropertyName} }});");
    }

    private static void GenerateLazyLoadSingleSQL(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string relatedTypeName)
    {
        // Get actual table name from metadata
        var relatedTableName = GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relatedTypeName;
        var entityName = info.EntityType.Split('.').Last();

        sb.AppendLine($"            // Lazy load {relationship.PropertyName}");

        var keyPropertyName = GetKeyPropertyName(info);
        
        if (relationship.Type == Models.RelationshipType.OneToOne && !string.IsNullOrEmpty(relationship.MappedBy))
        {
            // Owner side of OneToOne with MappedBy - query by owner's ID on inverse side's FK
            var inverseFkColumn = relationship.JoinColumn?.Name ?? $"{entityName}Id";
            sb.AppendLine($"            var sql = @\"SELECT * FROM {relatedTableName} WHERE {inverseFkColumn} = @Id\";");
            sb.AppendLine($"            var result = await _connection.QueryAsync<{relationship.TargetEntityFullType}>(sql, new {{ Id = entity.{keyPropertyName} }});");
        }
        else
        {
            // ManyToOne or inverse side of OneToOne - query by FK on current entity
            var fkColumnName = relationship.JoinColumn?.Name ?? $"{relatedTypeName}Id";
            var hasFkProperty = HasProperty(info, fkColumnName);
            var relatedKeyPropertyName = GetKeyPropertyName(info, relationship.TargetEntityType);
            sb.AppendLine($"            var sql = @\"SELECT * FROM {relatedTableName} WHERE {relatedKeyPropertyName} = @Id\";");
            
            if (hasFkProperty)
            {
                var fkPropertyName = GetPropertyNameForColumn(info, fkColumnName);
                sb.AppendLine($"            var result = await _connection.QueryAsync<{relationship.TargetEntityFullType}>(sql, new {{ Id = entity.{fkPropertyName} }});");
            }
            else
            {
                // Use navigation property's key if FK property doesn't exist
                var relatedKeyType = GetRelatedEntityKeyType(info, relationship.TargetEntityType);
                var defaultKeyValue = relatedKeyType == "Guid" ? "Guid.Empty" : relatedKeyType == "int" ? "0" : relatedKeyType == "long" ? "0L" : $"default({relatedKeyType})";
                sb.AppendLine($"            var result = await _connection.QueryAsync<{relationship.TargetEntityFullType}>(sql, new {{ Id = entity.{relationship.PropertyName}?.{relatedKeyPropertyName} ?? {defaultKeyValue} }});");
            }
        }

        sb.AppendLine("            return result.FirstOrDefault();");
    }

    // Generate eager loading overrides
    private static string GenerateEagerLoadingOverrides(RepositoryInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("        #region Eager Loading Overrides");
        sb.AppendLine();

        var entityName = info.EntityType.Split('.').Last();
        var eagerRelationships = info.EagerRelationships;

        if (eagerRelationships.Count == 0)
        {
            sb.AppendLine("        #endregion");
            return sb.ToString();
        }

        // Check if we have only single-entity relationships or also collections
        var hasSingleOnly = eagerRelationships.All(r => !r.IsCollection);
        var hasCollections = eagerRelationships.Any(r => r.IsCollection);

        if (hasSingleOnly)
        {
            // Simple case: only ManyToOne or OneToOne relationships
            GenerateSimpleEagerGetByIdOverride(sb, info, entityName, eagerRelationships);
        }
        else if (!hasCollections)
        {
            // Only single relationships
            GenerateSimpleEagerGetByIdOverride(sb, info, entityName, eagerRelationships);
        }
        else
        {
            // Complex case: has collections - need multiple queries or careful JOIN
            GenerateComplexEagerGetByIdOverride(sb, info, entityName, eagerRelationships);
        }

        // Generate GetAllAsync override
        GenerateEagerGetAllOverride(sb, info, entityName, eagerRelationships);

        // Generate GetByIdsAsync for batch loading
        GenerateBatchLoadingMethod(sb, info, entityName, eagerRelationships);

        sb.AppendLine("        #endregion");
        return sb.ToString();
    }

    private static void GenerateSimpleEagerGetByIdOverride(StringBuilder sb, RepositoryInfo info, string entityName, List<Models.RelationshipMetadata> eagerRelationships)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets a {entityName} by its ID with eager relationships loaded asynchronously.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <param name=\"id\">The {entityName} identifier.</param>");
        sb.AppendLine($"        /// <returns>The {entityName} with eager relationships loaded if found; otherwise, null.</returns>");
        sb.AppendLine($"        public override async Task<{info.EntityType}?> GetByIdAsync({info.KeyType} id)");
        sb.AppendLine("        {");

        // Get actual table name from metadata
        var entityTableName = info.EntityMetadata?.TableName ?? entityName;

        // Build SQL with JOINs for all eager relationships
        var keyPropertyName = GetKeyPropertyName(info);
        var sqlBuilder = new StringBuilder($"SELECT e.*");
        var joins = new List<string>();
        var splitOns = new List<string> { keyPropertyName };
        var typeParams = new List<string> { info.EntityType };
        var aliases = new Dictionary<string, string> { { entityName, "e" } };
        int aliasCounter = 0;

        foreach (var relationship in eagerRelationships)
        {
            if (relationship.IsCollection) continue; // Skip collections in simple case

            var alias = $"r{aliasCounter++}";
            var relatedTypeName = relationship.TargetEntityType;
            var relatedTableName = GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relatedTypeName;
            var foreignKeyColumn = relationship.JoinColumn?.Name ?? $"{relatedTypeName}Id";

            var relatedKeyPropertyName = GetKeyPropertyName(info, relationship.TargetEntityType);
            aliases[relatedTypeName] = alias;
            sqlBuilder.Append($", {alias}.*");
            joins.Add($"LEFT JOIN {relatedTableName} {alias} ON e.{foreignKeyColumn} = {alias}.{relatedKeyPropertyName}");
            splitOns.Add(relatedKeyPropertyName);
            typeParams.Add(relationship.TargetEntityFullType);
        }

        sqlBuilder.Append($" FROM {entityTableName} e");
        foreach (var join in joins)
        {
            sqlBuilder.Append($" {join}");
        }
        sqlBuilder.Append($" WHERE e.{keyPropertyName} = @Id");

        sb.AppendLine($"            var sql = @\"{sqlBuilder}\";");
        sb.AppendLine();

        // Generate Dapper query with multi-mapping
        if (eagerRelationships.Count == 1)
        {
            var rel = eagerRelationships[0];
            sb.AppendLine($"            var result = await _connection.QueryAsync<{info.EntityType}, {rel.TargetEntityFullType}, {info.EntityType}>(");
            sb.AppendLine($"                sql,");
            sb.AppendLine($"                (entity, related) => {{ entity.{rel.PropertyName} = related; return entity; }},");
            sb.AppendLine($"                new {{ Id = id }},");
            sb.AppendLine($"                splitOn: \"{string.Join(",", splitOns.Skip(1))}\");");
        }
        else if (eagerRelationships.Count == 2)
        {
            var rel1 = eagerRelationships[0];
            var rel2 = eagerRelationships[1];
            sb.AppendLine($"            var result = await _connection.QueryAsync<{info.EntityType}, {rel1.TargetEntityFullType}, {rel2.TargetEntityFullType}, {info.EntityType}>(");
            sb.AppendLine($"                sql,");
            sb.AppendLine($"                (entity, related1, related2) => {{ entity.{rel1.PropertyName} = related1; entity.{rel2.PropertyName} = related2; return entity; }},");
            sb.AppendLine($"                new {{ Id = id }},");
            sb.AppendLine($"                splitOn: \"{string.Join(",", splitOns.Skip(1))}\");");
        }
        else
        {
            // Fall back to multiple queries for more than 2 relationships
            sb.AppendLine($"            var entity = await base.GetByIdAsync(id);");
            sb.AppendLine($"            if (entity == null) return null;");
            sb.AppendLine();
            foreach (var rel in eagerRelationships)
            {
                sb.AppendLine($"            entity.{rel.PropertyName} = await GetByIdWith{rel.PropertyName}Async(id);");
            }
        }

        sb.AppendLine("            return result.FirstOrDefault();");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateComplexEagerGetByIdOverride(StringBuilder sb, RepositoryInfo info, string entityName, List<Models.RelationshipMetadata> eagerRelationships)
    {
        // When we have collections, we need to use separate queries to avoid cartesian product
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets a {entityName} by its ID with eager relationships loaded asynchronously.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <param name=\"id\">The {entityName} identifier.</param>");
        sb.AppendLine($"        /// <returns>The {entityName} with eager relationships loaded if found; otherwise, null.</returns>");
        sb.AppendLine($"        public override async Task<{info.EntityType}?> GetByIdAsync({info.KeyType} id)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var entity = await base.GetByIdAsync(id);");
        sb.AppendLine($"            if (entity == null) return null;");
        sb.AppendLine();

        // Load each eager relationship
        foreach (var rel in eagerRelationships)
        {
            if (rel.IsCollection)
            {
                var foreignKeyColumn = rel.JoinColumn?.Name ?? $"{entityName}Id";
                sb.AppendLine($"            // Load {rel.PropertyName} collection");
                sb.AppendLine($"            var {rel.PropertyName.ToLower()}Sql = @\"SELECT * FROM {rel.TargetEntityType} WHERE {foreignKeyColumn} = @Id\";");
                sb.AppendLine($"            entity.{rel.PropertyName} = (await _connection.QueryAsync<{rel.TargetEntityFullType}>({rel.PropertyName.ToLower()}Sql, new {{ Id = id }})).ToList();");
                sb.AppendLine();
            }
            else
            {
                var foreignKeyColumn = rel.JoinColumn?.Name ?? $"{rel.TargetEntityType}Id";
                var hasFkProperty = HasProperty(info, foreignKeyColumn);
                var isFkNullable = IsPropertyNullable(info, foreignKeyColumn);
                var relatedKeyType = GetRelatedEntityKeyType(info, rel.TargetEntityType);
                var nullCheck = isFkNullable ? "!= null" : $"!= default({relatedKeyType})";

                var relatedKeyPropertyName = GetKeyPropertyName(info, rel.TargetEntityType);
                sb.AppendLine($"            // Load {rel.PropertyName}");
                sb.AppendLine($"            var {rel.PropertyName.ToLower()}Sql = @\"SELECT r.* FROM {rel.TargetEntityType} r WHERE r.{relatedKeyPropertyName} = @ForeignKeyId\";");
                
                if (hasFkProperty)
                {
                    var foreignKeyProperty = GetPropertyNameForColumn(info, foreignKeyColumn);
                    sb.AppendLine($"            var {rel.PropertyName.ToLower()}FkValue = entity.{foreignKeyProperty};");
                    sb.AppendLine($"            if ({rel.PropertyName.ToLower()}FkValue {nullCheck})");
                }
                else
                {
                    // Use navigation property's key if FK property doesn't exist
                    var defaultKeyValue = relatedKeyType == "Guid" ? "Guid.Empty" : relatedKeyType == "int" ? "0" : relatedKeyType == "long" ? "0L" : $"default({relatedKeyType})";
                    sb.AppendLine($"            var {rel.PropertyName.ToLower()}FkValue = entity.{rel.PropertyName}?.{relatedKeyPropertyName} ?? {defaultKeyValue};");
                    sb.AppendLine($"            if (entity.{rel.PropertyName} != null)");
                }
                
                sb.AppendLine($"            {{");
                sb.AppendLine($"                entity.{rel.PropertyName} = (await _connection.QueryAsync<{rel.TargetEntityFullType}>({rel.PropertyName.ToLower()}Sql, new {{ ForeignKeyId = {rel.PropertyName.ToLower()}FkValue }})).FirstOrDefault();");
                sb.AppendLine($"            }}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("            return entity;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateEagerGetAllOverride(StringBuilder sb, RepositoryInfo info, string entityName, List<Models.RelationshipMetadata> eagerRelationships)
    {
        // GetAllAsync with eager loading
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets all {entityName} entities with eager relationships loaded asynchronously.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <returns>A collection of {entityName} entities with eager relationships loaded.</returns>");
        sb.AppendLine($"        public override async Task<IEnumerable<{info.EntityType}>> GetAllAsync()");
        sb.AppendLine("        {");
        sb.AppendLine($"            var entities = (await base.GetAllAsync()).ToList();");
        sb.AppendLine($"            if (!entities.Any()) return entities;");
        sb.AppendLine();

        // Load relationships for all entities
        foreach (var rel in eagerRelationships)
        {
            if (rel.IsCollection)
            {
                var foreignKeyColumn = rel.JoinColumn?.Name ?? $"{entityName}Id";
                var foreignKeyProperty = GetPropertyNameForColumn(info, foreignKeyColumn, rel.TargetEntityType);

                var keyPropertyName = GetKeyPropertyName(info);
                sb.AppendLine($"            // Load {rel.PropertyName} for all entities");
                sb.AppendLine($"            var ids = entities.Select(e => e.{keyPropertyName}).ToArray();");
                sb.AppendLine($"            var {rel.PropertyName.ToLower()}Sql = @\"SELECT * FROM {rel.TargetEntityType} WHERE {foreignKeyColumn} IN @Ids\";");
                sb.AppendLine($"            var all{rel.PropertyName} = (await _connection.QueryAsync<{rel.TargetEntityFullType}>({rel.PropertyName.ToLower()}Sql, new {{ Ids = ids }})).ToList();");
                sb.AppendLine($"            var {rel.PropertyName.ToLower()}ByEntity = all{rel.PropertyName}.GroupBy(r => r.{foreignKeyProperty}).ToDictionary(g => g.Key, g => g.ToList());");
                sb.AppendLine($"            foreach (var entity in entities)");
                sb.AppendLine($"            {{");
                sb.AppendLine($"                if ({rel.PropertyName.ToLower()}ByEntity.TryGetValue(entity.{keyPropertyName}, out var items))");
                sb.AppendLine($"                    entity.{rel.PropertyName} = items;");
                sb.AppendLine($"            }}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("            return entities;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateBatchLoadingMethod(StringBuilder sb, RepositoryInfo info, string entityName, List<Models.RelationshipMetadata> eagerRelationships)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets multiple {entityName} entities by their IDs with eager relationships loaded asynchronously.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <param name=\"ids\">The collection of {entityName} identifiers.</param>");
        sb.AppendLine($"        /// <returns>A collection of {entityName} entities with eager relationships loaded.</returns>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> GetByIdsAsync(IEnumerable<{info.KeyType}> ids)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var idArray = ids.ToArray();");
        sb.AppendLine($"            if (!idArray.Any()) return Enumerable.Empty<{info.EntityType}>();");
        sb.AppendLine();

        // Get actual table name from metadata
        var entityTableName = info.EntityMetadata?.TableName ?? entityName;

        var keyPropertyName = GetKeyPropertyName(info);
        sb.AppendLine($"            var sql = @\"SELECT * FROM {entityTableName} WHERE {keyPropertyName} IN @Ids\";");
        sb.AppendLine($"            var entities = (await _connection.QueryAsync<{info.EntityType}>(sql, new {{ Ids = idArray }})).ToList();");
        sb.AppendLine($"            if (!entities.Any()) return entities;");
        sb.AppendLine();

        // Load relationships for all entities
        foreach (var rel in eagerRelationships)
        {
            if (rel.IsCollection)
            {
                var foreignKeyColumn = rel.JoinColumn?.Name ?? $"{entityName}Id";
                var foreignKeyProperty = GetPropertyNameForColumn(info, foreignKeyColumn, rel.TargetEntityType);
                var relatedTableName = GetTableNameFromMetadata(info, rel.TargetEntityType) ?? rel.TargetEntityType;

                sb.AppendLine($"            // Load {rel.PropertyName} for all entities");
                sb.AppendLine($"            var {rel.PropertyName.ToLower()}Sql = @\"SELECT * FROM {relatedTableName} WHERE {foreignKeyColumn} IN @Ids\";");
                sb.AppendLine($"            var all{rel.PropertyName} = (await _connection.QueryAsync<{rel.TargetEntityFullType}>({rel.PropertyName.ToLower()}Sql, new {{ Ids = idArray }})).ToList();");
                sb.AppendLine($"            var {rel.PropertyName.ToLower()}ByEntity = all{rel.PropertyName}.GroupBy(r => r.{foreignKeyProperty}).ToDictionary(g => g.Key, g => g.ToList());");
                sb.AppendLine($"            foreach (var entity in entities)");
                sb.AppendLine($"            {{");
                sb.AppendLine($"                if ({rel.PropertyName.ToLower()}ByEntity.TryGetValue(entity.{keyPropertyName}, out var items))");
                sb.AppendLine($"                    entity.{rel.PropertyName} = items;");
                sb.AppendLine($"            }}");
                sb.AppendLine();
            }
            else
            {
                var foreignKeyColumn = rel.JoinColumn?.Name ?? $"{rel.TargetEntityType}Id";
                var foreignKeyProperty = GetPropertyNameForColumn(info, foreignKeyColumn);
                var relatedTableName = GetTableNameFromMetadata(info, rel.TargetEntityType) ?? rel.TargetEntityType;
                var isFkNullable = IsPropertyNullable(info, foreignKeyColumn);
                var relatedKeyType = GetRelatedEntityKeyType(info, rel.TargetEntityType);
                var fkPropertyType = GetForeignKeyPropertyType(info, foreignKeyColumn);
                var nullCheck = isFkNullable ? "!= null" : $"!= default({relatedKeyType})";
                var fkValueCheck = isFkNullable ? "fkValue != null && " : "";
                // Cast is needed if FK type differs from related entity's key type, regardless of nullability
                var needsCast = fkPropertyType != null && fkPropertyType != relatedKeyType;
                var fkValueCast = needsCast ? $"({relatedKeyType})fkValue" : "fkValue";

                sb.AppendLine($"            // Load {rel.PropertyName} for all entities");
                sb.AppendLine($"            var {rel.PropertyName.ToLower()}Ids = entities.Select(e => e.{foreignKeyProperty}).Where(v => v {nullCheck}).Distinct().ToArray();");
                sb.AppendLine($"            if ({rel.PropertyName.ToLower()}Ids.Any())");
                sb.AppendLine($"            {{");
                var relatedKeyPropertyName = GetKeyPropertyName(info, rel.TargetEntityType);
                sb.AppendLine($"                var {rel.PropertyName.ToLower()}Sql = @\"SELECT * FROM {relatedTableName} WHERE {relatedKeyPropertyName} IN @Ids\";");
                sb.AppendLine($"                var all{rel.PropertyName} = (await _connection.QueryAsync<{rel.TargetEntityFullType}>({rel.PropertyName.ToLower()}Sql, new {{ Ids = {rel.PropertyName.ToLower()}Ids }})).ToDictionary(r => r.{relatedKeyPropertyName});");
                sb.AppendLine($"                foreach (var entity in entities)");
                sb.AppendLine($"                {{");
                sb.AppendLine($"                    var fkValue = entity.{foreignKeyProperty};");
                sb.AppendLine($"                    if ({fkValueCheck}all{rel.PropertyName}.TryGetValue({fkValueCast}, out var related))");
                sb.AppendLine($"                        entity.{rel.PropertyName} = related;");
                sb.AppendLine($"                }}");
                sb.AppendLine($"            }}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("            return entities;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static string GenerateCascadeOperationOverrides(RepositoryInfo info)
    {
        var sb = new StringBuilder();

        sb.AppendLine("        #region Cascade Operations");
        sb.AppendLine();

        // Check for Persist cascades - affects AddAsync
        var persistCascades = info.CascadeRelationships.Where(r => (r.CascadeTypes & Models.CascadeType.Persist) != 0).ToList();
        if (persistCascades.Any())
        {
            sb.AppendLine(GenerateCascadeAddMethod(info, persistCascades));
        }

        // Check for Merge cascades - affects UpdateAsync
        var mergeCascades = info.CascadeRelationships.Where(r => (r.CascadeTypes & Models.CascadeType.Merge) != 0).ToList();
        if (mergeCascades.Any())
        {
            sb.AppendLine(GenerateCascadeUpdateMethod(info, mergeCascades));
        }

        // Check for Remove cascades - affects DeleteAsync
        var removeCascades = info.CascadeRelationships.Where(r => (r.CascadeTypes & Models.CascadeType.Remove) != 0).ToList();
        if (removeCascades.Any())
        {
            sb.AppendLine(GenerateCascadeDeleteMethod(info, removeCascades));
        }

        sb.AppendLine("        #endregion");
        sb.AppendLine();

        return sb.ToString();
    }

    private static string GenerateCascadeAddMethod(RepositoryInfo info, List<Models.RelationshipMetadata> cascades)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Adds an entity with cascade persist support.");
        sb.AppendLine($"        /// Automatically persists related entities marked with CascadeType.Persist.");
        sb.AppendLine($"        /// Cascades: {string.Join(", ", cascades.Select(c => c.PropertyName))}");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public async Task<{info.EntityType}> AddWithCascadeAsync({info.EntityType} entity)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            if (entity == null) throw new ArgumentNullException(nameof(entity));");
        sb.AppendLine();

        // Generate cascade logic for each relationship with Persist
        foreach (var cascade in cascades)
        {
            if (cascade.IsCollection)
            {
                // Collection cascade (OneToMany) - Persist children after parent
                sb.AppendLine($"            // Cascade persist {cascade.PropertyName} collection (children persisted after parent)");
                sb.AppendLine($"            var {cascade.PropertyName.ToLower()}ToPersist = entity.{cascade.PropertyName}?.ToList() ?? new List<{cascade.TargetEntityFullType}>();");
                sb.AppendLine();
            }
            else
            {
                // Single entity cascade (ManyToOne, OneToOne) - Persist parent first
                var fkColumnName = cascade.JoinColumn?.Name ?? $"{cascade.TargetEntityType}Id";
                var hasFkProperty = HasProperty(info, fkColumnName);

                sb.AppendLine($"            // Cascade persist {cascade.PropertyName} (parent persisted first)");
                sb.AppendLine($"            if (entity.{cascade.PropertyName} != null)");
                sb.AppendLine($"            {{");
                var relatedKeyPropertyName = GetKeyPropertyName(info, cascade.TargetEntityType);
                sb.AppendLine($"                // Check if entity is transient (Id is default value)");
                sb.AppendLine($"                if (entity.{cascade.PropertyName}.{relatedKeyPropertyName} == default)");
                sb.AppendLine($"                {{");
                sb.AppendLine($"                    // Persist the related entity first");
                sb.AppendLine($"                    await _entityManager.PersistAsync(entity.{cascade.PropertyName});");
                sb.AppendLine($"                    ");
                if (hasFkProperty)
                {
                    var fkPropertyName = GetPropertyNameForColumn(info, fkColumnName);
                    sb.AppendLine($"                    // Update FK on main entity (if FK property exists)");
                    sb.AppendLine($"                    entity.{fkPropertyName} = entity.{cascade.PropertyName}.{relatedKeyPropertyName};");
                }
                else
                {
                    sb.AppendLine($"                    // Note: FK property doesn't exist - FK is managed automatically via @JoinColumn");
                }
                sb.AppendLine($"                }}");
                sb.AppendLine($"            }}");
                sb.AppendLine();
            }
        }

        sb.AppendLine($"            // Persist the main entity");
        sb.AppendLine($"            var result = await AddAsync(entity);");
        sb.AppendLine();

        // Now persist collections (children after parent)
        foreach (var cascade in cascades.Where(c => c.IsCollection))
        {
            // Determine FK column name from MappedBy or convention
            var fkColumn = cascade.MappedBy != null
                ? $"{info.EntityType.Split('.').Last()}Id"
                : $"{cascade.TargetEntityType}Id";
            
            // Check if FK property exists on the related entity
            var hasFkProperty = HasProperty(info, fkColumn, cascade.TargetEntityType);
            var fkPropertyName = hasFkProperty ? GetPropertyNameForColumn(info, fkColumn, cascade.TargetEntityType) : null;
            var ownerPropertyName = cascade.MappedBy ?? info.EntityType.Split('.').Last();

            sb.AppendLine($"            // Persist {cascade.PropertyName} collection after parent");
            sb.AppendLine($"            if ({cascade.PropertyName.ToLower()}ToPersist.Any())");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                foreach (var item in {cascade.PropertyName.ToLower()}ToPersist)");
            sb.AppendLine($"                {{");
            sb.AppendLine($"                    // Set FK to parent");
            if (hasFkProperty && fkPropertyName != null)
            {
                sb.AppendLine($"                    item.{fkPropertyName} = result.Id;");
            }
            else
            {
                // Use navigation property if FK property doesn't exist
                sb.AppendLine($"                    item.{ownerPropertyName} = result;");
            }
            sb.AppendLine($"                    await _entityManager.PersistAsync(item);");
            sb.AppendLine($"                }}");
            sb.AppendLine($"            }}");
            sb.AppendLine();
        }

        sb.AppendLine($"            return result;");
        sb.AppendLine($"        }}");
        sb.AppendLine();

        return sb.ToString();
    }

    private static string GenerateCascadeUpdateMethod(RepositoryInfo info, List<Models.RelationshipMetadata> cascades)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Updates an entity with cascade merge support.");
        sb.AppendLine($"        /// Automatically updates related entities marked with CascadeType.Merge.");
        sb.AppendLine($"        /// Cascades: {string.Join(", ", cascades.Select(c => c.PropertyName))}");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public async Task UpdateWithCascadeAsync({info.EntityType} entity)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            if (entity == null) throw new ArgumentNullException(nameof(entity));");
        sb.AppendLine();

        // Update single entity relationships first
        foreach (var cascade in cascades.Where(c => !c.IsCollection))
        {
            sb.AppendLine($"            // Cascade merge {cascade.PropertyName}");
            sb.AppendLine($"            if (entity.{cascade.PropertyName} != null)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                // Update if entity exists (has Id), persist if new");
            sb.AppendLine($"                if (entity.{cascade.PropertyName}.Id != default)");
            sb.AppendLine($"                {{");
            sb.AppendLine($"                    await _entityManager.MergeAsync(entity.{cascade.PropertyName});");
            sb.AppendLine($"                }}");
            sb.AppendLine($"                else");
            sb.AppendLine($"                {{");
            sb.AppendLine($"                    await _entityManager.PersistAsync(entity.{cascade.PropertyName});");
            sb.AppendLine($"                }}");
            sb.AppendLine($"            }}");
            sb.AppendLine();
        }

        sb.AppendLine($"            // Update the main entity");
        sb.AppendLine($"            await UpdateAsync(entity);");
        sb.AppendLine();

        // Handle collection cascades
        foreach (var cascade in cascades.Where(c => c.IsCollection))
        {
            // Determine FK column name from MappedBy or convention
            var fkColumn = cascade.MappedBy != null
                ? $"{info.EntityType.Split('.').Last()}Id"
                : $"{cascade.TargetEntityType}Id";
            
            // Check if FK property exists on the related entity
            var hasFkProperty = HasProperty(info, fkColumn, cascade.TargetEntityType);
            var fkPropertyName = hasFkProperty ? GetPropertyNameForColumn(info, fkColumn, cascade.TargetEntityType) : null;
            var ownerPropertyName = cascade.MappedBy ?? info.EntityType.Split('.').Last();

            var keyPropertyName = GetKeyPropertyName(info);
            var relatedKeyPropertyName = GetKeyPropertyName(info, cascade.TargetEntityType);
            
            sb.AppendLine($"            // Cascade merge {cascade.PropertyName} collection");
            sb.AppendLine($"            if (entity.{cascade.PropertyName} != null)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                var currentItems = entity.{cascade.PropertyName}.ToList();");
            sb.AppendLine($"                ");

            if (cascade.OrphanRemoval)
            {
                var relatedTableName = GetTableNameFromMetadata(info, cascade.TargetEntityType) ?? cascade.TargetEntityType;
                sb.AppendLine($"                // Load existing items to detect orphans (OrphanRemoval=true)");
                sb.AppendLine($"                var fkColumnName = \"{fkColumn}\";");
                sb.AppendLine($"                var sql = $\"SELECT * FROM {relatedTableName} WHERE {{fkColumnName}} = @ParentId\";");
                sb.AppendLine($"                var existingItems = (await _connection.QueryAsync<{cascade.TargetEntityFullType}>(sql, new {{ ParentId = entity.{keyPropertyName} }})).ToList();");
                sb.AppendLine($"                ");
                sb.AppendLine($"                var currentIds = currentItems.Where(i => i.{relatedKeyPropertyName} != default).Select(i => i.{relatedKeyPropertyName}).ToHashSet();");
                sb.AppendLine($"                ");
                sb.AppendLine($"                // Delete orphaned items");
                sb.AppendLine($"                foreach (var existing in existingItems)");
                sb.AppendLine($"                {{");
                sb.AppendLine($"                    if (!currentIds.Contains(existing.{relatedKeyPropertyName}))");
                sb.AppendLine($"                    {{");
                sb.AppendLine($"                        await _entityManager.RemoveAsync(existing);");
                sb.AppendLine($"                    }}");
                sb.AppendLine($"                }}");
                sb.AppendLine($"                ");
            }

            sb.AppendLine($"                // Update existing items or persist new ones");
            sb.AppendLine($"                foreach (var item in currentItems)");
            sb.AppendLine($"                {{");
            sb.AppendLine($"                    // Ensure FK is set");
            if (hasFkProperty && fkPropertyName != null)
            {
                sb.AppendLine($"                    item.{fkPropertyName} = entity.{keyPropertyName};");
            }
            else
            {
                // Use navigation property if FK property doesn't exist
                sb.AppendLine($"                    item.{ownerPropertyName} = entity;");
            }
            sb.AppendLine($"                    ");
            sb.AppendLine($"                    if (item.{relatedKeyPropertyName} != default)");
            sb.AppendLine($"                    {{");
            sb.AppendLine($"                        await _entityManager.MergeAsync(item);");
            sb.AppendLine($"                    }}");
            sb.AppendLine($"                    else");
            sb.AppendLine($"                    {{");
            sb.AppendLine($"                        await _entityManager.PersistAsync(item);");
            sb.AppendLine($"                    }}");
            sb.AppendLine($"                }}");
            sb.AppendLine($"            }}");
            sb.AppendLine();
        }

        sb.AppendLine($"        }}");
        sb.AppendLine();

        return sb.ToString();
    }

    private static string GenerateCascadeDeleteMethod(RepositoryInfo info, List<Models.RelationshipMetadata> cascades)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Deletes an entity with cascade remove support.");
        sb.AppendLine($"        /// Automatically deletes related entities marked with CascadeType.Remove.");
        sb.AppendLine($"        /// Cascades: {string.Join(", ", cascades.Select(c => c.PropertyName))}");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public async Task DeleteWithCascadeAsync({info.KeyType} id)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            // Load entity to check relationships");
        sb.AppendLine($"            var entity = await GetByIdAsync(id);");
        sb.AppendLine($"            if (entity == null)");
        sb.AppendLine($"                throw new InvalidOperationException($\"{info.EntityType} with id {{id}} not found\");");
        sb.AppendLine();

        // Delete collections first (children before parent)
        foreach (var cascade in cascades.Where(c => c.IsCollection))
        {
            var fkColumn = cascade.MappedBy != null
                ? $"{info.EntityType}Id".ToLower()
                : $"{cascade.TargetEntityType}Id".ToLower();
            var relatedTableName = GetTableNameFromMetadata(info, cascade.TargetEntityType) ?? cascade.TargetEntityType;

            sb.AppendLine($"            // Cascade remove {cascade.PropertyName} collection (delete children first)");
            sb.AppendLine($"            var fkColumn{cascade.PropertyName} = \"{fkColumn}\";");
            sb.AppendLine($"            var sql{cascade.PropertyName} = $\"SELECT * FROM {relatedTableName} WHERE {{fkColumn{cascade.PropertyName}}} = @ParentId\";");
            sb.AppendLine($"            var {cascade.PropertyName.ToLower()}Items = await _connection.QueryAsync<{cascade.TargetEntityFullType}>(sql{cascade.PropertyName}, new {{ ParentId = id }});");
            sb.AppendLine($"            ");
            sb.AppendLine($"            foreach (var item in {cascade.PropertyName.ToLower()}Items)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                await _entityManager.RemoveAsync(item);");
            sb.AppendLine($"            }}");
            sb.AppendLine();
        }

        // Handle single entity cascades
        foreach (var cascade in cascades.Where(c => !c.IsCollection))
        {
            sb.AppendLine($"            // Cascade remove {cascade.PropertyName}");
            sb.AppendLine($"            if (entity.{cascade.PropertyName} != null)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                await _entityManager.RemoveAsync(entity.{cascade.PropertyName});");
            sb.AppendLine($"            }}");
            sb.AppendLine();
        }

        sb.AppendLine($"            // Delete the main entity");
        sb.AppendLine($"            await DeleteAsync(id);");
        sb.AppendLine($"        }}");
        sb.AppendLine();

        return sb.ToString();
    }

    // Generate orphan removal override for UpdateAsync
    private static string GenerateOrphanRemovalUpdateOverride(RepositoryInfo info)
    {
        var sb = new StringBuilder();
        var orphanRemovalRelationships = info.OrphanRemovalRelationships;

        sb.AppendLine("        #region Orphan Removal Support");
        sb.AppendLine();
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Updates an entity with orphan removal support.");
        sb.AppendLine($"        /// Automatically deletes orphaned child entities that are no longer referenced.");
        sb.AppendLine($"        /// Orphan removal enabled for: {string.Join(", ", orphanRemovalRelationships.Select(r => r.PropertyName))}");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public override async Task UpdateAsync({info.EntityType} entity)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            if (entity == null) throw new ArgumentNullException(nameof(entity));");
        sb.AppendLine();
        
        var keyPropertyName = GetKeyPropertyName(info);
        
        // Load existing entity with relationships to compare
        sb.AppendLine($"            // Load existing entity to detect orphaned relationships");
        sb.AppendLine($"            var existing = await GetByIdAsync(entity.{keyPropertyName});");
        sb.AppendLine($"            if (existing == null)");
        sb.AppendLine($"                throw new InvalidOperationException($\"{info.EntityType} with id {{entity.{keyPropertyName}}} not found\");");
        sb.AppendLine();

        // Process each orphan removal relationship
        // Note: ManyToOne relationships are NOT supported for orphan removal because:
        // - They are the inverse side of OneToMany (the "many" side)
        // - Removing the relationship only sets the FK to null, doesn't delete the parent
        // - The parent entity should not be deleted when a child removes the reference
        foreach (var rel in orphanRemovalRelationships)
        {
            if (rel.Type == Models.RelationshipType.ManyToMany)
            {
                // ManyToMany: Collection orphan removal (uses join table)
                GenerateManyToManyOrphanRemoval(sb, info, rel, keyPropertyName);
            }
            else if (rel.IsCollection)
            {
                // OneToMany: Collection orphan removal
                GenerateOneToManyOrphanRemoval(sb, info, rel, keyPropertyName);
            }
            else if (rel.Type == Models.RelationshipType.OneToOne)
            {
                // OneToOne: Single entity orphan removal
                GenerateOneToOneOrphanRemoval(sb, info, rel, keyPropertyName);
            }
            // ManyToOne is explicitly NOT supported - see comment above
        }

        sb.AppendLine($"            // Update the main entity");
        sb.AppendLine($"            await base.UpdateAsync(entity);");
        sb.AppendLine($"        }}");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();

        return sb.ToString();
    }

    private static void GenerateOneToManyOrphanRemoval(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata rel, string keyPropertyName)
    {
        // Determine FK column name from MappedBy or convention
        var fkColumn = rel.MappedBy != null
            ? $"{info.EntityType.Split('.').Last()}Id"
            : $"{rel.TargetEntityType}Id";
        
        var relatedTableName = GetTableNameFromMetadata(info, rel.TargetEntityType) ?? rel.TargetEntityType;
        var relatedKeyPropertyName = GetKeyPropertyName(info, rel.TargetEntityType);
        
        sb.AppendLine($"            // Orphan removal for {rel.PropertyName} collection (OneToMany)");
        sb.AppendLine($"            if (entity.{rel.PropertyName} != null)");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                var currentItems = entity.{rel.PropertyName}.ToList();");
        sb.AppendLine($"                ");
        sb.AppendLine($"                // Load existing items from database");
        sb.AppendLine($"                var fkColumnName = \"{fkColumn}\";");
        sb.AppendLine($"                var sql = $\"SELECT * FROM {relatedTableName} WHERE {{fkColumnName}} = @ParentId\";");
        sb.AppendLine($"                var existingItems = (await _connection.QueryAsync<{rel.TargetEntityFullType}>(sql, new {{ ParentId = entity.{keyPropertyName} }})).ToList();");
        sb.AppendLine($"                ");
        sb.AppendLine($"                // Identify orphaned items (in existing but not in current)");
        sb.AppendLine($"                var currentIds = currentItems.Where(i => i.{relatedKeyPropertyName} != default).Select(i => i.{relatedKeyPropertyName}).ToHashSet();");
        sb.AppendLine($"                ");
        sb.AppendLine($"                // Delete orphaned items");
        sb.AppendLine($"                foreach (var existing in existingItems)");
        sb.AppendLine($"                {{");
        sb.AppendLine($"                    if (!currentIds.Contains(existing.{relatedKeyPropertyName}))");
        sb.AppendLine($"                    {{");
        sb.AppendLine($"                        await _entityManager.RemoveAsync(existing);");
        sb.AppendLine($"                    }}");
        sb.AppendLine($"                }}");
        sb.AppendLine($"            }}");
        sb.AppendLine($"            else");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                // Collection is null - delete all existing items (orphan removal)");
        sb.AppendLine($"                var fkColumnName = \"{fkColumn}\";");
        sb.AppendLine($"                var sql = $\"SELECT * FROM {relatedTableName} WHERE {{fkColumnName}} = @ParentId\";");
        sb.AppendLine($"                var existingItems = (await _connection.QueryAsync<{rel.TargetEntityFullType}>(sql, new {{ ParentId = entity.{keyPropertyName} }})).ToList();");
        sb.AppendLine($"                ");
        sb.AppendLine($"                foreach (var existing in existingItems)");
        sb.AppendLine($"                {{");
        sb.AppendLine($"                    await _entityManager.RemoveAsync(existing);");
        sb.AppendLine($"                }}");
        sb.AppendLine($"            }}");
        sb.AppendLine();
    }

    private static void GenerateOneToOneOrphanRemoval(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata rel, string keyPropertyName)
    {
        var relatedKeyPropertyName = GetKeyPropertyName(info, rel.TargetEntityType);
        var relatedTableName = GetTableNameFromMetadata(info, rel.TargetEntityType) ?? rel.TargetEntityType;
        
        // Determine FK column - for OneToOne, it could be on either side
        var fkColumn = rel.JoinColumn?.Name;
        if (string.IsNullOrEmpty(fkColumn))
        {
            // If owner side, FK is on target entity pointing to this entity
            if (rel.IsOwner)
            {
                fkColumn = $"{info.EntityType.Split('.').Last()}Id";
            }
            else
            {
                // Inverse side - FK is on this entity pointing to target
                fkColumn = $"{rel.TargetEntityType}Id";
            }
        }
        
        sb.AppendLine($"            // Orphan removal for {rel.PropertyName} (OneToOne)");
        sb.AppendLine($"            ");
        if (rel.IsOwner)
        {
            // Owner side: FK is on target entity
            sb.AppendLine($"            // Load existing related entity (owner side - FK on target)");
            sb.AppendLine($"            var fkColumnName = \"{fkColumn}\";");
            sb.AppendLine($"            var existingSql = $\"SELECT * FROM {relatedTableName} WHERE {{fkColumnName}} = @ParentId\";");
            sb.AppendLine($"            var existingRelated = await _connection.QueryFirstOrDefaultAsync<{rel.TargetEntityFullType}>(existingSql, new {{ ParentId = entity.{keyPropertyName} }});");
        }
        else
        {
            // Inverse side: FK is on current entity, query target entity using FK value
            // Note: existing.{rel.PropertyName} may be null because GetByIdAsync doesn't eagerly load relationships
            // So we need to query the current entity's table to get the FK value, then query the target entity
            var currentTableName = GetTableNameFromMetadata(info, info.EntityType) ?? GetTableName(info.EntityType);
            var relatedKeyType = GetRelatedEntityKeyType(info, rel.TargetEntityType);
            var defaultKeyValue = relatedKeyType == "Guid" ? "Guid.Empty" : relatedKeyType == "int" ? "0" : relatedKeyType == "long" ? "0L" : $"default({relatedKeyType})";
            sb.AppendLine($"            // Load existing related entity (inverse side - FK on current entity)");
            sb.AppendLine($"            // Query current entity's table to get FK value, then query target entity");
            sb.AppendLine($"            var fkColumnName = \"{fkColumn}\";");
            sb.AppendLine($"            var fkValueSql = $\"SELECT {{fkColumnName}} FROM {currentTableName} WHERE {GetKeyPropertyName(info)} = @ParentId\";");
            sb.AppendLine($"            var fkValue = await _connection.QueryFirstOrDefaultAsync<{relatedKeyType}>(fkValueSql, new {{ ParentId = entity.{keyPropertyName} }});");
            // Check if FK value is valid (not null for reference types, not default for value types)
            // For reference types (string), null check is sufficient. For value types, check against default.
            var isReferenceType = relatedKeyType == "string";
            var fkValueCheck = isReferenceType ? "fkValue != null" : $"fkValue != null && fkValue != {defaultKeyValue}";
            sb.AppendLine($"            var existingRelated = {fkValueCheck} ? await _connection.QueryFirstOrDefaultAsync<{rel.TargetEntityFullType}>($\"SELECT * FROM {relatedTableName} WHERE {relatedKeyPropertyName} = @FkValue\", new {{ FkValue = fkValue }}) : null;");
        }
        sb.AppendLine($"            ");
        sb.AppendLine($"            // Check if relationship was cleared or replaced");
        sb.AppendLine($"            if (existingRelated != null)");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                if (entity.{rel.PropertyName} == null)");
        sb.AppendLine($"                {{");
        sb.AppendLine($"                    // Relationship cleared - delete orphan (orphan removal)");
        sb.AppendLine($"                    await _entityManager.RemoveAsync(existingRelated);");
        sb.AppendLine($"                }}");
        sb.AppendLine($"                else if (entity.{rel.PropertyName}.{relatedKeyPropertyName} != existingRelated.{relatedKeyPropertyName})");
        sb.AppendLine($"                {{");
        sb.AppendLine($"                    // Relationship replaced - delete old orphan (orphan removal)");
        sb.AppendLine($"                    await _entityManager.RemoveAsync(existingRelated);");
        sb.AppendLine($"                }}");
        sb.AppendLine($"            }}");
        sb.AppendLine();
    }

    private static void GenerateManyToManyOrphanRemoval(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata rel, string keyPropertyName)
    {
        // ManyToMany uses a join table, so we need to:
        // 1. Get current items from the collection
        // 2. Get existing items from join table
        // 3. Find items that were removed (in existing but not in current)
        // 4. Check if removed items are referenced by other entities
        // 5. Delete only if not referenced elsewhere (true orphan removal)
        
        var joinTable = rel.JoinTable;
        string joinTableName;
        string ownerKeyColumn;
        string targetKeyColumn;
        var relatedKeyPropertyName = GetKeyPropertyName(info, rel.TargetEntityType);
        var relatedKeyType = GetRelatedEntityKeyType(info, rel.TargetEntityType);
        
        if (joinTable == null)
        {
            // Fallback to convention-based join table name
            var entityName = info.EntityType.Split('.').Last();
            var relatedName = rel.TargetEntityType.Split('.').Last();
            joinTableName = $"{entityName}{relatedName}";
            ownerKeyColumn = $"{entityName}Id";
            targetKeyColumn = $"{relatedName}Id";
        }
        else
        {
            // Use join table metadata if available
            joinTableName = string.IsNullOrEmpty(joinTable.Schema)
                ? joinTable.Name
                : $"{joinTable.Schema}.{joinTable.Name}";
            ownerKeyColumn = joinTable.JoinColumns.FirstOrDefault() ?? $"{info.EntityType.Split('.').Last()}Id";
            targetKeyColumn = joinTable.InverseJoinColumns.FirstOrDefault() ?? $"{rel.TargetEntityType.Split('.').Last()}Id";
        }
        
        sb.AppendLine($"            // Orphan removal for {rel.PropertyName} collection (ManyToMany)");
        sb.AppendLine($"            // Note: ManyToMany orphan removal checks if entities are referenced elsewhere");
        sb.AppendLine($"            if (entity.{rel.PropertyName} != null)");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                var currentItems = entity.{rel.PropertyName}.ToList();");
        sb.AppendLine($"                ");
        sb.AppendLine($"                // Load existing relationships from join table");
        sb.AppendLine($"                var sql = $\"SELECT {targetKeyColumn} FROM {joinTableName} WHERE {ownerKeyColumn} = @ParentId\";");
        sb.AppendLine($"                var existingRelatedIds = (await _connection.QueryAsync<{relatedKeyType}>(sql, new {{ ParentId = entity.{keyPropertyName} }})).ToList();");
        sb.AppendLine($"                ");
        sb.AppendLine($"                var currentIds = currentItems.Where(i => i.{relatedKeyPropertyName} != default).Select(i => i.{relatedKeyPropertyName}).ToHashSet();");
        sb.AppendLine($"                ");
        sb.AppendLine($"                // Find removed items (in existing but not in current)");
        sb.AppendLine($"                var removedIds = existingRelatedIds.Except(currentIds).ToList();");
        sb.AppendLine($"                ");
        sb.AppendLine($"                // For each removed item, check if it's referenced by other entities");
        sb.AppendLine($"                foreach (var removedId in removedIds)");
        sb.AppendLine($"                {{");
        sb.AppendLine($"                    // Check if this entity is referenced by other entities in the join table");
        sb.AppendLine($"                    var checkSql = $\"SELECT COUNT(*) FROM {joinTableName} WHERE {targetKeyColumn} = @RemovedId AND {ownerKeyColumn} != @ParentId\";");
        sb.AppendLine($"                    var referenceCount = await _connection.QuerySingleAsync<int>(checkSql, new {{ RemovedId = removedId, ParentId = entity.{keyPropertyName} }});");
        sb.AppendLine($"                    ");
        sb.AppendLine($"                    // If not referenced elsewhere, delete the orphaned entity");
        sb.AppendLine($"                    if (referenceCount == 0)");
        sb.AppendLine($"                    {{");
        sb.AppendLine($"                        var orphanedEntity = await _entityManager.FindAsync<{rel.TargetEntityFullType}>(removedId);");
        sb.AppendLine($"                        if (orphanedEntity != null)");
        sb.AppendLine($"                        {{");
        sb.AppendLine($"                            await _entityManager.RemoveAsync(orphanedEntity);");
        sb.AppendLine($"                        }}");
        sb.AppendLine($"                    }}");
        sb.AppendLine($"                }}");
        sb.AppendLine($"            }}");
        sb.AppendLine($"            else");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                // Collection is null - check all existing relationships for orphan removal");
        sb.AppendLine($"                var sql = $\"SELECT {targetKeyColumn} FROM {joinTableName} WHERE {ownerKeyColumn} = @ParentId\";");
        sb.AppendLine($"                var existingRelatedIds = (await _connection.QueryAsync<{relatedKeyType}>(sql, new {{ ParentId = entity.{keyPropertyName} }})).ToList();");
        sb.AppendLine($"                ");
        sb.AppendLine($"                foreach (var relatedId in existingRelatedIds)");
        sb.AppendLine($"                {{");
        sb.AppendLine($"                    // Check if this entity is referenced by other entities");
        sb.AppendLine($"                    var checkSql = $\"SELECT COUNT(*) FROM {joinTableName} WHERE {targetKeyColumn} = @RelatedId AND {ownerKeyColumn} != @ParentId\";");
        sb.AppendLine($"                    var referenceCount = await _connection.QuerySingleAsync<int>(checkSql, new {{ RelatedId = relatedId, ParentId = entity.{keyPropertyName} }});");
        sb.AppendLine($"                    ");
        sb.AppendLine($"                    // If not referenced elsewhere, delete the orphaned entity");
        sb.AppendLine($"                    if (referenceCount == 0)");
        sb.AppendLine($"                    {{");
        sb.AppendLine($"                        var orphanedEntity = await _entityManager.FindAsync<{rel.TargetEntityFullType}>(relatedId);");
        sb.AppendLine($"                        if (orphanedEntity != null)");
        sb.AppendLine($"                        {{");
        sb.AppendLine($"                            await _entityManager.RemoveAsync(orphanedEntity);");
        sb.AppendLine($"                        }}");
        sb.AppendLine($"                    }}");
        sb.AppendLine($"                }}");
        sb.AppendLine($"            }}");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a static dictionary mapping property names to column names for sorting support.
    /// </summary>
    private static string GeneratePropertyColumnMapping(RepositoryInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("        #region Property-to-Column Mapping");
        sb.AppendLine();
        sb.AppendLine("        private static readonly Dictionary<string, string> _propertyColumnMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)");
        sb.AppendLine("        {");

        if (info.EntityMetadata?.Properties != null)
        {
            foreach (var property in info.EntityMetadata.Properties)
            {
                if (!string.IsNullOrEmpty(property.Name) && !string.IsNullOrEmpty(property.ColumnName))
                {
                    sb.AppendLine($"            {{ \"{property.Name}\", \"{property.ColumnName}\" }},");
                }
            }
        }

        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        private static string GetColumnNameForProperty(string? propertyName, string defaultColumnName)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (string.IsNullOrEmpty(propertyName))");
        sb.AppendLine("                return defaultColumnName;");
        sb.AppendLine();
        sb.AppendLine("            // Security: Only return column names that exist in the map to prevent SQL injection");
        sb.AppendLine("            // If property name is not found, return default column name instead of unsanitized input");
        sb.AppendLine("            return _propertyColumnMap.TryGetValue(propertyName, out var columnName) ? columnName : defaultColumnName;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();

        return sb.ToString();
    }

    // Generate relationship query methods
    private static string GenerateRelationshipQueryMethods(RepositoryInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("        #region Relationship Query Methods");
        sb.AppendLine();

        var entityName = info.EntityType.Split('.').Last();
        var tableName = info.EntityMetadata?.TableName ?? entityName;

        foreach (var relationship in info.Relationships)
        {
            // Generate FindBy methods for ManyToOne relationships (find by parent)
            if (relationship.Type == Models.RelationshipType.ManyToOne)
            {
                GenerateFindByParentMethod(sb, info, relationship, tableName);
                GenerateCountByParentMethod(sb, info, relationship, tableName);
                // Generate property-based queries (e.g., FindByCustomerNameAsync)
                GeneratePropertyBasedQueries(sb, info, relationship, tableName);
                // Generate advanced filters (date ranges, amount filters)
                GenerateAdvancedFilters(sb, info, relationship, tableName);
                // Generate complex filters (OR/AND combinations)
                GenerateComplexFilters(sb, info, relationship, tableName);
            }

            // Generate Has/Count methods for OneToMany relationships (check if parent has children)
            if (relationship.Type == Models.RelationshipType.OneToMany && !string.IsNullOrEmpty(relationship.MappedBy))
            {
                GenerateHasChildrenMethod(sb, info, relationship);
                GenerateCountChildrenMethod(sb, info, relationship);
                // Generate aggregate methods for numeric properties
                GenerateAggregateMethods(sb, info, relationship);
                // Generate GROUP BY aggregate methods
                GenerateGroupByAggregateMethods(sb, info, relationship);
                // Generate subquery-based filters
                GenerateSubqueryFilters(sb, info, relationship);
                // Generate inverse relationship queries (FindWith/Without/WithCount)
                GenerateInverseRelationshipQueries(sb, info, relationship);
            }
        }

        // Generate multi-level navigation queries (e.g., OrderItem → Order → Customer)
        GenerateMultiLevelNavigationQueries(sb, info, tableName);

        sb.AppendLine("        #endregion");
        sb.AppendLine();

        return sb.ToString();
    }

    private static void GenerateFindByParentMethod(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string tableName)
    {
        var targetEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        var foreignKeyColumn = relationship.JoinColumn?.Name ?? $"{targetEntitySimpleName}Id";
        var paramName = ToCamelCase(targetEntitySimpleName) + "Id";
        var keyColumnName = GetKeyColumnName(info);
        var relatedKeyType = GetRelatedEntityKeyType(info, relationship.TargetEntityType);

        // Generate method without pagination (backward compatibility)
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}IdAsync({relatedKeyType} {paramName})");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = \"SELECT * FROM {tableName} WHERE {foreignKeyColumn} = @{paramName} ORDER BY {keyColumnName}\";");
        sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {paramName} }});");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate method with pagination
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} with pagination support.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <param name=\"{paramName}\">The {relationship.PropertyName} identifier.</param>");
        sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
        sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}IdAsync({relatedKeyType} {paramName}, int skip, int take)");
        sb.AppendLine("        {");
        // Use database-specific pagination syntax (will need provider-specific handling, but for now use OFFSET/FETCH which works in SQL Server, PostgreSQL, SQLite)
        sb.AppendLine($"            var sql = $\"SELECT * FROM {tableName} WHERE {foreignKeyColumn} = @{paramName} ORDER BY {keyColumnName} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
        sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {paramName}, skip, take }});");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate method with pagination and sorting
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} with pagination and sorting support.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <param name=\"{paramName}\">The {relationship.PropertyName} identifier.</param>");
        sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
        sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
        sb.AppendLine("        /// <param name=\"orderBy\">Property name to order by. Defaults to primary key if null or empty.</param>");
        sb.AppendLine("        /// <param name=\"ascending\">Sort direction. True for ascending, false for descending.</param>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}IdAsync({relatedKeyType} {paramName}, int skip, int take, string? orderBy = null, bool ascending = true)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var orderByColumn = GetColumnNameForProperty(orderBy, \"{keyColumnName}\");");
        sb.AppendLine($"            var direction = ascending ? \"ASC\" : \"DESC\";");
        sb.AppendLine($"            var sql = $\"SELECT * FROM {tableName} WHERE {foreignKeyColumn} = @{paramName} ORDER BY {{orderByColumn}} {{direction}} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
        sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {paramName}, skip, take }});");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateCountByParentMethod(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string tableName)
    {
        var targetEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        var foreignKeyColumn = relationship.JoinColumn?.Name ?? $"{targetEntitySimpleName}Id";
        var paramName = ToCamelCase(targetEntitySimpleName) + "Id";
        var relatedKeyType = GetRelatedEntityKeyType(info, relationship.TargetEntityType);

        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Counts {info.EntityType} entities by {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<int> CountBy{relationship.PropertyName}IdAsync({relatedKeyType} {paramName})");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = \"SELECT COUNT(*) FROM {tableName} WHERE {foreignKeyColumn} = @{paramName}\";");
        sb.AppendLine($"            return await _connection.ExecuteScalarAsync<int>(sql, new {{ {paramName} }});");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateHasChildrenMethod(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        var childTableName = GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relationship.TargetEntityType;
        var parentEntityName = info.EntityType.Split('.').Last();
        // Get FK column from inverse ManyToOne relationship
        var foreignKeyColumn = GetForeignKeyColumnForOneToMany(info, relationship, parentEntityName);

        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Checks if the entity has any {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<bool> Has{relationship.PropertyName}Async({info.KeyType} id)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = \"SELECT COUNT(*) FROM {childTableName} WHERE {foreignKeyColumn} = @id\";");
        sb.AppendLine($"            var count = await _connection.ExecuteScalarAsync<int>(sql, new {{ id }});");
        sb.AppendLine($"            return count > 0;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateCountChildrenMethod(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        var childTableName = GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relationship.TargetEntityType;
        var parentEntityName = info.EntityType.Split('.').Last();
        // Get FK column from inverse ManyToOne relationship
        var foreignKeyColumn = GetForeignKeyColumnForOneToMany(info, relationship, parentEntityName);

        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Counts the number of {relationship.PropertyName} for the entity.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<int> Count{relationship.PropertyName}Async({info.KeyType} id)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = \"SELECT COUNT(*) FROM {childTableName} WHERE {foreignKeyColumn} = @id\";");
        sb.AppendLine($"            return await _connection.ExecuteScalarAsync<int>(sql, new {{ id }});");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates property-based query methods for ManyToOne relationships.
    /// For example, FindByCustomerNameAsync, FindByCustomerEmailAsync, etc.
    /// </summary>
    private static void GeneratePropertyBasedQueries(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string tableName)
    {
        // Get related entity metadata
        var relatedEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(relatedEntitySimpleName, out var relatedMetadata))
        {
            return; // Can't generate property-based queries without metadata
        }

        if (relatedMetadata.Properties == null)
        {
            return;
        }

        var targetEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        var foreignKeyColumn = relationship.JoinColumn?.Name ?? $"{targetEntitySimpleName}Id";
        var relatedTableName = GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relatedEntitySimpleName;
        var keyColumnName = GetKeyColumnName(info);
        // Use column name instead of property name for the JOIN condition
        var relatedKeyColumnName = GetKeyColumnName(info, relationship.TargetEntityType);

        // Generate query methods for each property of the related entity (excluding primary key and relationships)
        foreach (var property in relatedMetadata.Properties)
        {
            // Skip primary key, relationships, and complex types
            if (property.IsPrimaryKey)
                continue;

            // Skip if property type is a collection or complex object (likely a relationship)
            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") || 
                property.TypeName.Contains("IEnumerable") || !IsSimpleType(property.TypeName))
                continue;

            var propertyParamName = ToCamelCase(property.Name);
            var methodName = $"FindBy{relationship.PropertyName}{property.Name}Async";

            // Generate method without pagination (backward compatibility)
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName}.{property.Name}.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName})");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
            sb.AppendLine($"                INNER JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE r.{property.ColumnName} = @{propertyParamName}");
            sb.AppendLine($"                ORDER BY e.{keyColumnName}\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {propertyParamName} }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate method with pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName}.{property.Name} with pagination support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{propertyParamName}\">The {property.Name} value.</param>");
            sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
            sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName}, int skip, int take)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
            sb.AppendLine($"                INNER JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE r.{property.ColumnName} = @{propertyParamName}");
            sb.AppendLine($"                ORDER BY e.{keyColumnName} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {propertyParamName}, skip, take }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate method with pagination and sorting
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName}.{property.Name} with pagination and sorting support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{propertyParamName}\">The {property.Name} value.</param>");
            sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
            sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
            sb.AppendLine("        /// <param name=\"orderBy\">Property name to order by. Defaults to primary key if null or empty.</param>");
            sb.AppendLine("        /// <param name=\"ascending\">Sort direction. True for ascending, false for descending.</param>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName}, int skip, int take, string? orderBy = null, bool ascending = true)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var orderByColumn = GetColumnNameForProperty(orderBy, \"{keyColumnName}\");");
            sb.AppendLine($"            var direction = ascending ? \"ASC\" : \"DESC\";");
            sb.AppendLine($"            var sql = $\"SELECT e.* FROM {tableName} e");
            sb.AppendLine($"                INNER JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE r.{property.ColumnName} = @{propertyParamName}");
            sb.AppendLine($"                ORDER BY e.{{orderByColumn}} {{direction}} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {propertyParamName}, skip, take }});");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates advanced filter methods for ManyToOne relationships.
    /// For example, FindByCustomerAndDateRangeAsync, FindCustomerOrdersAboveAmountAsync, etc.
    /// </summary>
    private static void GenerateAdvancedFilters(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string tableName)
    {
        // Get related entity metadata
        var relatedEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(relatedEntitySimpleName, out var relatedMetadata))
        {
            return; // Can't generate advanced filters without metadata
        }

        // Get current entity metadata for date/amount filters on the current entity
        if (info.EntityMetadata?.Properties == null)
        {
            return;
        }

        var targetEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        var foreignKeyColumn = relationship.JoinColumn?.Name ?? $"{targetEntitySimpleName}Id";
        var relatedTableName = GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relatedEntitySimpleName;
        var keyColumnName = GetKeyColumnName(info);
        var relatedKeyColumnName = GetKeyColumnName(info, relationship.TargetEntityType);
        var relatedKeyType = GetRelatedEntityKeyType(info, relationship.TargetEntityType);
        var relatedKeyParamName = ToCamelCase(targetEntitySimpleName) + "Id";

        // Generate date range filters for DateTime properties on the current entity
        foreach (var property in info.EntityMetadata.Properties)
        {
            if (property.IsPrimaryKey)
                continue;

            if (!IsDateTimeType(property.TypeName))
                continue;

            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") ||
                property.TypeName.Contains("IEnumerable"))
                continue;

            var propertyParamName = ToCamelCase(property.Name);
            var propertyColumnName = property.ColumnName;

            // Generate date range filter with relationship (without pagination)
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName} and {property.Name} date range.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}And{property.Name}RangeAsync({relatedKeyType} {relatedKeyParamName}, DateTime start{property.Name}, DateTime end{property.Name})");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
            sb.AppendLine($"                INNER JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE e.{foreignKeyColumn} = @{relatedKeyParamName}");
            sb.AppendLine($"                    AND e.{propertyColumnName} >= @start{property.Name}");
            sb.AppendLine($"                    AND e.{propertyColumnName} <= @end{property.Name}");
            sb.AppendLine($"                ORDER BY e.{keyColumnName}\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {relatedKeyParamName}, start{property.Name}, end{property.Name} }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate date range filter with pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} and {property.Name} date range with pagination support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{relatedKeyParamName}\">The {relationship.PropertyName} identifier.</param>");
            sb.AppendLine($"        /// <param name=\"start{property.Name}\">Start date.</param>");
            sb.AppendLine($"        /// <param name=\"end{property.Name}\">End date.</param>");
            sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
            sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}And{property.Name}RangeAsync({relatedKeyType} {relatedKeyParamName}, DateTime start{property.Name}, DateTime end{property.Name}, int skip, int take)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
            sb.AppendLine($"                INNER JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE e.{foreignKeyColumn} = @{relatedKeyParamName}");
            sb.AppendLine($"                    AND e.{propertyColumnName} >= @start{property.Name}");
            sb.AppendLine($"                    AND e.{propertyColumnName} <= @end{property.Name}");
            sb.AppendLine($"                ORDER BY e.{keyColumnName} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {relatedKeyParamName}, start{property.Name}, end{property.Name}, skip, take }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate date range filter with pagination and sorting
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} and {property.Name} date range with pagination and sorting support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{relatedKeyParamName}\">The {relationship.PropertyName} identifier.</param>");
            sb.AppendLine($"        /// <param name=\"start{property.Name}\">Start date.</param>");
            sb.AppendLine($"        /// <param name=\"end{property.Name}\">End date.</param>");
            sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
            sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
            sb.AppendLine("        /// <param name=\"orderBy\">Property name to order by. Defaults to primary key if null or empty.</param>");
            sb.AppendLine("        /// <param name=\"ascending\">Sort direction. True for ascending, false for descending.</param>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}And{property.Name}RangeAsync({relatedKeyType} {relatedKeyParamName}, DateTime start{property.Name}, DateTime end{property.Name}, int skip, int take, string? orderBy = null, bool ascending = true)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var orderByColumn = GetColumnNameForProperty(orderBy, \"{keyColumnName}\");");
            sb.AppendLine($"            var direction = ascending ? \"ASC\" : \"DESC\";");
            sb.AppendLine($"            var sql = $\"SELECT e.* FROM {tableName} e");
            sb.AppendLine($"                INNER JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE e.{foreignKeyColumn} = @{relatedKeyParamName}");
            sb.AppendLine($"                    AND e.{propertyColumnName} >= @start{property.Name}");
            sb.AppendLine($"                    AND e.{propertyColumnName} <= @end{property.Name}");
            sb.AppendLine($"                ORDER BY e.{{orderByColumn}} {{direction}} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {relatedKeyParamName}, start{property.Name}, end{property.Name}, skip, take }});");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        // Generate amount/quantity filters for numeric properties on the current entity
        foreach (var property in info.EntityMetadata.Properties)
        {
            if (property.IsPrimaryKey)
                continue;

            if (!IsNumericType(property.TypeName))
                continue;

            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") ||
                property.TypeName.Contains("IEnumerable"))
                continue;

            var propertyParamName = ToCamelCase(property.Name);
            var propertyColumnName = property.ColumnName;
            var returnType = property.TypeName.TrimEnd('?');

            // Generate minimum amount filter with relationship (without pagination)
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName} with {property.Name} greater than or equal to the specified value.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> Find{relationship.PropertyName}{property.Name}AboveAsync({relatedKeyType} {relatedKeyParamName}, {returnType} min{property.Name})");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
            sb.AppendLine($"                INNER JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE e.{foreignKeyColumn} = @{relatedKeyParamName}");
            sb.AppendLine($"                    AND e.{propertyColumnName} >= @min{property.Name}");
            sb.AppendLine($"                ORDER BY e.{keyColumnName}\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {relatedKeyParamName}, min{property.Name} }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate minimum amount filter with pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} with {property.Name} greater than or equal to the specified value, with pagination support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{relatedKeyParamName}\">The {relationship.PropertyName} identifier.</param>");
            sb.AppendLine($"        /// <param name=\"min{property.Name}\">Minimum {property.Name} value.</param>");
            sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
            sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> Find{relationship.PropertyName}{property.Name}AboveAsync({relatedKeyType} {relatedKeyParamName}, {returnType} min{property.Name}, int skip, int take)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
            sb.AppendLine($"                INNER JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE e.{foreignKeyColumn} = @{relatedKeyParamName}");
            sb.AppendLine($"                    AND e.{propertyColumnName} >= @min{property.Name}");
            sb.AppendLine($"                ORDER BY e.{keyColumnName} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {relatedKeyParamName}, min{property.Name}, skip, take }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate minimum amount filter with pagination and sorting
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} with {property.Name} greater than or equal to the specified value, with pagination and sorting support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{relatedKeyParamName}\">The {relationship.PropertyName} identifier.</param>");
            sb.AppendLine($"        /// <param name=\"min{property.Name}\">Minimum {property.Name} value.</param>");
            sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
            sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
            sb.AppendLine("        /// <param name=\"orderBy\">Property name to order by. Defaults to primary key if null or empty.</param>");
            sb.AppendLine("        /// <param name=\"ascending\">Sort direction. True for ascending, false for descending.</param>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> Find{relationship.PropertyName}{property.Name}AboveAsync({relatedKeyType} {relatedKeyParamName}, {returnType} min{property.Name}, int skip, int take, string? orderBy = null, bool ascending = true)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var orderByColumn = GetColumnNameForProperty(orderBy, \"{keyColumnName}\");");
            sb.AppendLine($"            var direction = ascending ? \"ASC\" : \"DESC\";");
            sb.AppendLine($"            var sql = $\"SELECT e.* FROM {tableName} e");
            sb.AppendLine($"                INNER JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE e.{foreignKeyColumn} = @{relatedKeyParamName}");
            sb.AppendLine($"                    AND e.{propertyColumnName} >= @min{property.Name}");
            sb.AppendLine($"                ORDER BY e.{{orderByColumn}} {{direction}} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {relatedKeyParamName}, min{property.Name}, skip, take }});");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates subquery-based filter methods for OneToMany relationships.
    /// For example, FindWithMinimumItemsAsync - finds entities with at least N child entities.
    /// </summary>
    private static void GenerateSubqueryFilters(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Get child entity metadata
        var childEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(childEntitySimpleName, out var childMetadata))
        {
            return; // Can't generate subquery filters without metadata
        }

        var childTableName = GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? childEntitySimpleName;
        var parentEntityName = info.EntityType.Split('.').Last();
        var tableName = GetTableNameFromMetadata(info, info.EntityType) ?? parentEntityName;
        var keyColumnName = GetKeyColumnName(info);
        
        // For OneToMany, the JoinColumn is on the inverse ManyToOne relationship
        var foreignKeyColumn = GetForeignKeyColumnForOneToMany(info, relationship, parentEntityName);

        // Generate FindWithMinimum{Property}Async - finds parents with at least N children (without pagination)
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities that have at least the specified number of {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindWithMinimum{relationship.PropertyName}Async(int minCount)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
        sb.AppendLine($"                WHERE (");
        sb.AppendLine($"                    SELECT COUNT(*)");
        sb.AppendLine($"                    FROM {childTableName} c");
        sb.AppendLine($"                    WHERE c.{foreignKeyColumn} = e.{keyColumnName}");
        sb.AppendLine($"                ) >= @minCount");
        sb.AppendLine($"                ORDER BY e.{keyColumnName}\";");
        sb.AppendLine("            return await _connection.QueryAsync<" + info.EntityType + ">(sql, new { minCount });");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate FindWithMinimum{Property}Async with pagination
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds {info.EntityType} entities that have at least the specified number of {relationship.PropertyName}, with pagination support.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <param name=\"minCount\">Minimum number of {relationship.PropertyName}.</param>");
        sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
        sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindWithMinimum{relationship.PropertyName}Async(int minCount, int skip, int take)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
        sb.AppendLine($"                WHERE (");
        sb.AppendLine($"                    SELECT COUNT(*)");
        sb.AppendLine($"                    FROM {childTableName} c");
        sb.AppendLine($"                    WHERE c.{foreignKeyColumn} = e.{keyColumnName}");
        sb.AppendLine($"                ) >= @minCount");
        sb.AppendLine($"                ORDER BY e.{keyColumnName} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
        sb.AppendLine("            return await _connection.QueryAsync<" + info.EntityType + ">(sql, new { minCount, skip, take });");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate FindWithMinimum{Property}Async with pagination and sorting
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds {info.EntityType} entities that have at least the specified number of {relationship.PropertyName}, with pagination and sorting support.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <param name=\"minCount\">Minimum number of {relationship.PropertyName}.</param>");
        sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
        sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
        sb.AppendLine("        /// <param name=\"orderBy\">Property name to order by. Defaults to primary key if null or empty.</param>");
        sb.AppendLine("        /// <param name=\"ascending\">Sort direction. True for ascending, false for descending.</param>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindWithMinimum{relationship.PropertyName}Async(int minCount, int skip, int take, string? orderBy = null, bool ascending = true)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var orderByColumn = GetColumnNameForProperty(orderBy, \"{keyColumnName}\");");
        sb.AppendLine($"            var direction = ascending ? \"ASC\" : \"DESC\";");
        sb.AppendLine($"            var sql = $\"SELECT e.* FROM {tableName} e");
        sb.AppendLine($"                WHERE (");
        sb.AppendLine($"                    SELECT COUNT(*)");
        sb.AppendLine($"                    FROM {childTableName} c");
        sb.AppendLine($"                    WHERE c.{foreignKeyColumn} = e.{keyColumnName}");
        sb.AppendLine($"                ) >= @minCount");
        sb.AppendLine($"                ORDER BY e.{{orderByColumn}} {{direction}} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
        sb.AppendLine("            return await _connection.QueryAsync<" + info.EntityType + ">(sql, new { minCount, skip, take });");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates inverse relationship query methods for OneToMany relationships.
    /// For example, FindWithOrdersAsync, FindWithoutOrdersAsync, FindWithOrderCountAsync.
    /// These methods are generated on the parent entity (e.g., Customer) to find entities based on their child relationships.
    /// </summary>
    private static void GenerateInverseRelationshipQueries(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Get child entity metadata
        var childEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(childEntitySimpleName, out var childMetadata))
        {
            return; // Can't generate inverse queries without metadata
        }

        var childTableName = GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? childEntitySimpleName;
        var parentEntityName = info.EntityType.Split('.').Last();
        var tableName = GetTableNameFromMetadata(info, info.EntityType) ?? parentEntityName;
        var keyColumnName = GetKeyColumnName(info);
        
        // For OneToMany, the JoinColumn is on the inverse ManyToOne relationship
        var foreignKeyColumn = GetForeignKeyColumnForOneToMany(info, relationship, parentEntityName);

        // Generate FindWith{Property}Async - finds parents that have at least one child
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities that have at least one {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindWith{relationship.PropertyName}Async()");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = @\"SELECT DISTINCT e.* FROM {tableName} e");
        sb.AppendLine($"                INNER JOIN {childTableName} c ON c.{foreignKeyColumn} = e.{keyColumnName}");
        sb.AppendLine($"                ORDER BY e.{keyColumnName}\";");
        sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql);");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate FindWithout{Property}Async - finds parents that have no children
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities that have no {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindWithout{relationship.PropertyName}Async()");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
        sb.AppendLine($"                WHERE NOT EXISTS (");
        sb.AppendLine($"                    SELECT 1");
        sb.AppendLine($"                    FROM {childTableName} c");
        sb.AppendLine($"                    WHERE c.{foreignKeyColumn} = e.{keyColumnName}");
        sb.AppendLine($"                )");
        sb.AppendLine($"                ORDER BY e.{keyColumnName}\";");
        sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql);");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate FindWith{Property}CountAsync - finds parents with at least N children
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities that have at least the specified number of {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <param name=\"minCount\">Minimum number of {relationship.PropertyName}.</param>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindWith{relationship.PropertyName}CountAsync(int minCount)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
        sb.AppendLine($"                WHERE (");
        sb.AppendLine($"                    SELECT COUNT(*)");
        sb.AppendLine($"                    FROM {childTableName} c");
        sb.AppendLine($"                    WHERE c.{foreignKeyColumn} = e.{keyColumnName}");
        sb.AppendLine($"                ) >= @minCount");
        sb.AppendLine($"                ORDER BY e.{keyColumnName}\";");
        sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ minCount }});");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates multi-level navigation queries.
    /// For example, FindByCustomerNameAsync on OrderItemRepository navigates: OrderItem → Order → Customer
    /// Currently supports 2-level navigation through ManyToOne relationships.
    /// </summary>
    private static void GenerateMultiLevelNavigationQueries(StringBuilder sb, RepositoryInfo info, string tableName)
    {
        if (info.EntitiesMetadata == null || info.EntitiesMetadata.Count == 0)
            return; // Can't generate multi-level queries without metadata

        var entityName = info.EntityType.Split('.').Last();
        var keyColumnName = GetKeyColumnName(info);

        // Find 2-level navigation paths: Current Entity → Intermediate Entity → Target Entity
        // Only support ManyToOne → ManyToOne chains for now
        foreach (var firstLevelRel in info.Relationships)
        {
            if (firstLevelRel.Type != Models.RelationshipType.ManyToOne)
                continue;

            var intermediateEntitySimpleName = firstLevelRel.TargetEntityType.Split('.').Last();
            
            // For multi-level navigation, we need to find entities that the intermediate entity relates to.
            // Since we don't have relationship metadata for intermediate entities, we'll check all entities
            // in metadata and generate queries assuming they might be reachable through the intermediate entity.
            // This is a simplified approach - in practice, we'd need full relationship metadata for all entities.
            
            // Check all entities in metadata to see if they could be targets of a second-level relationship
            foreach (var entityMetadataEntry in info.EntitiesMetadata)
            {
                var targetEntitySimpleName = entityMetadataEntry.Key;
                var targetMetadata = entityMetadataEntry.Value;
                
                // Skip if target is the same as intermediate (no second level needed)
                if (targetEntitySimpleName == intermediateEntitySimpleName)
                    continue;
                
                // Skip if target is the current entity (can't navigate to itself)
                if (targetEntitySimpleName == entityName)
                    continue;
                
                // Skip if we don't have properties for the target
                if (targetMetadata.Properties == null || targetMetadata.Properties.Count == 0)
                    continue;
                
                // Extract relationships from the intermediate entity to find its relationship to the target entity
                // For example, when generating OrderItem → Order → Customer, we need Order's relationship to Customer
                Models.RelationshipMetadata? secondLevelRel = null;
                if (info.Compilation != null)
                {
                    // Try to extract relationships from the intermediate entity
                    var intermediateEntityFullType = firstLevelRel.TargetEntityFullType;
                    var intermediateEntitySimpleNameForExtraction = intermediateEntitySimpleName;
                    
                    var intermediateRelationships = ExtractRelationships(info.Compilation, intermediateEntityFullType);
                    if (intermediateRelationships.Count == 0)
                    {
                        // Try with just the simple name (might need namespace)
                        intermediateRelationships = ExtractRelationships(info.Compilation, intermediateEntitySimpleNameForExtraction);
                    }
                    
                    // Find the ManyToOne relationship from intermediate entity to target entity
                    secondLevelRel = intermediateRelationships.FirstOrDefault(r => 
                        r.Type == Models.RelationshipType.ManyToOne && 
                        r.TargetEntityType.Split('.').Last() == targetEntitySimpleName);
                }
                
                // Only generate queries if the intermediate entity actually has a relationship to the target entity
                if (secondLevelRel == null)
                    continue;

                // Use JoinColumn from the intermediate entity's relationship
                var secondLevelFkColumn = secondLevelRel.JoinColumn?.Name ?? $"{targetEntitySimpleName}Id";
                var secondLevelKeyColumn = GetKeyColumnName(info, targetEntitySimpleName);

                // Generate property-based queries for target entity properties
                if (targetMetadata.Properties == null)
                    continue;
                    
                foreach (var property in targetMetadata.Properties)
                {
                    if (property.IsPrimaryKey)
                        continue;

                    if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") ||
                        property.TypeName.Contains("IEnumerable") || !IsSimpleType(property.TypeName))
                        continue;

                    var methodName = $"FindBy{firstLevelRel.PropertyName}{targetEntitySimpleName}{property.Name}Async";
                    var propertyParamName = ToCamelCase(property.Name);

                    // Build SQL with two JOINs
                    var intermediateTableName = GetTableNameFromMetadata(info, firstLevelRel.TargetEntityType) ?? intermediateEntitySimpleName;
                    var targetTableName = GetTableNameFromMetadata(info, targetEntitySimpleName) ?? targetEntitySimpleName;
                    
                    var firstFkColumn = firstLevelRel.JoinColumn?.Name ?? $"{intermediateEntitySimpleName}Id";
                    var firstKeyColumn = GetKeyColumnName(info, firstLevelRel.TargetEntityType);
                    
                    // Use the pre-calculated FK column and key column
                    var secondFkColumn = secondLevelFkColumn;
                    var secondKeyColumn = secondLevelKeyColumn;

                    // Generate method without pagination
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine($"        /// Finds all {info.EntityType} entities by navigating through {firstLevelRel.PropertyName} → {targetEntitySimpleName} to {targetEntitySimpleName}.{property.Name}.");
                    sb.AppendLine("        /// </summary>");
                    sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName})");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
                    sb.AppendLine($"                INNER JOIN {intermediateTableName} r1 ON e.{firstFkColumn} = r1.{firstKeyColumn}");
                    sb.AppendLine($"                INNER JOIN {targetTableName} r2 ON r1.{secondFkColumn} = r2.{secondKeyColumn}");
                    sb.AppendLine($"                WHERE r2.{property.ColumnName} = @{propertyParamName}");
                    sb.AppendLine($"                ORDER BY e.{keyColumnName}\";");
                    sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {propertyParamName} }});");
                    sb.AppendLine("        }");
                    sb.AppendLine();

                    // Generate method with pagination
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine($"        /// Finds {info.EntityType} entities by navigating through {firstLevelRel.PropertyName} → {targetEntitySimpleName} to {targetEntitySimpleName}.{property.Name}, with pagination support.");
                    sb.AppendLine("        /// </summary>");
                    sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName}, int skip, int take)");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
                    sb.AppendLine($"                INNER JOIN {intermediateTableName} r1 ON e.{firstFkColumn} = r1.{firstKeyColumn}");
                    sb.AppendLine($"                INNER JOIN {targetTableName} r2 ON r1.{secondFkColumn} = r2.{secondKeyColumn}");
                    sb.AppendLine($"                WHERE r2.{property.ColumnName} = @{propertyParamName}");
                    sb.AppendLine($"                ORDER BY e.{keyColumnName} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
                    sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {propertyParamName}, skip, take }});");
                    sb.AppendLine("        }");
                    sb.AppendLine();

                    // Generate method with pagination and sorting
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine($"        /// Finds {info.EntityType} entities by navigating through {firstLevelRel.PropertyName} → {targetEntitySimpleName} to {targetEntitySimpleName}.{property.Name}, with pagination and sorting support.");
                    sb.AppendLine("        /// </summary>");
                    sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName}, int skip, int take, string? orderBy = null, bool ascending = true)");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            var orderByColumn = GetColumnNameForProperty(orderBy, \"{keyColumnName}\");");
                    sb.AppendLine($"            var direction = ascending ? \"ASC\" : \"DESC\";");
                    sb.AppendLine($"            var sql = $\"SELECT e.* FROM {tableName} e");
                    sb.AppendLine($"                INNER JOIN {intermediateTableName} r1 ON e.{firstFkColumn} = r1.{firstKeyColumn}");
                    sb.AppendLine($"                INNER JOIN {targetTableName} r2 ON r1.{secondFkColumn} = r2.{secondKeyColumn}");
                    sb.AppendLine($"                WHERE r2.{property.ColumnName} = @{propertyParamName}");
                    sb.AppendLine($"                ORDER BY e.{{orderByColumn}} {{direction}} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
                    sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {propertyParamName}, skip, take }});");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                }
            }
        }
    }

    /// <summary>
    /// Generates complex filter queries with OR/AND combinations for ManyToOne relationships.
    /// For example, FindByCustomerOrSupplierAsync, FindByCustomerAndStatusAsync.
    /// </summary>
    private static void GenerateComplexFilters(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string tableName)
    {
        var entityName = info.EntityType.Split('.').Last();
        var keyColumnName = GetKeyColumnName(info);
        var targetEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        var foreignKeyColumn = relationship.JoinColumn?.Name ?? $"{targetEntitySimpleName}Id";
        var relatedKeyType = GetRelatedEntityKeyType(info, relationship.TargetEntityType);
        var paramName = ToCamelCase(targetEntitySimpleName) + "Id";

        // Generate OR combinations: FindBy{Property1}Or{Property2}Async (requires at least 2 relationships)
        if (info.Relationships.Count >= 2)
        {
            foreach (var otherRel in info.Relationships)
        {
            if (otherRel == relationship || otherRel.Type != Models.RelationshipType.ManyToOne)
                continue;

            var otherEntitySimpleName = otherRel.TargetEntityType.Split('.').Last();
            var otherFkColumn = otherRel.JoinColumn?.Name ?? $"{otherEntitySimpleName}Id";
            var otherKeyType = GetRelatedEntityKeyType(info, otherRel.TargetEntityType);
            var otherParamName = ToCamelCase(otherEntitySimpleName) + "Id";

            // Generate FindBy{Property1}Or{Property2}Async
            var orMethodName = $"FindBy{relationship.PropertyName}Or{otherRel.PropertyName}Async";
            
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName} or {otherRel.PropertyName}.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{paramName}\">The {relationship.PropertyName} identifier (nullable).</param>");
            sb.AppendLine($"        /// <param name=\"{otherParamName}\">The {otherRel.PropertyName} identifier (nullable).</param>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {orMethodName}({relatedKeyType}? {paramName}, {otherKeyType}? {otherParamName})");
            sb.AppendLine("        {");
            sb.AppendLine($"            var conditions = new List<string>();");
            sb.AppendLine($"            var parameters = new Dictionary<string, object>();");
            sb.AppendLine();
            sb.AppendLine($"            if ({paramName}.HasValue)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                conditions.Add(\"{foreignKeyColumn} = @{paramName}\");");
            sb.AppendLine($"                parameters.Add(\"{paramName}\", {paramName}.Value);");
            sb.AppendLine($"            }}");
            sb.AppendLine();
            sb.AppendLine($"            if ({otherParamName}.HasValue)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                conditions.Add(\"{otherFkColumn} = @{otherParamName}\");");
            sb.AppendLine($"                parameters.Add(\"{otherParamName}\", {otherParamName}.Value);");
            sb.AppendLine($"            }}");
            sb.AppendLine();
            sb.AppendLine($"            if (conditions.Count == 0)");
            sb.AppendLine($"                return Enumerable.Empty<{info.EntityType}>();");
            sb.AppendLine();
            sb.AppendLine($"            var whereClause = string.Join(\" OR \", conditions);");
            sb.AppendLine($"            var sql = $\"SELECT * FROM {tableName} WHERE {{whereClause}} ORDER BY {keyColumnName}\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, parameters);");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate FindBy{Property1}Or{Property2}Async with pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} or {otherRel.PropertyName}, with pagination support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {orMethodName}({relatedKeyType}? {paramName}, {otherKeyType}? {otherParamName}, int skip, int take)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var conditions = new List<string>();");
            sb.AppendLine($"            var parameters = new Dictionary<string, object> {{ {{ \"skip\", skip }}, {{ \"take\", take }} }};");
            sb.AppendLine();
            sb.AppendLine($"            if ({paramName}.HasValue)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                conditions.Add(\"{foreignKeyColumn} = @{paramName}\");");
            sb.AppendLine($"                parameters.Add(\"{paramName}\", {paramName}.Value);");
            sb.AppendLine($"            }}");
            sb.AppendLine();
            sb.AppendLine($"            if ({otherParamName}.HasValue)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                conditions.Add(\"{otherFkColumn} = @{otherParamName}\");");
            sb.AppendLine($"                parameters.Add(\"{otherParamName}\", {otherParamName}.Value);");
            sb.AppendLine($"            }}");
            sb.AppendLine();
            sb.AppendLine($"            if (conditions.Count == 0)");
            sb.AppendLine($"                return Enumerable.Empty<{info.EntityType}>();");
            sb.AppendLine();
            sb.AppendLine($"            var whereClause = string.Join(\" OR \", conditions);");
            sb.AppendLine($"            var sql = $\"SELECT * FROM {tableName} WHERE {{whereClause}} ORDER BY {keyColumnName} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, parameters);");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate FindBy{Property1}Or{Property2}Async with pagination and sorting
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} or {otherRel.PropertyName}, with pagination and sorting support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {orMethodName}({relatedKeyType}? {paramName}, {otherKeyType}? {otherParamName}, int skip, int take, string? orderBy = null, bool ascending = true)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var orderByColumn = GetColumnNameForProperty(orderBy, \"{keyColumnName}\");");
            sb.AppendLine($"            var direction = ascending ? \"ASC\" : \"DESC\";");
            sb.AppendLine($"            var conditions = new List<string>();");
            sb.AppendLine($"            var parameters = new Dictionary<string, object> {{ {{ \"skip\", skip }}, {{ \"take\", take }} }};");
            sb.AppendLine();
            sb.AppendLine($"            if ({paramName}.HasValue)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                conditions.Add(\"{foreignKeyColumn} = @{paramName}\");");
            sb.AppendLine($"                parameters.Add(\"{paramName}\", {paramName}.Value);");
            sb.AppendLine($"            }}");
            sb.AppendLine();
            sb.AppendLine($"            if ({otherParamName}.HasValue)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                conditions.Add(\"{otherFkColumn} = @{otherParamName}\");");
            sb.AppendLine($"                parameters.Add(\"{otherParamName}\", {otherParamName}.Value);");
            sb.AppendLine($"            }}");
            sb.AppendLine();
            sb.AppendLine($"            if (conditions.Count == 0)");
            sb.AppendLine($"                return Enumerable.Empty<{info.EntityType}>();");
            sb.AppendLine();
            sb.AppendLine($"            var whereClause = string.Join(\" OR \", conditions);");
            sb.AppendLine($"            var sql = $\"SELECT * FROM {tableName} WHERE {{whereClause}} ORDER BY {{orderByColumn}} {{direction}} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, parameters);");
            sb.AppendLine("        }");
            sb.AppendLine();
            }
        }

        // Generate AND combinations with entity properties: FindBy{Property}And{PropertyName}Async
        if (info.EntityMetadata?.Properties != null)
        {
            foreach (var property in info.EntityMetadata.Properties)
            {
                if (property.IsPrimaryKey)
                    continue;

                if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") ||
                    property.TypeName.Contains("IEnumerable") || !IsSimpleType(property.TypeName))
                    continue;

                var propertyParamName = ToCamelCase(property.Name);
                var andMethodName = $"FindBy{relationship.PropertyName}And{property.Name}Async";

                // Generate FindBy{Property}And{PropertyName}Async
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName} and {property.Name}.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        /// <param name=\"{paramName}\">The {relationship.PropertyName} identifier.</param>");
                sb.AppendLine($"        /// <param name=\"{propertyParamName}\">The {property.Name} value.</param>");
                sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {andMethodName}({relatedKeyType} {paramName}, {property.TypeName} {propertyParamName})");
                sb.AppendLine("        {");
                sb.AppendLine($"            var sql = @\"SELECT * FROM {tableName}");
                sb.AppendLine($"                WHERE {foreignKeyColumn} = @{paramName} AND {property.ColumnName} = @{propertyParamName}");
                sb.AppendLine($"                ORDER BY {keyColumnName}\";");
                sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {paramName}, {propertyParamName} }});");
                sb.AppendLine("        }");
                sb.AppendLine();

                // Generate FindBy{Property}And{PropertyName}Async with pagination
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} and {property.Name}, with pagination support.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {andMethodName}({relatedKeyType} {paramName}, {property.TypeName} {propertyParamName}, int skip, int take)");
                sb.AppendLine("        {");
                sb.AppendLine($"            var sql = @\"SELECT * FROM {tableName}");
                sb.AppendLine($"                WHERE {foreignKeyColumn} = @{paramName} AND {property.ColumnName} = @{propertyParamName}");
                sb.AppendLine($"                ORDER BY {keyColumnName} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
                sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {paramName}, {propertyParamName}, skip, take }});");
                sb.AppendLine("        }");
                sb.AppendLine();

                // Generate FindBy{Property}And{PropertyName}Async with pagination and sorting
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} and {property.Name}, with pagination and sorting support.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {andMethodName}({relatedKeyType} {paramName}, {property.TypeName} {propertyParamName}, int skip, int take, string? orderBy = null, bool ascending = true)");
                sb.AppendLine("        {");
                sb.AppendLine($"            var orderByColumn = GetColumnNameForProperty(orderBy, \"{keyColumnName}\");");
                sb.AppendLine($"            var direction = ascending ? \"ASC\" : \"DESC\";");
                sb.AppendLine($"            var sql = $\"SELECT * FROM {tableName}");
                sb.AppendLine($"                WHERE {foreignKeyColumn} = @{paramName} AND {property.ColumnName} = @{propertyParamName}");
                sb.AppendLine($"                ORDER BY {{orderByColumn}} {{direction}} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
                sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {paramName}, {propertyParamName}, skip, take }});");
                sb.AppendLine("        }");
                sb.AppendLine();
            }
        }
    }

    /// <summary>
    /// Checks if a type is a DateTime type.
    /// </summary>
    private static bool IsDateTimeType(string typeName)
    {
        var normalizedType = typeName.TrimEnd('?'); // Remove nullable marker
        return normalizedType == "DateTime" || normalizedType == "System.DateTime" || 
               normalizedType == "DateTimeOffset" || normalizedType == "System.DateTimeOffset";
    }

    /// <summary>
    /// Generates aggregate methods for OneToMany relationships.
    /// For example, GetTotalOrderAmountAsync, GetAverageOrderAmountAsync, etc.
    /// </summary>
    private static void GenerateAggregateMethods(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Get child entity metadata
        var childEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(childEntitySimpleName, out var childMetadata))
        {
            return; // Can't generate aggregate methods without metadata
        }

        if (childMetadata.Properties == null)
        {
            return;
        }

        var childTableName = GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? childEntitySimpleName;
        var parentEntityName = info.EntityType.Split('.').Last();
        
        // For OneToMany, the JoinColumn is on the inverse ManyToOne relationship
        // Find the FK column from the child entity's ManyToOne relationship
        var foreignKeyColumn = GetForeignKeyColumnForOneToMany(info, relationship, parentEntityName);

        // Generate aggregate methods for each numeric property of the child entity
        foreach (var property in childMetadata.Properties)
        {
            // Skip primary key, relationships, and non-numeric types
            if (property.IsPrimaryKey)
                continue;

            if (!IsNumericType(property.TypeName))
                continue;

            // Skip if property type is a collection or complex object
            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") || 
                property.TypeName.Contains("IEnumerable"))
                continue;

            var propertyName = property.Name;
            var propertyColumnName = property.ColumnName;
            var returnType = property.TypeName.TrimEnd('?'); // Remove nullable marker for return type

            // Generate SUM method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the sum of {relationship.PropertyName}.{propertyName} for the entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<{returnType}> GetTotal{relationship.PropertyName}{propertyName}Async({info.KeyType} id)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"SELECT COALESCE(SUM({propertyColumnName}), 0) FROM {childTableName} WHERE {foreignKeyColumn} = @id\";");
            sb.AppendLine($"            return await _connection.ExecuteScalarAsync<{returnType}>(sql, new {{ id }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate AVG method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the average of {relationship.PropertyName}.{propertyName} for the entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<{returnType}?> GetAverage{relationship.PropertyName}{propertyName}Async({info.KeyType} id)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"SELECT AVG({propertyColumnName}) FROM {childTableName} WHERE {foreignKeyColumn} = @id\";");
            sb.AppendLine($"            return await _connection.ExecuteScalarAsync<{returnType}?>(sql, new {{ id }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate MIN method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the minimum {relationship.PropertyName}.{propertyName} for the entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<{returnType}?> GetMin{relationship.PropertyName}{propertyName}Async({info.KeyType} id)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"SELECT MIN({propertyColumnName}) FROM {childTableName} WHERE {foreignKeyColumn} = @id\";");
            sb.AppendLine($"            return await _connection.ExecuteScalarAsync<{returnType}?>(sql, new {{ id }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate MAX method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the maximum {relationship.PropertyName}.{propertyName} for the entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<{returnType}?> GetMax{relationship.PropertyName}{propertyName}Async({info.KeyType} id)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"SELECT MAX({propertyColumnName}) FROM {childTableName} WHERE {foreignKeyColumn} = @id\";");
            sb.AppendLine($"            return await _connection.ExecuteScalarAsync<{returnType}?>(sql, new {{ id }});");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates GROUP BY aggregate methods for OneToMany relationships.
    /// For example, GetOrderCountsByCustomerAsync, GetTotalOrderAmountsByCustomerAsync, etc.
    /// These methods return Dictionary&lt;ParentKeyType, AggregateType&gt; grouped by parent entity.
    /// </summary>
    private static void GenerateGroupByAggregateMethods(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Get child entity metadata
        var childEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(childEntitySimpleName, out var childMetadata))
        {
            return; // Can't generate GROUP BY methods without metadata
        }

        if (childMetadata.Properties == null)
        {
            return;
        }

        var childTableName = GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? childEntitySimpleName;
        var parentEntityName = info.EntityType.Split('.').Last();
        
        // For OneToMany, the JoinColumn is on the inverse ManyToOne relationship
        var foreignKeyColumn = GetForeignKeyColumnForOneToMany(info, relationship, parentEntityName);
        
        // Get parent entity key type and column name
        var parentKeyType = info.KeyType;
        var parentKeyColumnName = GetKeyColumnName(info, info.EntityType);

        // Generate COUNT method (always available, doesn't require numeric property)
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets the count of {relationship.PropertyName} grouped by parent entity.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<Dictionary<{parentKeyType}, int>> Get{relationship.PropertyName}CountsBy{parentEntityName}Async()");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = $\"SELECT {foreignKeyColumn} AS Key, COUNT(*) AS Value FROM {childTableName} GROUP BY {foreignKeyColumn}\";");
        sb.AppendLine($"            var results = await _connection.QueryAsync<({parentKeyType} Key, int Value)>(sql);");
        sb.AppendLine("            return results.ToDictionary(r => r.Key, r => r.Value);");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate aggregate methods for each numeric property of the child entity
        foreach (var property in childMetadata.Properties)
        {
            // Skip primary key, relationships, and non-numeric types
            if (property.IsPrimaryKey)
                continue;

            if (!IsNumericType(property.TypeName))
                continue;

            // Skip if property type is a collection or complex object
            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") || 
                property.TypeName.Contains("IEnumerable"))
                continue;

            var propertyName = property.Name;
            var propertyColumnName = property.ColumnName;
            var returnType = property.TypeName.TrimEnd('?'); // Remove nullable marker for return type

            // Generate SUM GROUP BY method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the sum of {relationship.PropertyName}.{propertyName} grouped by parent entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<Dictionary<{parentKeyType}, {returnType}>> GetTotal{relationship.PropertyName}{propertyName}By{parentEntityName}Async()");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"SELECT {foreignKeyColumn} AS Key, COALESCE(SUM({propertyColumnName}), 0) AS Value FROM {childTableName} GROUP BY {foreignKeyColumn}\";");
            sb.AppendLine($"            var results = await _connection.QueryAsync<({parentKeyType} Key, {returnType} Value)>(sql);");
            sb.AppendLine("            return results.ToDictionary(r => r.Key, r => r.Value);");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate AVG GROUP BY method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the average of {relationship.PropertyName}.{propertyName} grouped by parent entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<Dictionary<{parentKeyType}, {returnType}?>> GetAverage{relationship.PropertyName}{propertyName}By{parentEntityName}Async()");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"SELECT {foreignKeyColumn} AS Key, AVG({propertyColumnName}) AS Value FROM {childTableName} GROUP BY {foreignKeyColumn}\";");
            sb.AppendLine($"            var results = await _connection.QueryAsync<({parentKeyType} Key, {returnType}? Value)>(sql);");
            sb.AppendLine("            return results.ToDictionary(r => r.Key, r => r.Value);");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate MIN GROUP BY method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the minimum {relationship.PropertyName}.{propertyName} grouped by parent entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<Dictionary<{parentKeyType}, {returnType}?>> GetMin{relationship.PropertyName}{propertyName}By{parentEntityName}Async()");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"SELECT {foreignKeyColumn} AS Key, MIN({propertyColumnName}) AS Value FROM {childTableName} GROUP BY {foreignKeyColumn}\";");
            sb.AppendLine($"            var results = await _connection.QueryAsync<({parentKeyType} Key, {returnType}? Value)>(sql);");
            sb.AppendLine("            return results.ToDictionary(r => r.Key, r => r.Value);");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate MAX GROUP BY method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the maximum {relationship.PropertyName}.{propertyName} grouped by parent entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<Dictionary<{parentKeyType}, {returnType}?>> GetMax{relationship.PropertyName}{propertyName}By{parentEntityName}Async()");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"SELECT {foreignKeyColumn} AS Key, MAX({propertyColumnName}) AS Value FROM {childTableName} GROUP BY {foreignKeyColumn}\";");
            sb.AppendLine($"            var results = await _connection.QueryAsync<({parentKeyType} Key, {returnType}? Value)>(sql);");
            sb.AppendLine("            return results.ToDictionary(r => r.Key, r => r.Value);");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Checks if a type is a simple type that can be used in WHERE clauses.
    /// </summary>
    private static bool IsSimpleType(string typeName)
    {
        var simpleTypes = new[] { "string", "int", "long", "decimal", "double", "float", "bool", "DateTime", "Guid", "byte", "short", "char" };
        var normalizedType = typeName.TrimEnd('?'); // Remove nullable marker
        return simpleTypes.Contains(normalizedType) || normalizedType.StartsWith("System.");
    }

    /// <summary>
    /// Checks if a type is numeric and can be used in aggregate functions.
    /// </summary>
    private static bool IsNumericType(string typeName)
    {
        var numericTypes = new[] { "int", "long", "decimal", "double", "float", "byte", "short", "System.Int32", "System.Int64", "System.Decimal", "System.Double", "System.Single", "System.Byte", "System.Int16" };
        var normalizedType = typeName.TrimEnd('?'); // Remove nullable marker
        return numericTypes.Contains(normalizedType) || normalizedType.StartsWith("System.Int") || normalizedType.StartsWith("System.Decimal") || normalizedType.StartsWith("System.Double") || normalizedType.StartsWith("System.Single");
    }

    // Generate separate interface for relationship query methods
    private static string GeneratePartialInterface(RepositoryInfo info)
    {
        var sb = new StringBuilder();

        // Create interface name with Partial suffix (e.g., IOrderRepositoryPartial)
        var partialInterfaceName = info.InterfaceName + "Partial";

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// This code was generated by NPA.Generators.RepositoryGenerator");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
        sb.AppendLine($"namespace {info.Namespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Extended interface for {info.InterfaceName} with relationship query methods.");
        sb.AppendLine($"    /// This interface is automatically implemented by the generated repository.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public interface {partialInterfaceName}");
        sb.AppendLine("    {");

        var entityName = info.EntityType.Split('.').Last();

        foreach (var relationship in info.Relationships)
        {
            // Generate GetByIdWith{Property}Async signature
            GenerateGetByIdWithPropertySignature(sb, info, relationship, entityName);

            // Generate Load{Property}Async signature for lazy loading
            if (relationship.FetchType == Models.FetchType.Lazy)
            {
                GenerateLoadPropertySignature(sb, info, relationship, entityName);
            }

            // Generate FindBy method signatures for ManyToOne relationships
            if (relationship.Type == Models.RelationshipType.ManyToOne)
            {
                GenerateFindByParentSignature(sb, info, relationship);
                GenerateCountByParentSignature(sb, info, relationship);
                GeneratePropertyBasedQuerySignatures(sb, info, relationship);
                GenerateAdvancedFilterSignatures(sb, info, relationship);
                GenerateComplexFilterSignatures(sb, info, relationship);
            }

            // Generate Has/Count method signatures for OneToMany relationships
            if (relationship.Type == Models.RelationshipType.OneToMany && !string.IsNullOrEmpty(relationship.MappedBy))
            {
                GenerateHasChildrenSignature(sb, info, relationship);
                GenerateCountChildrenSignature(sb, info, relationship);
                GenerateAggregateMethodSignatures(sb, info, relationship);
                GenerateGroupByAggregateMethodSignatures(sb, info, relationship);
                GenerateSubqueryFilterSignatures(sb, info, relationship);
                GenerateInverseRelationshipQuerySignatures(sb, info, relationship);
            }
        }

        // Generate multi-level navigation query signatures
        GenerateMultiLevelNavigationQuerySignatures(sb, info);

        // Generate cascade operation method signatures if applicable
        var persistCascades = info.CascadeRelationships.Where(r => (r.CascadeTypes & Models.CascadeType.Persist) != 0).ToList();
        if (persistCascades.Any())
        {
            GenerateAddWithCascadeSignature(sb, info, persistCascades, entityName);
        }

        var mergeCascades = info.CascadeRelationships.Where(r => (r.CascadeTypes & Models.CascadeType.Merge) != 0).ToList();
        if (mergeCascades.Any())
        {
            GenerateUpdateWithCascadeSignature(sb, info, mergeCascades, entityName);
        }

        var removeCascades = info.CascadeRelationships.Where(r => (r.CascadeTypes & Models.CascadeType.Remove) != 0).ToList();
        if (removeCascades.Any())
        {
            GenerateDeleteWithCascadeSignature(sb, info, removeCascades, entityName);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateGetByIdWithPropertySignature(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string entityName)
    {
        var propertyName = relationship.PropertyName;
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets a {entityName} by its ID with {propertyName} loaded asynchronously.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <param name=\"id\">The {entityName} identifier.</param>");
        sb.AppendLine($"        /// <returns>The {entityName} with {propertyName} loaded if found; otherwise, null.</returns>");
        sb.AppendLine($"        Task<{info.EntityType}?> GetByIdWith{propertyName}Async({info.KeyType} id);");
        sb.AppendLine();
    }

    private static void GenerateLoadPropertySignature(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string entityName)
    {
        var propertyName = relationship.PropertyName;
        var relatedTypeName = relationship.TargetEntityType;
        var relatedTypeFullName = relationship.TargetEntityFullType;

        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Loads {propertyName} for an existing {entityName} entity asynchronously.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <param name=\"entity\">The {entityName} entity.</param>");

        if (relationship.IsCollection)
        {
            sb.AppendLine($"        /// <returns>A collection of {relatedTypeName} entities.</returns>");
            sb.AppendLine($"        Task<IEnumerable<{relatedTypeFullName}>> Load{propertyName}Async({info.EntityType} entity);");
        }
        else
        {
            sb.AppendLine($"        /// <returns>The loaded {relatedTypeName} entity if found; otherwise, null.</returns>");
            var returnType = relatedTypeFullName.TrimEnd('?');
            sb.AppendLine($"        Task<{returnType}?> Load{propertyName}Async({info.EntityType} entity);");
        }
        sb.AppendLine();
    }

    private static void GenerateFindByParentSignature(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        var targetEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        var paramName = ToCamelCase(targetEntitySimpleName) + "Id";
        var relatedKeyType = GetRelatedEntityKeyType(info, relationship.TargetEntityType);

        // Signature without pagination
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}IdAsync({relatedKeyType} {paramName});");
        sb.AppendLine();

        // Signature with pagination
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} with pagination support.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}IdAsync({relatedKeyType} {paramName}, int skip, int take);");
        sb.AppendLine();

        // Signature with pagination and sorting
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} with pagination and sorting support.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}IdAsync({relatedKeyType} {paramName}, int skip, int take, string? orderBy = null, bool ascending = true);");
        sb.AppendLine();
    }

    private static void GenerateCountByParentSignature(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        var targetEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        var paramName = ToCamelCase(targetEntitySimpleName) + "Id";
        var relatedKeyType = GetRelatedEntityKeyType(info, relationship.TargetEntityType);

        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Counts {info.EntityType} entities by {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<int> CountBy{relationship.PropertyName}IdAsync({relatedKeyType} {paramName});");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates property-based query method signatures for the interface.
    /// </summary>
    private static void GeneratePropertyBasedQuerySignatures(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Get related entity metadata
        var relatedEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(relatedEntitySimpleName, out var relatedMetadata))
        {
            return; // Can't generate signatures without metadata
        }

        if (relatedMetadata.Properties == null)
        {
            return;
        }

        // Generate signatures for each property of the related entity
        foreach (var property in relatedMetadata.Properties)
        {
            // Skip primary key, relationships, and complex types
            if (property.IsPrimaryKey)
                continue;

            // Skip if property type is a collection or complex object
            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") || 
                property.TypeName.Contains("IEnumerable") || !IsSimpleType(property.TypeName))
                continue;

            var propertyParamName = ToCamelCase(property.Name);
            var methodName = $"FindBy{relationship.PropertyName}{property.Name}Async";

            // Signature without pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName}.{property.Name}.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName});");
            sb.AppendLine();

            // Signature with pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName}.{property.Name} with pagination support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName}, int skip, int take);");
            sb.AppendLine();

            // Signature with pagination and sorting
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName}.{property.Name} with pagination and sorting support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName}, int skip, int take, string? orderBy = null, bool ascending = true);");
            sb.AppendLine();
        }
    }

    private static void GenerateHasChildrenSignature(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Checks if the entity has any {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<bool> Has{relationship.PropertyName}Async({info.KeyType} id);");
        sb.AppendLine();
    }

    private static void GenerateCountChildrenSignature(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Counts the number of {relationship.PropertyName} for the entity.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<int> Count{relationship.PropertyName}Async({info.KeyType} id);");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates aggregate method signatures for the interface.
    /// </summary>
    private static void GenerateAggregateMethodSignatures(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Get child entity metadata
        var childEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(childEntitySimpleName, out var childMetadata))
        {
            return; // Can't generate signatures without metadata
        }

        if (childMetadata.Properties == null)
        {
            return;
        }

        // Generate signatures for each numeric property of the child entity
        foreach (var property in childMetadata.Properties)
        {
            // Skip primary key, relationships, and non-numeric types
            if (property.IsPrimaryKey)
                continue;

            if (!IsNumericType(property.TypeName))
                continue;

            // Skip if property type is a collection or complex object
            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") || 
                property.TypeName.Contains("IEnumerable"))
                continue;

            var propertyName = property.Name;
            var returnType = property.TypeName.TrimEnd('?'); // Remove nullable marker for return type

            // Generate SUM signature
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the sum of {relationship.PropertyName}.{propertyName} for the entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<{returnType}> GetTotal{relationship.PropertyName}{propertyName}Async({info.KeyType} id);");
            sb.AppendLine();

            // Generate AVG signature
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the average of {relationship.PropertyName}.{propertyName} for the entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<{returnType}?> GetAverage{relationship.PropertyName}{propertyName}Async({info.KeyType} id);");
            sb.AppendLine();

            // Generate MIN signature
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the minimum {relationship.PropertyName}.{propertyName} for the entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<{returnType}?> GetMin{relationship.PropertyName}{propertyName}Async({info.KeyType} id);");
            sb.AppendLine();

            // Generate MAX signature
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the maximum {relationship.PropertyName}.{propertyName} for the entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<{returnType}?> GetMax{relationship.PropertyName}{propertyName}Async({info.KeyType} id);");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates GROUP BY aggregate method signatures for the interface.
    /// </summary>
    private static void GenerateGroupByAggregateMethodSignatures(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Get child entity metadata
        var childEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(childEntitySimpleName, out var childMetadata))
        {
            return; // Can't generate signatures without metadata
        }

        if (childMetadata.Properties == null)
        {
            return;
        }

        var parentEntityName = info.EntityType.Split('.').Last();
        var parentKeyType = info.KeyType;

        // Generate COUNT signature (always available)
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets the count of {relationship.PropertyName} grouped by parent entity.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<Dictionary<{parentKeyType}, int>> Get{relationship.PropertyName}CountsBy{parentEntityName}Async();");
        sb.AppendLine();

        // Generate signatures for each numeric property of the child entity
        foreach (var property in childMetadata.Properties)
        {
            // Skip primary key, relationships, and non-numeric types
            if (property.IsPrimaryKey)
                continue;

            if (!IsNumericType(property.TypeName))
                continue;

            // Skip if property type is a collection or complex object
            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") || 
                property.TypeName.Contains("IEnumerable"))
                continue;

            var propertyName = property.Name;
            var returnType = property.TypeName.TrimEnd('?'); // Remove nullable marker for return type

            // Generate SUM GROUP BY signature
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the sum of {relationship.PropertyName}.{propertyName} grouped by parent entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<Dictionary<{parentKeyType}, {returnType}>> GetTotal{relationship.PropertyName}{propertyName}By{parentEntityName}Async();");
            sb.AppendLine();

            // Generate AVG GROUP BY signature
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the average of {relationship.PropertyName}.{propertyName} grouped by parent entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<Dictionary<{parentKeyType}, {returnType}?>> GetAverage{relationship.PropertyName}{propertyName}By{parentEntityName}Async();");
            sb.AppendLine();

            // Generate MIN GROUP BY signature
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the minimum {relationship.PropertyName}.{propertyName} grouped by parent entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<Dictionary<{parentKeyType}, {returnType}?>> GetMin{relationship.PropertyName}{propertyName}By{parentEntityName}Async();");
            sb.AppendLine();

            // Generate MAX GROUP BY signature
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the maximum {relationship.PropertyName}.{propertyName} grouped by parent entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<Dictionary<{parentKeyType}, {returnType}?>> GetMax{relationship.PropertyName}{propertyName}By{parentEntityName}Async();");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates advanced filter method signatures for the interface.
    /// </summary>
    private static void GenerateAdvancedFilterSignatures(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Get current entity metadata for date/amount filters
        if (info.EntityMetadata?.Properties == null)
        {
            return;
        }

        var relatedKeyType = GetRelatedEntityKeyType(info, relationship.TargetEntityType);
        var targetEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        var relatedKeyParamName = ToCamelCase(targetEntitySimpleName) + "Id";

        // Generate date range filter signatures for DateTime properties
        foreach (var property in info.EntityMetadata.Properties)
        {
            if (property.IsPrimaryKey)
                continue;

            if (!IsDateTimeType(property.TypeName))
                continue;

            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") ||
                property.TypeName.Contains("IEnumerable"))
                continue;

            // Signature without pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName} and {property.Name} date range.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}And{property.Name}RangeAsync({relatedKeyType} {relatedKeyParamName}, DateTime start{property.Name}, DateTime end{property.Name});");
            sb.AppendLine();

            // Signature with pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} and {property.Name} date range with pagination support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}And{property.Name}RangeAsync({relatedKeyType} {relatedKeyParamName}, DateTime start{property.Name}, DateTime end{property.Name}, int skip, int take);");
            sb.AppendLine();

            // Signature with pagination and sorting
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} and {property.Name} date range with pagination and sorting support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}And{property.Name}RangeAsync({relatedKeyType} {relatedKeyParamName}, DateTime start{property.Name}, DateTime end{property.Name}, int skip, int take, string? orderBy = null, bool ascending = true);");
            sb.AppendLine();
        }

        // Generate amount/quantity filter signatures for numeric properties
        foreach (var property in info.EntityMetadata.Properties)
        {
            if (property.IsPrimaryKey)
                continue;

            if (!IsNumericType(property.TypeName))
                continue;

            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") ||
                property.TypeName.Contains("IEnumerable"))
                continue;

            var returnType = property.TypeName.TrimEnd('?');

            // Signature without pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName} with {property.Name} greater than or equal to the specified value.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> Find{relationship.PropertyName}{property.Name}AboveAsync({relatedKeyType} {relatedKeyParamName}, {returnType} min{property.Name});");
            sb.AppendLine();

            // Signature with pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} with {property.Name} greater than or equal to the specified value, with pagination support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> Find{relationship.PropertyName}{property.Name}AboveAsync({relatedKeyType} {relatedKeyParamName}, {returnType} min{property.Name}, int skip, int take);");
            sb.AppendLine();

            // Signature with pagination and sorting
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} with {property.Name} greater than or equal to the specified value, with pagination and sorting support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> Find{relationship.PropertyName}{property.Name}AboveAsync({relatedKeyType} {relatedKeyParamName}, {returnType} min{property.Name}, int skip, int take, string? orderBy = null, bool ascending = true);");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates complex filter query method signatures for the interface.
    /// </summary>
    private static void GenerateComplexFilterSignatures(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        var targetEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        var relatedKeyType = GetRelatedEntityKeyType(info, relationship.TargetEntityType);
        var paramName = ToCamelCase(targetEntitySimpleName) + "Id";

        // Generate OR combination signatures (requires at least 2 relationships)
        if (info.Relationships.Count >= 2)
        {
            foreach (var otherRel in info.Relationships)
            {
                if (otherRel == relationship || otherRel.Type != Models.RelationshipType.ManyToOne)
                    continue;

                var otherEntitySimpleName = otherRel.TargetEntityType.Split('.').Last();
                var otherKeyType = GetRelatedEntityKeyType(info, otherRel.TargetEntityType);
                var otherParamName = ToCamelCase(otherEntitySimpleName) + "Id";
                var orMethodName = $"FindBy{relationship.PropertyName}Or{otherRel.PropertyName}Async";

                // Signature without pagination
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName} or {otherRel.PropertyName}.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {orMethodName}({relatedKeyType}? {paramName}, {otherKeyType}? {otherParamName});");
                sb.AppendLine();

                // Signature with pagination
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} or {otherRel.PropertyName}, with pagination support.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {orMethodName}({relatedKeyType}? {paramName}, {otherKeyType}? {otherParamName}, int skip, int take);");
                sb.AppendLine();

                // Signature with pagination and sorting
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} or {otherRel.PropertyName}, with pagination and sorting support.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {orMethodName}({relatedKeyType}? {paramName}, {otherKeyType}? {otherParamName}, int skip, int take, string? orderBy = null, bool ascending = true);");
                sb.AppendLine();
            }
        }

        // Generate AND combination signatures with entity properties
        if (info.EntityMetadata?.Properties != null)
        {
            foreach (var property in info.EntityMetadata.Properties)
            {
                if (property.IsPrimaryKey)
                    continue;

                if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") ||
                    property.TypeName.Contains("IEnumerable") || !IsSimpleType(property.TypeName))
                    continue;

                var propertyParamName = ToCamelCase(property.Name);
                var andMethodName = $"FindBy{relationship.PropertyName}And{property.Name}Async";

                // Signature without pagination
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName} and {property.Name}.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {andMethodName}({relatedKeyType} {paramName}, {property.TypeName} {propertyParamName});");
                sb.AppendLine();

                // Signature with pagination
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} and {property.Name}, with pagination support.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {andMethodName}({relatedKeyType} {paramName}, {property.TypeName} {propertyParamName}, int skip, int take);");
                sb.AppendLine();

                // Signature with pagination and sorting
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} and {property.Name}, with pagination and sorting support.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {andMethodName}({relatedKeyType} {paramName}, {property.TypeName} {propertyParamName}, int skip, int take, string? orderBy = null, bool ascending = true);");
                sb.AppendLine();
            }
        }
    }

    /// <summary>
    /// Generates subquery filter method signatures for the interface.
    /// </summary>
    private static void GenerateSubqueryFilterSignatures(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Signature without pagination
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities that have at least the specified number of {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindWithMinimum{relationship.PropertyName}Async(int minCount);");
        sb.AppendLine();

        // Signature with pagination
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds {info.EntityType} entities that have at least the specified number of {relationship.PropertyName}, with pagination support.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindWithMinimum{relationship.PropertyName}Async(int minCount, int skip, int take);");
        sb.AppendLine();

        // Signature with pagination and sorting
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds {info.EntityType} entities that have at least the specified number of {relationship.PropertyName}, with pagination and sorting support.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindWithMinimum{relationship.PropertyName}Async(int minCount, int skip, int take, string? orderBy = null, bool ascending = true);");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates inverse relationship query method signatures for the interface.
    /// </summary>
    private static void GenerateInverseRelationshipQuerySignatures(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Signature for FindWith{Property}Async
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities that have at least one {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindWith{relationship.PropertyName}Async();");
        sb.AppendLine();

        // Signature for FindWithout{Property}Async
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities that have no {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindWithout{relationship.PropertyName}Async();");
        sb.AppendLine();

        // Signature for FindWith{Property}CountAsync
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities that have at least the specified number of {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindWith{relationship.PropertyName}CountAsync(int minCount);");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates multi-level navigation query method signatures for the interface.
    /// </summary>
    private static void GenerateMultiLevelNavigationQuerySignatures(StringBuilder sb, RepositoryInfo info)
    {
        if (info.EntitiesMetadata == null || info.EntitiesMetadata.Count == 0)
            return;

        // Generate signatures for 2-level navigation paths
        foreach (var firstLevelRel in info.Relationships)
        {
            if (firstLevelRel.Type != Models.RelationshipType.ManyToOne)
                continue;

            var intermediateEntitySimpleName = firstLevelRel.TargetEntityType.Split('.').Last();

            // Check all entities in metadata as potential targets
            foreach (var entityMetadataEntry in info.EntitiesMetadata)
            {
                var targetEntitySimpleName = entityMetadataEntry.Key;
                var targetMetadata = entityMetadataEntry.Value;
                
                if (targetEntitySimpleName == intermediateEntitySimpleName)
                    continue;
                
                if (targetMetadata.Properties == null)
                    continue;

                // Verify that the intermediate entity actually has a relationship to the target entity
                // This ensures we only generate signatures for valid navigation paths
                Models.RelationshipMetadata? secondLevelRel = null;
                if (info.Compilation != null)
                {
                    var intermediateEntityFullType = firstLevelRel.TargetEntityFullType;
                    var intermediateEntitySimpleNameForExtraction = intermediateEntitySimpleName;
                    
                    var intermediateRelationships = ExtractRelationships(info.Compilation, intermediateEntityFullType);
                    if (intermediateRelationships.Count == 0)
                    {
                        intermediateRelationships = ExtractRelationships(info.Compilation, intermediateEntitySimpleNameForExtraction);
                    }
                    
                    secondLevelRel = intermediateRelationships.FirstOrDefault(r => 
                        r.Type == Models.RelationshipType.ManyToOne && 
                        r.TargetEntityType.Split('.').Last() == targetEntitySimpleName);
                }
                
                // Only generate signatures if the relationship actually exists
                if (secondLevelRel == null)
                    continue;

                foreach (var property in targetMetadata.Properties)
                {
                    if (property.IsPrimaryKey)
                        continue;

                    if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") ||
                        property.TypeName.Contains("IEnumerable") || !IsSimpleType(property.TypeName))
                        continue;

                    var methodName = $"FindBy{firstLevelRel.PropertyName}{targetEntitySimpleName}{property.Name}Async";
                    var propertyParamName = ToCamelCase(property.Name);

                    // Signature without pagination
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine($"        /// Finds all {info.EntityType} entities by navigating through {firstLevelRel.PropertyName} → {targetEntitySimpleName} to {targetEntitySimpleName}.{property.Name}.");
                    sb.AppendLine("        /// </summary>");
                    sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName});");
                    sb.AppendLine();

                    // Signature with pagination
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine($"        /// Finds {info.EntityType} entities by navigating through {firstLevelRel.PropertyName} → {targetEntitySimpleName} to {targetEntitySimpleName}.{property.Name}, with pagination support.");
                    sb.AppendLine("        /// </summary>");
                    sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName}, int skip, int take);");
                    sb.AppendLine();

                    // Signature with pagination and sorting
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine($"        /// Finds {info.EntityType} entities by navigating through {firstLevelRel.PropertyName} → {targetEntitySimpleName} to {targetEntitySimpleName}.{property.Name}, with pagination and sorting support.");
                    sb.AppendLine("        /// </summary>");
                    sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName}, int skip, int take, string? orderBy = null, bool ascending = true);");
                    sb.AppendLine();
                }
            }
        }
    }

    private static void GenerateAddWithCascadeSignature(StringBuilder sb, RepositoryInfo info, List<Models.RelationshipMetadata> cascades, string entityName)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Adds a {entityName} with cascade persist support.");
        sb.AppendLine($"        /// Automatically persists related entities marked with CascadeType.Persist.");
        sb.AppendLine($"        /// Cascades: {string.Join(", ", cascades.Select(c => c.PropertyName))}");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<{info.EntityType}> AddWithCascadeAsync({info.EntityType} entity);");
        sb.AppendLine();
    }

    private static void GenerateUpdateWithCascadeSignature(StringBuilder sb, RepositoryInfo info, List<Models.RelationshipMetadata> cascades, string entityName)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Updates a {entityName} with cascade merge support.");
        sb.AppendLine($"        /// Automatically updates related entities marked with CascadeType.Merge.");
        sb.AppendLine($"        /// Cascades: {string.Join(", ", cascades.Select(c => c.PropertyName))}");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task UpdateWithCascadeAsync({info.EntityType} entity);");
        sb.AppendLine();
    }

    private static void GenerateDeleteWithCascadeSignature(StringBuilder sb, RepositoryInfo info, List<Models.RelationshipMetadata> cascades, string entityName)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Deletes a {entityName} with cascade remove support.");
        sb.AppendLine($"        /// Automatically deletes related entities marked with CascadeType.Remove.");
        sb.AppendLine($"        /// Cascades: {string.Join(", ", cascades.Select(c => c.PropertyName))}");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task DeleteWithCascadeAsync({info.KeyType} id);");
        sb.AppendLine();
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // If already PascalCase (starts with upper) and no underscores, just return
        if (char.IsUpper(input[0]) && !input.Contains("_")) return input;

        var parts = input.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length > 0)
            {
                sb.Append(char.ToUpper(part[0]));
                if (part.Length > 1)
                    sb.Append(part.Substring(1));
            }
        }
        return sb.ToString();
    }

    private static string GetPropertyNameForColumn(RepositoryInfo info, string columnName, string? entityTypeName = null)
    {
        EntityMetadataInfo? metadata = info.EntityMetadata;

        if (!string.IsNullOrEmpty(entityTypeName))
        {
            var simpleName = entityTypeName!.Split('.').Last();
            if (info.EntitiesMetadata != null && info.EntitiesMetadata.TryGetValue(simpleName, out var m))
            {
                metadata = m;
            }
        }

        if (metadata?.Properties != null)
        {
            var prop = metadata.Properties.FirstOrDefault(p =>
                string.Equals(p.ColumnName, columnName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));

            if (prop != null) return prop.Name;
        }

        return ToPascalCase(columnName);
    }

    /// <summary>
    /// Checks if a property exists on an entity by column name or property name.
    /// </summary>
    private static bool HasProperty(RepositoryInfo info, string columnOrPropertyName, string? entityTypeName = null)
    {
        EntityMetadataInfo? metadata = info.EntityMetadata;

        if (!string.IsNullOrEmpty(entityTypeName))
        {
            var simpleName = entityTypeName!.Split('.').Last();
            if (info.EntitiesMetadata != null && info.EntitiesMetadata.TryGetValue(simpleName, out var m))
            {
                metadata = m;
            }
        }

        if (metadata?.Properties != null)
        {
            return metadata.Properties.Any(p =>
                string.Equals(p.ColumnName, columnOrPropertyName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Name, columnOrPropertyName, StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    private static bool IsPropertyNullable(RepositoryInfo info, string columnName, string? entityTypeName = null)
    {
        EntityMetadataInfo? metadata = info.EntityMetadata;

        if (!string.IsNullOrEmpty(entityTypeName))
        {
            var simpleName = entityTypeName!.Split('.').Last();
            if (info.EntitiesMetadata != null && info.EntitiesMetadata.TryGetValue(simpleName, out var m))
            {
                metadata = m;
            }
        }

        if (metadata?.Properties != null)
        {
            var prop = metadata.Properties.FirstOrDefault(p =>
                string.Equals(p.ColumnName, columnName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));

            if (prop != null) return prop.IsNullable;
        }

        // Default to non-nullable if we can't determine
        return false;
    }

    private static string GetRelatedEntityKeyType(RepositoryInfo info, string relatedEntityTypeName)
    {
        // Try to get from EntitiesMetadata first
        var simpleName = relatedEntityTypeName.Split('.').Last();
        if (info.EntitiesMetadata != null && info.EntitiesMetadata.TryGetValue(simpleName, out var metadata))
        {
            // Find the primary key property
            var keyProperty = metadata.Properties?.FirstOrDefault(p => p.IsPrimaryKey);
            if (keyProperty != null)
            {
                return keyProperty.TypeName;
            }
        }

        // Fallback: try to extract from compilation
        // This is a best-effort approach - if we can't determine, use the current entity's key type
        // In practice, most entities use the same key type, so this is a reasonable fallback
        return info.KeyType;
    }

    /// <summary>
    /// Gets the type of a foreign key property by column name.
    /// </summary>
    private static string? GetForeignKeyPropertyType(RepositoryInfo info, string columnName, string? entityTypeName = null)
    {
        EntityMetadataInfo? metadata = info.EntityMetadata;

        if (!string.IsNullOrEmpty(entityTypeName))
        {
            var simpleName = entityTypeName!.Split('.').Last();
            if (info.EntitiesMetadata != null && info.EntitiesMetadata.TryGetValue(simpleName, out var m))
            {
                metadata = m;
            }
        }

        if (metadata?.Properties != null)
        {
            var prop = metadata.Properties.FirstOrDefault(p =>
                string.Equals(p.ColumnName, columnName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));

            if (prop != null) return prop.TypeName;
        }

        return null;
    }

    /// <summary>
    /// Gets the primary key property name for an entity. Returns "Id" if not found.
    /// </summary>
    private static string GetKeyPropertyName(RepositoryInfo info, string? entityTypeName = null)
    {
        EntityMetadataInfo? metadata = info.EntityMetadata;

        if (!string.IsNullOrEmpty(entityTypeName))
        {
            var simpleName = entityTypeName!.Split('.').Last();
            if (info.EntitiesMetadata != null && info.EntitiesMetadata.TryGetValue(simpleName, out var m))
            {
                metadata = m;
            }
        }

        if (metadata?.Properties != null)
        {
            var keyProperty = metadata.Properties.FirstOrDefault(p => p.IsPrimaryKey);
            if (keyProperty != null)
            {
                return keyProperty.Name;
            }
        }

        // Default to "Id" if we can't determine
        return "Id";
    }

    /// <summary>
    /// Gets the primary key column name for an entity. Returns "Id" if not found.
    /// </summary>
    private static string GetKeyColumnName(RepositoryInfo info, string? entityTypeName = null)
    {
        EntityMetadataInfo? metadata = info.EntityMetadata;

        if (!string.IsNullOrEmpty(entityTypeName))
        {
            var simpleName = entityTypeName!.Split('.').Last();
            if (info.EntitiesMetadata != null && info.EntitiesMetadata.TryGetValue(simpleName, out var m))
            {
                metadata = m;
            }
        }

        if (metadata?.Properties != null)
        {
            var keyProperty = metadata.Properties.FirstOrDefault(p => p.IsPrimaryKey);
            if (keyProperty != null)
            {
                return keyProperty.ColumnName;
            }
        }

        // Default to "Id" if we can't determine
        return "Id";
    }

    /// <summary>
    /// Gets the foreign key column name for a OneToMany relationship.
    /// The JoinColumn is defined on the inverse ManyToOne relationship, not on the OneToMany.
    /// </summary>
    private static string GetForeignKeyColumnForOneToMany(RepositoryInfo info, Models.RelationshipMetadata oneToManyRelationship, string parentEntityName)
    {
        // If JoinColumn is specified on the OneToMany (shouldn't normally happen, but check first)
        if (oneToManyRelationship.JoinColumn != null && !string.IsNullOrEmpty(oneToManyRelationship.JoinColumn.Name))
        {
            return oneToManyRelationship.JoinColumn.Name;
        }

        // The JoinColumn is on the child entity's ManyToOne relationship
        // We need to find the child entity's ManyToOne relationship that points back to the parent
        // The MappedBy property tells us the property name on the child entity
        if (string.IsNullOrEmpty(oneToManyRelationship.MappedBy))
        {
            // No MappedBy means we can't determine the inverse relationship
            // Fall back to default naming convention
            return $"{parentEntityName}Id";
        }

        // Extract relationships from child entity to get the JoinColumn from ManyToOne
        if (info.Compilation != null)
        {
            // Try both full type and simple type name
            var childEntityFullType = oneToManyRelationship.TargetEntityFullType;
            var childEntitySimpleNameForExtraction = oneToManyRelationship.TargetEntityType.Split('.').Last();
            
            // Try full type first, then simple name
            var childRelationships = ExtractRelationships(info.Compilation, childEntityFullType);
            if (childRelationships.Count == 0)
            {
                // Try with just the simple name (might need namespace)
                childRelationships = ExtractRelationships(info.Compilation, childEntitySimpleNameForExtraction);
            }
            
            // Find the ManyToOne relationship that matches MappedBy
            var inverseManyToOne = childRelationships.FirstOrDefault(r => 
                r.Type == Models.RelationshipType.ManyToOne && 
                r.PropertyName.Equals(oneToManyRelationship.MappedBy, StringComparison.OrdinalIgnoreCase));
            
            if (inverseManyToOne != null && inverseManyToOne.JoinColumn != null && !string.IsNullOrEmpty(inverseManyToOne.JoinColumn.Name))
            {
                return inverseManyToOne.JoinColumn.Name;
            }
        }

        // Fallback: Try to find the FK property on the child entity
        // The FK property name is typically {MappedBy}Id (e.g., "CustomerId" if MappedBy is "Customer")
        // NOTE: We only look for the FK property (ending with "Id"), NOT the navigation property name
        var childEntitySimpleName = oneToManyRelationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata != null && info.EntitiesMetadata.TryGetValue(childEntitySimpleName, out var childMetadata))
        {
            if (childMetadata.Properties != null)
            {
                // Look for a property that matches the FK naming pattern (e.g., "CustomerId")
                // Do NOT match the navigation property name (e.g., "Customer") as that's not the FK column
                var fkPropertyName = $"{oneToManyRelationship.MappedBy}Id";
                var fkProperty = childMetadata.Properties.FirstOrDefault(p => 
                    p.Name.Equals(fkPropertyName, StringComparison.OrdinalIgnoreCase));

                if (fkProperty != null)
                {
                    // Use the column name from the FK property
                    return fkProperty.ColumnName;
                }
            }
        }

        // Fall back to default naming convention
        return $"{parentEntityName}Id";
    }
}

internal class RepositoryInfo
{
    public string InterfaceName { get; set; } = string.Empty;
    public string FullInterfaceName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string KeyType { get; set; } = string.Empty;
    public List<MethodInfo> Methods { get; set; } = new();
    public bool HasCompositeKey { get; set; }
    public List<string> CompositeKeyProperties { get; set; } = new();
    public List<ManyToManyRelationshipInfo> ManyToManyRelationships { get; set; } = new();
    public MultiTenantInfo? MultiTenantInfo { get; set; }
    public EntityMetadataInfo? EntityMetadata { get; set; }
    public Dictionary<string, EntityMetadataInfo> EntitiesMetadata { get; set; } = new();

    // Relationship-aware repository generation
    public List<Models.RelationshipMetadata> Relationships { get; set; } = new();
    public bool HasRelationships => Relationships != null && Relationships.Count > 0;
    
    // Compilation for extracting relationships from related entities
    public Compilation? Compilation { get; set; }

    // Eager loading support
    public bool HasEagerRelationships => Relationships != null && Relationships.Any(r => r.FetchType == Models.FetchType.Eager && (r.IsOwner || string.IsNullOrEmpty(r.MappedBy)));
    public List<Models.RelationshipMetadata> EagerRelationships => Relationships?.Where(r => r.FetchType == Models.FetchType.Eager && (r.IsOwner || string.IsNullOrEmpty(r.MappedBy))).ToList() ?? new();

    // Cascade operations
    public bool HasCascadeRelationships => Relationships != null && Relationships.Any(r => r.CascadeTypes != 0);
    public List<Models.RelationshipMetadata> CascadeRelationships => Relationships?.Where(r => r.CascadeTypes != 0).ToList() ?? new();

    // Orphan removal support
    public bool HasOrphanRemovalRelationships => Relationships != null && Relationships.Any(r => r.OrphanRemoval);
    public List<Models.RelationshipMetadata> OrphanRemovalRelationships => Relationships?.Where(r => r.OrphanRemoval).ToList() ?? new();
}

internal class MultiTenantInfo
{
    public bool IsMultiTenant { get; set; }
    public string TenantIdProperty { get; set; } = "TenantId";
    public bool EnforceTenantIsolation { get; set; } = true;
    public bool AllowCrossTenantQueries { get; set; } = false;
}

internal class ManyToManyRelationshipInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public string CollectionElementType { get; set; } = string.Empty;
    public string JoinTableName { get; set; } = string.Empty;
    public string JoinTableSchema { get; set; } = string.Empty;
    public string[] JoinColumns { get; set; } = Array.Empty<string>();
    public string[] InverseJoinColumns { get; set; } = Array.Empty<string>();
    public string MappedBy { get; set; } = string.Empty;
}

internal class MethodInfo
{
    public string Name { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public List<ParameterInfo> Parameters { get; set; } = new();
    public MethodAttributeInfo Attributes { get; set; } = new();
    public IMethodSymbol? Symbol { get; set; }
}

/// <summary>
/// Represents parameter information for a method.
/// </summary>
public class ParameterInfo
{
    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameter type.
    /// </summary>
    public string Type { get; set; } = string.Empty;
}

internal class MethodAttributeInfo
{
    public bool HasQuery { get; set; }
    public string? QuerySql { get; set; }
    public bool NativeQuery { get; set; }

    public bool HasNamedQuery { get; set; }
    public string? NamedQueryName { get; set; }

    public bool HasStoredProcedure { get; set; }
    public string? ProcedureName { get; set; }
    public string? Schema { get; set; }

    public bool HasMultiMapping { get; set; }
    public string? KeyProperty { get; set; }
    public string? SplitOn { get; set; }

    public bool HasBulkOperation { get; set; }
    public int BatchSize { get; set; }
    public bool UseTransaction { get; set; }

    public int? CommandTimeout { get; set; }
    public bool Buffered { get; set; } = true;

    // New custom generator attributes
    public bool HasGeneratedMethod { get; set; }
    public bool IncludeNullCheck { get; set; } = true;
    public bool GenerateAsync { get; set; }
    public bool GenerateSync { get; set; }
    public string? CustomSql { get; set; }
    public bool IncludeLogging { get; set; }
    public bool IncludeErrorHandling { get; set; }
    public string? MethodDescription { get; set; }

    public bool IgnoreInGeneration { get; set; }
    public string? IgnoreReason { get; set; }

    public bool HasCustomImplementation { get; set; }
    public bool GeneratePartialStub { get; set; } = true;
    public string? ImplementationHint { get; set; }
    public bool CustomImplementationRequired { get; set; } = true;

    public bool HasCacheResult { get; set; }
    public int CacheDuration { get; set; } = 300;
    public string? CacheKeyPattern { get; set; }
    public string? CacheRegion { get; set; }
    public bool CacheNulls { get; set; }
    public int CachePriority { get; set; }
    public bool CacheSlidingExpiration { get; set; }

    public bool HasValidateParameters { get; set; }
    public bool ThrowOnNull { get; set; } = true;
    public bool ValidateStringsNotEmpty { get; set; }
    public bool ValidateCollectionsNotEmpty { get; set; }
    public bool ValidatePositive { get; set; }
    public string? ValidationErrorMessage { get; set; }

    public bool HasRetryOnFailure { get; set; }
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMilliseconds { get; set; } = 100;
    public bool RetryExponentialBackoff { get; set; } = true;
    public int RetryMaxDelayMilliseconds { get; set; } = 30000;
    public bool LogRetries { get; set; } = true;

    public bool HasTransactionScope { get; set; }
    public bool TransactionRequired { get; set; } = true;
    public string? TransactionIsolationLevel { get; set; } = "ReadCommitted";
    public int TransactionTimeoutSeconds { get; set; } = 30;
    public bool TransactionAutoRollback { get; set; } = true;
    public bool TransactionJoinAmbient { get; set; } = true;

    // PerformanceMonitor attribute
    public bool HasPerformanceMonitor { get; set; }
    public bool IncludeParameters { get; set; }
    public int WarnThresholdMs { get; set; }
    public string? MetricCategory { get; set; }
    public bool TrackMemory { get; set; }
    public bool TrackQueryCount { get; set; }
    public string? MetricName { get; set; }

    // Audit attribute
    public bool HasAudit { get; set; }
    public bool AuditIncludeOldValue { get; set; }
    public bool AuditIncludeNewValue { get; set; } = true;
    public string AuditCategory { get; set; } = "Data";
    public string AuditSeverity { get; set; } = "Normal";
    public bool AuditIncludeParameters { get; set; } = true;
    public bool AuditCaptureUser { get; set; } = true;
    public string? AuditDescription { get; set; }
    public bool AuditCaptureIpAddress { get; set; }
}