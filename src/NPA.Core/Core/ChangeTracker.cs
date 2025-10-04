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

    /// <inheritdoc />
    public void Track<T>(T entity, EntityState state) where T : class
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _trackedEntities[entity] = state;

        // Store original values for change detection
        if (state == EntityState.Unchanged)
        {
            StoreOriginalValues(entity);
        }
    }

    /// <inheritdoc />
    public EntityState? GetState<T>(T entity) where T : class
    {
        if (entity == null)
            return null;

        return _trackedEntities.TryGetValue(entity, out var state) ? state : null;
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
}
