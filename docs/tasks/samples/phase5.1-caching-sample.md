# Phase 5.1: Caching Sample

> **⚠️ PLANNED FEATURE**: This sample describes functionality planned for Phase 5.1. Caching support is not yet implemented in NPA. This document serves as a design reference and future implementation guide.

## 📋 Task Overview

**Objective**: Demonstrate first-level and second-level caching for improved query performance.

**Priority**: High  
**Estimated Time**: 5-6 hours  
**Dependencies**: Phase 5.1 (Caching Support) - **NOT YET IMPLEMENTED**  
**Target Framework**: .NET 8.0  
**Sample Name**: CachingSample  
**Status**: 📋 Planned for Phase 5

## 🎯 Success Criteria

- [ ] Demonstrates first-level cache (EntityManager scope)
- [ ] Shows second-level cache (application-wide)
- [ ] Includes cache invalidation strategies
- [ ] Uses Redis for distributed caching
- [ ] Shows performance improvements
- [ ] Demonstrates cache-aside pattern
- [ ] Includes cache statistics

## 📝 Caching Strategies

### 1. First-Level Cache (Session Cache)
```csharp
// Automatic within EntityManager scope
var customer1 = await entityManager.FindAsync<Customer>(1);
var customer2 = await entityManager.FindAsync<Customer>(1); // Returns cached instance
Assert.Same(customer1, customer2); // Same object reference
```

### 2. Second-Level Cache (Application Cache)
```csharp
[Entity]
[Cacheable(Region = "customers", Strategy = CacheStrategy.ReadWrite)]
[Table("customers")]
public class Customer
{
    // Entity properties
}

// Cached across EntityManager instances
using (var em1 = CreateEntityManager())
{
    var customer = await em1.FindAsync<Customer>(1); // Database hit
}

using (var em2 = CreateEntityManager())
{
    var customer = await em2.FindAsync<Customer>(1); // Cache hit
}
```

### 3. Query Result Cache
```csharp
var results = await entityManager.CreateQuery<Customer>()
    .Where("IsActive = true")
    .SetCacheable(true)
    .SetCacheRegion("active-customers")
    .GetResultListAsync(); // Cached results
```

## 💻 Cache Configuration

```csharp
// appsettings.json
{
  "NPA": {
    "Caching": {
      "Enabled": true,
      "Provider": "Redis",
      "Redis": {
        "Configuration": "localhost:6379",
        "InstanceName": "NPA:"
      },
      "Regions": {
        "customers": {
          "Expiration": "00:30:00",
          "Priority": "High"
        },
        "products": {
          "Expiration": "00:15:00",
          "Priority": "Normal"
        }
      }
    }
  }
}
```

## 📊 Performance Metrics

| Operation | No Cache | With Cache | Improvement |
|-----------|----------|------------|-------------|
| Find by ID | 5ms | 0.5ms | 10x |
| Query 100 records | 50ms | 2ms | 25x |
| Complex join | 200ms | 10ms | 20x |

## 🔧 Cache Providers

- **Memory Cache** - In-memory, single server
- **Redis** - Distributed, scalable
- **SQL Server** - Database-backed cache
- **Custom** - Implement ICacheProvider

## 📚 Learning Outcomes

- Caching patterns and strategies
- Performance optimization
- Distributed caching
- Cache invalidation
- Cache coherency

---

*Created: October 8, 2025*  
*Status: ⏳ Pending*
