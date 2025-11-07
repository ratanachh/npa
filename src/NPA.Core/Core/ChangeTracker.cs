using System.Reflection;
using NPA.Core.Metadata;

namespace NPA.Core.Core;

/// <summary>
/// Tracks changes to entities within the persistence context.
/// </summary>
public sealed class ChangeTracker : IChangeTracker
{
    private readonly Dictionary<object, EntityState> _trackedEntities = new();
    private readonly Dictionary<object, object> _originalValues = new();
    private readonly Queue<QueuedOperation> _operationQueue = new();

    /// <inheritdoc />
    public void Track<T>(T entity, EntityState state) where T : class
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // Check if an entity with the same ID is already tracked
        var idProperty = entity.GetType().GetProperty("Id");
        if (idProperty != null)
        {
            var entityId = idProperty.GetValue(entity);
            if (entityId != null)
            {
                var existingEntity = GetTrackedEntityById<T>(entityId);
                
                if (existingEntity != null)
                {
                    // Update the existing tracked entity with new values
                    CopyEntityValues(entity, existingEntity);
                    
                    // Update the state
                    _trackedEntities[existingEntity] = state;
                    
                    // Store original values for change detection
                    if (state == EntityState.Unchanged || state == EntityState.Added)
                    {
                        StoreOriginalValues(existingEntity);
                    }
                    
                    return;
                }
            }
        }

        // Track new entity
        _trackedEntities[entity] = state;

        // Store original values for change detection
        if (state == EntityState.Unchanged || state == EntityState.Added)
        {
            StoreOriginalValues(entity);
        }
    }

    /// <inheritdoc />
    public EntityState GetState<T>(T entity) where T : class
    {
        if (entity == null)
            return EntityState.Detached;

        return _trackedEntities.TryGetValue(entity, out var state) ? state : EntityState.Detached;
    }

    /// <inheritdoc />
    public void SetState<T>(T entity, EntityState state) where T : class
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (_trackedEntities.ContainsKey(entity))
        {
            _trackedEntities[entity] = state;
        }
        else
        {
            Track(entity, state);
        }
    }

    /// <inheritdoc />
    public void Untrack<T>(T entity) where T : class
    {
        if (entity == null)
            return;

        _trackedEntities.Remove(entity);
        _originalValues.Remove(entity);
    }

    /// <inheritdoc />
    public IEnumerable<object> GetTrackedEntities(EntityState state)
    {
        return _trackedEntities
            .Where(kvp => kvp.Value == state)
            .Select(kvp => kvp.Key);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _trackedEntities.Clear();
        _originalValues.Clear();
        _operationQueue.Clear();
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<object, EntityState> GetPendingChanges()
    {
        return _trackedEntities
            .Where(kvp => kvp.Value != EntityState.Unchanged)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Detects if an entity has been modified by comparing current values with original values.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity has been modified; otherwise, false.</returns>
    public bool IsModified(object entity)
    {
        if (entity == null || !_trackedEntities.ContainsKey(entity))
            return false;

        var currentState = _trackedEntities[entity];
        if (currentState != EntityState.Unchanged)
            return true;

        // Compare current values with original values
        if (!_originalValues.TryGetValue(entity, out var originalValues))
            return false;

        var entityType = entity.GetType();
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (!property.CanRead || !property.CanWrite)
                continue;

            var currentValue = property.GetValue(entity);
            var originalValue = GetOriginalValue(originalValues, property.Name);

            if (!Equals(currentValue, originalValue))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Updates the original values for an entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    public void UpdateOriginalValues(object entity)
    {
        if (entity == null)
            return;

        StoreOriginalValues(entity);
    }

    /// <summary>
    /// Finds a tracked entity by its ID.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The entity ID.</param>
    /// <returns>The tracked entity if found, otherwise null.</returns>
    public T? GetTrackedEntityById<T>(object id) where T : class
    {
        if (id == null)
            return null;

        var entityType = typeof(T);
        var idProperty = entityType.GetProperty("Id");

        if (idProperty == null)
            return null;

        foreach (var kvp in _trackedEntities)
        {
            if (kvp.Key.GetType() == entityType)
            {
                var entityId = idProperty.GetValue(kvp.Key);
                if (Equals(entityId, id))
                {
                    return (T)kvp.Key;
                }
            }
        }

        return null;
    }


    private void StoreOriginalValues(object entity)
    {
        var entityType = entity.GetType();
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var originalValues = new Dictionary<string, object?>();

        foreach (var property in properties)
        {
            if (!property.CanRead)
                continue;

            var value = property.GetValue(entity);
            originalValues[property.Name] = value;
        }

        _originalValues[entity] = originalValues;
    }

    private static object? GetOriginalValue(object originalValues, string propertyName)
    {
        if (originalValues is Dictionary<string, object?> dict &&
            dict.TryGetValue(propertyName, out var value))
        {
            return value;
        }

        return null;
    }


    /// <summary>
    /// Checks if an entity has changes (alias for IsModified for better readability).
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity has changes; otherwise, false.</returns>
    public bool HasChanges(object entity)
    {
        if (entity == null || !_trackedEntities.ContainsKey(entity))
            return false;

        var currentState = _trackedEntities[entity];
        
        // For Added entities, we need to check if they have been modified since being added
        if (currentState == EntityState.Added)
        {
            // Check if the entity has been modified since it was added
            if (!_originalValues.TryGetValue(entity, out var originalValues))
                return false;

            var entityType = entity.GetType();
            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (!property.CanRead || !property.CanWrite)
                    continue;

                var currentValue = property.GetValue(entity);
                var originalValue = GetOriginalValue(originalValues, property.Name);

                if (!Equals(currentValue, originalValue))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        // For other states, use the existing IsModified logic
        return IsModified(entity);
    }

    /// <summary>
    /// Copies property values from source entity to target entity.
    /// </summary>
    /// <param name="source">The source entity.</param>
    /// <param name="target">The target entity.</param>
    public void CopyEntityValues(object source, object target)
    {
        if (source == null || target == null)
            return;

        var sourceType = source.GetType();
        var targetType = target.GetType();

        if (sourceType != targetType)
            return;

        var properties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.CanRead && property.CanWrite)
            {
                var value = property.GetValue(source);
                property.SetValue(target, value);
            }
        }
    }

    /// <inheritdoc />
    public void QueueOperation(object entity, EntityState state, Func<string> sqlGenerator, object parameters)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        if (sqlGenerator == null)
            throw new ArgumentNullException(nameof(sqlGenerator));
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        var priority = GetOperationPriority(state);

        _operationQueue.Enqueue(new QueuedOperation
        {
            Entity = entity,
            State = state,
            SqlGenerator = sqlGenerator,
            Parameters = parameters,
            Priority = priority
        });
    }

    /// <inheritdoc />
    public IEnumerable<QueuedOperation> GetQueuedOperations()
    {
        // Return operations ordered by priority (INSERT → UPDATE → DELETE)
        return _operationQueue.OrderBy(op => op.Priority);
    }

    /// <inheritdoc />
    public void ClearQueue()
    {
        _operationQueue.Clear();
    }

    /// <inheritdoc />
    public int GetQueuedOperationCount()
    {
        return _operationQueue.Count;
    }

    private static int GetOperationPriority(EntityState state)
    {
        return state switch
        {
            EntityState.Added => 1,      // INSERT first
            EntityState.Modified => 2,   // UPDATE second
            EntityState.Deleted => 3,    // DELETE last
            _ => 0
        };
    }
}
