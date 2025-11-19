# Phase 7.2: Eager Loading Support

## Overview
Implement eager loading capabilities to efficiently fetch entity graphs in a single query. Support loading strategies and provide fine-grained control over what relationships to load.

## Objectives
- Implement fetch type attributes (Eager, Lazy)
- Generate optimized JOIN queries for eager loading
- Support selective relationship loading
- Implement N+1 query prevention strategies

## Tasks

### 1. Fetch Strategy Attributes ✅ COMPLETED
- [x] FetchType enum already exists (Eager, Lazy) in NPA.Core.Annotations
- [x] Relationship attributes support fetch configuration
- [x] Fetch strategy metadata extracted
- [x] Fetch plan generated based on entity configuration

### 2. Query Builder Enhancement ✅ COMPLETED (Basic)
- [x] Automatic JOIN generation for eager relationships (single relationships)
- [x] Query optimization for simple relationships (ManyToOne, OneToOne)
- [x] LEFT JOIN support based on nullable relationships
- [x] Efficient split-on logic for Dapper multi-mapping
- [ ] Advanced: Complex multi-collection joins (deferred - cartesian product issue)

### 3. Include Method Generation (Deferred to Phase 7.3)
- [ ] Generate `Include<TProperty>` methods for explicit relationship loading
- [ ] Support chained includes for nested relationships
- [ ] Create `ThenInclude` methods for deep loading
- [ ] Implement include expression parsing

### 4. Batch Loading Strategy ✅ COMPLETED
- [x] Implement batch loading for collections via GetByIdsAsync
- [x] Generate optimized queries for multiple entity loads
- [x] Support WHERE IN clause for batch fetching
- [x] Batch loading prevents N+1 queries

### 5. Select Loading (Projection) (Deferred to Phase 7.4)
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
- [x] Fetch type configuration works correctly (Eager/Lazy attributes)
- [x] Eager loading generates optimized JOIN queries for single relationships
- [x] GetByIdAsync() override loads eager relationships automatically
- [x] GetAllAsync() override prepares for batch loading
- [x] GetByIdsAsync() batch loading prevents N+1 queries
- [x] Performance comparable to hand-written queries for simple cases
- [x] Memory efficient for single relationships
- [ ] Include methods work for all relationship types (deferred to Phase 7.3)
- [ ] Nested includes load correctly (deferred to Phase 7.3)
- [ ] Complex multi-collection eager loading (deferred - cartesian product)
- [ ] Circular reference handling (future enhancement)

**Status**: ✅ **Phase 7.2 COMPLETED (Basic)** (November 19, 2025)

**What Was Implemented**:
1. Automatic eager loading for FetchType.Eager relationships
2. Override GetByIdAsync() to auto-load eager relationships with LEFT JOIN
3. Override GetAllAsync() foundation for batch loading
4. New GetByIdsAsync() method for batch loading with WHERE IN
5. Smart query generation for single relationships (ManyToOne, OneToOne)
6. Separate queries for complex cases to avoid cartesian product

**Implementation Details**:
- Detects `FetchType.Eager` relationships at generation time
- Generates override methods that call base + load relationships
- Simple case: Single LEFT JOIN for one eager ManyToOne/OneToOne
- Complex case: Separate queries per relationship to avoid cartesian product
- Batch loading: Uses WHERE IN with dictionary grouping for efficiency

**Known Limitations**:
- Multiple collection eager loads use separate queries (not single JOIN)
- No Include() fluent API yet (deferred to Phase 7.3)
- No nested/deep includes (deferred to Phase 7.3)
- Table names use entity names (should use [Table] attribute)
- Foreign keys inferred from convention (should read actual schema)

**Deferred Features**:
- Phase 7.3: Include() fluent API for explicit loading
- Phase 7.4: Advanced multi-collection optimization
- Phase 7.5: Projection/DTO support
- Phase 7.6: Deep loading (ThenInclude)

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
