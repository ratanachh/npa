using System.Data;
using System.Reflection;
using Dapper;
using Microsoft.Extensions.Logging;
using NPA.Core.Metadata;

namespace NPA.Core.Core;

/// <summary>
/// Provides entity lifecycle management functionality.
/// </summary>
public sealed class EntityManager : IEntityManager
{
    private readonly IDbConnection _connection;
    private readonly IMetadataProvider _metadataProvider;
    private readonly IChangeTracker _changeTracker;
    private readonly ILogger<EntityManager>? _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityManager"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="metadataProvider">The metadata provider.</param>
    /// <param name="logger">The logger (optional).</param>
    public EntityManager(IDbConnection connection, IMetadataProvider metadataProvider, ILogger<EntityManager>? logger = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
        _changeTracker = new ChangeTracker();
        _logger = logger;
    }

    /// <inheritdoc />
    public IMetadataProvider MetadataProvider => _metadataProvider;

    /// <inheritdoc />
    public IChangeTracker ChangeTracker => _changeTracker;

    /// <inheritdoc />
    public async Task PersistAsync<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        _logger?.LogDebug("Persisting entity of type {EntityType}", typeof(T).Name);

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = GenerateInsertSql(metadata);
        var parameters = ExtractParameters(entity, metadata);

        try
        {
            var id = await _connection.QuerySingleAsync<object>(sql, parameters);
            SetEntityId(entity, id, metadata);
            _changeTracker.Track(entity, EntityState.Added);

            _logger?.LogDebug("Successfully persisted entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error persisting entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<T?> FindAsync<T>(object id) where T : class
    {
        ThrowIfDisposed();
        if (id == null) throw new ArgumentNullException(nameof(id));

        _logger?.LogDebug("Finding entity of type {EntityType} with ID {Id}", typeof(T).Name, id);

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = GenerateSelectSql(metadata);
        var parameters = new { id };

        try
        {
            var entity = await _connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
            if (entity != null)
            {
                _changeTracker.Track(entity, EntityState.Unchanged);
            }

            _logger?.LogDebug("Found entity of type {EntityType} with ID {Id}: {Found}", typeof(T).Name, id, entity != null);
            return entity;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error finding entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<T?> FindAsync<T>(CompositeKey key) where T : class
    {
        ThrowIfDisposed();
        if (key == null) throw new ArgumentNullException(nameof(key));

        _logger?.LogDebug("Finding entity of type {EntityType} with composite key", typeof(T).Name);

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = GenerateSelectByCompositeKeySql(metadata, key);
        var parameters = CreateParametersFromCompositeKey(key, metadata);

        try
        {
            var entity = await _connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
            if (entity != null)
            {
                _changeTracker.Track(entity, EntityState.Unchanged);
            }

            _logger?.LogDebug("Found entity of type {EntityType} with composite key: {Found}", typeof(T).Name, entity != null);
            return entity;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error finding entity of type {EntityType} with composite key", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task MergeAsync<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        _logger?.LogDebug("Merging entity of type {EntityType}", typeof(T).Name);

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        
        // Check if entity is already tracked
        var currentState = _changeTracker.GetState(entity);
        if (currentState == null)
        {
            // Entity not tracked, try to find it first
            var id = GetEntityId(entity, metadata);
            if (id != null)
            {
                var existingEntity = await FindAsync<T>(id);
                if (existingEntity != null)
                {
                    _changeTracker.Track(entity, EntityState.Modified);
                }
                else
                {
                    await PersistAsync(entity);
                    return;
                }
            }
            else
            {
                await PersistAsync(entity);
                return;
            }
        }

        var sql = GenerateUpdateSql(metadata);
        var parameters = ExtractParameters(entity, metadata);

        try
        {
            await _connection.ExecuteAsync(sql, parameters);
            _changeTracker.SetState(entity, EntityState.Modified);

            _logger?.LogDebug("Successfully merged entity of type {EntityType}", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error merging entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        _logger?.LogDebug("Removing entity of type {EntityType}", typeof(T).Name);

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = GenerateDeleteSql(metadata);
        var parameters = ExtractIdParameters(entity, metadata);

        try
        {
            await _connection.ExecuteAsync(sql, parameters);
            _changeTracker.SetState(entity, EntityState.Deleted);

            _logger?.LogDebug("Successfully removed entity of type {EntityType}", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync<T>(object id) where T : class
    {
        ThrowIfDisposed();
        if (id == null) throw new ArgumentNullException(nameof(id));

        _logger?.LogDebug("Removing entity of type {EntityType} with ID {Id}", typeof(T).Name, id);

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = GenerateDeleteSql(metadata);
        var parameters = new { id };

        try
        {
            await _connection.ExecuteAsync(sql, parameters);

            _logger?.LogDebug("Successfully removed entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task FlushAsync()
    {
        ThrowIfDisposed();

        _logger?.LogDebug("Flushing pending changes");

        var pendingChanges = _changeTracker.GetPendingChanges();
        
        foreach (var kvp in pendingChanges)
        {
            var entity = kvp.Key;
            var state = kvp.Value;

            try
            {
                switch (state)
                {
                    case EntityState.Added:
                        await CallGenericMethodAsync(nameof(PersistAsync), entity);
                        break;
                    case EntityState.Modified:
                        await CallGenericMethodAsync(nameof(MergeAsync), entity);
                        break;
                    case EntityState.Deleted:
                        await CallGenericMethodAsync(nameof(RemoveAsync), entity);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error flushing entity of type {EntityType} with state {State}", entity.GetType().Name, state);
                throw;
            }
        }

        _logger?.LogDebug("Successfully flushed {Count} pending changes", pendingChanges.Count);
    }

    /// <inheritdoc />
    public Task ClearAsync()
    {
        ThrowIfDisposed();

        _logger?.LogDebug("Clearing persistence context");

        _changeTracker.Clear();

        _logger?.LogDebug("Successfully cleared persistence context");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public bool Contains<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        if (entity == null) return false;

        return _changeTracker.GetState(entity) != null;
    }

    /// <inheritdoc />
    public void Detach<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        if (entity == null) return;

        _changeTracker.Untrack(entity);
        _logger?.LogDebug("Detached entity of type {EntityType}", typeof(T).Name);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _changeTracker.Clear();
            _disposed = true;
            _logger?.LogDebug("EntityManager disposed");
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EntityManager));
    }

    private string GenerateInsertSql(EntityMetadata metadata)
    {
        var columns = metadata.Properties.Values
            .Where(p => !p.IsPrimaryKey || p.GenerationType != Annotations.GenerationType.Identity)
            .Select(p => p.ColumnName);

        var values = metadata.Properties.Values
            .Where(p => !p.IsPrimaryKey || p.GenerationType != Annotations.GenerationType.Identity)
            .Select(p => "@" + p.PropertyName);

        var columnList = string.Join(", ", columns);
        var valueList = string.Join(", ", values);

        // Check if we have an identity column to return
        var identityColumn = metadata.Properties.Values
            .FirstOrDefault(p => p.IsPrimaryKey && p.GenerationType == Annotations.GenerationType.Identity);
        
        if (identityColumn != null)
        {
            return $"INSERT INTO {metadata.FullTableName} ({columnList}) VALUES ({valueList}) RETURNING {identityColumn.ColumnName};";
        }
        
        return $"INSERT INTO {metadata.FullTableName} ({columnList}) VALUES ({valueList});";
    }

    private string GenerateSelectSql(EntityMetadata metadata)
    {
        var columns = metadata.Properties.Values.Select(p => p.ColumnName);
        var columnList = string.Join(", ", columns);

        return $"SELECT {columnList} FROM {metadata.FullTableName} WHERE {metadata.Properties[metadata.PrimaryKeyProperty].ColumnName} = @id";
    }

    private string GenerateSelectByCompositeKeySql(EntityMetadata metadata, CompositeKey key)
    {
        var columns = metadata.Properties.Values.Select(p => p.ColumnName);
        var columnList = string.Join(", ", columns);

        var whereConditions = key.Values.Keys
            .Select(propertyName => metadata.Properties[propertyName].ColumnName + " = @" + propertyName);

        var whereClause = string.Join(" AND ", whereConditions);

        return $"SELECT {columnList} FROM {metadata.FullTableName} WHERE {whereClause}";
    }

    private string GenerateUpdateSql(EntityMetadata metadata)
    {
        var setClauses = metadata.Properties.Values
            .Where(p => !p.IsPrimaryKey)
            .Select(p => $"{p.ColumnName} = @{p.PropertyName}");

        var setClause = string.Join(", ", setClauses);
        var primaryKeyColumn = metadata.Properties[metadata.PrimaryKeyProperty].ColumnName;

        return $"UPDATE {metadata.FullTableName} SET {setClause} WHERE {primaryKeyColumn} = @{metadata.PrimaryKeyProperty}";
    }

    private string GenerateDeleteSql(EntityMetadata metadata)
    {
        var primaryKeyColumn = metadata.Properties[metadata.PrimaryKeyProperty].ColumnName;
        return $"DELETE FROM {metadata.FullTableName} WHERE {primaryKeyColumn} = @id";
    }

    private object ExtractParameters(object entity, EntityMetadata metadata)
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
                var value = property.GetValue(entity);
                parameters[propertyName] = value;
            }
        }

        return parameters;
    }

    private object ExtractIdParameters(object entity, EntityMetadata metadata)
    {
        var entityType = entity.GetType();
        var idProperty = entityType.GetProperty(metadata.PrimaryKeyProperty);
        
        if (idProperty?.CanRead != true)
            throw new InvalidOperationException($"Cannot read primary key property {metadata.PrimaryKeyProperty}");

        var id = idProperty.GetValue(entity);
        return new { id };
    }

    private object CreateParametersFromCompositeKey(CompositeKey key, EntityMetadata metadata)
    {
        var parameters = new Dictionary<string, object?>();

        foreach (var kvp in key.Values)
        {
            parameters[kvp.Key] = kvp.Value;
        }

        return parameters;
    }

    private object? GetEntityId(object entity, EntityMetadata metadata)
    {
        var entityType = entity.GetType();
        var idProperty = entityType.GetProperty(metadata.PrimaryKeyProperty);
        
        if (idProperty?.CanRead != true)
            return null;

        return idProperty.GetValue(entity);
    }

    private void SetEntityId(object entity, object id, EntityMetadata metadata)
    {
        var entityType = entity.GetType();
        var idProperty = entityType.GetProperty(metadata.PrimaryKeyProperty);
        
        if (idProperty?.CanWrite == true)
        {
            // Handle dynamic object returned from PostgreSQL RETURNING clause
            if (id != null && id.GetType().Name == "DapperRow")
            {
                var columnName = metadata.Properties[metadata.PrimaryKeyProperty].ColumnName;
                
                // Use dynamic to access the properties
                dynamic dapperRow = id;
                try
                {
                    // Try to get the specific column using dynamic access
                    var dynamicRow = (dynamic)dapperRow;
                    id = dynamicRow[columnName];
                }
                catch
                {
                    // Fallback: try to get the first property value
                    try
                    {
                        var properties = (IDictionary<string, object?>)dapperRow;
                        id = properties.Values.FirstOrDefault() ?? id;
                    }
                    catch
                    {
                        // If all else fails, try to convert the dynamic object
                        try
                        {
                            id = Convert.ChangeType(dapperRow, idProperty.PropertyType);
                        }
                        catch
                        {
                            // Last resort: use reflection to get the first property
                            var rowType = dapperRow.GetType();
                            var properties = rowType.GetProperties();
                            if (properties.Length > 0)
                            {
                                id = properties[0].GetValue(dapperRow);
                            }
                        }
                    }
                }
            }
            
            idProperty.SetValue(entity, id);
        }
    }

    private async Task CallGenericMethodAsync(string methodName, object entity)
    {
        var entityType = entity.GetType();
        var method = GetType().GetMethod(methodName);
        if (method == null)
            throw new InvalidOperationException($"Method {methodName} not found");

        var genericMethod = method.MakeGenericMethod(entityType);
        var task = (Task)genericMethod.Invoke(this, new[] { entity })!;
        await task;
    }
}
