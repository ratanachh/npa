# Phase 7.4: Bidirectional Relationship Management

## Overview

Bidirectional relationship management ensures that both sides of entity relationships stay synchronized automatically. This eliminates the need for manual synchronization code and prevents data inconsistencies.

## Understanding Bidirectional Relationships

### Key Concepts

**Owner Side vs Inverse Side:**
- **Owner Side**: The side that "owns" the foreign key in the database. Typically `@ManyToOne` or `@OneToOne` without `MappedBy`.
- **Inverse Side**: The side that references the owner side. Typically `@OneToMany` or `@OneToOne` with `MappedBy`.

**MappedBy Attribute:**
- `MappedBy` indicates that the relationship is managed by the other side.
- The value of `MappedBy` is the property name on the **owner side** that this relationship references.
- Example: If `Order` has `Customer Customer { get; set; }` and `Customer` has `ICollection<Order> Orders { get; set; }`, then `Orders` should have `MappedBy = "Customer"`.

### Example: Order ↔ Customer Relationship

```csharp
[Entity]
[Table("orders")]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    // Owner side - has the foreign key
    // Only the navigation property is required - the FK is managed automatically
    [ManyToOne(Cascade = CascadeType.Persist | CascadeType.Merge, Fetch = FetchType.Eager)]
    [JoinColumn("customer_id")]
    public Customer Customer { get; set; } = null!;
    
    // Optional: You can also expose the FK directly for convenience
    // [Column("customer_id")]
    // public int CustomerId { get; set; }
}

[Entity]
[Table("customers")]
public class Customer
{
    [Id]
    public int Id { get; set; }
    
    // Inverse side - MappedBy points to the property name on Order
    [OneToMany(MappedBy = nameof(Order.Customer), Cascade = CascadeType.All, OrphanRemoval = true)]
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
```

**Important Notes:**
- `MappedBy = nameof(Order.Customer)` means "this relationship is managed by the `Customer` property on the `Order` entity"
- The `Customer` property on `Order` is the **owner side** (manages the FK column `customer_id`)
- The `Orders` property on `Customer` is the **inverse side** (no FK, just a collection)
- **The foreign key property (`CustomerId`) is optional** - you only need the navigation property (`Customer`)
- The framework automatically manages the foreign key column based on the `@JoinColumn` annotation

## Generated Helper Methods

The `BidirectionalRelationshipGenerator` automatically generates helper classes for entities with bidirectional relationships.

### Generated Helper Class Structure

For each entity with bidirectional relationships, a helper class is generated:

```csharp
public static class OrderRelationshipHelper
{
    // Owner side methods (ManyToOne/OneToOne)
    public static void SetCustomer(Order entity, Customer? value);
    
    // Inverse side methods (OneToMany collections)
    public static void AddToOrders(Customer entity, Order item);
    public static void RemoveFromOrders(Customer entity, Order item);
    
    // Validation
    public static void ValidateRelationshipConsistency(Order entity);
}
```

### Owner Side Methods (Set{Property})

Generated for `@ManyToOne` and `@OneToOne` relationships without `MappedBy`:

```csharp
public static void SetCustomer(Order entity, Customer? value)
{
    if (entity == null) throw new ArgumentNullException(nameof(entity));
    
    var oldValue = entity.Customer;
    if (oldValue == value) return; // No change
    
    // Remove from old parent's collection
    if (oldValue != null)
    {
        if (oldValue.Orders?.Contains(entity) == true)
        {
            oldValue.Orders.Remove(entity);
        }
    }
    
    // Set new value
    // For non-nullable properties, use null-forgiving operator when value parameter is nullable
    entity.Customer = value!; // or entity.Customer = value; if property is nullable
    // Note: The foreign key column is managed automatically by the framework
    // If a CustomerId property exists, it may be updated for convenience,
    // but only the Customer navigation property is required
    
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

**What it does:**
1. Removes the entity from the old parent's collection
2. Sets the new relationship value (`entity.Customer = value` or `entity.Customer = value!` depending on nullability)
3. The framework automatically manages the foreign key column (`customer_id`) based on `@JoinColumn`
4. Adds the entity to the new parent's collection

**Note**: 
- The foreign key property (`CustomerId`) is **optional**. You only need the navigation property (`Customer`). The framework handles the foreign key column automatically.
- The generator automatically handles nullability: if the property is non-nullable, it uses `value!` to satisfy the compiler when the parameter is nullable.

### Inverse Side Methods (AddTo{Collection} / RemoveFrom{Collection})

Generated for `@OneToMany` and `@ManyToMany` relationships with `MappedBy`:

```csharp
public static void AddToOrders(Customer entity, Order item)
{
    if (entity == null) throw new ArgumentNullException(nameof(entity));
    if (item == null) throw new ArgumentNullException(nameof(item));
    
    entity.Orders ??= new List<Order>();
    
    // Add to collection if not already present
    if (!entity.Orders.Contains(item))
    {
        entity.Orders.Add(item);
    }
    
    // Set inverse side (owner side)
    if (item.Customer != entity)
    {
        item.Customer = entity;
        // The foreign key column is managed automatically by the framework
    }
}

