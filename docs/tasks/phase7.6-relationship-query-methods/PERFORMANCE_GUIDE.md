# Performance Best Practices Guide

## Overview

This guide provides performance best practices specifically for relationship query methods to ensure optimal application performance.

## Table of Contents

1. [Query Performance Metrics](#query-performance-metrics)
2. [Memory Management](#memory-management)
3. [Connection Management](#connection-management)
4. [Caching Strategies](#caching-strategies)
5. [Scalability Considerations](#scalability-considerations)

---

## Query Performance Metrics

### Performance Thresholds

**Target performance metrics for relationship queries:**

| Query Type | Target Time | Warning Threshold | Critical Threshold |
|------------|-------------|-------------------|-------------------|
| Simple FK Query | < 50ms | 100ms | 500ms |
| JOIN Query (1 level) | < 200ms | 500ms | 1000ms |
| Multi-level Navigation (2 levels) | < 500ms | 1000ms | 2000ms |
| Multi-level Navigation (3+ levels) | < 1000ms | 2000ms | 5000ms |
| COUNT/Aggregate | < 100ms | 300ms | 1000ms |
| Paginated Query | < 200ms | 500ms | 2000ms |
| Inverse Query (EXISTS) | < 300ms | 800ms | 2000ms |

---

### Monitoring Query Performance

**Implement performance monitoring:**

```csharp
public class PerformanceMonitoringOrderRepository
{
    private readonly IOrderRepository _repository;
    private readonly ILogger<PerformanceMonitoringOrderRepository> _logger;
    private readonly IMemoryCache _cache;
    
    public async Task<IEnumerable<Order>> FindByCustomerIdAsync(int customerId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _repository.FindByCustomerIdAsync(customerId);
            stopwatch.Stop();
            
            // Log if exceeds threshold
            if (stopwatch.ElapsedMilliseconds > 200)
            {
                _logger.LogWarning(
                    "Slow query: FindByCustomerIdAsync took {ms}ms for customer {id}",
                    stopwatch.ElapsedMilliseconds,
                    customerId
                );
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Query failed: FindByCustomerIdAsync took {ms}ms before failure",
                stopwatch.ElapsedMilliseconds
            );
            throw;
        }
    }
}
```

---

## Memory Management

### Avoid Materializing Large Collections

**❌ Problem:**
```csharp
// Could load thousands of orders into memory
var allOrders = await orderRepository.FindByCustomerIdAsync(customerId);
var processedOrders = allOrders.Where(o => o.Status == "Pending").ToList();
```

**✅ Solution:**
```csharp
// Use pagination to process in batches
const int batchSize = 100;
int skip = 0;
bool hasMore = true;

while (hasMore)
{
    var batch = await orderRepository.FindByCustomerIdAsync(
        customerId,
        skip: skip,
        take: batchSize
    );
    
    var batchList = batch.ToList();
    hasMore = batchList.Count == batchSize;
    
    // Process batch
    foreach (var order in batchList.Where(o => o.Status == "Pending"))
    {
        // Process order...
    }
    
    skip += batchSize;
}
```

---

### Use Streaming for Large Result Sets

**For very large datasets, consider streaming:**

```csharp
public async IAsyncEnumerable<Order> StreamCustomerOrdersAsync(int customerId)
{
    const int pageSize = 100;
    int skip = 0;
    bool hasMore = true;
    
    while (hasMore)
    {
        var page = await _orderRepository.FindByCustomerIdAsync(
            customerId,
            skip: skip,
            take: pageSize
        );
        
        var pageList = page.ToList();
        hasMore = pageList.Count == pageSize;
        
        foreach (var order in pageList)
        {
            yield return order;
        }
        
        skip += pageSize;
    }
}

// Usage
await foreach (var order in StreamCustomerOrdersAsync(customerId))
{
    // Process order without loading all into memory
}
```

---

### Dispose Resources Properly

**Ensure proper disposal of connections and readers:**

```csharp
// Repository methods handle disposal automatically
// But if using direct connections:
using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();
var orders = await connection.QueryAsync<Order>(sql, parameters);
// Connection disposed automatically
```

---

## Connection Management

### Connection Pooling

**Ensure connection pooling is enabled:**

```csharp
// In Startup.cs or Program.cs
services.AddSingleton<IDbConnection>(provider =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    var connection = new NpgsqlConnection(connectionString);
    // Connection pooling is enabled by default in Npgsql
    return connection;
});
```

**Best Practices:**
- Reuse connections (repository pattern handles this)
- Don't create new connections for each query
- Use dependency injection for connection management

---

### Async/Await Best Practices

**Always use async/await for database queries:**

```csharp
// ✅ Good: Async all the way
public async Task<IEnumerable<Order>> GetOrdersAsync(int customerId)
{
    return await _orderRepository.FindByCustomerIdAsync(customerId);
}

// ❌ Avoid: Blocking async calls
public IEnumerable<Order> GetOrders(int customerId)
{
    return _orderRepository.FindByCustomerIdAsync(customerId).Result; // Deadlock risk!
}
```

---

## Caching Strategies

### Cache Frequently Accessed Queries

**Cache read-heavy queries with appropriate expiration:**

```csharp
public class CachedOrderRepository
{
    private readonly IOrderRepository _repository;
    private readonly IMemoryCache _cache;
    
    public async Task<int> CountOrdersAsync(int customerId)
    {
        var cacheKey = $"customer_{customerId}_order_count";
        
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            // Cache for 5 minutes
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            
            // Set priority to avoid eviction under memory pressure
            entry.Priority = CacheItemPriority.Normal;
            
            return await _repository.CountOrdersAsync(customerId);
        });
    }
    
    // Invalidate cache when orders change
    public async Task InvalidateOrderCountCacheAsync(int customerId)
    {
        var cacheKey = $"customer_{customerId}_order_count";
        _cache.Remove(cacheKey);
    }
}
```

---

### Cache Invalidation Patterns

**Invalidate cache on data changes:**

```csharp
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IMemoryCache _cache;
    
    public async Task<Order> CreateOrderAsync(int customerId, Order order)
    {
        // Create order
        await _orderRepository.AddAsync(order);
        await _orderRepository.SaveChangesAsync();
        
        // Invalidate related caches
        _cache.Remove($"customer_{customerId}_order_count");
        _cache.Remove($"customer_{customerId}_order_total");
        _cache.Remove($"customer_{customerId}_has_orders");
        
        return order;
    }
}
```

---

### Distributed Caching

**For multi-instance applications, use distributed cache:**

```csharp
// Using Redis
public class DistributedCachedOrderRepository
{
    private readonly IOrderRepository _repository;
    private readonly IDistributedCache _cache;
    
    public async Task<int> CountOrdersAsync(int customerId)
    {
        var cacheKey = $"customer_{customerId}_order_count";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null && int.TryParse(cached, out var count))
        {
            return count;
        }
        
        count = await _repository.CountOrdersAsync(customerId);
        
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        
        await _cache.SetStringAsync(cacheKey, count.ToString(), options);
        
        return count;
    }
}
```

---

## Scalability Considerations

### Horizontal Scaling

**Design queries to work in distributed environments:**

```csharp
// ✅ Stateless queries (scalable)
var orders = await orderRepository.FindByCustomerIdAsync(customerId);

// ⚠️ Stateful operations (consider caching)
var orderCount = await customerRepository.CountOrdersAsync(customerId);
```

---

### Database Sharding Considerations

**If using database sharding, consider shard key in queries:**

```csharp
// Queries are automatically scoped to the connection's database
// For sharding, ensure connection points to correct shard before querying
var orders = await orderRepository.FindByCustomerIdAsync(customerId);
// Query executes on the shard determined by the connection
```

---

### Read Replicas

**Route read queries to read replicas:**

```csharp
public class ReadReplicaOrderRepository
{
    private readonly IOrderRepository _masterRepository;
    private readonly IOrderRepository _readReplicaRepository;
    
    public async Task<IEnumerable<Order>> FindByCustomerIdAsync(int customerId)
    {
        // Use read replica for queries
        return await _readReplicaRepository.FindByCustomerIdAsync(customerId);
    }
    
    public async Task<Order> AddAsync(Order order)
    {
        // Use master for writes
        return await _masterRepository.AddAsync(order);
    }
}
```

---

## Bulk Operations

### Batch Loading

**Load multiple relationships efficiently:**

```csharp
public async Task<Dictionary<int, IEnumerable<Order>>> GetOrdersByCustomersAsync(
    IEnumerable<int> customerIds)
{
    // Option 1: Use GROUP BY query (if available)
    var allOrders = await _orderRepository.GetAllAsync();
    var ordersByCustomer = allOrders
        .Where(o => customerIds.Contains(o.Customer.Id))
        .GroupBy(o => o.Customer.Id)
        .ToDictionary(g => g.Key, g => g.AsEnumerable());
    
    // Option 2: Parallel queries (if not too many)
    var tasks = customerIds.Select(id => 
        _orderRepository.FindByCustomerIdAsync(id)
    );
    var results = await Task.WhenAll(tasks);
    
    return customerIds.Zip(results)
        .ToDictionary(x => x.First, x => x.Second);
}
```

---

### Parallel Query Execution

**Execute independent queries in parallel:**

```csharp
public async Task<CustomerDashboard> GetDashboardAsync(int customerId)
{
    // Execute independent queries in parallel
    var orderCountTask = _customerRepository.CountOrdersAsync(customerId);
    var totalAmountTask = _customerRepository.GetTotalOrdersTotalAmountAsync(customerId);
    var recentOrdersTask = _orderRepository.FindByCustomerIdAsync(
        customerId, skip: 0, take: 10, orderBy: "OrderDate", ascending: false
    );
    var hasActiveOrdersTask = _customerRepository.HasOrdersAsync(customerId);
    
    await Task.WhenAll(
        orderCountTask,
        totalAmountTask,
        recentOrdersTask,
        hasActiveOrdersTask
    );
    
    return new CustomerDashboard
    {
        OrderCount = await orderCountTask,
        TotalAmount = await totalAmountTask,
        RecentOrders = (await recentOrdersTask).ToList(),
        HasActiveOrders = await hasActiveOrdersTask
    };
}
```

---

## Query Result Caching

### Cache Query Results

**Cache expensive query results:**

```csharp
public async Task<IEnumerable<Customer>> GetVipCustomersAsync()
{
    var cacheKey = "vip_customers";
    
    return await _cache.GetOrCreateAsync(cacheKey, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
        
        // Expensive query
        return await _customerRepository.FindCustomerTotalAmountAboveAsync(10000m);
    });
}
```

---

### Cache Warming

**Pre-warm cache for frequently accessed data:**

```csharp
public class CacheWarmupService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMemoryCache _cache;
    
    public async Task WarmupCacheAsync()
    {
        // Pre-cache frequently accessed queries
        var activeCustomers = await _customerRepository.FindWithOrdersAsync();
        
        foreach (var customer in activeCustomers.Take(100)) // Top 100
        {
            var cacheKey = $"customer_{customer.Id}_order_count";
            var count = await _customerRepository.CountOrdersAsync(customer.Id);
            _cache.Set(cacheKey, count, TimeSpan.FromMinutes(5));
        }
    }
}
```

---

## Performance Checklist

### Before Deployment

- [ ] All foreign key columns have indexes
- [ ] Composite indexes created for common query patterns
- [ ] Query execution plans reviewed and optimized
- [ ] Performance thresholds defined and monitored
- [ ] Caching strategy implemented for read-heavy queries
- [ ] Pagination implemented for all list queries
- [ ] Memory usage tested with large datasets
- [ ] Connection pooling configured correctly
- [ ] Async/await used consistently
- [ ] N+1 query patterns eliminated

### Ongoing Monitoring

- [ ] Query execution times logged
- [ ] Slow query alerts configured
- [ ] Database statistics kept up to date
- [ ] Cache hit rates monitored
- [ ] Memory usage monitored
- [ ] Connection pool utilization tracked
- [ ] Index usage analyzed regularly

---

## Performance Testing

### Load Testing Scenarios

```csharp
[Fact]
[Trait("Category", "Performance")]
public async Task FindByCustomerIdAsync_ShouldHandleConcurrentRequests()
{
    var customerId = 1;
    var concurrentRequests = 100;
    
    var tasks = Enumerable.Range(0, concurrentRequests)
        .Select(_ => _orderRepository.FindByCustomerIdAsync(customerId))
        .ToArray();
    
    var stopwatch = Stopwatch.StartNew();
    var results = await Task.WhenAll(tasks);
    stopwatch.Stop();
    
    // All requests should complete
    results.Should().AllSatisfy(r => r.Should().NotBeNull());
    
    // Should complete within reasonable time (e.g., 5 seconds)
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
}
```

---

## See Also

- [API Reference](API_REFERENCE.md)
- [Query Optimization Guide](OPTIMIZATION_GUIDE.md)
- [Relationship Query Patterns and Best Practices](PATTERNS_AND_BEST_PRACTICES.md)

