using NPA.Core.Annotations;

namespace NPA.Samples.Entities;

/// <summary>
/// Product entity with multi-tenancy support.
/// Demonstrates automatic tenant isolation using [MultiTenant] attribute.
/// </summary>
[Entity]
[Table("products")]
[MultiTenant] // Enables automatic tenant filtering
public class Product
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("category_name")]
    public string CategoryName { get; set; } = string.Empty;

    [Column("price")]
    public decimal Price { get; set; }

    [Column("stock_quantity")]
    public int StockQuantity { get; set; }

    [Column("category_id")]
    public long? CategoryId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"Product[{Id}] {Name} - {CategoryName} (${Price}) Stock: {StockQuantity} [Tenant: {TenantId}]";
    }
}
