# Phase 7.6: Relationship Query Methods

## Overview
Generate specialized query methods for navigating and querying entities based on their relationships. Provide intuitive methods to find entities through their related entities.

## Status Summary

**Current Status**: ✅ **MOSTLY IMPLEMENTED** (Core Features Complete)

**Latest Updates**: 
- ✅ Fixed ORDER BY clause bug - now correctly uses column names from `[Column]` attributes instead of property names
- ✅ Fixed fully qualified type name bugs - parameter names and FK column names now use simple type names
- ✅ Fixed multi-level navigation bug - now extracts relationships from intermediate entity instead of current entity
- ✅ **Fixed SQL injection vulnerability** - `GetColumnNameForProperty` now validates property names and returns safe default instead of unsanitized input
- ✅ Implemented GROUP BY aggregations (COUNT, SUM, AVG, MIN, MAX grouped by parent entity)
- ✅ Implemented Advanced Filters (date ranges, amount filters, subquery-based filters)
- ✅ Implemented Pagination Support (skip/take parameters for all collection queries)
- ✅ Implemented Configurable Sorting (orderBy and ascending parameters for all pagination overloads)
- ✅ Implemented Inverse Relationship Queries (FindWith/Without/WithCount methods for OneToMany relationships)
- ✅ Implemented Complex Filters (OR/AND combinations for relationship queries)
- ✅ Implemented Multi-Entity GROUP BY Queries (with JOINs to include parent entity properties in results)
- ⚠️ Partially Implemented Multi-Level Navigation (2-level navigation with relationship extraction from intermediate entities)

### ✅ What's Implemented
- **ManyToOne Relationships**: 
  - `FindBy{Property}IdAsync()` - Find entities by related entity ID
  - `CountBy{Property}IdAsync()` - Count entities by related entity ID
  - `FindBy{Property}{PropertyName}Async()` - Find entities by related entity properties (e.g., `FindByCustomerNameAsync`)
- **OneToMany Relationships**: 
  - `Has{Property}Async()` - Check if entity has related children
  - `Count{Property}Async()` - Count related children
  - `GetTotal{Property}{PropertyName}Async()` - SUM aggregate for numeric properties
  - `GetAverage{Property}{PropertyName}Async()` - AVG aggregate for numeric properties
  - `GetMin{Property}{PropertyName}Async()` - MIN aggregate for numeric properties
  - `GetMax{Property}{PropertyName}Async()` - MAX aggregate for numeric properties
  - `Get{Property}CountsBy{ParentEntity}Async()` - COUNT grouped by parent entity (returns `Dictionary<KeyType, int>`)
  - `GetTotal{Property}{PropertyName}By{ParentEntity}Async()` - SUM grouped by parent entity (returns `Dictionary<KeyType, AggregateType>`)
  - `GetAverage{Property}{PropertyName}By{ParentEntity}Async()` - AVG grouped by parent entity
  - `GetMin{Property}{PropertyName}By{ParentEntity}Async()` - MIN grouped by parent entity
  - `GetMax{Property}{PropertyName}By{ParentEntity}Async()` - MAX grouped by parent entity
  - `Get{ParentEntity}{Property}SummaryAsync()` - Multi-entity GROUP BY with JOINs (returns tuple with parent properties and aggregates)
- **Advanced Filters**:
  - `FindBy{Property}And{PropertyName}RangeAsync()` - Date range filters on relationships (e.g., `FindByCustomerAndOrderDateRangeAsync`)
  - `Find{Property}{PropertyName}AboveAsync()` - Amount/quantity filters (e.g., `FindCustomerTotalAmountAboveAsync`)
  - `FindWithMinimum{Property}Async()` - Subquery-based filters (e.g., `FindWithMinimumOrdersAsync`)
- **Pagination Support**:
  - All collection query methods have pagination overloads with `skip` and `take` parameters
  - Uses SQL `OFFSET ... ROWS FETCH NEXT ... ROWS ONLY` syntax (SQL Server, PostgreSQL, SQLite compatible)
  - Backward compatible - original methods without pagination still available