public static void RemoveFromOrders(Customer entity, Order item)
{
    if (entity == null) throw new ArgumentNullException(nameof(entity));
    if (item == null) throw new ArgumentNullException(nameof(item));
    
    if (entity.Orders?.Contains(item) == true)
    {
        entity.Orders.Remove(item);
    }
    
    // Clear inverse side
    if (item.Customer == entity)
    {
        // Only assign null if the owner-side property is nullable
        // For non-nullable properties, the null assignment is skipped
        item.Customer = null; // Only generated if Customer property is nullable
        // The foreign key column is managed automatically by the framework
        // If a CustomerId property exists, it may be cleared for convenience
    }
}
```

**What they do:**
- `AddToOrders`: Adds item to collection and sets the owner side property
- `RemoveFromOrders`: Removes item from collection and clears the owner side property (only if the owner-side property is nullable)

**Note**: For `RemoveFromOrders`, if the owner-side property (e.g., `Order.Customer`) is non-nullable, the null assignment is skipped. Only the foreign key is cleared. This is a design constraint - non-nullable relationships should not be removed by setting them to null.

## Usage Examples

### Setting a Relationship (Owner Side)

```csharp
var order = new Order { OrderNumber = "ORD-001" };
var customer = new Customer { Name = "John Doe" };

// Use helper to set relationship - both sides stay synchronized
OrderRelationshipHelper.SetCustomer(order, customer);

// Now:
// - order.Customer == customer ✓
// - order.CustomerId == customer.Id ✓
// - customer.Orders.Contains(order) == true ✓
```

### Adding to Collection (Inverse Side)

```csharp
var customer = new Customer { Name = "John Doe" };
var order1 = new Order { OrderNumber = "ORD-001" };
var order2 = new Order { OrderNumber = "ORD-002" };

// Use helper to add - both sides stay synchronized
CustomerRelationshipHelper.AddToOrders(customer, order1);
CustomerRelationshipHelper.AddToOrders(customer, order2);

// Now:
// - customer.Orders.Count == 2 ✓
// - order1.Customer == customer ✓
// - order2.Customer == customer ✓
```

### Removing from Collection (Inverse Side)

```csharp
// Remove order from customer's collection
CustomerRelationshipHelper.RemoveFromOrders(customer, order1);

// Now:
// - customer.Orders.Contains(order1) == false ✓
// - order1.Customer == null ✓ (only if Customer property is nullable)
// - The foreign key column (customer_id) is automatically cleared by the framework ✓
// Note: If Order.Customer is non-nullable, the property is not set to null (only FK is cleared)
```

## Implementation Details

### How Inverse Collection Property is Found

When generating code for the owner side (e.g., `Order.Customer`), the generator:

1. Looks up the target entity type (`Customer`)
2. Extracts all relationships from that entity
3. Finds the `OneToMany` relationship where `MappedBy == "Customer"` (the property name)
4. Uses that relationship's property name as the inverse collection property

This ensures the generated code uses the correct property name without reflection.

### How Nullability is Determined

**For Owner Side Properties:**
- The generator inspects the property symbol's `NullableAnnotation` attribute
- `NullableAnnotation.Annotated` means the property is nullable (`Customer?`)
- `NullableAnnotation.NotAnnotated` means the property is non-nullable (`Customer`)

**For Inverse Side (Owner-Side Property Nullability):**
- When processing inverse side relationships (e.g., `Customer.Orders` with `MappedBy = "Customer"`), the generator:
  1. Looks up the owner entity type (`Order`)
  2. Finds the property referenced by `MappedBy` (`Customer`)
  3. Checks that property's `NullableAnnotation` to determine if it's nullable
  4. Stores this information in `IsNullable` for use during code generation

This allows the generator to correctly handle null assignments and null-forgiving operators based on the actual property nullability.

### Direct Property Access (No Reflection)

All generated code uses direct property access for performance:

```csharp
// ✅ Direct access (generated code)
item.Customer = entity;
item.CustomerId = entity.Id;

