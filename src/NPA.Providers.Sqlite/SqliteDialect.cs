using NPA.Core.Providers;

namespace NPA.Providers.Sqlite;

/// <summary>
/// SQLite-specific dialect implementation.
/// </summary>
public class SqliteDialect : ISqlDialect
{
    /// <inheritdoc />
    public string GetLastInsertedIdSql()
    {
        // SQLite uses last_insert_rowid() function
        return "SELECT last_insert_rowid()";
    }

    /// <inheritdoc />
    public string GetNextSequenceValueSql(string sequenceName)
    {
        // SQLite doesn't support sequences natively (uses AUTOINCREMENT)
        throw new NotSupportedException("SQLite does not support sequences. Use AUTOINCREMENT for identity columns instead.");
    }

    /// <inheritdoc />
    public string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier cannot be null or empty.", nameof(identifier));

        // SQLite uses double quotes for escaping identifiers (SQL standard)
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }

    /// <inheritdoc />
    public string GetCreateTableValuedParameterTypeSql(string typeName, IEnumerable<string> columnDefinitions)
    {
        // SQLite doesn't support table-valued parameters
        throw new NotSupportedException("SQLite does not support table-valued parameters.");
    }

    /// <inheritdoc />
    public string GetTableExistsSql(string tableName, string? schemaName = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));

        // SQLite doesn't support schemas (schema parameter is ignored)
        return "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name = @TableName";
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

        // SQLite uses LIMIT/OFFSET
        return $"{sql} LIMIT {limit} OFFSET {offset}";
    }

    /// <inheritdoc />
    public string GetDataTypeMapping(Type dotNetType, int? length = null, int? precision = null, int? scale = null)
    {
        if (dotNetType == null)
            throw new ArgumentNullException(nameof(dotNetType));

        // Handle nullable types
        var actualType = Nullable.GetUnderlyingType(dotNetType) ?? dotNetType;

        // SQLite has a limited type system with type affinity:
        // INTEGER, REAL, TEXT, BLOB, NULL
        return actualType switch
        {
            // Integer types - all map to INTEGER
            Type t when t == typeof(byte) => "INTEGER",
            Type t when t == typeof(short) => "INTEGER",
            Type t when t == typeof(int) => "INTEGER",
            Type t when t == typeof(long) => "INTEGER",
            Type t when t == typeof(bool) => "INTEGER", // 0 or 1
            
            // Floating point types - map to REAL
            Type t when t == typeof(float) => "REAL",
            Type t when t == typeof(double) => "REAL",
            Type t when t == typeof(decimal) => "REAL", // SQLite doesn't have exact decimal
            
            // Date/Time types - stored as TEXT in ISO8601 format
            Type t when t == typeof(DateTime) => "TEXT",
            Type t when t == typeof(DateTimeOffset) => "TEXT",
            Type t when t == typeof(TimeSpan) => "TEXT",
            Type t when t == typeof(DateOnly) => "TEXT",
            Type t when t == typeof(TimeOnly) => "TEXT",
            
            // String types
            Type t when t == typeof(string) => "TEXT",
            Type t when t == typeof(char) => "TEXT",
            
            // GUID - stored as TEXT
            Type t when t == typeof(Guid) => "TEXT",
            
            // Binary data
            Type t when t == typeof(byte[]) => "BLOB",
            
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

        // SQLite uses FTS5 for full-text search (requires extension)
        var columns = string.Join(", ", columnNames.Select(EscapeIdentifier));
        return $"CREATE VIRTUAL TABLE {EscapeIdentifier($"fts_{tableName}")} USING fts5({columns}, content='{EscapeIdentifier(tableName)}')";
    }
}

