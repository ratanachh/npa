using System;
using System.Collections.Generic;
using System.Linq;
using NPA.Design.Models;

namespace NPA.Design.Generators.Helpers;

/// <summary>
/// Helper class for metadata access and manipulation.
/// </summary>
internal static class MetadataHelper
{
    /// <summary>
    /// Gets the column name for a property, using metadata if available.
    /// </summary>
    public static string GetColumnNameForProperty(string propertyName, EntityMetadataInfo? entityMetadata)
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

    /// <summary>
    /// Gets the property name for a column, using metadata if available.
    /// </summary>
    public static string GetPropertyNameForColumn(RepositoryInfo info, string columnName, string? entityTypeName = null)
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

        return StringHelper.ToPascalCase(columnName);
    }

    /// <summary>
    /// Checks if a property exists on an entity by column name or property name.
    /// </summary>
    public static bool HasProperty(RepositoryInfo info, string columnOrPropertyName, string? entityTypeName = null)
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

    /// <summary>
    /// Checks if a property is nullable.
    /// </summary>
    public static bool IsPropertyNullable(RepositoryInfo info, string columnName, string? entityTypeName = null)
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

    /// <summary>
    /// Gets the key type of a related entity.
    /// </summary>
    public static string GetRelatedEntityKeyType(RepositoryInfo info, string relatedEntityTypeName)
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
    public static string? GetForeignKeyPropertyType(RepositoryInfo info, string columnName, string? entityTypeName = null)
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
    public static string GetKeyPropertyName(RepositoryInfo info, string? entityTypeName = null)
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
    public static string GetKeyColumnName(RepositoryInfo info, string? entityTypeName = null)
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
    /// Gets the table name from metadata, or generates a default name.
    /// </summary>
    public static string? GetTableNameFromMetadata(RepositoryInfo info, string entityType)
    {
        // Try to find the entity in the metadata dictionary
        var simpleName = entityType.Split('.').Last();
        if (info.EntitiesMetadata != null && info.EntitiesMetadata.TryGetValue(simpleName, out var metadata))
        {
            return metadata.TableName;
        }
        return null;
    }

    /// <summary>
    /// Builds a column list from metadata, or returns "*" if no metadata available.
    /// </summary>
    public static string BuildColumnList(EntityMetadataInfo? metadata)
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
}

