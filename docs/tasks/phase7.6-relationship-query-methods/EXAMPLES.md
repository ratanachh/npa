# Relationship Query Methods - Examples

## Overview

This document provides practical examples for common scenarios using relationship query methods.

## Table of Contents

1. [E-Commerce Application](#e-commerce-application)
2. [Content Management System](#content-management-system)
3. [Inventory Management](#inventory-management)
4. [Reporting and Analytics](#reporting-and-analytics)
5. [Multi-Tenant Application](#multi-tenant-application)

---

## E-Commerce Application

### Example 1: Customer Order History

**Scenario:** Display a customer's order history with pagination.

```csharp
public class CustomerOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    
    public async Task<OrderHistoryViewModel> GetOrderHistoryAsync(
        int customerId,
        int page,
        int pageSize)
    {
        // Get orders with pagination
        var orders = await _orderRepository.FindByCustomerIdAsync(
            customerId,
            skip: (page - 1) * pageSize,
            take: pageSize,
            orderBy: "OrderDate",
            ascending: false
        );
        
        // Get total count for pagination
        var totalCount = await _orderRepository.CountByCustomerIdAsync(customerId);
        
        // Get customer summary statistics
        var totalSpent = await _customerRepository.GetTotalOrdersTotalAmountAsync(customerId);
        var averageOrderValue = await _customerRepository.GetAverageOrdersTotalAmountAsync(customerId);
        
        return new OrderHistoryViewModel
        {
            Orders = orders.ToList(),
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            CustomerStats = new CustomerStats
            {
                TotalSpent = totalSpent,
                AverageOrderValue = averageOrderValue
            }
        };
    }
}
```

---

### Example 2: Finding Products in Customer Orders

**Scenario:** Find all products a customer has ordered.

```csharp
public class CustomerProductService
{
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly IOrderRepository _orderRepository;
    
    public async Task<IEnumerable<OrderItem>> GetCustomerOrderItemsAsync(int customerId)
    {
        // Navigate: OrderItem → Order → Customer
        var items = await _orderItemRepository.FindByOrderCustomerIdAsync(customerId);
        
        return items;
    }
    
    public async Task<Dictionary<string, int>> GetCustomerProductQuantitiesAsync(int customerId)
    {
        var items = await GetCustomerOrderItemsAsync(customerId);
        
        return items
            .GroupBy(item => item.ProductName)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
    }
}
```

---

### Example 3: VIP Customer Identification

**Scenario:** Identify VIP customers based on order value.

```csharp
public class VipCustomerService
{
    private readonly ICustomerRepository _customerRepository;
    
    public async Task<IEnumerable<Customer>> GetVipCustomersAsync(decimal threshold)
    {
        // Find customers with total order amount above threshold
        return await _customerRepository.FindCustomerTotalAmountAboveAsync(threshold);
    }
    
    public async Task<IEnumerable<Customer>> GetActiveCustomersAsync()
    {
        // Find customers who have placed at least one order
        return await _customerRepository.FindWithOrdersAsync();
    }
    
    public async Task<IEnumerable<Customer>> GetInactiveCustomersAsync()
    {
        // Find customers who haven't placed any orders
        return await _customerRepository.FindWithoutOrdersAsync();
    }
    
    public async Task<CustomerSegmentation> SegmentCustomersAsync()
    {
        var vipThreshold = 10000m;
        var activeThreshold = 5; // Minimum 5 orders
        
        var vipCustomers = await GetVipCustomersAsync(vipThreshold);
        var activeCustomers = await _customerRepository.FindWithOrdersCountAsync(activeThreshold);
        var inactiveCustomers = await GetInactiveCustomersAsync();
        
        return new CustomerSegmentation
        {
            VipCustomers = vipCustomers.ToList(),
            ActiveCustomers = activeCustomers.ToList(),
            InactiveCustomers = inactiveCustomers.ToList()
        };
    }
}
```

---

### Example 4: Order Filtering

**Scenario:** Find orders matching multiple criteria.

```csharp
public class OrderFilterService
{
    private readonly IOrderRepository _orderRepository;
    
    public async Task<IEnumerable<Order>> FindRecentOrdersByCustomerAsync(
        int customerId,
        int daysBack = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-daysBack);
        var endDate = DateTime.UtcNow;
        
        // Use date range filter
        return await _orderRepository.FindByCustomerAndOrderDateRangeAsync(
            customerId,
            startDate,
            endDate
        );
    }
    
    public async Task<IEnumerable<Order>> FindPendingOrdersByCustomerAsync(int customerId)
    {
        // Use AND combination filter
        return await _orderRepository.FindByCustomerAndStatusAsync(customerId, "Pending");
    }
    
    public async Task<IEnumerable<Order>> FindOrdersByCustomerOrSupplierAsync(
        int? customerId,
        int? supplierId)
    {
        // Use OR combination filter
        return await _orderRepository.FindByCustomerOrSupplierAsync(customerId, supplierId);
    }
}
```

---

## Content Management System

### Example 5: Article Comments

**Scenario:** Manage comments on articles.

```csharp
public class ArticleCommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IArticleRepository _articleRepository;
    
    public async Task<ArticleCommentStats> GetArticleCommentStatsAsync(int articleId)
    {
        var commentCount = await _articleRepository.CountCommentsAsync(articleId);
        var hasComments = await _articleRepository.HasCommentsAsync(articleId);
        
        return new ArticleCommentStats
        {
            ArticleId = articleId,
            CommentCount = commentCount,
            HasComments = hasComments
        };
    }
    
    public async Task<IEnumerable<Article>> GetArticlesWithCommentsAsync()
    {
        // Find articles that have comments
        return await _articleRepository.FindWithCommentsAsync();
    }
    
    public async Task<IEnumerable<Article>> GetPopularArticlesAsync(int minComments)
    {
        // Find articles with at least minComments comments
        return await _articleRepository.FindWithCommentsCountAsync(minComments);
    }
}
```

---

## Inventory Management

### Example 6: Product Orders by Location

**Scenario:** Find order items for products ordered from a specific location.

```csharp
public class InventoryLocationService
{
    private readonly IOrderItemRepository _orderItemRepository;
    
    public async Task<IEnumerable<OrderItem>> GetOrderItemsByWarehouseCityAsync(string city)
    {
        // Navigate: OrderItem → Order → Warehouse → Address
        // This would require a 3-level navigation
        var items = await _orderItemRepository.FindByOrderWarehouseAddressCityAsync(city);
        
        return items;
    }
    
    public async Task<Dictionary<int, int>> GetProductQuantitiesByWarehouseAsync(int warehouseId)
    {
        // Navigate: OrderItem → Order → Warehouse
        var items = await _orderItemRepository.FindByOrderWarehouseIdAsync(warehouseId);
        
        return items
            .GroupBy(item => item.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
    }
}
```

---

## Reporting and Analytics

### Example 7: Sales Reporting

**Scenario:** Generate sales reports with aggregations.

```csharp
public class SalesReportService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IOrderRepository _orderRepository;
    
    public async Task<SalesReport> GenerateSalesReportAsync(DateTime startDate, DateTime endDate)
    {
        // Get all customers with orders in date range
        var allCustomers = await _customerRepository.GetAllAsync();
        var customersWithOrders = new List<CustomerSalesData>();
        
        foreach (var customer in allCustomers)
        {
            var orders = await _orderRepository.FindByCustomerAndOrderDateRangeAsync(
                customer.Id,
                startDate,
                endDate
            );
            
            if (orders.Any())
            {
                customersWithOrders.Add(new CustomerSalesData
                {
                    Customer = customer,
                    OrderCount = orders.Count(),
                    TotalAmount = orders.Sum(o => o.TotalAmount),
                    AverageOrderValue = orders.Average(o => o.TotalAmount)
                });
            }
        }
        
        return new SalesReport
        {
            StartDate = startDate,
            EndDate = endDate,
            CustomerSales = customersWithOrders,
            TotalRevenue = customersWithOrders.Sum(c => c.TotalAmount),
            AverageOrderValue = customersWithOrders.Average(c => c.AverageOrderValue)
        };
    }
    
    // More efficient version using GROUP BY
    public async Task<Dictionary<int, CustomerSalesSummary>> GetCustomerSalesSummariesAsync()
    {
        var summaries = await _customerRepository.GetCustomerOrdersSummaryAsync();
        
        return summaries.ToDictionary(
            s => s.Parent.Id,
            s => new CustomerSalesSummary
            {
                OrderCount = s.Count,
                TotalAmount = s.Total ?? 0m,
                AverageAmount = s.Average ?? 0.0
            }
        );
    }
}
```

---

### Example 8: Customer Lifetime Value

**Scenario:** Calculate customer lifetime value based on order history.

```csharp
public class CustomerLifetimeValueService
{
    private readonly ICustomerRepository _customerRepository;
    
    public async Task<CustomerLifetimeValue> CalculateLifetimeValueAsync(int customerId)
    {
        var orderCount = await _customerRepository.CountOrdersAsync(customerId);
        var totalSpent = await _customerRepository.GetTotalOrdersTotalAmountAsync(customerId);
        var averageOrderValue = await _customerRepository.GetAverageOrdersTotalAmountAsync(customerId);
        var minOrderValue = await _customerRepository.GetMinOrdersTotalAmountAsync(customerId);
        var maxOrderValue = await _customerRepository.GetMaxOrdersTotalAmountAsync(customerId);
        
        return new CustomerLifetimeValue
        {
            CustomerId = customerId,
            TotalOrders = orderCount,
            TotalSpent = totalSpent,
            AverageOrderValue = averageOrderValue,
            MinOrderValue = minOrderValue ?? 0m,
            MaxOrderValue = maxOrderValue ?? 0m,
            LifetimeValue = totalSpent
        };
    }
}
```

---

## Multi-Tenant Application

### Example 9: Tenant-Scoped Queries

**Scenario:** Query orders within a tenant context.

```csharp
public class TenantOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ITenantProvider _tenantProvider;
    
    public async Task<IEnumerable<Order>> GetTenantCustomerOrdersAsync(int customerId)
    {
        // Assuming repository respects tenant isolation
        // Query is automatically scoped to current tenant
        return await _orderRepository.FindByCustomerIdAsync(customerId);
    }
    
    public async Task<IEnumerable<Customer>> GetTenantActiveCustomersAsync()
    {
        // Find customers with orders (automatically tenant-scoped)
        return await _customerRepository.FindWithOrdersAsync();
    }
}
```

---

## Complete Example: E-Commerce Order Management System

```csharp
public class CompleteOrderManagementSystem
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CompleteOrderManagementSystem> _logger;
    
    // Get customer dashboard with all statistics
    public async Task<CustomerDashboard> GetCustomerDashboardAsync(int customerId)
    {
        // Use parallel execution for independent queries
        var orderCountTask = _customerRepository.CountOrdersAsync(customerId);
        var totalSpentTask = _customerRepository.GetTotalOrdersTotalAmountAsync(customerId);
        var recentOrdersTask = GetRecentOrdersAsync(customerId);
        var orderStatsTask = GetOrderStatisticsAsync(customerId);
        
        await Task.WhenAll(orderCountTask, totalSpentTask, recentOrdersTask, orderStatsTask);
        
        return new CustomerDashboard
        {
            OrderCount = await orderCountTask,
            TotalSpent = await totalSpentTask,
            RecentOrders = await recentOrdersTask,
            OrderStatistics = await orderStatsTask
        };
    }
    
    private async Task<IEnumerable<Order>> GetRecentOrdersAsync(int customerId)
    {
        return await _orderRepository.FindByCustomerIdAsync(
            customerId,
            skip: 0,
            take: 10,
            orderBy: "OrderDate",
            ascending: false
        );
    }
    
    private async Task<OrderStatistics> GetOrderStatisticsAsync(int customerId)
    {
        var cacheKey = $"customer_{customerId}_stats";
        
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            
            var total = await _customerRepository.GetTotalOrdersTotalAmountAsync(customerId);
            var average = await _customerRepository.GetAverageOrdersTotalAmountAsync(customerId);
            var min = await _customerRepository.GetMinOrdersTotalAmountAsync(customerId);
            var max = await _customerRepository.GetMaxOrdersTotalAmountAsync(customerId);
            
            return new OrderStatistics
            {
                TotalAmount = total,
                AverageAmount = average,
                MinAmount = min ?? 0m,
                MaxAmount = max ?? 0m
            };
        });
    }
    
    // Find orders with complex filtering
    public async Task<IEnumerable<Order>> SearchOrdersAsync(OrderSearchCriteria criteria)
    {
        IEnumerable<Order> orders;
        
        // Start with customer filter if provided
        if (criteria.CustomerId.HasValue)
        {
            if (criteria.Status != null)
            {
                orders = await _orderRepository.FindByCustomerAndStatusAsync(
                    criteria.CustomerId.Value,
                    criteria.Status
                );
            }
            else if (criteria.StartDate.HasValue || criteria.EndDate.HasValue)
            {
                orders = await _orderRepository.FindByCustomerAndOrderDateRangeAsync(
                    criteria.CustomerId.Value,
                    criteria.StartDate,
                    criteria.EndDate
                );
            }
            else
            {
                orders = await _orderRepository.FindByCustomerIdAsync(criteria.CustomerId.Value);
            }
        }
        else
        {
            orders = await _orderRepository.GetAllAsync();
        }
        
        // Apply additional in-memory filters if needed
        if (criteria.MinAmount.HasValue)
        {
            orders = orders.Where(o => o.TotalAmount >= criteria.MinAmount.Value);
        }
        
        if (criteria.MaxAmount.HasValue)
        {
            orders = orders.Where(o => o.TotalAmount <= criteria.MaxAmount.Value);
        }
        
        // Apply pagination
        if (criteria.Page.HasValue && criteria.PageSize.HasValue)
        {
            orders = orders
                .Skip((criteria.Page.Value - 1) * criteria.PageSize.Value)
                .Take(criteria.PageSize.Value);
        }
        
        return orders;
    }
    
    // Get product popularity by location
    public async Task<Dictionary<string, int>> GetProductPopularityByCityAsync()
    {
        // Get all cities
        var allCustomers = await _customerRepository.GetAllAsync();
        var cities = allCustomers
            .SelectMany(c => c.Addresses)
            .Select(a => a.City)
            .Distinct();
        
        var popularity = new Dictionary<string, int>();
        
        foreach (var city in cities)
        {
            // Navigate: OrderItem → Order → Customer → Address
            var items = await _orderItemRepository.FindByOrderCustomerAddressCityAsync(city);
            var productCount = items.GroupBy(i => i.ProductName).Count();
            popularity[city] = productCount;
        }
        
        return popularity;
    }
}

public class OrderSearchCriteria
{
    public int? CustomerId { get; set; }
    public string? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}
```

---

## Advanced Examples

### Example 10: Real-Time Analytics

```csharp
public class RealTimeAnalyticsService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IMemoryCache _cache;
    
    public async Task<RealTimeMetrics> GetRealTimeMetricsAsync()
    {
        // Cache frequently accessed metrics
        var activeCustomersTask = GetCachedActiveCustomersAsync();
        var vipCustomersTask = GetCachedVipCustomersAsync();
        var recentOrdersTask = GetCachedRecentOrdersAsync();
        
        await Task.WhenAll(activeCustomersTask, vipCustomersTask, recentOrdersTask);
        
        return new RealTimeMetrics
        {
            ActiveCustomersCount = (await activeCustomersTask).Count(),
            VipCustomersCount = (await vipCustomersTask).Count(),
            RecentOrdersCount = (await recentOrdersTask).Count()
        };
    }
    
    private async Task<IEnumerable<Customer>> GetCachedActiveCustomersAsync()
    {
        return await _cache.GetOrCreateAsync("active_customers", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return await _customerRepository.FindWithOrdersAsync();
        });
    }
    
    private async Task<IEnumerable<Customer>> GetCachedVipCustomersAsync()
    {
        return await _cache.GetOrCreateAsync("vip_customers", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await _customerRepository.FindCustomerTotalAmountAboveAsync(10000m);
        });
    }
    
    private async Task<IEnumerable<Order>> GetCachedRecentOrdersAsync()
    {
        // This would need a custom method or use GetAllAsync with filtering
        // For demonstration, assuming a method exists
        return await _cache.GetOrCreateAsync("recent_orders", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            var allOrders = await _orderRepository.GetAllAsync();
            return allOrders.Where(o => o.OrderDate >= DateTime.UtcNow.AddHours(-24));
        });
    }
}
```

---

## See Also

- [API Reference](API_REFERENCE.md)
- [Query Optimization Guide](OPTIMIZATION_GUIDE.md)
- [Relationship Query Patterns and Best Practices](PATTERNS_AND_BEST_PRACTICES.md)
- [Performance Best Practices](PERFORMANCE_GUIDE.md)