- **Configurable Sorting**:
  - All pagination overloads support optional `orderBy` and `ascending` parameters
  - Property-to-column mapping dictionary generated at compile time for runtime column name resolution
  - Defaults to primary key column if `orderBy` is null or empty
  - Supports ascending/descending sort direction
  - Respects `[Column]` attributes for custom column names
  - **Security**: `GetColumnNameForProperty` validates property names against the mapping dictionary to prevent SQL injection
  - Invalid property names fall back to the default column name (primary key) instead of being used directly in SQL
- **Interface Generation**: Separate partial interfaces for relationship query methods
- **Efficient Queries**: Direct WHERE clauses and JOIN queries with no N+1 problems
- **Type Safety**: Uses correct key types from related entities

### 📋 What's Planned
- ⚠️ Multi-level navigation (Partially Implemented - 2-level navigation working, requires relationship extraction from intermediate entities)
- Complex relationship filters (OR/AND combinations)

## Objectives
- ✅ Generate relationship navigation query methods (ID-based and property-based)
- ✅ Support filtering by related entity properties (✅ Implemented)
- ✅ Implement exists/count methods for relationships
- ✅ Create optimized relationship queries (direct WHERE clauses and JOIN queries)

## Implementation Status

**Status**: ✅ **PARTIALLY IMPLEMENTED** (Basic Methods Complete)

### ✅ Completed Features

#### 1. Navigation Query Methods
- ✅ Generate `FindBy{PropertyName}IdAsync` methods for ManyToOne relationships
- ✅ Support finding entities by parent foreign key
- ✅ Generate `FindBy{PropertyName}{PropertyName}Async` methods for property-based queries
- ✅ Support filtering by related entity properties using JOIN queries
- ✅ Automatically generates methods for all simple properties of related entities

#### 2. Collection Query Methods
- ✅ Generate `Has{PropertyName}Async` methods for OneToMany relationships
- ✅ Generate `Count{PropertyName}Async` methods for OneToMany relationships
- ✅ Support existence and count checks

#### 3. Relationship Existence Methods
- ✅ Generate `Has{PropertyName}Async` methods
- ✅ Efficient existence queries using COUNT
- ✅ Optimized queries with no N+1 problems

#### 4. Count and Aggregate Methods
- ✅ Generate `CountBy{PropertyName}IdAsync` for ManyToOne relationships
- ✅ Generate `Count{PropertyName}Async` for OneToMany relationships
- ✅ Generate `GetTotal{Property}{PropertyName}Async` for SUM aggregations
- ✅ Generate `GetAverage{Property}{PropertyName}Async` for AVG aggregations
- ✅ Generate `GetMin{Property}{PropertyName}Async` for MIN aggregations
- ✅ Generate `GetMax{Property}{PropertyName}Async` for MAX aggregations
- ✅ Automatically generates aggregate methods for all numeric properties
- ✅ Generate `Get{Property}CountsBy{ParentEntity}Async` for COUNT GROUP BY aggregations
- ✅ Generate `GetTotal{Property}{PropertyName}By{ParentEntity}Async` for SUM GROUP BY aggregations
- ✅ Generate `GetAverage{Property}{PropertyName}By{ParentEntity}Async` for AVG GROUP BY aggregations
- ✅ Generate `GetMin{Property}{PropertyName}By{ParentEntity}Async` for MIN GROUP BY aggregations
- ✅ Generate `GetMax{Property}{PropertyName}By{ParentEntity}Async` for MAX GROUP BY aggregations

#### 5. Advanced Filters
- ✅ Generate date range filters for DateTime properties (`FindByCustomerAndOrderDateRangeAsync`)
- ✅ Generate amount/quantity filters for numeric properties (`FindCustomerTotalAmountAboveAsync`)
- ✅ Generate subquery-based filters for OneToMany relationships (`FindWithMinimumOrdersAsync`)
- ✅ Automatically generates filters for all DateTime and numeric properties
- ✅ Supports filtering by relationship ID combined with date/amount conditions

### 📋 Planned Features

