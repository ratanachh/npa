using NPA.Core.Annotations;

namespace UdemyCloneSaaS.Entities;

/// <summary>
/// Represents a SaaS tenant (e.g., organization or institution using the platform).
/// Each tenant has its own isolated set of courses, instructors, and students.
/// </summary>
[Entity]
[Table("tenants")]
public class Tenant
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("domain")]
    public string? Domain { get; set; }

    [Column("subscription_tier")]
    public string SubscriptionTier { get; set; } = "Free"; // Free, Pro, Enterprise

    [Column("max_instructors")]
    public int MaxInstructors { get; set; } = 10;

    [Column("max_courses")]
    public int MaxCourses { get; set; } = 50;

    [Column("max_students")]
    public int MaxStudents { get; set; } = 1000;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("trial_ends_at")]
    public DateTime? TrialEndsAt { get; set; }
}
