# Phase 7 Demo: Advanced Relationship Management

This comprehensive demo showcases all Phase 7 features of NPA's advanced relationship management capabilities.

## Overview

Phase 7 introduces powerful relationship management features that rival and exceed traditional ORMs like Entity Framework and Hibernate. This demo project demonstrates all implemented features in a single, easy-to-understand application.

## Features Demonstrated

### ✅ Phase 7.1: Relationship-Aware Repository Generation (COMPLETE)

The generator automatically creates relationship-specific methods based on entity mappings:

**Generated Methods:**
- `GetByIdWith{Property}Async(TKey id)` - Eager load specific relationships with SQL JOINs
- `Load{Property}Async(TEntity entity)` - Lazy load relationships on demand

**Example:**
```csharp
// Generated from Order entity with [ManyToOne] to Customer
public async Task<Order?> GetByIdWithCustomerAsync(int id)
{
    // Uses LEFT JOIN and Dapper multi-mapping
    var sql = @"SELECT e.*, r.* FROM orders e LEFT JOIN customers r ON e.customer_id = r.Id WHERE e.Id = @Id";
    // ... Dapper QueryAsync with multi-mapping
}
```

**Key Features:**
- ✓ Automatic owner vs inverse side detection
- ✓ SQL JOIN generation with proper foreign keys
- ✓ Dapper multi-mapping for efficient hydration
- ✓ Support for OneToOne, ManyToOne, OneToMany relationships

### ✅ Phase 7.2: Eager Loading Support (BASIC COMPLETE)

Automatic eager loading based on `FetchType.Eager` configuration:

**Generated Overrides:**
- `override GetByIdAsync(TKey id)` - Automatically loads eager relationships
- `GetByIdsAsync(IEnumerable<TKey> ids)` - Batch loading to prevent N+1 queries

**Example:**
```csharp
// Order entity with FetchType.Eager on Customer property
// Generated override automatically includes Customer
public override async Task<Order?> GetByIdAsync(int id)
{
    var sql = @"SELECT e.*, r.* FROM orders e LEFT JOIN customers r ON e.customer_id = r.Id WHERE e.Id = @Id";
    // Customer is automatically populated
}
```

**Key Features:**
- ✓ FetchType.Eager attribute support
- ✓ Smart query generation (single JOIN or separate queries)
- ✓ Batch loading with `WHERE IN` for collections
- ✓ Prevention of N+1 query problems

### ✅ Phase 7.3: Cascade Operations Enhancement (COMPLETE)

Full cascade operation support for all relationship lifecycle events:

**Generated Methods:**
- `AddWithCascadeAsync(TEntity entity)` - Cascade persist with correct ordering
- `UpdateWithCascadeAsync(TEntity entity)` - Cascade merge with orphan removal
- `DeleteWithCascadeAsync(TKey id)` - Cascade delete with FK safety

**Cascade Strategies:**
- **Parent-First**: ManyToOne/OneToOne relationships persisted before main entity
- **Child-After**: OneToMany collections persisted after parent (FK dependency)
- **Children-First**: Collections deleted before parent (FK constraint safety)

**Example:**
```csharp
// Customer with CascadeType.All on Orders
public async Task<Customer> AddWithCascadeAsync(Customer entity)
{
    // Persist main entity first
    var result = await AddAsync(entity);
    
    // Persist Orders collection after (child-after strategy)
    foreach (var order in entity.Orders ?? [])
    {
        order.CustomerId = result.Id; // Set FK
        await _entityManager.PersistAsync(order);
    }
    
    return result;
}
```

**Key Features:**
- ✓ CascadeType flags: Persist, Merge, Remove, Refresh, Detach, All
- ✓ Transient entity detection (checks for default Id values)
- ✓ OrphanRemoval support for deleted collection items
- ✓ Proper operation ordering to respect FK constraints
- ✓ EntityManager integration for atomic operations

### ✅ Phase 7.4: Bidirectional Relationship Management (COMPLETE)

Automatic synchronization helper methods for bidirectional relationships with full type safety and nullability support:

**Generated Helper Classes:**
- `{Entity}RelationshipHelper` static classes with synchronization methods
- `Set{Property}(entity, value)` - For owner side (ManyToOne, OneToOne)
- `AddTo{Collection}(entity, item)` - For inverse side collections
- `RemoveFrom{Collection}(entity, item)` - For inverse side collections
- `ValidateRelationshipConsistency(entity)` - Validates FK and navigation property consistency

**Example:**
```csharp
// Generated CustomerRelationshipHelper
public static void AddToOrders(Customer entity, Order item)
{
    entity.Orders ??= new List<Order>();
    entity.Orders.Add(item);
    
    // Synchronize inverse side using direct property access (no reflection)
    item.Customer = entity;
    
    // Only set FK if the property exists on the target entity
    if (HasForeignKeyProperty)
    {
        item.CustomerId = entity.Id;
    }
}

// Generated OrderRelationshipHelper
public static void SetCustomer(Order entity, Customer? value)
{
    var oldValue = entity.Customer;
    if (oldValue != null)
    {
        // Remove from old parent's collection using direct property access
        if (oldValue.Orders?.Contains(entity) == true)
        {
            oldValue.Orders.Remove(entity);
        }
    }
    
    // Set new value with nullability handling
    // Non-nullable properties use null-forgiving operator
    entity.Customer = value!; // or value; for nullable properties
    
    // Add to new parent's collection
    if (value != null)
    {
        value.Orders ??= new List<Order>();
        if (!value.Orders.Contains(entity))
        {
            value.Orders.Add(entity);
        }
    }
}
```

