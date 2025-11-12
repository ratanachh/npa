using Microsoft.Extensions.DependencyInjection;
using NPA.Core.Configuration;
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
        return AddNpaMySql(services, connectionString, null);
    }

    /// <summary>
    /// Adds NPA with MySQL provider to the service collection with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The MySQL connection string.</param>
    /// <param name="configure">Optional configuration action for MySQL options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNpaMySql(this IServiceCollection services, string connectionString, Action<MySqlOptions>? configure)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        var options = new MySqlOptions();
        configure?.Invoke(options);

        // Register options
        services.AddSingleton(options);

        // Build effective connection string with pooling configuration
        var effectiveConnectionString = BuildConnectionString(connectionString, options);

        // Register core services
        services.AddNpaMetadataProvider(); // Uses generated provider if available for 10-100x performance
        services.AddSingleton<IDatabaseProvider, MySqlProvider>();
        
        // Register MySQL connection
        services.AddScoped<IDbConnection>(_ => new MySqlConnection(effectiveConnectionString));

        // Register EntityManager
        services.AddScoped<IEntityManager, EntityManager>();

        return services;
    }

    /// <summary>
    /// Builds an effective connection string with pooling and other configuration options.
    /// </summary>
    private static string BuildConnectionString(string baseConnectionString, MySqlOptions options)
    {
        var builder = new MySqlConnectionStringBuilder(baseConnectionString);

        // Apply connection pooling options
        builder.Pooling = options.Pooling.Enabled;
        builder.MinimumPoolSize = (uint)options.Pooling.MinPoolSize;
        builder.MaximumPoolSize = (uint)options.Pooling.MaxPoolSize;

        if (options.Pooling.ConnectionLifetime.HasValue)
            builder.ConnectionLifeTime = (uint)options.Pooling.ConnectionLifetime.Value.TotalSeconds;

        if (options.Pooling.IdleTimeout.HasValue)
            builder.ConnectionIdleTimeout = (uint)options.Pooling.IdleTimeout.Value.TotalSeconds;

        // Apply connection timeout
        builder.ConnectionTimeout = (uint)options.Pooling.ConnectionTimeout.TotalSeconds;

        // Apply command timeout
        if (options.CommandTimeout.HasValue)
            builder.DefaultCommandTimeout = (uint)options.CommandTimeout.Value;

        // Apply other MySQL-specific options
        builder.ConnectionReset = options.Pooling.ResetOnReturn;

        if (options.AllowLoadLocalInfile.HasValue)
            builder.AllowLoadLocalInfile = options.AllowLoadLocalInfile.Value;

        if (options.AllowUserVariables.HasValue)
            builder.AllowUserVariables = options.AllowUserVariables.Value;

        if (!string.IsNullOrEmpty(options.CharacterSet))
            builder.CharacterSet = options.CharacterSet;

        if (options.UseCompression.HasValue)
            builder.UseCompression = options.UseCompression.Value;

        if (!string.IsNullOrEmpty(options.SslMode))
        {
            if (Enum.TryParse<MySqlSslMode>(options.SslMode, true, out var sslMode))
                builder.SslMode = sslMode;
        }

        builder.AllowPublicKeyRetrieval = options.AllowPublicKeyRetrieval;

        return builder.ConnectionString;
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

/// <summary>
/// Configuration options for MySQL provider.
/// </summary>
public class MySqlOptions
{
    /// <summary>
    /// Gets or sets the connection pooling options.
    /// Configure Min/Max pool size, connection lifetime, and idle timeout for optimal performance.
    /// </summary>
    public ConnectionPoolOptions Pooling { get; set; } = new();

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    public int? CommandTimeout { get; set; }

    /// <summary>
    /// Gets or sets the maximum batch size for bulk operations.
    /// </summary>
    public int MaxBatchSize { get; set; } = 5000;

    /// <summary>
    /// Gets or sets a value indicating whether to allow LOAD DATA LOCAL INFILE.
    /// </summary>
    public bool? AllowLoadLocalInfile { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to allow user variables.
    /// </summary>
    public bool? AllowUserVariables { get; set; }

    /// <summary>
    /// Gets or sets the character set for the connection.
    /// </summary>
    public string? CharacterSet { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use compression.
    /// </summary>
    public bool? UseCompression { get; set; }

    /// <summary>
    /// Gets or sets the SSL mode for connections.
    /// </summary>
    public string? SslMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to allow public key retrieval for RSA authentication.
    /// </summary>
    public bool AllowPublicKeyRetrieval { get; set; } = false;
}

