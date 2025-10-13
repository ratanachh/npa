using NPA.Core.Annotations;

namespace NPA.Samples.Entities;

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

    public override string ToString()
    {
        return $"Product[{Id}] {Name} - {CategoryName} (${Price}) Stock: {StockQuantity}";
    }
}
