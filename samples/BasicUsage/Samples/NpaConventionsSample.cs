using System.Data;
using Dapper;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace BasicUsage.Samples;

/// <summary>
/// Demonstrates NPA query method naming conventions.
/// These methods require NO [Query] attribute - the SQL is generated automatically!
/// Based on Spring Data JPA conventions for developer familiarity.
/// </summary>
public class NpaConventionsSample
{
    public static async Task RunAsync(IDbConnection connection)
    {
        Console.WriteLine("=== NPA Query Method Conventions ===");
        Console.WriteLine();

        await SeedDataAsync(connection);
        
        // Note: In a real application, these repositories would be injected via DI
        // For this sample, we'll demonstrate the method signatures and expected SQL

        await DemonstrateComparisonOperatorsAsync();
        await DemonstrateStringOperatorsAsync();
        await DemonstrateCollectionOperatorsAsync();
        await DemonstrateNullAndBooleanChecksAsync();
        await DemonstrateDateTimeOperatorsAsync();
        await DemonstrateMultipleConditionsAsync();
        await DemonstrateOrderingAsync();
        await DemonstrateCountAndExistsAsync();
        await DemonstrateSynonymsAsync();
        await DemonstrateResultLimitingAsync();
        await DemonstrateRegexPatternMatchingAsync();
        await DemonstrateDistinctAsync();

        Console.WriteLine();
        Console.WriteLine("=== Sample Complete ===");
    }

    private static async Task SeedDataAsync(IDbConnection connection)
    {
        // Create sample table
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS catalog_products (
                id SERIAL PRIMARY KEY,
                name VARCHAR(200) NOT NULL,
                category VARCHAR(100),
                price DECIMAL(10, 2),
                stock_quantity INTEGER,
                is_active BOOLEAN DEFAULT TRUE,
                created_at TIMESTAMP DEFAULT NOW(),
                discontinued_at TIMESTAMP
            )");

        // Insert sample data
        await connection.ExecuteAsync(@"
            INSERT INTO catalog_products (name, category, price, stock_quantity, is_active, created_at) VALUES
            ('Laptop Pro', 'Electronics', 1299.99, 15, true, NOW() - INTERVAL '30 days'),
            ('Wireless Mouse', 'Electronics', 29.99, 150, true, NOW() - INTERVAL '10 days'),
            ('Office Desk', 'Furniture', 399.99, 8, true, NOW() - INTERVAL '5 days'),
            ('Ergonomic Chair', 'Furniture', 249.99, 0, true, NOW() - INTERVAL '2 days'),
            ('Old Keyboard', 'Electronics', 15.00, 5, false, NOW() - INTERVAL '90 days')
            ON CONFLICT DO NOTHING");
    }

    private static Task DemonstrateComparisonOperatorsAsync()
    {
        Console.WriteLine("--- Comparison Operators ---");
        Console.WriteLine("Method: FindByPriceGreaterThanAsync(decimal price)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE price > @price");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindByStockQuantityLessThanAsync(int quantity)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE stock_quantity < @quantity");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindByPriceGreaterThanEqualAsync(decimal price)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE price >= @price");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindByPriceBetweenAsync(decimal min, decimal max)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE price BETWEEN @min AND @max");
        Console.WriteLine();
        
        return Task.CompletedTask;
    }

    private static Task DemonstrateStringOperatorsAsync()
    {
        Console.WriteLine("--- String Operators ---");
        Console.WriteLine("Method: FindByNameContainingAsync(string keyword)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE name LIKE CONCAT('%', @keyword, '%')");
        Console.WriteLine("  Example: FindByNameContainingAsync(\"Pro\") → finds 'Laptop Pro'");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindByNameStartingWithAsync(string prefix)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE name LIKE CONCAT(@prefix, '%')");
        Console.WriteLine("  Example: FindByNameStartingWithAsync(\"Lap\") → finds 'Laptop Pro'");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindByCategoryEndingWithAsync(string suffix)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE category LIKE CONCAT('%', @suffix)");
        Console.WriteLine("  Example: FindByCategoryEndingWithAsync(\"ture\") → finds 'Furniture'");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindByNameNotContainingAsync(string keyword)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE name NOT LIKE CONCAT('%', @keyword, '%')");
        Console.WriteLine();
        
        return Task.CompletedTask;
    }

