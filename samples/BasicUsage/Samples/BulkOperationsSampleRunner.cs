using Dapper;
using Microsoft.Extensions.DependencyInjection;
using NPA.Core.Core;
using NPA.Providers.PostgreSql.Extensions;
using NPA.Samples.Core;
using Npgsql;
using Testcontainers.PostgreSql;

namespace NPA.Samples.Samples;

/// <summary>
/// Runner for bulk operations sample.
/// Sets up PostgreSQL database with Testcontainers and runs all bulk operation demos.
/// </summary>
public class BulkOperationsSampleRunner : ISample
{
    public string Name => "Bulk Operations (Phase 3.3)";
    public string Description => "Demonstrates high-performance bulk insert, update, and delete operations";

    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("NPA ORM - Bulk Operations Performance Demonstration");
        Console.WriteLine(new string('=', 70));

        // Run each demo with its own isolated container
        await RunDemoAsync("Demo 1: Bulk Insert", async (sample) => await sample.Demo1_BulkInsert());
        await RunDemoAsync("Demo 2: Bulk Update", async (sample) => await sample.Demo2_BulkUpdate());
        await RunDemoAsync("Demo 3: Bulk Delete", async (sample) => await sample.Demo3_BulkDelete());
        await RunDemoAsync("Demo 4: Performance Comparison", async (sample) => await sample.Demo4_PerformanceComparison());
        await RunDemoAsync("Demo 5: Large Dataset", async (sample) => await sample.Demo5_LargeDataset());
        await RunDemoAsync("Demo 6: Complex Data", async (sample) => await sample.Demo6_ComplexData());

        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("✓ All bulk operation demos completed successfully!");
        Console.WriteLine(new string('=', 70));

        // Wait for user input before returning to menu
        Console.WriteLine("\nPress any key to return to the menu...");
        Console.ReadKey();
    }

    private async Task RunDemoAsync(string demoName, Func<BulkOperationsSample, Task> demoAction)
    {
        Console.WriteLine($"\n[Container] Starting new PostgreSQL container for {demoName}...");
        
        // Create a fresh PostgreSQL container for this demo
        var postgres = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("npa_bulk_demo")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithCleanUp(true)
            .Build();

        await using (postgres)
        {
            await postgres.StartAsync();
            var connectionString = postgres.GetConnectionString();
            Console.WriteLine($"[Container] Container started. Connection: {connectionString}");

            // Setup dependency injection with fresh services
            var services = new ServiceCollection();
            services.AddPostgreSqlProvider(connectionString);
            var serviceProvider = services.BuildServiceProvider();
            var entityManager = serviceProvider.GetRequiredService<IEntityManager>();

            // Initialize database schema
            await InitializeDatabaseAsync(connectionString);

            // Verify database is empty
            await using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                var count = await conn.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM bulk_products");
                Console.WriteLine($"[Container] Database initialized. Record count: {count}");
            }

            // Run the demo
            var sample = new BulkOperationsSample(entityManager);
            await demoAction(sample);

            Console.WriteLine($"[Container] Demo completed. Stopping container...");
        }
        
        Console.WriteLine($"[Container] Container disposed for {demoName}");
    }

    private async Task InitializeDatabaseAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            -- Drop existing tables
            DROP TABLE IF EXISTS bulk_products CASCADE;
            DROP TABLE IF EXISTS bulk_categories CASCADE;

            -- Create categories table
            CREATE TABLE bulk_categories (
                id BIGSERIAL PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                description TEXT,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            );

            -- Create products table
            CREATE TABLE bulk_products (
                id BIGSERIAL PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                sku VARCHAR(100) NOT NULL UNIQUE,
                description TEXT,
                price DECIMAL(18,2) NOT NULL,
                stock INTEGER NOT NULL DEFAULT 0,
                category VARCHAR(100) NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            );

            -- Create indexes for better query performance
            CREATE INDEX idx_bulk_products_category ON bulk_products(category);
            CREATE INDEX idx_bulk_products_sku ON bulk_products(sku);
            CREATE INDEX idx_bulk_products_price ON bulk_products(price);
            CREATE INDEX idx_bulk_products_is_active ON bulk_products(is_active);
        ";

        await command.ExecuteNonQueryAsync();
        Console.WriteLine("Database schema initialized with indexes");
    }
}
