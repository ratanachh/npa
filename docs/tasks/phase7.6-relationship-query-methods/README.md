# Phase 7.6: Relationship Query Methods

## Overview
Generate specialized query methods for navigating and querying entities based on their relationships. Provide intuitive methods to find entities through their related entities.

## Objectives
- Generate relationship navigation query methods
- Support filtering by related entity properties
- Implement exists/count methods for relationships
- Create optimized relationship queries

## Tasks

### 1. Navigation Query Methods
- [ ] Generate `GetByRelatedEntityAsync` methods
- [ ] Create methods to find entities by relationship properties
- [ ] Support navigation through multiple levels
- [ ] Implement pagination for relationship queries

### 2. Collection Query Methods
- [ ] Generate `GetItemsByParentAsync` methods
- [ ] Create `FindAllByRelatedEntityAsync` methods
- [ ] Support filtering collections by parent properties
- [ ] Implement sorting for relationship collections

### 3. Relationship Existence Methods
- [ ] Generate `HasRelatedEntityAsync` methods
- [ ] Create `ExistsWithRelationshipAsync` methods
- [ ] Support checking multiple relationships
- [ ] Implement efficient existence queries

### 4. Count and Aggregate Methods
- [ ] Generate `CountRelatedAsync` methods
- [ ] Create `GetRelationshipStatisticsAsync` methods
- [ ] Support aggregation functions on relationships
- [ ] Implement grouped counts

### 5. Advanced Relationship Queries
- [ ] Generate methods for complex relationship filters
- [ ] Support OR/AND combinations in relationship queries
- [ ] Implement subquery generation for relationships
- [ ] Create methods for relationship graph queries

## Example Usage

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

// Generated query methods:
public interface IOrderRepository : IRepository<Order, int>
{
    // Navigation methods
    Task<IEnumerable<Order>> FindByCustomerIdAsync(int customerId);
    Task<IEnumerable<Order>> FindByCustomerNameAsync(string customerName);
    Task<IEnumerable<Order>> FindByCustomerCountryAsync(string country);
    
    // Relationship existence
    Task<bool> HasItemsAsync(int orderId);
    Task<bool> CustomerHasOrdersAsync(int customerId);
    
    // Count methods
    Task<int> CountByCustomerIdAsync(int customerId);
    Task<int> CountItemsAsync(int orderId);
    Task<Dictionary<int, int>> GetOrderCountsByCustomerAsync();
    
    // Advanced queries
    Task<IEnumerable<Order>> FindByCustomerAndDateRangeAsync(
        int customerId, 
        DateTime startDate, 
        DateTime endDate);
    
    Task<IEnumerable<Order>> FindWithMinimumItemsAsync(int minItems);
    Task<IEnumerable<Order>> FindCustomerOrdersAboveAmountAsync(
        int customerId, 
        decimal minAmount);
}

public interface ICustomerRepository : IRepository<Customer, int>
{
    // Inverse relationship queries
    Task<IEnumerable<Customer>> FindWithOrdersAsync();
    Task<IEnumerable<Customer>> FindWithoutOrdersAsync();
    Task<IEnumerable<Customer>> FindWithOrderCountAsync(int minOrderCount);
    Task<IEnumerable<Customer>> FindByOrderDateRangeAsync(
        DateTime startDate, 
        DateTime endDate);
    
    // Aggregates
    Task<decimal> GetTotalOrderAmountAsync(int customerId);
    Task<Dictionary<int, decimal>> GetTotalOrderAmountsByCustomerAsync();
}
```

## Generated Code Examples

### Find By Related Entity
```csharp
public async Task<IEnumerable<Order>> FindByCustomerIdAsync(int customerId)
{
    const string sql = @"
        SELECT o.*
        FROM orders o
        WHERE o.customer_id = @customerId
        ORDER BY o.order_date DESC";
    
    return await _connection.QueryAsync<Order>(sql, new { customerId });
}

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

### Relationship Existence Check
```csharp
public async Task<bool> HasItemsAsync(int orderId)
{
    const string sql = @"
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM order_items WHERE order_id = @orderId
        ) THEN 1 ELSE 0 END";
    
    return await _connection.ExecuteScalarAsync<bool>(sql, new { orderId });
}

public async Task<bool> CustomerHasOrdersAsync(int customerId)
{
    const string sql = @"
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM orders WHERE customer_id = @customerId
        ) THEN 1 ELSE 0 END";
    
    return await _connection.ExecuteScalarAsync<bool>(sql, new { customerId });
}
```

