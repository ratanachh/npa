# Phase 7.5: Orphan Removal

**Status**: ✅ COMPLETE

## Overview
Implement automatic orphan removal to delete child entities that are no longer referenced by their parent. This ensures referential integrity and prevents orphaned records in the database.

## Objectives
- ✅ Implement orphan removal configuration
- ✅ Generate automatic orphan detection logic
- ✅ Support orphan removal for all relationship types
- ✅ Ensure transactional consistency

## Tasks

### 1. Orphan Removal Configuration
- [x] Create `OrphanRemoval` property for relationship attributes
- [x] Integrate with OneToOne and OneToMany relationships
- [x] Define orphan detection rules
- [x] Implement configuration validation

### 2. Orphan Detection Logic
- [x] Generate methods to detect orphaned entities
- [x] Compare current vs previous relationship state
- [x] Identify removed entities from collections
- [x] Detect cleared single-valued relationships

### 3. Automatic Deletion
- [x] Generate delete operations for orphaned entities
- [x] Implement transactional orphan removal
- [x] Support cascading orphan removal
- [x] Handle nullable vs non-nullable relationships

### 4. Collection Management
- [x] Track collection modifications
- [x] Detect removed items from collections
- [x] Support collection clear operations
- [x] Handle collection replacement

### 5. Repository Enhancement
- [x] Enhance Update methods with orphan removal
- [x] Generate pre-update orphan detection
- [x] Implement post-update orphan cleanup
- [x] Support bulk orphan removal

## Implementation Summary

### Completed Features

1. **OneToMany Orphan Removal**
   - Automatic detection of removed items from collections
   - Deletion of orphaned entities when items are removed
   - Support for collection clear operations (null collection deletes all)
   - Comparison of existing vs current items to identify orphans

2. **OneToOne Orphan Removal**
   - Detection when relationship is cleared (set to null)
   - Detection when relationship is replaced (different entity)
   - Automatic deletion of orphaned entities
   - Support for both owner and inverse sides

3. **UpdateAsync Override**
   - Generated override of `UpdateAsync` when entities have orphan removal relationships
   - Loads existing entity to compare relationships
   - Detects and deletes orphaned entities before updating
   - Works independently of cascade operations

4. **Attribute Support**
   - `OneToManyAttribute.OrphanRemoval` property
   - `OneToOneAttribute.OrphanRemoval` property (new in Phase 7.5)
   - Proper extraction in RelationshipExtractor
   - Metadata provider support

### Generated Code Features

- **Orphan Removal Region**: Clear separation of orphan removal logic
- **Comprehensive Comments**: Detailed documentation in generated code
- **Error Handling**: Proper null checks and validation
- **Database Queries**: Efficient queries to load existing relationships
- **EntityManager Integration**: Uses EntityManager for consistent deletion

### Test Coverage

- ✅ OneToMany orphan removal tests
- ✅ OneToOne orphan removal tests
- ✅ Multiple relationships support
- ✅ Collection clear operations
- ✅ Null collection handling
- ✅ Verification that UpdateAsync is overridden correctly

## Example Usage

```csharp
[Entity]
[Table("orders")]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    public string OrderNumber { get; set; }
    
    // Orphan removal enabled - items removed from collection will be deleted
    [OneToMany(MappedBy = "Order")]
    [Cascade(CascadeType.All)]
    [OrphanRemoval(true)]
    public ICollection<OrderItem> Items { get; set; }
    
    // No orphan removal - address remains even if cleared
    [OneToOne]
    [JoinColumn("billing_address_id")]
    public Address BillingAddress { get; set; }
}

[Entity]
[Table("order_items")]
public class OrderItem
{
    [Id]
    public int Id { get; set; }
    
    [ManyToOne]
    public Order Order { get; set; }
    
    public int OrderId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
}

// Usage scenarios:
var order = await orderRepository.GetByIdAsync(1);

// Scenario 1: Remove item from collection
var itemToRemove = order.Items.First();
order.Items.Remove(itemToRemove);
await orderRepository.UpdateAsync(order);
// itemToRemove is automatically deleted from database

// Scenario 2: Clear collection
order.Items.Clear();
await orderRepository.UpdateAsync(order);
// All items are automatically deleted from database

// Scenario 3: Replace collection
order.Items = new List<OrderItem>
{
    new OrderItem { ProductName = "New Item", Quantity = 1 }
};
await orderRepository.UpdateAsync(order);
// Old items are deleted, new item is inserted
```

## Generated Code Examples

