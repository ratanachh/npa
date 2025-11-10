using NPA.Core.Annotations;

namespace UdemyCloneSaaS.Entities;

/// <summary>
/// Represents an instructor who creates and teaches courses.
/// Multi-tenant: Each tenant has its own instructors.
/// </summary>
[Entity]
[Table("instructors")]
[MultiTenant]
public class Instructor
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("bio")]
    public string? Bio { get; set; }

    [Column("title")]
    public string? Title { get; set; }

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("total_students")]
    public int TotalStudents { get; set; } = 0;

    [Column("total_reviews")]
    public int TotalReviews { get; set; } = 0;

    [Column("average_rating")]
    public decimal AverageRating { get; set; } = 0.0m;

    [Column("is_verified")]
    public bool IsVerified { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"Instructor[{Id}] {Name} ({Email}) - {TotalStudents} students, {AverageRating:F1}★";
    }
}
