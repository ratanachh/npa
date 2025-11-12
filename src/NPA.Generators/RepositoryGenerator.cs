using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

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
            EntitiesMetadata = entitiesMetadata
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

        foreach (var namedArg in multiTenantAttr.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "TenantIdProperty":
                case "tenantIdProperty":
                    if (namedArg.Value.Value is string prop)
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

        // Check constructor arguments as well (for tenantIdProperty)
        if (multiTenantAttr.ConstructorArguments.Length > 0 && 
            multiTenantAttr.ConstructorArguments[0].Value is string constructorProp)
        {
            tenantIdProperty = constructorProp;
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

        var metadata = new EntityMetadataInfo
        {
            Name = entityType.Name,
            Namespace = entityType.ContainingNamespace?.ToDisplayString() ?? string.Empty,
            FullName = entityType.ToDisplayString(),
            TableName = GetTableNameFromAttribute(entityType),
            SchemaName = GetSchemaNameFromAttribute(entityType),
            Properties = new List<PropertyMetadataInfo>(),
            Relationships = new List<RelationshipMetadataInfo>()
        };

        // Extract property metadata
        foreach (var property in entityType.GetMembers().OfType<IPropertySymbol>())
        {
            var columnName = GetColumnNameFromAttribute(property);
            var isPrimaryKey = property.GetAttributes().Any(a => a.AttributeClass?.Name == "IdAttribute");
            var isRequired = property.GetAttributes().Any(a => a.AttributeClass?.Name == "RequiredAttribute");
            var isUnique = property.GetAttributes().Any(a => a.AttributeClass?.Name == "UniqueAttribute");

            metadata.Properties.Add(new PropertyMetadataInfo
            {
                Name = property.Name,
                TypeName = property.Type.ToDisplayString(),
                ColumnName = columnName,
                IsNullable = property.NullableAnnotation == NullableAnnotation.Annotated,
                IsPrimaryKey = isPrimaryKey,
                IsRequired = isRequired,
                IsUnique = isUnique
            });
            
            // Extract relationship metadata from ManyToOne and OneToMany attributes
            var manyToOneAttr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "ManyToOneAttribute");
            var oneToManyAttr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "OneToManyAttribute");
            var oneToOneAttr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "OneToOneAttribute");
            
            if (manyToOneAttr != null && property.Type is INamedTypeSymbol relatedType)
            {
                metadata.Relationships.Add(new RelationshipMetadataInfo
                {
                    PropertyName = property.Name,
                    Type = "ManyToOne",
                    TargetEntity = relatedType.Name
                });
            }
            else if (oneToManyAttr != null && property.Type is INamedTypeSymbol { TypeArguments.Length: > 0 } collectionType)
            {
                var targetType = collectionType.TypeArguments[0] as INamedTypeSymbol;
                if (targetType != null)
                {
                    metadata.Relationships.Add(new RelationshipMetadataInfo
                    {
                        PropertyName = property.Name,
                        Type = "OneToMany",
                        TargetEntity = targetType.Name
                    });
                }
            }
            else if (oneToOneAttr != null && property.Type is INamedTypeSymbol oneToOneRelatedType)
            {
                metadata.Relationships.Add(new RelationshipMetadataInfo
                {
                    PropertyName = property.Name,
                    Type = "OneToOne",
                    TargetEntity = oneToOneRelatedType.Name
                });
            }
        }

        return metadata;
    }

    private static string GetTableNameFromAttribute(INamedTypeSymbol entityType)
    {
        var tableAttr = entityType.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TableAttribute");

        if (tableAttr != null && tableAttr.ConstructorArguments.Length > 0)
        {
            if (tableAttr.ConstructorArguments[0].Value is string tableName)
                return tableName;
        }

        // Default: pluralize entity name and convert to snake_case
        return MethodConventionAnalyzer.ToSnakeCase(entityType.Name) + "s";
    }

    private static string? GetSchemaNameFromAttribute(INamedTypeSymbol entityType)
    {
        var tableAttr = entityType.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TableAttribute");

        if (tableAttr != null)
        {
            var schemaArg = tableAttr.NamedArguments.FirstOrDefault(a => a.Key == "Schema");
            if (schemaArg.Value.Value is string schema)
                return schema;
        }

        return null;
    }

    private static string GetColumnNameFromAttribute(IPropertySymbol property)
    {
        var columnAttr = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute");

        if (columnAttr != null && columnAttr.ConstructorArguments.Length > 0)
        {
            if (columnAttr.ConstructorArguments[0].Value is string columnName)
                return columnName;
        }

        // Default: convert property name to snake_case
        return MethodConventionAnalyzer.ToSnakeCase(property.Name);
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
        sb.AppendLine($"    public class {GetImplementationName(info.InterfaceName)} : BaseRepository<{info.EntityType}, {info.KeyType}>, {info.FullInterfaceName}");
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
            sb.AppendLine($"            var sql = @\"");
            sb.AppendLine($"                SELECT r.*");
            sb.AppendLine($"                FROM {joinTable} jt");
            sb.AppendLine($"                INNER JOIN {relatedName} r ON jt.{targetKeyColumn} = r.Id");
            sb.AppendLine($"                WHERE jt.{ownerKeyColumn} = @{entityName}Id\";");
            sb.AppendLine();
            sb.AppendLine($"            return await _connection.QueryAsync<{relationship.CollectionElementType}>(");
            sb.AppendLine($"                sql,");
            sb.AppendLine($"                new {{ {entityName}Id = {ToCamelCase(entityName)}Id }},");
            sb.AppendLine("                _transaction);");
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
            sb.AppendLine($"                new {{ {entityName}Id = {ToCamelCase(entityName)}Id, {relatedName}Id = {ToCamelCase(relatedName)}Id }},");
            sb.AppendLine("                _transaction);");
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
            sb.AppendLine($"                new {{ {entityName}Id = {ToCamelCase(entityName)}Id, {relatedName}Id = {ToCamelCase(relatedName)}Id }},");
            sb.AppendLine("                _transaction);");
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
            sb.AppendLine($"                new {{ {entityName}Id = {ToCamelCase(entityName)}Id, {relatedName}Id = {ToCamelCase(relatedName)}Id }},");
            sb.AppendLine("                _transaction);");
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

        // Check for custom attributes first
        if (attrs.HasQuery)
        {
            sb.Append(GenerateQueryMethodBody(method, info, attrs));
        }
        else if (attrs.HasStoredProcedure)
        {
            sb.Append(GenerateStoredProcedureMethodBody(method, info.EntityType, attrs));
        }
        else if (attrs.HasBulkOperation)
        {
            sb.Append(GenerateBulkOperationMethodBody(method, info.EntityType, attrs));
        }
        else if (method.Symbol != null)
        {
            // Use convention-based generation
            var convention = MethodConventionAnalyzer.AnalyzeMethod(method.Symbol);
            sb.Append(GenerateConventionBasedMethodBody(method, info.EntityType, convention));
        }
        else
        {
            // Fallback to simple conventions
            sb.Append(GenerateSimpleConventionBody(method, info.EntityType));
        }

        return sb.ToString();
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

    private static string GenerateConventionBasedMethodBody(MethodInfo method, string entityType, MethodConvention convention)
    {
        var sb = new StringBuilder();
        var isAsync = method.ReturnType.StartsWith("System.Threading.Tasks.Task");
        var tableName = GetTableName(entityType);

        switch (convention.QueryType)
        {
            case QueryType.Select:
                sb.Append(GenerateSelectQuery(method, entityType, tableName, convention, isAsync));
                break;
            case QueryType.Count:
                sb.Append(GenerateCountQuery(method, entityType, tableName, convention, isAsync));
                break;
            case QueryType.Exists:
                sb.Append(GenerateExistsQuery(method, entityType, tableName, convention, isAsync));
                break;
            case QueryType.Delete:
                sb.Append(GenerateDeleteQuery(method, entityType, tableName, convention, isAsync));
                break;
            case QueryType.Update:
            case QueryType.Insert:
                sb.Append(GenerateModificationQuery(method, entityType, convention, isAsync));
                break;
            default:
                sb.AppendLine($"            throw new NotImplementedException(\"Method {method.Name} requires manual implementation\");");
                break;
        }

        return sb.ToString();
    }

    private static string GenerateSelectQuery(MethodInfo method, string entityType, string tableName, MethodConvention convention, bool isAsync)
    {
        var sb = new StringBuilder();
        var whereClause = BuildWhereClause(convention.PropertyNames, convention.PropertySeparators, convention.Parameters);
        var orderByClause = BuildOrderByClause(convention.OrderByProperties);
        var paramObj = GenerateParameterObject(convention.Parameters);
        var hasParameters = !string.IsNullOrEmpty(paramObj) && paramObj != "null";

        // Build the full SQL query with optional DISTINCT and LIMIT
        var selectClause = convention.HasDistinct ? "SELECT DISTINCT *" : "SELECT *";
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
                    sb.AppendLine($"            return await _connection.QueryAsync<{entityType}>(sql);");
                }
                else
                {
                    sb.AppendLine($"            return _connection.Query<{entityType}>(sql);");
                }
            }
            else
            {
                if (isAsync)
                {
                    sb.AppendLine($"            return await _connection.QueryFirstOrDefaultAsync<{entityType}>(sql);");
                }
                else
                {
                    sb.AppendLine($"            return _connection.QueryFirstOrDefault<{entityType}>(sql);");
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
                    sb.AppendLine($"            return await _connection.QueryAsync<{entityType}>(sql, {paramObj});");
                }
                else
                {
                    sb.AppendLine($"            return _connection.Query<{entityType}>(sql, {paramObj});");
                }
            }
            else
            {
                if (isAsync)
                {
                    sb.AppendLine($"            return await _connection.QueryFirstOrDefaultAsync<{entityType}>(sql, {paramObj});");
                }
                else
                {
                    sb.AppendLine($"            return _connection.QueryFirstOrDefault<{entityType}>(sql, {paramObj});");
                }
            }
        }

        return sb.ToString();
    }

    private static string GenerateCountQuery(MethodInfo method, string entityType, string tableName, MethodConvention convention, bool isAsync)
    {
        var sb = new StringBuilder();
        var whereClause = BuildWhereClause(convention.PropertyNames, convention.PropertySeparators, convention.Parameters);
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

    private static string GenerateExistsQuery(MethodInfo method, string entityType, string tableName, MethodConvention convention, bool isAsync)
    {
        var sb = new StringBuilder();
        var whereClause = BuildWhereClause(convention.PropertyNames, convention.PropertySeparators, convention.Parameters);
        var paramObj = GenerateParameterObject(convention.Parameters);

        sb.AppendLine($"            var sql = \"SELECT COUNT(1) FROM {tableName} WHERE {whereClause}\";");
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

    private static string GenerateDeleteQuery(MethodInfo method, string entityType, string tableName, MethodConvention convention, bool isAsync)
    {
        var sb = new StringBuilder();
        var whereClause = BuildWhereClause(convention.PropertyNames, convention.PropertySeparators, convention.Parameters);
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

    private static string BuildWhereClause(List<string> propertyNames, List<string> separators, List<ParameterInfo> parameters)
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
                var columnName = MethodConventionAnalyzer.ToSnakeCase(propertyName);
                
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
                var columnName = MethodConventionAnalyzer.ToSnakeCase(propExpression);
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

    private static string BuildOrderByClause(List<OrderByInfo> orderByProperties)
    {
        if (orderByProperties.Count == 0)
            return string.Empty;

        var clauses = new List<string>();
        foreach (var orderBy in orderByProperties)
        {
            var columnName = MethodConventionAnalyzer.ToSnakeCase(orderBy.PropertyName);
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

    private static string GenerateSimpleConventionBody(MethodInfo method, string entityType)
    {
        var sb = new StringBuilder();

        // Simple convention analysis (fallback)
        if (method.Name.StartsWith("GetAll") || method.Name.StartsWith("FindAll"))
        {
            sb.AppendLine($"            var sql = \"SELECT * FROM {GetTableName(entityType)}\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{entityType}>(sql);");
        }
        else if (method.Name.StartsWith("GetById") || method.Name.StartsWith("FindById"))
        {
            sb.AppendLine($"            var sql = \"SELECT * FROM {GetTableName(entityType)} WHERE id = @id\";");
            sb.AppendLine($"            return await _connection.QueryFirstOrDefaultAsync<{entityType}>(sql, new {{ id }});");
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

/// <summary>
/// Equality comparer for RepositoryInfo to enable incremental generator caching.
/// Only regenerates code when repository metadata actually changes.
/// </summary>
internal class RepositoryInfoComparer : IEqualityComparer<RepositoryInfo>
{
    public bool Equals(RepositoryInfo? x, RepositoryInfo? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        // Compare basic properties
        if (x.InterfaceName != y.InterfaceName ||
            x.FullInterfaceName != y.FullInterfaceName ||
            x.Namespace != y.Namespace ||
            x.EntityType != y.EntityType ||
            x.KeyType != y.KeyType ||
            x.HasCompositeKey != y.HasCompositeKey)
            return false;

        // Compare composite key properties
        if (!x.CompositeKeyProperties.SequenceEqual(y.CompositeKeyProperties))
            return false;

        // Compare methods
        if (x.Methods.Count != y.Methods.Count)
            return false;

        for (int i = 0; i < x.Methods.Count; i++)
        {
            if (!MethodInfoEquals(x.Methods[i], y.Methods[i]))
                return false;
        }

        // Compare many-to-many relationships
        if (x.ManyToManyRelationships.Count != y.ManyToManyRelationships.Count)
            return false;

        for (int i = 0; i < x.ManyToManyRelationships.Count; i++)
        {
            if (!ManyToManyRelationshipInfoEquals(x.ManyToManyRelationships[i], y.ManyToManyRelationships[i]))
                return false;
        }

        // Compare multi-tenancy information
        if (!MultiTenantInfoEquals(x.MultiTenantInfo, y.MultiTenantInfo))
            return false;

        return true;
    }

    public int GetHashCode(RepositoryInfo obj)
    {
        if (obj is null) return 0;

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (obj.InterfaceName?.GetHashCode() ?? 0);
            hash = hash * 31 + (obj.FullInterfaceName?.GetHashCode() ?? 0);
            hash = hash * 31 + (obj.Namespace?.GetHashCode() ?? 0);
            hash = hash * 31 + (obj.EntityType?.GetHashCode() ?? 0);
            hash = hash * 31 + (obj.KeyType?.GetHashCode() ?? 0);
            hash = hash * 31 + obj.HasCompositeKey.GetHashCode();

            foreach (var prop in obj.CompositeKeyProperties)
                hash = hash * 31 + (prop?.GetHashCode() ?? 0);

            foreach (var method in obj.Methods)
                hash = hash * 31 + GetMethodInfoHashCode(method);

            foreach (var rel in obj.ManyToManyRelationships)
                hash = hash * 31 + GetManyToManyHashCode(rel);

            if (obj.MultiTenantInfo != null)
                hash = hash * 31 + GetMultiTenantHashCode(obj.MultiTenantInfo);

            return hash;
        }
    }

    private bool MethodInfoEquals(MethodInfo x, MethodInfo y)
    {
        if (x.Name != y.Name || x.ReturnType != y.ReturnType)
            return false;

        if (x.Parameters.Count != y.Parameters.Count)
            return false;

        for (int i = 0; i < x.Parameters.Count; i++)
        {
            if (x.Parameters[i].Name != y.Parameters[i].Name ||
                x.Parameters[i].Type != y.Parameters[i].Type)
                return false;
        }

        return MethodAttributeInfoEquals(x.Attributes, y.Attributes);
    }

    private bool MethodAttributeInfoEquals(MethodAttributeInfo x, MethodAttributeInfo y)
    {
        return x.HasQuery == y.HasQuery &&
               x.QuerySql == y.QuerySql &&
               x.NativeQuery == y.NativeQuery &&
               x.HasStoredProcedure == y.HasStoredProcedure &&
               x.ProcedureName == y.ProcedureName &&
               x.Schema == y.Schema &&
               x.HasMultiMapping == y.HasMultiMapping &&
               x.KeyProperty == y.KeyProperty &&
               x.SplitOn == y.SplitOn &&
               x.HasBulkOperation == y.HasBulkOperation &&
               x.BatchSize == y.BatchSize &&
               x.UseTransaction == y.UseTransaction &&
               x.CommandTimeout == y.CommandTimeout &&
               x.Buffered == y.Buffered;
    }

    private bool ManyToManyRelationshipInfoEquals(ManyToManyRelationshipInfo x, ManyToManyRelationshipInfo y)
    {
        return x.PropertyName == y.PropertyName &&
               x.PropertyType == y.PropertyType &&
               x.CollectionElementType == y.CollectionElementType &&
               x.JoinTableName == y.JoinTableName &&
               x.JoinTableSchema == y.JoinTableSchema &&
               x.JoinColumns.SequenceEqual(y.JoinColumns) &&
               x.InverseJoinColumns.SequenceEqual(y.InverseJoinColumns) &&
               x.MappedBy == y.MappedBy;
    }

    private bool MultiTenantInfoEquals(MultiTenantInfo? x, MultiTenantInfo? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;
        
        return x.IsMultiTenant == y.IsMultiTenant &&
               x.TenantIdProperty == y.TenantIdProperty &&
               x.EnforceTenantIsolation == y.EnforceTenantIsolation &&
               x.AllowCrossTenantQueries == y.AllowCrossTenantQueries;
    }

    private int GetMethodInfoHashCode(MethodInfo method)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (method.Name?.GetHashCode() ?? 0);
            hash = hash * 31 + (method.ReturnType?.GetHashCode() ?? 0);
            foreach (var param in method.Parameters)
            {
                hash = hash * 31 + (param.Name?.GetHashCode() ?? 0);
                hash = hash * 31 + (param.Type?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }

    private int GetManyToManyHashCode(ManyToManyRelationshipInfo rel)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (rel.PropertyName?.GetHashCode() ?? 0);
            hash = hash * 31 + (rel.CollectionElementType?.GetHashCode() ?? 0);
            hash = hash * 31 + (rel.JoinTableName?.GetHashCode() ?? 0);
            return hash;
        }
    }

    private int GetMultiTenantHashCode(MultiTenantInfo info)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + info.IsMultiTenant.GetHashCode();
            hash = hash * 31 + (info.TenantIdProperty?.GetHashCode() ?? 0);
            hash = hash * 31 + info.EnforceTenantIsolation.GetHashCode();
            hash = hash * 31 + info.AllowCrossTenantQueries.GetHashCode();
            return hash;
        }
    }
}
