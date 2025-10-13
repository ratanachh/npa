using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Core.Core;
using NPA.Core.Extensions;
using NPA.Providers.PostgreSql.Extensions;
using NPA.Samples.Core;
using NPA.Samples.Entities;
using Npgsql;
using Testcontainers.PostgreSql;

namespace NPA.Samples.Features;

public class AdvancedQueriesSample : ISample
{
    public string Name => "Advanced CPQL Queries";
    public string Description => "Demonstrates advanced CPQL features like JOINs, GROUP BY, aggregates, and functions.";

    public async Task RunAsync()
    {
        var postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("npa_advanced_queries")
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

            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            services.AddPostgreSqlProvider(connectionString);

            await using var serviceProvider = services.BuildServiceProvider();

            await CreateDatabaseSchemaAsync(connectionString);
            await SeedTestDataAsync(serviceProvider);

            var entityManager = serviceProvider.GetRequiredService<IEntityManager>();

            await RunAllExamplesAsync(entityManager);
        }
    }

    private async Task RunAllExamplesAsync(IEntityManager entityManager)
    {
        Console.WriteLine("\n--- Running Advanced Query Examples ---");

        // Example 1: Complex WHERE
        var products = await entityManager.CreateQuery<Product>("SELECT p FROM Product p WHERE (p.CategoryName = :cat1 AND p.Price < 100) OR (p.CategoryName = :cat2 AND p.Price > 200)")
            .SetParameter("cat1", "Electronics")
            .SetParameter("cat2", "Furniture")
            .GetResultListAsync();
        Console.WriteLine($"1. Found {products.Count()} products with complex WHERE clause.");

        // Example 2: JOIN operation
        var ordersWithUsers = await entityManager.CreateQuery<Order>("SELECT o FROM Order o JOIN o.User u WHERE u.Username = :username")
            .SetParameter("username", "john.doe")
            .GetResultListAsync();
        Console.WriteLine($"2. Found {ordersWithUsers.Count()} order(s) for user 'john.doe'.");

        // Example 3: GROUP BY and HAVING
        var categoryCounts = await entityManager.CreateQuery<object>("SELECT p.CategoryName, COUNT(p.Id) FROM Product p GROUP BY p.CategoryName HAVING COUNT(p.Id) > 1")
            .GetResultListAsync();
        Console.WriteLine("3. Found categories with more than 1 product.");

        // Example 4: Aggregate Functions
        var totalInventoryValue = await entityManager.CreateQuery<Product>("SELECT SUM(p.Price * p.StockQuantity) FROM Product p").ExecuteScalarAsync();
        Console.WriteLine($"4. Total inventory value: {totalInventoryValue:C}");
    }

    private async Task CreateDatabaseSchemaAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        const string createTablesSql = @"
            CREATE TABLE IF NOT EXISTS users (
                id BIGSERIAL PRIMARY KEY,
                username VARCHAR(255) NOT NULL,
                email VARCHAR(255) NOT NULL,
                created_at TIMESTAMP NOT NULL,
                is_active BOOLEAN NOT NULL
            );
            CREATE TABLE IF NOT EXISTS products (
                id BIGSERIAL PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                category_name VARCHAR(255) NOT NULL,
                price DECIMAL(10, 2) NOT NULL,
                stock_quantity INT NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT true,
                created_at TIMESTAMP NOT NULL
            );
            CREATE TABLE IF NOT EXISTS orders (
                id BIGSERIAL PRIMARY KEY,
                order_number VARCHAR(255) NOT NULL,
                customer_name VARCHAR(255) NOT NULL,
                order_date TIMESTAMP NOT NULL,
                total_amount DECIMAL(10, 2) NOT NULL,
                status VARCHAR(50) NOT NULL,
                shipped_date TIMESTAMP NULL,
                user_id BIGINT NULL REFERENCES users(id)
            );
        ";

        await using var command = new NpgsqlCommand(createTablesSql, connection);
        await command.ExecuteNonQueryAsync();
        Console.WriteLine("Database schema created successfully");
    }

    private async Task SeedTestDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<IEntityManager>();

        Console.WriteLine("Seeding test data...");

        var user = new User { Username = "john.doe", Email = "john.doe@example.com", IsActive = true, CreatedAt = DateTime.UtcNow };
        await em.PersistAsync(user);

        var products = new[]
        {
            new Product { Name = "Laptop Pro", CategoryName = "Electronics", Price = 1299.99m, StockQuantity = 15 },
            new Product { Name = "Wireless Mouse", CategoryName = "Electronics", Price = 29.99m, StockQuantity = 100 },
            new Product { Name = "Office Chair", CategoryName = "Furniture", Price = 299.99m, StockQuantity = 20 },
        };
        foreach (var p in products) await em.PersistAsync(p);

        var orders = new[]
        {
            new Order { OrderNumber = "ORD-001", CustomerName = "John Doe", OrderDate = DateTime.UtcNow.AddDays(-10), TotalAmount = 1329.98m, Status = "Shipped", UserId = user.Id },
            new Order { OrderNumber = "ORD-002", CustomerName = "Jane Smith", OrderDate = DateTime.UtcNow.AddDays(-8), TotalAmount = 599.99m, Status = "Delivered" },
        };
        foreach (var o in orders) await em.PersistAsync(o);

        Console.WriteLine("Test data seeded.");
    }
}