**Key Features:**
- ✅ Bidirectional OneToMany/ManyToOne synchronization
- ✅ Bidirectional OneToOne synchronization
- ✅ Automatic FK synchronization with existence checking
- ✅ Collection initialization
- ✅ **Direct property access (no reflection)** - improved performance and type safety
- ✅ **Nullability-aware code generation** - respects nullable/non-nullable properties
- ✅ **FK property existence checking** - only generates FK assignments when property exists
- ✅ **Type-safe casting** - handles different FK and key types correctly
- ✅ **Validation methods** - consistency checking for relationships

## Project Structure

```
Phase7Demo/
├── Entities.cs          # Customer, Order, OrderItem, User, UserProfile
├── Repositories.cs      # ICustomerRepository, IOrderRepository, etc.
├── Program.cs           # Comprehensive demo of all features
├── Phase7Demo.csproj    # Project configuration
└── obj/generated/       # Generated source files (after build)
    ├── NPA.Generators.RepositoryGenerator/
    │   ├── CustomerRepositoryImplementation.g.cs
    │   ├── OrderRepositoryImplementation.g.cs
    │   ├── OrderItemRepositoryImplementation.g.cs
    │   └── UserRepositoryImplementation.g.cs
    └── NPA.Generators.BidirectionalRelationshipGenerator/
        ├── CustomerRelationshipHelper.g.cs
        ├── OrderRelationshipHelper.g.cs
        ├── OrderItemRelationshipHelper.g.cs
        ├── UserRelationshipHelper.g.cs
        └── UserProfileRelationshipHelper.g.cs
```

## Entity Relationships

```
Customer (1) ──< Orders (N)       OneToMany/ManyToOne (Bidirectional)
    ↑                                  - Cascade: All
    │                                  - OrphanRemoval: true
    │
Order (1) ──< OrderItems (N)      OneToMany/ManyToOne (Bidirectional)
    ↑                                  - Cascade: All
    │                                  - OrphanRemoval: true
    │
OrderItem (N) >── Order (1)       (Inverse of above)
                                       - Eager loading
                                       - Cascade: Persist | Merge

User (1) ──── UserProfile (1)     OneToOne (Bidirectional)
    ↑                                  - User is owner side (has MappedBy)
    │                                  - UserProfile has FK
    │
```

## Running the Demo

```bash
# Build the project
dotnet build

# Run the demo
dotnet run

# Check generated code
ls obj/generated/NPA.Generators*/
```

## Expected Output

The demo will:
1. ✅ List all Phase 7.1 generated relationship methods
2. ✅ Show Phase 7.2 eager loading and batch loading capabilities
3. ✅ Demonstrate Phase 7.3 cascade operation features
4. ✅ Execute Phase 7.4 bidirectional synchronization demos
5. 📁 List all generated files for inspection

## Testing Generated Code

After running the demo, check these generated files:

```bash
# Relationship-aware repository with all features
cat obj/generated/NPA.Generators.RepositoryGenerator/OrderRepositoryImplementation.g.cs

# Cascade operations (Customer has CascadeType.All)
cat obj/generated/NPA.Generators.RepositoryGenerator/CustomerRepositoryImplementation.g.cs

# Bidirectional synchronization helpers
cat obj/generated/NPA.Generators.BidirectionalRelationshipGenerator/CustomerRelationshipHelper.g.cs
cat obj/generated/NPA.Generators.BidirectionalRelationshipGenerator/OrderRelationshipHelper.g.cs
```

## Key Learnings

### 1. Automatic Code Generation
NPA generates 100% of the boilerplate relationship code at compile time using Roslyn source generators. No reflection at runtime (except in helper methods).

### 2. Type Safety
All generated methods are strongly typed. The compiler catches relationship configuration errors.

### 3. Performance
- SQL JOINs are generated efficiently
- Batch loading prevents N+1 queries
- No runtime overhead for relationship detection

### 4. JPA-Style Annotations
```csharp
[ManyToOne(Cascade = CascadeType.Persist, Fetch = FetchType.Eager)]
[JoinColumn("customer_id")]
public Customer Customer { get; set; }

[OneToMany(MappedBy = nameof(Order.Customer), OrphanRemoval = true)]
public ICollection<Order> Orders { get; set; }
```

## Recent Improvements (Phase 7.4)

### ✅ Completed Enhancements

1. **Removed Reflection** - All helper methods now use direct property access for better performance and type safety
2. **Nullability Handling** - Generated code correctly handles nullable and non-nullable properties:
   - Non-nullable properties use null-forgiving operator (`!`) when needed
   - Nullable properties allow null assignment in RemoveFrom methods
3. **FK Property Existence Checking** - FK assignments are only generated when the property exists on the target entity
4. **Type-Safe Casting** - Correctly handles cases where FK type differs from related entity's key type
5. **Inverse Collection Property Detection** - Automatically finds and uses inverse collection properties without reflection

### 🔄 Future Enhancements

- [ ] Repository integration with automatic validation
- [ ] Performance optimizations (compiled expressions for large collections)
- [ ] Support for ManyToMany bidirectional relationships

## Related Documentation

- [Phase 7.1 README](../../docs/tasks/phase7.1-relationship-aware-repository-generation/README.md)
- [Phase 7.2 README](../../docs/tasks/phase7.2-eager-loading-support/README.md)
- [Phase 7.3 README](../../docs/tasks/phase7.3-cascade-operations-enhancement/README.md)
- [Phase 7.4 README](../../docs/tasks/phase7.4-bidirectional-relationship-management/README.md)
- [Phase 7 Progress Document](../../docs/tasks/phase7.4-bidirectional-relationship-management/PROGRESS.md)

## Next Phases

- **Phase 7.5**: Orphan Removal Enhancement
- **Phase 7.6**: Relationship Query Methods
- **Phase 8**: Advanced Performance Features

---

**Status**: All core Phase 7 features working and tested! ✨
