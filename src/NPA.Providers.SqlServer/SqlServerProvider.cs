using System.Data;
using NPA.Core.Annotations;
using NPA.Core.Metadata;
using NPA.Core.Providers;

namespace NPA.Providers.SqlServer;

/// <summary>
/// SQL Server-specific database provider implementation.
/// </summary>
public class SqlServerProvider : IDatabaseProvider
{
    private readonly ISqlDialect _dialect;
    private readonly ITypeConverter _typeConverter;
    private readonly IBulkOperationProvider _bulkOperationProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerProvider"/> class.
    /// </summary>
    public SqlServerProvider()
    {
        _dialect = new SqlServerDialect();
        _typeConverter = new SqlServerTypeConverter();
        _bulkOperationProvider = new SqlServerBulkOperationProvider(_dialect, _typeConverter);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerProvider"/> class with custom dependencies.
    /// </summary>
    /// <param name="dialect">The SQL dialect.</param>
    /// <param name="typeConverter">The type converter.</param>
    /// <param name="bulkOperationProvider">The bulk operation provider.</param>
    public SqlServerProvider(ISqlDialect dialect, ITypeConverter typeConverter, IBulkOperationProvider bulkOperationProvider)
    {
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        _typeConverter = typeConverter ?? throw new ArgumentNullException(nameof(typeConverter));
        _bulkOperationProvider = bulkOperationProvider ?? throw new ArgumentNullException(nameof(bulkOperationProvider));
    }

    /// <inheritdoc />
    public string GenerateInsertSql(EntityMetadata metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        var tableName = ResolveTableName(metadata);
        var columns = metadata.Properties.Values
            .Where(p => !p.IsPrimaryKey || p.GenerationType != GenerationType.Identity)
            .Select(p => ResolveColumnName(p))
            .ToList();

        if (!columns.Any())
            throw new InvalidOperationException($"No columns found for INSERT operation on table {tableName}");

        var columnList = string.Join(", ", columns);
        var parameterList = string.Join(", ", columns.Select(c => GetParameterPlaceholder(GetPropertyNameFromColumn(metadata, c))));

        // Check if we have an identity column to return the generated ID
        var identityColumn = metadata.Properties.Values
            .FirstOrDefault(p => p.IsPrimaryKey && p.GenerationType == GenerationType.Identity);

        if (identityColumn != null)
        {
            // SQL Server uses OUTPUT clause or SCOPE_IDENTITY() to return generated IDs
            return $"INSERT INTO {tableName} ({columnList}) VALUES ({parameterList}); {_dialect.GetLastInsertedIdSql()};";
        }

        return $"INSERT INTO {tableName} ({columnList}) VALUES ({parameterList});";
    }

    /// <inheritdoc />
    public string GenerateUpdateSql(EntityMetadata metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        var tableName = ResolveTableName(metadata);
        var primaryKey = metadata.Properties.Values.FirstOrDefault(p => p.IsPrimaryKey)
            ?? throw new InvalidOperationException($"No primary key found for entity {metadata.EntityType.Name}");

        var primaryKeyColumn = ResolveColumnName(primaryKey);

        var setClauses = metadata.Properties.Values
            .Where(p => !p.IsPrimaryKey)
            .Select(p => $"{ResolveColumnName(p)} = {GetParameterPlaceholder(p.PropertyName)}")
            .ToList();

        if (!setClauses.Any())
            throw new InvalidOperationException($"No columns found for UPDATE operation on table {tableName}");

        var setClause = string.Join(", ", setClauses);

        return $"UPDATE {tableName} SET {setClause} WHERE {primaryKeyColumn} = {GetParameterPlaceholder(primaryKey.PropertyName)};";
    }

    /// <inheritdoc />
    public string GenerateDeleteSql(EntityMetadata metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        var tableName = ResolveTableName(metadata);
        var primaryKey = metadata.Properties.Values.FirstOrDefault(p => p.IsPrimaryKey)
            ?? throw new InvalidOperationException($"No primary key found for entity {metadata.EntityType.Name}");

        var primaryKeyColumn = ResolveColumnName(primaryKey);

        return $"DELETE FROM {tableName} WHERE {primaryKeyColumn} = {GetParameterPlaceholder("id")};";
    }

    /// <inheritdoc />
    public string GenerateSelectSql(EntityMetadata metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        var tableName = ResolveTableName(metadata);
        var columns = metadata.Properties.Values
            .Select(p => $"{ResolveColumnName(p)} AS {p.PropertyName}")
            .ToList();

        var columnList = string.Join(", ", columns);

        return $"SELECT {columnList} FROM {tableName};";
    }

    /// <inheritdoc />
    public string GenerateSelectByIdSql(EntityMetadata metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        var tableName = ResolveTableName(metadata);
        var primaryKey = metadata.Properties.Values.FirstOrDefault(p => p.IsPrimaryKey)
            ?? throw new InvalidOperationException($"No primary key found for entity {metadata.EntityType.Name}");

        var columns = metadata.Properties.Values
            .Select(p => $"{ResolveColumnName(p)} AS {p.PropertyName}")
            .ToList();

        var columnList = string.Join(", ", columns);
        var primaryKeyColumn = ResolveColumnName(primaryKey);

        return $"SELECT {columnList} FROM {tableName} WHERE {primaryKeyColumn} = {GetParameterPlaceholder("id")};";
    }

    /// <inheritdoc />
    public string GenerateCountSql(EntityMetadata metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        var tableName = ResolveTableName(metadata);

        return $"SELECT COUNT(*) FROM {tableName};";
    }

    /// <inheritdoc />
    public string ResolveTableName(EntityMetadata metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        var tableName = _dialect.EscapeIdentifier(metadata.TableName);

        if (!string.IsNullOrWhiteSpace(metadata.SchemaName))
        {
            var schemaName = _dialect.EscapeIdentifier(metadata.SchemaName);
            return $"{schemaName}.{tableName}";
        }

        return tableName;
    }

    /// <inheritdoc />
    public string ResolveColumnName(PropertyMetadata property)
    {
        if (property == null)
            throw new ArgumentNullException(nameof(property));

        return _dialect.EscapeIdentifier(property.ColumnName);
    }

    /// <inheritdoc />
    public string GetParameterPlaceholder(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
            throw new ArgumentException("Parameter name cannot be null or empty.", nameof(parameterName));

        return $"@{parameterName}";
    }

    /// <inheritdoc />
    public object? ConvertParameterValue(object? value, Type targetType)
    {
        return _typeConverter.ConvertToDatabase(value, targetType);
    }

    /// <inheritdoc />
    public async Task<int> BulkInsertAsync<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata, CancellationToken cancellationToken = default)
    {
        return await _bulkOperationProvider.BulkInsertAsync(connection, entities, metadata, cancellationToken);
    }

