# Phase 7.3: Cascade Operations Enhancement

## Overview
Enhance cascade operations to properly handle relationship lifecycle events. Implement comprehensive cascade strategies for persist, update, remove, merge, and refresh operations.

## Objectives
- Implement cascade type attributes and strategies
- Generate cascade-aware repository methods
- Support cascade configuration per relationship
- Ensure transactional integrity for cascaded operations

## Tasks

### 1. Cascade Type Attribute
- [ ] Create `CascadeAttribute` with CascadeType enum
- [ ] Support multiple cascade types per relationship
- [ ] Define cascade types: Persist, Update, Remove, Merge, Refresh, All, None
- [ ] Integrate cascade metadata with relationship attributes

### 2. Cascade Persist Implementation
- [ ] Generate methods to cascade insert operations
- [ ] Handle transient entity graph persistence
- [ ] Implement order of operations (parent before children)
- [ ] Support bulk cascade inserts

### 3. Cascade Update Implementation
- [ ] Generate methods to cascade update operations
- [ ] Detect and update modified relationship graphs
- [ ] Handle partial updates in relationships
- [ ] Support selective cascade updates

### 4. Cascade Remove Implementation
- [ ] Generate methods to cascade delete operations
- [ ] Implement proper deletion order (children before parent)
- [ ] Handle nullable relationships during cascade delete
- [ ] Support soft delete cascading

### 5. Cascade Merge Implementation
- [ ] Generate methods to merge detached entity graphs
- [ ] Handle conflicting changes in relationships
- [ ] Implement merge strategy configuration
- [ ] Support partial graph merging

## Example Usage

```csharp
[Entity]
[Table("orders")]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    // Cascade all operations to items
    [OneToMany(MappedBy = "Order")]
    [Cascade(CascadeType.All)]
    public ICollection<OrderItem> Items { get; set; }
    
    // Only cascade persist, not delete
    [ManyToOne]
    [JoinColumn("customer_id")]
    [Cascade(CascadeType.Persist)]
    public Customer Customer { get; set; }
    
    // No cascade operations
    [ManyToOne]
    [JoinColumn("shipping_address_id")]
    [Cascade(CascadeType.None)]
    public Address ShippingAddress { get; set; }
}

// Usage:
var order = new Order
{
    OrderNumber = "ORD-001",
    Customer = new Customer { Name = "John Doe" }, // Will be cascaded
    Items = new List<OrderItem>
    {
        new OrderItem { ProductId = 1, Quantity = 2 },
        new OrderItem { ProductId = 2, Quantity = 1 }
    } // Will be cascaded
};

// Single call cascades to all configured relationships
await orderRepository.AddAsync(order);
```

## Generated Code Examples