#### 1. Navigation Query Methods (Advanced)
- ✅ Generate methods to find entities by related entity properties (e.g., `FindByCustomerNameAsync`)
- [ ] Support navigation through multiple levels
- [ ] Implement pagination for relationship queries

#### 2. Collection Query Methods (Advanced)
- [ ] Create `FindAllByRelatedEntityAsync` methods
- [ ] Support filtering collections by parent properties
- [ ] Implement sorting for relationship collections

#### 3. Relationship Existence Methods (Advanced)
- [ ] Create `ExistsWithRelationshipAsync` methods
- [ ] Support checking multiple relationships
- [ ] Enhanced existence queries with additional filters

#### 4. Count and Aggregate Methods (Advanced)
- ✅ Support aggregation functions on relationships (SUM, AVG, MIN, MAX)
- ✅ Generate methods like `GetTotalOrderAmountAsync`, `GetAverageOrderAmountAsync`, etc.
- [ ] Create `GetRelationshipStatisticsAsync` methods with GROUP BY
- [ ] Implement grouped counts and statistics across multiple entities

#### 5. Advanced Relationship Queries
- [ ] Generate methods for complex relationship filters
- [ ] Support OR/AND combinations in relationship queries
- [ ] Implement subquery generation for relationships
- [ ] Create methods for relationship graph queries
- [ ] Support date range queries on relationships
- [ ] Support amount/quantity-based filters

## Example Usage

### Current Implementation (✅ Implemented)

```csharp
[Entity]
[Table("customers")]
public class Customer
{
    [Id]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Country { get; set; }
    
    [OneToMany(MappedBy = "Customer")]
    public ICollection<Order> Orders { get; set; }
}

[Entity]
[Table("orders")]
public class Order
{
    [Id]
    public int Id { get; set; }
    public string OrderNumber { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    
    [ManyToOne]
    [JoinColumn("customer_id")]
    public Customer Customer { get; set; }
    public int CustomerId { get; set; }
    
    [OneToMany(MappedBy = "Order")]
    public ICollection<OrderItem> Items { get; set; }
}

// ✅ Currently Generated Methods:
public interface IOrderRepository : IRepository<Order, int>, IOrderRepositoryPartial
{
    // ✅ Navigation methods (ManyToOne)
    Task<IEnumerable<Order>> FindByCustomerIdAsync(int customerId);
    Task<IEnumerable<Order>> FindByCustomerIdAsync(int customerId, int skip, int take); // Pagination
    Task<int> CountByCustomerIdAsync(int customerId);
    
    // ✅ Property-based queries (ManyToOne)
    Task<IEnumerable<Order>> FindByCustomerNameAsync(string name);
    Task<IEnumerable<Order>> FindByCustomerNameAsync(string name, int skip, int take); // Pagination
    Task<IEnumerable<Order>> FindByCustomerCountryAsync(string country);
    Task<IEnumerable<Order>> FindByCustomerCountryAsync(string country, int skip, int take); // Pagination
    
    // ✅ Relationship existence (OneToMany - via Customer repository)
    // Note: These are generated on the parent entity (Customer)
}

public interface ICustomerRepository : IRepository<Customer, int>, ICustomerRepositoryPartial
{
    // ✅ Relationship existence and count (OneToMany)
    Task<bool> HasOrdersAsync(int id);
    Task<int> CountOrdersAsync(int id);
    
    // ✅ Aggregate methods (OneToMany)
    Task<decimal> GetTotalOrdersTotalAmountAsync(int id);
    Task<decimal?> GetAverageOrdersTotalAmountAsync(int id);
    Task<decimal?> GetMinOrdersTotalAmountAsync(int id);
    Task<decimal?> GetMaxOrdersTotalAmountAsync(int id);
}
```

### Planned Methods (📋 Not Yet Implemented)

