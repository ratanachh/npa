namespace NPA.Core.Providers;

/// <summary>
/// Defines the contract for database-specific SQL dialect features.
/// </summary>
public interface ISqlDialect
{
    /// <summary>
    /// Gets the SQL statement to retrieve the last inserted identity value.
    /// </summary>
    /// <returns>The SQL statement for retrieving the last inserted identity value.</returns>
    string GetLastInsertedIdSql();

    /// <summary>
    /// Gets the SQL statement to retrieve the next value from a sequence.
    /// </summary>
    /// <param name="sequenceName">The sequence name.</param>
    /// <returns>The SQL statement for retrieving the next sequence value.</returns>
    string GetNextSequenceValueSql(string sequenceName);

    /// <summary>
    /// Escapes a database identifier (table name, column name, etc.).
    /// </summary>
    /// <param name="identifier">The identifier to escape.</param>
    /// <returns>The escaped identifier.</returns>
    string EscapeIdentifier(string identifier);

    /// <summary>
    /// Gets the SQL statement for creating a table-valued parameter type.
    /// </summary>
    /// <param name="typeName">The type name.</param>
    /// <param name="columnDefinitions">The column definitions.</param>
    /// <returns>The SQL statement for creating the table-valued parameter type.</returns>
    string GetCreateTableValuedParameterTypeSql(string typeName, IEnumerable<string> columnDefinitions);

    /// <summary>
    /// Gets the SQL for checking if a table exists.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="schemaName">The schema name (optional).</param>
    /// <returns>The SQL statement for checking table existence.</returns>
    string GetTableExistsSql(string tableName, string? schemaName = null);

    /// <summary>
    /// Gets the SQL for limiting the number of results.
    /// </summary>
    /// <param name="sql">The base SQL statement.</param>
    /// <param name="offset">The number of rows to skip.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <returns>The SQL statement with pagination.</returns>
    string GetPaginationSql(string sql, int offset, int limit);

    /// <summary>
    /// Gets the data type mapping for the specified .NET type.
    /// </summary>
    /// <param name="dotNetType">The .NET type.</param>
    /// <param name="length">The length (optional).</param>
    /// <param name="precision">The precision (optional).</param>
    /// <param name="scale">The scale (optional).</param>
    /// <returns>The database-specific data type.</returns>
    string GetDataTypeMapping(Type dotNetType, int? length = null, int? precision = null, int? scale = null);

    /// <summary>
    /// Gets the SQL for enabling full-text search on a table.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="columnNames">The column names to include in the full-text index.</param>
    /// <returns>The SQL statement for enabling full-text search.</returns>
    string GetFullTextSearchSql(string tableName, IEnumerable<string> columnNames);
}