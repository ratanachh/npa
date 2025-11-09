namespace NPA.Core.Annotations;

/// <summary>
/// Indicates that the result of a method should be cached automatically.
/// The generator will wrap the method implementation with caching logic.
/// </summary>
/// <example>
/// <code>
/// [CacheResult(Duration = 300)]  // Cache for 5 minutes
/// Task&lt;User?&gt; GetByIdAsync(int id);
/// 
/// [CacheResult(Duration = 60, KeyPattern = "user:email:{email}")]
/// Task&lt;User?&gt; FindByEmailAsync(string email);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class CacheResultAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the cache duration in seconds.
    /// Default is 300 seconds (5 minutes).
    /// </summary>
    public int Duration { get; set; } = 300;

    /// <summary>
    /// Gets or sets the cache key pattern.
    /// Use {paramName} to include parameter values in the key.
    /// If not specified, a key will be generated from the method name and parameters.
    /// </summary>
    /// <example>
    /// "user:id:{id}" or "products:category:{categoryId}:page:{page}"
    /// </example>
    public string? KeyPattern { get; set; }

    /// <summary>
    /// Gets or sets the cache region for organizing cached items.
    /// Default is the entity type name.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets whether to cache null results.
    /// Default is false (don't cache nulls).
    /// </summary>
    public bool CacheNulls { get; set; }

    /// <summary>
    /// Gets or sets the cache priority.
    /// Higher priority items are less likely to be evicted.
    /// Default is 0 (normal priority).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets whether to use sliding expiration.
    /// If true, the cache duration resets on each access.
    /// Default is false (absolute expiration).
    /// </summary>
    public bool SlidingExpiration { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheResultAttribute"/> class.
    /// </summary>
    public CacheResultAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheResultAttribute"/> class with a duration.
    /// </summary>
    /// <param name="duration">The cache duration in seconds.</param>
    public CacheResultAttribute(int duration)
    {
        Duration = duration;
    }
}
