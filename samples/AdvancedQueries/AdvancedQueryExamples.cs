using Microsoft.Extensions.DependencyInjection;
using NPA.Core.Core;
using AdvancedQueries.Entities;

namespace AdvancedQueries;

/// <summary>
/// Demonstrates advanced CPQL query capabilities (Phase 1.3).
/// Shows complex WHERE clauses, aggregations, and parameter binding patterns.
/// Note: Joins and subqueries require Phase 2.3 (JPQL) - not yet implemented.
/// </summary>
public class AdvancedQueryExamples
{
    private readonly IServiceProvider _serviceProvider;

    public AdvancedQueryExamples(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task RunAllExamples()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("NPA Advanced CPQL Query Examples (Phase 1.3)");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        await SeedTestData();

        await Example1_ComplexWhereConditions();
        await Example2_RangeQueries();
        await Example3_PatternMatching();
        await Example4_DateTimeQueries();
        await Example5_NullHandling();
        await Example6_AggregationQueries();
        await Example7_BulkUpdates();
        await Example8_MultipleParameters();
        await Example9_SubstringAndFunctions();
        await Example10_StatusFiltering();

        Console.WriteLine("\n" + "=".PadRight(80, '='));
        Console.WriteLine("All examples completed!");
        Console.WriteLine("=".PadRight(80, '='));
    }

    private async Task SeedTestData()
    {
        using var scope = _serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("Seeding test data...\n");

        // Seed Products
        var products = new[]
        {
            new Product { Name = "Laptop Pro 15", CategoryName = "Electronics", Price = 1299.99m, StockQuantity = 15 },
            new Product { Name = "Laptop Air 13", CategoryName = "Electronics", Price = 999.99m, StockQuantity = 25 },
            new Product { Name = "Wireless Mouse", CategoryName = "Electronics", Price = 29.99m, StockQuantity = 100 },
            new Product { Name = "Mechanical Keyboard", CategoryName = "Electronics", Price = 149.99m, StockQuantity = 50 },
            new Product { Name = "Office Chair Pro", CategoryName = "Furniture", Price = 299.99m, StockQuantity = 20 },
            new Product { Name = "Standing Desk", CategoryName = "Furniture", Price = 599.99m, StockQuantity = 10 },
            new Product { Name = "Monitor 27\"", CategoryName = "Electronics", Price = 399.99m, StockQuantity = 30 },
            new Product { Name = "USB-C Cable", CategoryName = "Electronics", Price = 12.99m, StockQuantity = 200, IsActive = false },
            new Product { Name = "Desk Lamp LED", CategoryName = "Furniture", Price = 49.99m, StockQuantity = 60 },
            new Product { Name = "Webcam HD", CategoryName = "Electronics", Price = 79.99m, StockQuantity = 45 },
        };

        foreach (var product in products)
        {
            await em.PersistAsync(product);
        }

        // Seed Orders
        var orders = new[]
        {
            new Order { OrderNumber = "ORD-2024-001", CustomerName = "John Doe", OrderDate = DateTime.UtcNow.AddDays(-10), TotalAmount = 1329.98m, Status = "Shipped", ShippedDate = DateTime.UtcNow.AddDays(-8) },
            new Order { OrderNumber = "ORD-2024-002", CustomerName = "Jane Smith", OrderDate = DateTime.UtcNow.AddDays(-8), TotalAmount = 599.99m, Status = "Delivered", ShippedDate = DateTime.UtcNow.AddDays(-6) },
            new Order { OrderNumber = "ORD-2024-003", CustomerName = "Bob Johnson", OrderDate = DateTime.UtcNow.AddDays(-5), TotalAmount = 179.97m, Status = "Processing", ShippedDate = null },
            new Order { OrderNumber = "ORD-2024-004", CustomerName = "Alice Williams", OrderDate = DateTime.UtcNow.AddDays(-3), TotalAmount = 1699.98m, Status = "Pending", ShippedDate = null },
            new Order { OrderNumber = "ORD-2024-005", CustomerName = "Charlie Brown", OrderDate = DateTime.UtcNow.AddDays(-1), TotalAmount = 42.98m, Status = "Pending", ShippedDate = null },
        };

        foreach (var order in orders)
        {
            await em.PersistAsync(order);
        }

        Console.WriteLine($"✓ Seeded {products.Length} products and {orders.Length} orders\n");
    }

