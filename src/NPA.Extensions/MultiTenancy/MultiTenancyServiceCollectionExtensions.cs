using Microsoft.Extensions.DependencyInjection;
using NPA.Core.MultiTenancy;

namespace NPA.Extensions.MultiTenancy;

/// <summary>
/// Extension methods for configuring multi-tenancy services.
/// </summary>
public static class MultiTenancyServiceCollectionExtensions
{
    /// <summary>
    /// Adds multi-tenancy support with in-memory tenant store.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMultiTenancy(this IServiceCollection services)
    {
        services.AddSingleton<ITenantProvider, AsyncLocalTenantProvider>();
        services.AddSingleton<ITenantStore, InMemoryTenantStore>();
        services.AddSingleton<TenantManager>();

        return services;
    }

    /// <summary>
    /// Adds multi-tenancy support with a custom tenant store.
    /// </summary>
    /// <typeparam name="TTenantStore">The tenant store implementation</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMultiTenancy<TTenantStore>(this IServiceCollection services)
        where TTenantStore : class, ITenantStore
    {
        services.AddSingleton<ITenantProvider, AsyncLocalTenantProvider>();
        services.AddSingleton<ITenantStore, TTenantStore>();
        services.AddSingleton<TenantManager>();

        return services;
    }

    /// <summary>
    /// Adds multi-tenancy support with a custom tenant provider and store.
    /// </summary>
    /// <typeparam name="TTenantProvider">The tenant provider implementation</typeparam>
    /// <typeparam name="TTenantStore">The tenant store implementation</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMultiTenancy<TTenantProvider, TTenantStore>(this IServiceCollection services)
        where TTenantProvider : class, ITenantProvider
        where TTenantStore : class, ITenantStore
    {
        services.AddSingleton<ITenantProvider, TTenantProvider>();
        services.AddSingleton<ITenantStore, TTenantStore>();
        services.AddSingleton<TenantManager>();

        return services;
    }
}
