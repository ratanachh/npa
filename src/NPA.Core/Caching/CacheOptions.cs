using System;

namespace NPA.Core.Caching;

/// <summary>
/// Configuration options for caching behavior.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Gets or sets the default expiration time for cached items.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the maximum size of the cache (in bytes).
    /// Null means no limit.
    /// </summary>
    public long? SizeLimit { get; set; }

    /// <summary>
    /// Gets or sets whether to enable cache statistics.
    /// </summary>
    public bool EnableStatistics { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache key prefix for all cache entries.
    /// </summary>
    public string KeyPrefix { get; set; } = "npa:";

    /// <summary>
    /// Gets or sets whether to enable sliding expiration.
    /// When enabled, cache expiration resets on each access.
    /// </summary>
    public bool UseSlidingExpiration { get; set; } = false;
}
