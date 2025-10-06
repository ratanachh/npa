using System.Data;
using System.Data.SqlTypes;
using System.Reflection;
using Dapper;
using Microsoft.Data.SqlClient;
using NPA.Core.Annotations;
using NPA.Core.Metadata;
using NPA.Core.Providers;

namespace NPA.Providers.SqlServer;

/// <summary>
/// SQL Server-specific bulk operation provider using SqlBulkCopy and table-valued parameters.
/// </summary>
public class SqlServerBulkOperationProvider : IBulkOperationProvider
{
    private readonly ISqlDialect _dialect;
    private readonly ITypeConverter _typeConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerBulkOperationProvider"/> class.
    /// </summary>
    /// <param name="dialect">The SQL dialect.</param>
    /// <param name="typeConverter">The type converter.</param>
    public SqlServerBulkOperationProvider(ISqlDialect dialect, ITypeConverter typeConverter)
    {
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        _typeConverter = typeConverter ?? throw new ArgumentNullException(nameof(typeConverter));
    }

    /// <inheritdoc />
    public int MaxBatchSize => 10000; // SQL Server recommended batch size

    /// <inheritdoc />
    public bool SupportsTableValuedParameters => true;

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

        if (connection is not SqlConnection sqlConnection)
            throw new ArgumentException("Connection must be a SqlConnection for SQL Server bulk operations.", nameof(connection));

        var tableName = GetUnescapedTableName(metadata);
        var dataTable = CreateDataTableFromEntities(entityList, metadata);

