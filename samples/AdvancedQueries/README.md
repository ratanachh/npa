# AdvancedQueries Sample - Phase 1.3 CPQL Demonstrations

## 📋 Overview

This sample demonstrates **advanced CPQL (Custom Persistence Query Language)** capabilities using the Phase 1.3 implementation. It showcases complex WHERE conditions, aggregations, pattern matching, and bulk updates.

> **Note**: This sample uses **CPQL** (Phase 1.3 ✅ completed). Advanced features like JOINs, subqueries, and CTEs require **enhanced CPQL** implementation (Phase 2.3 📋 planned).

## 🎯 What This Sample Demonstrates

### Phase 1.3 Features (All Working ✅)
1. **Complex WHERE Conditions** - Multiple AND/OR combinations
2. **Range Queries** - BETWEEN equivalent with >= and <=
3. **Pattern Matching** - LIKE queries for text search
4. **DateTime Queries** - Date range filtering
5. **NULL Handling** - IS NULL/IS NOT NULL
6. **Aggregation Queries** - COUNT operations
7. **Bulk Updates** - UPDATE with WHERE conditions
8. **Multiple Parameters** - Complex parameter binding
9. **String Functions** - Prefix/suffix matching
10. **Status Filtering** - Multiple OR conditions

### Limitations (Require Phase 2.3)
- ❌ JOINs between tables
- ❌ Subqueries
- ❌ SUM, AVG, MIN, MAX aggregations
- ❌ GROUP BY and HAVING
- ❌ CTEs (Common Table Expressions)
- ❌ Window functions

## 🏗️ Project Structure

```
AdvancedQueries/
├── AdvancedQueries.csproj
├── Program.cs
├── DatabaseManager.cs
├── AdvancedQueryExamples.cs
├── README.md
└── Entities/
    ├── Product.cs
    └── Order.cs
```

## 🚀 Running the Sample

### Prerequisites
- .NET 8.0 SDK
- Docker Desktop running
- PostgreSQL will run in a Testcontainer

### Commands
```bash
# From repository root
dotnet build samples/AdvancedQueries
dotnet run --project samples/AdvancedQueries

# Or from sample directory
cd samples/AdvancedQueries
dotnet run
```

## 📊 Expected Output

```
NPA Advanced CPQL Queries Sample
Using Phase 1.3 features (Entity Mapping + CRUD + CPQL)

Starting PostgreSQL container...
Database schema and indexes created successfully
Database ready!

================================================================================
NPA Advanced CPQL Query Examples (Phase 1.3)
================================================================================

Seeding test data...
✓ Seeded 10 products and 5 orders

1. Complex WHERE Conditions (AND/OR)
--------------------------------------------------------------------------------
Found 5 products:
  - Product[3] Wireless Mouse - Electronics ($29.99) Stock: 100
  - Product[10] Webcam HD - Electronics ($79.99) Stock: 45
  - Product[5] Office Chair Pro - Furniture ($299.99) Stock: 20
  - Product[6] Standing Desk - Furniture ($599.99) Stock: 10

2. Range Queries (BETWEEN equivalent)
--------------------------------------------------------------------------------
Products in $50-$500 range:
  - Mechanical Keyboard: $149.99
  - Office Chair Pro: $299.99
  - Standing Desk: $599.99
  - Monitor 27": $399.99
  - Desk Lamp LED: $49.99
  - Webcam HD: $79.99

3. Pattern Matching (LIKE equivalent)
--------------------------------------------------------------------------------
Products matching 'Laptop':
  - Laptop Pro 15
  - Laptop Air 13

4. DateTime Queries (Recent Orders)
--------------------------------------------------------------------------------
Orders from last 7 days:
  - ORD-2024-003: 2025-10-03
  - ORD-2024-004: 2025-10-05
  - ORD-2024-005: 2025-10-07

5. NULL Handling (Unshipped Orders)
--------------------------------------------------------------------------------
Unshipped orders:
  - ORD-2024-003: Processing
  - ORD-2024-004: Pending
  - ORD-2024-005: Pending

6. Aggregation Queries (COUNT, SUM, AVG)
--------------------------------------------------------------------------------
Active products: 9
Electronics count: 6
Note: SUM/AVG aggregations require Phase 2.3 (Enhanced CPQL)

7. Bulk UPDATE Operations
--------------------------------------------------------------------------------
✓ Updated 3 Furniture items (+5% price)

8. Multiple Parameters (Complex Filtering)
--------------------------------------------------------------------------------
Premium electronics in stock:
  - Laptop Pro 15: $1299.99 (Stock: 15)
  - Laptop Air 13: $999.99 (Stock: 25)
  - Mechanical Keyboard: $149.99 (Stock: 50)
  - Monitor 27": $399.99 (Stock: 30)
  - Webcam HD: $79.99 (Stock: 45)

9. String Functions (Prefix/Suffix Matching)
--------------------------------------------------------------------------------
Orders from 2024:
  - ORD-2024-001: $1329.98
  - ORD-2024-002: $599.99
  - ORD-2024-003: $179.97
  - ORD-2024-004: $1699.98
  - ORD-2024-005: $42.98

10. Status-Based Filtering (IN equivalent)
--------------------------------------------------------------------------------
Active orders (Pending/Processing):
  - ORD-2024-003: Processing - $179.97
  - ORD-2024-004: Pending - $1699.98
  - ORD-2024-005: Pending - $42.98

Completed orders: 1

================================================================================
All examples completed!
================================================================================

Press any key to exit...
```

