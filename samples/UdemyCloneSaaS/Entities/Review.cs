using NPA.Core.Annotations;

namespace UdemyCloneSaaS.Entities;

/// <summary>
/// Represents a student's review/rating of a course.
/// Multi-tenant: Reviews are scoped to tenants.
/// </summary>
[Entity]
[Table("reviews")]
[MultiTenant]
public class Review
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [Column("course_id")]
    public long CourseId { get; set; }

    [Column("student_id")]
    public long StudentId { get; set; }

    [Column("rating")]
    public int Rating { get; set; } = 5; // 1-5 stars

    [Column("title")]
    public string? Title { get; set; }

    [Column("comment")]
    public string? Comment { get; set; }

    [Column("is_verified_purchase")]
    public bool IsVerifiedPurchase { get; set; } = true;

    [Column("helpful_count")]
    public int HelpfulCount { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public override string ToString()
    {
        return $"Review[{Id}] {Rating}★ by Student#{StudentId} for Course#{CourseId} - {Title}";
    }
}
