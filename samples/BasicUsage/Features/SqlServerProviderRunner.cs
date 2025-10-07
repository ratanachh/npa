using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Providers.SqlServer.Extensions;
using Testcontainers.MsSql;

namespace BasicUsage.Features;

public static class SqlServerProviderRunner
{
    public static async Task RunAsync()
    {
        var sqlServerContainer = new MsSqlBuilder()
            .WithPassword("MyStrong@Passw0rd")
            .WithCleanUp(true)
            .Build();
        bool containerStarted = false;
        IServiceProvider serviceProvider = null!;
        string connectionString = "";
        try
        {
            Console.WriteLine("Starting SQL Server container...");
            await sqlServerContainer.StartAsync();
            containerStarted = true;
            connectionString = sqlServerContainer.GetConnectionString();
            Console.WriteLine("SQL Server container started.");

            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            services.AddSqlServerProvider(connectionString);
            serviceProvider = services.BuildServiceProvider();

            await CreateDatabaseSchemaSqlServer(connectionString);
            // Consolidated Phase 1.1 - 1.4 demo
            await Phase1Demo.RunAsync(serviceProvider, "sqlserver");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            if (containerStarted)
                await sqlServerContainer.StopAsync();
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
