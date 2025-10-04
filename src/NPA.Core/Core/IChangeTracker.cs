using NPA.Core.Metadata;

namespace NPA.Core.Core;

/// <summary>
/// Tracks changes to entities within the persistence context.
/// </summary>
public interface IChangeTracker
{
    /// <summary>
    /// Tracks an entity with the specified state.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to track.</param>
    /// <param name="state">The initial state of the entity.</param>
    void Track<T>(T entity, EntityState state) where T : class;

    /// <summary>
    /// Gets the current state of an entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>The current state of the entity, or Detached if not tracked.</returns>
    EntityState GetState<T>(T entity) where T : class;

    /// <summary>
    /// Updates the state of an entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="state">The new state.</param>
    void SetState<T>(T entity, EntityState state) where T : class;

    /// <summary>
    /// Removes an entity from tracking.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to stop tracking.</param>
    void Untrack<T>(T entity) where T : class;

    /// <summary>
    /// Gets all tracked entities with the specified state.
    /// </summary>
    /// <param name="state">The entity state to filter by.</param>
    /// <returns>A collection of tracked entities.</returns>
    IEnumerable<object> GetTrackedEntities(EntityState state);

    /// <summary>
    /// Clears all tracked entities.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets all pending changes.
    /// </summary>
    /// <returns>A dictionary of entities and their states.</returns>
    IReadOnlyDictionary<object, EntityState> GetPendingChanges();

    /// <summary>
    /// Detects if an entity has been modified by comparing current values with original values.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity has been modified; otherwise, false.</returns>
    bool IsModified(object entity);

    /// <summary>
    /// Finds a tracked entity by its ID.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The entity ID.</param>
    /// <returns>The tracked entity if found, otherwise null.</returns>
    T? GetTrackedEntityById<T>(object id) where T : class;

    /// <summary>
    /// Checks if an entity has changes (alias for IsModified for better readability).
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity has changes; otherwise, false.</returns>
    bool HasChanges(object entity);

    /// <summary>
    /// Copies property values from source entity to target entity.
    /// </summary>
    /// <param name="source">The source entity.</param>
    /// <param name="target">The target entity.</param>
    void CopyEntityValues(object source, object target);
}
