using NPA.Core.Annotations;

namespace UdemyCloneSaaS.Entities;

/// <summary>
/// Represents a lesson/lecture within a course.
/// Multi-tenant: Lessons belong to courses which are tenant-scoped.
/// </summary>
[Entity]
[Table("lessons")]
[MultiTenant]
public class Lesson
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [Column("course_id")]
    public long CourseId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("video_url")]
    public string? VideoUrl { get; set; }

    [Column("duration_minutes")]
    public int DurationMinutes { get; set; } = 0;

    [Column("order_index")]
    public int OrderIndex { get; set; } = 0;

    [Column("is_free_preview")]
    public bool IsFreePreview { get; set; } = false;

    [Column("content_type")]
    public string ContentType { get; set; } = "Video"; // Video, Article, Quiz

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"Lesson[{Id}] {Title} - {DurationMinutes}min ({ContentType})";
    }
}
