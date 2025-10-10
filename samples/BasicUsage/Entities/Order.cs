using NPA.Core.Annotations;

namespace BasicUsage.Entities;

/// <summary>
/// Order entity demonstrating ManyToOne relationship (Phase 2.1).
/// </summary>
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

    [Column("order_date")]
    public DateTime OrderDate { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    // Phase 2.1: ManyToOne relationship - Many orders belong to one user
    [ManyToOne(Fetch = FetchType.Eager)]
    [JoinColumn("user_id", Nullable = false)]
    public User? User { get; set; }
}

