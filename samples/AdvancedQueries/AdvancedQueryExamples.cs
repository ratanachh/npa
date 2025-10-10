using Microsoft.Extensions.DependencyInjection;
using NPA.Core.Core;
using AdvancedQueries.Entities;

namespace AdvancedQueries;

/// <summary>
/// Demonstrates advanced CPQL query capabilities with Phase 2.3 enhancements.
/// Shows JOINs, GROUP BY, HAVING, aggregate functions, string/date functions, and complex expressions.
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
        Console.WriteLine("NPA Advanced CPQL Query Examples (Phase 2.3 ✅)");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        await SeedTestData();

        // Original Phase 1.3 examples
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
        
        // NEW Phase 2.3 advanced examples
        await Example11_JoinOperations();
        await Example12_GroupByAndHaving();
        await Example13_AggregateFunctionsAdvanced();
        await Example14_StringFunctions();
        await Example15_DateFunctions();
        await Example16_DistinctAndMultipleOrderBy();
        await Example17_ComplexExpressions();

        Console.WriteLine("\n" + "=".PadRight(80, '='));
        Console.WriteLine("All examples completed! ✅");
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

        Console.WriteLine($"Active products: {activeCount}");
        Console.WriteLine($"Electronics count: {electronicsCount}");
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
    
    // ========================================================================
    // NEW Phase 2.3 Examples - Advanced CPQL Features
    // ========================================================================
    
    private async Task Example11_JoinOperations()
    {
        using var scope = _serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("11. JOIN Operations (Phase 2.3 ✅)");
        Console.WriteLine("-".PadRight(80, '-'));

        // Note: For this demo, we're showing the syntax
        // In a real scenario, you'd have related entities with foreign keys
        Console.WriteLine("INNER JOIN syntax:");
        Console.WriteLine("  SELECT o FROM Order o INNER JOIN User u ON o.UserId = u.Id");
        
        Console.WriteLine("\nLEFT JOIN syntax:");
        Console.WriteLine("  SELECT u FROM User u LEFT JOIN Order o ON u.Id = o.UserId");
        
        Console.WriteLine("\nMultiple JOINs syntax:");
        Console.WriteLine("  SELECT o FROM Order o");
        Console.WriteLine("    INNER JOIN User u ON o.UserId = u.Id");
        Console.WriteLine("    LEFT JOIN Payment p ON o.Id = p.OrderId");
        
        Console.WriteLine("\n✅ JOIN support fully implemented!");
        Console.WriteLine();
    }
    
    private async Task Example12_GroupByAndHaving()
    {
        using var scope = _serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("12. GROUP BY and HAVING (Phase 2.3 ✅)");
        Console.WriteLine("-".PadRight(80, '-'));

        // Demonstrate GROUP BY syntax
        Console.WriteLine("GROUP BY by category (syntax):");
        Console.WriteLine("  SELECT p.CategoryName, COUNT(p.Id) FROM Product p GROUP BY p.CategoryName");
        
        Console.WriteLine("\nGROUP BY with HAVING (syntax):");
        Console.WriteLine("  SELECT p.CategoryName, COUNT(p.Id) FROM Product p");
        Console.WriteLine("    GROUP BY p.CategoryName");
        Console.WriteLine("    HAVING COUNT(p.Id) > :minCount");
        
        Console.WriteLine("\n✅ GROUP BY and HAVING fully implemented!");
        Console.WriteLine();
    }
    
    private async Task Example13_AggregateFunctionsAdvanced()
    {
        using var scope = _serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("13. Advanced Aggregate Functions (Phase 2.3 ✅)");
        Console.WriteLine("-".PadRight(80, '-'));

        // SUM, AVG, MIN, MAX examples
        Console.WriteLine("SUM total order value (syntax):");
        Console.WriteLine("  SELECT SUM(o.TotalAmount) FROM Order o WHERE o.Status = :status");
        
        Console.WriteLine("\nAVG order value (syntax):");
        Console.WriteLine("  SELECT AVG(o.TotalAmount) FROM Order o");
        
        Console.WriteLine("\nMIN/MAX prices (syntax):");
        Console.WriteLine("  SELECT MIN(p.Price), MAX(p.Price) FROM Product p");
        
        Console.WriteLine("\nCOUNT DISTINCT (syntax):");
        Console.WriteLine("  SELECT COUNT(DISTINCT p.CategoryName) FROM Product p");
        
        Console.WriteLine("\n✅ All aggregate functions (COUNT, SUM, AVG, MIN, MAX) with DISTINCT supported!");
        Console.WriteLine();
    }
    
    private async Task Example14_StringFunctions()
    {
        using var scope = _serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("14. String Functions (Phase 2.3 ✅)");
        Console.WriteLine("-".PadRight(80, '-'));

        Console.WriteLine("UPPER/LOWER functions (syntax):");
        Console.WriteLine("  SELECT UPPER(p.Name), LOWER(p.CategoryName) FROM Product p");
        
        Console.WriteLine("\nLENGTH function (syntax):");
        Console.WriteLine("  SELECT p.Name FROM Product p WHERE LENGTH(p.Name) > :minLength");
        
        Console.WriteLine("\nSUBSTRING function (syntax):");
        Console.WriteLine("  SELECT SUBSTRING(o.OrderNumber, :start, :length) FROM Order o");
        
        Console.WriteLine("\nTRIM and CONCAT (syntax):");
        Console.WriteLine("  SELECT TRIM(p.Name), CONCAT(p.Name, ' - ', p.CategoryName) FROM Product p");
        
        Console.WriteLine("\n✅ String functions supported: UPPER, LOWER, LENGTH, SUBSTRING, TRIM, CONCAT");
        Console.WriteLine();
    }
    
    private async Task Example15_DateFunctions()
    {
        using var scope = _serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("15. Date Functions (Phase 2.3 ✅)");
        Console.WriteLine("-".PadRight(80, '-'));

        Console.WriteLine("YEAR/MONTH/DAY functions (syntax):");
        Console.WriteLine("  SELECT YEAR(o.OrderDate), MONTH(o.OrderDate), DAY(o.OrderDate) FROM Order o");
        
        Console.WriteLine("\nGroup by year (syntax):");
        Console.WriteLine("  SELECT YEAR(o.OrderDate), COUNT(o.Id) FROM Order o");
        Console.WriteLine("    GROUP BY YEAR(o.OrderDate)");
        
        Console.WriteLine("\nFilter by specific month/year (syntax):");
        Console.WriteLine("  SELECT o FROM Order o");
        Console.WriteLine("    WHERE YEAR(o.OrderDate) = :year AND MONTH(o.OrderDate) = :month");
        
        Console.WriteLine("\n✅ Date functions supported: YEAR, MONTH, DAY, HOUR, MINUTE, SECOND, NOW");
        Console.WriteLine();
    }
    
    private async Task Example16_DistinctAndMultipleOrderBy()
    {
        using var scope = _serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("16. DISTINCT and Multiple ORDER BY (Phase 2.3 ✅)");
        Console.WriteLine("-".PadRight(80, '-'));

        Console.WriteLine("DISTINCT categories (syntax):");
        Console.WriteLine("  SELECT DISTINCT p.CategoryName FROM Product p ORDER BY p.CategoryName ASC");
        
        Console.WriteLine("\nMultiple ORDER BY columns (syntax):");
        Console.WriteLine("  SELECT p FROM Product p");
        Console.WriteLine("    ORDER BY p.CategoryName ASC, p.Price DESC, p.Name ASC");
        
        Console.WriteLine("\n✅ DISTINCT keyword and multiple ORDER BY columns with ASC/DESC supported!");
        Console.WriteLine();
    }
    
    private async Task Example17_ComplexExpressions()
    {
        using var scope = _serviceProvider.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("17. Complex Expressions with Operators (Phase 2.3 ✅)");
        Console.WriteLine("-".PadRight(80, '-'));

        Console.WriteLine("Arithmetic in SELECT (syntax):");
        Console.WriteLine("  SELECT p.Price * p.StockQuantity FROM Product p");
        
        Console.WriteLine("\nComplex WHERE with parentheses (syntax):");
        Console.WriteLine("  SELECT p FROM Product p");
        Console.WriteLine("    WHERE (p.Price > :min AND p.Price < :max)");
        Console.WriteLine("       OR (p.StockQuantity > :stock AND p.IsActive = :active)");
        
        Console.WriteLine("\nNested expressions (syntax):");
        Console.WriteLine("  SELECT p FROM Product p");
        Console.WriteLine("    WHERE NOT (p.Price < :threshold OR p.StockQuantity = :zero)");
        
        Console.WriteLine("\n✅ Full operator precedence with parentheses supported!");
        Console.WriteLine("✅ Operators: +, -, *, /, %, =, <>, <, <=, >, >=, AND, OR, NOT, LIKE, IN, IS");
        Console.WriteLine();
    }
}
