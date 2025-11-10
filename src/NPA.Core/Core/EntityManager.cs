using System.Data;
using System.Reflection;
using Dapper;
using Microsoft.Extensions.Logging;
using NPA.Core.Annotations;
using NPA.Core.Metadata;
using NPA.Core.MultiTenancy;
using NPA.Core.Providers;
using NPA.Core.Query;
using NPA.Core.Query.CPQL;

namespace NPA.Core.Core;

/// <summary>
/// Provides entity lifecycle management functionality.
/// </summary>
public sealed class EntityManager : IEntityManager
{
    private readonly IDbConnection _connection;
    private readonly IMetadataProvider _metadataProvider;
    private readonly IDatabaseProvider _databaseProvider;
    private readonly IChangeTracker _changeTracker;
    private readonly ICascadeService _cascadeService;
    private readonly ITenantProvider? _tenantProvider;
    private readonly ILogger<EntityManager>? _logger;
    private ITransaction? _currentTransaction;
    private bool _disposed;

    /// <inheritdoc />
    public EntityManager(
        IDbConnection connection, 
        IMetadataProvider metadataProvider, 
        IDatabaseProvider databaseProvider, 
        ILogger<EntityManager>? logger = null,
        ITenantProvider? tenantProvider = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
        _databaseProvider = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
        _changeTracker = new ChangeTracker();
        _cascadeService = new CascadeService();
        _logger = logger;
        _tenantProvider = tenantProvider;
    }

    /// <inheritdoc />
    public IMetadataProvider MetadataProvider => _metadataProvider;
    /// <inheritdoc />
    public IChangeTracker ChangeTracker => _changeTracker;

    private void EnsureConnectionOpen()
    {
        if (_connection.State == ConnectionState.Closed)
        {
            _connection.Open();
        }
    }

    /// <inheritdoc />
    public async Task PersistAsync<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        
        // Auto-populate TenantId for multi-tenant entities
        AutoPopulateTenantId(entity);
        
        // Process cascade persist operations BEFORE persisting the entity
        var visited = new HashSet<object>();
        await _cascadeService.CascadePersistAsync(entity, metadata, this, visited);
        
