namespace NPA.Core.Query.CPQL.SqlGeneration;

/// <summary>
/// Interface for mapping entity and property names to database names.
/// </summary>
public interface IEntityMapper
{
    /// <summary>
    /// Gets the table name for an entity.
    /// </summary>
    /// <param name="entityName">The entity name.</param>
    /// <returns>The table name.</returns>
    string GetTableName(string entityName);
    
    /// <summary>
    /// Gets the column name for a property.
    /// </summary>
    /// <param name="entityAlias">The entity alias (optional).</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The column name with optional alias prefix.</returns>
    string GetColumnName(string? entityAlias, string propertyName);
}

