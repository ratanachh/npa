# Phase 1.3: CPQL Query API Sample

## 📋 Task Overview

**Objective**: Demonstrate NPA's CPQL (Custom Persistence Query Language) for building dynamic queries.

**Priority**: High  
**Estimated Time**: 3-4 hours  
**Dependencies**: Phase 1.1 (Entity Mapping), Phase 1.2 (CRUD Operations)  
**Target Framework**: .NET 8.0  
**Sample Name**: CpqlQuerySample

## 🎯 Success Criteria

- [ ] Demonstrates CPQL query syntax
- [ ] Shows parameter binding
- [ ] Includes single result queries
- [ ] Demonstrates list result queries
- [ ] Shows update queries
- [ ] Includes aggregate queries (COUNT, SUM)
- [ ] Uses Dapper-powered execution
- [ ] Production-ready error handling

## 📝 Detailed Requirements

### 1. CPQL Query Types

**SELECT Queries** - Retrieve entities
**UPDATE Queries** - Bulk update operations
**Aggregate Queries** - COUNT, SUM, AVG, etc.

> **Note**: CPQL is NPA's lightweight query language. JPQL-like syntax is planned for Phase 2.3.

### 2. Query Features

- Parameter binding for SQL injection prevention
- Type-safe result mapping
- Single and multiple result handling
- Aggregate operations

## 🏗️ Implementation Plan

### Step 1: Project Setup
```bash
dotnet new console -n CpqlQuerySample
cd CpqlQuerySample
dotnet add reference ../../src/NPA.Core/NPA.Core.csproj
dotnet add reference ../../src/NPA.Providers.PostgreSql/NPA.Providers.PostgreSql.csproj
dotnet add package Npgsql --version 9.0.3
dotnet add package Testcontainers --version 3.6.0
dotnet add package Testcontainers.PostgreSql --version 3.6.0
```

### Step 2: Create Entity Classes

**Product.cs**
```csharp
using NPA.Core.Annotations;

namespace CpqlQuerySample.Entities;

[Entity]
[Table("products")]
public class Product
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("name", Length = 200, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [Column("category", Length = 50)]
    public string Category { get; set; } = string.Empty;

    [Column("price", Precision = 18, Scale = 2, IsNullable = false)]
    public decimal Price { get; set; }

    [Column("stock_quantity", IsNullable = false)]
    public int StockQuantity { get; set; }

    [Column("is_active", IsNullable = false)]
    public bool IsActive { get; set; } = true;

    [Column("created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"Product[Id={Id}, Name={Name}, Category={Category}, Price={Price:C}, Stock={StockQuantity}, Active={IsActive}]";
    }
}
```

### Step 3: Create Query Demonstrations