        if (IsTransient(entity, metadata))
        {
            if (HasActiveTransaction)
            {
                // Queue for deferred execution - capture parameters now
                var parameters = ExtractParameters(entity, metadata, skipIdentityKeys: HasGeneratedId(metadata));
                _changeTracker.QueueOperation(
                    entity,
                    EntityState.Added,
                    () => _databaseProvider.GenerateInsertSql(metadata),
                    parameters
                );
                _changeTracker.Track(entity, EntityState.Added);
            }
            else
            {
                await InsertEntityAsync(entity, metadata);
            }
        }
        else
        {
            var existingEntity = await FindAsync<T>(GetEntityId(entity, metadata)!);
            if (existingEntity != null)
            {
                await MergeAsync(entity);
            }
            else
            {
                if (HasActiveTransaction)
                {
                    var parameters = ExtractParameters(entity, metadata, skipIdentityKeys: HasGeneratedId(metadata));
                    _changeTracker.QueueOperation(
                        entity,
                        EntityState.Added,
                        () => _databaseProvider.GenerateInsertSql(metadata),
                        parameters
                    );
                    _changeTracker.Track(entity, EntityState.Added);
                }
                else
                {
                    await InsertEntityAsync(entity, metadata);
                }
            }
        }
    }

    private async Task InsertEntityAsync<T>(T entity, EntityMetadata metadata) where T : class
    {
        var sql = _databaseProvider.GenerateInsertSql(metadata);

        if (HasGeneratedId(metadata))
        {
            var parameters = ExtractParameters(entity, metadata, skipIdentityKeys: true);
            var id = await _connection.QuerySingleAsync<object>(sql, parameters);
            SetEntityId(entity, id, metadata);
        }
        else
        {
            var parameters = ExtractParameters(entity, metadata, skipIdentityKeys: false);
            await _connection.ExecuteAsync(sql, parameters);
        }

        _changeTracker.Track(entity, EntityState.Added);
    }

    /// <inheritdoc />
    public async Task<T?> FindAsync<T>(object id) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        if (id == null) throw new ArgumentNullException(nameof(id));

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = _databaseProvider.GenerateSelectByIdSql(metadata);
        var parameters = new { id };

        var entity = await _connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
        if (entity != null) _changeTracker.Track(entity, EntityState.Unchanged);
        return entity;
    }

    /// <inheritdoc />
    public async Task<T?> FindAsync<T>(CompositeKey key) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        if (key == null) throw new ArgumentNullException(nameof(key));

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = GenerateSelectByCompositeKeySql(metadata, key);
        var parameters = CreateParametersFromCompositeKey(key, metadata);

        var entity = await _connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
        if (entity != null) _changeTracker.Track(entity, EntityState.Unchanged);
        return entity;
    }

    /// <inheritdoc />
    public async Task MergeAsync<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        
        // Validate TenantId for multi-tenant entities
        ValidateTenantId(entity);

        // Process cascade merge operations BEFORE merging the entity
        var visited = new HashSet<object>();
        await _cascadeService.CascadeMergeAsync(entity, metadata, this, visited);

        if (HasActiveTransaction)
        {
            // Queue for deferred execution - capture parameters now
            var parameters = ExtractParameters(entity, metadata, skipIdentityKeys: false);
            _changeTracker.QueueOperation(
                entity,
                EntityState.Modified,
                () => _databaseProvider.GenerateUpdateSql(metadata),
                parameters
            );
            _changeTracker.SetState(entity, EntityState.Modified);
        }
        else
        {
            // Immediate execution with internal transaction
            var sql = _databaseProvider.GenerateUpdateSql(metadata);
            var parameters = ExtractParameters(entity, metadata, skipIdentityKeys: false);

            using var transaction = _connection.BeginTransaction();
            try
            {
                await _connection.ExecuteAsync(sql, parameters, transaction);
                transaction.Commit();
                _changeTracker.SetState(entity, EntityState.Modified);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var metadata = _metadataProvider.GetEntityMetadata<T>();

        // Process cascade remove operations BEFORE removing the entity (depth-first)
        var visited = new HashSet<object>();
        await _cascadeService.CascadeRemoveAsync(entity, metadata, this, visited);

        if (HasActiveTransaction)
        {
            // Queue for deferred execution - capture parameters now
            var parameters = ExtractIdParameters(entity, metadata);
            _changeTracker.QueueOperation(
                entity,
                EntityState.Deleted,
                () => _databaseProvider.GenerateDeleteSql(metadata),
                parameters
            );
            _changeTracker.SetState(entity, EntityState.Deleted);
        }
        else
        {
            // Immediate execution
            var sql = _databaseProvider.GenerateDeleteSql(metadata);
            var parameters = ExtractIdParameters(entity, metadata);
            await _connection.ExecuteAsync(sql, parameters);
            _changeTracker.SetState(entity, EntityState.Deleted);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync<T>(object id) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        if (id == null) throw new ArgumentNullException(nameof(id));

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = _databaseProvider.GenerateDeleteSql(metadata);
        await _connection.ExecuteAsync(sql, new { id });
    }

    /// <inheritdoc />
    public async Task RemoveAsync<T>(CompositeKey key) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        if (key == null) throw new ArgumentNullException(nameof(key));

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = GenerateDeleteByCompositeKeySql(metadata, key);
        var parameters = CreateParametersFromCompositeKey(key, metadata);
        await _connection.ExecuteAsync(sql, parameters);
    }

    /// <inheritdoc />
    public async Task FlushAsync()
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();

        // Execute queued operations if in a transaction
        if (HasActiveTransaction && _changeTracker.GetQueuedOperationCount() > 0)
        {
            var operations = _changeTracker.GetQueuedOperations();
            var transaction = _currentTransaction!.DbTransaction;

            foreach (var operation in operations)
            {
                var sql = operation.SqlGenerator();
                var parameters = operation.Parameters;

                if (operation.State == EntityState.Added && HasGeneratedIdForEntity(operation.Entity))
                {
                    // For INSERT with identity, retrieve generated ID
                    var id = await _connection.QuerySingleAsync<object>(sql, parameters, transaction);
                    SetEntityIdForEntity(operation.Entity, id);
                }
                else
                {
                    await _connection.ExecuteAsync(sql, parameters, transaction);
                }

                // Update entity state after execution
                if (operation.State == EntityState.Deleted)
                {
                    _changeTracker.Untrack(operation.Entity);
                }
                else
                {
                    _changeTracker.SetState(operation.Entity, EntityState.Unchanged);
                }
            }

            _changeTracker.ClearQueue();
            return;
        }

        // Fallback: Execute pending changes from change tracker (backward compatibility)
        var pendingChanges = _changeTracker.GetPendingChanges();
        foreach (var kvp in pendingChanges)
        {
            switch (kvp.Value)
            {
                case EntityState.Added: await CallGenericMethodAsync(nameof(InsertEntityAsync), kvp.Key); break;
                case EntityState.Modified: await CallGenericMethodAsync(nameof(MergeAsync), kvp.Key); break;
                case EntityState.Deleted: await CallGenericMethodAsync(nameof(RemoveAsync), kvp.Key); break;
            }
        }
    }

    /// <inheritdoc />
    public Task ClearAsync()
    {
        ThrowIfDisposed();
    _changeTracker.Clear();
    _changeTracker.ClearQueue();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Persist<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var metadata = _metadataProvider.GetEntityMetadata<T>();

        if (IsTransient(entity, metadata))
        {
            if (HasActiveTransaction)
            {
                // Queue for deferred execution - capture parameters now
                var parameters = ExtractParameters(entity, metadata, skipIdentityKeys: HasGeneratedId(metadata));
                _changeTracker.QueueOperation(
                    entity,
                    EntityState.Added,
                    () => _databaseProvider.GenerateInsertSql(metadata),
                    parameters
                );
                _changeTracker.Track(entity, EntityState.Added);
            }
            else
            {
                InsertEntity(entity, metadata);
            }
        }
        else
        {
            var existingEntity = Find<T>(GetEntityId(entity, metadata)!);
            if (existingEntity != null)
            {
                Merge(entity);
            }
            else
            {
                if (HasActiveTransaction)
                {
                    var parameters = ExtractParameters(entity, metadata, skipIdentityKeys: HasGeneratedId(metadata));
                    _changeTracker.QueueOperation(
                        entity,
                        EntityState.Added,
                        () => _databaseProvider.GenerateInsertSql(metadata),
                        parameters
                    );
                    _changeTracker.Track(entity, EntityState.Added);
                }
                else
                {
                    InsertEntity(entity, metadata);
                }
            }
        }
    }

    private void InsertEntity<T>(T entity, EntityMetadata metadata) where T : class
    {
        var sql = _databaseProvider.GenerateInsertSql(metadata);

        if (HasGeneratedId(metadata))
        {
            var parameters = ExtractParameters(entity, metadata, skipIdentityKeys: true);
            var id = _connection.QuerySingle<object>(sql, parameters);
            SetEntityId(entity, id, metadata);
        }
        else
        {
            var parameters = ExtractParameters(entity, metadata, skipIdentityKeys: false);
            _connection.Execute(sql, parameters);
        }

        _changeTracker.Track(entity, EntityState.Added);
    }

    /// <inheritdoc />
    public T? Find<T>(object id) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        if (id == null) throw new ArgumentNullException(nameof(id));

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = _databaseProvider.GenerateSelectByIdSql(metadata);
        var parameters = new { id };

        var entity = _connection.QueryFirstOrDefault<T>(sql, parameters);
        if (entity != null) _changeTracker.Track(entity, EntityState.Unchanged);
        return entity;
    }

    /// <inheritdoc />
    public T? Find<T>(CompositeKey key) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        if (key == null) throw new ArgumentNullException(nameof(key));

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = GenerateSelectByCompositeKeySql(metadata, key);
        var parameters = CreateParametersFromCompositeKey(key, metadata);

        var entity = _connection.QueryFirstOrDefault<T>(sql, parameters);
        if (entity != null) _changeTracker.Track(entity, EntityState.Unchanged);
        return entity;
    }

    /// <inheritdoc />
    public void Merge<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var metadata = _metadataProvider.GetEntityMetadata<T>();

        if (HasActiveTransaction)
        {
            // Queue for deferred execution - capture parameters now
            var parameters = ExtractParameters(entity, metadata, skipIdentityKeys: false);
            _changeTracker.QueueOperation(
                entity,
                EntityState.Modified,
                () => _databaseProvider.GenerateUpdateSql(metadata),
                parameters
            );
            _changeTracker.SetState(entity, EntityState.Modified);
        }
        else
        {
            // Immediate execution with internal transaction
            var sql = _databaseProvider.GenerateUpdateSql(metadata);
            var parameters = ExtractParameters(entity, metadata, skipIdentityKeys: false);

            using var transaction = _connection.BeginTransaction();
            try
            {
                _connection.Execute(sql, parameters, transaction);
                transaction.Commit();
                _changeTracker.SetState(entity, EntityState.Modified);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    /// <inheritdoc />
    public void Remove<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var metadata = _metadataProvider.GetEntityMetadata<T>();

        if (HasActiveTransaction)
        {
            // Queue for deferred execution - capture parameters now
            var parameters = ExtractIdParameters(entity, metadata);
            _changeTracker.QueueOperation(
                entity,
                EntityState.Deleted,
                () => _databaseProvider.GenerateDeleteSql(metadata),
                parameters
            );
            _changeTracker.SetState(entity, EntityState.Deleted);
        }
        else
        {
            // Immediate execution
            var sql = _databaseProvider.GenerateDeleteSql(metadata);
            var parameters = ExtractIdParameters(entity, metadata);
            _connection.Execute(sql, parameters);
            _changeTracker.SetState(entity, EntityState.Deleted);
        }
    }

    /// <inheritdoc />
    public void Remove<T>(object id) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        if (id == null) throw new ArgumentNullException(nameof(id));

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = _databaseProvider.GenerateDeleteSql(metadata);
        _connection.Execute(sql, new { id });
    }

    /// <inheritdoc />
    public void Remove<T>(CompositeKey key) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        if (key == null) throw new ArgumentNullException(nameof(key));

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = GenerateDeleteByCompositeKeySql(metadata, key);
        var parameters = CreateParametersFromCompositeKey(key, metadata);
        _connection.Execute(sql, parameters);
    }

    /// <inheritdoc />
    public void Flush()
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();

        // Execute queued operations if in a transaction
        if (HasActiveTransaction && _changeTracker.GetQueuedOperationCount() > 0)
        {
            var operations = _changeTracker.GetQueuedOperations();
            var transaction = _currentTransaction!.DbTransaction;

            foreach (var operation in operations)
            {
                var sql = operation.SqlGenerator();
                var parameters = operation.Parameters;

                if (operation.State == EntityState.Added && HasGeneratedIdForEntity(operation.Entity))
                {
                    // For INSERT with identity, retrieve generated ID
                    var id = _connection.QuerySingle<object>(sql, parameters, transaction);
                    SetEntityIdForEntity(operation.Entity, id);
                }
                else
                {
                    _connection.Execute(sql, parameters, transaction);
                }

                // Update entity state after execution
                if (operation.State == EntityState.Deleted)
                {
                    _changeTracker.Untrack(operation.Entity);
                }
                else
                {
                    _changeTracker.SetState(operation.Entity, EntityState.Unchanged);
                }
            }

            _changeTracker.ClearQueue();
            return;
        }

        // Fallback: Execute pending changes from change tracker (backward compatibility)
        var pendingChanges = _changeTracker.GetPendingChanges();
        foreach (var kvp in pendingChanges)
        {
            switch (kvp.Value)
            {
                case EntityState.Added: CallGenericMethod(nameof(InsertEntity), kvp.Key); break;
                case EntityState.Modified: CallGenericMethod(nameof(Merge), kvp.Key); break;
                case EntityState.Deleted: CallGenericMethod(nameof(Remove), kvp.Key); break;
            }
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        ThrowIfDisposed();
    _changeTracker.Clear();
    _changeTracker.ClearQueue();
    }

    /// <inheritdoc />
    public bool Contains<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        return entity != null && _changeTracker.GetState(entity) != EntityState.Detached;
    }

    /// <inheritdoc />
    public void Detach<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        if (entity != null) _changeTracker.Untrack(entity);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _changeTracker.Clear();
        _disposed = true;
    }

    private void ThrowIfDisposed() { if (_disposed) throw new ObjectDisposedException(nameof(EntityManager)); }

    private string GenerateSelectByCompositeKeySql(EntityMetadata metadata, CompositeKey key)
    {
        var columns = string.Join(", ", metadata.Properties.Values.Select(p => $"{p.ColumnName} AS \"{p.PropertyName}\""));
        var where = string.Join(" AND ", key.Values.Keys.Select(p => $"{metadata.Properties[p].ColumnName} = @{p}"));
        return $"SELECT {columns} FROM {metadata.FullTableName} WHERE {where}";
    }

    private string GenerateDeleteByCompositeKeySql(EntityMetadata metadata, CompositeKey key)
    {
        var where = string.Join(" AND ", key.Values.Keys.Select(p => $"{metadata.Properties[p].ColumnName} = @{p}"));
        return $"DELETE FROM {metadata.FullTableName} WHERE {where}";
    }

    private object ExtractParameters(object entity, EntityMetadata metadata, bool skipIdentityKeys = true)
    {
        var parameters = new Dictionary<string, object?>();
        foreach (var prop in metadata.Properties.Values)
        {
            if (skipIdentityKeys && prop.IsPrimaryKey && prop.GenerationType == GenerationType.Identity) continue;
            parameters[prop.PropertyName] = prop.PropertyInfo.GetValue(entity);
        }
        return parameters;
    }

    private object ExtractIdParameters(object entity, EntityMetadata metadata)
    {
        var idValue = metadata.Properties[metadata.PrimaryKeyProperty].PropertyInfo.GetValue(entity);
        return new Dictionary<string, object?> { { "id", idValue } };
    }

    private object CreateParametersFromCompositeKey(CompositeKey key, EntityMetadata metadata)
    {
        return key.Values;
    }

    private object? GetEntityId(object entity, EntityMetadata metadata)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata), "Entity metadata cannot be null");
        
        if (metadata.HasCompositeKey) return null;
        
        if (string.IsNullOrEmpty(metadata.PrimaryKeyProperty))
            throw new InvalidOperationException($"Entity {metadata.EntityType?.Name ?? "Unknown"} has no primary key property defined");
        
        if (!metadata.Properties.TryGetValue(metadata.PrimaryKeyProperty, out var prop))
        {
            var availableProps = string.Join(", ", metadata.Properties.Keys);
            throw new InvalidOperationException($"Primary key property '{metadata.PrimaryKeyProperty}' not found in metadata for entity {metadata.EntityType?.Name ?? "Unknown"}. Available properties: {availableProps}");
        }
        
        return prop.PropertyInfo.GetValue(entity);
    }

    private void SetEntityId(object entity, object id, EntityMetadata metadata)
    {
        var prop = metadata.Properties[metadata.PrimaryKeyProperty];
        if (!prop.PropertyInfo.CanWrite) return;

        object? valueToSet = id;
        if (id is IDictionary<string, object> dapperRow)
        {
            valueToSet = dapperRow.Values.FirstOrDefault();
        }

        if (valueToSet == null || valueToSet == DBNull.Value) return;

        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        if (valueToSet.GetType() != targetType)
        {
            valueToSet = Convert.ChangeType(valueToSet, targetType);
        }

        prop.PropertyInfo.SetValue(entity, valueToSet);
    }

    private bool HasGeneratedId(EntityMetadata metadata)
    {
        if (metadata.HasCompositeKey) return false;
        var idProperty = metadata.Properties[metadata.PrimaryKeyProperty];
        return idProperty.GenerationType != null && idProperty.GenerationType != GenerationType.None;
    }

    private bool IsTransient(object entity, EntityMetadata metadata)
    {
        if (metadata.HasCompositeKey) return true; // Always treat composite key entities as potentially new, let the DB handle conflicts.

        var idValue = GetEntityId(entity, metadata);
        if (idValue == null) return true;

        var idType = metadata.Properties[metadata.PrimaryKeyProperty].PropertyType;
        object? defaultValue = idType.IsValueType ? Activator.CreateInstance(idType) : null;

        return idValue.Equals(defaultValue);
    }

    private bool HasGeneratedIdForEntity(object entity)
    {
        var metadata = _metadataProvider.GetEntityMetadata(entity.GetType());
        return HasGeneratedId(metadata);
    }

    private void SetEntityIdForEntity(object entity, object id)
    {
        var metadata = _metadataProvider.GetEntityMetadata(entity.GetType());
        SetEntityId(entity, id, metadata);
    }

    private async Task CallGenericMethodAsync(string methodName, object entity)
    {
        var method = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { entity.GetType(), typeof(EntityMetadata) }, null);
        if (method == null) throw new InvalidOperationException($"Method {methodName} not found.");
        var genericMethod = method.MakeGenericMethod(entity.GetType());
        var task = (Task)genericMethod.Invoke(this, new[] { entity, _metadataProvider.GetEntityMetadata(entity.GetType()) })!;
        await task;
    }

    private void CallGenericMethod(string methodName, object entity)
    {
        var method = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { entity.GetType(), typeof(EntityMetadata) }, null);
        if (method == null) throw new InvalidOperationException($"Method {methodName} not found.");
        var genericMethod = method.MakeGenericMethod(entity.GetType());
        genericMethod.Invoke(this, new[] { entity, _metadataProvider.GetEntityMetadata(entity.GetType()) });
    }

    /// <inheritdoc />
    public IQuery<T> CreateQuery<T>(string cpql) where T : class
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(cpql)) throw new ArgumentException("CPQL query cannot be null or empty", nameof(cpql));

        var parser = new QueryParser();
        var dialect = _databaseProvider.GetType().Name.Replace("Provider", "");
        var sqlGenerator = new SqlGenerator(_metadataProvider, dialect, _logger as ILogger<SqlGenerator>);
        var parameterBinder = new ParameterBinder();

        return new Query<T>(_connection, parser, sqlGenerator, parameterBinder, _metadataProvider, cpql, _logger as ILogger<Query<T>>);
    }

    /// <inheritdoc />
    public Task<ITransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();

        if (_currentTransaction != null && _currentTransaction.IsActive)
        {
            throw new InvalidOperationException("A transaction is already active. Commit or rollback the current transaction before starting a new one.");
        }

        _currentTransaction = new Transaction(_connection, this, isolationLevel);
        return Task.FromResult<ITransaction>(_currentTransaction);
    }

    /// <inheritdoc />
    public ITransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();

        if (_currentTransaction != null && _currentTransaction.IsActive)
        {
            throw new InvalidOperationException("A transaction is already active. Commit or rollback the current transaction before starting a new one.");
        }

        _currentTransaction = new Transaction(_connection, this, isolationLevel);
        return _currentTransaction;
    }

    /// <inheritdoc />
    public ITransaction? GetCurrentTransaction()
    {
        ThrowIfDisposed();
        return _currentTransaction?.IsActive == true ? _currentTransaction : null;
    }

    /// <inheritdoc />
    public bool HasActiveTransaction => _currentTransaction?.IsActive == true;

    /// <inheritdoc />
    public async Task<int> BulkInsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        
        var entityList = entities.ToList();
        if (entityList.Count == 0) return 0;

        var metadata = _metadataProvider.GetEntityMetadata<T>();

        _logger?.LogInformation("Bulk inserting {Count} {EntityType} entities", entityList.Count, typeof(T).Name);

        var affectedRows = await _databaseProvider.BulkInsertAsync(_connection, entityList, metadata, cancellationToken);

        _logger?.LogInformation("Bulk insert completed. {AffectedRows} rows affected", affectedRows);

        return affectedRows;
    }

    /// <inheritdoc />
    public int BulkInsert<T>(IEnumerable<T> entities) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        
        var entityList = entities.ToList();
        if (entityList.Count == 0) return 0;

        var metadata = _metadataProvider.GetEntityMetadata<T>();

        _logger?.LogInformation("Bulk inserting {Count} {EntityType} entities", entityList.Count, typeof(T).Name);

        var affectedRows = _databaseProvider.BulkInsert(_connection, entityList, metadata);

        _logger?.LogInformation("Bulk insert completed. {AffectedRows} rows affected", affectedRows);

        return affectedRows;
    }

    /// <inheritdoc />
    public async Task<int> BulkUpdateAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        
        var entityList = entities.ToList();
        if (entityList.Count == 0) return 0;

        var metadata = _metadataProvider.GetEntityMetadata<T>();

        _logger?.LogInformation("Bulk updating {Count} {EntityType} entities", entityList.Count, typeof(T).Name);

        var affectedRows = await _databaseProvider.BulkUpdateAsync(_connection, entityList, metadata, cancellationToken);

        _logger?.LogInformation("Bulk update completed. {AffectedRows} rows affected", affectedRows);

        return affectedRows;
    }

    /// <inheritdoc />
    public int BulkUpdate<T>(IEnumerable<T> entities) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        
        var entityList = entities.ToList();
        if (entityList.Count == 0) return 0;

        var metadata = _metadataProvider.GetEntityMetadata<T>();

        _logger?.LogInformation("Bulk updating {Count} {EntityType} entities", entityList.Count, typeof(T).Name);

        var affectedRows = _databaseProvider.BulkUpdate(_connection, entityList, metadata);

        _logger?.LogInformation("Bulk update completed. {AffectedRows} rows affected", affectedRows);

        return affectedRows;
    }

    /// <inheritdoc />
    public async Task<int> BulkDeleteAsync<T>(IEnumerable<object> ids, CancellationToken cancellationToken = default) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        
        if (ids == null) throw new ArgumentNullException(nameof(ids));
        
        var idList = ids.ToList();
        if (idList.Count == 0) return 0;

        var metadata = _metadataProvider.GetEntityMetadata<T>();

        _logger?.LogInformation("Bulk deleting {Count} {EntityType} entities", idList.Count, typeof(T).Name);

        var affectedRows = await _databaseProvider.BulkDeleteAsync(_connection, idList, metadata, cancellationToken);

        _logger?.LogInformation("Bulk delete completed. {AffectedRows} rows affected", affectedRows);

        return affectedRows;
    }

    /// <inheritdoc />
    public int BulkDelete<T>(IEnumerable<object> ids) where T : class
    {
        ThrowIfDisposed();
        EnsureConnectionOpen();
        
        if (ids == null) throw new ArgumentNullException(nameof(ids));
        
        var idList = ids.ToList();
        if (idList.Count == 0) return 0;

        var metadata = _metadataProvider.GetEntityMetadata<T>();

        _logger?.LogInformation("Bulk deleting {Count} {EntityType} entities", idList.Count, typeof(T).Name);

        var affectedRows = _databaseProvider.BulkDelete(_connection, idList, metadata);

        _logger?.LogInformation("Bulk delete completed. {AffectedRows} rows affected", affectedRows);

        return affectedRows;
    }
    
    /// <summary>
    /// Auto-populates the TenantId property if the entity is multi-tenant.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity instance.</param>
    private void AutoPopulateTenantId<T>(T entity) where T : class
    {
        var multiTenantAttr = typeof(T).GetCustomAttribute<MultiTenantAttribute>();
        if (multiTenantAttr == null || !multiTenantAttr.AutoPopulateTenantId)
            return;
        
        if (_tenantProvider == null)
            return;
        
        var currentTenantId = _tenantProvider.GetCurrentTenantId();
        if (string.IsNullOrEmpty(currentTenantId))
        {
            if (multiTenantAttr.ValidateTenantOnWrite)
            {
                throw new InvalidOperationException(
                    $"Cannot persist multi-tenant entity {typeof(T).Name} without a current tenant context. " +
                    "Set a tenant using ITenantProvider.SetCurrentTenant() before persisting.");
            }
            return;
        }
        
        // Find and set the TenantId property
        var tenantProperty = typeof(T).GetProperty(multiTenantAttr.TenantIdProperty);
        if (tenantProperty == null)
        {
            throw new InvalidOperationException(
                $"Property '{multiTenantAttr.TenantIdProperty}' not found on entity type {typeof(T).Name}. " +
                "Ensure the property exists or update the [MultiTenant] attribute.");
        }
        
        var currentValue = tenantProperty.GetValue(entity) as string;
        if (string.IsNullOrEmpty(currentValue))
        {
            tenantProperty.SetValue(entity, currentTenantId);
            _logger?.LogDebug("Auto-populated TenantId '{TenantId}' for entity {EntityType}", currentTenantId, typeof(T).Name);
        }
    }
    
    /// <summary>
    /// Validates the TenantId property if the entity is multi-tenant.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity instance.</param>
    /// <exception cref="InvalidOperationException">Thrown when tenant validation fails.</exception>
    private void ValidateTenantId<T>(T entity) where T : class
    {
        var multiTenantAttr = typeof(T).GetCustomAttribute<MultiTenantAttribute>();
        if (multiTenantAttr == null || !multiTenantAttr.ValidateTenantOnWrite)
            return;
        
        if (_tenantProvider == null)
            return;
        
        var currentTenantId = _tenantProvider.GetCurrentTenantId();
        if (string.IsNullOrEmpty(currentTenantId))
        {
            throw new InvalidOperationException(
                $"Cannot update multi-tenant entity {typeof(T).Name} without a current tenant context. " +
                "Set a tenant using ITenantProvider.SetCurrentTenant() before updating.");
        }
        
        // Find and validate the TenantId property
        var tenantProperty = typeof(T).GetProperty(multiTenantAttr.TenantIdProperty);
        if (tenantProperty == null)
        {
            throw new InvalidOperationException(
                $"Property '{multiTenantAttr.TenantIdProperty}' not found on entity type {typeof(T).Name}. " +
                "Ensure the property exists or update the [MultiTenant] attribute.");
        }
        
        var entityTenantId = tenantProperty.GetValue(entity) as string;
        if (string.IsNullOrEmpty(entityTenantId))
        {
            throw new InvalidOperationException(
                $"TenantId property '{multiTenantAttr.TenantIdProperty}' is not set on entity {typeof(T).Name}. " +
                "Ensure the entity has a valid tenant before updating.");
        }
        
        if (!entityTenantId.Equals(currentTenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Cross-tenant update detected! Entity {typeof(T).Name} belongs to tenant '{entityTenantId}' " +
                $"but current tenant context is '{currentTenantId}'. Cross-tenant modifications are not allowed.");
        }
    }
}
