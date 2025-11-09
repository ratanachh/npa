using System;
using System.Linq;
using System.Text;

namespace NPA.Core.Caching;

/// <summary>
/// Generates cache keys for entities and queries.
/// </summary>
public class CacheKeyGenerator
{
    private readonly string _prefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheKeyGenerator"/> class.
    /// </summary>
    /// <param name="prefix">The key prefix.</param>
    public CacheKeyGenerator(string prefix = "npa:")
    {
        _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
    }

    /// <summary>
    /// Generates a cache key for an entity by its ID.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="id">The entity ID.</param>
    /// <returns>The generated cache key.</returns>
    public string GenerateEntityKey<TEntity, TKey>(TKey id)
    {
        var entityName = typeof(TEntity).Name.ToLowerInvariant();
        return $"{_prefix}entity:{entityName}:{id}";
    }

    /// <summary>
    /// Generates a cache key for a query.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="query">The query string or identifier.</param>
    /// <param name="parameters">Query parameters.</param>
    /// <returns>The generated cache key.</returns>
    public string GenerateQueryKey<TEntity>(string query, params object[] parameters)
    {
        var entityName = typeof(TEntity).Name.ToLowerInvariant();
        var sb = new StringBuilder();
        sb.Append(_prefix);
        sb.Append("query:");
        sb.Append(entityName);
        sb.Append(":");
        sb.Append(query);

        if (parameters != null && parameters.Length > 0)
        {
            sb.Append(":");
            sb.Append(string.Join(":", parameters.Select(p => p?.ToString() ?? "null")));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates a cache key for a region pattern.
    /// </summary>
    /// <param name="region">The cache region.</param>
    /// <returns>The generated cache key pattern.</returns>
    public string GenerateRegionPattern(string region)
    {
        if (string.IsNullOrEmpty(region))
            throw new ArgumentException("Region cannot be null or empty", nameof(region));

        return $"{_prefix}region:{region}:*";
    }

    /// <summary>
    /// Generates a cache key for all entities of a specific type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>The generated cache key pattern.</returns>
    public string GenerateEntityPattern<TEntity>()
    {
        var entityName = typeof(TEntity).Name.ToLowerInvariant();
        return $"{_prefix}entity:{entityName}:*";
    }

    /// <summary>
    /// Generates a custom cache key.
    /// </summary>
    /// <param name="keyParts">The parts to combine into a key.</param>
    /// <returns>The generated cache key.</returns>
    public string GenerateKey(params string[] keyParts)
    {
        if (keyParts == null || keyParts.Length == 0)
            throw new ArgumentException("Key parts cannot be null or empty", nameof(keyParts));

        return $"{_prefix}{string.Join(":", keyParts)}";
    }
}
