using System;

namespace NPA.Core.Caching;

/// <summary>
/// Attribute to mark methods or classes for caching.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class CacheAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the cache key pattern.
    /// Can include parameter placeholders like {0}, {1}, etc.
    /// </summary>
    public string? KeyPattern { get; set; }

    /// <summary>
    /// Gets or sets the cache expiration duration in seconds.
    /// </summary>
    public int ExpirationSeconds { get; set; } = 300; // 5 minutes default

    /// <summary>
    /// Gets or sets the cache region for logical partitioning.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets whether to use sliding expiration.
    /// </summary>
    public bool UseSlidingExpiration { get; set; } = false;

    /// <summary>
    /// Gets the expiration as a TimeSpan.
    /// </summary>
    public TimeSpan Expiration => TimeSpan.FromSeconds(ExpirationSeconds);
}
