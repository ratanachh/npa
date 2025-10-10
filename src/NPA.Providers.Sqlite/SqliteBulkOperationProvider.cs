using System.Data;
using System.Reflection;
using Dapper;
using NPA.Core.Annotations;
using NPA.Core.Metadata;
using NPA.Core.Providers;

namespace NPA.Providers.Sqlite;

/// <summary>
/// SQLite-specific bulk operation provider.
/// </summary>
public class SqliteBulkOperationProvider : IBulkOperationProvider
{
    private readonly ISqlDialect _dialect;
    private readonly ITypeConverter _typeConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteBulkOperationProvider"/> class.
    /// </summary>
    /// <param name="dialect">The SQL dialect.</param>
    /// <param name="typeConverter">The type converter.</param>
    public SqliteBulkOperationProvider(ISqlDialect dialect, ITypeConverter typeConverter)
    {
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        _typeConverter = typeConverter ?? throw new ArgumentNullException(nameof(typeConverter));
    }

    /// <inheritdoc />
    public int MaxBatchSize => 500; // SQLite recommended batch size (conservative due to parameter limits)

    /// <inheritdoc />
    public bool SupportsTableValuedParameters => false; // SQLite doesn't support TVPs

    /// <inheritdoc />
    public async Task<int> BulkInsertAsync<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        var entityList = entities.ToList();
        if (!entityList.Any())
            return 0;

        // SQLite bulk insert strategy: Use multi-row INSERT within transactions
        var tableName = GetUnescapedTableName(metadata);
        var columns = metadata.Properties.Values
            .Where(p => !p.IsPrimaryKey || p.GenerationType != GenerationType.Identity)
            .ToList();

        var columnNames = string.Join(", ", columns.Select(p => _dialect.EscapeIdentifier(p.ColumnName)));
        
        var totalInserted = 0;
        var batches = entityList.Chunk(MaxBatchSize);

        foreach (var batch in batches)
        {
            var valueLists = new List<string>();
            var parameters = new DynamicParameters();
            var paramIndex = 0;

            var entityType = typeof(T);
            foreach (var entity in batch)
            {
                var valueParams = new List<string>();
                foreach (var property in columns)
                {
                    var paramName = $"p{paramIndex}";
                    var propertyInfo = entityType.GetProperty(property.PropertyName);
                    var value = propertyInfo?.GetValue(entity);
                    var convertedValue = _typeConverter.ConvertToDatabase(value, property.PropertyType);
                    parameters.Add(paramName, convertedValue);
                    valueParams.Add($"@{paramName}");
                    paramIndex++;
                }
                valueLists.Add($"({string.Join(", ", valueParams)})");
            }

            var sql = $"INSERT INTO {_dialect.EscapeIdentifier(tableName)} ({columnNames}) VALUES {string.Join(", ", valueLists)}";
            var affected = await connection.ExecuteAsync(sql, parameters);
            totalInserted += affected;
        }

        return totalInserted;
    }

    /// <inheritdoc />
    public async Task<int> BulkUpdateAsync<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        var entityList = entities.ToList();
        if (!entityList.Any())
            return 0;

        // SQLite bulk update strategy: Execute individual UPDATE statements in a transaction
        var tableName = GetUnescapedTableName(metadata);
        var setColumns = metadata.Properties.Values
            .Where(p => !p.IsPrimaryKey)
            .ToList();
        var keyColumns = metadata.Properties.Values
            .Where(p => p.IsPrimaryKey)
            .ToList();

        var setClause = string.Join(", ", setColumns.Select(p => $"{_dialect.EscapeIdentifier(p.ColumnName)} = @{p.PropertyName}"));
        var whereClause = string.Join(" AND ", keyColumns.Select(p => $"{_dialect.EscapeIdentifier(p.ColumnName)} = @{p.PropertyName}"));

        var sql = $"UPDATE {_dialect.EscapeIdentifier(tableName)} SET {setClause} WHERE {whereClause}";

        var totalUpdated = 0;
        var batches = entityList.Chunk(MaxBatchSize);

        foreach (var batch in batches)
        {
            var affected = await connection.ExecuteAsync(sql, batch);
            totalUpdated += affected;
        }

        return totalUpdated;
    }

    /// <inheritdoc />
    public async Task<int> BulkDeleteAsync(IDbConnection connection, IEnumerable<object> ids, EntityMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));
        if (ids == null)
            throw new ArgumentNullException(nameof(ids));
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        var idList = ids.ToList();
        if (!idList.Any())
            return 0;

        // SQLite bulk delete strategy: Use IN clause
        var tableName = GetUnescapedTableName(metadata);
        var primaryKeyColumn = metadata.Properties[metadata.PrimaryKeyProperty];
        var columnName = _dialect.EscapeIdentifier(primaryKeyColumn.ColumnName);

        var totalDeleted = 0;
        var batches = idList.Chunk(MaxBatchSize);

        foreach (var batch in batches)
        {
            var parameters = new DynamicParameters();
            var paramNames = new List<string>();
            var paramIndex = 0;

            foreach (var id in batch)
            {
                var paramName = $"p{paramIndex}";
                parameters.Add(paramName, id);
                paramNames.Add($"@{paramName}");
                paramIndex++;
            }

            var sql = $"DELETE FROM {_dialect.EscapeIdentifier(tableName)} WHERE {columnName} IN ({string.Join(", ", paramNames)})";
            var affected = await connection.ExecuteAsync(sql, parameters);
            totalDeleted += affected;
        }

        return totalDeleted;
    }

    /// <inheritdoc />
    public int BulkInsert<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata)
    {
        return BulkInsertAsync<T>(connection, entities, metadata).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public int BulkUpdate<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata)
    {
        return BulkUpdateAsync<T>(connection, entities, metadata).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public int BulkDelete(IDbConnection connection, IEnumerable<object> ids, EntityMetadata metadata)
    {
        return BulkDeleteAsync(connection, ids, metadata).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public object CreateTableValuedParameter<T>(IEnumerable<T> entities, EntityMetadata metadata, string typeName)
    {
        // SQLite doesn't support table-valued parameters
        throw new NotSupportedException("SQLite does not support table-valued parameters. Use BulkInsertAsync instead.");
    }

    private string GetUnescapedTableName(EntityMetadata metadata)
    {
        // Return the table name without schema prefix (SQLite doesn't support schemas)
        return metadata.TableName;
    }
}