```csharp
// ✅ Implemented: Navigation by related entity properties
// Task<IEnumerable<Order>> FindByCustomerNameAsync(string customerName); // ✅ Now implemented
// Task<IEnumerable<Order>> FindByCustomerCountryAsync(string country); // ✅ Now implemented

// 📋 Planned: Advanced relationship queries
Task<IEnumerable<Order>> FindByCustomerAndDateRangeAsync(
    int customerId, 
    DateTime startDate, 
    DateTime endDate);

Task<IEnumerable<Order>> FindWithMinimumItemsAsync(int minItems);
Task<IEnumerable<Order>> FindCustomerOrdersAboveAmountAsync(
    int customerId, 
    decimal minAmount);

// 📋 Planned: Inverse relationship queries
Task<IEnumerable<Customer>> FindWithOrdersAsync();
Task<IEnumerable<Customer>> FindWithoutOrdersAsync();
Task<IEnumerable<Customer>> FindWithOrderCountAsync(int minOrderCount);

// ✅ Implemented: Basic aggregate methods (on OneToMany relationships)
// Task<decimal> GetTotalOrdersTotalAmountAsync(int id); // ✅ Now implemented
// Task<decimal?> GetAverageOrdersTotalAmountAsync(int id); // ✅ Now implemented

// ✅ Implemented: GROUP BY aggregations
// Task<Dictionary<int, int>> GetOrdersCountsByCustomerAsync(); // ✅ Now implemented
// Task<Dictionary<int, decimal>> GetTotalOrdersTotalAmountByCustomerAsync(); // ✅ Now implemented
```

## Generated Code Examples

### ✅ Currently Implemented Methods

#### Find By Related Entity (ManyToOne)
```csharp
// Generated for Order entity with ManyToOne Customer relationship
// Note: ORDER BY uses the actual column name from [Column] attribute, not property name
public async Task<IEnumerable<Order>> FindByCustomerIdAsync(int customerId)
{
    var sql = "SELECT * FROM orders WHERE customer_id = @customerId ORDER BY order_id";
    return await _connection.QueryAsync<Order>(sql, new { customerId });
}

public async Task<int> CountByCustomerIdAsync(int customerId)
{
    var sql = "SELECT COUNT(*) FROM orders WHERE customer_id = @customerId";
    return await _connection.ExecuteScalarAsync<int>(sql, new { customerId });
}
```

#### Relationship Existence Check (OneToMany)
```csharp
// Generated for Customer entity with OneToMany Orders relationship
public async Task<bool> HasOrdersAsync(int id)
{
    var sql = "SELECT COUNT(*) FROM Order WHERE CustomerId = @id";
    var count = await _connection.ExecuteScalarAsync<int>(sql, new { id });
    return count > 0;
}

public async Task<int> CountOrdersAsync(int id)
{
    var sql = "SELECT COUNT(*) FROM Order WHERE CustomerId = @id";
    return await _connection.ExecuteScalarAsync<int>(sql, new { id });
}
```

### ✅ Implemented Code Examples

#### Find By Related Entity Properties
```csharp
// ✅ Implemented: Find by related entity property (uses JOIN)
// Note: ORDER BY and JOIN conditions use actual column names from [Column] attributes
public async Task<IEnumerable<Order>> FindByCustomerNameAsync(string name)
{
    var sql = @"SELECT e.* FROM orders e
                INNER JOIN customers r ON e.customer_id = r.customer_id
                WHERE r.name = @name
                ORDER BY e.order_id";
    return await _connection.QueryAsync<Order>(sql, new { name });
}
```