### Update with Orphan Removal
```csharp
public async Task UpdateAsync(Order order)
{
    using var transaction = await _connection.BeginTransactionAsync();
    try
    {
        // Load existing order with relationships
        var existing = await GetByIdWithItemsAsync(order.Id);
        if (existing == null)
            throw new EntityNotFoundException($"Order {order.Id} not found");
        
        // Update order
        const string updateOrderSql = @"
            UPDATE orders 
            SET order_number = @OrderNumber,
                updated_at = @UpdatedAt
            WHERE id = @Id";
        
        order.UpdatedAt = DateTime.UtcNow;
        await _connection.ExecuteAsync(updateOrderSql, order, transaction);
        
        // Handle Items with orphan removal
        if (order.Items != null)
        {
            var existingItems = existing.Items ?? new List<OrderItem>();
            var currentItems = order.Items;
            
            // Identify orphaned items (in existing but not in current)
            var existingIds = existingItems.Select(i => i.Id).ToHashSet();
            var currentIds = currentItems.Where(i => i.Id > 0).Select(i => i.Id).ToHashSet();
            var orphanedIds = existingIds.Except(currentIds).ToList();
            
            // Delete orphaned items
            if (orphanedIds.Any())
            {
                const string deleteOrphansSql = "DELETE FROM order_items WHERE id IN @ids";
                await _connection.ExecuteAsync(
                    deleteOrphansSql, 
                    new { ids = orphanedIds }, 
                    transaction);
            }
            
            // Update or insert current items
            var itemRepo = _repositoryFactory.GetRepository<OrderItem, int>();
            foreach (var item in currentItems)
            {
                item.OrderId = order.Id;
                if (item.Id > 0)
                    await itemRepo.UpdateAsync(item);
                else
                    await itemRepo.AddAsync(item);
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

### Collection Clear with Orphan Removal
```csharp
public async Task ClearItemsAsync(int orderId)
{
    using var transaction = await _connection.BeginTransactionAsync();
    try
    {
        // With orphan removal, clear means delete all items
        const string sql = "DELETE FROM order_items WHERE order_id = @orderId";
        await _connection.ExecuteAsync(sql, new { orderId }, transaction);
        
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### OneToOne Orphan Removal
```csharp
[Entity]
[Table("users")]
public class User
{
    [Id]
    public int Id { get; set; }
    
    // Profile will be deleted if cleared or changed
    [OneToOne(MappedBy = "User")]
    [Cascade(CascadeType.All)]
    [OrphanRemoval(true)]
    public UserProfile Profile { get; set; }
}

// Generated update method:
public async Task UpdateAsync(User user)
{
    using var transaction = await _connection.BeginTransactionAsync();
    try
    {
        // Load existing user
        var existing = await GetByIdWithProfileAsync(user.Id);
        
        // Update user
        const string updateUserSql = @"
            UPDATE users 
            SET username = @Username,
                email = @Email
            WHERE id = @Id";
        
        await _connection.ExecuteAsync(updateUserSql, user, transaction);
        
        // Handle Profile with orphan removal
        if (existing.Profile != null && user.Profile == null)
        {
            // Profile was cleared - delete it (orphan removal)
            const string deleteProfileSql = "DELETE FROM user_profiles WHERE user_id = @userId";
            await _connection.ExecuteAsync(
                deleteProfileSql, 
                new { userId = user.Id }, 
                transaction);
        }
        else if (existing.Profile != null && 
                 user.Profile != null && 
                 existing.Profile.Id != user.Profile.Id)
        {
            // Profile was replaced - delete old one (orphan removal)
            const string deleteOldProfileSql = "DELETE FROM user_profiles WHERE id = @id";
            await _connection.ExecuteAsync(
                deleteOldProfileSql, 
                new { id = existing.Profile.Id }, 
                transaction);
            
            // Insert or update new profile
            var profileRepo = _repositoryFactory.GetRepository<UserProfile, int>();
            if (user.Profile.Id > 0)
                await profileRepo.UpdateAsync(user.Profile);
            else
                await profileRepo.AddAsync(user.Profile);
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

### Orphan Removal Attribute
```csharp
public class OrphanRemovalAttribute : Attribute
{
    public bool Enabled { get; }
    
    public OrphanRemovalAttribute(bool enabled = true)
    {
        Enabled = enabled;
    }
}

// Usage in relationship attributes:
[AttributeUsage(AttributeTargets.Property)]
public class OneToManyAttribute : Attribute
{
    public string? MappedBy { get; set; }
    public FetchType Fetch { get; set; } = FetchType.Lazy;
    public bool OrphanRemoval { get; set; } = false; // Integrated property
}
```

## Acceptance Criteria
- [x] Orphan removal configuration properly detected
- [x] Orphaned entities automatically deleted
- [x] Collection modifications tracked correctly
- [x] Single-valued relationship changes handled
- [x] Transactional integrity maintained
- [x] Performance optimized for bulk operations
- [x] No accidental deletions (false positives)
- [x] Works with cascade operations

**All acceptance criteria met!** ✅

## Dependencies
- Phase 7.1: Relationship-Aware Repository Generation
- Phase 7.3: Cascade Operations Enhancement
- Phase 7.4: Bidirectional Relationship Management

## Testing Requirements
- Unit tests for orphan detection logic
- Tests for collection remove operations
- Tests for collection clear operations
- Tests for collection replacement
- Tests for OneToOne relationship changes
- Integration tests with repository operations
- Tests for transactional rollback scenarios
- Performance tests for bulk orphan removal

## Performance Considerations
- Batch delete operations for multiple orphans
- Minimize database round trips
- Use efficient queries for orphan detection
- Consider soft delete for audit trails
- Optimize for large collections

## Safety Considerations
- Validate orphan removal configuration
- Prevent accidental data loss
- Log orphan removal operations
- Support soft delete alternative
- Provide pre-delete hooks

## Documentation
- Complete guide on orphan removal
- When to use orphan removal
- Differences from cascade delete
- Performance implications
- Common pitfalls and solutions
- Migration guide for existing data
