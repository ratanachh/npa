using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Core.Configuration;
using NPA.Core.Core;
using NPA.Core.Extensions;
using NPA.Core.Metadata;
using NPA.Core.Providers;
using System.Data;

namespace NPA.Providers.SqlServer.Extensions;

/// <summary>
/// Extension methods for configuring SQL Server provider services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SQL Server database provider services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqlServerProvider(this IServiceCollection services, string connectionString)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        // Register SQL Server-specific services
        services.AddSingleton<ISqlDialect, SqlServerDialect>();
        services.AddSingleton<ITypeConverter, SqlServerTypeConverter>();
        services.AddSingleton<IBulkOperationProvider>(provider =>
        {
            var dialect = provider.GetRequiredService<ISqlDialect>();
            var typeConverter = provider.GetRequiredService<ITypeConverter>();
            return new SqlServerBulkOperationProvider(dialect, typeConverter);
        });

        // Register the main database provider
        services.AddSingleton<IDatabaseProvider, SqlServerProvider>();

        // Register connection factory
        services.AddTransient<IDbConnection>(_ => new SqlConnection(connectionString));

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
    /// Adds SQL Server database provider services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <param name="configure">Custom configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqlServerProvider(this IServiceCollection services, string connectionString, Action<SqlServerOptions> configure)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var options = new SqlServerOptions();
        configure(options);

        // Build effective connection string with pooling configuration
        var effectiveConnectionString = BuildConnectionString(connectionString, options);

        // Register options
        services.AddSingleton(options);

        // Register SQL Server-specific services with options
        services.AddSingleton<ISqlDialect, SqlServerDialect>();
        services.AddSingleton<ITypeConverter, SqlServerTypeConverter>();
        services.AddSingleton<IBulkOperationProvider>(provider =>
        {
            var dialect = provider.GetRequiredService<ISqlDialect>();
            var typeConverter = provider.GetRequiredService<ITypeConverter>();
            return new SqlServerBulkOperationProvider(dialect, typeConverter);
        });

        // Register the main database provider
        services.AddSingleton<IDatabaseProvider, SqlServerProvider>();

        // Register connection factory with effective connection string
        services.AddTransient<IDbConnection>(_ => new SqlConnection(effectiveConnectionString));

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
    /// Builds an effective connection string with pooling and SQL Server-specific options.
    /// </summary>
    private static string BuildConnectionString(string baseConnectionString, SqlServerOptions options)
    {
        var builder = new SqlConnectionStringBuilder(baseConnectionString);

        // Apply connection pooling settings
        builder.Pooling = options.Pooling.Enabled;
        builder.MinPoolSize = options.Pooling.MinPoolSize;
        builder.MaxPoolSize = options.Pooling.MaxPoolSize;
        builder.ConnectTimeout = (int)options.Pooling.ConnectionTimeout.TotalSeconds;

        if (options.Pooling.ConnectionLifetime.HasValue)
        {
            builder.LoadBalanceTimeout = (int)options.Pooling.ConnectionLifetime.Value.TotalSeconds;
        }

        builder.Enlist = true; // Automatic transaction enlistment

        // Apply SQL Server-specific options
        if (options.CommandTimeout.HasValue)
        {
            builder.CommandTimeout = options.CommandTimeout.Value;
        }

        builder.MultipleActiveResultSets = options.EnableMars;
        builder.Encrypt = options.Encrypt;
        builder.TrustServerCertificate = options.TrustServerCertificate;

        return builder.ConnectionString;
    }

    /// <summary>
    /// Adds SQL Server database provider services with a connection factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionFactory">The connection factory.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqlServerProvider(this IServiceCollection services, Func<IServiceProvider, IDbConnection> connectionFactory)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (connectionFactory == null)
            throw new ArgumentNullException(nameof(connectionFactory));

        // Register SQL Server-specific services
        services.AddSingleton<ISqlDialect, SqlServerDialect>();
        services.AddSingleton<ITypeConverter, SqlServerTypeConverter>();
        services.AddSingleton<IBulkOperationProvider>(provider =>
        {
            var dialect = provider.GetRequiredService<ISqlDialect>();
            var typeConverter = provider.GetRequiredService<ITypeConverter>();
            return new SqlServerBulkOperationProvider(dialect, typeConverter);
        });

        // Register the main database provider
        services.AddSingleton<IDatabaseProvider, SqlServerProvider>();

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
/// Configuration options for SQL Server provider.
/// </summary>
public class SqlServerOptions
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
    /// Gets or sets the bulk copy timeout in seconds.
    /// </summary>
    public int BulkCopyTimeout { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Gets or sets the maximum batch size for bulk operations.
    /// </summary>
    public int MaxBatchSize { get; set; } = 10000;

    /// <summary>
    /// Gets or sets a value indicating whether to enable retry logic.
    /// </summary>
    public bool EnableRetryLogic { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the retry delay in milliseconds.
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets a value indicating whether to enable multiple active result sets (MARS).
    /// </summary>
    public bool EnableMars { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to encrypt the connection.
    /// </summary>
    public bool Encrypt { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to trust the server certificate.
    /// </summary>
    public bool TrustServerCertificate { get; set; } = false;
}