**QueryExamples.cs**
```csharp
using NPA.Core.Core;
using CpqlQuerySample.Entities;

namespace CpqlQuerySample;

public class QueryExamples
{
    private readonly IEntityManager _entityManager;

    public QueryExamples(IEntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    public async Task RunAllExamples()
    {
        Console.WriteLine("=".PadRight(70, '='));
        Console.WriteLine("CPQL Query API Demo");
        Console.WriteLine("=".PadRight(70, '='));
        Console.WriteLine();

        await SeedDataAsync();
        
        await Example1_SimpleSelect();
        await Example2_ParameterizedQuery();
        await Example3_SingleResult();
        await Example4_AggregateQueries();
        await Example5_UpdateQuery();
        await Example6_ComplexWhere();
    }

    private async Task SeedDataAsync()
    {
        Console.WriteLine("Seeding test data...\n");
        
        var products = new[]
        {
            new Product { Name = "Laptop Pro", Category = "Electronics", Price = 1299.99m, StockQuantity = 15 },
            new Product { Name = "Wireless Mouse", Category = "Electronics", Price = 29.99m, StockQuantity = 100 },
            new Product { Name = "Office Chair", Category = "Furniture", Price = 199.99m, StockQuantity = 25 },
            new Product { Name = "Desk Lamp", Category = "Furniture", Price = 49.99m, StockQuantity = 50 },
            new Product { Name = "USB Cable", Category = "Electronics", Price = 9.99m, StockQuantity = 200 }
        };

        foreach (var product in products)
        {
            await _entityManager.PersistAsync(product);
        }
        
        Console.WriteLine($"✓ Seeded {products.Length} products\n");
    }

    private async Task Example1_SimpleSelect()
    {
        Console.WriteLine("1. Simple SELECT Query");
        Console.WriteLine("-".PadRight(70, '-'));

        // CPQL: SELECT all products
        var query = _entityManager.CreateQuery<Product>(
            "SELECT p FROM Product p");

        var products = await query.GetResultListAsync();

        Console.WriteLine($"Found {products.Count()} products:");
        foreach (var product in products)
        {
            Console.WriteLine($"  - {product}");
        }
        Console.WriteLine();
    }

    private async Task Example2_ParameterizedQuery()
    {
        Console.WriteLine("2. Parameterized Query (SQL Injection Safe)");
        Console.WriteLine("-".PadRight(70, '-'));

        // CPQL with parameters
        var query = _entityManager.CreateQuery<Product>(
            "SELECT p FROM Product p WHERE p.Category = @category")
            .SetParameter("category", "Electronics");

        var electronics = await query.GetResultListAsync();

        Console.WriteLine($"Found {electronics.Count()} electronics:");
        foreach (var product in electronics)
        {
            Console.WriteLine($"  - {product.Name} (${product.Price})");
        }
        Console.WriteLine();
    }

    private async Task Example3_SingleResult()
    {
        Console.WriteLine("3. Single Result Query");
        Console.WriteLine("-".PadRight(70, '-'));

        // Get specific product by name
        var query = _entityManager.CreateQuery<Product>(
            "SELECT p FROM Product p WHERE p.Name = @name")
            .SetParameter("name", "Laptop Pro");

        var product = await query.GetSingleResultAsync();

        if (product != null)
        {
            Console.WriteLine($"Found: {product}");
        }
        else
        {
            Console.WriteLine("Product not found");
        }
        Console.WriteLine();
    }

    private async Task Example4_AggregateQueries()
    {
        Console.WriteLine("4. Aggregate Queries (COUNT, SUM)");
        Console.WriteLine("-".PadRight(70, '-'));

        // Count all products
        var countQuery = _entityManager.CreateQuery<Product>(
            "SELECT COUNT(p) FROM Product p");
        var count = await countQuery.ExecuteScalarAsync<long>();
        Console.WriteLine($"Total products: {count}");

        // Count by category
        var categoryCountQuery = _entityManager.CreateQuery<Product>(
            "SELECT COUNT(p) FROM Product p WHERE p.Category = @category")
            .SetParameter("category", "Electronics");
        var electronicsCount = await categoryCountQuery.ExecuteScalarAsync<long>();
        Console.WriteLine($"Electronics count: {electronicsCount}");

        Console.WriteLine();
    }

    private async Task Example5_UpdateQuery()
    {
        Console.WriteLine("5. Bulk UPDATE Query");
        Console.WriteLine("-".PadRight(70, '-'));

        // Increase prices for a category
        var updateQuery = _entityManager.CreateQuery<Product>(
            "UPDATE Product p SET p.Price = p.Price * @multiplier WHERE p.Category = @category")
            .SetParameter("multiplier", 1.10m)
            .SetParameter("category", "Furniture");

        var updatedCount = await updateQuery.ExecuteUpdateAsync();

        Console.WriteLine($"✓ Updated {updatedCount} furniture item prices (+10%)");
        
        // Verify the update
        var furnitureQuery = _entityManager.CreateQuery<Product>(
            "SELECT p FROM Product p WHERE p.Category = @category")
            .SetParameter("category", "Furniture");
        var furniture = await furnitureQuery.GetResultListAsync();
        
        foreach (var item in furniture)
        {
            Console.WriteLine($"  - {item.Name}: ${item.Price:F2}");
        }
        Console.WriteLine();
    }

    private async Task Example6_ComplexWhere()
    {
        Console.WriteLine("6. Complex WHERE Conditions");
        Console.WriteLine("-".PadRight(70, '-'));

        // Multiple conditions with AND
        var query = _entityManager.CreateQuery<Product>(
            "SELECT p FROM Product p WHERE p.Price > @minPrice AND p.StockQuantity < @maxStock AND p.IsActive = @active")
            .SetParameter("minPrice", 50.0m)
            .SetParameter("maxStock", 100)
            .SetParameter("active", true);

        var products = await query.GetResultListAsync();

        Console.WriteLine($"Products (Price > $50, Stock < 100, Active):");
        foreach (var product in products)
        {
            Console.WriteLine($"  - {product.Name}: ${product.Price}, Stock: {product.StockQuantity}");
        }
        Console.WriteLine();
    }
}
```

