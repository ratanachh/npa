using NPA.Core.Annotations;

namespace NPA.Samples.Entities;

/// <summary>
/// Category entity with multi-tenancy support.
/// Demonstrates hierarchical data with tenant isolation.
/// </summary>
[Entity]
[Table("categories")]
[MultiTenant]
public class Category
{
    [Id]
    [Column("id")]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }

    [Column("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("parent_category_id")]
    public long? ParentCategoryId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public override string ToString()
    {
        return $"Category[{Id}] {Name} [Tenant: {TenantId}]";
    }
}