### Cascade Insert
```csharp
public async Task<Order> AddAsync(Order order)
{
    using var transaction = await _connection.BeginTransactionAsync();
    try
    {
        // Check cascade configuration for Customer (CascadeType.Persist)
        if (order.Customer != null && order.Customer.Id == 0)
        {
            // Cascade insert customer
            var customerRepo = _repositoryFactory.GetRepository<Customer, int>();
            order.Customer = await customerRepo.AddAsync(order.Customer);
            order.CustomerId = order.Customer.Id;
        }
        
        // Insert the order
        const string sql = @"
            INSERT INTO orders (order_number, customer_id, shipping_address_id)
            VALUES (@OrderNumber, @CustomerId, @ShippingAddressId)
            RETURNING *";
        
        var insertedOrder = await _connection.QuerySingleAsync<Order>(sql, order, transaction);
        
        // Check cascade configuration for Items (CascadeType.All)
        if (order.Items?.Any() == true)
        {
            var itemRepo = _repositoryFactory.GetRepository<OrderItem, int>();
            foreach (var item in order.Items)
            {
                item.OrderId = insertedOrder.Id;
                await itemRepo.AddAsync(item); // Cascade insert
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

### Cascade Update
```csharp
public async Task UpdateAsync(Order order)
{
    using var transaction = await _connection.BeginTransactionAsync();
    try
    {
        // Update the order
        const string sql = @"
            UPDATE orders 
            SET order_number = @OrderNumber,
                customer_id = @CustomerId,
                shipping_address_id = @ShippingAddressId,
                updated_at = @UpdatedAt
            WHERE id = @Id";
        
        order.UpdatedAt = DateTime.UtcNow;
        await _connection.ExecuteAsync(sql, order, transaction);
        
        // Check cascade configuration for Items (CascadeType.All includes Update)
        if (order.Items?.Any() == true)
        {
            var itemRepo = _repositoryFactory.GetRepository<OrderItem, int>();
            
            // Get existing items
            var existingItems = await itemRepo.FindByOrderIdAsync(order.Id);
            var existingIds = existingItems.Select(i => i.Id).ToHashSet();
            var currentIds = order.Items.Where(i => i.Id > 0).Select(i => i.Id).ToHashSet();
            
            // Delete removed items
            var removedIds = existingIds.Except(currentIds);
            foreach (var removedId in removedIds)
            {
                await itemRepo.DeleteAsync(removedId); // Cascade delete
            }
            
            // Update or insert items
            foreach (var item in order.Items)
            {
                item.OrderId = order.Id;
                if (item.Id > 0)
                    await itemRepo.UpdateAsync(item); // Cascade update
                else
                    await itemRepo.AddAsync(item); // Cascade insert
            }
        }
        
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### Cascade Delete
```csharp
public async Task DeleteAsync(int id)
{
    using var transaction = await _connection.BeginTransactionAsync();
    try
    {
        // Load order to check cascade configuration
        var order = await GetByIdAsync(id);
        if (order == null)
            throw new EntityNotFoundException($"Order with id {id} not found");
        
        // Check cascade configuration for Items (CascadeType.All includes Remove)
        if (order.Items?.Any() == true)
        {
            var itemRepo = _repositoryFactory.GetRepository<OrderItem, int>();
            foreach (var item in order.Items)
            {
                await itemRepo.DeleteAsync(item.Id); // Cascade delete
            }
        }
        
        // Delete the order
        const string sql = "DELETE FROM orders WHERE id = @id";
        await _connection.ExecuteAsync(sql, new { id }, transaction);
        
        // Note: Customer is not deleted (CascadeType.Persist only)
        // Note: ShippingAddress is not deleted (CascadeType.None)
        
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

## Cascade Type Definitions

```csharp
[Flags]
public enum CascadeType
{
    None = 0,
    Persist = 1 << 0,    // Cascade insert operations
    Update = 1 << 1,     // Cascade update operations
    Remove = 1 << 2,     // Cascade delete operations
    Merge = 1 << 3,      // Cascade merge operations
    Refresh = 1 << 4,    // Cascade refresh operations
    All = Persist | Update | Remove | Merge | Refresh
}

[AttributeUsage(AttributeTargets.Property)]
public class CascadeAttribute : Attribute
{
    public CascadeType Type { get; }
    
    public CascadeAttribute(CascadeType type)
    {
        Type = type;
    }
}
```

## Acceptance Criteria
- [ ] All cascade types properly implemented
- [ ] Cascade configuration respected in generated code
- [ ] Transactional integrity maintained
- [ ] Proper operation ordering (parent/child)
- [ ] Circular cascade prevention
- [ ] Performance optimization for cascade operations
- [ ] Error handling and rollback on failures
- [ ] Support for soft deletes

## Dependencies
- Phase 7.1: Relationship-Aware Repository Generation
- Phase 3.1: Transaction Management
- Phase 3.2: Cascade Operations (basic)

## Testing Requirements
- Unit tests for each cascade type
- Integration tests for complex cascade scenarios
- Tests for circular relationship cascades
- Performance tests for deep cascades
- Transaction rollback tests
- Edge case tests (null collections, empty collections)

## Performance Considerations
- Batch operations where possible
- Optimize cascade depth
- Implement cascade cycle detection
- Provide cascade depth limits
- Monitor transaction size

## Documentation
- Complete guide on cascade strategies
- Decision matrix for choosing cascade types
- Best practices for cascade configuration
- Performance implications of cascades
- Examples for common scenarios
