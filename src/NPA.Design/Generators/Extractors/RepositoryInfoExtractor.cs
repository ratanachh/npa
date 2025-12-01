using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using NPA.Design.Models;
using NPA.Design.Generators.Analyzers;

namespace NPA.Design.Generators.Extractors;

/// <summary>
/// Extracts repository information from interface declarations.
/// </summary>
internal static class RepositoryInfoExtractor
{
    /// <summary>
    /// Checks if a syntax node is a repository interface.
    /// </summary>
    public static bool IsRepositoryInterface(SyntaxNode node)
    {
        // Optimized predicate: Only consider interface declarations with attributes
        if (node is not InterfaceDeclarationSyntax interfaceDecl)
            return false;

        // Quick check: Must have attributes and name should contain "Repository"
        // This reduces the number of nodes passed to the expensive semantic analysis
        return interfaceDecl.AttributeLists.Count > 0 &&
               interfaceDecl.Identifier.Text.Contains("Repository");
    }

    /// <summary>
    /// Extracts repository information from a generator syntax context.
    /// </summary>
    public static RepositoryInfo? GetRepositoryInfo(GeneratorSyntaxContext context)
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
        var (hasCompositeKey, compositeKeyProps) = EntityAnalyzer.DetectCompositeKey(semanticModel.Compilation, entityType);

        // Detect many-to-many relationships
        var manyToManyRelationships = EntityAnalyzer.DetectManyToManyRelationships(semanticModel.Compilation, entityType);

        // Detect multi-tenancy
        var multiTenantInfo = EntityAnalyzer.DetectMultiTenancy(semanticModel.Compilation, entityType);

        // Extract entity metadata (table name and column mappings)
        var entityMetadata = EntityAnalyzer.ExtractEntityMetadata(semanticModel.Compilation, entityType);

        // Build dictionary of all entity metadata (main + related entities)
        var entitiesMetadata = EntityAnalyzer.BuildEntityMetadataDictionary(semanticModel.Compilation, entityMetadata);

        // Extract relationship metadata
        var relationships = EntityAnalyzer.ExtractRelationships(semanticModel.Compilation, entityType);

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

    /// <summary>
    /// Extracts entity and key types from repository interface.
    /// </summary>
    public static (string? entityType, string? keyType) ExtractRepositoryTypes(INamedTypeSymbol interfaceSymbol)
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
            // Remove "I" prefix (1 char) and "Repository" suffix (10 chars) = 11 total
            const int prefixLength = 1; // "I"
            const int suffixLength = 10; // "Repository"
            const int totalRemovedLength = prefixLength + suffixLength;
            
            var entityTypeLength = interfaceName.Length - totalRemovedLength;
            if (entityTypeLength <= 0)
            {
                // Interface name is exactly "IRepository" or shorter - cannot infer entity type
                return (null, null);
            }
            
            // Extract entity type: remove first char ("I") and last 10 chars ("Repository")
            var entityType = interfaceName.Substring(prefixLength, entityTypeLength);
            if (string.IsNullOrEmpty(entityType))
            {
                return (null, null);
            }
            
            return (entityType, "object"); // Default key type
        }

        return (null, null);
    }

    /// <summary>
    /// Extracts method attributes from a method symbol.
    /// </summary>
    public static MethodAttributeInfo ExtractMethodAttributes(IMethodSymbol method)
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

    /// <summary>
    /// Gets a named argument value from an attribute.
    /// </summary>
    private static T? GetNamedArgument<T>(AttributeData attr, string name)
    {
        var namedArg = attr.NamedArguments.FirstOrDefault(arg => arg.Key == name);
        if (namedArg.Value.Value is T value)
        {
            return value;
        }
        return default;
    }
}

