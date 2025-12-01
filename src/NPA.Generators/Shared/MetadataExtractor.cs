using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

using NPA.Generators.Models;

namespace NPA.Generators.Shared;

/// <summary>
/// Shared utility for extracting entity metadata from Roslyn symbols.
/// Used by both EntityMetadataGenerator and RepositoryGenerator to ensure consistency.
/// </summary>
internal static class MetadataExtractor
{
    /// <summary>
    /// Extracts complete entity metadata from a type symbol.
    /// </summary>
    public static EntityMetadataInfo? ExtractEntityMetadata(INamedTypeSymbol entityType)
    {
        if (entityType == null)
            return null;

        var metadata = new EntityMetadataInfo
        {
            Name = entityType.Name,
            Namespace = entityType.ContainingNamespace?.ToDisplayString() ?? string.Empty,
            FullName = entityType.ToDisplayString(),
            TableName = ExtractTableName(entityType),
            SchemaName = ExtractSchemaName(entityType),
            Properties = ExtractProperties(entityType),
            Relationships = ExtractRelationships(entityType),
            NamedQueries = ExtractNamedQueries(entityType)
        };

        return metadata;
    }

    /// <summary>
    /// Extracts table name from [Table] attribute or generates default.
    /// </summary>
    public static string ExtractTableName(INamedTypeSymbol entityType)
    {
        var tableAttr = entityType.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TableAttribute" || a.AttributeClass?.Name == "Table");

        if (tableAttr?.ConstructorArguments.FirstOrDefault().Value is string tableName)
            return tableName;

        // Default: pluralize entity name and convert to snake_case
        return ToSnakeCase(entityType.Name) + "s";
    }

    /// <summary>
    /// Extracts schema name from [Table] attribute.
    /// </summary>
    public static string? ExtractSchemaName(INamedTypeSymbol entityType)
    {
        var tableAttr = entityType.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TableAttribute" || a.AttributeClass?.Name == "Table");

        return tableAttr?.NamedArguments
            .FirstOrDefault(na => na.Key == "Schema")
            .Value.Value?.ToString();
    }

    /// <summary>
    /// Extracts all property metadata from entity type.
    /// </summary>
    public static List<PropertyMetadataInfo> ExtractProperties(INamedTypeSymbol entityType)
    {
        var properties = new List<PropertyMetadataInfo>();
        var relationshipAttributeNames = new[] { "OneToOneAttribute", "OneToManyAttribute", "ManyToOneAttribute", "ManyToManyAttribute" };

        foreach (var member in entityType.GetMembers().OfType<IPropertySymbol>())
        {
            // Skip static properties and relationship properties
            if (member.IsStatic || member.GetAttributes().Any(a => relationshipAttributeNames.Contains(a.AttributeClass?.Name ?? "")))
                continue;

            var typeName = member.Type.ToDisplayString();
            if (member.Type.IsReferenceType && member.NullableAnnotation == NullableAnnotation.Annotated)
            {
                typeName = typeName.TrimEnd('?');
            }

            var columnName = ExtractColumnName(member);
            // Note: Keep exact property name if no [Column] attribute for compatibility
            if (string.IsNullOrEmpty(columnName))
            {
                columnName = member.Name;  // Use property name as-is, not snake_case
            }

            properties.Add(new PropertyMetadataInfo
            {
                Name = member.Name,
                TypeName = typeName,
                ColumnName = columnName,
                IsNullable = member.NullableAnnotation == NullableAnnotation.Annotated,
                IsPrimaryKey = HasAttribute(member, "IdAttribute", "Id"),
                IsIdentity = HasGeneratedValueAttribute(member),
                IsRequired = HasAttribute(member, "RequiredAttribute", "Required") || member.NullableAnnotation != NullableAnnotation.Annotated,
                IsUnique = ExtractIsUnique(member),
                Length = ExtractLength(member),
                Precision = ExtractPrecision(member),
                Scale = ExtractScale(member)
            });
        }

        return properties;
    }

