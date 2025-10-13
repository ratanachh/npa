using System.Data;
using System.Reflection;
using Dapper;
using Microsoft.Extensions.Logging;
using NPA.Core.Annotations;
using NPA.Core.Metadata;
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
    private readonly ILogger<EntityManager>? _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityManager"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="metadataProvider">The metadata provider.</param>
    /// <param name="databaseProvider">The database provider.</param>
    /// <param name="logger">The logger (optional).</param>
    public EntityManager(IDbConnection connection, IMetadataProvider metadataProvider, IDatabaseProvider databaseProvider, ILogger<EntityManager>? logger = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
        _databaseProvider = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
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
        var entityId = GetEntityId(entity, metadata);
        
        // If entity already has an ID, check if it exists in the database
        if (entityId != null)
        {
            var existingEntity = await FindAsync<T>(entityId);
            if (existingEntity != null)
            {
                // Entity exists, perform UPDATE
                var sql = _databaseProvider.GenerateUpdateSql(metadata);
                var parameters = ExtractParameters(entity, metadata, skipIdentityKeys: false); // Include primary key for UPDATE

                try
                {
                    var rowsAffected = await _connection.ExecuteAsync(sql, parameters);
                    if (rowsAffected > 0)
                    {
                        _changeTracker.Track(entity, EntityState.Modified);
                        _logger?.LogDebug("Successfully updated existing entity of type {EntityType} with ID {Id}", typeof(T).Name, entityId);
                    }
                    else
                    {
                        _changeTracker.Track(entity, EntityState.Unchanged);
                        _logger?.LogDebug("No changes made to entity of type {EntityType} with ID {Id}", typeof(T).Name, entityId);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error updating entity of type {EntityType}", typeof(T).Name);
                    throw;
                }
            }
            else
            {
                // Entity has ID but doesn't exist, perform INSERT
                await InsertEntityAsync(entity, metadata);
            }
        }
        else
        {
            // Entity has no ID, perform INSERT
            await InsertEntityAsync(entity, metadata);
        }
    }

    private async Task InsertEntityAsync<T>(T entity, EntityMetadata metadata) where T : class
    {
        var sql = "";
        object? parameters;
        if (HasGeneratedId(metadata))
        {
            // Entity has auto-generated ID (Identity, Sequence, etc.)
            sql = _databaseProvider.GenerateInsertSql(metadata);
            parameters = ExtractParameters(entity, metadata);

            try
            {
                var id = await _connection.QuerySingleAsync<object>(sql, parameters);
                SetEntityId(entity, id, metadata);
                _changeTracker.Track(entity, EntityState.Added);

                _logger?.LogDebug("Successfully inserted entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error inserting entity of type {EntityType}", typeof(T).Name);
                throw;
            }
        }
        else
        {

            sql = _databaseProvider.GenerateInsertSql(metadata);
            parameters = ExtractParameters(entity, metadata, skipIdentityKeys: false); // Include key column

            try
            {
                await _connection.ExecuteAsync(sql, parameters);
                _changeTracker.Track(entity, EntityState.Added);

                _logger?.LogDebug(
                    metadata.HasCompositeKey
                        ? "Successfully inserted entity of type {EntityType} with composite key"
                        : "Successfully inserted entity of type {EntityType} with manual key", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error inserting entity of type {EntityType}", typeof(T).Name);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<T?> FindAsync<T>(object id) where T : class
    {
        ThrowIfDisposed();
        if (id == null) throw new ArgumentNullException(nameof(id));

        _logger?.LogDebug("Finding entity of type {EntityType} with ID {Id}", typeof(T).Name, id);

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = _databaseProvider.GenerateSelectByIdSql(metadata);
        var parameters = new { id };

        try
        {
            // Query as dynamic first to get raw values
            var rawEntity = await _connection.QueryFirstOrDefaultAsync(sql, parameters);
            if (rawEntity == null)
            {
                _logger?.LogDebug("Entity of type {EntityType} with ID {Id} not found", typeof(T).Name, id);
                return null;
            }

            // Manually map to entity to handle PostgreSQL boolean values and case differences
            var entity = Activator.CreateInstance<T>();
            var rawDict = (IDictionary<string, object>)rawEntity;
            // Build a case-insensitive view of the row to handle PostgreSQL folding of unquoted aliases
            var ciDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in rawDict)
                ciDict[kv.Key] = kv.Value;

            foreach (var prop in metadata.Properties.Values)
            {
                var entityProp = typeof(T).GetProperty(prop.PropertyName);
                // Use PropertyName as key since we alias columns as PropertyName; match case-insensitively for providers like PostgreSQL
                if (entityProp != null && entityProp.CanWrite && ciDict.TryGetValue(prop.PropertyName, out var rawValue))
                {
                    // Handle PostgreSQL boolean string mapping
                    if ((prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?)) && rawValue is string boolStr)
                    {
                        var boolValue = bool.Parse(boolStr);
                        entityProp.SetValue(entity, boolValue);
                    }
                    else
                    {
                        // Convert numeric types when needed (e.g., decimal -> long)
                        if (entityProp.PropertyType == typeof(long) && rawValue is decimal dec)
                        {
                            entityProp.SetValue(entity, Convert.ToInt64(dec));
                        }
                        else
                        {
                            entityProp.SetValue(entity, rawValue);
                        }
                    }
                }
            }

            // Check if entity is already tracked and return the tracked version
            var trackedEntity = _changeTracker.GetTrackedEntityById<T>(id);
            if (trackedEntity != null)
            {
                // Update the tracked entity with fresh data from database
                _changeTracker.CopyEntityValues(entity, trackedEntity);
                _changeTracker.SetState(trackedEntity, EntityState.Unchanged);
                entity = trackedEntity;
            }
            else
            {
                // Track new entity
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
            _logger?.LogDebug("Entity state: {State}, HasChanges: {HasChanges}", currentState, _changeTracker.HasChanges(entity));
            if (currentState == EntityState.Detached)
            {
                // Entity not tracked, try to find it first
                var id = GetEntityId(entity, metadata);
                if (id != null)
                {
                    var existingEntity = await FindAsync<T>(id);
                    if (existingEntity != null)
                    {
                        // Copy the modified values to the tracked entity
                        _changeTracker.CopyEntityValues(entity, existingEntity);
                        _changeTracker.SetState(existingEntity, EntityState.Modified);
                        entity = existingEntity; // Use the tracked entity for the update
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
            else
            {
                // Entity is already tracked, check if it has been modified
                if (currentState == EntityState.Unchanged)
                {
                    // Check if the entity has been modified since it was tracked
                    if (_changeTracker.HasChanges(entity))
                    {
                        _changeTracker.SetState(entity, EntityState.Modified);
                        _logger?.LogDebug("Entity of type {EntityType} has been modified, updating state to Modified", typeof(T).Name);
                    }
                    else
                    {
                        _logger?.LogDebug("Entity of type {EntityType} has no changes, skipping update", typeof(T).Name);
                        return;
                    }
                }
                else if (currentState == EntityState.Added)
                {
                    // If entity is in Added state, it means it was just persisted
                    // Check if it has been modified since persistence
                    if (_changeTracker.HasChanges(entity))
                    {
                        _changeTracker.SetState(entity, EntityState.Modified);
                        _logger?.LogDebug("Entity of type {EntityType} has been modified after persistence, updating state to Modified", typeof(T).Name);
                    }
                    else
                    {
                        _logger?.LogDebug("Entity of type {EntityType} has no changes after persistence, skipping update", typeof(T).Name);
                        return;
                    }
                }
                else
                {
                    _logger?.LogDebug("Entity of type {EntityType} is already in state {State}, proceeding with update", typeof(T).Name, currentState);
                }
            }

            var sql = _databaseProvider.GenerateUpdateSql(metadata);
            var parameters = ExtractParameters(entity, metadata, skipIdentityKeys: false); // Include primary key for UPDATE

            // Debug logging
            Console.WriteLine($"Generated UPDATE SQL: {sql}");
            if (parameters is Dictionary<string, object?> paramDict)
            {
                Console.WriteLine($"Parameters: {string.Join(", ", paramDict.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
            }
            
            // Additional debug logging for the specific test case
            Console.WriteLine($"Entity before update - IsActive: {entity.GetType().GetProperty("IsActive")?.GetValue(entity)}, Email: {entity.GetType().GetProperty("Email")?.GetValue(entity)}");
                
            // Log the actual SQL execution
            _logger?.LogDebug("Executing UPDATE with parameters: {Parameters}", parameters);

           try
           {
               // Use a transaction to ensure the UPDATE is committed
               using var transaction = _connection.BeginTransaction();
               try
               {
                   var rowsAffected = await _connection.ExecuteAsync(sql, parameters, transaction);
                   _logger?.LogDebug("UPDATE executed, rows affected: {RowsAffected}", rowsAffected);
                   
                   if (rowsAffected == 0)
                   {
                       _logger?.LogWarning("No rows were affected by the UPDATE operation");
                   }
                   
                   // Commit the transaction
                   transaction.Commit();
                   
                   _changeTracker.SetState(entity, EntityState.Modified);

                   _logger?.LogDebug("Successfully merged entity of type {EntityType}", typeof(T).Name);
                   
                   // Debug logging after update
                   _logger?.LogDebug("Entity after update - IsActive: {IsActive}, Email: {Email}", 
                       entity.GetType().GetProperty("IsActive")?.GetValue(entity),
                       entity.GetType().GetProperty("Email")?.GetValue(entity));
               }
               catch
               {
                   transaction.Rollback();
                   throw;
               }
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
        var sql = _databaseProvider.GenerateDeleteSql(metadata);
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
        var sql = _databaseProvider.GenerateDeleteSql(metadata);
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
    public async Task RemoveAsync<T>(CompositeKey key) where T : class
    {
        ThrowIfDisposed();
        if (key == null) throw new ArgumentNullException(nameof(key));

        _logger?.LogDebug("Removing entity of type {EntityType} with composite key", typeof(T).Name);

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = GenerateDeleteByCompositeKeySql(metadata, key);
        var parameters = CreateParametersFromCompositeKey(key, metadata);

        try
        {
            await _connection.ExecuteAsync(sql, parameters);

            _logger?.LogDebug("Successfully removed entity of type {EntityType} with composite key", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing entity of type {EntityType} with composite key", typeof(T).Name);
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
                        await ExecuteInsertAsync(entity);
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

    #region Synchronous Methods

    /// <inheritdoc />
    public void Persist<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        _logger?.LogDebug("Persisting entity of type {EntityType} (sync)", typeof(T).Name);

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var entityId = GetEntityId(entity, metadata);
        
        // If entity already has an ID, check if it exists in the database
        if (entityId != null)
        {
            var existingEntity = Find<T>(entityId);
            if (existingEntity != null)
            {
                // Entity exists, perform UPDATE
                var sql = _databaseProvider.GenerateUpdateSql(metadata);
                var parameters = ExtractParameters(entity, metadata, skipIdentityKeys: false); // Include primary key for UPDATE

                try
                {
                    var rowsAffected = _connection.Execute(sql, parameters);
                    if (rowsAffected > 0)
                    {
                        _changeTracker.Track(entity, EntityState.Modified);
                        _logger?.LogDebug("Successfully updated existing entity of type {EntityType} with ID {Id} (sync)", typeof(T).Name, entityId);
                    }
                    else
                    {
                        _changeTracker.Track(entity, EntityState.Unchanged);
                        _logger?.LogDebug("No changes made to entity of type {EntityType} with ID {Id} (sync)", typeof(T).Name, entityId);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error updating entity of type {EntityType} (sync)", typeof(T).Name);
                    throw;
                }
            }
            else
            {
                // Entity has ID but doesn't exist, perform INSERT
                InsertEntity(entity, metadata);
            }
        }
        else
        {
            // Entity has no ID, perform INSERT
            InsertEntity(entity, metadata);
        }
    }

    private void InsertEntity<T>(T entity, EntityMetadata metadata) where T : class
    {
        var sql = "";
        object? parameters;
        if (HasGeneratedId(metadata))
        {
            // Entity has auto-generated ID (Identity, Sequence, etc.)
            sql = _databaseProvider.GenerateInsertSql(metadata);
            parameters = ExtractParameters(entity, metadata);

            try
            {
                var id = _connection.QuerySingle<object>(sql, parameters);
                SetEntityId(entity, id, metadata);
                _changeTracker.Track(entity, EntityState.Added);

                _logger?.LogDebug("Successfully inserted entity of type {EntityType} with ID {Id} (sync)", typeof(T).Name, id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error inserting entity of type {EntityType} (sync)", typeof(T).Name);
                throw;
            }
        }
        else
        {
            sql = _databaseProvider.GenerateInsertSql(metadata);
            parameters = ExtractParameters(entity, metadata, skipIdentityKeys: false); // Include key column

            try
            {
                _connection.Execute(sql, parameters);
                _changeTracker.Track(entity, EntityState.Added);

                _logger?.LogDebug(
                    metadata.HasCompositeKey
                        ? "Successfully inserted entity of type {EntityType} with manual key (sync)"
                        : "Successfully inserted entity of type {EntityType} with composite key (sync)",
                    typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error inserting entity of type {EntityType} (sync)", typeof(T).Name);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public T? Find<T>(object id) where T : class
    {
        ThrowIfDisposed();
        if (id == null) throw new ArgumentNullException(nameof(id));

        _logger?.LogDebug("Finding entity of type {EntityType} with ID {Id} (sync)", typeof(T).Name, id);

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = _databaseProvider.GenerateSelectByIdSql(metadata);
        var parameters = new { id };

        try
        {
            // Query as dynamic first to get raw values
            var rawEntity = _connection.QueryFirstOrDefault(sql, parameters);
            if (rawEntity == null)
            {
                _logger?.LogDebug("Entity of type {EntityType} with ID {Id} not found (sync)", typeof(T).Name, id);
                return null;
            }

            // Manually map to entity to handle PostgreSQL boolean values
            var entity = Activator.CreateInstance<T>();
            var rawDict = (IDictionary<string, object>)rawEntity;
            
            foreach (var prop in metadata.Properties.Values)
            {
                var entityProp = typeof(T).GetProperty(prop.PropertyName);
                // Use PropertyName as key since we now alias columns in SELECT (e.g., column_name AS PropertyName)
                if (entityProp != null && entityProp.CanWrite && rawDict.TryGetValue(prop.PropertyName, out var rawValue))
                {
                    // Handle PostgreSQL boolean string mapping
                    if ((prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?)) && rawValue is string boolStr)
                    {
                        var boolValue = bool.Parse(boolStr);
                        entityProp.SetValue(entity, boolValue);
                    }
                    else
                    {
                        entityProp.SetValue(entity, rawValue);
                    }
                }
            }

            // Check if entity is already tracked and return the tracked version
            var trackedEntity = _changeTracker.GetTrackedEntityById<T>(id);
            if (trackedEntity != null)
            {
                // Update the tracked entity with fresh data from database
                _changeTracker.CopyEntityValues(entity, trackedEntity);
                _changeTracker.SetState(trackedEntity, EntityState.Unchanged);
                entity = trackedEntity;
            }
            else
            {
                // Track new entity
                _changeTracker.Track(entity, EntityState.Unchanged);
            }

            _logger?.LogDebug("Found entity of type {EntityType} with ID {Id} (sync): {Found}", typeof(T).Name, id, entity != null);
            return entity;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error finding entity of type {EntityType} with ID {Id} (sync)", typeof(T).Name, id);
            throw;
        }
    }

    /// <inheritdoc />
    public T? Find<T>(CompositeKey key) where T : class
    {
        ThrowIfDisposed();
        if (key == null) throw new ArgumentNullException(nameof(key));

        _logger?.LogDebug("Finding entity of type {EntityType} with composite key (sync)", typeof(T).Name);

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = GenerateSelectByCompositeKeySql(metadata, key);
        var parameters = CreateParametersFromCompositeKey(key, metadata);

        try
        {
            var entity = _connection.QueryFirstOrDefault<T>(sql, parameters);
            if (entity != null)
            {
                _changeTracker.Track(entity, EntityState.Unchanged);
            }

            _logger?.LogDebug("Found entity of type {EntityType} with composite key (sync): {Found}", typeof(T).Name, entity != null);
            return entity;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error finding entity of type {EntityType} with composite key (sync)", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public void Merge<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        _logger?.LogDebug("Merging entity of type {EntityType} (sync)", typeof(T).Name);

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        
        // Check if entity is already tracked
        var currentState = _changeTracker.GetState(entity);
        _logger?.LogDebug("Entity state: {State}, HasChanges: {HasChanges}", currentState, _changeTracker.HasChanges(entity));
        if (currentState == EntityState.Detached)
        {
            // Entity not tracked, try to find it first
            var id = GetEntityId(entity, metadata);
            if (id != null)
            {
                var existingEntity = Find<T>(id);
                if (existingEntity != null)
                {
                    // Copy the modified values to the tracked entity
                    _changeTracker.CopyEntityValues(entity, existingEntity);
                    _changeTracker.SetState(existingEntity, EntityState.Modified);
                    entity = existingEntity; // Use the tracked entity for the update
                }
                else
                {
                    Persist(entity);
                    return;
                }
            }
            else
            {
                Persist(entity);
                return;
            }
        }
        else
        {
            // Entity is already tracked, check if it has been modified
            if (currentState == EntityState.Unchanged)
            {
                // Check if the entity has been modified since it was tracked
                if (_changeTracker.HasChanges(entity))
                {
                    _changeTracker.SetState(entity, EntityState.Modified);
                    _logger?.LogDebug("Entity of type {EntityType} has been modified, updating state to Modified (sync)", typeof(T).Name);
                }
                else
                {
                    _logger?.LogDebug("Entity of type {EntityType} has no changes, skipping update (sync)", typeof(T).Name);
                    return;
                }
            }
            else if (currentState == EntityState.Added)
            {
                // If entity is in Added state, it means it was just persisted
                // Check if it has been modified since persistence
                if (_changeTracker.HasChanges(entity))
                {
                    _changeTracker.SetState(entity, EntityState.Modified);
                    _logger?.LogDebug("Entity of type {EntityType} has been modified after persistence, updating state to Modified (sync)", typeof(T).Name);
                }
                else
                {
                    _logger?.LogDebug("Entity of type {EntityType} has no changes after persistence, skipping update (sync)", typeof(T).Name);
                    return;
                }
            }
            else
            {
                _logger?.LogDebug("Entity of type {EntityType} is already in state {State}, proceeding with update (sync)", typeof(T).Name, currentState);
            }
        }

        var sql = _databaseProvider.GenerateUpdateSql(metadata);
        var parameters = ExtractParameters(entity, metadata, skipIdentityKeys: false); // Include primary key for UPDATE

        try
        {
            // Use a transaction to ensure the UPDATE is committed
            using var transaction = _connection.BeginTransaction();
            try
            {
                var rowsAffected = _connection.Execute(sql, parameters, transaction);
                _logger?.LogDebug("UPDATE executed, rows affected: {RowsAffected} (sync)", rowsAffected);
                
                if (rowsAffected == 0)
                {
                    _logger?.LogWarning("No rows were affected by the UPDATE operation (sync)");
                }
                
                // Commit the transaction
                transaction.Commit();
                
                _changeTracker.SetState(entity, EntityState.Modified);

                _logger?.LogDebug("Successfully merged entity of type {EntityType} (sync)", typeof(T).Name);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error merging entity of type {EntityType} (sync)", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public void Remove<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        _logger?.LogDebug("Removing entity of type {EntityType} (sync)", typeof(T).Name);

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = _databaseProvider.GenerateDeleteSql(metadata);
        var parameters = ExtractIdParameters(entity, metadata);

        try
        {
            _connection.Execute(sql, parameters);
            _changeTracker.SetState(entity, EntityState.Deleted);

            _logger?.LogDebug("Successfully removed entity of type {EntityType} (sync)", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing entity of type {EntityType} (sync)", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public void Remove<T>(object id) where T : class
    {
        ThrowIfDisposed();
        if (id == null) throw new ArgumentNullException(nameof(id));

        _logger?.LogDebug("Removing entity of type {EntityType} with ID {Id} (sync)", typeof(T).Name, id);

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = _databaseProvider.GenerateDeleteSql(metadata);
        var parameters = new { id };

        try
        {
            _connection.Execute(sql, parameters);

            _logger?.LogDebug("Successfully removed entity of type {EntityType} with ID {Id} (sync)", typeof(T).Name, id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing entity of type {EntityType} with ID {Id} (sync)", typeof(T).Name, id);
            throw;
        }
    }

    /// <inheritdoc />
    public void Remove<T>(CompositeKey key) where T : class
    {
        ThrowIfDisposed();
        if (key == null) throw new ArgumentNullException(nameof(key));

        _logger?.LogDebug("Removing entity of type {EntityType} with composite key (sync)", typeof(T).Name);

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = GenerateDeleteByCompositeKeySql(metadata, key);
        var parameters = CreateParametersFromCompositeKey(key, metadata);

        try
        {
            _connection.Execute(sql, parameters);

            _logger?.LogDebug("Successfully removed entity of type {EntityType} with composite key (sync)", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing entity of type {EntityType} with composite key (sync)", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        ThrowIfDisposed();

        _logger?.LogDebug("Flushing pending changes (sync)");

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
                        ExecuteInsert(entity);
                        break;
                    case EntityState.Modified:
                        CallGenericMethod(nameof(Merge), entity);
                        break;
                    case EntityState.Deleted:
                        CallGenericMethod(nameof(Remove), entity);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error flushing entity of type {EntityType} with state {State} (sync)", entity.GetType().Name, state);
                throw;
            }
        }

        _logger?.LogDebug("Successfully flushed {Count} pending changes (sync)", pendingChanges.Count);
    }

    private void ExecuteInsert(object entity)
    {
        var entityType = entity.GetType();
        var metadata = _metadataProvider.GetEntityMetadata(entityType);
        var sql = _databaseProvider.GenerateInsertSql(metadata);
        var parameters = ExtractParameters(entity, metadata);

        try
        {
            _logger?.LogDebug("Executing SQL: {Sql} (sync)", sql);
            _logger?.LogDebug("Parameters: {Parameters} (sync)", string.Join(", ", ((Dictionary<string, object?>)parameters).Select(kvp => $"{kvp.Key}={kvp.Value}")));
            
            var id = _connection.QuerySingle<object>(sql, parameters);
            SetEntityId(entity, id, metadata);
            _changeTracker.SetState(entity, EntityState.Unchanged);

            _logger?.LogDebug("Successfully executed insert for entity of type {EntityType} with ID {Id} (sync)", entityType.Name, id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing insert for entity of type {EntityType} (sync)", entityType.Name);
            throw;
        }
    }

    private void CallGenericMethod(string methodName, object entity)
    {
        var entityType = entity.GetType();
        
        // Get the method with specific signature to avoid ambiguity
        var method = GetType().GetMethod(methodName, new[] { entityType });
        if (method == null)
            throw new InvalidOperationException($"Method {methodName} not found with entity parameter");

        var genericMethod = method.MakeGenericMethod(entityType);
        genericMethod.Invoke(this, new[] { entity });
    }

    /// <inheritdoc />
    public void Clear()
    {
        ThrowIfDisposed();

        _logger?.LogDebug("Clearing persistence context (sync)");

        _changeTracker.Clear();

        _logger?.LogDebug("Successfully cleared persistence context (sync)");
    }

    #endregion

    /// <inheritdoc />
    public bool Contains<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        if (entity == null) return false;

        return _changeTracker.GetState(entity) != EntityState.Detached;
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
            // Note: Do NOT dispose the connection - it's injected and we don't own its lifecycle
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

    private string GenerateSelectByCompositeKeySql(EntityMetadata metadata, CompositeKey key)
    {
        // Quote property names to preserve case (important for PostgreSQL)
        var columns = metadata.Properties.Values.Select(p => $"{p.ColumnName} AS \"{p.PropertyName}\"");
        var columnList = string.Join(", ", columns);

        var whereConditions = key.Values.Keys
            .Select(propertyName => metadata.Properties[propertyName].ColumnName + " = @" + propertyName);

        var whereClause = string.Join(" AND ", whereConditions);

        return $"SELECT {columnList} FROM {metadata.FullTableName} WHERE {whereClause}";
    }

    private string GenerateDeleteByCompositeKeySql(EntityMetadata metadata, CompositeKey key)
    {
        var whereConditions = key.Values.Keys
            .Select(propertyName => metadata.Properties[propertyName].ColumnName + " = @" + propertyName);

        var whereClause = string.Join(" AND ", whereConditions);

        return $"DELETE FROM {metadata.FullTableName} WHERE {whereClause}";
    }

    private bool HasGeneratedId(EntityMetadata metadata)
    {
        // For composite keys, do NOT treat as having a generated ID.
        // Even if one part is generated, we can't handle composite identity retrieval here.
        // We always take the explicit insert path and include provided key values.
        if (metadata.HasCompositeKey)
            return false;
        
        // For single keys, check the primary key property
        var idProperty = metadata.Properties[metadata.PrimaryKeyProperty];
        return idProperty.GenerationType != null && 
               idProperty.GenerationType != GenerationType.None;
    }

    private async Task ExecuteInsertAsync(object entity)
    {
        var entityType = entity.GetType();
        var metadata = _metadataProvider.GetEntityMetadata(entityType);
        var sql = _databaseProvider.GenerateInsertSql(metadata);
        var parameters = ExtractParameters(entity, metadata);

        try
        {
            _logger?.LogDebug("Executing SQL: {Sql}", sql);
            _logger?.LogDebug("Parameters: {Parameters}", string.Join(", ", ((Dictionary<string, object?>)parameters).Select(kvp => $"{kvp.Key}={kvp.Value}")));
            
            var id = await _connection.QuerySingleAsync<object>(sql, parameters);
            SetEntityId(entity, id, metadata);
            _changeTracker.SetState(entity, EntityState.Unchanged);

            _logger?.LogDebug("Successfully executed insert for entity of type {EntityType} with ID {Id}", entityType.Name, id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing insert for entity of type {EntityType}", entityType.Name);
            throw;
        }
    }

    private object ExtractParameters(object entity, EntityMetadata metadata, bool skipIdentityKeys = true)
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
                // Skip identity columns only for INSERT operations
                if (skipIdentityKeys && propertyMetadata.IsPrimaryKey && propertyMetadata.GenerationType == Annotations.GenerationType.Identity)
                {
                    continue;
                }

                var value = property.GetValue(entity);
                // Use property name as key to match the @PropertyName format in SQL
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

            if (id != null && idProperty.PropertyType == typeof(long) && id is decimal dec)
            {
                idProperty.SetValue(entity, Convert.ToInt64(dec));
            }
            else
            {
                idProperty.SetValue(entity, id);
            }
        }
    }

    private async Task CallGenericMethodAsync(string methodName, object entity)
    {
        var entityType = entity.GetType();
        
        // Get the method with specific signature to avoid ambiguity
        var method = GetType().GetMethod(methodName, new[] { entityType });
        if (method == null)
            throw new InvalidOperationException($"Method {methodName} not found with entity parameter");

        var genericMethod = method.MakeGenericMethod(entityType);
        var task = (Task)genericMethod.Invoke(this, new[] { entity })!;
        await task;
    }

    /// <inheritdoc />
    public IQuery<T> CreateQuery<T>(string cpql) where T : class
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(cpql))
            throw new ArgumentException("CPQL query cannot be null or empty", nameof(cpql));

        _logger?.LogDebug("Creating query for entity type {EntityType} with CPQL: {Cpql}", typeof(T).Name, cpql);

        // Set up enhanced parser with CPQL support
        var parser = new QueryParser();
        
        // Determine database dialect for SQL generation
        var dialect = _databaseProvider.GetType().Name.Replace("Provider", "");
        
        // Create SqlGenerator with logger for SQL/parameter logging
        // Note: SqlGenerator doesn't have a direct logger parameter, it will be added via Query
        var sqlGenerator = new SqlGenerator(_metadataProvider, dialect, null);
        
        var parameterBinder = new ParameterBinder();

        // Register entity for advanced queries
        var entityResolver = new EntityResolver(_metadataProvider);
        entityResolver.RegisterEntity(typeof(T).Name, typeof(T));

        // Create Query with logger - this will show SQL and parameters
        return new Query<T>(_connection, parser, sqlGenerator, parameterBinder, _metadataProvider, cpql, _logger as ILogger<Query<T>>);
    }
}
