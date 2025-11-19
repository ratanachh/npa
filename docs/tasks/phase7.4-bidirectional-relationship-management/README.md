# Phase 7.4: Bidirectional Relationship Management

## Overview
Implement automatic bidirectional relationship synchronization to maintain consistency between both sides of a relationship. Ensure that when one side is modified, the other side is automatically updated.

## Objectives
- Implement bidirectional relationship detection
- Generate synchronization methods for relationship consistency
- Support `mappedBy` configuration
- Prevent infinite recursion in bidirectional updates

## Tasks

### 1. Bidirectional Relationship Detection
- [x] Analyze entity classes for bidirectional relationships
- [x] Validate `mappedBy` configuration consistency
- [x] Detect inverse relationship properties
- [x] Build bidirectional relationship metadata

### 2. Relationship Synchronization Methods
- [x] Generate helper methods to synchronize both sides
- [x] Implement add/remove methods for collections
- [x] Create set methods for single-valued relationships
- [x] Support null handling in synchronization

### 3. Owner Side Management
- [x] Identify relationship owner (without `mappedBy`)
- [x] Generate owner-side persistence logic
- [x] Implement inverse side update methods
- [ ] Handle orphan removal on owner side

### 4. Change Tracking Integration
- [ ] Track relationship changes on both sides
- [ ] Detect and prevent circular updates
- [ ] Implement change comparison for relationships
- [ ] Generate dirty checking for relationships

### 5. Repository Method Enhancement
- [ ] Enhance Add/Update methods with synchronization
- [ ] Generate methods to fix inconsistent relationships
- [ ] Implement validation for bidirectional consistency
- [ ] Support bulk synchronization operations

## Example Usage

```csharp
// Bidirectional OneToMany/ManyToOne
[Entity]
[Table("customers")]
public class Customer
{
    [Id]
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    [OneToMany(MappedBy = "Customer")]
    public ICollection<Order> Orders { get; set; }
}

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
    
    public int CustomerId { get; set; }
}

// Bidirectional OneToOne
[Entity]
[Table("users")]
public class User
{
    [Id]
    public int Id { get; set; }
    
    public string Username { get; set; }
    
    [OneToOne(MappedBy = "User")]
    public UserProfile Profile { get; set; }
}

[Entity]
[Table("user_profiles")]
public class UserProfile
{
    [Id]
    public int Id { get; set; }
    
    [OneToOne]
    [JoinColumn("user_id")]
    public User User { get; set; }
    
    public int UserId { get; set; }
}

// Usage - automatic synchronization:
var customer = new Customer { Name = "John Doe" };
var order = new Order { OrderNumber = "ORD-001" };

// Setting one side automatically updates the other
order.Customer = customer; // Automatically adds order to customer.Orders
// customer.Orders now contains order

// Or from the other side
customer.Orders.Add(order); // Automatically sets order.Customer = customer
// order.Customer now references customer
```

## Generated Code Examples

### Synchronization Helper Methods
```csharp
// Generated in Order entity or repository helper
public static class OrderRelationshipHelper
{
    public static void SetCustomer(Order order, Customer customer)
    {
        // Remove from old customer if exists
        if (order.Customer != null && order.Customer != customer)
        {
            order.Customer.Orders?.Remove(order);
        }
        
        // Set new customer
        order.Customer = customer;
        order.CustomerId = customer?.Id ?? 0;
        
        // Add to new customer's collection
        if (customer != null)
        {
            customer.Orders ??= new List<Order>();
            if (!customer.Orders.Contains(order))
            {
                customer.Orders.Add(order);
            }
        }
    }
    
    public static void AddToCustomerOrders(Customer customer, Order order)
    {
        customer.Orders ??= new List<Order>();
        
        // Add to collection if not already present
        if (!customer.Orders.Contains(order))
        {
            customer.Orders.Add(order);
        }
        
        // Set inverse side
        if (order.Customer != customer)
        {
            order.Customer = customer;
            order.CustomerId = customer.Id;
        }
    }
    
    public static void RemoveFromCustomerOrders(Customer customer, Order order)
    {
        if (customer.Orders?.Contains(order) == true)
        {
            customer.Orders.Remove(order);
        }
        
        // Clear inverse side
        if (order.Customer == customer)
        {
            order.Customer = null;
            order.CustomerId = 0;
        }
    }
}
```