## 💻 Code Highlights

### Complex WHERE Conditions
```csharp
var query = em.CreateQuery<Product>(
    "SELECT p FROM Product p WHERE (p.Category = @category1 AND p.Price < @maxPrice) OR (p.Category = @category2 AND p.Price > @minPrice)")
    .SetParameter("category1", "Electronics")
    .SetParameter("maxPrice", 100m)
    .SetParameter("category2", "Furniture")
    .SetParameter("minPrice", 200m);
```

### Bulk Updates
```csharp
var updateQuery = em.CreateQuery<Product>(
    "UPDATE Product p SET p.Price = p.Price * @multiplier WHERE p.Category = @category")
    .SetParameter("multiplier", 1.05m)
    .SetParameter("category", "Furniture");

var updatedCount = await updateQuery.ExecuteUpdateAsync();
```

### Aggregations
```csharp
var countQuery = em.CreateQuery<Product>(
    "SELECT COUNT(p) FROM Product p WHERE p.IsActive = @active")
    .SetParameter("active", true);
    
var activeCount = await countQuery.ExecuteScalarAsync<long>();
```

## 📚 Key Concepts Demonstrated

### 1. SQL Injection Prevention
All queries use parameterized binding - values never concatenated into SQL strings.

### 2. Type-Safe Results
CPQL queries return strongly-typed entities with full IntelliSense support.

### 3. Flexible Filtering
Complex boolean logic with AND/OR combinations for sophisticated queries.

### 4. Null Safety
Proper handling of nullable columns and NULL comparisons.

### 5. Dapper Performance
All queries execute through Dapper for optimal database performance.

## 🔗 Related Documentation

- [Phase 1.3 Task Document](../../docs/tasks/samples/phase1.3-cpql-query-sample.md)
- [CPQL Query Documentation](../../docs/tasks/phase1.3-simple-query-support/README.md)
- [Main README](../../README.md)

## 💡 Best Practices Shown

1. **Always parameterize** dynamic values
2. **Use scoped EntityManager** for proper lifecycle
3. **Handle exceptions** gracefully
4. **Dispose resources** with `await using`
5. **Test queries** with real data
6. **Index frequently** queried columns

## 🚀 Next Steps

After running this sample, explore:
- Phase 2.3 - Enhanced CPQL for JOINs and subqueries (planned)
- Phase 3.3 - Bulk operations for large datasets (planned)
- Phase 5.1 - Caching for query performance (planned)

---

*Created: October 8, 2025*  
*Status: ✅ Functional with Phase 1.3 features*  
*Database: PostgreSQL 17 (Testcontainers)*
