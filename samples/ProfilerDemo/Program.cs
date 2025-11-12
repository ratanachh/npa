using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Core.Core;
using NPA.Monitoring;
using NPA.Providers.PostgreSql.Extensions;
using Npgsql;
using ProfilerDemo.Repositories;
using ProfilerDemo.Services;
using Testcontainers.PostgreSql;

namespace ProfilerDemo;

/// <summary>
/// NPA Profiler Demo - Enterprise Edition
/// 
/// Demonstrates:
/// - Performance monitoring and profiling at scale (1M+ records)
/// - Faker (Bogus) for realistic test data generation
/// - Dependency Injection with Microsoft.Extensions
/// - Repository Pattern with NPA source generators
/// - Performance comparison of different query patterns
/// - Testcontainers for isolated testing
/// 
/// Key Performance Scenarios:
/// - Indexed vs Full Table Scan queries
/// - N+1 Problem vs Batch Queries
/// - Pagination performance degradation
/// - Aggregate queries at scale
/// - Bulk operations efficiency
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     NPA Profiler Demo - Enterprise Edition                    ║");
        Console.WriteLine("║     Performance Profiling with 10,000 Records                 ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

        // Setup PostgreSQL testcontainer
        var postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("profiler_demo")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithCleanUp(true)
            .Build();

        await using (postgresContainer)
        {
            Console.WriteLine("Starting PostgreSQL container...");
            await postgresContainer.StartAsync();
            Console.WriteLine("✓ PostgreSQL container started\n");

            var connectionString = postgresContainer.GetConnectionString();

            // Initialize database schema
            await InitializeDatabaseAsync(connectionString);

            // Setup dependency injection
            var services = new ServiceCollection();

            // NPA providers
            services.AddPostgreSqlProvider(connectionString);

            // Register all NPA repositories using the generated extension method
            // This automatically registers IEntityManager, BaseRepository, and all repositories
            services.AddNPA();

            // Logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Register PerformanceMonitor
            services.AddSingleton<PerformanceMonitor>();

            // Register services
            services.AddScoped<ProfilerDemoService>();

            var serviceProvider = services.BuildServiceProvider();

            try
            {
                // Run the profiler demo
                var demoService = serviceProvider.GetRequiredService<ProfilerDemoService>();
                await demoService.RunAsync();

                Console.WriteLine("\n✓ Demo complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            Console.WriteLine("\nStopping PostgreSQL container...");
        }

        Console.WriteLine("✓ PostgreSQL container stopped and cleaned up");
    }

    private static async Task InitializeDatabaseAsync(string connectionString)
    {
        Console.WriteLine("Creating database schema...");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            -- Drop table if exists
            DROP TABLE IF EXISTS users CASCADE;

            -- Create users table
            CREATE TABLE users (
                id SERIAL PRIMARY KEY,
                username VARCHAR(100) NOT NULL,
                email VARCHAR(255) NOT NULL,
                first_name VARCHAR(100) NOT NULL,
                last_name VARCHAR(100) NOT NULL,
                age INT NOT NULL,
                country VARCHAR(100) NOT NULL,
                city VARCHAR(100) NOT NULL,
                created_at TIMESTAMP NOT NULL,
                last_login TIMESTAMP,
                is_active BOOLEAN NOT NULL DEFAULT true,
                account_balance DECIMAL(18, 2) NOT NULL DEFAULT 0
            );

            -- Create indexes for performance testing
            CREATE INDEX idx_users_email ON users(email);
            CREATE INDEX idx_users_username ON users(username);
            CREATE INDEX idx_users_country_city ON users(country, city);
            CREATE INDEX idx_users_age ON users(age);
            CREATE INDEX idx_users_created_at ON users(created_at);
        ";

        await cmd.ExecuteNonQueryAsync();

        Console.WriteLine("✓ Database schema created successfully\n");
    }
}
