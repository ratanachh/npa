# Phase 3.3: Bulk Operations Sample

> **⚠️ PLANNED FEATURE**: This sample describes functionality planned for Phase 3.3. Bulk operations are not yet implemented in NPA. This document serves as a design reference and future implementation guide.

## 📋 Task Overview

**Objective**: Demonstrate high-performance bulk insert, update, and delete operations using Dapper's batch capabilities.

**Priority**: High  
**Estimated Time**: 4-6 hours  
**Dependencies**: Phase 3.3 (Bulk Operations) - **NOT YET IMPLEMENTED**  
**Target Framework**: .NET 8.0  
**Sample Name**: BulkOperationsSample  
**Status**: 📋 Planned for Phase 3

## 🎯 Success Criteria

- [ ] Demonstrates bulk insert (1000+ records)
- [ ] Shows bulk update performance
- [ ] Includes bulk delete operations
- [ ] Compares bulk vs individual operations
- [ ] Measures performance metrics
- [ ] Shows provider-specific optimizations
- [ ] Includes progress reporting

## 📝 Bulk Operations

### 1. Bulk Insert
```csharp
// Generate 10,000 test records
var customers = GenerateTestCustomers(10000);

// Bulk insert - efficient batch operation
var stopwatch = Stopwatch.StartNew();
await entityManager.BulkInsertAsync(customers);
stopwatch.Stop();

Console.WriteLine($"Bulk inserted {customers.Count} records in {stopwatch.ElapsedMilliseconds}ms");
```

### 2. Bulk Update
```csharp
// Load customers to update
var customers = await entityManager.CreateQuery<Customer>()
    .Where("IsActive = false")
    .GetResultListAsync();

// Update in bulk
foreach (var customer in customers)
{
    customer.UpdatedAt = DateTime.UtcNow;
}

await entityManager.BulkUpdateAsync(customers);
```

### 3. Bulk Delete
```csharp
var idsToDelete = inactiveCustomers.Select(c => c.Id);
await entityManager.BulkDeleteAsync<Customer>(idsToDelete);
```

## 💻 Performance Comparisons

| Operation | Individual | Bulk | Speedup |
|-----------|-----------|------|---------|
| Insert 10K | ~30s | ~2s | 15x |
| Update 10K | ~25s | ~1.5s | 16x |
| Delete 10K | ~20s | ~1s | 20x |

## 📊 Features

- **Progress Callbacks** - Track operation progress
- **Batch Sizing** - Configurable batch sizes
- **Error Handling** - Partial failure recovery
- **Provider Optimization** - SQL Server BulkCopy, PostgreSQL COPY
- **Memory Management** - Streaming for large datasets

## 📚 Learning Outcomes

- Performance optimization techniques
- Bulk operation patterns
- Provider-specific features
- Memory-efficient data processing
- Progress reporting

---

*Created: October 8, 2025*  
*Status: ⏳ Pending*
