# Phase 7.2: Eager Loading Support

## Overview
Implement eager loading capabilities to efficiently fetch entity graphs in a single query. Support loading strategies and provide fine-grained control over what relationships to load.

## Objectives
- Implement fetch type attributes (Eager, Lazy)
- Generate optimized JOIN queries for eager loading
- Support selective relationship loading
- Implement N+1 query prevention strategies

## Tasks

### 1. Fetch Strategy Attributes
- [ ] Create `FetchAttribute` with FetchType enum (Eager, Lazy)
- [ ] Extend relationship attributes to support fetch configuration
- [ ] Implement fetch strategy metadata
- [ ] Generate fetch plan based on entity configuration

### 2. Query Builder Enhancement
- [ ] Implement automatic JOIN generation for eager relationships
- [ ] Create query optimization for multiple relationships
- [ ] Support LEFT JOIN, INNER JOIN based on nullable relationships
- [ ] Generate efficient split-on logic for Dapper

### 3. Include Method Generation
- [ ] Generate `Include<TProperty>` methods for explicit relationship loading
- [ ] Support chained includes for nested relationships
- [ ] Create `ThenInclude` methods for deep loading
- [ ] Implement include expression parsing

### 4. Batch Loading Strategy
- [ ] Implement batch loading for collections
- [ ] Generate optimized queries for multiple entity loads
- [ ] Support WHERE IN clause for batch fetching
- [ ] Create batch size configuration

### 5. Select Loading (Projection)
- [ ] Generate methods to load specific relationship properties
- [ ] Support projection to DTOs with relationships
- [ ] Implement partial entity loading
- [ ] Create optimized queries for selective loading

## Example Usage

```csharp
[Entity]
[Table("customers")]
public class Customer
{
    [Id]
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    // Eager load by default
    [OneToMany(MappedBy = "Customer")]
    [Fetch(FetchType.Eager)]
    public ICollection<Order> Orders { get; set; }
    
    // Lazy load by default
    [OneToOne(MappedBy = "Customer")]
    [Fetch(FetchType.Lazy)]
    public CustomerProfile Profile { get; set; }
}

// Generated repository methods:
public interface ICustomerRepository : IRepository<Customer, int>
{
    // Respects eager fetch configuration
    Task<Customer?> GetByIdAsync(int id); // Auto-loads Orders
    
    // Explicit include methods
    Task<Customer?> GetByIdAsync(int id, params Expression<Func<Customer, object>>[] includes);
    
    // Fluent API
    IQueryBuilder<Customer> Include(Expression<Func<Customer, object>> navigationProperty);
    IQueryBuilder<Customer> ThenInclude<TProperty>(Expression<Func<TProperty, object>> navigationProperty);
}

// Usage examples:
// Load with specific relationships
var customer = await repository.GetByIdAsync(1, 
    c => c.Orders, 
    c => c.Profile);

// Fluent API
var customers = await repository
    .Include(c => c.Orders)
        .ThenInclude<Order>(o => o.Items)
    .Include(c => c.Profile)
    .FindAllAsync();
```

## Generated Code Examples

### Eager Loading Query
```csharp
public async Task<Customer?> GetByIdAsync(int id)
{
    // Auto-generated based on Fetch attributes
    const string sql = @"
        SELECT 
            c.*,
            o.*
        FROM customers c
        LEFT JOIN orders o ON o.customer_id = c.id
        WHERE c.id = @id";
    
    var customerDict = new Dictionary<int, Customer>();
    
    await _connection.QueryAsync<Customer, Order, Customer>(
        sql,
        (customer, order) =>
        {
            if (!customerDict.TryGetValue(customer.Id, out var existingCustomer))
            {
                existingCustomer = customer;
                existingCustomer.Orders = new List<Order>();
                customerDict.Add(customer.Id, existingCustomer);
            }
            
            if (order != null)
                existingCustomer.Orders.Add(order);
            
            return existingCustomer;
        },
        new { id },
        splitOn: "id");
    
    return customerDict.Values.FirstOrDefault();
}
```

### Include Method Implementation
```csharp
public async Task<Customer?> GetByIdAsync(int id, params Expression<Func<Customer, object>>[] includes)
{
    var sqlBuilder = new StringBuilder("SELECT c.* FROM customers c WHERE c.id = @id");
    var joins = new List<string>();
    var splitOn = new List<string> { "id" };
    
    foreach (var include in includes)
    {
        var propertyName = GetPropertyName(include);
        
        if (propertyName == "Orders")
        {
            joins.Add("LEFT JOIN orders o ON o.customer_id = c.id");
            sqlBuilder.Append(", o.*");
            splitOn.Add("id");
        }
        else if (propertyName == "Profile")
        {
            joins.Add("LEFT JOIN customer_profiles p ON p.customer_id = c.id");
            sqlBuilder.Append(", p.*");
            splitOn.Add("id");
        }
    }
    
    foreach (var join in joins)
    {
        sqlBuilder.Append(" ").Append(join);
    }
    
    // Execute query with appropriate type mapping...
}
```

### Batch Loading
```csharp
public async Task<IEnumerable<Customer>> GetByIdsAsync(IEnumerable<int> ids)
{
    const string sql = @"
        SELECT 
            c.*,
            o.*
        FROM customers c
        LEFT JOIN orders o ON o.customer_id = c.id
        WHERE c.id IN @ids";
    
    var customerDict = new Dictionary<int, Customer>();
    
    await _connection.QueryAsync<Customer, Order, Customer>(
        sql,
        (customer, order) =>
        {
            if (!customerDict.TryGetValue(customer.Id, out var existingCustomer))
            {
                existingCustomer = customer;
                existingCustomer.Orders = new List<Order>();
                customerDict.Add(customer.Id, existingCustomer);
            }
            
            if (order != null)
                existingCustomer.Orders.Add(order);
            
            return existingCustomer;
        },
        new { ids = ids.ToArray() },
        splitOn: "id");
    
    return customerDict.Values;
}
```

## Acceptance Criteria
- [ ] Fetch type configuration works correctly
- [ ] Eager loading generates optimized JOIN queries
- [ ] Include methods work for all relationship types
- [ ] Nested includes load correctly
- [ ] Batch loading prevents N+1 queries
- [ ] Performance comparable to hand-written queries
- [ ] Memory efficient for large result sets
- [ ] Circular reference handling

## Dependencies
- Phase 7.1: Relationship-Aware Repository Generation
- Phase 2.1: Relationship Mapping

## Testing Requirements
- Unit tests for fetch strategy detection
- Integration tests for eager loading queries
- Performance tests comparing eager vs lazy loading
- Tests for complex relationship graphs
- N+1 query detection tests
- Memory usage tests with large datasets

## Performance Considerations
- Avoid cartesian product in multi-collection joins
- Implement query splitting for multiple collections
- Use batch loading where appropriate
- Provide configuration for max depth
- Monitor query complexity

## Documentation
- Guide on choosing fetch strategies
- Best practices for eager loading
- Performance optimization tips
- Examples for each loading strategy
- Troubleshooting guide for common issues
