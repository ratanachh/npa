using NPA.Core.Annotations;

namespace NPA.Core.Tests.CompositeKeys;

/// <summary>
/// Test entity with a composite primary key (OrderId + ProductId).
/// Represents an item in an order - classic composite key example.
/// </summary>
[Entity]
[Table("order_items")]
public class OrderItemTestEntity
{
    [Id]
    [Column("order_id")]
    public long OrderId { get; set; }

    [Id]
    [Column("product_id")]
    public long ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("discount")]
    public decimal? Discount { get; set; }
}

