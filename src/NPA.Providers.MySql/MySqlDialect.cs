using NPA.Core.Annotations;
using NPA.Core.Metadata;
using NPA.Core.Providers;

namespace NPA.Providers.MySql;

/// <summary>
/// MySQL/MariaDB-specific dialect implementation.
/// </summary>
public class MySqlDialect : ISqlDialect
{
    /// <inheritdoc />
    public string GetLastInsertedIdSql()
    {
        return "SELECT LAST_INSERT_ID()";
    }

    /// <inheritdoc />
    public string GetNextSequenceValueSql(string sequenceName)
    {
        if (string.IsNullOrWhiteSpace(sequenceName))
            throw new ArgumentException("Sequence name cannot be null or empty.", nameof(sequenceName));

        // MySQL 8.0+ supports sequences
        return $"SELECT NEXTVAL({EscapeIdentifier(sequenceName)})";
    }

    /// <inheritdoc />
    public string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier cannot be null or empty.", nameof(identifier));

        // MySQL uses backticks for escaping identifiers
        return $"`{identifier.Replace("`", "``")}`";
    }

    /// <inheritdoc />
    public string GetCreateTableValuedParameterTypeSql(string typeName, IEnumerable<string> columnDefinitions)
    {
        // MySQL doesn't have table-valued parameters like SQL Server
        // This would be used for temporary tables instead
        throw new NotSupportedException("MySQL does not support table-valued parameters. Use temporary tables instead.");
    }

    /// <inheritdoc />
    public string GetTableExistsSql(string tableName, string? schemaName = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));

        // MySQL uses INFORMATION_SCHEMA to check table existence
        // Schema in MySQL is the database name
        if (!string.IsNullOrWhiteSpace(schemaName))
        {
            return @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = @SchemaName AND TABLE_NAME = @TableName";
        }

        return @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @TableName";
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

        // MySQL uses LIMIT offset, count syntax
        return $"{sql} LIMIT {offset}, {limit}";
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
            Type t when t == typeof(byte) => "TINYINT UNSIGNED",
            Type t when t == typeof(sbyte) => "TINYINT",
            Type t when t == typeof(short) => "SMALLINT",
            Type t when t == typeof(ushort) => "SMALLINT UNSIGNED",
            Type t when t == typeof(int) => "INT",
            Type t when t == typeof(uint) => "INT UNSIGNED",
            Type t when t == typeof(long) => "BIGINT",
            Type t when t == typeof(ulong) => "BIGINT UNSIGNED",
            
            // Floating point types
            Type t when t == typeof(float) => "FLOAT",
            Type t when t == typeof(double) => "DOUBLE",
            Type t when t == typeof(decimal) => precision.HasValue && scale.HasValue 
                ? $"DECIMAL({precision},{scale})" 
                : precision.HasValue 
                    ? $"DECIMAL({precision},0)" 
                    : "DECIMAL(18,2)",
            
            // Boolean - MySQL uses TINYINT(1)
            Type t when t == typeof(bool) => "TINYINT(1)",
            
            // Date/Time types
            Type t when t == typeof(DateTime) => "DATETIME",
            Type t when t == typeof(DateTimeOffset) => "DATETIME", // Store as UTC
            Type t when t == typeof(TimeSpan) => "TIME",
            Type t when t == typeof(DateOnly) => "DATE",
            Type t when t == typeof(TimeOnly) => "TIME",
            
            // String types
            Type t when t == typeof(string) => length.HasValue 
                ? length.Value == -1 
                    ? "TEXT" 
                    : $"VARCHAR({length})"
                : "VARCHAR(255)",
            Type t when t == typeof(char) => "CHAR(1)",
            
            // GUID - MySQL stores as CHAR(36) or BINARY(16)
            Type t when t == typeof(Guid) => "CHAR(36)",
            
            // Binary data
            Type t when t == typeof(byte[]) => length.HasValue 
                ? length.Value == -1 
                    ? "LONGBLOB" 
                    : $"VARBINARY({length})"
                : "BLOB",
            
            // Default
            _ => "VARCHAR(255)"
        };
    }

    /// <inheritdoc />
    public string GetFullTextSearchSql(string tableName, IEnumerable<string> columnNames)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));

        var columns = columnNames?.ToList() ?? throw new ArgumentNullException(nameof(columnNames));
        if (!columns.Any())
            throw new ArgumentException("At least one column name must be provided.", nameof(columnNames));

        var columnList = string.Join(", ", columns.Select(EscapeIdentifier));
        var indexName = $"FT_IDX_{tableName}";

        return $"CREATE FULLTEXT INDEX {EscapeIdentifier(indexName)} ON {EscapeIdentifier(tableName)} ({columnList})";
    }

    /// <summary>
    /// Gets the SQL for a MATCH AGAINST full-text search query.
    /// </summary>
    /// <param name="columnNames">The column names to search.</param>
    /// <param name="searchMode">The search mode (IN NATURAL LANGUAGE MODE, IN BOOLEAN MODE, etc.).</param>
    /// <returns>The MATCH AGAINST SQL expression.</returns>
    public string GetMatchAgainstSql(IEnumerable<string> columnNames, string searchMode = "IN NATURAL LANGUAGE MODE")
    {
        var columns = columnNames?.ToList() ?? throw new ArgumentNullException(nameof(columnNames));
        if (!columns.Any())
            throw new ArgumentException("At least one column name must be provided.", nameof(columnNames));

        var columnList = string.Join(", ", columns.Select(EscapeIdentifier));
        return $"MATCH({columnList}) AGAINST(@searchTerm {searchMode})";
    }

    /// <summary>
    /// Gets the SQL for JSON operations.
    /// </summary>
    /// <param name="columnName">The column name containing JSON data.</param>
    /// <param name="jsonPath">The JSON path expression.</param>
    /// <returns>The JSON SQL expression.</returns>
    public string GetJsonExtractSql(string columnName, string jsonPath)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

        if (string.IsNullOrWhiteSpace(jsonPath))
            throw new ArgumentException("JSON path cannot be null or empty.", nameof(jsonPath));

        return $"JSON_EXTRACT({EscapeIdentifier(columnName)}, '{jsonPath}')";
    }

    /// <summary>
    /// Gets the SQL for checking if JSON is valid.
    /// </summary>
    /// <param name="columnName">The column name containing JSON data.</param>
    /// <returns>The JSON validation SQL expression.</returns>
    public string GetJsonValidSql(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

        return $"JSON_VALID({EscapeIdentifier(columnName)})";
    }

    /// <summary>
    /// Gets the SQL for creating an auto increment column.
    /// </summary>
    /// <param name="columnName">The column name.</param>
    /// <param name="dataType">The data type.</param>
    /// <returns>The auto increment column SQL definition.</returns>
    public string GetAutoIncrementColumnSql(string columnName, string dataType)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

        if (string.IsNullOrWhiteSpace(dataType))
            throw new ArgumentException("Data type cannot be null or empty.", nameof(dataType));

        return $"{EscapeIdentifier(columnName)} {dataType} AUTO_INCREMENT NOT NULL";
    }

    /// <summary>
    /// Gets the SQL for creating a spatial index.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="columnName">The column name with spatial data.</param>
    /// <returns>The spatial index SQL statement.</returns>
    public string GetSpatialIndexSql(string tableName, string columnName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));

        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

        var indexName = $"SP_IDX_{tableName}_{columnName}";
        return $"CREATE SPATIAL INDEX {EscapeIdentifier(indexName)} ON {EscapeIdentifier(tableName)} ({EscapeIdentifier(columnName)})";
    }

    /// <summary>
    /// Gets the SQL for inserting with ON DUPLICATE KEY UPDATE.
    /// </summary>
    /// <param name="metadata">The entity metadata.</param>
    /// <returns>The UPSERT SQL statement.</returns>
    public string GetUpsertSql(EntityMetadata metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        var tableName = metadata.TableName;
        var columns = metadata.Properties.Values
            .Where(p => !p.IsPrimaryKey || p.GenerationType != GenerationType.Identity)
            .ToList();

        var columnNames = columns.Select(p => EscapeIdentifier(p.ColumnName)).ToList();
        var parameters = columns.Select(p => $"@{p.PropertyName}").ToList();
        var updates = columns.Select(p => $"{EscapeIdentifier(p.ColumnName)} = VALUES({EscapeIdentifier(p.ColumnName)})").ToList();

        var columnList = string.Join(", ", columnNames);
        var parameterList = string.Join(", ", parameters);
        var updateList = string.Join(", ", updates);

        return $@"INSERT INTO {EscapeIdentifier(tableName)} ({columnList}) 
VALUES ({parameterList})
ON DUPLICATE KEY UPDATE {updateList}";
    }
}

