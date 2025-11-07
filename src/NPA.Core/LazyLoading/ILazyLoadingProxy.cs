namespace NPA.Core.LazyLoading;

/// <summary>
/// Defines the contract for lazy loading proxies.
/// Provides tracking of loaded properties for entities with lazy-loaded relationships.
/// </summary>
public interface ILazyLoadingProxy
{
    /// <summary>
    /// Gets the underlying entity.
    /// </summary>
    object Entity { get; }

    /// <summary>
    /// Gets the lazy loader for this proxy.
    /// </summary>
    ILazyLoader LazyLoader { get; }

    /// <summary>
    /// Checks if the specified property has been loaded.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>True if the property has been loaded; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when propertyName is null or empty.</exception>
    bool IsLoaded(string propertyName);

    /// <summary>
    /// Loads the specified property lazily.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when propertyName is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when property is not found.</exception>
    Task LoadAsync(string propertyName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the specified property as loaded.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <exception cref="ArgumentException">Thrown when propertyName is null or empty.</exception>
    void MarkAsLoaded(string propertyName);

    /// <summary>
    /// Marks the specified property as not loaded.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <exception cref="ArgumentException">Thrown when propertyName is null or empty.</exception>
    void MarkAsNotLoaded(string propertyName);
}
