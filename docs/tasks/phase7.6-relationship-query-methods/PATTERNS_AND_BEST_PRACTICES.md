# Relationship Query Patterns and Best Practices

## Overview

This guide presents common patterns and best practices for using relationship query methods effectively in your NPA applications.

## Table of Contents

1. [Common Query Patterns](#common-query-patterns)
2. [Anti-Patterns to Avoid](#anti-patterns-to-avoid)
3. [Best Practices](#best-practices)
4. [Design Patterns](#design-patterns)

---

## Common Query Patterns

### Pattern 1: Finding Related Entities

**Scenario:** Get all orders for a specific customer.

**✅ Recommended:**
```csharp
var orders = await orderRepository.FindByCustomerIdAsync(customerId);
```

**❌ Avoid:**
```csharp
var allOrders = await orderRepository.GetAllAsync();
var customerOrders = allOrders.Where(o => o.Customer?.Id == customerId).ToList();
```

**Why:**
- Uses database-level filtering (faster)
- Reduces memory usage
- Leverages database indexes

---

### Pattern 2: Existence Checks

**Scenario:** Check if a customer has any orders before allowing deletion.

**✅ Recommended:**
```csharp
var hasOrders = await customerRepository.HasOrdersAsync(customerId);
if (hasOrders)
{
    throw new InvalidOperationException("Cannot delete customer with existing orders");
}
```

**❌ Avoid:**
```csharp
var orders = await orderRepository.FindByCustomerIdAsync(customerId);
if (orders.Any())
{
    throw new InvalidOperationException("Cannot delete customer with existing orders");
}
```

**Why:**
- COUNT query stops after finding first match (potentially)
- No entity materialization overhead
- Better performance for large datasets

---

### Pattern 3: Filtering with Multiple Criteria

**Scenario:** Find pending orders for a customer placed in the last month.

**✅ Recommended:**
```csharp
var orders = await orderRepository.FindByCustomerAndOrderDateRangeAsync(
    customerId,
    startDate: DateTime.UtcNow.AddMonths(-1),
    endDate: DateTime.UtcNow
);
// Then filter by status in memory (or use complex filter if available)
var pendingOrders = orders.Where(o => o.Status == "Pending").ToList();
```

**Better: Use AND combination (if generated):**
```csharp
var orders = await orderRepository.FindByCustomerAndStatusAsync(
    customerId, 
    "Pending"
);
// Then filter by date range
var recentOrders = orders.Where(o => 
    o.OrderDate >= DateTime.UtcNow.AddMonths(-1)
).ToList();
```

**Best: Custom query for complex filters:**
```csharp
// Use [Query] attribute or custom repository method
var sql = @"
    SELECT * FROM orders 
    WHERE customer_id = @customerId 
      AND status = @status
      AND order_date >= @startDate 
      AND order_date <= @endDate";
var orders = await connection.QueryAsync<Order>(sql, new { 
    customerId, status = "Pending", 
    startDate = DateTime.UtcNow.AddMonths(-1),
    endDate = DateTime.UtcNow 
});
```

---

### Pattern 4: Finding Parents by Child Criteria

**Scenario:** Find all customers who have placed orders above a certain amount.

**✅ Recommended:**
```csharp
var customers = await customerRepository.FindCustomerTotalAmountAboveAsync(1000m);
```

**❌ Avoid:**
```csharp
var allCustomers = await customerRepository.GetAllAsync();
var filteredCustomers = new List<Customer>();
foreach (var customer in allCustomers)
{
    var total = await customerRepository.GetTotalOrdersTotalAmountAsync(customer.Id);
    if (total >= 1000m)
    {
        filteredCustomers.Add(customer);
    }
}
```

**Why:**
- Single database query with subquery
- No N+1 query problem
- Efficient aggregation

---

### Pattern 5: Pagination with Sorting

**Scenario:** Display customer orders sorted by date (newest first) with pagination.

**✅ Recommended:**
```csharp
var orders = await orderRepository.FindByCustomerIdAsync(
    customerId,
    skip: (pageNumber - 1) * pageSize,
    take: pageSize,
    orderBy: "OrderDate",
    ascending: false
);
```

**Benefits:**
- Database-level sorting (efficient)
- Only fetches required page
- Consistent ordering

---

### Pattern 6: Finding Empty Relationships

**Scenario:** Find all customers who haven't placed any orders (for marketing campaigns).

**✅ Recommended:**
```csharp
var inactiveCustomers = await customerRepository.FindWithoutOrdersAsync();
```

**Use Cases:**
- Identifying inactive users
- Cleanup operations
- Targeted marketing

---

### Pattern 7: Multi-Level Navigation

**Scenario:** Find all order items for customers in a specific city.

**✅ Recommended:**
```csharp
var items = await orderItemRepository.FindByOrderCustomerAddressCityAsync("New York");
```

**Benefits:**
- Single query with multiple JOINs
- Database-optimized navigation
- Type-safe navigation path

**Alternative for complex queries:**
```csharp
// If method not generated, use custom query
var sql = @"
    SELECT oi.* FROM order_items oi
    INNER JOIN orders o ON oi.order_id = o.id
    INNER JOIN customers c ON o.customer_id = c.id
    INNER JOIN addresses a ON c.id = a.customer_id
    WHERE a.city = @city";
var items = await connection.QueryAsync<OrderItem>(sql, new { city });
```

---

### Pattern 8: Aggregating Related Data

**Scenario:** Get order statistics for a customer dashboard.

**✅ Recommended:**
```csharp
var orderCount = await customerRepository.CountOrdersAsync(customerId);
var totalAmount = await customerRepository.GetTotalOrdersTotalAmountAsync(customerId);
var averageAmount = await customerRepository.GetAverageOrdersTotalAmountAsync(customerId);
var minAmount = await customerRepository.GetMinOrdersTotalAmountAsync(customerId);
var maxAmount = await customerRepository.GetMaxOrdersTotalAmountAsync(customerId);
```

**For Multiple Customers (Efficient):**
```csharp
var counts = await customerRepository.GetOrdersCountsByCustomerAsync();
var totals = await customerRepository.GetTotalOrdersTotalAmountByCustomerAsync();
// Combine in application logic
```

---

## Anti-Patterns to Avoid

### Anti-Pattern 1: N+1 Queries

**❌ Avoid:**
```csharp
var customers = await customerRepository.GetAllAsync();
foreach (var customer in customers)
{
    var orders = await orderRepository.FindByCustomerIdAsync(customer.Id);
    // Process orders...
}
```

**✅ Fix:**
```csharp
var customers = await customerRepository.GetAllAsync();
var customerIds = customers.Select(c => c.Id).ToList();

// Option 1: Use batch query (custom implementation needed)
var allOrders = await orderRepository.FindByCustomerIdsAsync(customerIds); // Custom method

// Option 2: Use grouping query
var ordersByCustomer = (await orderRepository.GetAllAsync())
    .GroupBy(o => o.Customer.Id)
    .ToDictionary(g => g.Key, g => g.ToList());

foreach (var customer in customers)
{
    var orders = ordersByCustomer.GetValueOrDefault(customer.Id, new List<Order>());
    // Process orders...
}
```

---

### Anti-Pattern 2: Fetching All to Filter in Memory

**❌ Avoid:**
```csharp
var allOrders = await orderRepository.GetAllAsync();
var customerOrders = allOrders.Where(o => o.Customer?.Id == customerId).ToList();
```

**✅ Fix:**
```csharp
var customerOrders = await orderRepository.FindByCustomerIdAsync(customerId);
```

---

### Anti-Pattern 3: Not Using Pagination

**❌ Avoid:**
```csharp
var allOrders = await orderRepository.FindByCustomerIdAsync(customerId);
// Could return thousands of records!
```

**✅ Fix:**
```csharp
var orders = await orderRepository.FindByCustomerIdAsync(
    customerId,
    skip: 0,
    take: 50
);
```

---

### Anti-Pattern 4: Counting by Materializing Collections

**❌ Avoid:**
```csharp
var orders = await orderRepository.FindByCustomerIdAsync(customerId);
var count = orders.Count();
```

**✅ Fix:**
```csharp
var count = await orderRepository.CountByCustomerIdAsync(customerId);
```

---

### Anti-Pattern 5: Deep Navigation Chains

**❌ Avoid:**
```csharp
var items = await orderItemRepository.GetAllAsync();
var filteredItems = items
    .Where(item => item.Order?.Customer?.Address?.City == "New York")
    .ToList();
```

**✅ Fix:**
```csharp
var items = await orderItemRepository.FindByOrderCustomerAddressCityAsync("New York");
```

---

## Best Practices

### 1. Use Type-Safe Methods

**✅ Good:**
```csharp
var orders = await orderRepository.FindByCustomerIdAsync(customerId);
```

**❌ Avoid:**
```csharp
var sql = $"SELECT * FROM orders WHERE customer_id = {customerId}"; // SQL injection risk
```

---

### 2. Handle Nullable Relationships

**✅ Good:**
```csharp
// For nullable relationships, use nullable parameters
var orders = await orderRepository.FindByCustomerOrSupplierAsync(
    customerId: 123,
    supplierId: null
);
```

**Note:** OR combination methods accept nullable parameters.

---

### 3. Use Appropriate Return Types

**✅ Good:**
```csharp
// For existence checks
var hasOrders = await customerRepository.HasOrdersAsync(customerId);

// For counts
var count = await customerRepository.CountOrdersAsync(customerId);

// For collections
var orders = await orderRepository.FindByCustomerIdAsync(customerId);
```

---

### 4. Combine Filters Efficiently

**✅ Good:**
```csharp
// Use AND combination for multiple filters on same entity
var orders = await orderRepository.FindByCustomerAndStatusAsync(customerId, "Pending");

// Then apply additional filters in memory if needed
var recentOrders = orders.Where(o => o.OrderDate >= DateTime.UtcNow.AddDays(-30)).ToList();
```

---

### 5. Use Aggregates for Statistics

**✅ Good:**
```csharp
var total = await customerRepository.GetTotalOrdersTotalAmountAsync(customerId);
var average = await customerRepository.GetAverageOrdersTotalAmountAsync(customerId);
```

**❌ Avoid:**
```csharp
var orders = await orderRepository.FindByCustomerIdAsync(customerId);
var total = orders.Sum(o => o.TotalAmount);
var average = orders.Average(o => o.TotalAmount);
```

---

### 6. Cache Expensive Queries

**✅ Good:**
```csharp
var cacheKey = $"customer_{customerId}_order_count";
var count = await _cache.GetOrCreateAsync(cacheKey, async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
    return await customerRepository.CountOrdersAsync(customerId);
});
```

---

### 7. Batch Related Queries

**✅ Good:**
```csharp
// Get multiple aggregates in parallel
var countTask = customerRepository.CountOrdersAsync(customerId);
var totalTask = customerRepository.GetTotalOrdersTotalAmountAsync(customerId);
var hasOrdersTask = customerRepository.HasOrdersAsync(customerId);

await Task.WhenAll(countTask, totalTask, hasOrdersTask);

var count = await countTask;
var total = await totalTask;
var hasOrders = await hasOrdersTask;
```

---

## Design Patterns

### Repository Pattern

**Use repositories for all database access:**

```csharp
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    
    public async Task<OrderSummary> GetOrderSummaryAsync(int customerId)
    {
        var orderCount = await _customerRepository.CountOrdersAsync(customerId);
        var totalAmount = await _customerRepository.GetTotalOrdersTotalAmountAsync(customerId);
        var recentOrders = await _orderRepository.FindByCustomerIdAsync(
            customerId,
            skip: 0,
            take: 10,
            orderBy: "OrderDate",
            ascending: false
        );
        
        return new OrderSummary
        {
            OrderCount = orderCount,
            TotalAmount = totalAmount,
            RecentOrders = recentOrders.ToList()
        };
    }
}
```

---

### Specification Pattern

**Combine generated methods with specifications:**

```csharp
public class OrderSpecification
{
    public int? CustomerId { get; set; }
    public string? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public async Task<IEnumerable<Order>> FindOrdersAsync(
    IOrderRepository repository,
    OrderSpecification spec)
{
    IEnumerable<Order> orders;
    
    if (spec.CustomerId.HasValue && spec.Status != null)
    {
        orders = await repository.FindByCustomerAndStatusAsync(
            spec.CustomerId.Value,
            spec.Status
        );
    }
    else if (spec.CustomerId.HasValue)
    {
        orders = await repository.FindByCustomerIdAsync(spec.CustomerId.Value);
    }
    else
    {
        orders = await repository.GetAllAsync();
    }
    
    // Apply additional filters
    if (spec.StartDate.HasValue)
    {
        orders = orders.Where(o => o.OrderDate >= spec.StartDate.Value);
    }
    if (spec.EndDate.HasValue)
    {
        orders = orders.Where(o => o.OrderDate <= spec.EndDate.Value);
    }
    
    return orders;
}
```

---

### Facade Pattern

**Create service facades for complex queries:**

```csharp
public class CustomerAnalyticsService
{
    private readonly ICustomerRepository _customerRepository;
    
    public async Task<CustomerAnalytics> GetAnalyticsAsync(int customerId)
    {
        var analytics = new CustomerAnalytics
        {
            CustomerId = customerId,
            OrderCount = await _customerRepository.CountOrdersAsync(customerId),
            TotalSpent = await _customerRepository.GetTotalOrdersTotalAmountAsync(customerId),
            AverageOrderValue = await _customerRepository.GetAverageOrdersTotalAmountAsync(customerId),
            HasActiveOrders = await _customerRepository.HasOrdersAsync(customerId)
        };
        
        return analytics;
    }
}
```

---

## Common Scenarios

### Scenario 1: E-Commerce Order Management

```csharp
public class OrderManagementService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    
    // Get customer's order history with pagination
    public async Task<PagedResult<Order>> GetCustomerOrderHistoryAsync(
        int customerId,
        int page,
        int pageSize)
    {
        var orders = await _orderRepository.FindByCustomerIdAsync(
            customerId,
            skip: (page - 1) * pageSize,
            take: pageSize,
            orderBy: "OrderDate",
            ascending: false
        );
        
        var totalCount = await _orderRepository.CountByCustomerIdAsync(customerId);
        
        return new PagedResult<Order>
        {
            Items = orders.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
    
    // Find VIP customers (total spent > threshold)
    public async Task<IEnumerable<Customer>> GetVipCustomersAsync(decimal threshold)
    {
        return await _customerRepository.FindCustomerTotalAmountAboveAsync(threshold);
    }
    
    // Get active customers (customers with orders)
    public async Task<IEnumerable<Customer>> GetActiveCustomersAsync()
    {
        return await _customerRepository.FindWithOrdersAsync();
    }
}
```

---

### Scenario 2: Inventory Management

```csharp
public class InventoryService
{
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly IProductRepository _productRepository;
    
    // Find products in orders for a specific customer
    public async Task<IEnumerable<OrderItem>> GetCustomerOrderItemsAsync(
        int customerId)
    {
        // Navigate: OrderItem → Order → Customer
        var items = await _orderItemRepository.FindByOrderCustomerIdAsync(customerId);
        return items;
    }
    
    // Find order items for orders in a specific city
    public async Task<IEnumerable<OrderItem>> GetOrderItemsByCityAsync(string city)
    {
        // Navigate: OrderItem → Order → Customer → Address
        var items = await _orderItemRepository.FindByOrderCustomerAddressCityAsync(city);
        return items;
    }
}
```

---

### Scenario 3: Reporting and Analytics

```csharp
public class ReportingService
{
    private readonly ICustomerRepository _customerRepository;
    
    // Get order statistics grouped by customer
    public async Task<Dictionary<int, CustomerOrderStats>> GetCustomerOrderStatsAsync()
    {
        var counts = await _customerRepository.GetOrdersCountsByCustomerAsync();
        var totals = await _customerRepository.GetTotalOrdersTotalAmountByCustomerAsync();
        
        var stats = new Dictionary<int, CustomerOrderStats>();
        foreach (var kvp in counts)
        {
            stats[kvp.Key] = new CustomerOrderStats
            {
                CustomerId = kvp.Key,
                OrderCount = kvp.Value,
                TotalAmount = totals.GetValueOrDefault(kvp.Key, 0m)
            };
        }
        
        return stats;
    }
    
    // Get customer summary with detailed stats
    public async Task<IEnumerable<CustomerSummary>> GetCustomerSummariesAsync()
    {
        var summaries = await _customerRepository.GetCustomerOrdersSummaryAsync();
        
        return summaries.Select(s => new CustomerSummary
        {
            Customer = s.Parent,
            OrderCount = s.Count,
            TotalAmount = s.Total ?? 0m,
            AverageOrderValue = s.Average ?? 0.0
        });
    }
}
```

---

## Summary

### Key Takeaways

1. **Use generated methods** - They're optimized and type-safe
2. **Always paginate** - Never fetch all records without pagination
3. **Prefer COUNT over materialization** - Use count methods instead of counting collections
4. **Avoid N+1 queries** - Use batch operations or grouping
5. **Use appropriate methods** - Choose the most specific method for your use case
6. **Handle nullability** - Be aware of nullable relationships and parameters
7. **Cache strategically** - Cache read-heavy, rarely changing queries
8. **Monitor performance** - Track query execution times

---

## See Also

- [API Reference](API_REFERENCE.md)
- [Query Optimization Guide](OPTIMIZATION_GUIDE.md)
- [Performance Best Practices](PERFORMANCE_GUIDE.md)
- [Examples](EXAMPLES.md)