    private static Task DemonstrateCollectionOperatorsAsync()
    {
        Console.WriteLine("--- Collection Operators ---");
        Console.WriteLine("Method: FindByIdInAsync(long[] ids)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE id IN @ids");
        Console.WriteLine("  Example: FindByIdInAsync(new[] {1, 3, 5}) → finds products 1, 3, and 5");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindByCategoryNotInAsync(string[] categories)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE category NOT IN @categories");
        Console.WriteLine("  Example: FindByCategoryNotInAsync(new[] {\"Electronics\"}) → excludes electronics");
        Console.WriteLine();
        
        return Task.CompletedTask;
    }

    private static Task DemonstrateNullAndBooleanChecksAsync()
    {
        Console.WriteLine("--- Null and Boolean Checks ---");
        Console.WriteLine("Method: FindByDiscontinuedAtIsNullAsync()");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE discontinued_at IS NULL");
        Console.WriteLine("  Example: Finds all active (not discontinued) products");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindByDiscontinuedAtIsNotNullAsync()");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE discontinued_at IS NOT NULL");
        Console.WriteLine("  Example: Finds all discontinued products");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindByIsActiveTrueAsync()");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE is_active = TRUE");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindByIsActiveFalseAsync()");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE is_active = FALSE");
        Console.WriteLine();
        
        return Task.CompletedTask;
    }

    private static Task DemonstrateDateTimeOperatorsAsync()
    {
        Console.WriteLine("--- Date/Time Operators ---");
        Console.WriteLine("Method: FindByCreatedAtAfterAsync(DateTime date)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE created_at > @date");
        Console.WriteLine("  Example: FindByCreatedAtAfterAsync(DateTime.Now.AddDays(-7)) → products from last week");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindByCreatedAtBeforeAsync(DateTime date)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE created_at < @date");
        Console.WriteLine("  Example: FindByCreatedAtBeforeAsync(DateTime.Now.AddMonths(-1)) → products older than a month");
        Console.WriteLine();
        
        return Task.CompletedTask;
    }

    private static Task DemonstrateMultipleConditionsAsync()
    {
        Console.WriteLine("--- Multiple Conditions ---");
        Console.WriteLine("Method: FindByCategoryAndIsActiveTrueAsync(string category)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE category = @category AND is_active = TRUE");
        Console.WriteLine("  Example: FindByCategoryAndIsActiveTrueAsync(\"Electronics\") → active electronics only");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindByCategoryAndPriceGreaterThanAsync(string category, decimal price)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE category = @category AND price > @price");
        Console.WriteLine("  Example: FindByCategoryAndPriceGreaterThanAsync(\"Furniture\", 300) → expensive furniture");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindByStockQuantityLessThanAndIsActiveTrueAsync(int quantity)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE stock_quantity < @quantity AND is_active = TRUE");
        Console.WriteLine("  Example: FindByStockQuantityLessThanAndIsActiveTrueAsync(10) → low stock items");
        Console.WriteLine();
        
        return Task.CompletedTask;
    }

    private static Task DemonstrateOrderingAsync()
    {
        Console.WriteLine("--- Ordering ---");
        Console.WriteLine("Method: FindByCategoryOrderByPriceAscAsync(string category)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE category = @category ORDER BY price ASC");
        Console.WriteLine("  Example: Products in category sorted by price low to high");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindAllOrderByCreatedAtDescAsync()");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products ORDER BY created_at DESC");
        Console.WriteLine("  Example: All products sorted by newest first");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindByCategoryOrderByNameAscThenPriceDescAsync(string category)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE category = @category ORDER BY name ASC, price DESC");
        Console.WriteLine("  Example: Products sorted by name A-Z, then price high to low");
        Console.WriteLine();
        
        return Task.CompletedTask;
    }

    private static Task DemonstrateCountAndExistsAsync()
    {
        Console.WriteLine("--- Count and Exists ---");
        Console.WriteLine("Method: CountByCategoryAsync(string category)");
        Console.WriteLine("  SQL: SELECT COUNT(*) FROM catalog_products WHERE category = @category");
        Console.WriteLine("  Returns: long (number of products)");
        Console.WriteLine();
        
        Console.WriteLine("Method: CountByPriceLessThanAsync(decimal price)");
        Console.WriteLine("  SQL: SELECT COUNT(*) FROM catalog_products WHERE price < @price");
        Console.WriteLine("  Returns: long (number of affordable products)");
        Console.WriteLine();
        
        Console.WriteLine("Method: ExistsByNameAsync(string name)");
        Console.WriteLine("  SQL: SELECT COUNT(1) FROM catalog_products WHERE name = @name");
        Console.WriteLine("  Returns: bool (true if product exists)");
        Console.WriteLine();
        
        Console.WriteLine("Method: ExistsByCategoryAndStockQuantityGreaterThanAsync(string category, int quantity)");
        Console.WriteLine("  SQL: SELECT COUNT(1) FROM catalog_products WHERE category = @category AND stock_quantity > @quantity");
        Console.WriteLine("  Returns: bool (true if in-stock products exist in category)");
        Console.WriteLine();
        
        return Task.CompletedTask;
    }

    private static Task DemonstrateSynonymsAsync()
    {
        Console.WriteLine("--- Keyword Synonyms (NPA compatibility) ---");
        Console.WriteLine("All Is-prefix variants and shorthands are supported!");
        Console.WriteLine();
        
        Console.WriteLine("Comparison with 'Is' prefix:");
        Console.WriteLine("  FindByPriceIsGreaterThanAsync(decimal price) = FindByPriceGreaterThanAsync(decimal price)");
        Console.WriteLine("  FindByStockQuantityIsLessThanAsync(int qty) = FindByStockQuantityLessThanAsync(int qty)");
        Console.WriteLine();
        
        Console.WriteLine("String operators with synonyms:");
        Console.WriteLine("  FindByNameContainsAsync(string keyword) = FindByNameContainingAsync(string keyword)");
        Console.WriteLine("  FindByNameIsContainingAsync(string keyword) = FindByNameContainingAsync(string keyword)");
        Console.WriteLine("  FindByNameStartsWithAsync(string prefix) = FindByNameStartingWithAsync(string prefix)");
        Console.WriteLine("  FindByNameEndsWithAsync(string suffix) = FindByNameEndingWithAsync(string suffix)");
        Console.WriteLine();
        
        Console.WriteLine("Equality synonyms:");
        Console.WriteLine("  FindByNameAsync(string name) = default equality");
        Console.WriteLine("  FindByNameIsAsync(string name) = same as above");
        Console.WriteLine("  FindByNameEqualsAsync(string name) = same as above");
        Console.WriteLine();
        
        Console.WriteLine("Null check shorthands:");
        Console.WriteLine("  FindByDiscontinuedAtNullAsync() = FindByDiscontinuedAtIsNullAsync()");
        Console.WriteLine("  FindByDiscontinuedAtNotNullAsync() = FindByDiscontinuedAtIsNotNullAsync()");
        Console.WriteLine();
        
        Console.WriteLine("Boolean synonyms:");
        Console.WriteLine("  FindByIsActiveIsTrueAsync() = FindByIsActiveTrueAsync()");
        Console.WriteLine("  FindByIsActiveIsFalseAsync() = FindByIsActiveFalseAsync()");
        Console.WriteLine();
        
        Console.WriteLine("Case modifiers:");
        Console.WriteLine("  FindByNameIgnoreCaseAsync(string name) - case-insensitive search");
        Console.WriteLine("  FindByNameIgnoringCaseAsync(string name) - synonym for IgnoreCase");
        Console.WriteLine("  FindByNameAllIgnoreCaseAsync(string name) - all properties case-insensitive");
        Console.WriteLine();
        
        return Task.CompletedTask;
    }

    private static Task DemonstrateResultLimitingAsync()
    {
        Console.WriteLine("--- Result Limiting (NEW!) ---");
        Console.WriteLine("Limit the number of results returned");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindFirst5ByCategoryOrderByPriceAscAsync(string category)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE category = @category ORDER BY price ASC FETCH FIRST 5 ROWS ONLY");
        Console.WriteLine("  Example: Get 5 cheapest products in category");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindTop10ByOrderByCreatedAtDescAsync()");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products ORDER BY created_at DESC FETCH FIRST 10 ROWS ONLY");
        Console.WriteLine("  Example: Get 10 newest products");
        Console.WriteLine();
        
        Console.WriteLine("Method: GetTop3ByStockQuantityGreaterThanOrderByStockQuantityDescAsync(int minStock)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE stock_quantity > @minStock ORDER BY stock_quantity DESC FETCH FIRST 3 ROWS ONLY");
        Console.WriteLine("  Example: Top 3 most stocked items");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindFirstByCategoryAsync(string category)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE category = @category FETCH FIRST 1 ROWS ONLY");
        Console.WriteLine("  Example: Get first match (defaults to 1 if no number specified)");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindTop20ByPriceGreaterThanOrderByPriceAscAsync(decimal minPrice)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE price > @minPrice ORDER BY price ASC FETCH FIRST 20 ROWS ONLY");
        Console.WriteLine("  Example: Top 20 products above price threshold, sorted by price");
        Console.WriteLine();
        
        Console.WriteLine("💡 Tip: Always use OrderBy with First/Top for predictable results!");
        Console.WriteLine();
        
        return Task.CompletedTask;
    }

    private static Task DemonstrateRegexPatternMatchingAsync()
    {
        Console.WriteLine("--- Regex Pattern Matching (NEW!) ---");
        Console.WriteLine("Match text fields using regular expressions");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindByNameRegexAsync(string pattern)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE name REGEXP @pattern");
        Console.WriteLine("  Example: FindByNameRegexAsync(@\"^Laptop.*\") → products starting with 'Laptop'");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindByCategoryMatchesAsync(string pattern)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE category REGEXP @pattern");
        Console.WriteLine("  Example: FindByCategoryMatchesAsync(@\"^(Electronics|Furniture)$\") → exact category matches");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindByNameMatchesRegexAsync(string pattern)");
        Console.WriteLine("  SQL: SELECT * FROM catalog_products WHERE name REGEXP @pattern");
        Console.WriteLine("  Example: FindByNameMatchesRegexAsync(@\".*Mouse|Keyboard.*\") → products with Mouse or Keyboard");
        Console.WriteLine();
        
        Console.WriteLine("Real-world use cases:");
        Console.WriteLine("  - Email validation: FindByContactEmailRegexAsync(@\"^[a-z0-9._%+-]+@[a-z0-9.-]+\\.[a-z]{2,}$\")");
        Console.WriteLine("  - SKU format: FindBySkuMatchesAsync(@\"^[A-Z]{3}-\\d{4}$\") → ABC-1234 format");
        Console.WriteLine("  - Product codes: FindByCodeRegexAsync(@\"^PROD-[0-9]{6}$\") → PROD-123456");
        Console.WriteLine();
        
        Console.WriteLine("⚠️  Note: Regex uses REGEXP operator (MySQL/MariaDB/PostgreSQL)");
        Console.WriteLine("   SQL Server requires custom CLR implementation");
        Console.WriteLine();
        
        return Task.CompletedTask;
    }

    private static Task DemonstrateDistinctAsync()
    {
        Console.WriteLine("--- Distinct Queries ---");
        Console.WriteLine("Remove duplicate rows from results");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindDistinctByCategoryAsync(string category)");
        Console.WriteLine("  SQL: SELECT DISTINCT * FROM catalog_products WHERE category = @category");
        Console.WriteLine("  Example: Unique products in category (removes duplicates)");
        Console.WriteLine();
        
        Console.WriteLine("Method: CountDistinctByCategoryAsync(string category)");
        Console.WriteLine("  SQL: SELECT COUNT(DISTINCT *) FROM catalog_products WHERE category = @category");
        Console.WriteLine("  Example: Count unique products in category");
        Console.WriteLine();
        
        Console.WriteLine("Method: FindDistinctByIsActiveTrueOrderByNameAscAsync()");
        Console.WriteLine("  SQL: SELECT DISTINCT * FROM catalog_products WHERE is_active = TRUE ORDER BY name ASC");
        Console.WriteLine("  Example: Unique active products sorted by name");
        Console.WriteLine();
        
        return Task.CompletedTask;
    }
}