// ❌ NOT using reflection (old approach)
var prop = item.GetType().GetProperty("Customer");
prop.SetValue(item, entity);
```

### Nullability Handling

The generator automatically handles nullable and non-nullable properties:

**For Owner Side (Set{Property} methods):**
- The generator checks if the property is nullable using `NullableAnnotation`
- If the property is **non-nullable** but the `value` parameter is nullable (`Customer?`), it uses the null-forgiving operator: `entity.Customer = value!;`
- If the property is **nullable**, it assigns directly: `entity.Customer = value;`

**For Inverse Side (RemoveFrom{Collection} methods):**
- The generator checks the owner-side property's nullability by looking up the owner entity
- If the owner-side property is **nullable**, it generates: `item.Customer = null;`
- If the owner-side property is **non-nullable**, it skips the null assignment and adds a comment explaining why

**Example:**
```csharp
// If Order.Customer is non-nullable (public Customer Customer { get; set; } = null!;)
public static void SetCustomer(Order entity, Customer? value)
{
    // ...
    entity.Customer = value!; // Null-forgiving operator used
    // ...
}

// If Order.Customer is nullable (public Customer? Customer { get; set; };)
public static void SetCustomer(Order entity, Customer? value)
{
    // ...
    entity.Customer = value; // Direct assignment
    // ...
}

// If Order.Customer is non-nullable
public static void RemoveFromOrders(Customer entity, Order item)
{
    // ...
    if (item.Customer == entity)
    {
        // Note: Customer is non-nullable, skipping null assignment
        // Only FK is cleared: item.CustomerId = 0;
    }
}

// If Order.Customer is nullable
public static void RemoveFromOrders(Customer entity, Order item)
{
    // ...
    if (item.Customer == entity)
    {
        item.Customer = null; // Null assignment allowed
        item.CustomerId = 0;
    }
}
```

## Validation

The generated helper includes validation methods:

```csharp
public static void ValidateRelationshipConsistency(Order entity)
{
    // Validates that OrderId matches Order.Id if Order is set
    if (entity.Order != null)
    {
        var expectedFk = entity.Order.Id;
        if (entity.OrderId != expectedFk)
        {
            throw new InvalidOperationException(
                $"Bidirectional relationship inconsistency: OrderId ({entity.OrderId}) " +
                $"does not match Order.Id ({expectedFk})");
        }
    }
    else if (entity.OrderId != 0)
    {
        throw new InvalidOperationException(
            $"Bidirectional relationship inconsistency: OrderId is {entity.OrderId} " +
            $"but Order is null");
    }
}
```

## Best Practices

1. **Always use helper methods** for bidirectional relationships
2. **Don't manually set both sides** - let the helper handle it
3. **Use `nameof()` for MappedBy** to avoid typos: `MappedBy = nameof(Order.Customer)`
4. **Validate relationships** before persisting if needed
5. **Owner side manages the FK** - set relationships from the owner side when possible

## Limitations

- Currently supports: `ManyToOne`, `OneToMany`, `OneToOne`
- `ManyToMany` bidirectional support is planned
- Complex relationship graphs may need manual handling
- Circular dependencies are detected and prevented

## Testing

The implementation includes comprehensive tests covering:
- Setting owner side relationships
- Adding/removing from inverse collections
- Null handling
- Foreign key synchronization
- Collection initialization
- Validation logic

## Future Enhancements

- [ ] ManyToMany bidirectional support
- [ ] Change tracking integration
- [ ] Performance optimizations for large collections
- [ ] Batch synchronization operations
