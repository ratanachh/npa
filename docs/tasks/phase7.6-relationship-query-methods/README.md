# Phase 7.6: Relationship Query Methods

## Overview
Generate specialized query methods for navigating and querying entities based on their relationships. Provide intuitive methods to find entities through their related entities.

## Status Summary

**Current Status**: ✅ **PARTIALLY IMPLEMENTED** (Basic Methods Complete)

### ✅ What's Implemented
- **ManyToOne Relationships**: `FindBy{Property}IdAsync()` and `CountBy{Property}IdAsync()` methods
- **OneToMany Relationships**: `Has{Property}Async()` and `Count{Property}Async()` methods
- **Interface Generation**: Separate partial interfaces for relationship query methods
- **Efficient Queries**: Direct WHERE clauses with no N+1 problems

### 📋 What's Planned
- Property-based queries (e.g., `FindByCustomerNameAsync`)
- Aggregate methods (SUM, AVG, MIN, MAX, GROUP BY)
- Advanced filters (date ranges, amounts, subqueries)
- Pagination and sorting support
- Multi-level navigation
- Complex relationship filters

## Objectives
- ✅ Generate relationship navigation query methods (basic - ID-based only)
- [ ] Support filtering by related entity properties (planned)
- ✅ Implement exists/count methods for relationships (basic)
- ✅ Create optimized relationship queries (basic - direct WHERE clauses)

## Implementation Status

**Status**: ✅ **PARTIALLY IMPLEMENTED** (Basic Methods Complete)

### ✅ Completed Features

#### 1. Navigation Query Methods (Basic)
- ✅ Generate `FindBy{PropertyName}IdAsync` methods for ManyToOne relationships
- ✅ Support finding entities by parent foreign key
- ⚠️ Limited to ID-based queries (not property-based yet)

#### 2. Collection Query Methods (Basic)
- ✅ Generate `Has{PropertyName}Async` methods for OneToMany relationships
- ✅ Generate `Count{PropertyName}Async` methods for OneToMany relationships
- ⚠️ Limited to existence and count checks

#### 3. Relationship Existence Methods (Basic)
- ✅ Generate `Has{PropertyName}Async` methods
- ✅ Efficient existence queries using COUNT
- ⚠️ Single relationship checks only (no multiple relationship support yet)

#### 4. Count Methods (Basic)
- ✅ Generate `CountBy{PropertyName}IdAsync` for ManyToOne relationships
- ✅ Generate `Count{PropertyName}Async` for OneToMany relationships
- ⚠️ Basic count only (no aggregates or statistics yet)

### 📋 Planned Features

#### 1. Navigation Query Methods (Advanced)
- [ ] Generate methods to find entities by related entity properties (e.g., `FindByCustomerNameAsync`)
- [ ] Support navigation through multiple levels
- [ ] Implement pagination for relationship queries
- [ ] Support filtering by related entity properties

#### 2. Collection Query Methods (Advanced)
- [ ] Create `FindAllByRelatedEntityAsync` methods
- [ ] Support filtering collections by parent properties
- [ ] Implement sorting for relationship collections

#### 3. Relationship Existence Methods (Advanced)
- [ ] Create `ExistsWithRelationshipAsync` methods
- [ ] Support checking multiple relationships
- [ ] Enhanced existence queries with additional filters

#### 4. Count and Aggregate Methods (Advanced)
- [ ] Create `GetRelationshipStatisticsAsync` methods
- [ ] Support aggregation functions on relationships (SUM, AVG, MIN, MAX)
- [ ] Implement grouped counts and statistics
- [ ] Generate methods like `GetTotalOrderAmountAsync`

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
    Task<int> CountByCustomerIdAsync(int customerId);
    
    // ✅ Relationship existence (OneToMany - via Customer repository)
    // Note: These are generated on the parent entity (Customer)
}

public interface ICustomerRepository : IRepository<Customer, int>, ICustomerRepositoryPartial
{
    // ✅ Relationship existence and count (OneToMany)
    Task<bool> HasOrdersAsync(int id);
    Task<int> CountOrdersAsync(int id);
}
```

### Planned Methods (📋 Not Yet Implemented)

```csharp
// 📋 Planned: Navigation by related entity properties
Task<IEnumerable<Order>> FindByCustomerNameAsync(string customerName);
Task<IEnumerable<Order>> FindByCustomerCountryAsync(string country);

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

// 📋 Planned: Aggregate methods
Task<decimal> GetTotalOrderAmountAsync(int customerId);
Task<Dictionary<int, decimal>> GetTotalOrderAmountsByCustomerAsync();
Task<Dictionary<int, int>> GetOrderCountsByCustomerAsync();
```

## Generated Code Examples

### ✅ Currently Implemented Methods

#### Find By Related Entity (ManyToOne)
```csharp
// Generated for Order entity with ManyToOne Customer relationship
public async Task<IEnumerable<Order>> FindByCustomerIdAsync(int customerId)
{
    var sql = "SELECT * FROM Order WHERE customer_id = @customerId ORDER BY Id";
    return await _connection.QueryAsync<Order>(sql, new { customerId });
}

