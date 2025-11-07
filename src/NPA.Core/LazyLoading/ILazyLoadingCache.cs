namespace NPA.Core.LazyLoading;

/// <summary>
/// Defines the contract for lazy loading cache.
/// Provides caching for lazily loaded entities to avoid repeated database queries.
/// </summary>
public interface ILazyLoadingCache
{
    /// <summary>
    /// Adds a value to the cache for the specified entity and property.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="value">The value to cache.</param>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    /// <exception cref="ArgumentException">Thrown when propertyName is null or empty.</exception>
    void Add<T>(object entity, string propertyName, T value);

    /// <summary>
    /// Gets a value from the cache for the specified entity and property.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The cached value, or default if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    /// <exception cref="ArgumentException">Thrown when propertyName is null or empty.</exception>
    T? Get<T>(object entity, string propertyName);

    /// <summary>
    /// Tries to get a value from the cache for the specified entity and property.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="value">The cached value if found.</param>
    /// <returns>True if the value was found in cache; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    /// <exception cref="ArgumentException">Thrown when propertyName is null or empty.</exception>
    bool TryGet<T>(object entity, string propertyName, out T? value);

    /// <summary>
    /// Removes a value from the cache for the specified entity and property.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    /// <exception cref="ArgumentException">Thrown when propertyName is null or empty.</exception>
    void Remove(object entity, string propertyName);

    /// <summary>
    /// Removes all cached values for the specified entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    void Remove(object entity);

    /// <summary>
    /// Clears all cached values.
    /// </summary>
    void Clear();

    /// <summary>
    /// Checks if the cache contains a value for the specified entity and property.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>True if the cache contains the value; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    /// <exception cref="ArgumentException">Thrown when propertyName is null or empty.</exception>
    bool Contains(object entity, string propertyName);
}