#### Advanced Relationship Queries (✅ Implemented)
```csharp
// ✅ Implemented: Date range filters with relationships
public async Task<IEnumerable<Order>> FindByCustomerAndOrderDateRangeAsync(
    int customerId, 
    DateTime startOrderDate, 
    DateTime endOrderDate)
{
    var sql = @"SELECT e.* FROM orders e
                INNER JOIN customers r ON e.customer_id = r.Id
                WHERE e.customer_id = @customerId
                    AND e.order_date >= @startOrderDate
                    AND e.order_date <= @endOrderDate
                ORDER BY e.Id";
    
    return await _connection.QueryAsync<Order>(
        sql, 
        new { customerId, startOrderDate, endOrderDate });
}

// ✅ Implemented: Amount filters with relationships
public async Task<IEnumerable<Order>> FindCustomerTotalAmountAboveAsync(
    int customerId, 
    decimal minTotalAmount)
{
    var sql = @"SELECT e.* FROM orders e
                INNER JOIN customers r ON e.customer_id = r.Id
                WHERE e.customer_id = @customerId
                    AND e.total_amount >= @minTotalAmount
                ORDER BY e.Id";
    
    return await _connection.QueryAsync<Order>(
        sql, 
        new { customerId, minTotalAmount });
}

// ✅ Implemented: Subquery-based filters
public async Task<IEnumerable<Customer>> FindWithMinimumOrdersAsync(int minCount)
{
    var sql = @"SELECT e.* FROM customers e
                WHERE (
                    SELECT COUNT(*)
                    FROM orders c
                    WHERE c.customer_id = e.Id
                ) >= @minCount
                ORDER BY e.Id";
    
    return await _connection.QueryAsync<Customer>(sql, new { minCount });
}

// ✅ Implemented: Pagination support
public async Task<IEnumerable<Order>> FindByCustomerIdAsync(int customerId, int skip, int take)
{
    var sql = $"SELECT * FROM orders WHERE customer_id = @customerId ORDER BY order_id OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";
    return await _connection.QueryAsync<Order>(sql, new { customerId, skip, take });
}

public async Task<IEnumerable<Order>> FindByCustomerNameAsync(string name, int skip, int take)
{
    var sql = @"SELECT e.* FROM orders e
                INNER JOIN customers r ON e.customer_id = r.customer_id
                WHERE r.name = @name
                ORDER BY e.order_id OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";
    return await _connection.QueryAsync<Order>(sql, new { name, skip, take });
}
```

#### Aggregate Methods
```csharp
// ✅ Implemented: Aggregate functions on relationships
public async Task<decimal> GetTotalOrdersTotalAmountAsync(int id)
{
    var sql = $"SELECT COALESCE(SUM(total_amount), 0) FROM orders WHERE CustomerId = @id";
    return await _connection.ExecuteScalarAsync<decimal>(sql, new { id });
}

public async Task<decimal?> GetAverageOrdersTotalAmountAsync(int id)
{
    var sql = $"SELECT AVG(total_amount) FROM orders WHERE CustomerId = @id";
    return await _connection.ExecuteScalarAsync<decimal?>(sql, new { id });
}

public async Task<decimal?> GetMinOrdersTotalAmountAsync(int id)
{
    var sql = $"SELECT MIN(total_amount) FROM orders WHERE CustomerId = @id";
    return await _connection.ExecuteScalarAsync<decimal?>(sql, new { id });
}

public async Task<decimal?> GetMaxOrdersTotalAmountAsync(int id)
{
    var sql = $"SELECT MAX(total_amount) FROM orders WHERE CustomerId = @id";
    return await _connection.ExecuteScalarAsync<decimal?>(sql, new { id });
}
```

### ✅ Implemented Code Examples

#### Multi-Entity GROUP BY Aggregations
```csharp
// ✅ Implemented: Multi-entity GROUP BY with JOINs
// Returns tuple with parent entity properties and aggregates
public async Task<IEnumerable<(int CustomerId, string Name, string Email, int OrdersCount, decimal TotalTotalAmount, int TotalQuantity)>> 
    GetCustomerOrdersSummaryAsync()
{
    var sql = @"SELECT p.customer_id AS CustomerId, p.customer_name AS Name, p.customer_email AS Email, 
                COUNT(c.customer_id) AS OrdersCount, 
                COALESCE(SUM(c.total_amount), 0) AS TotalTotalAmount, 
                COALESCE(SUM(c.order_quantity), 0) AS TotalQuantity
                FROM customers p
                LEFT JOIN orders c ON p.customer_id = c.customer_id
                GROUP BY p.customer_id, p.customer_name, p.customer_email";
    
    return await _connection.QueryAsync<(int CustomerId, string Name, string Email, int OrdersCount, decimal TotalTotalAmount, int TotalQuantity)>(sql);
}
```

## Implementation Details

