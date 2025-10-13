using NPA.Core.Annotations;
using NPA.Samples.Entities;
using System.Collections.Generic;

namespace NPA.Samples.Entities;

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
    public bool IsActive { get; set; }

    [OneToMany(MappedBy = "User")]
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
