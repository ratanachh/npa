using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Core.Providers;
using NPA.Providers.SqlServer;
using System.Data;
using Testcontainers.MsSql;

namespace BasicUsage.Features;

/// <summary>
/// Runs the Phase 1 demo using SQL Server provider (Phase 1.4 - in progress).
/// Note: SQL Server provider is still being completed.
/// </summary>
public static class SqlServerProviderRunner
{
    public static async Task RunAsync()
    {
        var sqlServerContainer = new MsSqlBuilder()
            .WithPassword("MyStrong@Passw0rd")
            .WithCleanUp(true)
            .Build();
        
        bool containerStarted = false;
        IServiceProvider? serviceProvider = null;

        try
        {
            Console.WriteLine("Starting SQL Server container...");
            await sqlServerContainer.StartAsync();
            containerStarted = true;
            
            var connectionString = sqlServerContainer.GetConnectionString();
            Console.WriteLine("SQL Server container started.");

            // Setup Dependency Injection
            var services = new ServiceCollection();
            services.AddLogging(builder => 
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            
            // Register NPA services with SQL Server provider
            services.AddSingleton<IMetadataProvider, MetadataProvider>();
            services.AddSingleton<IDatabaseProvider, SqlServerProvider>();
            services.AddScoped<IDbConnection>(sp =>
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                return connection;
            });
            services.AddScoped<EntityManager>();

            serviceProvider = services.BuildServiceProvider();

            // Create database schema
            await CreateDatabaseSchemaSqlServer(connectionString);

            // Run Phase 1 demo
            await Phase1Demo.RunAsync(serviceProvider, "sqlserver");
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
                Console.WriteLine("\nStopping SQL Server container...");
                await sqlServerContainer.StopAsync();
                await sqlServerContainer.DisposeAsync();
            }
        }
    }

    private static async Task CreateDatabaseSchemaSqlServer(string connectionString)
    {
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync();
        var createTableSql = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='users' AND xtype='U')
            CREATE TABLE users (
                id BIGINT IDENTITY(1,1) PRIMARY KEY,
                username NVARCHAR(255) NOT NULL,
                email NVARCHAR(255) NOT NULL,
                created_at DATETIME2 NOT NULL,
                is_active BIT NOT NULL
            )";
        await using var command = new Microsoft.Data.SqlClient.SqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
        Console.WriteLine("Database schema created successfully");
    }
}
