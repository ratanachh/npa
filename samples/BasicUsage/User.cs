using NPA.Core.Annotations;
using System;

namespace BasicUsage
{
    [Entity]
    [Table("users")]
    /// <summary>
    /// Sample User entity for demonstration.
    /// </summary>
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
    }
}
