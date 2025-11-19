# Phase 7.1: Relationship-Aware Repository Generation

## Overview
Enhance the repository generator to automatically create relationship-aware methods based on entity relationship mappings. Generate specialized CRUD operations that understand and handle related entities.

## Objectives
- Generate repository methods that automatically handle relationship loading
- Create specialized insert/update operations for entities with relationships
- Generate relationship validation methods
- Support relationship metadata in generated repositories

## Tasks

### 1. Relationship Metadata Detection ✅ COMPLETED
- [x] Analyze entity classes for relationship attributes (OneToOne, OneToMany, ManyToOne, ManyToMany)
- [x] Extract relationship metadata (type, cardinality, mappedBy, fetch type)
- [x] Build relationship graph for each entity
- [x] Detect owner vs inverse side of relationships

### 2. Enhanced Insert Operations (Deferred to Phase 7.3)
- [ ] Generate `AddAsync` methods that handle foreign key relationships
- [ ] Create `AddWithRelatedAsync` methods for saving entity with its related entities
- [ ] Implement validation for required relationships
- [ ] Generate methods to handle relationship ordering (parent before child)

### 3. Enhanced Update Operations (Deferred to Phase 7.4)
- [ ] Generate `UpdateAsync` methods that preserve relationship integrity
- [ ] Create `UpdateWithRelatedAsync` for updating entity and its relationships
- [ ] Implement change tracking for relationship collections
- [ ] Generate methods to detect and update modified relationships

### 4. Enhanced Delete Operations (Deferred to Phase 7.5)
- [ ] Generate `DeleteAsync` with relationship awareness
- [ ] Create methods to check for dependent entities before deletion
- [ ] Implement relationship constraint validation
- [ ] Generate methods to handle nullable vs required foreign keys

### 5. Relationship Query Methods ✅ COMPLETED (Basic)
- [x] Generate `GetByIdWith{Property}Async` methods for eager relationships
- [x] Create `Load{Property}Async` methods for lazy relationships
- [x] Generate methods to load specific relationships
- [x] Skip inverse side of relationships (no duplicate methods)

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
- [x] Repository generator detects all relationship types in entities
- [x] Generated methods handle foreign key constraints correctly (basic SQL generation)
- [x] Relationship-aware query methods load related entities
- [x] Query methods properly join and load related entities (LEFT JOIN with Dapper multi-mapping)
- [x] Validation for owner vs inverse side (skips inverse side)
- [x] Support for both eager and lazy fetch types
- [x] Basic test coverage for relationship query scenarios (Phase7Demo)
- [ ] Transaction handling for multi-entity operations (deferred to Phase 7.3)
- [ ] Insert/Update/Delete with relationships (deferred to Phase 7.3-7.5)
- [ ] Performance optimization for relationship queries (deferred to Phase 7.2)
- [ ] Comprehensive test coverage for all relationship scenarios (ongoing)

**Status**: ✅ **Phase 7.1 COMPLETED** (November 19, 2025)

**What Was Implemented**:
1. RelationshipModels.cs - Lightweight metadata classes (RelationshipMetadata, JoinColumnInfo, JoinTableInfo)
2. RelationshipExtractor.cs - Complete relationship extraction logic from entity properties
3. RepositoryGenerator enhancements - ExtractRelationships() and GenerateRelationshipAwareMethods()
4. Generated methods: GetByIdWith{Property}Async for eager relationships
5. Generated methods: Load{Property}Async for lazy relationships
6. SQL generation with LEFT JOIN and Dapper multi-mapping
7. Test project Phase7Demo validating implementation

**Deferred to Future Phases**:
- Phase 7.2: Enhanced query methods (GetAllWithRelationships, Include() fluent API)
- Phase 7.3: Insert/Update with cascade
- Phase 7.4: Bidirectional relationship management
- Phase 7.5: Delete with orphan removal
- Phase 7.6: Advanced relationship queries

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
