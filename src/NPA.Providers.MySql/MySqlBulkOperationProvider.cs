using System.Data;
using System.Reflection;
using Dapper;
using NPA.Core.Annotations;
using NPA.Core.Metadata;
using NPA.Core.Providers;

namespace NPA.Providers.MySql;

/// <summary>
/// MySQL/MariaDB-specific bulk operation provider.
/// </summary>
public class MySqlBulkOperationProvider : IBulkOperationProvider
{
    private readonly ISqlDialect _dialect;
    private readonly ITypeConverter _typeConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlBulkOperationProvider"/> class.
    /// </summary>
    /// <param name="dialect">The SQL dialect.</param>
    /// <param name="typeConverter">The type converter.</param>
    public MySqlBulkOperationProvider(ISqlDialect dialect, ITypeConverter typeConverter)
    {
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        _typeConverter = typeConverter ?? throw new ArgumentNullException(nameof(typeConverter));
    }

    /// <inheritdoc />
    public int MaxBatchSize => 1000; // MySQL recommended batch size (lower than SQL Server)

    /// <inheritdoc />
    public bool SupportsTableValuedParameters => false; // MySQL doesn't support TVPs

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

        // MySQL bulk insert strategy: Use multi-row INSERT
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

        // MySQL bulk update strategy: Use multiple UPDATE statements or CASE
        var primaryKey = metadata.Properties.Values.FirstOrDefault(p => p.IsPrimaryKey)
            ?? throw new InvalidOperationException($"No primary key found for entity {metadata.EntityType.Name}");

        var totalUpdated = 0;
        var batches = entityList.Chunk(MaxBatchSize);
        var entityType = typeof(T);
        var primaryKeyInfo = entityType.GetProperty(primaryKey.PropertyName);

        foreach (var batch in batches)
        {
            var tableName = GetUnescapedTableName(metadata);
            var updates = new List<string>();

            foreach (var entity in batch)
            {
                var pkValue = primaryKeyInfo?.GetValue(entity);
                var setClauses = metadata.Properties.Values
                    .Where(p => !p.IsPrimaryKey)
                    .Select(p => $"{_dialect.EscapeIdentifier(p.ColumnName)} = @{p.PropertyName}_{pkValue}")
                    .ToList();

                var where = $"{_dialect.EscapeIdentifier(primaryKey.ColumnName)} = @pk_{pkValue}";
                updates.Add($"UPDATE {_dialect.EscapeIdentifier(tableName)} SET {string.Join(", ", setClauses)} WHERE {where}");
            }

            var parameters = new DynamicParameters();
            foreach (var entity in batch)
            {
                var pkValue = primaryKeyInfo?.GetValue(entity);
                parameters.Add($"pk_{pkValue}", pkValue);

                foreach (var property in metadata.Properties.Values.Where(p => !p.IsPrimaryKey))
                {
                    var propertyInfo = entityType.GetProperty(property.PropertyName);
                    var value = propertyInfo?.GetValue(entity);
                    parameters.Add($"{property.PropertyName}_{pkValue}", _typeConverter.ConvertToDatabase(value, property.PropertyType));
                }
            }

            var sql = string.Join("; ", updates);
            var affected = await connection.ExecuteAsync(sql, parameters);
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

        var primaryKey = metadata.Properties.Values.FirstOrDefault(p => p.IsPrimaryKey)
            ?? throw new InvalidOperationException($"No primary key found for entity {metadata.EntityType.Name}");

        var tableName = GetUnescapedTableName(metadata);
        var primaryKeyColumn = _dialect.EscapeIdentifier(primaryKey.ColumnName);

        var totalDeleted = 0;
        var batches = idList.Chunk(MaxBatchSize);

        foreach (var batch in batches)
        {
            // Use IN clause for batch delete
            var parameterNames = batch.Select((id, index) => $"@id{index}").ToList();
            var parameters = new DynamicParameters();
            
            for (int i = 0; i < batch.Length; i++)
            {
                parameters.Add($"id{i}", batch.ElementAt(i));
            }

            var sql = $"DELETE FROM {_dialect.EscapeIdentifier(tableName)} WHERE {primaryKeyColumn} IN ({string.Join(", ", parameterNames)})";
            var affected = await connection.ExecuteAsync(sql, parameters);
            totalDeleted += affected;
        }

        return totalDeleted;
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

        // MySQL bulk insert strategy: Use multi-row INSERT
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
            var affected = connection.Execute(sql, parameters);
            totalInserted += affected;
        }

        return totalInserted;
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

        var totalUpdated = 0;
        var entityType = typeof(T);

        foreach (var entity in entityList)
        {
            var parameters = new DynamicParameters();
            var setClauses = new List<string>();

            foreach (var property in metadata.Properties.Values.Where(p => !p.IsPrimaryKey))
            {
                var propertyInfo = entityType.GetProperty(property.PropertyName);
                var value = propertyInfo?.GetValue(entity);
                var convertedValue = _typeConverter.ConvertToDatabase(value, property.PropertyType);
                
                parameters.Add(property.PropertyName, convertedValue);
                setClauses.Add($"{_dialect.EscapeIdentifier(property.ColumnName)} = @{property.PropertyName}");
            }

            var pkPropertyInfo = entityType.GetProperty(primaryKey.PropertyName);
            var pkValue = pkPropertyInfo?.GetValue(entity);
            parameters.Add(primaryKey.PropertyName, pkValue);

            var sql = $@"UPDATE {_dialect.EscapeIdentifier(GetUnescapedTableName(metadata))} 
SET {string.Join(", ", setClauses)} 
WHERE {_dialect.EscapeIdentifier(primaryKey.ColumnName)} = @{primaryKey.PropertyName}";

            var affected = connection.Execute(sql, parameters);
            totalUpdated += affected;
        }

        return totalUpdated;
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
        var primaryKeyColumn = _dialect.EscapeIdentifier(primaryKey.ColumnName);

        // Use IN clause for bulk deletes
        var parameters = new DynamicParameters();
        var parameterNames = new List<string>();
        
        for (int i = 0; i < idList.Count; i++)
        {
            var paramName = $"id{i}";
            parameters.Add(paramName, idList[i]);
            parameterNames.Add($"@{paramName}");
        }

        var sql = $"DELETE FROM {tableName} WHERE {primaryKeyColumn} IN ({string.Join(", ", parameterNames)})";
        return connection.Execute(sql, parameters);
    }

    /// <inheritdoc />
    public object CreateTableValuedParameter<T>(IEnumerable<T> entities, EntityMetadata metadata, string typeName)
    {
        // MySQL doesn't support table-valued parameters
        throw new NotSupportedException("MySQL does not support table-valued parameters. Use multi-row INSERT instead.");
    }

    private string GetUnescapedTableName(EntityMetadata metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata.SchemaName))
            return $"{metadata.SchemaName}.{metadata.TableName}";
        
        return metadata.TableName;
    }
}