/// <summary>
/// Sample entity for demonstrating NPA conventions.
/// </summary>
[Entity]
[Table("catalog_products")]
public class CatalogProduct
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [Column("price")]
    public decimal Price { get; set; }

    [Column("stock_quantity")]
    public int StockQuantity { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("discontinued_at")]
    public DateTime? DiscontinuedAt { get; set; }
}

/// <summary>
/// Repository demonstrating ALL NPA query method conventions.
/// NO [Query] attributes needed - SQL is generated automatically!
/// </summary>
[Repository]
public interface ICatalogProductRepository : IRepository<CatalogProduct, long>
{
    // === Comparison Operators ===
    Task<IEnumerable<CatalogProduct>> FindByPriceGreaterThanAsync(decimal price);
    Task<IEnumerable<CatalogProduct>> FindByPriceLessThanAsync(decimal price);
    Task<IEnumerable<CatalogProduct>> FindByPriceGreaterThanEqualAsync(decimal price);
    Task<IEnumerable<CatalogProduct>> FindByPriceLessThanEqualAsync(decimal price);
    Task<IEnumerable<CatalogProduct>> FindByPriceBetweenAsync(decimal min, decimal max);
    Task<IEnumerable<CatalogProduct>> FindByStockQuantityLessThanAsync(int quantity);

