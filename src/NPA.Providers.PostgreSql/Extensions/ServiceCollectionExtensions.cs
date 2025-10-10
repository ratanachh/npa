using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Core.Core;
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

        // Register metadata provider
        services.AddSingleton<IMetadataProvider, MetadataProvider>();

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

        // Register connection factory with custom connection configuration
        services.AddTransient<IDbConnection>(provider =>
        {
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);

            // Apply options
            if (options.CommandTimeout.HasValue)
                connectionStringBuilder.CommandTimeout = options.CommandTimeout.Value;

            if (options.MaxPoolSize.HasValue)
                connectionStringBuilder.MaxPoolSize = options.MaxPoolSize.Value;

            if (options.MinPoolSize.HasValue)
                connectionStringBuilder.MinPoolSize = options.MinPoolSize.Value;

            if (options.ConnectionIdleLifetime.HasValue)
                connectionStringBuilder.ConnectionIdleLifetime = options.ConnectionIdleLifetime.Value;

            if (options.ConnectionPruningInterval.HasValue)
                connectionStringBuilder.ConnectionPruningInterval = options.ConnectionPruningInterval.Value;

            if (options.EnablePooling.HasValue)
                connectionStringBuilder.Pooling = options.EnablePooling.Value;

            if (options.Timeout.HasValue)
                connectionStringBuilder.Timeout = options.Timeout.Value;

            return new NpgsqlConnection(connectionStringBuilder.ConnectionString);
        });

        // Register metadata provider
        services.AddSingleton<IMetadataProvider, MetadataProvider>();

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
        services.AddSingleton<IMetadataProvider, MetadataProvider>();

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
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    public int? CommandTimeout { get; set; }

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    public int? Timeout { get; set; }

    /// <summary>
    /// Gets or sets the maximum batch size for bulk operations.
    /// </summary>
    public int MaxBatchSize { get; set; } = 5000;

    /// <summary>
    /// Gets or sets a value indicating whether to enable connection pooling.
    /// </summary>
    public bool? EnablePooling { get; set; }

    /// <summary>
    /// Gets or sets the minimum pool size.
    /// </summary>
    public int? MinPoolSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum pool size.
    /// </summary>
    public int? MaxPoolSize { get; set; }

    /// <summary>
    /// Gets or sets the connection idle lifetime in seconds.
    /// </summary>
    public int? ConnectionIdleLifetime { get; set; }

    /// <summary>
    /// Gets or sets the connection pruning interval in seconds.
    /// </summary>
    public int? ConnectionPruningInterval { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable SSL mode.
    /// </summary>
    public string? SslMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to trust the server certificate.
    /// </summary>
    public bool TrustServerCertificate { get; set; } = false;

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
    /// Gets or sets a value indicating whether to enable prepared statements.
    /// </summary>
    public bool AutoPrepareMinUsages { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of automatic prepared statements.
    /// </summary>
    public int MaxAutoPrepare { get; set; } = 20;
}

