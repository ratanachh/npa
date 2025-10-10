using NPA.Core.Metadata;
using NPA.Core.Query;

namespace NPA.Core.Core;

/// <summary>
/// Provides entity lifecycle management functionality.
/// </summary>
public interface IEntityManager : IDisposable
{
    /// <summary>
    /// Persists a new entity to the database asynchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to persist.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PersistAsync<T>(T entity) where T : class;

    /// <summary>
    /// Persists a new entity to the database synchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to persist.</param>
    void Persist<T>(T entity) where T : class;

    /// <summary>
    /// Finds an entity by its primary key asynchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The primary key value.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the found entity, or null if not found.</returns>
    Task<T?> FindAsync<T>(object id) where T : class;

    /// <summary>
    /// Finds an entity by its primary key synchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The primary key value.</param>
    /// <returns>The found entity, or null if not found.</returns>
    T? Find<T>(object id) where T : class;

    /// <summary>
    /// Finds an entity by its composite key asynchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="key">The composite key.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the found entity, or null if not found.</returns>
    Task<T?> FindAsync<T>(CompositeKey key) where T : class;

    /// <summary>
    /// Finds an entity by its composite key synchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="key">The composite key.</param>
    /// <returns>The found entity, or null if not found.</returns>
    T? Find<T>(CompositeKey key) where T : class;

    /// <summary>
    /// Merges changes from an entity into the database asynchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to merge.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MergeAsync<T>(T entity) where T : class;

    /// <summary>
    /// Merges changes from an entity into the database synchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to merge.</param>
    void Merge<T>(T entity) where T : class;

    /// <summary>
    /// Removes an entity from the database asynchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAsync<T>(T entity) where T : class;

    /// <summary>
    /// Removes an entity from the database synchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to remove.</param>
    void Remove<T>(T entity) where T : class;

    /// <summary>
    /// Removes an entity by its primary key asynchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The primary key value.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAsync<T>(object id) where T : class;

    /// <summary>
    /// Removes an entity by its primary key synchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The primary key value.</param>
    void Remove<T>(object id) where T : class;

    /// <summary>
    /// Removes an entity by its composite key asynchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="key">The composite key.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAsync<T>(CompositeKey key) where T : class;

    /// <summary>
    /// Removes an entity by its composite key synchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="key">The composite key.</param>
    void Remove<T>(CompositeKey key) where T : class;

    /// <summary>
    /// Flushes all pending changes to the database asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FlushAsync();

    /// <summary>
    /// Flushes all pending changes to the database synchronously.
    /// </summary>
    void Flush();

    /// <summary>
    /// Clears the persistence context asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearAsync();

    /// <summary>
    /// Clears the persistence context synchronously.
    /// </summary>
    void Clear();

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

    /// <summary>
    /// Creates a query for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="cpql">The CPQL query string.</param>
    /// <returns>A query instance.</returns>
    IQuery<T> CreateQuery<T>(string cpql) where T : class;
}