public async Task<int> CountByCustomerIdAsync(int customerId)
{
    var sql = "SELECT COUNT(*) FROM Order WHERE customer_id = @customerId";
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

### 📋 Planned Code Examples (Not Yet Implemented)

#### Find By Related Entity Properties
```csharp
// 📋 Planned: Find by related entity property (requires JOIN)
public async Task<IEnumerable<Order>> FindByCustomerNameAsync(string customerName)
{
    const string sql = @"
        SELECT o.*
        FROM orders o
        INNER JOIN customers c ON c.id = o.customer_id
        WHERE c.name = @customerName
        ORDER BY o.order_date DESC";
    
    return await _connection.QueryAsync<Order>(sql, new { customerName });
}
```

#### Advanced Relationship Queries
```csharp
// 📋 Planned: Complex relationship filters
public async Task<IEnumerable<Order>> FindByCustomerAndDateRangeAsync(
    int customerId, 
    DateTime startDate, 
    DateTime endDate)
{
    const string sql = @"
        SELECT o.*
        FROM orders o
        WHERE o.customer_id = @customerId
            AND o.order_date >= @startDate
            AND o.order_date <= @endDate
        ORDER BY o.order_date DESC";
    
    return await _connection.QueryAsync<Order>(
        sql, 
        new { customerId, startDate, endDate });
}

// 📋 Planned: Subquery-based filters
public async Task<IEnumerable<Order>> FindWithMinimumItemsAsync(int minItems)
{
    const string sql = @"
        SELECT o.*
        FROM orders o
        WHERE (
            SELECT COUNT(*) 
            FROM order_items oi 
            WHERE oi.order_id = o.id
        ) >= @minItems
        ORDER BY o.order_date DESC";
    
    return await _connection.QueryAsync<Order>(sql, new { minItems });
}
```

#### Aggregate Methods
```csharp
// 📋 Planned: Aggregate functions on relationships
public async Task<decimal> GetTotalOrderAmountAsync(int customerId)
{
    const string sql = @"
        SELECT COALESCE(SUM(total_amount), 0)
        FROM orders
        WHERE customer_id = @customerId";
    
    return await _connection.ExecuteScalarAsync<decimal>(sql, new { customerId });
}

public async Task<Dictionary<int, int>> GetOrderCountsByCustomerAsync()
{
    const string sql = @"
        SELECT customer_id, COUNT(*) as order_count
        FROM orders
        GROUP BY customer_id";
    
    var results = await _connection.QueryAsync<(int CustomerId, int OrderCount)>(sql);
    return results.ToDictionary(r => r.CustomerId, r => r.OrderCount);
}
```

## Implementation Details

### Code Generation Location

The relationship query methods are generated in:
- **File**: `src/NPA.Generators/RepositoryGenerator.cs`
- **Method**: `GenerateRelationshipQueryMethods()` (lines 3502-3532)
- **Helper Methods**:
  - `GenerateFindByParentMethod()` - For ManyToOne relationships
  - `GenerateCountByParentMethod()` - For ManyToOne relationships
  - `GenerateHasChildrenMethod()` - For OneToMany relationships
  - `GenerateCountChildrenMethod()` - For OneToMany relationships

### Interface Generation

The methods are declared in a separate partial interface:
- **Interface Name**: `{RepositoryName}Partial` (e.g., `IOrderRepositoryPartial`)
- **File Pattern**: `{EntityName}RepositoryExtensions.g.cs`
- **Implementation**: The generated repository class implements both the main interface and the partial interface

### Current Limitations

1. **ID-Based Queries Only**: Currently only supports finding by foreign key IDs, not by related entity properties
2. **No JOIN Queries**: Does not generate methods that require JOINs to filter by related entity properties
3. **No Aggregates**: Does not generate SUM, AVG, MIN, MAX, or GROUP BY queries
4. **No Complex Filters**: Does not support date ranges, amount filters, or subquery-based filters
5. **No Pagination**: Generated methods do not support pagination parameters
6. **No Sorting Options**: Sorting is fixed (by primary key) and not configurable
7. **Single Relationship Only**: Does not support queries across multiple relationships

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
- [ ] Methods to find by related entity properties (not just IDs)
- [ ] Pagination support where appropriate
- [ ] Aggregate functions work correctly (SUM, AVG, MIN, MAX, GROUP BY)
- [ ] Complex filters properly implemented (date ranges, amounts, subqueries)
- [ ] Support for multi-level navigation
- [ ] Configurable sorting options
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

### 📋 Remaining Test Requirements
- [ ] Integration tests for all generated methods
- [ ] Performance tests for complex queries
- [ ] Tests for edge cases (empty collections, null relationships)
- [ ] Tests for aggregate functions accuracy (when implemented)
- [ ] Tests for pagination and sorting (when implemented)
- [ ] Tests for JOIN-based queries (when implemented)
- [ ] Tests for subquery-based filters (when implemented)

## Performance Considerations

### ✅ Current Optimizations
- ✅ Direct WHERE clauses (no unnecessary JOINs for ID-based queries)
- ✅ Efficient COUNT queries for existence checks
- ✅ No N+1 query problems (single query per method call)

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

### 📋 Documentation To-Do
- [ ] Complete API documentation for all planned methods
- [ ] Query optimization guidelines
- [ ] Relationship query patterns and best practices
- [ ] Performance best practices guide
- [ ] Examples for common scenarios
- [ ] Troubleshooting guide
- [ ] Migration guide for adopting relationship query methods

## Next Steps

### Immediate Priorities
1. **Property-Based Queries**: Implement `FindBy{RelatedEntity}{Property}Async` methods (e.g., `FindByCustomerNameAsync`)
2. **Aggregate Methods**: Implement SUM, AVG, MIN, MAX, and GROUP BY queries
3. **Advanced Filters**: Support date ranges, amount filters, and subquery-based filters

### Future Enhancements
1. **Pagination Support**: Add skip/take parameters to collection queries
2. **Configurable Sorting**: Allow custom ORDER BY clauses
3. **Multi-Level Navigation**: Support queries across multiple relationship levels
4. **Complex Filters**: OR/AND combinations, multiple relationship filters
