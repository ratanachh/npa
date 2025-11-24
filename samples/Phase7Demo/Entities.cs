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

    // Owner side - generates LoadCustomerAsync (lazy loading)
    // Non-nullable property - will use null-forgiving operator in generated code
    // The foreign key column (customer_id) is automatically managed by @JoinColumn
    // You don't need to expose CustomerId - the framework handles it automatically
    [ManyToOne(Cascade = CascadeType.Persist | CascadeType.Merge, Fetch = FetchType.Lazy)]
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

    // Owner side - generates LoadOrderAsync (lazy loading)
    // The foreign key column (order_id) is automatically managed by @JoinColumn
    // You don't need to expose OrderId - the framework handles it automatically
    [ManyToOne(Cascade = CascadeType.Persist | CascadeType.Merge, Fetch = FetchType.Lazy)]
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

    [Column("bio")]
    public string Bio { get; set; } = string.Empty;

    [Column("avatar_url")]
    public string AvatarUrl { get; set; } = string.Empty;

    // Bidirectional OneToOne - inverse side (has FK)
    // Nullable property - can be set to null in RemoveFrom methods
    // The foreign key column (user_id) is automatically managed by @JoinColumn
    // You don't need to expose UserId - the framework handles it automatically
    [OneToOne]
    [JoinColumn("user_id")]
    public User? User { get; set; }
}

// TEST CASE 1: OneToOne with different ID types (int to Guid)
[Entity]
[Table("order_lines")]
public class OrderLine
{
    [Id]
    [Column("id")]
    public int Id { get; set; }
    
    // OrderLine uses int, Product uses Guid
    // The foreign key column (product_id) should be Guid type
    [OneToOne(Fetch = FetchType.Lazy)]
    [JoinColumn("product_id")]
    public Product Product { get; set; } = null!;
}

// TEST CASE 2: OneToMany with same ID types (Guid to Guid)
[Entity]
[Table("products")]
public class Product
{
    [Id]
    [Column("id")]
    public Guid Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    // Owner side - Product (Guid) belongs to Category (Guid)
    [ManyToOne(Cascade = CascadeType.Persist | CascadeType.Merge, Fetch = FetchType.Lazy)]
    [JoinColumn("category_id")]
    public Category? Category { get; set; }
    
    // TEST CASE 3: OneToMany with different ID types (Guid to long)
    // Owner side - Product (Guid) belongs to Supplier (long)
    [ManyToOne(Cascade = CascadeType.Persist | CascadeType.Merge, Fetch = FetchType.Lazy)]
    [JoinColumn("supplier_id")]
    public Supplier? Supplier { get; set; }
    
    // TEST CASE 4: ManyToMany with different ID types (Guid to int)
    // Owner side - Product (Guid) has many Tags (int)
    [ManyToMany(Fetch = FetchType.Lazy)]
    [JoinTable("product_tags", JoinColumns = new[] { "product_id" }, InverseJoinColumns = new[] { "tag_id" })]
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}

[Entity]
[Table("categories")]
public class Category
{
    [Id]
    [Column("id")]
    public Guid Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    // Inverse side - Category (Guid) has many Products (Guid) - same type
    [OneToMany(MappedBy = nameof(Product.Category), Cascade = CascadeType.All, Fetch = FetchType.Lazy)]
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

// TEST CASE 3: OneToMany with different ID types
[Entity]
[Table("suppliers")]
public class Supplier
{
    [Id]
    [Column("id")]
    public long Id { get; set; }  // long ID
    
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    // Inverse side - Supplier (long) has many Products (Guid) - different types
    [OneToMany(MappedBy = nameof(Product.Supplier), Cascade = CascadeType.All, Fetch = FetchType.Lazy)]
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

// TEST CASE 4: ManyToMany with different ID types
[Entity]
[Table("tags")]
public class Tag
{
    [Id]
    [Column("id")]
    public int Id { get; set; }  // int ID
    
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    // ManyToMany - Tag (int) <-> Product (Guid) - different types
    [ManyToMany(MappedBy = nameof(Product.Tags), Fetch = FetchType.Lazy)]
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
