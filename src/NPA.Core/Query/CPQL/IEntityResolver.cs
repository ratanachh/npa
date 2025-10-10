using NPA.Core.Metadata;

namespace NPA.Core.Query.CPQL;

/// <summary>
/// Interface for resolving entity information during query parsing.
/// </summary>
public interface IEntityResolver
{
    /// <summary>
    /// Gets entity metadata by entity name.
    /// </summary>
    /// <param name="entityName">The entity name.</param>
    /// <returns>The entity metadata.</returns>
    EntityMetadata GetEntityMetadata(string entityName);
    
    /// <summary>
    /// Gets the table name for an entity.
    /// </summary>
    /// <param name="entityName">The entity name.</param>
    /// <returns>The table name.</returns>
    string GetTableName(string entityName);
    
    /// <summary>
    /// Gets the column name for a property.
    /// </summary>
    /// <param name="entityName">The entity name.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The column name.</returns>
    string GetColumnName(string entityName, string propertyName);
    
    /// <summary>
    /// Checks if an entity has a relationship property.
    /// </summary>
    /// <param name="entityName">The entity name.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>True if the property is a relationship; otherwise, false.</returns>
    bool IsRelationshipProperty(string entityName, string propertyName);
    
    /// <summary>
    /// Gets relationship metadata for a property.
    /// </summary>
    /// <param name="entityName">The entity name.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The relationship metadata.</returns>
    RelationshipMetadata? GetRelationshipMetadata(string entityName, string propertyName);
}

