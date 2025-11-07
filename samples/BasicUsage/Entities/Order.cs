using NPA.Core.Annotations;

namespace NPA.Samples.Entities;

[Entity]
[Table("orders")]
public class Order
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("order_number")]
    public string OrderNumber { get; set; } = string.Empty;

    [Column("customer_name")]
    public string CustomerName { get; set; } = string.Empty;

    [Column("order_date")]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("shipped_date")]
    public DateTime? ShippedDate { get; set; }

    [Column("user_id")]
    public long? UserId { get; set; }

    [ManyToOne]
    [JoinColumn("user_id")]
    public User? User { get; set; }

    public override string ToString()
    {
        return $"Order[{OrderNumber}] {CustomerName} - ${TotalAmount} ({Status})";
    }
}
