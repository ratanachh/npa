# NPA Query Method Keywords - Complete Reference

This reference documents all supported NPA query method keywords. See `NpaConventionsSample.cs` for working code examples.

These conventions are based on Spring Data JPA for developer familiarity and ease of adoption.

## Table of Contents
- [Subject Keywords](#subject-keywords)
- [Custom Query Attributes](#custom-query-attributes-new)
- [Comparison Operators](#comparison-operators)
- [String Operators](#string-operators)
- [Collection Operators](#collection-operators)
- [Null Checks](#null-checks)
- [Boolean Values](#boolean-values)
- [Date/Time Operators](#date-time-operators)
- [Logical Operators](#logical-operators)
- [Modifiers](#modifiers)
- [Result Limiting](#result-limiting-new)
- [Pattern Matching](#pattern-matching-regex-new)
- [Keyword Synonyms](#keyword-synonyms)
- [Advanced Examples](#advanced-examples)

---

## Subject Keywords

Control the type of query operation:

| Keyword | Query Type | Returns | Example |
|---------|------------|---------|---------|
| `Find` | SELECT | Collection or single entity | `FindByCategoryAsync` |
| `Get` | SELECT | Collection or single entity | `GetByNameAsync` |
| `Query` | SELECT | Collection or single entity | `QueryByPriceAsync` |
| `Search` | SELECT | Collection or single entity | `SearchByKeywordAsync` |
| `Read` | SELECT | Collection or single entity | `ReadByIdAsync` |
| `Stream` | SELECT | Collection or single entity | `StreamByStatusAsync` |
| `Count` | COUNT | `long` or `int` | `CountByCategoryAsync` |
| `Exists` | EXISTS | `bool` | `ExistsByNameAsync` |
| `Delete` | DELETE | `Task` or affected rows | `DeleteByIdAsync` |
| `Remove` | DELETE | `Task` or affected rows | `RemoveByStatusAsync` |

---

## Custom Query Attributes 🆕

Override method naming conventions with custom SQL or CPQL queries (NEW in this version):

| Attribute | Purpose | Return Type | Example |
|-----------|---------|-------------|---------|
| `[Query]` | Execute custom SQL/CPQL query | Collection or single entity | `[Query("SELECT * FROM Products WHERE Price > @price")]` |
| `[StoredProcedure]` | Call stored procedure | Collection or single entity | `[StoredProcedure("sp_GetTopProducts")]` |

**Query Attribute:**
Use `[Query]` to execute custom SQL or CPQL (Custom Persistence Query Language) while maintaining repository pattern.

CPQL is a simplified entity-based query syntax similar to JPA that gets automatically converted to SQL:

```csharp
public interface IProductRepository : IRepository<Product, long>
{
    // CPQL query (entity-based, automatically converted to SQL)
    [Query("SELECT p FROM Product p WHERE p.Price > :price")]
    Task<IEnumerable<Product>> GetExpensiveProductsAsync(decimal price);
    
    // Or use standard SQL directly
    [Query("SELECT * FROM Products WHERE Price > @price AND Category = @category")]
    Task<IEnumerable<Product>> GetExpensiveProducts2Async(decimal price, string category);
    
    // CPQL with aggregate functions
    [Query("SELECT COUNT(p) FROM Product p WHERE p.Category = :category")]
    Task<long> CountByCategoryAsync(string category);
    
    // CPQL with complex conditions
    [Query("SELECT p FROM Product p WHERE p.Price BETWEEN :min AND :max ORDER BY p.Price DESC")]
    Task<IEnumerable<Product>> GetProductsInRangeAsync(decimal min, decimal max);
}
```

**CPQL Syntax:**
- Entity-based: `SELECT p FROM Product p` instead of `SELECT * FROM Products`
- Property access: `p.Price`, `p.Category` (alias is removed automatically)
- Parameters: Use `:param` syntax (converted to `@param` for SQL)
- Supports: SELECT, WHERE, ORDER BY, aggregate functions (COUNT, AVG, SUM, MAX, MIN)
- Automatically converts to optimized SQL for your database

**SQL Syntax:**
You can also use standard SQL directly:
- Table-based: `SELECT * FROM Products`
- Parameters: Use `@param` syntax directly
- Full SQL power: joins, CTEs, window functions, etc.

**StoredProcedure Attribute:**
Use `[StoredProcedure]` to call database stored procedures:

```csharp
public interface IProductRepository : IRepository<Product, long>
{
    // Call stored procedure
    [StoredProcedure("sp_GetTopProducts")]
    Task<IEnumerable<Product>> GetTopProductsAsync(int count);
    
    // Stored procedure with multiple parameters
    [StoredProcedure("sp_SearchProducts")]
    Task<IEnumerable<Product>> SearchProductsAsync(
        string keyword, 
        decimal minPrice, 
        decimal maxPrice
    );
    
    // Stored procedure returning single entity
    [StoredProcedure("sp_GetProductDetails")]
    Task<Product> GetProductDetailsAsync(long productId);
}
```

**Parameter Binding:**
- CPQL: Use `:param` syntax (e.g., `:price`, `:category`)
- SQL: Use `@param` syntax (e.g., `@price`, `@category`)
- Parameters are matched by name to method parameters
- Supports all primitive types and strings
- Multiple parameters supported

**💡 Best Practices:**
- Use CPQL for entity-based queries (portable, database-agnostic)
- Use SQL for complex joins, CTEs, or database-specific features
- Use `[Query]` for complex queries that can't be expressed with conventions
- Use `[StoredProcedure]` to leverage existing database logic
- Keep queries readable with multiline strings (`@"..."`)
- Add comments to explain complex custom queries
- Consider using conventions first; custom queries are for special cases

---

## Comparison Operators

Compare numeric and date values:

| Keyword | SQL Operator | Example Method | Parameters |
|---------|--------------|----------------|------------|
| `GreaterThan` | `>` | `FindByPriceGreaterThanAsync(decimal price)` | 1 parameter |
| `GreaterThanEqual` | `>=` | `FindByAgeGreaterThanEqualAsync(int age)` | 1 parameter |
| `LessThan` | `<` | `FindByStockLessThanAsync(int stock)` | 1 parameter |
| `LessThanEqual` | `<=` | `FindByRatingLessThanEqualAsync(decimal rating)` | 1 parameter |
| `Between` | `BETWEEN ... AND` | `FindByPriceBetweenAsync(decimal min, decimal max)` | 2 parameters |

**Synonyms:** All comparison keywords support `Is` prefix: `IsGreaterThan`, `IsLessThan`, `IsBetween`, etc.

**Examples:**
```csharp
// Find expensive products
var products = await repo.FindByPriceGreaterThanAsync(100.00m);

// Find products in price range
var affordable = await repo.FindByPriceBetweenAsync(10.00m, 50.00m);

// Synonym - same as GreaterThan
var same = await repo.FindByPriceIsGreaterThanAsync(100.00m);
```

---

## String Operators

Match text patterns:

| Keyword | SQL Pattern | Example Method | Matches |
|---------|-------------|----------------|---------|
| `Like` | `LIKE '%value%'` | `FindByNameLikeAsync(string pattern)` | Contains |
| `Containing` | `LIKE '%value%'` | `FindByNameContainingAsync(string keyword)` | Contains |
| `NotLike` | `NOT LIKE '%value%'` | `FindByNameNotLikeAsync(string pattern)` | Not contains |
| `NotContaining` | `NOT LIKE '%value%'` | `FindByNameNotContainingAsync(string keyword)` | Not contains |
| `StartingWith` | `LIKE 'value%'` | `FindByEmailStartingWithAsync(string prefix)` | Starts with |
| `EndingWith` | `LIKE '%value'` | `FindByNameEndingWithAsync(string suffix)` | Ends with |

**Synonyms:**
- `Containing` → `Contains`, `IsContaining`
- `StartingWith` → `StartsWith`, `IsStartingWith`
- `EndingWith` → `EndsWith`, `IsEndingWith`
- `Like` → `IsLike`
- `NotLike` → `IsNotLike`

**Examples:**
```csharp
// Find products containing "Pro"
var products = await repo.FindByNameContainingAsync("Pro");

// Find emails starting with "admin"
var admins = await repo.FindByEmailStartingWithAsync("admin");

// All these are equivalent
var v1 = await repo.FindByNameContainingAsync("laptop");
var v2 = await repo.FindByNameContainsAsync("laptop");
var v3 = await repo.FindByNameIsContainingAsync("laptop");
```

---

## Collection Operators

Match against collections:

| Keyword | SQL Operator | Example Method | Parameter Type |
|---------|--------------|----------------|----------------|
| `In` | `IN (...)` | `FindByIdInAsync(long[] ids)` | Array/List |
| `NotIn` | `NOT IN (...)` | `FindByStatusNotInAsync(string[] statuses)` | Array/List |

**Synonyms:** `IsIn`, `IsNotIn`

**Examples:**
```csharp
// Find specific products by IDs
var products = await repo.FindByIdInAsync(new[] { 1L, 3L, 5L });

// Exclude certain categories
var filtered = await repo.FindByCategoryNotInAsync(new[] { "Electronics", "Toys" });

// Synonym
var same = await repo.FindByIdIsInAsync(new[] { 1L, 3L, 5L });
```

---

## Null Checks

Check for null or non-null values:

| Keyword | SQL Operator | Example Method | Parameters |
|---------|--------------|----------------|------------|
| `IsNull` | `IS NULL` | `FindByDeletedAtIsNullAsync()` | None |
| `IsNotNull` | `IS NOT NULL` | `FindByEmailIsNotNullAsync()` | None |
| `Null` | `IS NULL` | `FindByDeletedAtNullAsync()` | None (shorthand) |
| `NotNull` | `IS NOT NULL` | `FindByEmailNotNullAsync()` | None (shorthand) |

**Examples:**
```csharp
// Find active (not deleted) products
var active = await repo.FindByDeletedAtIsNullAsync();

// Find products with email addresses
var withEmail = await repo.FindByEmailIsNotNullAsync();

// Shorthand - same as IsNull
var deleted = await repo.FindByDeletedAtNullAsync();
```

---

## Boolean Values

Match boolean true/false:

| Keyword | SQL Value | Example Method | Parameters |
|---------|-----------|----------------|------------|
| `True` | `= TRUE` | `FindByIsActiveTrueAsync()` | None |
| `False` | `= FALSE` | `FindByIsDeletedFalseAsync()` | None |

**Synonyms:** `IsTrue`, `IsFalse`

**Examples:**
```csharp
// Find active products
var active = await repo.FindByIsActiveTrueAsync();

// Find inactive products
var inactive = await repo.FindByIsActiveFalseAsync();

// Synonym
var same = await repo.FindByIsActiveIsTrueAsync();
```

---

## Date/Time Operators

Compare dates and timestamps:

| Keyword | SQL Operator | Example Method | Parameters |
|---------|--------------|----------------|------------|
| `Before` | `<` | `FindByCreatedAtBeforeAsync(DateTime date)` | 1 DateTime |
| `After` | `>` | `FindByCreatedAtAfterAsync(DateTime date)` | 1 DateTime |

**Synonyms:** `IsBefore`, `IsAfter`

**Examples:**
```csharp
// Find products created in last 7 days
var recent = await repo.FindByCreatedAtAfterAsync(DateTime.Now.AddDays(-7));

// Find old products
var old = await repo.FindByCreatedAtBeforeAsync(DateTime.Now.AddMonths(-6));

// Synonym
var same = await repo.FindByCreatedAtIsAfterAsync(DateTime.Now.AddDays(-7));
```

---

## Logical Operators

Combine multiple conditions:

| Keyword | SQL Operator | Example Method |
|---------|--------------|----------------|
| `And` | `AND` | `FindByCategoryAndIsActiveTrueAsync(string category)` |
| `Or` | `OR` | `FindByNameOrDescriptionAsync(string name, string desc)` |

**Examples:**
```csharp
// Category AND active
var electronics = await repo.FindByCategoryAndIsActiveTrueAsync("Electronics");

// Complex: Category AND price range
var affordable = await repo.FindByCategoryAndPriceBetweenAsync("Furniture", 100m, 500m);

// OR condition
var matches = await repo.FindByNameContainingOrCategoryAsync("laptop", "Electronics");
```

---

## Modifiers

### Ordering

Sort query results:

| Pattern | SQL | Example |
|---------|-----|---------|
| `OrderBy{Property}Asc` | `ORDER BY property ASC` | `FindAllOrderByNameAscAsync()` |
| `OrderBy{Property}Desc` | `ORDER BY property DESC` | `FindByCategoryOrderByPriceDescAsync(string category)` |
| `OrderBy...Then...` | Multiple ORDER BY | `FindAllOrderByNameAscThenPriceDescAsync()` |

**Examples:**
```csharp
// Sort by price ascending
var cheap = await repo.FindByCategoryOrderByPriceAscAsync("Electronics");

// Multiple sort: name A-Z, then price high-to-low
var sorted = await repo.FindAllOrderByNameAscThenPriceDescAsync();
```

### Distinct

Remove duplicate rows:

| Pattern | SQL | Example |
|---------|-----|---------|
| `Distinct` | `SELECT DISTINCT` | `FindDistinctByCategoryAsync(string category)` |
| `Distinct` (Count) | `COUNT(DISTINCT *)` | `CountDistinctByCategoryAsync(string category)` |

**Examples:**
```csharp
// Get unique products
var unique = await repo.FindDistinctByCategoryAsync("Electronics");

// Count unique
var count = await repo.CountDistinctByIsActiveTrueAsync();
```

### Case Insensitivity

Case-insensitive comparison:

| Keyword | Effect | Example |
|---------|--------|---------|
| `IgnoreCase` | Compare using LOWER() | `FindByNameIgnoreCaseAsync(string name)` |
| `IgnoringCase` | Same as IgnoreCase | `FindByNameIgnoringCaseAsync(string name)` |
| `AllIgnoreCase` | All properties case-insensitive | `FindByNameAllIgnoreCaseAsync(string name)` |
| `AllIgnoringCase` | Same as AllIgnoreCase | `FindByNameAllIgnoringCaseAsync(string name)` |

**Examples:**
```csharp
// Case-insensitive search
var products = await repo.FindByNameIgnoreCaseAsync("LAPTOP"); // finds "laptop", "Laptop", "LAPTOP"
```

---

## Result Limiting 🆕

Limit the number of results (NEW in this version):

| Pattern | SQL | Example | Returns |
|---------|-----|---------|---------|
| `First<n>` | `FETCH FIRST n ROWS ONLY` | `FindFirst5ByAsync()` | First 5 rows |
| `Top<n>` | `FETCH FIRST n ROWS ONLY` | `FindTop10ByAsync()` | First 10 rows |
| `First` | `FETCH FIRST 1 ROWS ONLY` | `FindFirstByAsync()` | First 1 row |
| `Top` | `FETCH FIRST 1 ROWS ONLY` | `FindTopByAsync()` | First 1 row |

**Examples:**
```csharp
// Get top 5 cheapest products
var cheapest = await repo.FindFirst5ByCategoryOrderByPriceAscAsync("Electronics");

// Get 10 newest products
var newest = await repo.FindTop10ByOrderByCreatedAtDescAsync();

// Get first match (returns 1 row)
var first = await repo.FindFirstByCategoryAsync("Furniture");

// Top 20 with price filter
var expensive = await repo.FindTop20ByPriceGreaterThanOrderByPriceDescAsync(100m);
```

**💡 Best Practices:**
- Always use `OrderBy` with `First`/`Top` for predictable results
- Without ordering, database may return any matching rows
- Uses ANSI SQL `FETCH FIRST n ROWS ONLY` (compatible with PostgreSQL, SQL Server 2012+, MySQL 8+)

---

## Pattern Matching (Regex) 🆕

Match using regular expressions (NEW in this version):

| Keyword | SQL Operator | Example | Pattern Type |
|---------|--------------|---------|--------------|
| `Regex` | `REGEXP` | `FindByEmailRegexAsync(string pattern)` | Regex |
| `Matches` | `REGEXP` | `FindByNameMatchesAsync(string pattern)` | Regex |
| `MatchesRegex` | `REGEXP` | `FindByCodeMatchesRegexAsync(string pattern)` | Regex |
| `IsMatches` | `REGEXP` | `FindByPhoneIsMatchesAsync(string pattern)` | Regex |

**Examples:**
```csharp
// Find .edu email addresses
var eduEmails = await repo.FindByEmailRegexAsync(@".*\.edu$");

// Find phone numbers in (123) 456-7890 format
var usPhones = await repo.FindByPhoneMatchesAsync(@"^\(\d{3}\) \d{3}-\d{4}$");

// Find SKUs matching ABC-1234 pattern
var skus = await repo.FindBySkuMatchesRegexAsync(@"^[A-Z]{3}-\d{4}$");

// Complex: product codes starting with PROD-
var products = await repo.FindByCodeRegexAsync(@"^PROD-[0-9]{6}$");

// Combine with other conditions
var filtered = await repo.FindByEmailRegexAndIsActiveTrueAsync(@".*@company\.com$");
```

**Real-World Use Cases:**
- **Email Validation**: `@"^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$"`
- **Phone Numbers**: `@"^\d{3}-\d{3}-\d{4}$"` (US format)
- **Postal Codes**: `@"^[A-Z]\d[A-Z] \d[A-Z]\d$"` (Canadian)
- **SKU Formats**: `@"^[A-Z]{2,4}-\d{4,6}$"`
- **Product Codes**: `@"^PROD-[0-9]{6}$"`

**⚠️ Database Support:**
- [Completed] **MySQL/MariaDB**: Native `REGEXP` operator
- [Completed] **PostgreSQL**: Uses `~` operator (compatible)
- ❌ **SQL Server**: Requires CLR functions (not natively supported)
- [Completed] **SQLite**: Requires REGEXP extension

---

## Keyword Synonyms

NPA supports multiple synonyms for better readability (based on Spring Data JPA conventions):

### Comparison Synonyms
```csharp
// All equivalent
FindByPriceGreaterThanAsync(100m)
FindByPriceIsGreaterThanAsync(100m)

// All equivalent
FindByPriceBetweenAsync(10m, 50m)
FindByPriceIsBetweenAsync(10m, 50m)
```

### String Operator Synonyms
```csharp
// All equivalent - "containing"
FindByNameContainingAsync("laptop")
FindByNameContainsAsync("laptop")
FindByNameIsContainingAsync("laptop")

// All equivalent - "starts with"
FindByNameStartingWithAsync("Pro")
FindByNameStartsWithAsync("Pro")
FindByNameIsStartingWithAsync("Pro")

// All equivalent - "ends with"
FindByNameEndingWithAsync("Pro")
FindByNameEndsWithAsync("Pro")
FindByNameIsEndingWithAsync("Pro")
```

### Equality/Inequality Synonyms
```csharp
// All equivalent - equality
FindByNameAsync("Laptop")
FindByNameIsAsync("Laptop")
FindByNameEqualsAsync("Laptop")

// All equivalent - inequality
FindByCategoryNotAsync("Electronics")
FindByCategoryIsNotAsync("Electronics")
```

### Null Check Synonyms
```csharp
// All equivalent - is null
FindByDeletedAtIsNullAsync()
FindByDeletedAtNullAsync()

// All equivalent - is not null
FindByDeletedAtIsNotNullAsync()
FindByDeletedAtNotNullAsync()
```

### Boolean Synonyms
```csharp
// All equivalent - true
FindByIsActiveTrueAsync()
FindByIsActiveIsTrueAsync()

// All equivalent - false
FindByIsActiveFalseAsync()
FindByIsActiveIsFalseAsync()
```

### Collection Synonyms
```csharp
// All equivalent - in
FindByIdInAsync(ids)
FindByIdIsInAsync(ids)

// All equivalent - not in
FindByStatusNotInAsync(statuses)
FindByStatusIsNotInAsync(statuses)
```

### Date/Time Synonyms
```csharp
// All equivalent - after
FindByCreatedAtAfterAsync(date)
FindByCreatedAtIsAfterAsync(date)

// All equivalent - before
FindByCreatedAtBeforeAsync(date)
FindByCreatedAtIsBeforeAsync(date)
```

### Case Modifier Synonyms
```csharp
// All equivalent
FindByNameIgnoreCaseAsync("laptop")
FindByNameIgnoringCaseAsync("laptop")

// All equivalent
FindByNameAllIgnoreCaseAsync("laptop")
FindByNameAllIgnoringCaseAsync("laptop")
```

---

## Advanced Examples

### Custom Query Attributes
```csharp
// Simple custom query
[Query("SELECT * FROM Products WHERE Price > @price")]
var expensive = await repo.GetExpensiveProductsAsync(100m);

// Complex join query
[Query(@"
    SELECT p.* 
    FROM Products p 
    INNER JOIN OrderItems oi ON p.Id = oi.ProductId 
    GROUP BY p.Id 
    HAVING COUNT(*) > @minOrders
")]
var popular = await repo.GetPopularProductsAsync(5);

// Stored procedure call
[StoredProcedure("sp_GetTopSellingProducts")]
var topSelling = await repo.GetTopSellingProductsAsync(10);
```

### Combining First/Top with Other Keywords
```csharp
// Top 10 products containing "Pro" that are active, sorted by price
var products = await repo.FindFirst10ByNameContainsAndIsActiveTrueOrderByPriceAscAsync("Pro");

// Top 5 products in price range, newest first
var recent = await repo.GetTop5ByPriceBetweenOrderByCreatedAtDescAsync(50m, 200m);

// First product in category
var first = await repo.FindFirstByCategoryAsync("Electronics");
```

### Combining Regex with Other Keywords
```csharp
// Products matching pattern AND above price threshold
var premium = await repo.FindByNameRegexAndPriceGreaterThanAsync(@"^Premium.*", 500m);

// Top 20 products matching code pattern
var codes = await repo.FindFirst20BySkuMatchesOrderByNameAscAsync(@"^PROD-\d{6}$");

// Regex with active filter
var active = await repo.FindByEmailRegexAndIsActiveTrueAsync(@".*@company\.com$");
```

### Complex Multi-Condition Queries
```csharp
// Category AND price range AND active, sorted
var filtered = await repo.FindByCategoryAndPriceBetweenAndIsActiveTrueOrderByPriceAscAsync(
    "Electronics", 100m, 500m);

// Name contains OR category equals, with ordering
var matches = await repo.FindByNameContainingOrCategoryOrderByNameAscAsync("laptop", "Electronics");

// Top 10 distinct products in category
var unique = await repo.FindDistinctTop10ByCategoryOrderByPriceAscAsync("Furniture");
```

### Distinct with Combinations
```csharp
// Distinct products matching condition
var unique = await repo.FindDistinctByPriceGreaterThanAsync(100m);

// Count distinct with filters
var count = await repo.CountDistinctByPriceGreaterThanAndIsActiveTrueAsync(50m);

// Distinct with ordering
var sorted = await repo.FindDistinctByIsActiveTrueOrderByNameAscAsync();
```

---

## Quick Reference Chart

| Category | Keywords | Count |
|----------|----------|-------|
| **Subject Keywords** | Find, Get, Query, Search, Read, Stream, Count, Exists, Delete, Remove | 10 |
| **Custom Queries** 🆕 | [Query], [StoredProcedure] | 2 attributes |
| **Comparison** | GreaterThan, LessThan, Between, etc. | 5 base + synonyms |
| **String** | Like, Containing, StartingWith, EndingWith, etc. | 6 base + synonyms |
| **Collection** | In, NotIn | 2 base + synonyms |
| **Null** | IsNull, IsNotNull, Null, NotNull | 4 |
| **Boolean** | True, False | 2 base + synonyms |
| **Date/Time** | Before, After | 2 base + synonyms |
| **Logical** | And, Or | 2 |
| **Modifiers** | OrderBy, Distinct, IgnoreCase | 3 categories |
| **Result Limiting** 🆕 | First, Top, First<n>, Top<n> | 4 patterns |
| **Pattern Matching** 🆕 | Regex, Matches, MatchesRegex, IsMatches | 4 |
| **Total Synonyms** | Is-prefix, shorthands, case variants | 30+ |

**Total**: **50+ unique keyword variants + 2 custom query attributes** supported!

---

## Tips & Best Practices

1. **Use OrderBy with First/Top**: Always specify ordering for predictable results
   ```csharp
   ✅ FindFirst10ByOrderByCreatedAtDescAsync()
   ❌ FindFirst10ByAsync() // Random 10 rows!
   ```

2. **Custom Queries for Complex Logic**: Use `[Query]` or `[StoredProcedure]` when conventions aren't enough
   ```csharp
   ✅ [Query("SELECT * FROM Products WHERE Price > @price * 1.2")]
   ❌ Trying to express complex calculations in method names
   ```

3. **Property Names Must Match**: Use exact PascalCase property names
   ```csharp
   ✅ FindByStockQuantityAsync(int qty)    // Property: StockQuantity
   ❌ FindByStock_QuantityAsync(int qty)   // Won't work!
   ```

4. **Regex Patterns**: Escape special characters in patterns
   ```csharp
   ✅ FindByEmailRegexAsync(@".*\.com$")   // Escaped dot
   ❌ FindByEmailRegexAsync(@".*..com$")   // Wrong!
   ```

5. **Use Synonyms for Readability**: Choose what reads best
   ```csharp
   FindByNameContainsAsync("laptop")      // Concise
   FindByNameIsContainingAsync("laptop")  // More explicit
   ```

6. **Combine Features**: Mix keywords for powerful queries
   ```csharp
   FindFirst10ByNameRegexAndPriceGreaterThanOrderByPriceAscAsync(pattern, 100m)
   ```

---

## See Also

- **Working Examples**: `NpaConventionsSample.cs`
- **Test Coverage**: `NpaSynonymTests.cs`, `ResultLimitingTests.cs`, `RegexPatternMatchingTests.cs`, `CustomQueryAttributesTests.cs`
- **Documentation**: `SPRING_DATA_NPA_SUPPORT.md`
- **Spring Data JPA Reference** (inspiration): https://docs.spring.io/spring-data/jpa/reference/jpa/query-methods.html