### Code Generation Location

The relationship query methods are generated in:
- **File**: `src/NPA.Generators/RepositoryGenerator.cs`
- **Method**: `GenerateRelationshipQueryMethods()` (lines 3502-3532)
- **Helper Methods**:
  - `GenerateFindByParentMethod()` - For ManyToOne relationships (ID-based queries)
  - `GenerateCountByParentMethod()` - For ManyToOne relationships (count queries)
  - `GenerateHasChildrenMethod()` - For OneToMany relationships (existence checks)
  - `GenerateCountChildrenMethod()` - For OneToMany relationships (count queries)
  - `GeneratePropertyBasedQueries()` - For ManyToOne relationships (property-based queries with JOINs)
  - `GenerateAggregateMethods()` - For OneToMany relationships (SUM, AVG, MIN, MAX)
  - `GetKeyColumnName()` - Retrieves actual column name for primary keys (respects `[Column]` attributes)
  - `GetForeignKeyColumnForOneToMany()` - Retrieves foreign key column from inverse ManyToOne relationship

### Interface Generation

The methods are declared in a separate partial interface:
- **Interface Name**: `{RepositoryName}Partial` (e.g., `IOrderRepositoryPartial`)
- **File Pattern**: `{EntityName}RepositoryExtensions.g.cs`
- **Implementation**: The generated repository class implements both the main interface and the partial interface

### Current Limitations

1. ✅ **Property-Based Queries**: Now supports finding by related entity properties (e.g., `FindByCustomerNameAsync`)
2. ✅ **JOIN Queries**: Generates methods that use JOINs to filter by related entity properties
3. ✅ **Aggregates**: Generates SUM, AVG, MIN, MAX methods for single entities
4. ✅ **GROUP BY Aggregations**: Generates COUNT, SUM, AVG, MIN, MAX methods grouped by parent entity (returns Dictionary)
5. ✅ **Advanced Filters**: Generates date range filters, amount/quantity filters, and subquery-based filters
6. ✅ **Pagination**: ✅ Implemented - skip/take parameters added to all collection queries
7. ✅ **Configurable Sorting**: ✅ Implemented - orderBy and ascending parameters for all pagination overloads
8. ⚠️ **Multi-Level Navigation**: ⚠️ Partially Implemented - 2-level navigation working (e.g., OrderItem → Order → Customer). Requires successful relationship extraction from intermediate entities. If extraction fails, methods are not generated to prevent incorrect SQL.
9. ✅ **Complex Filters**: ✅ Implemented - OR combinations (`FindBy{Property1}Or{Property2}Async`) and AND combinations (`FindBy{Property}And{PropertyName}Async`) with full pagination and sorting support

### Column Name Handling

✅ **Correctly Handles Custom Column Names**: The generator correctly uses column names from `[Column]` attributes in:
- **JOIN conditions**: Uses the related entity's primary key column name (not property name)
- **ORDER BY clauses**: Uses the current entity's primary key column name (not property name)
- **WHERE clauses**: Uses property column names for filtering

This ensures that when entities have custom column names via `[Column]` attributes, the generated SQL queries use the correct database column names, preventing runtime query failures.

### Security: SQL Injection Protection

✅ **SQL Injection Protection in Configurable Sorting**: The `GetColumnNameForProperty` method validates all property names before using them in SQL queries:
- **Validation**: Only property names that exist in the `_propertyColumnMap` dictionary are allowed
- **Fallback**: Invalid property names fall back to the safe default column name (primary key) instead of being used directly in SQL
- **Protection**: This prevents SQL injection attacks through the `orderBy` parameter in all pagination overloads that support custom sorting
- **Implementation**: The method returns `defaultColumnName` instead of the unsanitized `propertyName` when validation fails

### Multi-Level Navigation Details

⚠️ **Partially Implemented**: 2-level navigation queries are supported (e.g., `OrderItem → Order → Customer`).

