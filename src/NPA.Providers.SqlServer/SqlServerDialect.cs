using NPA.Core.Providers;

namespace NPA.Providers.SqlServer;

/// <summary>
/// SQL Server-specific dialect implementation.
/// </summary>
public class SqlServerDialect : ISqlDialect
{
    /// <inheritdoc />
    public string GetLastInsertedIdSql()
    {
        return "SELECT SCOPE_IDENTITY()";
    }

    /// <inheritdoc />
    public string GetNextSequenceValueSql(string sequenceName)
    {
        if (string.IsNullOrWhiteSpace(sequenceName))
            throw new ArgumentException("Sequence name cannot be null or empty.", nameof(sequenceName));

        return $"SELECT NEXT VALUE FOR {EscapeIdentifier(sequenceName)}";
    }

    /// <inheritdoc />
    public string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier cannot be null or empty.", nameof(identifier));

        // SQL Server uses square brackets for escaping identifiers
        return $"[{identifier.Replace("]", "]]")}]";
    }

    /// <inheritdoc />
    public string GetCreateTableValuedParameterTypeSql(string typeName, IEnumerable<string> columnDefinitions)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            throw new ArgumentException("Type name cannot be null or empty.", nameof(typeName));

        var columns = string.Join(",\n    ", columnDefinitions);
        return $@"CREATE TYPE {EscapeIdentifier(typeName)} AS TABLE
(
    {columns}
)";
    }

    /// <inheritdoc />
    public string GetTableExistsSql(string tableName, string? schemaName = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));

        var schema = string.IsNullOrWhiteSpace(schemaName) ? "dbo" : schemaName;
        
        return @"SELECT CASE WHEN EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES 
    WHERE TABLE_SCHEMA = @SchemaName AND TABLE_NAME = @TableName
) THEN 1 ELSE 0 END";
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

        // SQL Server uses OFFSET/FETCH for pagination (requires ORDER BY)
        return $"{sql} OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY";
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
            Type t when t == typeof(byte) => "TINYINT",
            Type t when t == typeof(short) => "SMALLINT",
            Type t when t == typeof(int) => "INT",
            Type t when t == typeof(long) => "BIGINT",
            
            // Floating point types
            Type t when t == typeof(float) => "REAL",
            Type t when t == typeof(double) => "FLOAT",
            Type t when t == typeof(decimal) => precision.HasValue && scale.HasValue 
                ? $"DECIMAL({precision},{scale})" 
                : precision.HasValue 
                    ? $"DECIMAL({precision},0)" 
                    : "DECIMAL(18,2)",
            
            // Boolean
            Type t when t == typeof(bool) => "BIT",
            
            // Date/Time types
            Type t when t == typeof(DateTime) => "DATETIME2",
            Type t when t == typeof(DateTimeOffset) => "DATETIMEOFFSET",
            Type t when t == typeof(TimeSpan) => "TIME",
            Type t when t == typeof(DateOnly) => "DATE",
            Type t when t == typeof(TimeOnly) => "TIME",
            
            // String types
            Type t when t == typeof(string) => length.HasValue 
                ? length.Value == -1 
                    ? "NVARCHAR(MAX)" 
                    : $"NVARCHAR({length})"
                : "NVARCHAR(255)",
            Type t when t == typeof(char) => "NCHAR(1)",
            
            // GUID
            Type t when t == typeof(Guid) => "UNIQUEIDENTIFIER",
            
            // Binary data
            Type t when t == typeof(byte[]) => length.HasValue 
                ? length.Value == -1 
                    ? "VARBINARY(MAX)" 
                    : $"VARBINARY({length})"
                : "VARBINARY(MAX)",
            
            // SQL Server specific types
            Type t when t.Name == "SqlGeography" => "GEOGRAPHY",
            Type t when t.Name == "SqlGeometry" => "GEOMETRY",
            Type t when t.Name == "SqlHierarchyId" => "HIERARCHYID",
            
            // Default
            _ => "NVARCHAR(255)"
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

        return $@"CREATE FULLTEXT INDEX ON {EscapeIdentifier(tableName)}
(
    {columnList}
)
KEY INDEX PK_{tableName}";
    }

    /// <summary>
    /// Gets the SQL for a CONTAINS full-text search query.
    /// </summary>
    /// <param name="columnName">The column name to search.</param>
    /// <param name="searchTerm">The search term.</param>
    /// <returns>The CONTAINS SQL expression.</returns>
    public string GetContainsSql(string columnName, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException("Search term cannot be null or empty.", nameof(searchTerm));

        return $"CONTAINS({EscapeIdentifier(columnName)}, @searchTerm)";
    }

    /// <summary>
    /// Gets the SQL for a FREETEXT full-text search query.
    /// </summary>
    /// <param name="columnName">The column name to search.</param>
    /// <param name="searchTerm">The search term.</param>
    /// <returns>The FREETEXT SQL expression.</returns>
    public string GetFreeTextSql(string columnName, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException("Search term cannot be null or empty.", nameof(searchTerm));

        return $"FREETEXT({EscapeIdentifier(columnName)}, @searchTerm)";
    }

    /// <summary>
    /// Gets the SQL for creating an identity column.
    /// </summary>
    /// <param name="columnName">The column name.</param>
    /// <param name="dataType">The data type.</param>
    /// <param name="seed">The identity seed value.</param>
    /// <param name="increment">The identity increment value.</param>
    /// <returns>The identity column SQL definition.</returns>
    public string GetIdentityColumnSql(string columnName, string dataType, int seed = 1, int increment = 1)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

        if (string.IsNullOrWhiteSpace(dataType))
            throw new ArgumentException("Data type cannot be null or empty.", nameof(dataType));

        return $"{EscapeIdentifier(columnName)} {dataType} IDENTITY({seed},{increment}) NOT NULL";
    }

    /// <summary>
    /// Gets the SQL for JSON operations.
    /// </summary>
    /// <param name="columnName">The column name containing JSON data.</param>
    /// <param name="jsonPath">The JSON path expression.</param>
    /// <returns>The JSON SQL expression.</returns>
    public string GetJsonValueSql(string columnName, string jsonPath)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

        if (string.IsNullOrWhiteSpace(jsonPath))
            throw new ArgumentException("JSON path cannot be null or empty.", nameof(jsonPath));

        return $"JSON_VALUE({EscapeIdentifier(columnName)}, '{jsonPath}')";
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

        return $"ISJSON({EscapeIdentifier(columnName)}) = 1";
    }
}