using NPA.Core.Annotations;

namespace AdvancedQueries.Entities;

[Entity]
[Table("products")]
public class Product
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("category_name")]
    public string CategoryName { get; set; } = string.Empty;

    [Column("price")]
    public decimal Price { get; set; }

    [Column("stock_quantity")]
    public int StockQuantity { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Phase 2.1: ManyToOne relationship - Many products belong to one category
    [ManyToOne]
    [JoinColumn("category_id")]
    public Category? Category { get; set; }

    public override string ToString()
    {
        return $"Product[{Id}] {Name} - {CategoryName} (${Price}) Stock: {StockQuantity}";
    }
}
