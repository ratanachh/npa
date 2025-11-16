using System.Data;
using System.Text;
using Dapper;
using NPA.Core.Annotations;
using NPA.Core.Metadata;
using NPA.Core.Providers;
using Npgsql;

namespace NPA.Providers.PostgreSql;

/// <summary>
/// PostgreSQL-specific bulk operation provider using COPY and batch operations.
/// </summary>
public class PostgreSqlBulkOperationProvider : IBulkOperationProvider
{
    private readonly ISqlDialect _dialect;
    private readonly ITypeConverter _typeConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlBulkOperationProvider"/> class.
    /// </summary>
    /// <param name="dialect">The SQL dialect.</param>
    /// <param name="typeConverter">The type converter.</param>
    public PostgreSqlBulkOperationProvider(ISqlDialect dialect, ITypeConverter typeConverter)
    {
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        _typeConverter = typeConverter ?? throw new ArgumentNullException(nameof(typeConverter));
    }

    /// <inheritdoc />
    public int MaxBatchSize => 5000; // PostgreSQL recommended batch size

    /// <inheritdoc />
    public bool SupportsTableValuedParameters => false; // PostgreSQL uses different approach

    /// <inheritdoc />
    public object CreateTableValuedParameter<T>(IEnumerable<T> entities, EntityMetadata metadata, string typeName)
    {
        // PostgreSQL doesn't use table-valued parameters in the same way as SQL Server
        // This method is not applicable for PostgreSQL
        throw new NotSupportedException("PostgreSQL does not support table-valued parameters. Use COPY or temporary tables instead.");
    }

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

        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            // Fallback to batch INSERT if not using NpgsqlConnection
            return await BatchInsertAsync(connection, entityList, metadata, cancellationToken);
        }

        var tableName = GetTableName(metadata);
        var columns = metadata.Properties.Values
            .Where(p => !p.IsPrimaryKey || p.GenerationType != GenerationType.Identity)
            .ToList();

        if (!columns.Any())
            throw new InvalidOperationException($"No columns found for bulk insert operation on table {tableName}");

        try
        {
            // Ensure connection is open
            if (npgsqlConnection.State != ConnectionState.Open)
                await npgsqlConnection.OpenAsync(cancellationToken);

            // Use COPY for high-performance bulk insert
            var columnNames = string.Join(", ", columns.Select(p => _dialect.EscapeIdentifier(p.ColumnName)));
            var copyCommand = $"COPY {tableName} ({columnNames}) FROM STDIN (FORMAT BINARY)";

            using var writer = await npgsqlConnection.BeginBinaryImportAsync(copyCommand, cancellationToken);

            foreach (var entity in entityList)
            {
                if (entity == null) continue;

                await writer.StartRowAsync(cancellationToken);

                foreach (var property in columns)
                {
                    var value = GetPropertyValue(entity, property.PropertyName);
                    var convertedValue = _typeConverter.ConvertToDatabase(value, property.PropertyType);

                    if (convertedValue == null || convertedValue is DBNull)
                    {
                        await writer.WriteNullAsync(cancellationToken);
                    }
                    else
                    {
                        await writer.WriteAsync(convertedValue, cancellationToken);
                    }
                }
            }

            await writer.CompleteAsync(cancellationToken);
            return entityList.Count;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Bulk insert operation failed for table {tableName}: {ex.Message}", ex);
        }
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

        var primaryKey = metadata.Properties.Values.FirstOrDefault(p => p.IsPrimaryKey)
            ?? throw new InvalidOperationException($"No primary key found for entity {metadata.EntityType.Name}");

        // Use temporary table approach for bulk updates
        var tempTableName = $"temp_{metadata.TableName}_{Guid.NewGuid():N}";

        try
        {
            // Ensure we have an NpgsqlConnection for temp table operations
            if (connection is not NpgsqlConnection npgsqlConnection)
                throw new InvalidOperationException("Bulk update requires an NpgsqlConnection");

            // Ensure connection is open
            if (npgsqlConnection.State != ConnectionState.Open)
                await npgsqlConnection.OpenAsync(cancellationToken);

            // Create temporary table
            var createTempTableSql = GenerateCreateTempTableSql(metadata, tempTableName);
            await npgsqlConnection.ExecuteAsync(createTempTableSql);

            // Bulk insert into temp table
            await BulkInsertToTempTableAsync(npgsqlConnection, entityList, metadata, tempTableName, cancellationToken);

            // Perform UPDATE using temp table
            var updateSql = GenerateUpdateFromTempTableSql(metadata, tempTableName, primaryKey);
            var rowsAffected = await npgsqlConnection.ExecuteAsync(updateSql);

            // Drop temporary table (don't escape - safe GUID-based name)
            await npgsqlConnection.ExecuteAsync($"DROP TABLE IF EXISTS {tempTableName}");

            return rowsAffected;
        }
        catch (Exception ex)
        {
            // Clean up temp table on error
            try
            {
                if (connection is NpgsqlConnection npgsqlConnection && npgsqlConnection.State == ConnectionState.Open)
                {
                    await npgsqlConnection.ExecuteAsync($"DROP TABLE IF EXISTS {tempTableName}");
                }
            }
            catch
            {
                // Ignore cleanup errors
            }

            throw new InvalidOperationException($"Bulk update operation failed for table {metadata.TableName}: {ex.Message}", ex);
        }
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

        var primaryKey = metadata.Properties.Values.FirstOrDefault(p => p.IsPrimaryKey)
            ?? throw new InvalidOperationException($"No primary key found for entity {metadata.EntityType.Name}");

        var tableName = GetTableName(metadata);
        var pkColumn = _dialect.EscapeIdentifier(primaryKey.ColumnName);

        try
        {
            // Convert object array to typed array for PostgreSQL
            var typedArray = ConvertToTypedArray(idList, primaryKey.PropertyType);
            
            // Use ANY array operator for bulk delete
            var sql = $"DELETE FROM {tableName} WHERE {pkColumn} = ANY(@ids)";
            var rowsAffected = await connection.ExecuteAsync(sql, new { ids = typedArray });
            return rowsAffected;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Bulk delete operation failed for table {tableName}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public int BulkInsert<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata)
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

        return BatchInsert(connection, entityList, metadata);
    }

    /// <inheritdoc />
    public int BulkUpdate<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata)
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

        var primaryKey = metadata.Properties.Values.FirstOrDefault(p => p.IsPrimaryKey)
            ?? throw new InvalidOperationException($"No primary key found for entity {metadata.EntityType.Name}");

        var tempTableName = $"temp_{metadata.TableName}_{Guid.NewGuid():N}";

        try
        {
            var createTempTableSql = GenerateCreateTempTableSql(metadata, tempTableName);
            connection.Execute(createTempTableSql);

            BulkInsertToTempTable(connection, entityList, metadata, tempTableName);

            var mergeSql = GenerateMergeSqlForUpdate(metadata, tempTableName, primaryKey);
            var rowsAffected = connection.Execute(mergeSql);

            return rowsAffected;
        }
        finally
        {
            try
            {
                connection.Execute($"DROP TABLE IF EXISTS {tempTableName}");
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <inheritdoc />
    public int BulkDelete(IDbConnection connection, IEnumerable<object> ids, EntityMetadata metadata)
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

        var primaryKey = metadata.Properties.Values.FirstOrDefault(p => p.IsPrimaryKey)
            ?? throw new InvalidOperationException($"No primary key found for entity {metadata.EntityType.Name}");

        var tableName = _dialect.EscapeIdentifier(GetUnescapedTableName(metadata));
        var pkColumn = _dialect.EscapeIdentifier(primaryKey.ColumnName);

        var sql = $"DELETE FROM {tableName} WHERE {pkColumn} = ANY(@ids)";

        try
        {
            var rowsAffected = connection.Execute(sql, new { ids = idList.ToArray() });
            return rowsAffected;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Bulk delete operation failed for table {tableName}: {ex.Message}", ex);
        }
    }

    private async Task<int> BatchInsertAsync<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata, CancellationToken cancellationToken)
    {
        var entityList = entities.ToList();
        var batches = entityList.Chunk(MaxBatchSize);
        var totalInserted = 0;

        foreach (var batch in batches)
        {
            var sql = GenerateBatchInsertSql(metadata, batch.Count());
            var parameters = ExtractBatchParameters(batch.ToList(), metadata);
            var affectedRows = await connection.ExecuteAsync(sql, parameters);
            totalInserted += affectedRows;
        }

        return totalInserted;
    }

    private string GenerateBatchInsertSql(EntityMetadata metadata, int count)
    {
        var tableName = GetTableName(metadata);
        var columns = metadata.Properties.Values
            .Where(p => !p.IsPrimaryKey || p.GenerationType != GenerationType.Identity)
            .ToList();

        var columnList = string.Join(", ", columns.Select(p => _dialect.EscapeIdentifier(p.ColumnName)));
        var valuesList = new List<string>();

        for (int i = 0; i < count; i++)
        {
            var paramList = string.Join(", ", columns.Select(p => $"@{p.PropertyName}{i}"));
            valuesList.Add($"({paramList})");
        }

        return $"INSERT INTO {tableName} ({columnList}) VALUES {string.Join(", ", valuesList)}";
    }

    private Dictionary<string, object?> ExtractBatchParameters<T>(IList<T> entities, EntityMetadata metadata)
    {
        var parameters = new Dictionary<string, object?>();
        var columns = metadata.Properties.Values
            .Where(p => !p.IsPrimaryKey || p.GenerationType != GenerationType.Identity)
            .ToList();

        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (entity == null) continue;

            foreach (var property in columns)
            {
                var value = GetPropertyValue(entity, property.PropertyName);
                parameters[$"{property.PropertyName}{i}"] = value;
            }
        }

        return parameters;
    }

    private async Task BulkInsertToTempTableAsync<T>(NpgsqlConnection npgsqlConnection, IList<T> entities, EntityMetadata metadata, string tempTableName, CancellationToken cancellationToken)
    {
        // Use COPY for temp table insert
        var columns = metadata.Properties.Values.ToList();
        var columnNames = string.Join(", ", columns.Select(p => _dialect.EscapeIdentifier(p.ColumnName)));
        // Don't escape temp table name - it's safe (GUID-based) and escaping causes case-sensitivity issues
        var copyCommand = $"COPY {tempTableName} ({columnNames}) FROM STDIN (FORMAT BINARY)";

        // Connection should already be open from BulkUpdateAsync
        if (npgsqlConnection.State != ConnectionState.Open)
            await npgsqlConnection.OpenAsync(cancellationToken);

        using var writer = await npgsqlConnection.BeginBinaryImportAsync(copyCommand, cancellationToken);

            foreach (var entity in entities)
            {
                if (entity == null) continue;

                await writer.StartRowAsync(cancellationToken);

                foreach (var property in columns)
                {
                    var value = GetPropertyValue(entity, property.PropertyName);
                    var convertedValue = _typeConverter.ConvertToDatabase(value, property.PropertyType);

                    if (convertedValue == null || convertedValue is DBNull)
                        await writer.WriteNullAsync(cancellationToken);
                    else
                        await writer.WriteAsync(convertedValue, cancellationToken);
                }
            }

            await writer.CompleteAsync(cancellationToken);
    }

    private string GenerateCreateTempTableSql(EntityMetadata metadata, string tempTableName)
    {
        var columns = metadata.Properties.Values
            .Select(p => $"{_dialect.EscapeIdentifier(p.ColumnName)} {_dialect.GetDataTypeMapping(p.PropertyType)}")
            .ToList();

        // Don't escape temp table name - it's safe (GUID-based) and escaping causes case-sensitivity issues
        // Don't use ON COMMIT DROP - we'll explicitly drop the table when done

        return $@"CREATE TEMP TABLE {tempTableName} (
    {string.Join(",\n    ", columns)}
)";
    }

    private string GenerateUpdateFromTempTableSql(EntityMetadata metadata, string tempTableName, PropertyMetadata primaryKey)
    {
        var tableName = GetTableName(metadata);
        var pkColumn = _dialect.EscapeIdentifier(primaryKey.ColumnName);
        // Don't escape temp table name

        var updateColumns = metadata.Properties.Values
            .Where(p => !p.IsPrimaryKey)
            .Select(p => $"{_dialect.EscapeIdentifier(p.ColumnName)} = temp.{_dialect.EscapeIdentifier(p.ColumnName)}")
            .ToList();

        return $@"UPDATE {tableName} AS target
SET {string.Join(",\n    ", updateColumns)}
FROM {tempTableName} AS temp
WHERE target.{pkColumn} = temp.{pkColumn}";
    }

    private string GetTableName(EntityMetadata metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata.SchemaName))
        {
            return $"{_dialect.EscapeIdentifier(metadata.SchemaName)}.{_dialect.EscapeIdentifier(metadata.TableName)}";
        }

        return _dialect.EscapeIdentifier(metadata.TableName);
    }

    private object? GetPropertyValue<T>(T entity, string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName);
        return property?.GetValue(entity);
    }

    private int BatchInsert<T>(IDbConnection connection, IList<T> entities, EntityMetadata metadata)
    {
        var batches = entities.Chunk(MaxBatchSize);
        var totalInserted = 0;

        foreach (var batch in batches)
        {
            var columns = metadata.Properties.Values
                .Where(p => !p.IsPrimaryKey || p.GenerationType != GenerationType.Identity)
                .ToList();

            var sql = GenerateBatchInsertSql(metadata, batch.Count());
            var parameters = new DynamicParameters();
            
            var entityType = typeof(T);
            int entityIndex = 0;
            foreach (var entity in batch)
            {
                foreach (var property in columns)
                {
                    var propertyInfo = entityType.GetProperty(property.PropertyName);
                    var value = propertyInfo?.GetValue(entity);
                    var convertedValue = _typeConverter.ConvertToDatabase(value, property.PropertyType);
                    
                    parameters.Add($"p{entityIndex}_{property.PropertyName}", convertedValue);
                }
                entityIndex++;
            }

            var rowsInserted = connection.Execute(sql, parameters);
            totalInserted += rowsInserted;
        }

        return totalInserted;
    }

    private void BulkInsertToTempTable<T>(IDbConnection connection, IList<T> entities, EntityMetadata metadata, string tempTableName)
    {
        var batches = entities.Chunk(MaxBatchSize);

        foreach (var batch in batches)
        {
            var columns = metadata.Properties.Values.ToList();
            var columnNames = string.Join(", ", columns.Select(p => _dialect.EscapeIdentifier(p.ColumnName)));

            var valueRows = new List<string>();
            var parameters = new DynamicParameters();
            var entityType = typeof(T);
            int entityIndex = 0;

            foreach (var entity in batch)
            {
                var valueParams = new List<string>();
                foreach (var property in columns)
                {
                    var paramName = $"p{entityIndex}_{property.PropertyName}";
                    var propertyInfo = entityType.GetProperty(property.PropertyName);
                    var value = propertyInfo?.GetValue(entity);
                    var convertedValue = _typeConverter.ConvertToDatabase(value, property.PropertyType);
                    
                    parameters.Add(paramName, convertedValue);
                    valueParams.Add($"@{paramName}");
                }
                valueRows.Add($"({string.Join(", ", valueParams)})");
                entityIndex++;
            }

            var sql = $"INSERT INTO {tempTableName} ({columnNames}) VALUES {string.Join(", ", valueRows)}";
            connection.Execute(sql, parameters);
        }
    }

    private string GenerateMergeSqlForUpdate(EntityMetadata metadata, string tempTableName, PropertyMetadata primaryKey)
    {
        var tableName = GetTableName(metadata);
        var pkColumn = _dialect.EscapeIdentifier(primaryKey.ColumnName);

        var updateColumns = metadata.Properties.Values
            .Where(p => !p.IsPrimaryKey)
            .Select(p => $"{_dialect.EscapeIdentifier(p.ColumnName)} = temp.{_dialect.EscapeIdentifier(p.ColumnName)}")
            .ToList();

        return $@"UPDATE {tableName} AS target
SET {string.Join(",\n    ", updateColumns)}
FROM {tempTableName} AS temp
WHERE target.{pkColumn} = temp.{pkColumn}";
    }

    private string GetUnescapedTableName(EntityMetadata metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata.SchemaName))
            return $"{metadata.SchemaName}.{metadata.TableName}";
        
        return metadata.TableName;
    }

    private Array ConvertToTypedArray(List<object> objects, Type targetType)
    {
        // Create a typed array based on the target type
        var array = Array.CreateInstance(targetType, objects.Count);
        for (int i = 0; i < objects.Count; i++)
        {
            array.SetValue(Convert.ChangeType(objects[i], targetType), i);
        }
        return array;
    }
}

