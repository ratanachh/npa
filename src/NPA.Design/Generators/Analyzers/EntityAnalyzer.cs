using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using NPA.Design.Models;
using NPA.Design.Shared;

namespace NPA.Design.Generators.Analyzers;

/// <summary>
/// Analyzes entity characteristics and extracts metadata.
/// </summary>
internal static class EntityAnalyzer
{
    /// <summary>
    /// Detects if an entity has a composite key.
    /// </summary>
    public static (bool hasCompositeKey, List<string> keyProperties) DetectCompositeKey(Compilation compilation, string entityTypeName)
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

    /// <summary>
    /// Detects many-to-many relationships in an entity.
    /// </summary>
    public static List<ManyToManyRelationshipInfo> DetectManyToManyRelationships(Compilation compilation, string entityTypeName)
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

    /// <summary>
    /// Detects multi-tenancy configuration for an entity.
    /// </summary>
    public static MultiTenantInfo? DetectMultiTenancy(Compilation compilation, string entityTypeName)
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

    /// <summary>
    /// Extracts entity metadata (table name and column mappings).
    /// </summary>
    public static EntityMetadataInfo? ExtractEntityMetadata(Compilation compilation, string entityTypeName)
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
    /// Builds a dictionary of entity metadata including the main entity and all related entities.
    /// </summary>
    public static Dictionary<string, EntityMetadataInfo> BuildEntityMetadataDictionary(
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

    /// <summary>
    /// Extracts relationship metadata from an entity.
    /// </summary>
    public static List<Models.RelationshipMetadata> ExtractRelationships(Compilation compilation, string entityTypeName)
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
            var relationshipMetadata = RelationshipExtractor.ExtractRelationshipMetadata(member);
            if (relationshipMetadata != null)
            {
                relationships.Add(relationshipMetadata);
            }
        }

        return relationships;
    }
}