    private async Task Example1_ComplexWhereConditions()
    {
        using var scope = _serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("1. Complex WHERE Conditions (AND/OR)");
        Console.WriteLine("-".PadRight(80, '-'));

        // Electronics under $100 OR Furniture over $200
        var query = em.CreateQuery<Product>(
            "SELECT p FROM Product p WHERE (p.CategoryName = @category1 AND p.Price < @maxPrice) OR (p.CategoryName = @category2 AND p.Price > @minPrice)")
            .SetParameter("category1", "Electronics")
            .SetParameter("maxPrice", 100m)
            .SetParameter("category2", "Furniture")
            .SetParameter("minPrice", 200m);

        var results = await query.GetResultListAsync();
        Console.WriteLine($"Found {results.Count()} products:");
        foreach (var product in results)
        {
            Console.WriteLine($"  - {product}");
        }
        Console.WriteLine();
    }

    private async Task Example2_RangeQueries()
    {
        using var scope = _serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("2. Range Queries (BETWEEN equivalent)");
        Console.WriteLine("-".PadRight(80, '-'));

        // Products priced between $50 and $500
        var query = em.CreateQuery<Product>(
            "SELECT p FROM Product p WHERE p.Price >= @minPrice AND p.Price <= @maxPrice AND p.IsActive = @active")
            .SetParameter("minPrice", 50m)
            .SetParameter("maxPrice", 500m)
            .SetParameter("active", true);

        var results = await query.GetResultListAsync();
        Console.WriteLine($"Products in $50-$500 range:");
        foreach (var product in results)
        {
            Console.WriteLine($"  - {product.Name}: ${product.Price}");
        }
        Console.WriteLine();
    }

    private async Task Example3_PatternMatching()
    {
        using var scope = _serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("3. Pattern Matching (LIKE equivalent)");
        Console.WriteLine("-".PadRight(80, '-'));

        // Products with "Laptop" in name
        var query = em.CreateQuery<Product>(
            "SELECT p FROM Product p WHERE p.Name LIKE @pattern")
            .SetParameter("pattern", "%Laptop%");

        var results = await query.GetResultListAsync();
        Console.WriteLine($"Products matching 'Laptop':");
        foreach (var product in results)
        {
            Console.WriteLine($"  - {product.Name}");
        }
        Console.WriteLine();
    }

    private async Task Example4_DateTimeQueries()
    {
        using var scope = _serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("4. DateTime Queries (Recent Orders)");
        Console.WriteLine("-".PadRight(80, '-'));

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var query = em.CreateQuery<Order>(
            "SELECT o FROM Order o WHERE o.OrderDate >= @startDate")
            .SetParameter("startDate", sevenDaysAgo);

        var results = await query.GetResultListAsync();
        Console.WriteLine($"Orders from last 7 days:");
        foreach (var order in results)
        {
            Console.WriteLine($"  - {order.OrderNumber}: {order.OrderDate:yyyy-MM-dd}");
        }
        Console.WriteLine();
    }

    private async Task Example5_NullHandling()
    {
        using var scope = _serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("5. NULL Handling (Unshipped Orders)");
        Console.WriteLine("-".PadRight(80, '-'));

        // Orders with no shipped date (NULL)
        var query = em.CreateQuery<Order>(
            "SELECT o FROM Order o WHERE o.ShippedDate IS NULL");

        var results = await query.GetResultListAsync();
        Console.WriteLine($"Unshipped orders:");
        foreach (var order in results)
        {
            Console.WriteLine($"  - {order.OrderNumber}: {order.Status}");
        }
        Console.WriteLine();
    }

