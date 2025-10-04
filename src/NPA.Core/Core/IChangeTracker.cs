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
    /// <returns>The current state of the entity, or null if not tracked.</returns>
    EntityState? GetState<T>(T entity) where T : class;

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
}
