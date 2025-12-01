# Troubleshooting Guide

## Overview

This guide helps you diagnose and resolve common issues with relationship query methods.

## Table of Contents

1. [Common Issues](#common-issues)
2. [Error Messages](#error-messages)
3. [Performance Issues](#performance-issues)
4. [Code Generation Issues](#code-generation-issues)
5. [Database Issues](#database-issues)

---

## Common Issues

### Issue 1: Method Not Generated

**Symptom:** Expected relationship query method is not present in the generated repository.

**Possible Causes:**
1. Relationship not properly defined with `[ManyToOne]` or `[OneToMany]` attributes
2. Missing `[JoinColumn]` attribute for ManyToOne relationships
3. Missing `MappedBy` property for OneToMany relationships
4. Entity metadata not available during generation

**Solutions:**

**Check relationship definition:**
```csharp
// ✅ Correct ManyToOne definition
[ManyToOne]
[JoinColumn("customer_id")]
public Customer? Customer { get; set; }

// ❌ Missing JoinColumn
[ManyToOne]
public Customer? Customer { get; set; } // FK column name cannot be determined
```

**Check OneToMany definition:**
```csharp
// ✅ Correct OneToMany definition
[OneToMany(MappedBy = "Customer")]
public ICollection<Order> Orders { get; set; }

// ❌ Missing MappedBy
[OneToMany]
public ICollection<Order> Orders { get; set; } // Cannot determine inverse relationship
```

**Verify entity metadata:**
- Ensure `[Entity]` and `[Table]` attributes are present
- Ensure `[Id]` attribute is on primary key property
- Check that related entity types are accessible

---

### Issue 2: SQL Injection Concerns

**Symptom:** Concerned about security of `orderBy` parameter.

**Resolution:**
The `orderBy` parameter is validated against a compile-time property-to-column mapping dictionary. Invalid property names fall back to the default column name (primary key), preventing SQL injection.

**Verification:**
```csharp
// ✅ Safe: Property name validated
var orders = await repository.FindByCustomerIdAsync(
    customerId,
    skip: 0,
    take: 10,
    orderBy: "TotalAmount", // Validated against property map
    ascending: false
);

// ✅ Safe: Invalid property name uses default
var orders = await repository.FindByCustomerIdAsync(
    customerId,
    skip: 0,
    take: 10,
    orderBy: "InvalidProperty", // Falls back to primary key column
    ascending: false
);
```

---

### Issue 3: Null Reference Exceptions

**Symptom:** `NullReferenceException` when calling relationship query methods.

**Possible Causes:**
1. Repository dependencies not initialized
2. Connection not open
3. EntityManager not properly configured

**Solutions:**

**Check repository initialization:**
```csharp
// ✅ Proper initialization
var repository = new OrderRepositoryImplementation(
    connection,      // IDbConnection
    entityManager,   // IEntityManager
    metadataProvider // IMetadataProvider
);

// Ensure connection is open
await connection.OpenAsync();
```

**Check EntityManager setup:**
```csharp
var metadataProvider = new MetadataProvider();
var databaseProvider = new PostgreSqlProvider();
var entityManager = new EntityManager(
    connection,
    metadataProvider,
    databaseProvider,
    logger
);
```

---

### Issue 4: Incorrect Results from Property-Based Queries

**Symptom:** `FindByCustomerNameAsync` returns unexpected results or no results.

**Possible Causes:**
1. Case sensitivity in database
2. Column name mismatch
3. JOIN condition incorrect

**Solutions:**

**Check column names:**
```csharp
// Verify [Column] attribute matches database
[Column("name")]
public string Name { get; set; } // Column name in database must be "name"
```

**Check case sensitivity:**
```sql
-- PostgreSQL: Case-sensitive by default
SELECT * FROM customers WHERE name = 'John Doe'; -- Exact match required

-- Use ILIKE for case-insensitive (would require custom query)
SELECT * FROM customers WHERE name ILIKE 'john doe';
```

**Verify JOIN conditions:**
- Check that `[JoinColumn]` attribute specifies correct FK column name
- Verify FK column exists in database
- Ensure related entity's primary key column name matches

---

## Error Messages

### "Generated code for {Type} not found"

**Cause:** The repository implementation class was not generated.

**Solution:**
1. Rebuild the project to trigger code generation
2. Check that `[Repository]` attribute is present on the interface
3. Verify the interface extends `IRepository<TEntity, TKey>`
4. Check build output for generator errors

---

### "Repository type {Type} not found in assembly"

**Cause:** The repository class was generated but not found in the compiled assembly.

**Solution:**
1. Check the namespace - generated code is in `NPA.Generators` namespace
2. Verify the class name matches expected pattern: `{InterfaceName}Implementation`
3. Ensure all dependencies are included in compilation references

---

### "Compilation failed: Unterminated string literal"

**Cause:** Generated SQL contains syntax errors, often due to special characters in table/column names.

**Solution:**
1. Check table and column names for special characters
2. Verify `[Table]` and `[Column]` attribute values
3. Ensure string literals in generated code are properly escaped

**Workaround:**
- Use simpler table/column names without special characters
- Quote identifiers in database if needed

---

### "Unknown constructor parameter type: {Type}"

**Cause:** Repository constructor expects a dependency type that's not provided.

**Solution:**
Ensure all required dependencies are provided when instantiating repository:
```csharp
// Required dependencies
var repository = new OrderRepositoryImplementation(
    connection,        // IDbConnection
    entityManager,     // IEntityManager
    metadataProvider   // IMetadataProvider
);
```

---

## Performance Issues

### Slow Query Performance

**Symptom:** Relationship queries are slower than expected.

**Diagnosis Steps:**

1. **Check for missing indexes:**
```sql
-- Check if index exists
SELECT * FROM pg_indexes WHERE tablename = 'orders' AND indexname LIKE '%customer_id%';

-- Create index if missing
CREATE INDEX idx_orders_customer_id ON orders(customer_id);
```

2. **Review query execution plan:**
```sql
EXPLAIN ANALYZE 
SELECT * FROM orders WHERE customer_id = 123;
```

3. **Check result set size:**
```csharp
// Use pagination for large result sets
var orders = await repository.FindByCustomerIdAsync(
    customerId,
    skip: 0,
    take: 50 // Limit result set size
);
```

**Common Fixes:**
- Add missing indexes on foreign keys
- Use pagination for large datasets
- Use COUNT methods instead of materializing collections
- Cache frequently accessed queries

---

### Memory Issues

**Symptom:** High memory usage when querying relationships.

**Causes and Solutions:**

**Large result sets:**
```csharp
// ❌ Problem: Loading all records into memory
var allOrders = await repository.FindByCustomerIdAsync(customerId);

// ✅ Solution: Use pagination
var orders = await repository.FindByCustomerIdAsync(
    customerId,
    skip: 0,
    take: 50
);
```

**N+1 query pattern:**
```csharp
// ❌ Problem: Multiple queries in loop
foreach (var customer in customers)
{
    var orders = await repository.FindByCustomerIdAsync(customer.Id);
}

// ✅ Solution: Batch query or use grouping
var allOrders = await repository.GetAllAsync();
var ordersByCustomer = allOrders
    .GroupBy(o => o.Customer.Id)
    .ToDictionary(g => g.Key, g => g.ToList());
```

---

## Code Generation Issues

### Methods Not Appearing in Interface

**Symptom:** Generated methods exist in implementation but not in interface.

**Cause:** Interface generation runs separately from implementation generation.

**Solution:**
1. Ensure `GenerateRelationshipAwareMethodSignatures` is called
2. Rebuild project to regenerate interfaces
3. Check that interface is marked as `partial`

```csharp
// ✅ Interface must be partial
[Repository]
public partial interface IOrderRepository : IRepository<Order, int>
{
}
```

---

### Incorrect SQL Generation

**Symptom:** Generated SQL queries have syntax errors or incorrect logic.

**Common Issues:**

**Column name issues:**
```csharp
// Ensure [Column] attribute specifies correct database column name
[Column("order_date")] // Must match database column name
public DateTime OrderDate { get; set; }
```

**FK column name issues:**
```csharp
// Ensure [JoinColumn] specifies correct FK column
[ManyToOne]
[JoinColumn("customer_id")] // Must match database FK column name
public Customer Customer { get; set; }
```

**Verify generated SQL:**
- Check generated code in `obj/Debug/net8.0/generated/` folder
- Look for SQL syntax errors
- Verify column and table names match database schema

---

## Database Issues

### Connection Errors

**Symptom:** Database connection errors when executing queries.

**Solutions:**

**Check connection string:**
```csharp
var connectionString = "Host=localhost;Database=npadb;Username=user;Password=pass";
var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync(); // Verify connection opens successfully
```

**Check connection state:**
```csharp
if (connection.State != ConnectionState.Open)
{
    await connection.OpenAsync();
}
```

---

### Transaction Issues

**Symptom:** Queries not seeing uncommitted changes in transactions.

**Solution:**
Ensure queries use the same connection and transaction:
```csharp
using var transaction = connection.BeginTransaction();
try
{
    await repository.AddAsync(order);
    // Queries in same transaction see uncommitted changes
    var orders = await repository.FindByCustomerIdAsync(customerId);
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
}
```

---

### Schema Mismatches

**Symptom:** Queries fail with "column does not exist" errors.

**Solutions:**

**Verify table names:**
```csharp
[Table("orders")] // Must match database table name (case-sensitive in PostgreSQL)
public class Order
{
}
```

**Verify column names:**
```csharp
[Column("customer_id")] // Must match database column name
public int CustomerId { get; set; }
```

**Check database schema:**
```sql
-- PostgreSQL
\d orders

-- SQL Server
EXEC sp_columns 'orders';
```

---

## Debugging Tips

### Enable SQL Logging

**Log generated SQL queries:**

```csharp
public class LoggingOrderRepository
{
    private readonly IOrderRepository _repository;
    private readonly ILogger<LoggingOrderRepository> _logger;
    
    public async Task<IEnumerable<Order>> FindByCustomerIdAsync(int customerId)
    {
        _logger.LogDebug("Executing FindByCustomerIdAsync for customer {CustomerId}", customerId);
        
        var result = await _repository.FindByCustomerIdAsync(customerId);
        
        _logger.LogDebug("Found {Count} orders for customer {CustomerId}", 
            result.Count(), customerId);
        
        return result;
    }
}
```

---

### Inspect Generated Code

**View generated repository code:**

1. Build the project
2. Navigate to `obj/Debug/net8.0/generated/` folder
3. Find files matching `*RepositoryImplementation.g.cs`
4. Review generated SQL and method implementations

---

### Test Queries Directly

**Test SQL queries directly in database:**

```sql
-- Test the SQL that would be generated
SELECT * FROM orders 
WHERE customer_id = 123 
ORDER BY id
OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY;
```

---

## Common Patterns for Resolution

### Pattern 1: Verify Relationship Metadata

```csharp
// Check if relationship is detected
var relationships = metadataProvider.GetRelationships(typeof(Order));
var customerRelationship = relationships.FirstOrDefault(r => r.PropertyName == "Customer");
if (customerRelationship == null)
{
    // Relationship not detected - check attributes
}
```

---

### Pattern 2: Verify Table/Column Names

```csharp
// Check table name
var tableName = metadataProvider.GetTableName(typeof(Order));
Console.WriteLine($"Table name: {tableName}");

// Check column names
var properties = metadataProvider.GetProperties(typeof(Order));
foreach (var prop in properties)
{
    Console.WriteLine($"Property: {prop.Name}, Column: {prop.ColumnName}");
}
```

---

### Pattern 3: Test with Simple Query

**Start with simplest query and build up:**

```csharp
// 1. Test basic query
var orders = await repository.GetAllAsync();

// 2. Test FK query
var orders = await repository.FindByCustomerIdAsync(123);

// 3. Test property-based query
var orders = await repository.FindByCustomerNameAsync("John Doe");

// 4. Test pagination
var orders = await repository.FindByCustomerIdAsync(123, skip: 0, take: 10);
```

---

## Getting Help

### Information to Provide

When reporting issues, include:

1. **Entity definitions** (with all attributes)
2. **Repository interface definition**
3. **Generated code** (if available)
4. **Error messages** (full stack trace)
5. **Database schema** (table and column definitions)
6. **Query execution plan** (if performance issue)

### Logging

Enable detailed logging:

```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

---

## See Also

- [API Reference](API_REFERENCE.md)
- [Query Optimization Guide](OPTIMIZATION_GUIDE.md)
- [Relationship Query Patterns and Best Practices](PATTERNS_AND_BEST_PRACTICES.md)
- [Performance Best Practices](PERFORMANCE_GUIDE.md)
- [Examples](EXAMPLES.md)

