using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NPA.Core.Caching;

/// <summary>
/// In-memory cache provider using IMemoryCache.
/// Supports pattern-based operations through key tracking.
/// </summary>
public class MemoryCacheProvider : ICacheProvider
{
    private readonly IMemoryCache _memoryCache;
    private readonly CacheOptions _options;
    private readonly ConcurrentDictionary<string, byte> _keys;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCacheProvider"/> class.
    /// </summary>
    /// <param name="memoryCache">The memory cache instance.</param>
    /// <param name="options">Cache configuration options.</param>
    public MemoryCacheProvider(IMemoryCache memoryCache, IOptions<CacheOptions>? options = null)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _options = options?.Value ?? new CacheOptions();
        _keys = new ConcurrentDictionary<string, byte>();
    }

    /// <inheritdoc/>
    public Task<T?> GetAsync<T>(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        EnsureNotDisposed();

        var fullKey = GetFullKey(key);
        _memoryCache.TryGetValue(fullKey, out T? value);
        return Task.FromResult(value);
    }

    /// <inheritdoc/>
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        EnsureNotDisposed();

        var fullKey = GetFullKey(key);
        var exp = expiration ?? _options.DefaultExpiration;

        var cacheOptions = _options.UseSlidingExpiration
            ? new MemoryCacheEntryOptions { SlidingExpiration = exp }
            : new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = exp };

        // Register post eviction callback to remove from key tracking
        cacheOptions.RegisterPostEvictionCallback((k, v, r, s) =>
        {
            _keys.TryRemove(fullKey, out _);
        });

        _memoryCache.Set(fullKey, value, cacheOptions);
        _keys.TryAdd(fullKey, 0);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        EnsureNotDisposed();

        var fullKey = GetFullKey(key);
        _memoryCache.Remove(fullKey);
        _keys.TryRemove(fullKey, out _);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveByPatternAsync(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

        EnsureNotDisposed();

        var fullPattern = GetFullKey(pattern);
        var keysToRemove = _keys.Keys.Where(k => MatchesPattern(k, fullPattern)).ToList();

        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ClearAsync()
    {
        EnsureNotDisposed();

        var keysToRemove = _keys.Keys.ToList();
        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        EnsureNotDisposed();

        var fullKey = GetFullKey(key);
        var exists = _memoryCache.TryGetValue(fullKey, out _);
        return Task.FromResult(exists);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<string>> GetKeysAsync(string pattern = "*")
    {
        EnsureNotDisposed();

        var fullPattern = GetFullKey(pattern);
        var matchingKeys = _keys.Keys
            .Where(k => MatchesPattern(k, fullPattern))
            .Select(k => RemovePrefix(k))
            .ToList();

        return Task.FromResult<IEnumerable<string>>(matchingKeys);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _keys.Clear();
        _disposed = true;
    }

    private string GetFullKey(string key)
    {
        return key.StartsWith(_options.KeyPrefix) ? key : _options.KeyPrefix + key;
    }

    private string RemovePrefix(string key)
    {
        return key.StartsWith(_options.KeyPrefix) ? key.Substring(_options.KeyPrefix.Length) : key;
    }

    private bool MatchesPattern(string key, string pattern)
    {
        // Simple wildcard matching (* at end)
        if (pattern.EndsWith("*"))
        {
            var prefix = pattern.Substring(0, pattern.Length - 1);
            return key.StartsWith(prefix);
        }

        return key == pattern;
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MemoryCacheProvider));
    }
}
