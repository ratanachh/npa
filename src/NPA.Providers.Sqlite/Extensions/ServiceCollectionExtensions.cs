using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Core.Core;
using NPA.Core.Extensions;
using NPA.Core.Metadata;
using NPA.Core.Providers;
using System.Data;

namespace NPA.Providers.Sqlite.Extensions;

/// <summary>
/// Extension methods for configuring SQLite provider services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SQLite database provider services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The SQLite connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqliteProvider(this IServiceCollection services, string connectionString)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        // Register SQLite-specific services
        services.AddSingleton<ISqlDialect, SqliteDialect>();
        services.AddSingleton<ITypeConverter, SqliteTypeConverter>();
        services.AddSingleton<IBulkOperationProvider>(provider =>
        {
            var dialect = provider.GetRequiredService<ISqlDialect>();
            var typeConverter = provider.GetRequiredService<ITypeConverter>();
            return new SqliteBulkOperationProvider(dialect, typeConverter);
        });

        // Register the main database provider
        services.AddSingleton<IDatabaseProvider, SqliteProvider>();

        // Register connection factory
        services.AddTransient<IDbConnection>(_ => new SqliteConnection(connectionString));

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
    /// Adds SQLite database provider services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The SQLite connection string.</param>
    /// <param name="configure">Custom configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqliteProvider(this IServiceCollection services, string connectionString, Action<SqliteOptions> configure)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var options = new SqliteOptions();
        configure(options);

        // Register options
        services.AddSingleton(options);

        // Register SQLite-specific services
        services.AddSingleton<ISqlDialect, SqliteDialect>();
        services.AddSingleton<ITypeConverter, SqliteTypeConverter>();
        services.AddSingleton<IBulkOperationProvider>(provider =>
        {
            var dialect = provider.GetRequiredService<ISqlDialect>();
            var typeConverter = provider.GetRequiredService<ITypeConverter>();
            return new SqliteBulkOperationProvider(dialect, typeConverter);
        });

        // Register the main database provider
        services.AddSingleton<IDatabaseProvider, SqliteProvider>();

        // Register connection factory with options
        services.AddTransient<IDbConnection>(provider =>
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);

            // Apply options
            if (options.CacheSize.HasValue)
                connectionStringBuilder.Cache = (SqliteCacheMode)options.CacheSize.Value;

            if (options.Mode.HasValue)
                connectionStringBuilder.Mode = options.Mode.Value;

            var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
            
            // Set pragmas if specified
            if (options.ForeignKeys.HasValue || options.JournalMode != null)
            {
                connection.Open();
                using var command = connection.CreateCommand();
                var pragmas = new List<string>();

                if (options.ForeignKeys.HasValue)
                    pragmas.Add($"PRAGMA foreign_keys = {(options.ForeignKeys.Value ? "ON" : "OFF")};");

                if (!string.IsNullOrEmpty(options.JournalMode))
                    pragmas.Add($"PRAGMA journal_mode = {options.JournalMode};");

                if (pragmas.Any())
                {
                    command.CommandText = string.Join("\n", pragmas);
                    command.ExecuteNonQuery();
                }
                
                connection.Close();
            }

            return connection;
        });

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
    /// Adds SQLite database provider services with a connection factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionFactory">The connection factory.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqliteProvider(this IServiceCollection services, Func<IServiceProvider, IDbConnection> connectionFactory)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (connectionFactory == null)
            throw new ArgumentNullException(nameof(connectionFactory));

        // Register SQLite-specific services
        services.AddSingleton<ISqlDialect, SqliteDialect>();
        services.AddSingleton<ITypeConverter, SqliteTypeConverter>();
        services.AddSingleton<IBulkOperationProvider>(provider =>
        {
            var dialect = provider.GetRequiredService<ISqlDialect>();
            var typeConverter = provider.GetRequiredService<ITypeConverter>();
            return new SqliteBulkOperationProvider(dialect, typeConverter);
        });

        // Register the main database provider
        services.AddSingleton<IDatabaseProvider, SqliteProvider>();

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
/// Configuration options for SQLite provider.
/// </summary>
public class SqliteOptions
{
    /// <summary>
    /// Gets or sets the cache size.
    /// </summary>
    public int? CacheSize { get; set; }

    /// <summary>
    /// Gets or sets the database open mode.
    /// </summary>
    public SqliteOpenMode? Mode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable foreign keys.
    /// </summary>
    public bool? ForeignKeys { get; set; }

    /// <summary>
    /// Gets or sets the journal mode (DELETE, TRUNCATE, PERSIST, MEMORY, WAL, OFF).
    /// </summary>
    public string? JournalMode { get; set; }

    /// <summary>
    /// Gets or sets the maximum batch size for bulk operations.
    /// </summary>
    public int MaxBatchSize { get; set; } = 500;
}

