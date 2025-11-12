using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Core.Configuration;
using NPA.Core.Core;
using NPA.Core.Extensions;
using NPA.Core.Metadata;
using NPA.Core.Providers;
using Npgsql;
using System.Data;

namespace NPA.Providers.PostgreSql.Extensions;

/// <summary>
/// Extension methods for configuring PostgreSQL provider services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds PostgreSQL database provider services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgreSqlProvider(this IServiceCollection services, string connectionString)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        // Register PostgreSQL-specific services
        services.AddSingleton<ISqlDialect, PostgreSqlDialect>();
        services.AddSingleton<ITypeConverter, PostgreSqlTypeConverter>();
        services.AddSingleton<IBulkOperationProvider>(provider =>
        {
            var dialect = provider.GetRequiredService<ISqlDialect>();
            var typeConverter = provider.GetRequiredService<ITypeConverter>();
            return new PostgreSqlBulkOperationProvider(dialect, typeConverter);
        });

        // Register the main database provider
        services.AddSingleton<IDatabaseProvider, PostgreSqlProvider>();

        // Register connection factory
        services.AddTransient<IDbConnection>(_ => new NpgsqlConnection(connectionString));

        // Register metadata provider (uses generated provider if available for 10-100x performance)
        services.AddNpaMetadataProvider();

        // Register entity manager as both interface and concrete type
        services.AddScoped<IEntityManager, EntityManager>();
        services.AddScoped<EntityManager>(provider =>
        {
            var connection = provider.GetRequiredService<IDbConnection>();
            var metadataProvider = provider.GetRequiredService<IMetadataProvider>();
            var databaseProvider = provider.GetRequiredService<IDatabaseProvider>();
            var logger = provider.GetService<ILogger<EntityManager>>();
            return new EntityManager(connection, metadataProvider, databaseProvider, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds PostgreSQL database provider services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="configure">Custom configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgreSqlProvider(this IServiceCollection services, string connectionString, Action<PostgreSqlOptions> configure)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var options = new PostgreSqlOptions();
        configure(options);

        // Register options
        services.AddSingleton(options);

        // Build effective connection string with pooling configuration
        var effectiveConnectionString = BuildConnectionString(connectionString, options);

        // Register PostgreSQL-specific services
        services.AddSingleton<ISqlDialect, PostgreSqlDialect>();
        services.AddSingleton<ITypeConverter, PostgreSqlTypeConverter>();
        services.AddSingleton<IBulkOperationProvider>(provider =>
        {
            var dialect = provider.GetRequiredService<ISqlDialect>();
            var typeConverter = provider.GetRequiredService<ITypeConverter>();
            return new PostgreSqlBulkOperationProvider(dialect, typeConverter);
        });

        // Register the main database provider
        services.AddSingleton<IDatabaseProvider, PostgreSqlProvider>();

        // Register connection factory with configured connection string
        services.AddTransient<IDbConnection>(_ => new NpgsqlConnection(effectiveConnectionString));

        // Register metadata provider
        services.AddNpaMetadataProvider(); // Uses generated provider if available for 10-100x performance

        // Register entity manager
        services.AddScoped<IEntityManager>(provider =>
        {
            var connection = provider.GetRequiredService<IDbConnection>();
            var metadataProvider = provider.GetRequiredService<IMetadataProvider>();
            var databaseProvider = provider.GetRequiredService<IDatabaseProvider>();
            var logger = provider.GetService<ILogger<EntityManager>>();
            return new EntityManager(connection, metadataProvider, databaseProvider, logger);
        });

        return services;
    }

    /// <summary>
    /// Builds an effective connection string with pooling and other configuration options.
    /// </summary>
    private static string BuildConnectionString(string baseConnectionString, PostgreSqlOptions options)
    {
        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString);

        // Apply connection pooling options
        builder.Pooling = options.Pooling.Enabled;
        builder.MinPoolSize = options.Pooling.MinPoolSize;
        builder.MaxPoolSize = options.Pooling.MaxPoolSize;

        if (options.Pooling.ConnectionLifetime.HasValue)
            builder.ConnectionLifetime = (int)options.Pooling.ConnectionLifetime.Value.TotalSeconds;

        if (options.Pooling.IdleTimeout.HasValue)
            builder.ConnectionIdleLifetime = (int)options.Pooling.IdleTimeout.Value.TotalSeconds;

        // Apply command timeout
        if (options.CommandTimeout.HasValue)
            builder.CommandTimeout = options.CommandTimeout.Value;

        // Apply connection timeout (different from command timeout)
        builder.Timeout = (int)options.Pooling.ConnectionTimeout.TotalSeconds;

        // Apply other PostgreSQL-specific options
        if (!string.IsNullOrEmpty(options.SslMode))
        {
            if (Enum.TryParse<SslMode>(options.SslMode, true, out var sslMode))
                builder.SslMode = sslMode;
        }

        if (!string.IsNullOrEmpty(options.ApplicationName))
            builder.ApplicationName = options.ApplicationName;

        builder.IncludeErrorDetail = options.IncludeErrorDetails;

        if (options.KeepAlive.HasValue)
            builder.KeepAlive = options.KeepAlive.Value;

        if (options.TcpKeepAliveTime.HasValue)
            builder.TcpKeepAliveTime = options.TcpKeepAliveTime.Value;

        if (options.TcpKeepAliveInterval.HasValue)
            builder.TcpKeepAliveInterval = options.TcpKeepAliveInterval.Value;

        if (options.MaxAutoPrepare > 0)
        {
            builder.MaxAutoPrepare = options.MaxAutoPrepare;
            if (options.AutoPrepareMinUsages > 0)
                builder.AutoPrepareMinUsages = options.AutoPrepareMinUsages;
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// Adds PostgreSQL database provider services with a connection factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionFactory">The connection factory.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgreSqlProvider(this IServiceCollection services, Func<IServiceProvider, IDbConnection> connectionFactory)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (connectionFactory == null)
            throw new ArgumentNullException(nameof(connectionFactory));

        // Register PostgreSQL-specific services
        services.AddSingleton<ISqlDialect, PostgreSqlDialect>();
        services.AddSingleton<ITypeConverter, PostgreSqlTypeConverter>();
        services.AddSingleton<IBulkOperationProvider>(provider =>
        {
            var dialect = provider.GetRequiredService<ISqlDialect>();
            var typeConverter = provider.GetRequiredService<ITypeConverter>();
            return new PostgreSqlBulkOperationProvider(dialect, typeConverter);
        });

        // Register the main database provider
        services.AddSingleton<IDatabaseProvider, PostgreSqlProvider>();

        // Register custom connection factory
        services.AddTransient(connectionFactory);

        // Register metadata provider
        services.AddNpaMetadataProvider(); // Uses generated provider if available for 10-100x performance

        // Register entity manager
        services.AddScoped<IEntityManager>(provider =>
        {
            var connection = provider.GetRequiredService<IDbConnection>();
            var metadataProvider = provider.GetRequiredService<IMetadataProvider>();
            var databaseProvider = provider.GetRequiredService<IDatabaseProvider>();
            var logger = provider.GetService<ILogger<EntityManager>>();
            return new EntityManager(connection, metadataProvider, databaseProvider, logger);
        });

        return services;
    }
}

/// <summary>
/// Configuration options for PostgreSQL provider.
/// </summary>
public class PostgreSqlOptions
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
    /// Gets or sets the SSL mode for connections.
    /// </summary>
    public string? SslMode { get; set; }
    /// <summary>
    /// Gets or sets the application name for connection tracking.
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include error details in exceptions.
    /// </summary>
    public bool IncludeErrorDetails { get; set; } = true;

    /// <summary>
    /// Gets or sets the keep-alive interval in seconds.
    /// </summary>
    public int? KeepAlive { get; set; }

    /// <summary>
    /// Gets or sets the TCP keep-alive time in seconds.
    /// </summary>
    public int? TcpKeepAliveTime { get; set; }

    /// <summary>
    /// Gets or sets the TCP keep-alive interval in seconds.
    /// </summary>
    public int? TcpKeepAliveInterval { get; set; }

    /// <summary>
    /// Gets or sets the minimum usage count before a statement is automatically prepared.
    /// Set to 0 to disable automatic preparation.
    /// </summary>
    public int AutoPrepareMinUsages { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum number of automatic prepared statements.
    /// </summary>
    public int MaxAutoPrepare { get; set; } = 20;
}