    // === String Operators ===
    Task<IEnumerable<CatalogProduct>> FindByNameContainingAsync(string keyword);
    Task<IEnumerable<CatalogProduct>> FindByNameStartingWithAsync(string prefix);
    Task<IEnumerable<CatalogProduct>> FindByCategoryEndingWithAsync(string suffix);
    Task<IEnumerable<CatalogProduct>> FindByNameNotContainingAsync(string keyword);

    // === Collection Operators ===
    Task<IEnumerable<CatalogProduct>> FindByIdInAsync(long[] ids);
    Task<IEnumerable<CatalogProduct>> FindByCategoryNotInAsync(string[] categories);

    // === Null Checks ===
    Task<IEnumerable<CatalogProduct>> FindByDiscontinuedAtIsNullAsync();
    Task<IEnumerable<CatalogProduct>> FindByDiscontinuedAtIsNotNullAsync();

    // === Boolean Checks ===
    Task<IEnumerable<CatalogProduct>> FindByIsActiveTrueAsync();
    Task<IEnumerable<CatalogProduct>> FindByIsActiveFalseAsync();

    // === Date/Time Operators ===
    Task<IEnumerable<CatalogProduct>> FindByCreatedAtAfterAsync(DateTime date);
    Task<IEnumerable<CatalogProduct>> FindByCreatedAtBeforeAsync(DateTime date);

