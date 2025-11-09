using Microsoft.Extensions.DependencyInjection;
using NPA.Monitoring.Audit;

namespace NPA.Monitoring.Extensions;

/// <summary>
/// Extension methods for configuring monitoring services.
/// </summary>
public static class MonitoringServiceCollectionExtensions
{
    /// <summary>
    /// Adds performance monitoring services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPerformanceMonitoring(this IServiceCollection services)
    {
        services.AddSingleton<IMetricCollector, InMemoryMetricCollector>();
        return services;
    }

    /// <summary>
    /// Adds audit logging services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuditLogging(this IServiceCollection services)
    {
        services.AddSingleton<IAuditStore, InMemoryAuditStore>();
        return services;
    }

    /// <summary>
    /// Adds both performance monitoring and audit logging services.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMonitoring(this IServiceCollection services)
    {
        services.AddPerformanceMonitoring();
        services.AddAuditLogging();
        return services;
    }
}
