# Phase 7.1: Relationship-Aware Repository Generation

## Overview
Enhance the repository generator to automatically create relationship-aware methods based on entity relationship mappings. Generate specialized CRUD operations that understand and handle related entities.

## Objectives
- Generate repository methods that automatically handle relationship loading
- Create specialized insert/update operations for entities with relationships
- Generate relationship validation methods
- Support relationship metadata in generated repositories

## Tasks

### 1. Relationship Metadata Detection
- [ ] Analyze entity classes for relationship attributes (OneToOne, OneToMany, ManyToOne, ManyToMany)
- [ ] Extract relationship metadata (type, cardinality, mappedBy, fetch type)
- [ ] Build relationship graph for each entity
- [ ] Detect circular dependencies in relationships

### 2. Enhanced Insert Operations
- [ ] Generate `AddAsync` methods that handle foreign key relationships
- [ ] Create `AddWithRelatedAsync` methods for saving entity with its related entities
- [ ] Implement validation for required relationships
- [ ] Generate methods to handle relationship ordering (parent before child)

### 3. Enhanced Update Operations
- [ ] Generate `UpdateAsync` methods that preserve relationship integrity
- [ ] Create `UpdateWithRelatedAsync` for updating entity and its relationships
- [ ] Implement change tracking for relationship collections
- [ ] Generate methods to detect and update modified relationships

### 4. Enhanced Delete Operations
- [ ] Generate `DeleteAsync` with relationship awareness
- [ ] Create methods to check for dependent entities before deletion
- [ ] Implement relationship constraint validation
- [ ] Generate methods to handle nullable vs required foreign keys

### 5. Relationship Query Methods
- [ ] Generate `GetByIdWithRelatedAsync` methods
- [ ] Create `FindWithRelatedAsync` methods
- [ ] Generate methods to load specific relationships
- [ ] Implement depth control for nested relationship loading

## Example Usage

```csharp
[Entity]
[Table("orders")]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    public string OrderNumber { get; set; }
    
    [ManyToOne]
    [JoinColumn("customer_id")]
    public Customer Customer { get; set; }
    
    [OneToMany(MappedBy = "Order")]
    public ICollection<OrderItem> Items { get; set; }
}

// Generated repository methods:
public interface IOrderRepository : IRepository<Order, int>
{
    // Automatically generated relationship-aware methods
    Task<Order?> GetByIdWithCustomerAsync(int id);
    Task<Order?> GetByIdWithItemsAsync(int id);
    Task<Order?> GetByIdWithAllRelationsAsync(int id);
    
    Task<Order> AddWithItemsAsync(Order order);
    Task UpdateWithItemsAsync(Order order);
    
    Task<bool> CanDeleteAsync(int id); // Check for dependent entities
}
```

## Generated Code Examples

### Insert with Relationship
```csharp
public async Task<Order> AddWithItemsAsync(Order order)
{
    // Validate required relationships
    if (order.Customer == null)
        throw new InvalidOperationException("Customer is required");
    
    // Start transaction
    using var transaction = await _connection.BeginTransactionAsync();
    try
    {
        // Insert parent entity
        var insertedOrder = await AddAsync(order);
        
        // Insert related items
        if (order.Items?.Any() == true)
        {
            foreach (var item in order.Items)
            {
                item.OrderId = insertedOrder.Id;
                // Use item repository to insert
            }
        }
        
        await transaction.CommitAsync();
        return insertedOrder;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### Query with Relationships
```csharp
public async Task<Order?> GetByIdWithAllRelationsAsync(int id)
{
    const string sql = @"
        SELECT o.*, c.*, i.*
        FROM orders o
        LEFT JOIN customers c ON o.customer_id = c.id
        LEFT JOIN order_items i ON i.order_id = o.id
        WHERE o.id = @id";
    
    var orderDict = new Dictionary<int, Order>();
    
    await _connection.QueryAsync<Order, Customer, OrderItem, Order>(
        sql,
        (order, customer, item) =>
        {
            if (!orderDict.TryGetValue(order.Id, out var existingOrder))
            {
                existingOrder = order;
                existingOrder.Items = new List<OrderItem>();
                orderDict.Add(order.Id, existingOrder);
            }
            
            if (customer != null)
                existingOrder.Customer = customer;
            
            if (item != null)
                existingOrder.Items.Add(item);
            
            return existingOrder;
        },
        new { id },
        splitOn: "id,id");
    
    return orderDict.Values.FirstOrDefault();
}
```

## Acceptance Criteria
- [ ] Repository generator detects all relationship types in entities
- [ ] Generated methods handle foreign key constraints correctly
- [ ] Relationship-aware CRUD operations maintain data integrity
- [ ] Query methods properly join and load related entities
- [ ] Transaction handling for multi-entity operations
- [ ] Validation for required relationships
- [ ] Performance optimization for relationship queries
- [ ] Comprehensive test coverage for all relationship scenarios

## Dependencies
- Phase 2.1: Relationship Mapping (completed)
- Phase 2.8: One-to-One Relationship Support (completed)
- Phase 3.1: Transaction Management (completed)

## Testing Requirements
- Unit tests for relationship metadata detection
- Integration tests for insert/update/delete with relationships
- Tests for circular relationship handling
- Performance tests for complex relationship graphs
- Edge case tests (null relationships, empty collections)

## Documentation
- API documentation for generated relationship methods
- Best practices guide for working with relationships
- Examples for each relationship type
- Performance optimization tips
