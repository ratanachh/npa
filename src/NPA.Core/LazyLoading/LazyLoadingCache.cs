using System.Collections.Concurrent;

namespace NPA.Core.LazyLoading;

/// <summary>
/// Thread-safe implementation of lazy loading cache.
/// Uses ConcurrentDictionary for thread-safe caching of loaded entities.
/// </summary>
public class LazyLoadingCache : ILazyLoadingCache
{
    private readonly ConcurrentDictionary<string, object?> _cache = new();

    /// <inheritdoc />
    public void Add<T>(object entity, string propertyName, T value)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

        var key = CreateKey(entity, propertyName);
        _cache[key] = value;
    }

    /// <inheritdoc />
    public T? Get<T>(object entity, string propertyName)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

        var key = CreateKey(entity, propertyName);
        return _cache.TryGetValue(key, out var value) ? (T?)value : default;
    }

    /// <inheritdoc />
    public bool TryGet<T>(object entity, string propertyName, out T? value)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

        var key = CreateKey(entity, propertyName);
        if (_cache.TryGetValue(key, out var cachedValue))
        {
            value = (T?)cachedValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public void Remove(object entity, string propertyName)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

        var key = CreateKey(entity, propertyName);
        _cache.TryRemove(key, out _);
    }

    /// <inheritdoc />
    public void Remove(object entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var entityKey = CreateEntityKey(entity);
        var keysToRemove = _cache.Keys.Where(k => k.StartsWith(entityKey, StringComparison.Ordinal)).ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        _cache.Clear();
    }

    /// <inheritdoc />
    public bool Contains(object entity, string propertyName)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

        var key = CreateKey(entity, propertyName);
        return _cache.ContainsKey(key);
    }

    private static string CreateKey(object entity, string propertyName)
    {
        var entityKey = CreateEntityKey(entity);
        return $"{entityKey}:{propertyName}";
    }

    private static string CreateEntityKey(object entity)
    {
        var entityType = entity.GetType();
        var hashCode = entity.GetHashCode();
        return $"{entityType.FullName}:{hashCode}";
    }
}
