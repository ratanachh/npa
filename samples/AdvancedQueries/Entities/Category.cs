using NPA.Core.Annotations;

namespace AdvancedQueries.Entities;

/// <summary>
/// Category entity demonstrating OneToMany relationship (Phase 2.1).
/// </summary>
[Entity]
[Table("categories")]
public class Category
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    // Phase 2.1: OneToMany relationship - One category has many products
    [OneToMany("Category", Fetch = FetchType.Lazy)]
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

