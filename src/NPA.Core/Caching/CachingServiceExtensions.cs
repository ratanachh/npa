using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace NPA.Core.Caching;

/// <summary>
/// Extension methods for configuring caching in dependency injection.
/// </summary>
public static class CachingServiceExtensions
{
    /// <summary>
    /// Adds NPA caching services using in-memory caching.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNpaMemoryCache(
        this IServiceCollection services,
        Action<CacheOptions>? configureOptions = null)
    {
        services.AddMemoryCache();
        
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<CacheOptions>(options => { });
        }

        services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
        services.AddSingleton<CacheKeyGenerator>();

        return services;
    }

    /// <summary>
    /// Adds a null cache provider (no caching).
    /// Useful for testing or when caching is disabled.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNpaNullCache(this IServiceCollection services)
    {
        services.AddSingleton<ICacheProvider, NullCacheProvider>();
        services.AddSingleton<CacheKeyGenerator>();

        return services;
    }
}
