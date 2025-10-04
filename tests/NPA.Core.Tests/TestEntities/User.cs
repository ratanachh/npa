using NPA.Core.Annotations;

namespace NPA.Core.Tests.TestEntities;

/// <summary>
/// Test entity for unit testing.
/// </summary>
[Entity]
[Table("users")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Test entity with composite key for unit testing.
/// </summary>
[Entity]
[Table("order_items")]
public class OrderItem
{
    [Id]
    [Column("order_id")]
    public long OrderId { get; set; }

    [Id]
    [Column("product_id")]
    public long ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("price")]
    public decimal Price { get; set; }
}
