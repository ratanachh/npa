using NPA.Core.Annotations;

namespace UdemyCloneSaaS.Entities;

/// <summary>
/// Represents a course category (e.g., "Web Development", "Data Science").
/// Multi-tenant: Each tenant can have custom categories.
/// </summary>
[Entity]
[Table("categories")]
[MultiTenant]
public class Category
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("icon")]
    public string? Icon { get; set; }

    [Column("parent_category_id")]
    public long? ParentCategoryId { get; set; }

    [Column("course_count")]
    public int CourseCount { get; set; } = 0;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"Category[{Id}] {Name} ({CourseCount} courses)";
    }
}
