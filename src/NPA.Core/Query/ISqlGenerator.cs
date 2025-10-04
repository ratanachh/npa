using NPA.Core.Metadata;

namespace NPA.Core.Query;

/// <summary>
/// Generates database-specific SQL from parsed queries.
/// </summary>
public interface ISqlGenerator
{
    /// <summary>
    /// Generates SQL from a parsed query.
    /// </summary>
    /// <param name="parsedQuery">The parsed query structure.</param>
    /// <param name="entityMetadata">The entity metadata for the main entity.</param>
    /// <returns>The generated SQL string.</returns>
    string Generate(ParsedQuery parsedQuery, EntityMetadata entityMetadata);

    /// <summary>
    /// Generates a SELECT SQL statement.
    /// </summary>
    /// <param name="parsedQuery">The parsed query structure.</param>
    /// <param name="entityMetadata">The entity metadata for the main entity.</param>
    /// <returns>The generated SELECT SQL.</returns>
    string GenerateSelect(ParsedQuery parsedQuery, EntityMetadata entityMetadata);

    /// <summary>
    /// Generates an UPDATE SQL statement.
    /// </summary>
    /// <param name="parsedQuery">The parsed query structure.</param>
    /// <param name="entityMetadata">The entity metadata for the main entity.</param>
    /// <returns>The generated UPDATE SQL.</returns>
    string GenerateUpdate(ParsedQuery parsedQuery, EntityMetadata entityMetadata);

    /// <summary>
    /// Generates a DELETE SQL statement.
    /// </summary>
    /// <param name="parsedQuery">The parsed query structure.</param>
    /// <param name="entityMetadata">The entity metadata for the main entity.</param>
    /// <returns>The generated DELETE SQL.</returns>
    string GenerateDelete(ParsedQuery parsedQuery, EntityMetadata entityMetadata);

    /// <summary>
    /// Generates a WHERE clause from the parsed query.
    /// </summary>
    /// <param name="whereClause">The WHERE clause string.</param>
    /// <param name="entityMetadata">The entity metadata for the main entity.</param>
    /// <returns>The generated WHERE clause.</returns>
    string GenerateWhereClause(string? whereClause, EntityMetadata entityMetadata);

    /// <summary>
    /// Generates an ORDER BY clause from the parsed query.
    /// </summary>
    /// <param name="orderByClause">The ORDER BY clause string.</param>
    /// <param name="entityMetadata">The entity metadata for the main entity.</param>
    /// <returns>The generated ORDER BY clause.</returns>
    string GenerateOrderByClause(string? orderByClause, EntityMetadata entityMetadata);
}
