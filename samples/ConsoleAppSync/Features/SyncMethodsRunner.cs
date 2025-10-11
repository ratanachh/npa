using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using NPA.Core.Core;
using NPA.Core.Extensions;
using NPA.Core.Metadata;
using NPA.Core.Providers;
using NPA.Providers.SqlServer;
using System.Data;
using Testcontainers.MsSql;

namespace ConsoleAppSync.Features;

/// <summary>
/// Demonstrates synchronous methods in NPA using Testcontainers.
/// Self-contained demo with SQL Server in Docker - no external database required!
/// </summary>
public static class SyncMethodsRunner
{
    public static async Task RunAsync(bool showSql = false)
    {
        Console.WriteLine(showSql ? "Running with SQL logging enabled\n" : "Running in normal mode (use --show-sql to see SQL)\n");
        
        var sqlServerContainer = new MsSqlBuilder()
            .WithPassword("YourStrong@Passw0rd")
            .WithCleanUp(true)
            .Build();
        
        bool containerStarted = false;
        IServiceProvider? serviceProvider = null;

        try
        {
            Console.WriteLine("🐳 Starting SQL Server container...");
            await sqlServerContainer.StartAsync();
            containerStarted = true;
            
            var connectionString = sqlServerContainer.GetConnectionString();
            Console.WriteLine("✓ SQL Server container started.\n");

            // Setup Dependency Injection
            var services = new ServiceCollection();
            services.AddLogging(builder => 
                builder.AddConsole().SetMinimumLevel(showSql ? LogLevel.Debug : LogLevel.Information));
            
            // Register NPA services with SQL Server provider
            services.AddNpaMetadataProvider(); // Uses generated provider if available for 10-100x performance
            services.AddSingleton<IDatabaseProvider, SqlServerProvider>();
            services.AddScoped<IDbConnection>(sp =>
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                return connection;
            });
            services.AddScoped<IEntityManager, EntityManager>();

            serviceProvider = services.BuildServiceProvider();

            // Create database schema
            await CreateDatabaseSchema(connectionString);

            // Get EntityManager from DI
            using var scope = serviceProvider.CreateScope();
            var entityManager = scope.ServiceProvider.GetRequiredService<IEntityManager>();

            // Run synchronous demos
            SyncMethodsDemo.RunCrudOperations(entityManager);
            SyncMethodsDemo.RunQueryOperations(entityManager);
            SyncMethodsDemo.RunBatchOperations(entityManager);

            Console.WriteLine("\n=== Demo Complete ===");
            
            if (!showSql)
            {
                Console.WriteLine("\n💡 Tip: Run with --show-sql or -v to see generated SQL and parameter values");
                Console.WriteLine("   Example: dotnet run -- --show-sql");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error: {ex.Message}");
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
                Console.WriteLine("\n🐳 Stopping SQL Server container...");
                await sqlServerContainer.StopAsync();
                await sqlServerContainer.DisposeAsync();
                Console.WriteLine("✓ Container stopped and removed");
            }
        }
    }

    private static async Task CreateDatabaseSchema(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        var createTableSql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'customers')
            BEGIN
                CREATE TABLE customers (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    name NVARCHAR(100) NOT NULL,
                    email NVARCHAR(255) NOT NULL,
                    phone NVARCHAR(20),
                    created_at DATETIME2 NOT NULL,
                    is_active BIT NOT NULL DEFAULT 1
                );
            END";
            
        await using var command = new SqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
        Console.WriteLine("✓ Database schema created\n");
    }
}