**How It Works**:
1. The generator identifies ManyToOne relationships on the current entity (first level)
2. For each first-level relationship, it extracts relationships from the intermediate entity using `ExtractRelationships`
3. It finds the ManyToOne relationship from the intermediate entity to potential target entities
4. If found, it generates query methods using the correct FK column from the intermediate entity's relationship
5. If not found, the method is not generated (prevents incorrect SQL)

**Bug Fix Applied**:
- **Previous Issue**: The code incorrectly searched for the second-level relationship in the current entity's relationships, leading to incorrect FK column names
- **Fix**: Now correctly extracts relationships from the intermediate entity, ensuring proper FK column usage and respecting custom `[JoinColumn]` attributes
- **Impact**: Multi-level navigation queries now use the correct foreign key columns from the intermediate entity's relationship metadata

**Limitations**:
- Requires successful relationship extraction from intermediate entities
- ✅ **3+ level navigation now supported** - Recursive path finding supports navigation paths of any depth (up to 5 levels by default)
- Methods are only generated when relationships can be successfully extracted
- ✅ Supports ManyToOne, OneToOne (both owner and inverse sides), and ManyToMany relationships in navigation paths

## Acceptance Criteria

### ✅ Completed Criteria
- [x] Basic navigation methods generated for ManyToOne relationships
- [x] Basic existence and count methods generated for OneToMany relationships
- [x] Query methods follow consistent naming conventions (`FindBy{Property}IdAsync`, `CountBy{Property}IdAsync`, `Has{Property}Async`, `Count{Property}Async`)
- [x] Efficient SQL queries generated (direct WHERE clauses, no N+1 problems)
- [x] Support for ManyToOne and OneToMany relationship types
- [x] Existence checks are optimized (using COUNT with > 0 check)

### 📋 Remaining Criteria
- [ ] Navigation methods generated for all relationship types (OneToOne, ManyToMany)
- [x] Methods to find by related entity properties (✅ Implemented: `FindBy{Property}{PropertyName}Async`)
- [x] Pagination support where appropriate (✅ Implemented: skip/take overloads for all collection queries)
- [x] Aggregate functions work correctly (✅ SUM, AVG, MIN, MAX implemented; ✅ GROUP BY implemented)
- [x] Complex filters properly implemented (✅ Date ranges, amounts, subqueries implemented)
- [x] Configurable sorting options (✅ Implemented: orderBy and ascending parameters for all pagination overloads)
- [x] Support for multi-level navigation (✅ Implemented: 2+ level navigation with recursive path finding, supports up to 5 levels)
- [ ] Support for OR/AND combinations in relationship queries

## Dependencies
- Phase 7.1: Relationship-Aware Repository Generation
- Phase 2.1: Relationship Mapping
- Phase 1.3: Simple Query Support

## Testing Requirements

### ✅ Completed Tests
- ✅ Unit tests for basic query generation (`RelationshipQueryGeneratorTests.cs`)
- ✅ Tests verify method signatures in generated interfaces
- ✅ Tests verify implementation class implements partial interface
- ✅ Tests verify property-based queries use correct JOIN conditions with column names
- ✅ Tests verify aggregate methods use correct foreign key column names
- ✅ Tests verify ORDER BY clauses use column names instead of property names
- ✅ Tests verify custom `[Column]` attributes are correctly handled in SQL generation
- ✅ Tests verify GROUP BY aggregate methods are generated correctly
- ✅ Tests verify GROUP BY methods use correct foreign key columns
- ✅ Tests verify GROUP BY methods return Dictionary types
- ✅ Tests verify complex filters (OR/AND combinations) are generated correctly
- ✅ Tests verify SQL injection protection in configurable sorting (`GetColumnNameForProperty_ShouldPreventSqlInjection`)

### 📋 Remaining Test Requirements
- [x] Integration tests for all generated methods (✅ Implemented: Integration tests using Testcontainers that compile and execute actual generated repository implementations)
- [x] Performance tests for complex queries (✅ Implemented: 6 performance tests covering large datasets, multi-level navigation, pagination, and inverse queries)
- [x] Tests for edge cases (empty collections, null relationships) (✅ Implemented: 6 comprehensive edge case tests)
- [x] Tests for aggregate functions accuracy (✅ Basic tests implemented)
- [x] Tests for pagination and sorting (✅ Implemented with comprehensive test coverage)
- [x] Tests for JOIN-based queries (✅ Property-based query tests implemented)
- [x] Tests for subquery-based filters (✅ Implemented: FindWithMinimumOrdersAsync and related tests)