    /// <summary>
    /// Extracts column name from [Column] attribute or returns property name as-is.
    /// </summary>
    public static string ExtractColumnName(IPropertySymbol property)
    {
        var columnAttr = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute" || a.AttributeClass?.Name == "Column");

        if (columnAttr != null && columnAttr.ConstructorArguments.Length > 0 && columnAttr.ConstructorArguments[0].Value is string columnName)
            return columnName;

        // No [Column] attribute: return property name as-is (preserve exact casing)
        return property.Name;
    }

    /// <summary>
    /// Checks if property has any of the specified attributes.
    /// </summary>
    public static bool HasAttribute(IPropertySymbol property, params string[] attributeNames)
    {
        return property.GetAttributes().Any(a => attributeNames.Contains(a.AttributeClass?.Name));
    }

    /// <summary>
    /// Checks if property has [GeneratedValue] attribute.
    /// </summary>
    public static bool HasGeneratedValueAttribute(IPropertySymbol property)
    {
        return property.GetAttributes().Any(a => a.AttributeClass?.Name == "GeneratedValueAttribute" || a.AttributeClass?.Name == "GeneratedValue");
    }

    /// <summary>
    /// Extracts IsUnique from [Column] attribute.
    /// </summary>
    public static bool ExtractIsUnique(IPropertySymbol property)
    {
        var columnAttr = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute" || a.AttributeClass?.Name == "Column");

        var uniqueArg = columnAttr?.NamedArguments.FirstOrDefault(na => na.Key == "IsUnique" || na.Key == "Unique");
        return uniqueArg?.Value.Value is bool isUnique && isUnique;
    }

    /// <summary>
    /// Extracts Length from [Column] attribute.
    /// </summary>
    public static int? ExtractLength(IPropertySymbol property)
    {
        var columnAttr = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute" || a.AttributeClass?.Name == "Column");

        var lengthArg = columnAttr?.NamedArguments.FirstOrDefault(na => na.Key == "Length");
        return lengthArg?.Value.Value as int?;
    }

    /// <summary>
    /// Extracts Precision from [Column] attribute.
    /// </summary>
    public static int? ExtractPrecision(IPropertySymbol property)
    {
        var columnAttr = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute" || a.AttributeClass?.Name == "Column");

        var precisionArg = columnAttr?.NamedArguments.FirstOrDefault(na => na.Key == "Precision");
        return precisionArg?.Value.Value as int?;
    }

    /// <summary>
    /// Extracts Scale from [Column] attribute.
    /// </summary>
    public static int? ExtractScale(IPropertySymbol property)
    {
        var columnAttr = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute" || a.AttributeClass?.Name == "Column");

        var scaleArg = columnAttr?.NamedArguments.FirstOrDefault(na => na.Key == "Scale");
        return scaleArg?.Value.Value as int?;
    }

    /// <summary>
    /// Extracts all relationship metadata from entity type.
    /// </summary>
    public static List<RelationshipMetadataInfo> ExtractRelationships(INamedTypeSymbol classSymbol)
    {
        var relationships = new List<RelationshipMetadataInfo>();

        foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            foreach (var attr in member.GetAttributes())
            {
                var attrName = attr.AttributeClass?.Name;
                string? targetEntity = null;
                string? relType = null;
                string? mappedBy = null;

                if (attrName == "OneToOneAttribute" || attrName == "OneToOne")
                {
                    relType = "OneToOne";
                    targetEntity = ExtractTargetEntityType(member.Type);
                    mappedBy = ExtractMappedBy(attr);
                }
                else if (attrName == "OneToManyAttribute" || attrName == "OneToMany")
                {
                    relType = "OneToMany";
                    targetEntity = ExtractCollectionElementType(member.Type);
                    mappedBy = ExtractMappedBy(attr);
                }
                else if (attrName == "ManyToOneAttribute" || attrName == "ManyToOne")
                {
                    relType = "ManyToOne";
                    targetEntity = ExtractTargetEntityType(member.Type);
                }
                else if (attrName == "ManyToManyAttribute" || attrName == "ManyToMany")
                {
                    relType = "ManyToMany";
                    targetEntity = ExtractCollectionElementType(member.Type);
                }

                if (relType != null && targetEntity != null)
                {
                    relationships.Add(new RelationshipMetadataInfo
                    {
                        PropertyName = member.Name,
                        Type = relType,
                        TargetEntity = targetEntity,
                        MappedBy = mappedBy,
                        IsOwner = relType == "ManyToOne" || (relType == "OneToOne" && string.IsNullOrEmpty(mappedBy))
                    });
                }
            }
        }

