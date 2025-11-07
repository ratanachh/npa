using NPA.Core.Annotations;

namespace NPA.Samples.Entities;

[Entity]
[Table("order_items")]
public class OrderItem
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("order_id")]
    public long OrderId { get; set; }

    [Column("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("subtotal")]
    public decimal Subtotal { get; set; }

    public override string ToString()
    {
        return $"  - {ProductName} x {Quantity} @ ${UnitPrice} = ${Subtotal}";
    }
}
