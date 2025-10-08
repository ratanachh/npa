using System.Data;
using Dapper;
using NPA.Core.Annotations;
using NPA.Core.Metadata;
using NPA.Core.Providers;
using Npgsql;

namespace NPA.Providers.PostgreSql;

/// <summary>
/// PostgreSQL database provider implementation for NPA.
/// </summary>
public class PostgreSqlProvider : IDatabaseProvider
{
    private const string QuoteChar = "\"";
    
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

        var tableName = EscapeIdentifier(metadata.TableName);

        if (!string.IsNullOrWhiteSpace(metadata.SchemaName))
        {
            var schemaName = EscapeIdentifier(metadata.SchemaName);
            return $"{schemaName}.{tableName}";
        }

        return tableName;
    }

    /// <inheritdoc />
    public string ResolveColumnName(PropertyMetadata property)
    {
        if (property == null)
            throw new ArgumentNullException(nameof(property));

        return EscapeIdentifier(property.ColumnName);
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
        if (value == null)
            return null;

        // PostgreSQL-specific type conversions
        if (targetType == typeof(bool) || targetType == typeof(bool?))
        {
            if (value is bool boolValue)
                return boolValue;
            if (value is string stringValue)
                return bool.Parse(stringValue);
        }

        return value;
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

        // For PostgreSQL, we'll use COPY or batch INSERT
        // For now, implement as batch INSERT
        var sql = GenerateInsertSql(metadata);
        var affectedRows = 0;

        foreach (var entity in entityList)
        {
            if (entity == null) continue;
            var parameters = ExtractParameters(entity, metadata);
            affectedRows += await connection.ExecuteAsync(sql, parameters);
        }

        return affectedRows;
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

        var sql = GenerateUpdateSql(metadata);
        var affectedRows = 0;

        foreach (var entity in entityList)
        {
            if (entity == null) continue;
            var parameters = ExtractParameters(entity, metadata);
            affectedRows += await connection.ExecuteAsync(sql, parameters);
        }

        return affectedRows;
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

        var sql = GenerateDeleteSql(metadata);
        var affectedRows = 0;

        foreach (var id in idList)
        {
            var parameters = new { id };
            affectedRows += await connection.ExecuteAsync(sql, parameters);
        }

        return affectedRows;
    }

    private string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier cannot be null or empty.", nameof(identifier));

        // PostgreSQL uses double quotes for identifiers and lowercases unquoted identifiers
        return $"{QuoteChar}{identifier}{QuoteChar}";
    }

    private Dictionary<string, object?> ExtractParameters(object entity, EntityMetadata metadata)
    {
        var parameters = new Dictionary<string, object?>();
        var entityType = entity.GetType();

        foreach (var kvp in metadata.Properties)
        {
            var propertyName = kvp.Key;
            var propertyMetadata = kvp.Value;
            var property = entityType.GetProperty(propertyName);

            if (property?.CanRead == true)
            {
                // Skip identity columns - they should be auto-generated by the database
                if (propertyMetadata.IsPrimaryKey && propertyMetadata.GenerationType == GenerationType.Identity)
                {
                    continue;
                }

                var value = property.GetValue(entity);
                parameters[propertyName] = value;
            }
        }
        return parameters;
    }

    private string GetPropertyNameFromColumn(EntityMetadata metadata, string columnName)
    {
        var property = metadata.Properties.Values
            .FirstOrDefault(p => EscapeIdentifier(p.ColumnName) == columnName);
        
        return property?.PropertyName ?? columnName.Trim('"');
    }
}