    private async Task Example6_AggregationQueries()
    {
        using var scope = _serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("6. Aggregation Queries (COUNT, SUM, AVG)");
        Console.WriteLine("-".PadRight(80, '-'));

        // Count active products
        var countQuery = em.CreateQuery<Product>(
            "SELECT COUNT(p) FROM Product p WHERE p.IsActive = @active")
            .SetParameter("active", true);
        var activeCountResult = await countQuery.ExecuteScalarAsync();
        var activeCount = Convert.ToInt64(activeCountResult ?? 0);

        // Count electronics
        var electronicsQuery = em.CreateQuery<Product>(
            "SELECT COUNT(p) FROM Product p WHERE p.CategoryName = @category")
            .SetParameter("category", "Electronics");
        var electronicsCountResult = await electronicsQuery.ExecuteScalarAsync();
        var electronicsCount = Convert.ToInt64(electronicsCountResult ?? 0);

        // Total inventory value (conceptual - would need SUM support)
        Console.WriteLine($"Active products: {activeCount}");
        Console.WriteLine($"Electronics count: {electronicsCount}");
        Console.WriteLine($"Note: SUM/AVG aggregations require Phase 2.3 (JPQL)");
        Console.WriteLine();
    }

    private async Task Example7_BulkUpdates()
    {
        using var scope = _serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("7. Bulk UPDATE Operations");
        Console.WriteLine("-".PadRight(80, '-'));

        // Increase price of all Electronics by 5%
        var updateQuery = em.CreateQuery<Product>(
            "UPDATE Product p SET p.Price = p.Price * @multiplier WHERE p.CategoryName = @category")
            .SetParameter("multiplier", 1.05m)
            .SetParameter("category", "Furniture");

        var updatedCount = await updateQuery.ExecuteUpdateAsync();
        Console.WriteLine($"✓ Updated {updatedCount} Furniture items (+5% price)");
        Console.WriteLine();
    }

    private async Task Example8_MultipleParameters()
    {
        using var scope = _serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("8. Multiple Parameters (Complex Filtering)");
        Console.WriteLine("-".PadRight(80, '-'));

        // High-value electronics with good stock
        var query = em.CreateQuery<Product>(
            "SELECT p FROM Product p WHERE p.CategoryName = @cat AND p.Price > @price AND p.StockQuantity > @stock AND p.IsActive = @active")
            .SetParameter("cat", "Electronics")
            .SetParameter("price", 100m)
            .SetParameter("stock", 20)
            .SetParameter("active", true);

        var results = await query.GetResultListAsync();
        Console.WriteLine($"Premium electronics in stock:");
        foreach (var product in results)
        {
            Console.WriteLine($"  - {product.Name}: ${product.Price} (Stock: {product.StockQuantity})");
        }
        Console.WriteLine();
    }

    private async Task Example9_SubstringAndFunctions()
    {
        using var scope = _serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("9. String Functions (Prefix/Suffix Matching)");
        Console.WriteLine("-".PadRight(80, '-'));

        // Orders starting with specific prefix
        var query = em.CreateQuery<Order>(
            "SELECT o FROM Order o WHERE o.OrderNumber LIKE @pattern")
            .SetParameter("pattern", "ORD-2024%");

        var results = await query.GetResultListAsync();
        Console.WriteLine($"Orders from 2024:");
        foreach (var order in results)
        {
            Console.WriteLine($"  - {order.OrderNumber}: ${order.TotalAmount}");
        }
        Console.WriteLine();
    }

    private async Task Example10_StatusFiltering()
    {
        using var scope = _serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("10. Status-Based Filtering (IN equivalent)");
        Console.WriteLine("-".PadRight(80, '-'));

        // Pending or Processing orders
        var query1 = em.CreateQuery<Order>(
            "SELECT o FROM Order o WHERE o.Status = @status1 OR o.Status = @status2")
            .SetParameter("status1", "Pending")
            .SetParameter("status2", "Processing");

        var results = await query1.GetResultListAsync();
        Console.WriteLine($"Active orders (Pending/Processing):");
        foreach (var order in results)
        {
            Console.WriteLine($"  - {order.OrderNumber}: {order.Status} - ${order.TotalAmount}");
        }

        // Completed orders
        var query2 = em.CreateQuery<Order>(
            "SELECT o FROM Order o WHERE o.Status = @status")
            .SetParameter("status", "Delivered");
        var completedCount = await query2.GetResultListAsync();

        Console.WriteLine($"\nCompleted orders: {completedCount.Count()}");
        Console.WriteLine();
    }
}
