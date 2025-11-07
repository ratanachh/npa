using Microsoft.Extensions.DependencyInjection;
using NPA.Core.Core;
using NPA.Providers.PostgreSql.Extensions;
using NPA.Samples.Core;
using Npgsql;
using Testcontainers.PostgreSql;

namespace NPA.Samples;

/// <summary>
/// Sample wrapper for Transaction Management demonstration.
/// Implements ISample for automatic discovery by SampleRunner.
/// </summary>
public class TransactionSampleRunner : ISample
{
    public string Name => "Transaction Management (Phase 3.1)";

    public string Description => "Demonstrates deferred execution, batching, rollback, and performance optimization";

    public async Task RunAsync()
    {
        var postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("npa_transaction_demo")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithCleanUp(true)
            .Build();

        await using (postgresContainer)
        {
            Console.WriteLine("Starting PostgreSQL container...");
            await postgresContainer.StartAsync();
            Console.WriteLine("PostgreSQL container started.");

            var connectionString = postgresContainer.GetConnectionString();

            // Setup dependency injection
            var services = new ServiceCollection();
            services.AddPostgreSqlProvider(connectionString);

            var serviceProvider = services.BuildServiceProvider();
            var entityManager = serviceProvider.GetRequiredService<IEntityManager>();

            // Initialize database schema
            await InitializeDatabaseAsync(connectionString);

            // Run transaction demos
            var transactionSample = new TransactionSample(entityManager);
            await transactionSample.RunAllDemosAsync();

            // Wait for user input before returning to menu
            Console.WriteLine("Press any key to return to the menu...");
            Console.ReadKey();
        }
    }

    /// <summary>
    /// Initializes the database schema for the transaction demo.
    /// </summary>
    private async Task InitializeDatabaseAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Create users table
        var createUsersTable = @"
            CREATE TABLE IF NOT EXISTS users (
                id BIGSERIAL PRIMARY KEY,
                username VARCHAR(100) NOT NULL,
                email VARCHAR(255) NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                is_active BOOLEAN NOT NULL DEFAULT true
            );";

        // Create orders table
        var createOrdersTable = @"
            CREATE TABLE IF NOT EXISTS orders (
                id BIGSERIAL PRIMARY KEY,
                order_number VARCHAR(50) NOT NULL,
                customer_name VARCHAR(255) NOT NULL,
                order_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                total_amount DECIMAL(10, 2) NOT NULL,
                status VARCHAR(50) NOT NULL DEFAULT 'Pending',
                shipped_date TIMESTAMP,
                user_id BIGINT,
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL
            );";

        // Create order_items table
        var createOrderItemsTable = @"
            CREATE TABLE IF NOT EXISTS order_items (
                id BIGSERIAL PRIMARY KEY,
                order_id BIGINT NOT NULL,
                product_name VARCHAR(255) NOT NULL,
                quantity INTEGER NOT NULL,
                unit_price DECIMAL(10, 2) NOT NULL,
                subtotal DECIMAL(10, 2) NOT NULL,
                FOREIGN KEY (order_id) REFERENCES orders(id) ON DELETE CASCADE
            );";

        await using var command = connection.CreateCommand();
        
        command.CommandText = createUsersTable;
        await command.ExecuteNonQueryAsync();

        command.CommandText = createOrdersTable;
        await command.ExecuteNonQueryAsync();

        command.CommandText = createOrderItemsTable;
        await command.ExecuteNonQueryAsync();

        Console.WriteLine("✓ Database schema initialized\n");
    }
}