## Performance Considerations

### ✅ Current Optimizations
- ✅ Direct WHERE clauses (no unnecessary JOINs for ID-based queries)
- ✅ Efficient COUNT queries for existence checks
- ✅ No N+1 query problems (single query per method call)
- ✅ Correct column name resolution (uses `[Column]` attributes for SQL generation)
- ✅ Efficient JOIN queries for property-based queries (single query with INNER JOIN)

### 📋 Planned Optimizations
- [ ] Use indexes on foreign keys (documentation/guidance)
- [ ] Optimize JOIN operations (when JOIN queries are implemented)
- [ ] Consider query result caching for frequently accessed relationships
- [ ] Monitor query execution plans
- [ ] Batch loading support for multiple relationship queries
- [ ] Query result pagination to limit memory usage

## Documentation

### ✅ Current Documentation
- ✅ XML documentation comments on all generated methods
- ✅ Interface documentation in generated partial interfaces
- ✅ This README with implementation status

### ✅ Documentation - COMPLETED
- [x] Complete API documentation for all planned methods ([API_REFERENCE.md](API_REFERENCE.md))
- [x] Query optimization guidelines ([OPTIMIZATION_GUIDE.md](OPTIMIZATION_GUIDE.md))
- [x] Relationship query patterns and best practices ([PATTERNS_AND_BEST_PRACTICES.md](PATTERNS_AND_BEST_PRACTICES.md))
- [x] Performance best practices guide ([PERFORMANCE_GUIDE.md](PERFORMANCE_GUIDE.md))
- [x] Examples for common scenarios ([EXAMPLES.md](EXAMPLES.md))
- [x] Troubleshooting guide ([TROUBLESHOOTING.md](TROUBLESHOOTING.md))

**Available Documentation:**
- [API Reference](API_REFERENCE.md) - Complete API documentation for all relationship query methods
- [Query Optimization Guide](OPTIMIZATION_GUIDE.md) - Strategies for optimizing relationship queries
- [Patterns and Best Practices](PATTERNS_AND_BEST_PRACTICES.md) - Common patterns and anti-patterns
- [Performance Guide](PERFORMANCE_GUIDE.md) - Performance best practices and monitoring
- [Examples](EXAMPLES.md) - Practical examples for common scenarios
- [Troubleshooting](TROUBLESHOOTING.md) - Common issues and solutions

## Next Steps

### Immediate Priorities
1. ✅ **Property-Based Queries**: ✅ Implemented `FindBy{RelatedEntity}{Property}Async` methods (e.g., `FindByCustomerNameAsync`)
2. ✅ **Aggregate Methods**: ✅ Implemented SUM, AVG, MIN, MAX methods for single entities
3. ✅ **GROUP BY Aggregations**: ✅ Implemented COUNT, SUM, AVG, MIN, MAX methods grouped by parent entity
4. ✅ **Advanced Filters**: ✅ Implemented date ranges, amount filters, and subquery-based filters
5. ✅ **Pagination Support**: ✅ Implemented skip/take overloads for all collection query methods
6. ✅ **Configurable Sorting**: ✅ Implemented orderBy and ascending parameters for all pagination overloads
7. ✅ **Inverse Relationship Queries**: ✅ Implemented FindWith/Without/WithCount methods for OneToMany relationships
8. **Bug Fixes**: ✅ Fixed ORDER BY clause to use column names instead of property names; ✅ Fixed fully qualified type name handling

### Future Enhancements
1. **Pagination Support**: Add skip/take parameters to collection queries
2. **Configurable Sorting**: Allow custom ORDER BY clauses
3. **Multi-Level Navigation**: Support queries across multiple relationship levels
4. **Complex Filters**: OR/AND combinations, multiple relationship filters
