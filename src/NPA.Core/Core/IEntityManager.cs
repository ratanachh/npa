using NPA.Core.Metadata;

namespace NPA.Core.Core;

/// <summary>
/// Provides entity lifecycle management functionality.
/// </summary>
public interface IEntityManager : IDisposable
{
    /// <summary>
    /// Persists a new entity to the database.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to persist.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PersistAsync<T>(T entity) where T : class;

    /// <summary>
    /// Finds an entity by its primary key.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The primary key value.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the found entity, or null if not found.</returns>
    Task<T?> FindAsync<T>(object id) where T : class;

    /// <summary>
    /// Finds an entity by its composite key.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="key">The composite key.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the found entity, or null if not found.</returns>
    Task<T?> FindAsync<T>(CompositeKey key) where T : class;

    /// <summary>
    /// Merges changes from an entity into the database.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to merge.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MergeAsync<T>(T entity) where T : class;

    /// <summary>
    /// Removes an entity from the database.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAsync<T>(T entity) where T : class;

    /// <summary>
    /// Removes an entity by its primary key.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The primary key value.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAsync<T>(object id) where T : class;

    /// <summary>
    /// Flushes all pending changes to the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FlushAsync();

    /// <summary>
    /// Clears the persistence context.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearAsync();

    /// <summary>
    /// Checks if an entity is currently managed by the persistence context.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity is managed; otherwise, false.</returns>
    bool Contains<T>(T entity) where T : class;

    /// <summary>
    /// Detaches an entity from the persistence context.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to detach.</param>
    void Detach<T>(T entity) where T : class;

    /// <summary>
    /// Gets the metadata provider.
    /// </summary>
    IMetadataProvider MetadataProvider { get; }

    /// <summary>
    /// Gets the change tracker.
    /// </summary>
    IChangeTracker ChangeTracker { get; }
}
