namespace NPA.Design.Tests.TestEntities;

/// <summary>
/// Test entity representing a user for multi-mapping scenarios
/// </summary>
public class UserWithRelations
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Address? Address { get; set; }
    public List<Order> Orders { get; set; } = new();
}

/// <summary>
/// Test entity representing an address for multi-mapping scenarios
/// </summary>
public class Address
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

/// <summary>
/// Test entity representing an order for multi-mapping scenarios
/// </summary>
public class Order
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime OrderDate { get; set; }
}