        return relationships;
    }

    /// <summary>
    /// Extracts target entity type from navigation property.
    /// </summary>
    public static string? ExtractTargetEntityType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedType && namedType.IsReferenceType)
        {
            return namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        }
        return typeSymbol.ToDisplayString().TrimEnd('?');
    }

    /// <summary>
    /// Extracts element type from collection (e.g., ICollection&lt;Order&gt; -> Order).
    /// </summary>
    public static string? ExtractCollectionElementType(ITypeSymbol typeSymbol)
    {
        var elementType = (typeSymbol as INamedTypeSymbol)?.TypeArguments.FirstOrDefault();
        if (elementType == null)
            return null;
            
        // Return fully qualified name for nested types
        if (elementType is INamedTypeSymbol namedType && namedType.IsReferenceType)
        {
            return namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        }
        return elementType.ToDisplayString();
    }

    /// <summary>
    /// Extracts MappedBy from relationship attribute.
    /// </summary>
    public static string? ExtractMappedBy(AttributeData attr)
    {
        // Check constructor arguments
        if (attr.ConstructorArguments.Length > 0)
        {
            return attr.ConstructorArguments[0].Value?.ToString();
        }

        // Check named arguments
        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg.Key == "MappedBy")
            {
                return namedArg.Value.Value?.ToString();
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts named queries from entity class.
    /// </summary>
    public static List<NamedQueryInfo> ExtractNamedQueries(INamedTypeSymbol classSymbol)
    {
        var namedQueries = new List<NamedQueryInfo>();

        foreach (var attr in classSymbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.Name;
            if (attrName == "NamedQueryAttribute" || attrName == "NamedQuery")
            {
                if (attr.ConstructorArguments.Length >= 2)
                {
                    var name = attr.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    var query = attr.ConstructorArguments[1].Value?.ToString() ?? string.Empty;

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(query))
                    {
                        var namedQuery = new NamedQueryInfo
                        {
                            Name = name,
                            Query = query
                        };

                        // Extract optional named arguments
                        foreach (var namedArg in attr.NamedArguments)
                        {
                            switch (namedArg.Key)
                            {
                                case "NativeQuery":
                                    namedQuery.NativeQuery = (bool)(namedArg.Value.Value ?? false);
                                    break;
                                case "CommandTimeout":
                                    if (namedArg.Value.Value is int timeout)
                                        namedQuery.CommandTimeout = timeout;
                                    break;
                                case "Buffered":
                                    namedQuery.Buffered = (bool)(namedArg.Value.Value ?? true);
                                    break;
                                case "Description":
                                    namedQuery.Description = namedArg.Value.Value?.ToString();
                                    break;
                            }
                        }

                        namedQueries.Add(namedQuery);
                    }
                }
            }
        }

        return namedQueries;
    }

    /// <summary>
    /// Converts PascalCase to snake_case.
    /// </summary>
    public static string ToSnakeCase(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        
        var result = new System.Text.StringBuilder();
        result.Append(char.ToLower(text[0]));
        
        for (int i = 1; i < text.Length; i++)
        {
            if (char.IsUpper(text[i]))
            {
                result.Append('_');
                result.Append(char.ToLower(text[i]));
            }
            else
            {
                result.Append(text[i]);
            }
        }
        
        return result.ToString();
    }
}
