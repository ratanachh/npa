# Relationship Query Optimization Guide

## Overview

This guide provides strategies and best practices for optimizing relationship queries in NPA repositories. Follow these guidelines to ensure optimal performance for your database queries.

## Table of Contents

1. [Database Indexing](#database-indexing)
2. [Query Patterns](#query-patterns)
3. [Pagination Strategies](#pagination-strategies)
4. [JOIN Optimization](#join-optimization)
5. [Monitoring and Analysis](#monitoring-and-analysis)

---

## Database Indexing

### Foreign Key Indexes

**Critical:** Always create indexes on foreign key columns to optimize relationship queries.

**Example:**
```sql
-- For orders table with customer_id foreign key
CREATE INDEX idx_orders_customer_id ON orders(customer_id);

-- For order_items table with order_id foreign key
CREATE INDEX idx_order_items_order_id ON order_items(order_id);
```

**Impact:**
- `FindByCustomerIdAsync` queries will use index scans instead of full table scans
- Reduces query time from O(n) to O(log n) for large datasets
- Essential for pagination performance

---

### Composite Indexes

For frequently queried combinations, create composite indexes:

```sql
-- For queries like FindByCustomerAndStatusAsync
CREATE INDEX idx_orders_customer_status ON orders(customer_id, status);

-- For date range queries
CREATE INDEX idx_orders_customer_date ON orders(customer_id, order_date);
```

**Guidelines:**
- Leftmost prefix rule: Index `(customer_id, status)` helps queries filtering by `customer_id` alone or both columns
- Order columns by selectivity (most selective first)
- Don't over-index (maintains write performance)

---

### Covering Indexes

Include frequently selected columns in indexes to avoid table lookups:

```sql
-- Index that covers common query columns
CREATE INDEX idx_orders_customer_covering 
ON orders(customer_id) 
INCLUDE (order_date, total_amount, status);
```

---

## Query Patterns

### Prefer COUNT over Full Queries

**Use COUNT methods for existence checks and counts:**

```csharp
// ✅ Good: Uses COUNT query (faster)
var count = await orderRepository.CountByCustomerIdAsync(123);
var exists = await customerRepository.HasOrdersAsync(123);

// ❌ Avoid: Fetches all records just to count
var orders = await orderRepository.FindByCustomerIdAsync(123);
var count = orders.Count();
```

**Performance Impact:**
- COUNT queries return only the count value (minimal data transfer)
- Full queries materialize all entity objects (memory overhead)
- Significant difference for large result sets

---

### Use Pagination for Large Result Sets

**Always paginate when result sets can be large:**

```csharp
// ✅ Good: Paginated query
var page = await orderRepository.FindByCustomerIdAsync(123, skip: 0, take: 20);

// ❌ Avoid: Fetching all records
var allOrders = await orderRepository.FindByCustomerIdAsync(123);
```

**Benefits:**
- Reduces memory usage
- Faster query execution
- Better user experience

**Best Practices:**
- Default page size: 20-50 records
- Maximum page size: 100-200 records
- Always provide pagination controls in UI

---

### Optimize JOIN Queries

**Property-based queries use JOINs - ensure indexes exist:**

```csharp
// This query performs JOIN between orders and customers
var orders = await orderRepository.FindByCustomerNameAsync("John Doe");
```

**Required Indexes:**
```sql
-- Index on customer_id (FK in orders)
CREATE INDEX idx_orders_customer_id ON orders(customer_id);

-- Index on name (queried column in customers)
CREATE INDEX idx_customers_name ON customers(name);

-- Composite index if frequently queried together
CREATE INDEX idx_customers_name_id ON customers(name, id);
```

---

### Multi-Level Navigation Optimization

**For deep navigation paths, ensure indexes on all join columns:**

```csharp
// 3-level navigation: OrderItem → Order → Customer → Address
var items = await orderItemRepository.FindByOrderCustomerAddressCityAsync("New York");
```

**Required Indexes:**
```sql
CREATE INDEX idx_order_items_order_id ON order_items(order_id);
CREATE INDEX idx_orders_customer_id ON orders(customer_id);
CREATE INDEX idx_addresses_customer_id ON addresses(customer_id);
CREATE INDEX idx_addresses_city ON addresses(city);
```

**Performance Tips:**
- Limit navigation depth (3-4 levels max)
- Consider denormalization for frequently accessed deep paths
- Use materialized views for complex multi-level aggregations

---

## Pagination Strategies

### Offset-Based Pagination

**Standard pagination using OFFSET/FETCH:**

```csharp
var page = await repository.FindByCustomerIdAsync(123, skip: 0, take: 20);
```

**Characteristics:**
- ✅ Simple to implement
- ✅ Works well for first pages
- ⚠️ Performance degrades for large offsets (OFFSET 10000 is slower)
- ⚠️ Results can change if data is inserted/deleted during pagination

**Best For:**
- Small to medium datasets
- Random page access (not common)
- First few pages

---

### Cursor-Based Pagination

**For large datasets, consider cursor-based pagination:**

```csharp
// Sort by a unique, sequential column (like primary key or timestamp)
var page = await repository.FindByCustomerIdAsync(
    123,
    skip: 0,
    take: 20,
    orderBy: "Id",  // Use sequential column
    ascending: true
);

// Next page: use last ID from previous page
var lastId = page.Last().Id;
var nextPage = await repository.FindByCustomerIdAsync(
    123,
    skip: 0,
    take: 20,
    orderBy: "Id",
    ascending: true
);
```

**Better Approach - Add a cursor-based method:**
```csharp
// Custom method (would need to be added manually or via extension)
Task<IEnumerable<Order>> FindByCustomerIdAfterIdAsync(int customerId, int lastId, int take)
```

**Advantages:**
- ✅ Consistent performance regardless of page number
- ✅ Stable results (not affected by data changes)
- ✅ More efficient for deep pagination

---

## JOIN Optimization

### Use Specific Column Lists

**Generated queries use `SELECT *` - consider custom queries for specific columns:**

```csharp
// Generated method uses SELECT *
var orders = await orderRepository.FindByCustomerIdAsync(123);

// Custom query with specific columns (better for large result sets)
var orderIds = await connection.QueryAsync<int>(
    "SELECT id FROM orders WHERE customer_id = @id",
    new { id = 123 }
);
```

---

### JOIN Order Matters

**The generator creates efficient JOIN orders, but verify:**

```sql
-- Generated query (optimized)
SELECT oi.* FROM order_items oi
INNER JOIN orders o ON oi.order_id = o.id      -- Smaller table first
INNER JOIN customers c ON o.customer_id = c.id -- Then join to customers
WHERE c.name = @name
```

**Optimization Tips:**
- Smaller tables should be joined first when possible
- Most selective filters should be applied early
- Use EXPLAIN/EXECUTION PLAN to verify join order

---

### Avoid Unnecessary JOINs

**Use ID-based queries when possible instead of property-based:**

```csharp
// ✅ Preferred: Direct FK query (no JOIN needed)
var orders = await orderRepository.FindByCustomerIdAsync(customerId);

// ⚠️ Less efficient: JOIN query (only use when you don't have the ID)
var orders = await orderRepository.FindByCustomerNameAsync(customerName);
```

---

## Monitoring and Analysis

### Query Execution Plans

**Regularly review execution plans for generated queries:**

```sql
-- PostgreSQL
EXPLAIN ANALYZE 
SELECT * FROM orders WHERE customer_id = 123;

-- SQL Server
SET SHOWPLAN_ALL ON;
SELECT * FROM orders WHERE customer_id = 123;
```

**What to Look For:**
- Index scans (good) vs table scans (bad)
- Missing index suggestions
- High cost operations (sorts, hash joins)

---

### Performance Metrics

**Monitor query performance using application metrics:**

```csharp
var stopwatch = Stopwatch.StartNew();
var orders = await orderRepository.FindByCustomerIdAsync(123);
stopwatch.Stop();

// Log if exceeds threshold
if (stopwatch.ElapsedMilliseconds > 1000)
{
    _logger.LogWarning("Slow query: FindByCustomerIdAsync took {ms}ms", 
        stopwatch.ElapsedMilliseconds);
}
```

**Thresholds:**
- Simple queries: < 100ms
- JOIN queries: < 500ms
- Multi-level navigation: < 1000ms
- Large pagination: < 2000ms

---

### Database Statistics

**Keep database statistics up to date:**

```sql
-- PostgreSQL
ANALYZE orders;
ANALYZE customers;

-- SQL Server
UPDATE STATISTICS orders;
UPDATE STATISTICS customers;
```

**Impact:**
- Helps query optimizer choose best execution plans
- Essential after large data changes
- Schedule regular statistics updates

---

## Caching Strategies

### Application-Level Caching

**Cache frequently accessed relationship queries:**

```csharp
// Example: Cache customer order counts
var cacheKey = $"customer_{customerId}_order_count";
var count = await _cache.GetOrCreateAsync(cacheKey, async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
    return await customerRepository.CountOrdersAsync(customerId);
});
```

**Cache Candidates:**
- Count queries (rarely change)
- Existence checks (rarely change)
- Small, frequently accessed result sets

**Cache Invalidation:**
- Invalidate on entity creation/deletion
- Use sliding expiration for frequently changing data
- Consider cache-aside pattern

---

### Query Result Caching

**For read-heavy scenarios, cache full query results:**

```csharp
var cacheKey = $"orders_customer_{customerId}_page_{pageNumber}";
var orders = await _cache.GetOrCreateAsync(cacheKey, async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
    return await orderRepository.FindByCustomerIdAsync(customerId, skip, take);
});
```

---

## Batch Operations

### Batch Loading

**When loading multiple relationships, consider batch operations:**

```csharp
// ❌ Avoid: N+1 query pattern
var customers = await customerRepository.GetAllAsync();
foreach (var customer in customers)
{
    var orderCount = await customerRepository.CountOrdersAsync(customer.Id);
}

// ✅ Better: Single query with GROUP BY
var counts = await customerRepository.GetOrdersCountsByCustomerAsync();
var countsDict = counts.ToDictionary(c => c.Key, c => c.Value);
foreach (var customer in customers)
{
    var orderCount = countsDict.GetValueOrDefault(customer.Id, 0);
}
```

---

## Best Practices Summary

### ✅ DO

1. **Create indexes on all foreign keys**
2. **Use COUNT methods instead of fetching all records**
3. **Always paginate large result sets**
4. **Monitor query execution plans**
5. **Keep database statistics updated**
6. **Cache frequently accessed, rarely changing data**
7. **Use ID-based queries when IDs are available**
8. **Limit multi-level navigation depth (3-4 levels)**

### ❌ DON'T

1. **Don't fetch all records just to count them**
2. **Don't skip pagination for potentially large result sets**
3. **Don't create too many indexes (hurts write performance)**
4. **Don't ignore query execution plans**
5. **Don't cache frequently changing data**
6. **Don't use property-based queries when ID is available**
7. **Don't navigate more than 4-5 relationship levels**

---

## Example: Complete Optimization Setup

```sql
-- 1. Create foreign key indexes
CREATE INDEX idx_orders_customer_id ON orders(customer_id);
CREATE INDEX idx_order_items_order_id ON order_items(order_id);
CREATE INDEX idx_addresses_customer_id ON addresses(customer_id);

-- 2. Create composite indexes for common query patterns
CREATE INDEX idx_orders_customer_status ON orders(customer_id, status);
CREATE INDEX idx_orders_customer_date ON orders(customer_id, order_date);

-- 3. Create indexes on frequently queried properties
CREATE INDEX idx_customers_name ON customers(name);
CREATE INDEX idx_customers_email ON customers(email);
CREATE INDEX idx_addresses_city ON addresses(city);

-- 4. Update statistics
ANALYZE orders;
ANALYZE customers;
ANALYZE order_items;
ANALYZE addresses;
```

```csharp
// 5. Use optimized query patterns in code
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMemoryCache _cache;
    
    public async Task<PagedResult<Order>> GetCustomerOrdersAsync(
        int customerId, 
        int page, 
        int pageSize)
    {
        // Use pagination
        var orders = await _orderRepository.FindByCustomerIdAsync(
            customerId,
            skip: page * pageSize,
            take: pageSize,
            orderBy: "OrderDate",
            ascending: false
        );
        
        // Use COUNT instead of counting collection
        var totalCount = await _orderRepository.CountByCustomerIdAsync(customerId);
        
        return new PagedResult<Order>
        {
            Items = orders,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
```

---

## Performance Checklist

- [ ] Foreign key indexes created
- [ ] Composite indexes for common query patterns
- [ ] Statistics updated after large data changes
- [ ] Pagination implemented for all list queries
- [ ] COUNT methods used instead of fetching all records
- [ ] Query execution plans reviewed
- [ ] Performance thresholds monitored
- [ ] Caching strategy implemented for read-heavy queries
- [ ] Batch operations used to avoid N+1 queries

---

## See Also

- [API Reference](API_REFERENCE.md)
- [Relationship Query Patterns and Best Practices](PATTERNS_AND_BEST_PRACTICES.md)
- [Performance Best Practices](PERFORMANCE_GUIDE.md)