    // === Multiple Conditions with AND ===
    Task<IEnumerable<CatalogProduct>> FindByCategoryAndIsActiveTrueAsync(string category);
    Task<IEnumerable<CatalogProduct>> FindByCategoryAndPriceGreaterThanAsync(string category, decimal price);
    Task<IEnumerable<CatalogProduct>> FindByStockQuantityLessThanAndIsActiveTrueAsync(int quantity);

    // === Ordering ===
    Task<IEnumerable<CatalogProduct>> FindByCategoryOrderByPriceAscAsync(string category);
    Task<IEnumerable<CatalogProduct>> FindByCategoryOrderByPriceDescAsync(string category);
    Task<IEnumerable<CatalogProduct>> FindAllOrderByCreatedAtDescAsync();
    Task<IEnumerable<CatalogProduct>> FindByCategoryOrderByNameAscThenPriceDescAsync(string category);

    // === Count Methods ===
    Task<long> CountByCategoryAsync(string category);
    Task<long> CountByPriceLessThanAsync(decimal price);
    Task<long> CountByIsActiveTrueAsync();

    // === Exists Methods ===
    Task<bool> ExistsByNameAsync(string name);
    Task<bool> ExistsByCategoryAndStockQuantityGreaterThanAsync(string category, int quantity);

    // === Delete Methods ===
    Task DeleteByCategoryAsync(string category);
    Task DeleteByDiscontinuedAtIsNotNullAsync();

    // === Keyword Synonyms (all variants work!) ===
    // Comparison with Is-prefix
    Task<IEnumerable<CatalogProduct>> FindByPriceIsGreaterThanAsync(decimal price);
    Task<IEnumerable<CatalogProduct>> FindByStockQuantityIsLessThanAsync(int quantity);
    Task<IEnumerable<CatalogProduct>> FindByPriceIsBetweenAsync(decimal min, decimal max);
    
    // String operator synonyms
    Task<IEnumerable<CatalogProduct>> FindByNameContainsAsync(string keyword);
    Task<IEnumerable<CatalogProduct>> FindByNameIsContainingAsync(string keyword);
    Task<IEnumerable<CatalogProduct>> FindByNameStartsWithAsync(string prefix);
    Task<IEnumerable<CatalogProduct>> FindByNameIsStartingWithAsync(string prefix);
    Task<IEnumerable<CatalogProduct>> FindByNameEndsWithAsync(string suffix);
    Task<IEnumerable<CatalogProduct>> FindByNameIsEndingWithAsync(string suffix);
    
    // Equality synonyms
    Task<CatalogProduct?> FindByNameIsAsync(string name);
    Task<CatalogProduct?> FindByNameEqualsAsync(string name);
    
