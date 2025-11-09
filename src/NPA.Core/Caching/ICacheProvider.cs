using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NPA.Core.Caching;

/// <summary>
/// Defines the contract for cache operations in the NPA library.
/// </summary>
public interface ICacheProvider : IDisposable
{
    /// <summary>
    /// Gets a cached value by key.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <returns>The cached value, or default if not found.</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Sets a value in the cache.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">Optional expiration time.</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Removes a cached value by key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    Task RemoveAsync(string key);

    /// <summary>
    /// Removes all cached values matching the specified pattern.
    /// </summary>
    /// <param name="pattern">The key pattern (e.g., "users:*").</param>
    Task RemoveByPatternAsync(string pattern);

    /// <summary>
    /// Clears all cached values.
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Gets all keys matching the specified pattern.
    /// </summary>
    /// <param name="pattern">The key pattern (default is "*" for all keys).</param>
    /// <returns>Collection of matching keys.</returns>
    Task<IEnumerable<string>> GetKeysAsync(string pattern = "*");
}
