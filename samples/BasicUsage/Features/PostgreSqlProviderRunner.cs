using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Core.Core;
using NPA.Core.Extensions;
using NPA.Core.Metadata;
using NPA.Core.Providers;
using NPA.Providers.PostgreSql;
using Npgsql;
using System.Data;
using Testcontainers.PostgreSql;

namespace BasicUsage.Features;

/// <summary>
/// Runs the Phase 1 demo using PostgreSQL provider (Phase 1.1-1.3 completed).
/// Demonstrates:
/// - Phase 1.1: Entity mapping with attributes
/// - Phase 1.2: EntityManager CRUD operations
/// - Phase 1.3: CPQL query language with parameters
/// </summary>
public static class PostgreSqlProviderRunner
{
    public static async Task RunAsync()
    {
        var postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("npa_basicusage")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithCleanUp(true)
            .Build();

        bool containerStarted = false;
        IServiceProvider? serviceProvider = null;

        try
        {
            Console.WriteLine("Starting PostgreSQL container...");
            await postgresContainer.StartAsync();
            containerStarted = true;
            
            var connectionString = postgresContainer.GetConnectionString();
            Console.WriteLine("PostgreSQL container started.");

            // Setup Dependency Injection
            var services = new ServiceCollection();
            services.AddLogging(builder => 
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            
            // Register NPA services with PostgreSQL provider
            services.AddNpaMetadataProvider(); // Uses generated provider if available for 10-100x performance
            services.AddSingleton<IDatabaseProvider, PostgreSqlProvider>();
            services.AddScoped<IDbConnection>(sp =>
            {
                var connection = new NpgsqlConnection(connectionString);
                connection.Open();
                return connection;
            });
            services.AddScoped<EntityManager>();

            serviceProvider = services.BuildServiceProvider();

            // Create database schema
            await CreateDatabaseSchemaPostgreSql(connectionString);

            // Run Phase 1 demo (1.1-1.3)
            await Phase1Demo.RunAsync(serviceProvider, "postgresql");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        finally
        {
            if (serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            if (containerStarted)
            {
                Console.WriteLine("\nStopping PostgreSQL container...");
                await postgresContainer.StopAsync();
                await postgresContainer.DisposeAsync();
            }
        }
    }

    private static async Task CreateDatabaseSchemaPostgreSql(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        const string createTableSql = @"
            CREATE TABLE IF NOT EXISTS users (
                id BIGSERIAL PRIMARY KEY,
                username VARCHAR(255) NOT NULL,
                email VARCHAR(255) NOT NULL,
                created_at TIMESTAMP NOT NULL,
                is_active BOOLEAN NOT NULL
            )";

        await using var command = new NpgsqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
        
        Console.WriteLine("Database schema created successfully");
    }
}
