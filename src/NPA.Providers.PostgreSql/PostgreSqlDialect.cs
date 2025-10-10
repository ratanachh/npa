using NPA.Core.Providers;

namespace NPA.Providers.PostgreSql;

/// <summary>
/// PostgreSQL-specific dialect implementation.
/// </summary>
public class PostgreSqlDialect : ISqlDialect
{
    /// <inheritdoc />
    public string GetLastInsertedIdSql()
    {
        // PostgreSQL uses RETURNING clause in INSERT statements
        // This method returns empty as it's handled differently
        return string.Empty;
    }

    /// <inheritdoc />
    public string GetNextSequenceValueSql(string sequenceName)
    {
        if (string.IsNullOrWhiteSpace(sequenceName))
            throw new ArgumentException("Sequence name cannot be null or empty.", nameof(sequenceName));

        return $"SELECT nextval('{EscapeIdentifier(sequenceName)}')";
    }

    /// <inheritdoc />
    public string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier cannot be null or empty.", nameof(identifier));

        // PostgreSQL uses double quotes for escaping identifiers
        // and lowercases unquoted identifiers
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }

    /// <inheritdoc />
    public string GetCreateTableValuedParameterTypeSql(string typeName, IEnumerable<string> columnDefinitions)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            throw new ArgumentException("Type name cannot be null or empty.", nameof(typeName));

        // PostgreSQL uses composite types instead of table-valued parameters
        var columns = string.Join(",\n    ", columnDefinitions);
        return $@"CREATE TYPE {EscapeIdentifier(typeName)} AS (
    {columns}
)";
    }

    /// <inheritdoc />
    public string GetTableExistsSql(string tableName, string? schemaName = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));

        var schema = string.IsNullOrWhiteSpace(schemaName) ? "public" : schemaName;
        
        return @"SELECT EXISTS (
    SELECT 1 FROM information_schema.tables 
    WHERE table_schema = @SchemaName AND table_name = @TableName
)";
    }

    /// <inheritdoc />
    public string GetPaginationSql(string sql, int offset, int limit)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL cannot be null or empty.", nameof(sql));

        if (offset < 0)
            throw new ArgumentException("Offset cannot be negative.", nameof(offset));

        if (limit <= 0)
            throw new ArgumentException("Limit must be positive.", nameof(limit));

        // PostgreSQL uses LIMIT/OFFSET for pagination
        return $"{sql} LIMIT {limit} OFFSET {offset}";
    }

    /// <inheritdoc />
    public string GetDataTypeMapping(Type dotNetType, int? length = null, int? precision = null, int? scale = null)
    {
        if (dotNetType == null)
            throw new ArgumentNullException(nameof(dotNetType));

        // Handle nullable types
        var actualType = Nullable.GetUnderlyingType(dotNetType) ?? dotNetType;

        return actualType switch
        {
            // Integer types
            Type t when t == typeof(byte) => "SMALLINT", // PostgreSQL doesn't have TINYINT
            Type t when t == typeof(short) => "SMALLINT",
            Type t when t == typeof(int) => "INTEGER",
            Type t when t == typeof(long) => "BIGINT",
            
            // Floating point types
            Type t when t == typeof(float) => "REAL",
            Type t when t == typeof(double) => "DOUBLE PRECISION",
            Type t when t == typeof(decimal) => precision.HasValue && scale.HasValue 
                ? $"NUMERIC({precision},{scale})" 
                : precision.HasValue 
                    ? $"NUMERIC({precision})"
                    : "NUMERIC",
            
            // Boolean
            Type t when t == typeof(bool) => "BOOLEAN",
            
            // Date/Time types
            Type t when t == typeof(DateTime) => "TIMESTAMP",
            Type t when t == typeof(DateTimeOffset) => "TIMESTAMP WITH TIME ZONE",
            Type t when t == typeof(TimeSpan) => "INTERVAL",
            Type t when t == typeof(DateOnly) => "DATE",
            Type t when t == typeof(TimeOnly) => "TIME",
            
            // String types
            Type t when t == typeof(string) => length.HasValue 
                ? $"VARCHAR({length})" 
                : "TEXT",
            Type t when t == typeof(char) => "CHAR(1)",
            
            // GUID
            Type t when t == typeof(Guid) => "UUID",
            
            // Binary data
            Type t when t == typeof(byte[]) => "BYTEA",
            
            // JSON
            Type t when t.Name == "JsonDocument" => "JSONB",
            
            // Default
            _ => "TEXT"
        };
    }

    /// <inheritdoc />
    public string GetFullTextSearchSql(string tableName, IEnumerable<string> columnNames)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));

        if (columnNames == null || !columnNames.Any())
            throw new ArgumentException("Column names cannot be null or empty.", nameof(columnNames));

        var columns = string.Join(" || ' ' || ", columnNames.Select(c => EscapeIdentifier(c)));
        return $"CREATE INDEX idx_fts_{tableName} ON {EscapeIdentifier(tableName)} USING GIN (to_tsvector('english', {columns}))";
    }

    /// <summary>
    /// Gets the SQL for full-text search using PostgreSQL's tsquery.
    /// </summary>
    /// <param name="columnName">The column name to search.</param>
    /// <param name="searchTerm">The search term.</param>
    /// <returns>The full-text search SQL.</returns>
    public string GetFullTextSearchSql(string columnName, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException("Search term cannot be null or empty.", nameof(searchTerm));

        return $"to_tsvector('english', {EscapeIdentifier(columnName)}) @@ plainto_tsquery('english', @searchTerm)";
    }

    /// <summary>
    /// Gets the SQL for JSON path query.
    /// </summary>
    /// <param name="columnName">The JSON column name.</param>
    /// <param name="path">The JSON path.</param>
    /// <returns>The JSON path query SQL.</returns>
    public string GetJsonPathSql(string columnName, string path)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        // PostgreSQL uses -> for JSON object access and ->> for text extraction
        return $"{EscapeIdentifier(columnName)}->'{path}'";
    }

    /// <summary>
    /// Gets the SQL for array containment check.
    /// </summary>
    /// <param name="columnName">The array column name.</param>
    /// <returns>The array containment SQL.</returns>
    public string GetArrayContainsSql(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

        return $"{EscapeIdentifier(columnName)} @> @value::jsonb";
    }

    /// <summary>
    /// Gets the SQL for creating a GIN index for full-text search.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="columnName">The column name.</param>
    /// <param name="indexName">The index name.</param>
    /// <returns>The CREATE INDEX SQL.</returns>
    public string GetCreateFullTextIndexSql(string tableName, string columnName, string indexName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));

        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

        if (string.IsNullOrWhiteSpace(indexName))
            throw new ArgumentException("Index name cannot be null or empty.", nameof(indexName));

        return $"CREATE INDEX {EscapeIdentifier(indexName)} ON {EscapeIdentifier(tableName)} USING GIN (to_tsvector('english', {EscapeIdentifier(columnName)}))";
    }

    /// <summary>
    /// Gets the SQL for UPSERT (INSERT ... ON CONFLICT) operation.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="columns">The column names.</param>
    /// <param name="conflictColumn">The conflict column (usually primary key).</param>
    /// <param name="updateColumns">The columns to update on conflict.</param>
    /// <returns>The UPSERT SQL.</returns>
    public string GetUpsertSql(string tableName, IEnumerable<string> columns, string conflictColumn, IEnumerable<string> updateColumns)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));

        var columnList = string.Join(", ", columns.Select(EscapeIdentifier));
        var valueList = string.Join(", ", columns.Select(c => $"@{c}"));
        var updateList = string.Join(", ", updateColumns.Select(c => $"{EscapeIdentifier(c)} = EXCLUDED.{EscapeIdentifier(c)}"));

        return $@"INSERT INTO {EscapeIdentifier(tableName)} ({columnList}) 
VALUES ({valueList})
ON CONFLICT ({EscapeIdentifier(conflictColumn)}) 
DO UPDATE SET {updateList}";
    }
}

