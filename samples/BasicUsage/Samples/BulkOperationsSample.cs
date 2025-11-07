using NPA.Core.Annotations;
using NPA.Core.Core;
using System.Diagnostics;

namespace NPA.Samples.Samples;

/// <summary>
/// Demonstrates bulk operations for high-performance data manipulation.
/// 
/// Bulk operations provide significant performance improvements over individual
/// operations by batching database calls and using provider-specific optimizations:
/// - PostgreSQL: COPY command (binary format)
/// - SQL Server: SqlBulkCopy
/// - MySQL: Multi-row INSERT statements
/// - SQLite: Batch INSERT in transactions
/// 
/// This sample demonstrates:
/// 1. BulkInsert - Insert thousands of records efficiently
/// 2. BulkUpdate - Update thousands of records efficiently
/// 3. BulkDelete - Delete thousands of records efficiently
/// 4. Performance comparison vs single operations
/// </summary>
public class BulkOperationsSample
{
    private readonly IEntityManager _entityManager;

    public BulkOperationsSample(IEntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    /// <summary>
    /// Demo 1: Bulk Insert Performance
    /// 
    /// Demonstrates inserting 10,000 products using bulk operations.
    /// Typically 10-100x faster than individual inserts.
    /// </summary>
    public async Task Demo1_BulkInsert()
    {
        Console.WriteLine("\n=== Demo 1: Bulk Insert Performance ===");
        Console.WriteLine("Inserting 10,000 products using bulk operations");

        // Generate 10,000 test products
        var products = GenerateBulkProducts(10000);

        var stopwatch = Stopwatch.StartNew();
        var affectedRows = await _entityManager.BulkInsertAsync(products);
        stopwatch.Stop();

        Console.WriteLine($"✓ Bulk insert completed");
        Console.WriteLine($"  Records inserted: {affectedRows:N0}");
        Console.WriteLine($"  Time elapsed: {stopwatch.ElapsedMilliseconds:N0} ms");
        Console.WriteLine($"  Records/second: {(affectedRows / stopwatch.Elapsed.TotalSeconds):N0}");
        Console.WriteLine($"  Average per record: {(stopwatch.Elapsed.TotalMilliseconds / affectedRows):F2} ms");
    }

    /// <summary>
    /// Demo 2: Bulk Update Performance
    /// 
    /// Demonstrates updating 10,000 products using bulk operations.
    /// Applies a 10% price increase to all products.
    /// </summary>
    public async Task Demo2_BulkUpdate()
    {
        Console.WriteLine("\n=== Demo 2: Bulk Update Performance ===");
        Console.WriteLine("Updating 10,000 products (10% price increase)");

        // First, insert products
        var products = GenerateBulkProducts(10000);
        await _entityManager.BulkInsertAsync(products);

        // Apply 10% price increase
        foreach (var product in products)
        {
            product.Price *= 1.10m;
        }

        var stopwatch = Stopwatch.StartNew();
        var affectedRows = await _entityManager.BulkUpdateAsync(products);
        stopwatch.Stop();

        Console.WriteLine($"✓ Bulk update completed");
        Console.WriteLine($"  Records updated: {affectedRows:N0}");
        Console.WriteLine($"  Time elapsed: {stopwatch.ElapsedMilliseconds:N0} ms");
        Console.WriteLine($"  Records/second: {(affectedRows / stopwatch.Elapsed.TotalSeconds):N0}");
        Console.WriteLine($"  Average per record: {(stopwatch.Elapsed.TotalMilliseconds / affectedRows):F2} ms");
    }

    /// <summary>
    /// Demo 3: Bulk Delete Performance
    /// 
    /// Demonstrates deleting 10,000 products using bulk operations.
    /// Deletes by ID list.
    /// </summary>
    public async Task Demo3_BulkDelete()
    {
        Console.WriteLine("\n=== Demo 3: Bulk Delete Performance ===");
        Console.WriteLine("Deleting 10,000 products by ID");

        // First, insert products
        var products = GenerateBulkProducts(10000);
        await _entityManager.BulkInsertAsync(products);

        // Collect IDs (assuming sequential IDs starting from 1)
        var ids = Enumerable.Range(1, 10000).Select(i => (object)(long)i).ToList();

        var stopwatch = Stopwatch.StartNew();
        var affectedRows = await _entityManager.BulkDeleteAsync<BulkProduct>(ids);
        stopwatch.Stop();

        Console.WriteLine($"✓ Bulk delete completed");
        Console.WriteLine($"  Records deleted: {affectedRows:N0}");
        Console.WriteLine($"  Time elapsed: {stopwatch.ElapsedMilliseconds:N0} ms");
        Console.WriteLine($"  Records/second: {(affectedRows / stopwatch.Elapsed.TotalSeconds):N0}");
        Console.WriteLine($"  Average per record: {(stopwatch.Elapsed.TotalMilliseconds / affectedRows):F2} ms");
    }

    /// <summary>
    /// Demo 4: Performance Comparison - Bulk vs Individual Operations
    /// 
    /// Compares bulk insert vs individual PersistAsync calls.
    /// Demonstrates dramatic performance improvement.
    /// </summary>
    public async Task Demo4_PerformanceComparison()
    {
        Console.WriteLine("\n=== Demo 4: Performance Comparison ===");
        Console.WriteLine("Comparing bulk operations vs individual operations");

        const int recordCount = 1000; // Use smaller count for individual operations

        // Test 1: Individual inserts
        Console.WriteLine($"\n1. Individual inserts ({recordCount:N0} records)...");
        var individualProducts = GenerateBulkProducts(recordCount);

        var individualStopwatch = Stopwatch.StartNew();
        foreach (var product in individualProducts)
        {
            await _entityManager.PersistAsync(product);
        }
        await _entityManager.FlushAsync();
        individualStopwatch.Stop();

        Console.WriteLine($"   Time: {individualStopwatch.ElapsedMilliseconds:N0} ms");
        Console.WriteLine($"   Records/sec: {(recordCount / individualStopwatch.Elapsed.TotalSeconds):N0}");

        // Clean up
        var individualIds = Enumerable.Range(1, recordCount).Select(i => (object)(long)i).ToList();
        await _entityManager.BulkDeleteAsync<BulkProduct>(individualIds);

        // Test 2: Bulk insert
        Console.WriteLine($"\n2. Bulk insert ({recordCount:N0} records)...");
        var bulkProducts = GenerateBulkProducts(recordCount);

        var bulkStopwatch = Stopwatch.StartNew();
        await _entityManager.BulkInsertAsync(bulkProducts);
        bulkStopwatch.Stop();

        Console.WriteLine($"   Time: {bulkStopwatch.ElapsedMilliseconds:N0} ms");
        Console.WriteLine($"   Records/sec: {(recordCount / bulkStopwatch.Elapsed.TotalSeconds):N0}");

        // Calculate improvement
        var improvement = (double)individualStopwatch.ElapsedMilliseconds / bulkStopwatch.ElapsedMilliseconds;
        Console.WriteLine($"\n✓ Performance Improvement: {improvement:F1}x faster");
        Console.WriteLine($"  Bulk operations are {improvement:F1} times faster than individual operations");
    }

    /// <summary>
    /// Demo 5: Large Dataset Handling
    /// 
    /// Demonstrates handling very large datasets (100,000 records).
    /// Shows batching and progress tracking.
    /// </summary>
    public async Task Demo5_LargeDataset()
    {
        Console.WriteLine("\n=== Demo 5: Large Dataset Handling ===");
        Console.WriteLine("Inserting 100,000 products in batches");

        const int totalRecords = 100000;
        const int batchSize = 10000;
        var batches = totalRecords / batchSize;

        var overallStopwatch = Stopwatch.StartNew();
        var totalInserted = 0;

        for (int batch = 0; batch < batches; batch++)
        {
            var batchProducts = GenerateBulkProducts(batchSize, batch * batchSize);
            
            var batchStopwatch = Stopwatch.StartNew();
            var inserted = await _entityManager.BulkInsertAsync(batchProducts);
            batchStopwatch.Stop();

            totalInserted += inserted;
            var progress = ((batch + 1) * 100.0 / batches);
            
            Console.WriteLine($"  Batch {batch + 1}/{batches}: {inserted:N0} records in {batchStopwatch.ElapsedMilliseconds:N0} ms ({progress:F0}% complete)");
        }

        overallStopwatch.Stop();

        Console.WriteLine($"\n✓ Large dataset insert completed");
        Console.WriteLine($"  Total records: {totalInserted:N0}");
        Console.WriteLine($"  Total time: {overallStopwatch.Elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine($"  Overall records/second: {(totalInserted / overallStopwatch.Elapsed.TotalSeconds):N0}");
    }

    /// <summary>
    /// Demo 6: Bulk Operations with Complex Data
    /// 
    /// Demonstrates bulk operations with categories and their products.
    /// Shows handling of related entities.
    /// </summary>
    public async Task Demo6_ComplexData()
    {
        Console.WriteLine("\n=== Demo 6: Bulk Operations with Complex Data ===");
        Console.WriteLine("Inserting categories and products");

        // Insert categories in bulk
        var categories = new List<BulkCategory>
        {
            new BulkCategory { Name = "Electronics", Description = "Electronic devices and accessories" },
            new BulkCategory { Name = "Books", Description = "Physical and digital books" },
            new BulkCategory { Name = "Clothing", Description = "Apparel and accessories" },
            new BulkCategory { Name = "Home & Garden", Description = "Home improvement and gardening" },
            new BulkCategory { Name = "Sports", Description = "Sports equipment and apparel" }
        };

        Console.WriteLine($"\nInserting {categories.Count} categories...");
        var categoriesInserted = await _entityManager.BulkInsertAsync(categories);
        Console.WriteLine($"✓ {categoriesInserted} categories inserted");

        // Insert products for each category
        Console.WriteLine($"\nInserting products for each category...");
        var allProducts = new List<BulkProduct>();
        
        for (int i = 0; i < categories.Count; i++)
        {
            var categoryProducts = GenerateBulkProducts(2000, i * 2000, categories[i].Name);
            allProducts.AddRange(categoryProducts);
        }

        var stopwatch = Stopwatch.StartNew();
        var productsInserted = await _entityManager.BulkInsertAsync(allProducts);
        stopwatch.Stop();

        Console.WriteLine($"✓ {productsInserted:N0} products inserted across {categories.Count} categories");
        Console.WriteLine($"  Time elapsed: {stopwatch.ElapsedMilliseconds:N0} ms");
        Console.WriteLine($"  Records/second: {(productsInserted / stopwatch.Elapsed.TotalSeconds):N0}");
    }

    /// <summary>
    /// Generates test products with sequential data.
    /// </summary>
    private List<BulkProduct> GenerateBulkProducts(int count, int startIndex = 0, string category = "General")
    {
        return Enumerable.Range(startIndex + 1, count)
            .Select(i => new BulkProduct
            {
                Name = $"{category} Product {i}",
                Sku = $"SKU-{category.ToUpper().Replace(" ", "")}-{i:D6}",
                Description = $"High-quality {category.ToLower()} product #{i}",
                Price = (i % 1000) + 9.99m,
                Stock = (i % 500) + 1,
                Category = category,
                IsActive = i % 10 != 0 // 90% active
            })
            .ToList();
    }
}

// ============================================================================
// Entity Definitions
// ============================================================================

/// <summary>
/// Product entity for bulk operations demonstration.
/// </summary>
[Entity]
[Table("bulk_products")]
public class BulkProduct
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("sku")]
    public string Sku { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    [Column("stock")]
    public int Stock { get; set; }

    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Category entity for complex bulk operations.
/// </summary>
[Entity]
[Table("bulk_categories")]
public class BulkCategory
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
