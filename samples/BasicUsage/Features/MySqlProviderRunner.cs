using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using NPA.Core.Core;
using NPA.Core.Extensions;
using NPA.Core.Metadata;
using NPA.Core.Providers;
using NPA.Providers.MySql;
using System.Data;
using Testcontainers.MySql;

namespace BasicUsage.Features;

/// <summary>
/// Runs the Phase 1 demo using MySQL provider (Phase 1.5 - COMPLETE).
/// Demonstrates: Entity mapping, CRUD operations, CPQL queries with MySQL.
/// Advanced features available: JSON, Spatial types, Full-Text Search, UPSERT.
/// </summary>
public static class MySqlProviderRunner
{
    public static async Task RunAsync()
    {
        var mySqlContainer = new MySqlBuilder()
            .WithPassword("MyStrong@Passw0rd")
            .WithCleanUp(true)
            .Build();
        
        bool containerStarted = false;
        IServiceProvider? serviceProvider = null;

        try
        {
            Console.WriteLine("Starting MySQL container...");
            await mySqlContainer.StartAsync();
            containerStarted = true;
            
            var connectionString = mySqlContainer.GetConnectionString();
            Console.WriteLine("MySQL container started.");

            // Setup Dependency Injection
            var services = new ServiceCollection();
            services.AddLogging(builder => 
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            
            // Register NPA services with MySQL provider
            services.AddNpaMetadataProvider(); // Uses generated provider if available for 10-100x performance
            services.AddSingleton<IDatabaseProvider, MySqlProvider>();
            services.AddScoped<IDbConnection>(sp =>
            {
                var connection = new MySqlConnection(connectionString);
                connection.Open();
                return connection;
            });
            services.AddScoped<EntityManager>();

            serviceProvider = services.BuildServiceProvider();

            // Create database schema
            await CreateDatabaseSchemaMySQL(connectionString);

            // Run Phase 1 demo
            await Phase1Demo.RunAsync(serviceProvider, "mysql");
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
                Console.WriteLine("\nStopping MySQL container...");
                await mySqlContainer.StopAsync();
                await mySqlContainer.DisposeAsync();
            }
        }
    }

    private static async Task CreateDatabaseSchemaMySQL(string connectionString)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS users (
                id BIGINT AUTO_INCREMENT PRIMARY KEY,
                username VARCHAR(255) NOT NULL,
                email VARCHAR(255) NOT NULL,
                created_at DATETIME NOT NULL,
                is_active TINYINT(1) NOT NULL
            )";
        await using var command = new MySqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
        Console.WriteLine("Database schema created successfully");
    }
}