### Step 4: Main Program

**Program.cs**
```csharp
using Microsoft.Extensions.Logging;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Providers.PostgreSql;
using Npgsql;
using Testcontainers.PostgreSql;
using CpqlQuerySample;

// Setup logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Warning);
});

var logger = loggerFactory.CreateLogger<EntityManager>();

// Start PostgreSQL container
var container = new PostgreSqlBuilder()
    .WithImage("postgres:17-alpine")
    .WithDatabase("sampledb")
    .WithUsername("sample_user")
    .WithPassword("sample_password")
    .Build();

Console.WriteLine("Starting PostgreSQL container...");
await container.StartAsync();

var connectionString = container.GetConnectionString();
var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

// Create table
const string createTableSql = @"
    CREATE TABLE IF NOT EXISTS products (
        id BIGSERIAL PRIMARY KEY,
        name VARCHAR(200) NOT NULL,
        category VARCHAR(50),
        price DECIMAL(18,2) NOT NULL,
        stock_quantity INTEGER NOT NULL,
        is_active BOOLEAN NOT NULL DEFAULT true,
        created_at TIMESTAMP NOT NULL
    );";

await using var command = new NpgsqlCommand(createTableSql, connection);
await command.ExecuteNonQueryAsync();

Console.WriteLine("Database ready!\n");

// Setup NPA
var metadataProvider = new MetadataProvider();
var databaseProvider = new PostgreSqlProvider();
var entityManager = new EntityManager(connection, metadataProvider, databaseProvider, logger);

// Run query examples
var examples = new QueryExamples(entityManager);
await examples.RunAllExamples();

// Cleanup
await connection.CloseAsync();
await connection.DisposeAsync();
await container.StopAsync();
await container.DisposeAsync();

Console.WriteLine("Press any key to exit...");
Console.ReadKey();
```

## 📁 Project Structure

```
samples/CpqlQuerySample/
├── CpqlQuerySample.csproj
├── Program.cs
├── QueryExamples.cs
├── README.md
└── Entities/
    └── Product.cs
```

## 🧪 Test Cases

- [ ] Simple SELECT returns all records
- [ ] Parameterized queries prevent SQL injection
- [ ] Single result query returns correct entity
- [ ] Aggregate queries return correct counts
- [ ] UPDATE queries modify correct records
- [ ] Complex WHERE conditions filter properly
- [ ] No runtime exceptions occur

## 📚 Learning Outcomes

- CPQL query syntax and structure
- Safe parameter binding
- Single vs list results
- Aggregate query operations
- Bulk update operations
- Dapper-powered query execution
- Query performance patterns

## 🔗 Key Features Demonstrated

- **Type Safety**: Strongly-typed query results
- **SQL Injection Prevention**: Parameterized queries
- **Dapper Integration**: High-performance query execution
- **Flexible Syntax**: Similar to JPA/JPQL but lightweight
- **Aggregate Support**: COUNT, SUM, and other operations

## 💡 Best Practices

1. **Always use parameters** for dynamic values
2. **Use single result methods** when expecting one entity
3. **Handle null results** from queries
4. **Dispose resources** properly
5. **Log query execution** for debugging

## 🔄 Next Steps

- Explore Phase 1.4 - SQL Server Provider
- Learn Phase 2.3 - JPQL-like Query Language (advanced)
- Try Phase 2.4 - Repository Pattern

---

*Created: October 8, 2025*  
*Status: ✅ Ready to Implement*