### Count Methods
```csharp
public async Task<int> CountByCustomerIdAsync(int customerId)
{
    const string sql = "SELECT COUNT(*) FROM orders WHERE customer_id = @customerId";
    return await _connection.ExecuteScalarAsync<int>(sql, new { customerId });
}

public async Task<int> CountItemsAsync(int orderId)
{
    const string sql = "SELECT COUNT(*) FROM order_items WHERE order_id = @orderId";
    return await _connection.ExecuteScalarAsync<int>(sql, new { orderId });
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

### Advanced Relationship Queries
```csharp
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

public async Task<IEnumerable<Customer>> FindWithOrdersAsync()
{
    const string sql = @"
        SELECT DISTINCT c.*
        FROM customers c
        INNER JOIN orders o ON o.customer_id = c.id
        ORDER BY c.name";
    
    return await _connection.QueryAsync<Customer>(sql);
}

public async Task<IEnumerable<Customer>> FindWithoutOrdersAsync()
{
    const string sql = @"
        SELECT c.*
        FROM customers c
        WHERE NOT EXISTS (
            SELECT 1 FROM orders o WHERE o.customer_id = c.id
        )
        ORDER BY c.name";
    
    return await _connection.QueryAsync<Customer>(sql);
}

public async Task<IEnumerable<Customer>> FindWithOrderCountAsync(int minOrderCount)
{
    const string sql = @"
        SELECT c.*
        FROM customers c
        WHERE (
            SELECT COUNT(*) 
            FROM orders o 
            WHERE o.customer_id = c.id
        ) >= @minOrderCount
        ORDER BY c.name";
    
    return await _connection.QueryAsync<Customer>(sql, new { minOrderCount });
}
```

### Aggregate Methods
```csharp
public async Task<decimal> GetTotalOrderAmountAsync(int customerId)
{
    const string sql = @"
        SELECT COALESCE(SUM(total_amount), 0)
        FROM orders
        WHERE customer_id = @customerId";
    
    return await _connection.ExecuteScalarAsync<decimal>(sql, new { customerId });
}

public async Task<Dictionary<int, decimal>> GetTotalOrderAmountsByCustomerAsync()
{
    const string sql = @"
        SELECT customer_id, SUM(total_amount) as total
        FROM orders
        GROUP BY customer_id";
    
    var results = await _connection.QueryAsync<(int CustomerId, decimal Total)>(sql);
    return results.ToDictionary(r => r.CustomerId, r => r.Total);
}

public async Task<IEnumerable<(int CustomerId, string CustomerName, int OrderCount, decimal TotalAmount)>> 
    GetCustomerOrderSummaryAsync()
{
    const string sql = @"
        SELECT 
            c.id as CustomerId,
            c.name as CustomerName,
            COUNT(o.id) as OrderCount,
            COALESCE(SUM(o.total_amount), 0) as TotalAmount
        FROM customers c
        LEFT JOIN orders o ON o.customer_id = c.id
        GROUP BY c.id, c.name
        ORDER BY TotalAmount DESC";
    
    return await _connection.QueryAsync<
        (int CustomerId, string CustomerName, int OrderCount, decimal TotalAmount)>(sql);
}
```

## Acceptance Criteria
- [ ] Navigation methods generated for all relationships
- [ ] Query methods follow consistent naming conventions
- [ ] Efficient SQL queries generated (no N+1 problems)
- [ ] Support for all relationship types
- [ ] Pagination support where appropriate
- [ ] Aggregate functions work correctly
- [ ] Existence checks are optimized
- [ ] Complex filters properly implemented

## Dependencies
- Phase 7.1: Relationship-Aware Repository Generation
- Phase 2.1: Relationship Mapping
- Phase 1.3: Simple Query Support

## Testing Requirements
- Unit tests for query generation
- Integration tests for all generated methods
- Performance tests for complex queries
- Tests for edge cases (empty collections, null relationships)
- Tests for aggregate functions accuracy
- Tests for pagination and sorting

## Performance Considerations
- Use indexes on foreign keys
- Optimize JOIN operations
- Avoid N+1 query problems
- Use appropriate query types (scalar, single, multiple)
- Consider query result caching
- Monitor query execution plans

## Documentation
- Complete API documentation for generated methods
- Query optimization guidelines
- Relationship query patterns
- Performance best practices
- Examples for common scenarios
- Troubleshooting guide
