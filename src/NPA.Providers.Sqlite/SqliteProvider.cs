using System.Data;
using NPA.Core.Annotations;
using NPA.Core.Metadata;
using NPA.Core.Providers;

namespace NPA.Providers.Sqlite;

/// <summary>
/// SQLite-specific database provider implementation.
/// </summary>
public class SqliteProvider : IDatabaseProvider
{
    private readonly ISqlDialect _dialect;
    private readonly ITypeConverter _typeConverter;
    private readonly IBulkOperationProvider _bulkOperationProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteProvider"/> class.
    /// </summary>
    public SqliteProvider()
    {
        _dialect = new SqliteDialect();
        _typeConverter = new SqliteTypeConverter();
        _bulkOperationProvider = new SqliteBulkOperationProvider(_dialect, _typeConverter);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteProvider"/> class with custom dependencies.
    /// </summary>
    /// <param name="dialect">The SQL dialect.</param>
    /// <param name="typeConverter">The type converter.</param>
    /// <param name="bulkOperationProvider">The bulk operation provider.</param>
    public SqliteProvider(ISqlDialect dialect, ITypeConverter typeConverter, IBulkOperationProvider bulkOperationProvider)
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
            .ToList();

        if (!columns.Any())
            throw new InvalidOperationException($"No insertable columns found for entity {metadata.EntityType.Name}");

        var columnList = string.Join(", ", columns.Select(p => _dialect.EscapeIdentifier(p.ColumnName)));
        var parameterList = string.Join(", ", columns.Select(p => GetParameterPlaceholder(p.PropertyName)));

        // Check if we have an auto increment column to return the generated ID
        var autoIncrementColumn = metadata.Properties.Values
            .FirstOrDefault(p => p.IsPrimaryKey && p.GenerationType == GenerationType.Identity);

        if (autoIncrementColumn != null)
        {
            // SQLite uses last_insert_rowid() to return generated IDs
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
    public string GenerateSelectByIdSql(EntityMetadata metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        var tableName = ResolveTableName(metadata);
        var primaryKey = metadata.Properties.Values.FirstOrDefault(p => p.IsPrimaryKey)
            ?? throw new InvalidOperationException($"No primary key found for entity {metadata.EntityType.Name}");

        var columns = metadata.Properties.Values
            .Select(p => $"{ResolveColumnName(p)} AS {_dialect.EscapeIdentifier(p.PropertyName)}")
            .ToList();

        var columnList = string.Join(", ", columns);
        var primaryKeyColumn = ResolveColumnName(primaryKey);

        return $"SELECT {columnList} FROM {tableName} WHERE {primaryKeyColumn} = {GetParameterPlaceholder("id")};";
    }

    /// <inheritdoc />
    public string GenerateSelectSql(EntityMetadata metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        var tableName = ResolveTableName(metadata);
        var columns = metadata.Properties.Values
            .Select(p => $"{ResolveColumnName(p)} AS {_dialect.EscapeIdentifier(p.PropertyName)}")
            .ToList();

        var columnList = string.Join(", ", columns);

        return $"SELECT {columnList} FROM {tableName};";
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

        // SQLite doesn't support schemas, so just use table name
        return _dialect.EscapeIdentifier(metadata.TableName);
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

    /// <inheritdoc />
    public ISqlDialect Dialect => _dialect;

    /// <inheritdoc />
    public ITypeConverter TypeConverter => _typeConverter;

    /// <inheritdoc />
    public IBulkOperationProvider BulkOperationProvider => _bulkOperationProvider;
}

