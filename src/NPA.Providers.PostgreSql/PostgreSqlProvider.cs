using System.Data;
using NPA.Core.Annotations;
using NPA.Core.Metadata;
using NPA.Core.Providers;

namespace NPA.Providers.PostgreSql;

/// <summary>
/// PostgreSQL-specific database provider implementation.
/// </summary>
public class PostgreSqlProvider : IDatabaseProvider
{
    private readonly ISqlDialect _dialect;
    private readonly ITypeConverter _typeConverter;
    private readonly IBulkOperationProvider _bulkOperationProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlProvider"/> class.
    /// </summary>
    public PostgreSqlProvider()
    {
        _dialect = new PostgreSqlDialect();
        _typeConverter = new PostgreSqlTypeConverter();
        _bulkOperationProvider = new PostgreSqlBulkOperationProvider(_dialect, _typeConverter);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlProvider"/> class with custom dependencies.
    /// </summary>
    /// <param name="dialect">The SQL dialect.</param>
    /// <param name="typeConverter">The type converter.</param>
    /// <param name="bulkOperationProvider">The bulk operation provider.</param>
    public PostgreSqlProvider(ISqlDialect dialect, ITypeConverter typeConverter, IBulkOperationProvider bulkOperationProvider)
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

        // Check if we have an identity/serial column to return the generated ID
        var identityColumn = metadata.Properties.Values
            .FirstOrDefault(p => p.IsPrimaryKey && p.GenerationType == GenerationType.Identity);

        if (identityColumn != null)
        {
            // PostgreSQL uses RETURNING clause to return generated IDs
            var primaryKeyColumn = ResolveColumnName(identityColumn);
            return $"INSERT INTO {tableName} ({columnList}) VALUES ({parameterList}) RETURNING {primaryKeyColumn};";
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
            .Select(p => ResolveColumnName(p))
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
            .Select(p => ResolveColumnName(p))
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
    public async Task<int> BulkUpdateAsync<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata, CancellationToken cancellationToken = default)
    {
        return await _bulkOperationProvider.BulkUpdateAsync(connection, entities, metadata, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> BulkDeleteAsync(IDbConnection connection, IEnumerable<object> ids, EntityMetadata metadata, CancellationToken cancellationToken = default)
    {
        return await _bulkOperationProvider.BulkDeleteAsync(connection, ids, metadata, cancellationToken);
    }

    private string GetPropertyNameFromColumn(EntityMetadata metadata, string columnName)
    {
        var property = metadata.Properties.Values
            .FirstOrDefault(p => _dialect.EscapeIdentifier(p.ColumnName) == columnName);
        
        return property?.PropertyName ?? columnName.Trim('"');
    }
}