### Repository with Synchronization
```csharp
public class OrderRepositoryImplementation : IOrderRepository
{
    public async Task<Order> AddAsync(Order order)
    {
        // Validate bidirectional consistency
        if (order.Customer != null && order.CustomerId != order.Customer.Id)
        {
            throw new InvalidOperationException(
                "Bidirectional relationship inconsistency detected. " +
                "CustomerId does not match Customer.Id");
        }
        
        // Synchronize before persist
        if (order.Customer != null)
        {
            OrderRelationshipHelper.SetCustomer(order, order.Customer);
        }
        
        // Persist order
        const string sql = @"
            INSERT INTO orders (order_number, customer_id)
            VALUES (@OrderNumber, @CustomerId)
            RETURNING *";
        
        var inserted = await _connection.QuerySingleAsync<Order>(sql, order);
        
        // Update synchronized relationship
        if (order.Customer != null)
        {
            inserted.Customer = order.Customer;
        }
        
        return inserted;
    }
    
    public async Task UpdateAsync(Order order)
    {
        // Load existing to detect relationship changes
        var existing = await GetByIdAsync(order.Id);
        if (existing == null)
            throw new EntityNotFoundException($"Order {order.Id} not found");
        
        // Handle customer change
        if (order.CustomerId != existing.CustomerId)
        {
            // Remove from old customer
            if (existing.Customer != null)
            {
                OrderRelationshipHelper.RemoveFromCustomerOrders(
                    existing.Customer, existing);
            }
            
            // Add to new customer
            if (order.Customer != null)
            {
                OrderRelationshipHelper.AddToCustomerOrders(
                    order.Customer, order);
            }
        }
        
        // Update order
        const string sql = @"
            UPDATE orders 
            SET order_number = @OrderNumber,
                customer_id = @CustomerId
            WHERE id = @Id";
        
        await _connection.ExecuteAsync(sql, order);
    }
}
```

### OneToOne Synchronization
```csharp
public static class UserProfileRelationshipHelper
{
    public static void SetUser(UserProfile profile, User user)
    {
        // Remove old bidirectional link
        if (profile.User != null && profile.User != user)
        {
            profile.User.Profile = null;
        }
        
        // Set new user
        profile.User = user;
        profile.UserId = user?.Id ?? 0;
        
        // Set inverse side
        if (user != null && user.Profile != profile)
        {
            user.Profile = profile;
        }
    }
}
```

## Acceptance Criteria
- [ ] Bidirectional relationships automatically synchronized
- [ ] Both sides of relationship always consistent
- [ ] No infinite recursion in synchronization
- [ ] Proper handling of null relationships
- [ ] Collection add/remove operations synchronized
- [ ] Single-valued relationship changes synchronized
- [ ] Validation detects inconsistent relationships
- [ ] Performance impact minimal

## Dependencies
- Phase 7.1: Relationship-Aware Repository Generation
- Phase 2.1: Relationship Mapping

## Testing Requirements
- Unit tests for synchronization helper methods
- Tests for all bidirectional relationship types
- Circular reference prevention tests
- Null handling tests
- Collection modification tests
- Integration tests with repository operations
- Performance tests for large collections

## Best Practices
- Always use generated helper methods for relationship modifications
- Validate consistency before persistence
- Prefer owner-side operations when possible
- Document which side is the owner
- Use transactions for bidirectional updates

## Documentation
- Guide on bidirectional relationship configuration
- Explanation of `mappedBy` attribute
- Best practices for relationship synchronization
- Common pitfalls and solutions
- Examples for each relationship type