    // Inequality synonyms
    Task<IEnumerable<CatalogProduct>> FindByCategoryNotAsync(string category);
    Task<IEnumerable<CatalogProduct>> FindByCategoryIsNotAsync(string category);
    
    // Null check shorthands
    Task<IEnumerable<CatalogProduct>> FindByDiscontinuedAtNullAsync();
    Task<IEnumerable<CatalogProduct>> FindByDiscontinuedAtNotNullAsync();
    
    // Boolean synonyms
    Task<IEnumerable<CatalogProduct>> FindByIsActiveIsTrueAsync();
    Task<IEnumerable<CatalogProduct>> FindByIsActiveIsFalseAsync();
    
    // Date/Time synonyms
    Task<IEnumerable<CatalogProduct>> FindByCreatedAtIsAfterAsync(DateTime date);
    Task<IEnumerable<CatalogProduct>> FindByCreatedAtIsBeforeAsync(DateTime date);
    
    // Collection synonyms
    Task<IEnumerable<CatalogProduct>> FindByIdIsInAsync(long[] ids);
    Task<IEnumerable<CatalogProduct>> FindByCategoryIsNotInAsync(string[] categories);
    
    // Case-insensitive modifiers
    Task<CatalogProduct?> FindByNameIgnoreCaseAsync(string name);
    Task<CatalogProduct?> FindByNameIgnoringCaseAsync(string name);
    Task<IEnumerable<CatalogProduct>> FindByNameAllIgnoreCaseAsync(string name);

    // === Result Limiting (NEW!) ===
    Task<IEnumerable<CatalogProduct>> FindFirst5ByCategoryOrderByPriceAscAsync(string category);
    Task<IEnumerable<CatalogProduct>> FindTop10ByOrderByCreatedAtDescAsync();
    Task<IEnumerable<CatalogProduct>> GetTop3ByStockQuantityGreaterThanOrderByStockQuantityDescAsync(int minStock);
    Task<CatalogProduct?> FindFirstByCategoryAsync(string category);
    Task<IEnumerable<CatalogProduct>> FindTop20ByPriceGreaterThanOrderByPriceAscAsync(decimal minPrice);
    Task<IEnumerable<CatalogProduct>> GetFirst10ByIsActiveTrueAsync();
    Task<IEnumerable<CatalogProduct>> FindTop5ByCategoryAndPriceLessThanOrderByPriceDescAsync(string category, decimal maxPrice);

    // === Pattern Matching - Regex (NEW!) ===
    Task<IEnumerable<CatalogProduct>> FindByNameRegexAsync(string pattern);
    Task<IEnumerable<CatalogProduct>> FindByCategoryMatchesAsync(string pattern);
    Task<IEnumerable<CatalogProduct>> FindByNameMatchesRegexAsync(string pattern);
    Task<IEnumerable<CatalogProduct>> FindByNameIsMatchesAsync(string pattern);
    Task<IEnumerable<CatalogProduct>> FindByNameRegexAndIsActiveTrueAsync(string pattern);
    Task<IEnumerable<CatalogProduct>> FindByCategoryMatchesOrNameContainsAsync(string categoryPattern, string nameKeyword);

    // === Distinct Queries ===
    Task<IEnumerable<CatalogProduct>> FindDistinctByCategoryAsync(string category);
    Task<long> CountDistinctByCategoryAsync(string category);
    Task<IEnumerable<CatalogProduct>> FindDistinctByIsActiveTrueOrderByNameAscAsync();
    Task<IEnumerable<CatalogProduct>> FindDistinctByPriceGreaterThanAsync(decimal price);

    // === Advanced Combinations ===
    // Combine First/Top with other keywords
    Task<IEnumerable<CatalogProduct>> FindFirst10ByNameContainsAndIsActiveTrueOrderByPriceAscAsync(string keyword);
    Task<IEnumerable<CatalogProduct>> GetTop5ByPriceBetweenOrderByCreatedAtDescAsync(decimal min, decimal max);
    
    // Combine Regex with other keywords
    Task<IEnumerable<CatalogProduct>> FindByNameRegexAndPriceGreaterThanAsync(string pattern, decimal price);
    Task<IEnumerable<CatalogProduct>> FindFirst20ByNameMatchesOrderByNameAscAsync(string pattern);
    
    // Distinct with combinations
    Task<IEnumerable<CatalogProduct>> FindDistinctTop10ByCategoryOrderByPriceAscAsync(string category);
    Task<long> CountDistinctByPriceGreaterThanAndIsActiveTrueAsync(decimal price);
}
