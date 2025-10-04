using System.Data;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NPA.Core.Core;
using NPA.Core.Metadata;
using Npgsql;
using Testcontainers.PostgreSql;

namespace BasicUsage;

/// <summary>
/// Sample application demonstrating basic NPA usage.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Starting NPA Sample with TestContainers...");
        
        // Create PostgreSQL container
        var postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("npadb")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithPortBinding(5432, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        try
        {
            // Start the container
            await postgresContainer.StartAsync();
            Console.WriteLine("PostgreSQL container started successfully");

            // Get connection string
            var connectionString = postgresContainer.GetConnectionString();
            Console.WriteLine($"Connection string: {connectionString}");

            // Create host builder
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Register NPA services
                    services.AddSingleton<IMetadataProvider, MetadataProvider>();
                    services.AddScoped<IDbConnection>(provider =>
                    {
                        return new NpgsqlConnection(connectionString);
                    });
                    services.AddScoped<IEntityManager, EntityManager>();
                })
                .Build();

            // Get logger
            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Starting NPA Basic Usage Sample with PostgreSQL");

                // Create database schema
                await CreateDatabaseSchema(connectionString, logger);

                // Get EntityManager
                using var scope = host.Services.CreateScope();
                var entityManager = scope.ServiceProvider.GetRequiredService<IEntityManager>();

                // Demonstrate basic CRUD operations
                await DemonstrateBasicCrud(entityManager, logger);

                logger.LogInformation("NPA Basic Usage Sample completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during execution");
            }
        }
        finally
        {
            // Clean up the container
            await postgresContainer.StopAsync();
            await postgresContainer.DisposeAsync();
            Console.WriteLine("PostgreSQL container stopped and disposed");
        }
    }

    private static async Task DemonstrateBasicCrud(IEntityManager entityManager, ILogger logger)
    {
        logger.LogInformation("=== Demonstrating Basic CRUD Operations ===");

        // Create a new user
        var user = new User
        {
            Username = "john_doe",
            Email = "john.doe@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        logger.LogInformation("Creating new user: {Username}", user.Username);

        try
        {
            // Persist the user
            await entityManager.PersistAsync(user);
            await entityManager.FlushAsync();

            logger.LogInformation("User created successfully with ID: {Id}", user.Id);

            // Find the user by ID
            var foundUser = await entityManager.FindAsync<User>(user.Id);
            if (foundUser != null)
            {
                logger.LogInformation("Found user: {Username} ({Email})", foundUser.Username, foundUser.Email);

                // Update the user
                foundUser.IsActive = false;
                await entityManager.MergeAsync(foundUser);
                await entityManager.FlushAsync();

                logger.LogInformation("User updated successfully");

                // Demonstrate entity state tracking
                var state = entityManager.ChangeTracker.GetState(foundUser);
                logger.LogInformation("User state after update: {State}", state);

                // Check if entity is managed
                var isManaged = entityManager.Contains(foundUser);
                logger.LogInformation("User is managed: {IsManaged}", isManaged);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database operations failed");
            throw;
        }
    }

    private static async Task CreateDatabaseSchema(string connectionString, ILogger logger)
    {
        logger.LogInformation("Creating database schema...");
        
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS users (
                id BIGSERIAL PRIMARY KEY,
                username VARCHAR(255) NOT NULL,
                email VARCHAR(255) NOT NULL,
                created_at TIMESTAMP NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT true
            );";
        
        using var command = new NpgsqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
        
        logger.LogInformation("Database schema created successfully");
    }
}

