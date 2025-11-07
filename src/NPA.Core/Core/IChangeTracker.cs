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

    /// <summary>
    /// Queues an operation for deferred execution.
    /// </summary>
    /// <param name="entity">The entity for the operation.</param>
    /// <param name="state">The entity state (Added, Modified, Deleted).</param>
    /// <param name="sqlGenerator">A function that generates the SQL for this operation.</param>
    /// <param name="parameters">The parameters for the SQL query (captured at queue time).</param>
    /// <remarks>
    /// Operations are queued when a transaction is active and executed in batch during Flush.
    /// This enables better performance through reduced database round-trips.
    /// Parameters are captured at queue time to avoid closure issues.
    /// </remarks>
    void QueueOperation(object entity, EntityState state, Func<string> sqlGenerator, object parameters);

    /// <summary>
    /// Gets all queued operations ordered by priority.
    /// </summary>
    /// <returns>A collection of queued operations ordered by priority (INSERT → UPDATE → DELETE).</returns>
    /// <remarks>
    /// Operations are ordered to ensure referential integrity:
    /// - INSERT operations first (priority 1)
    /// - UPDATE operations second (priority 2)
    /// - DELETE operations last (priority 3)
    /// </remarks>
    IEnumerable<QueuedOperation> GetQueuedOperations();

    /// <summary>
    /// Clears all queued operations.
    /// </summary>
    /// <remarks>
    /// This is called after Flush executes the operations or on transaction rollback.
    /// </remarks>
    void ClearQueue();

    /// <summary>
    /// Gets the count of queued operations.
    /// </summary>
    /// <returns>The number of operations currently queued for execution.</returns>
    int GetQueuedOperationCount();
}

/// <summary>
/// Represents an operation queued for deferred execution.
/// </summary>
public class QueuedOperation
{
    /// <summary>
    /// Gets or sets the entity for this operation.
    /// </summary>
    public object Entity { get; set; } = null!;

    /// <summary>
    /// Gets or sets the entity state (Added, Modified, Deleted).
    /// </summary>
    public EntityState State { get; set; }

    /// <summary>
    /// Gets or sets the SQL generator function.
    /// </summary>
    public Func<string> SqlGenerator { get; set; } = null!;

    /// <summary>
    /// Gets or sets the parameters for the SQL query (captured at queue time, not generated lazily).
    /// </summary>
    public object Parameters { get; set; } = null!;

    /// <summary>
    /// Gets or sets the priority (1 = INSERT, 2 = UPDATE, 3 = DELETE).
    /// </summary>
    public int Priority { get; set; }
}