    /// <inheritdoc />
    public int BulkInsert<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata)
    {
        return _bulkOperationProvider.BulkInsert(connection, entities, metadata);
    }

    /// <inheritdoc />
    public async Task<int> BulkUpdateAsync<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata, CancellationToken cancellationToken = default)
    {
        return await _bulkOperationProvider.BulkUpdateAsync(connection, entities, metadata, cancellationToken);
    }

    /// <inheritdoc />
    public int BulkUpdate<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata)
    {
        return _bulkOperationProvider.BulkUpdate(connection, entities, metadata);
    }

    /// <inheritdoc />
    public async Task<int> BulkDeleteAsync(IDbConnection connection, IEnumerable<object> ids, EntityMetadata metadata, CancellationToken cancellationToken = default)
    {
        return await _bulkOperationProvider.BulkDeleteAsync(connection, ids, metadata, cancellationToken);
    }

    /// <inheritdoc />
    public int BulkDelete(IDbConnection connection, IEnumerable<object> ids, EntityMetadata metadata)
    {
        return _bulkOperationProvider.BulkDelete(connection, ids, metadata);
    }

    /// <summary>
    /// Generates a SELECT SQL statement with WHERE conditions.
    /// </summary>
    /// <param name="metadata">The entity metadata.</param>
    /// <param name="whereConditions">The WHERE conditions.</param>
    /// <returns>The generated SELECT SQL statement.</returns>
    public string GenerateSelectWithWhereSql(EntityMetadata metadata, string whereConditions)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        var baseSql = GenerateSelectSql(metadata);
        
        if (!string.IsNullOrWhiteSpace(whereConditions))
        {
            baseSql = baseSql.TrimEnd(';');
            return $"{baseSql} WHERE {whereConditions};";
        }

        return baseSql;
    }

    /// <summary>
    /// Generates a SELECT SQL statement with pagination.
    /// </summary>
    /// <param name="metadata">The entity metadata.</param>
    /// <param name="orderByColumn">The column to order by.</param>
    /// <param name="offset">The number of rows to skip.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <returns>The generated SELECT SQL statement with pagination.</returns>
    public string GenerateSelectWithPaginationSql(EntityMetadata metadata, string orderByColumn, int offset, int limit)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        if (string.IsNullOrWhiteSpace(orderByColumn))
            throw new ArgumentException("Order by column cannot be null or empty.", nameof(orderByColumn));

        var tableName = ResolveTableName(metadata);
        var columns = metadata.Properties.Values
            .Select(p => ResolveColumnName(p))
            .ToList();

        var columnList = string.Join(", ", columns);
        var escapedOrderColumn = _dialect.EscapeIdentifier(orderByColumn);

        var baseSql = $"SELECT {columnList} FROM {tableName} ORDER BY {escapedOrderColumn}";
        return _dialect.GetPaginationSql(baseSql, offset, limit);
    }

    /// <summary>
    /// Generates SQL for creating a table based on entity metadata.
    /// </summary>
    /// <param name="metadata">The entity metadata.</param>
    /// <returns>The CREATE TABLE SQL statement.</returns>
    public string GenerateCreateTableSql(EntityMetadata metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        var tableName = ResolveTableName(metadata);
        var columnDefinitions = new List<string>();

        foreach (var property in metadata.Properties.Values)
        {
            var columnDef = GenerateColumnDefinition(property);
            columnDefinitions.Add(columnDef);
        }

        // Add primary key constraint
        var primaryKeyProperties = metadata.Properties.Values.Where(p => p.IsPrimaryKey).ToList();
        if (primaryKeyProperties.Any())
        {
            var pkColumns = primaryKeyProperties.Select(p => ResolveColumnName(p));
            var pkConstraint = $"CONSTRAINT PK_{metadata.TableName} PRIMARY KEY ({string.Join(", ", pkColumns)})";
            columnDefinitions.Add(pkConstraint);
        }

        var columnList = string.Join(",\n    ", columnDefinitions);

        return $@"CREATE TABLE {tableName}
(
    {columnList}
);";
    }

    private string GenerateColumnDefinition(PropertyMetadata property)
    {
        var columnName = ResolveColumnName(property);
        var dataType = _typeConverter.GetDatabaseTypeName(property.PropertyType, property.Length, property.Precision, property.Scale);

        var definition = $"{columnName} {dataType}";

        // Add identity specification
        if (property.IsPrimaryKey && property.GenerationType == GenerationType.Identity)
        {
            definition += " IDENTITY(1,1)";
        }

        // Add null/not null constraint
        if (!property.IsNullable)
        {
            definition += " NOT NULL";
        }

        // Add unique constraint
        if (property.IsUnique)
        {
            definition += " UNIQUE";
        }

        return definition;
    }

    private string GetPropertyNameFromColumn(EntityMetadata metadata, string columnName)
    {
        var property = metadata.Properties.Values
            .FirstOrDefault(p => _dialect.EscapeIdentifier(p.ColumnName) == columnName);
        
        return property?.PropertyName ?? columnName.Trim('[', ']');
    }
}