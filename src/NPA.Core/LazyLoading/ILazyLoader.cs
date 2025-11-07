namespace NPA.Core.LazyLoading;

/// <summary>
/// Defines the contract for lazy loading operations.
/// Provides methods to lazily load related entities and collections.
/// </summary>
public interface ILazyLoader
{
    /// <summary>
    /// Lazily loads a related entity for the specified property.
    /// </summary>
    /// <typeparam name="T">The type of the related entity.</typeparam>
    /// <param name="entity">The parent entity.</param>
    /// <param name="propertyName">The name of the property to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded related entity, or null if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    /// <exception cref="ArgumentException">Thrown when propertyName is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when relationship is not found.</exception>
    Task<T?> LoadAsync<T>(object entity, string propertyName, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Lazily loads a related collection for the specified property.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="entity">The parent entity.</param>
    /// <param name="propertyName">The name of the property to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded related collection, or empty collection if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    /// <exception cref="ArgumentException">Thrown when propertyName is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when relationship is not found.</exception>
    Task<IEnumerable<T>> LoadCollectionAsync<T>(object entity, string propertyName, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Checks if the specified property has been loaded.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>True if the property has been loaded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    /// <exception cref="ArgumentException">Thrown when propertyName is null or empty.</exception>
    bool IsLoaded(object entity, string propertyName);

    /// <summary>
    /// Marks the specified property as loaded.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    /// <exception cref="ArgumentException">Thrown when propertyName is null or empty.</exception>
    void MarkAsLoaded(object entity, string propertyName);

    /// <summary>
    /// Marks the specified property as not loaded and removes it from cache.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    /// <exception cref="ArgumentException">Thrown when propertyName is null or empty.</exception>
    void MarkAsNotLoaded(object entity, string propertyName);

    /// <summary>
    /// Clears all cached lazy loaded entities.
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Clears all cached lazy loaded entities for the specified entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    void ClearCache(object entity);

    /// <summary>
    /// Clears the cached lazy loaded entity for the specified property.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    /// <exception cref="ArgumentException">Thrown when propertyName is null or empty.</exception>
    void ClearCache(object entity, string propertyName);
}
