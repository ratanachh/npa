using NPA.Core.Annotations;
using System;
using System.Collections.Generic;

namespace Phase7Demo;

[Entity]
[Table("customers")]
public class Customer
{
    [Id]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    // Inverse side - no methods generated
    [OneToMany(MappedBy = nameof(Order.Customer), Cascade = CascadeType.All, OrphanRemoval = true, Fetch = FetchType.Lazy)]
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

[Entity]
[Table("orders")]
public class Order
{
    [Id]
    [Column("id")]
    public int Id { get; set; }

    [Column("order_number")]
    public string OrderNumber { get; set; } = string.Empty;

    [Column("order_date")]
    public DateTime OrderDate { get; set; }

    [Column("customer_id")]
    public int CustomerId { get; set; }

    // Owner side - generates GetByIdWithCustomerAsync
    [ManyToOne(Cascade = CascadeType.Persist | CascadeType.Merge, Fetch = FetchType.Eager)]
    [JoinColumn("customer_id")]
    public Customer Customer { get; set; } = null!;

    // Inverse side - no methods generated
    [OneToMany(MappedBy = nameof(OrderItem.Order), Cascade = CascadeType.All, OrphanRemoval = true, Fetch = FetchType.Lazy)]
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

[Entity]
[Table("order_items")]
public class OrderItem
{
    [Id]
    [Column("id")]
    public int Id { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("order_id")]
    public int OrderId { get; set; }

    // Owner side - generates GetByIdWithOrderAsync
    [ManyToOne(Cascade = CascadeType.Persist | CascadeType.Merge, Fetch = FetchType.Eager)]
    [JoinColumn("order_id")]
    public Order Order { get; set; } = null!;
}

[Entity]
[Table("users")]
public class User
{
    [Id]
    [Column("id")]
    public int Id { get; set; }

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    // Bidirectional OneToOne - owner side (has MappedBy pointing to inverse)
    [OneToOne(MappedBy = nameof(UserProfile.User))]
    public UserProfile? Profile { get; set; }
}

[Entity]
[Table("user_profiles")]
public class UserProfile
{
    [Id]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("bio")]
    public string Bio { get; set; } = string.Empty;

    [Column("avatar_url")]
    public string AvatarUrl { get; set; } = string.Empty;

    // Bidirectional OneToOne - inverse side (has FK)
    [OneToOne]
    [JoinColumn("user_id")]
    public User? User { get; set; }
}
