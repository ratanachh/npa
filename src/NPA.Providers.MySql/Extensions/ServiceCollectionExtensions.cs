using Microsoft.Extensions.DependencyInjection;
using NPA.Core.Core;
using NPA.Core.Extensions;
using NPA.Core.Metadata;
using NPA.Core.Providers;
using MySqlConnector;
using System.Data;

namespace NPA.Providers.MySql.Extensions;

/// <summary>
/// Extension methods for configuring MySQL provider with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds NPA with MySQL provider to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The MySQL connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNpaMySql(this IServiceCollection services, string connectionString)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        // Register core services
        services.AddNpaMetadataProvider(); // Uses generated provider if available for 10-100x performance
        services.AddSingleton<IDatabaseProvider, MySqlProvider>();
        
        // Register MySQL connection
        services.AddScoped<IDbConnection>(sp =>
        {
            var connection = new MySqlConnection(connectionString);
            connection.Open();
            return connection;
        });

        // Register EntityManager
        services.AddScoped<IEntityManager, EntityManager>();

        return services;
    }

    /// <summary>
    /// Adds NPA with MySQL provider to the service collection using a connection factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionFactory">Factory function to create MySQL connections.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNpaMySql(this IServiceCollection services, Func<IServiceProvider, IDbConnection> connectionFactory)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (connectionFactory == null)
            throw new ArgumentNullException(nameof(connectionFactory));

        // Register core services
        services.AddNpaMetadataProvider(); // Uses generated provider if available for 10-100x performance
        services.AddSingleton<IDatabaseProvider, MySqlProvider>();
        
        // Register MySQL connection with factory
        services.AddScoped(connectionFactory);

        // Register EntityManager
        services.AddScoped<IEntityManager, EntityManager>();

        return services;
    }

    /// <summary>
    /// Adds only the MySQL provider to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMySqlProvider(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        services.AddSingleton<IDatabaseProvider, MySqlProvider>();
        
        return services;
    }
}

