using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NPA.Core.Caching;

/// <summary>
/// A no-op cache provider for testing or when caching is disabled.
/// All operations return immediately without storing data.
/// </summary>
public class NullCacheProvider : ICacheProvider
{
    /// <summary>
    /// Always returns the default value for the type.
    /// </summary>
    public Task<T?> GetAsync<T>(string key)
    {
        return Task.FromResult<T?>(default);
    }

    /// <summary>
    /// Does nothing.
    /// </summary>
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Does nothing.
    /// </summary>
    public Task RemoveAsync(string key)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Does nothing.
    /// </summary>
    public Task RemoveByPatternAsync(string pattern)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Does nothing.
    /// </summary>
    public Task ClearAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Always returns false.
    /// </summary>
    public Task<bool> ExistsAsync(string key)
    {
        return Task.FromResult(false);
    }

    /// <summary>
    /// Always returns an empty collection.
    /// </summary>
    public Task<IEnumerable<string>> GetKeysAsync(string pattern = "*")
    {
        return Task.FromResult(Enumerable.Empty<string>());
    }

    /// <summary>
    /// Does nothing.
    /// </summary>
    public void Dispose()
    {
        // No resources to dispose
    }
}
