using NPA.Core.Annotations;

namespace BasicUsage;

/// <summary>
/// Sample User entity for demonstration.
/// Demonstrates basic entity mapping (Phase 1.1) and relationships (Phase 2.1).
/// </summary>
[Entity]
[Table("users")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Phase 2.1: OneToMany relationship - One user can have many orders
    [OneToMany("User", Cascade = CascadeType.All)]
    public ICollection<Entities.Order> Orders { get; set; } = new List<Entities.Order>();
}