        try
        {
            using var bulkCopy = new SqlBulkCopy(sqlConnection)
            {
                DestinationTableName = tableName,
                BatchSize = MaxBatchSize,
                BulkCopyTimeout = 300 // 5 minutes
            };

            // Map columns
            foreach (var property in metadata.Properties.Values)
            {
                // Skip identity columns in bulk insert
                if (property.IsPrimaryKey && property.GenerationType == GenerationType.Identity)
                    continue;

                bulkCopy.ColumnMappings.Add(property.PropertyName, property.ColumnName);
            }

            await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
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

        // For bulk updates, we'll use table-valued parameters with MERGE statement
        var tvpTypeName = $"{metadata.TableName}_TVP";
        var tempTableName = $"#temp_{metadata.TableName}_{Guid.NewGuid():N}";

        try
        {
            // Create a temporary table
            var createTempTableSql = GenerateCreateTempTableSql(metadata, tempTableName);
            await connection.ExecuteAsync(createTempTableSql);

            // Bulk insert into temp table
            await BulkInsertToTempTableAsync(connection, entityList, metadata, tempTableName, cancellationToken);

            // Perform MERGE operation
            var mergeSql = GenerateMergeSqlForUpdate(metadata, tempTableName, primaryKey);
            var rowsAffected = await connection.ExecuteAsync(mergeSql);

            return rowsAffected;
        }
        finally
        {
            // Clean up temp table
            try
            {
                await connection.ExecuteAsync($"DROP TABLE IF EXISTS {tempTableName}");
            }
            catch
            {
                // Ignore cleanup errors
            }
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

        // For small sets, use IN clause
        if (idList.Count <= 1000)
        {
            var tableName = _dialect.EscapeIdentifier(metadata.FullTableName);
            var primaryKeyColumn = _dialect.EscapeIdentifier(primaryKey.ColumnName);
            
            var parameters = new DynamicParameters();
            var parameterNames = new List<string>();
            
            for (int i = 0; i < idList.Count; i++)
            {
                var paramName = $"id{i}";
                parameters.Add(paramName, idList[i]);
                parameterNames.Add($"@{paramName}");
            }

            var sql = $"DELETE FROM {tableName} WHERE {primaryKeyColumn} IN ({string.Join(", ", parameterNames)})";
            return await connection.ExecuteAsync(sql, parameters);
        }

        // For larger sets, use temp table approach
        var tempTableName = $"#temp_delete_{metadata.TableName}_{Guid.NewGuid():N}";
        
        try
        {
            // Create temp table for IDs
            var createTempSql = $@"CREATE TABLE {tempTableName} 
(
    {_dialect.EscapeIdentifier(primaryKey.ColumnName)} {_typeConverter.GetDatabaseTypeName(primaryKey.PropertyType)}
)";
            await connection.ExecuteAsync(createTempSql);

            // Insert IDs into temp table
            var insertSql = $"INSERT INTO {tempTableName} VALUES (@id)";
            var parameters = idList.Select(id => new { id });
            await connection.ExecuteAsync(insertSql, parameters);

            // Perform bulk delete
            var deleteSql = $@"DELETE t1 
FROM {_dialect.EscapeIdentifier(metadata.FullTableName)} t1
INNER JOIN {tempTableName} t2 ON t1.{_dialect.EscapeIdentifier(primaryKey.ColumnName)} = t2.{_dialect.EscapeIdentifier(primaryKey.ColumnName)}";

            return await connection.ExecuteAsync(deleteSql);
        }
        finally
        {
            // Clean up temp table
            try
            {
                await connection.ExecuteAsync($"DROP TABLE IF EXISTS {tempTableName}");
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <inheritdoc />
    public object CreateTableValuedParameter<T>(IEnumerable<T> entities, EntityMetadata metadata, string typeName)
    {
        var dataTable = CreateDataTableFromEntities(entities, metadata);
        var parameter = new SqlParameter
        {
            ParameterName = "@data",
            SqlDbType = SqlDbType.Structured,
            TypeName = typeName,
            Value = dataTable
        };

        return parameter;
    }

    private DataTable CreateDataTableFromEntities<T>(IEnumerable<T> entities, EntityMetadata metadata)
    {
        var dataTable = new DataTable();
        var entityType = typeof(T);

        // Add columns to DataTable
        foreach (var property in metadata.Properties.Values)
        {
            // Skip identity columns for inserts
            if (property.IsPrimaryKey && property.GenerationType == GenerationType.Identity)
                continue;

            var propertyInfo = entityType.GetProperty(property.PropertyName);
            if (propertyInfo == null)
                continue;

            var columnType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
            dataTable.Columns.Add(property.PropertyName, columnType);
        }

        // Add rows to DataTable
        foreach (var entity in entities)
        {
            var row = dataTable.NewRow();
            
            foreach (var property in metadata.Properties.Values)
            {
                // Skip identity columns for inserts
                if (property.IsPrimaryKey && property.GenerationType == GenerationType.Identity)
                    continue;

                var propertyInfo = entityType.GetProperty(property.PropertyName);
                if (propertyInfo == null)
                    continue;

                var value = propertyInfo.GetValue(entity);
                var convertedValue = _typeConverter.ConvertToDatabase(value, propertyInfo.PropertyType);
                
                row[property.PropertyName] = convertedValue ?? DBNull.Value;
            }
            
            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    private async Task BulkInsertToTempTableAsync<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata, string tempTableName, CancellationToken cancellationToken)
    {
        if (connection is not SqlConnection sqlConnection)
            throw new ArgumentException("Connection must be a SqlConnection for SQL Server bulk operations.", nameof(connection));

        var dataTable = CreateDataTableFromEntitiesIncludingPK(entities, metadata);

        using var bulkCopy = new SqlBulkCopy(sqlConnection)
        {
            DestinationTableName = tempTableName,
            BatchSize = MaxBatchSize,
            BulkCopyTimeout = 300
        };

        // Map all columns including primary key for updates
        foreach (var property in metadata.Properties.Values)
        {
            bulkCopy.ColumnMappings.Add(property.PropertyName, property.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
    }

    private DataTable CreateDataTableFromEntitiesIncludingPK<T>(IEnumerable<T> entities, EntityMetadata metadata)
    {
        var dataTable = new DataTable();
        var entityType = typeof(T);

        // Add columns to DataTable (including primary key for updates)
        foreach (var property in metadata.Properties.Values)
        {
            var propertyInfo = entityType.GetProperty(property.PropertyName);
            if (propertyInfo == null)
                continue;

            var columnType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
            dataTable.Columns.Add(property.PropertyName, columnType);
        }

        // Add rows to DataTable
        foreach (var entity in entities)
        {
            var row = dataTable.NewRow();
            
            foreach (var property in metadata.Properties.Values)
            {
                var propertyInfo = entityType.GetProperty(property.PropertyName);
                if (propertyInfo == null)
                    continue;

                var value = propertyInfo.GetValue(entity);
                var convertedValue = _typeConverter.ConvertToDatabase(value, propertyInfo.PropertyType);
                
                row[property.PropertyName] = convertedValue ?? DBNull.Value;
            }
            
            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    private string GenerateCreateTempTableSql(EntityMetadata metadata, string tempTableName)
    {
        var columnDefinitions = new List<string>();

        foreach (var property in metadata.Properties.Values)
        {
            var columnName = _dialect.EscapeIdentifier(property.ColumnName);
            var dataType = _typeConverter.GetDatabaseTypeName(property.PropertyType, property.Length, property.Precision, property.Scale);
            
            var definition = $"{columnName} {dataType}";
            
            if (!property.IsNullable)
            {
                definition += " NOT NULL";
            }

            columnDefinitions.Add(definition);
        }

        var columnList = string.Join(",\n    ", columnDefinitions);
        
        return $@"CREATE TABLE {tempTableName}
(
    {columnList}
)";
    }

    private string GenerateMergeSqlForUpdate(EntityMetadata metadata, string tempTableName, PropertyMetadata primaryKey)
    {
        var targetTable = _dialect.EscapeIdentifier(metadata.FullTableName);
        var primaryKeyColumn = _dialect.EscapeIdentifier(primaryKey.ColumnName);

        var updateColumns = metadata.Properties.Values
            .Where(p => !p.IsPrimaryKey)
            .Select(p => 
            {
                var column = _dialect.EscapeIdentifier(p.ColumnName);
                return $"target.{column} = source.{column}";
            })
            .ToList();

        var updateClause = string.Join(",\n        ", updateColumns);

        return $@"MERGE {targetTable} AS target
USING {tempTableName} AS source
ON target.{primaryKeyColumn} = source.{primaryKeyColumn}
WHEN MATCHED THEN
    UPDATE SET
        {updateClause};";
    }

    private string GetUnescapedTableName(EntityMetadata metadata)
    {
        var tableName = metadata.TableName;
        
        if (!string.IsNullOrWhiteSpace(metadata.SchemaName))
        {
            return $"{metadata.SchemaName}.{tableName}";
        }

        return tableName;
    }
}