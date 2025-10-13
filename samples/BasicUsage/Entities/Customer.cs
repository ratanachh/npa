using NPA.Core.Annotations;

namespace NPA.Samples.Entities;

/// <summary>
/// Customer entity for console app demo.
/// Demonstrates basic entity mapping with NPA annotations.
/// </summary>
[Entity]
[Table("customers")]
public class Customer
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("email")]
    public string? Email { get; set; } = string.Empty;

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }
}
