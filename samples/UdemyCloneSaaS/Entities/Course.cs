using NPA.Core.Annotations;

namespace UdemyCloneSaaS.Entities;

/// <summary>
/// Represents a course that instructors create and students enroll in.
/// Multi-tenant: Each tenant has its own courses.
/// </summary>
[Entity]
[Table("courses")]
[MultiTenant]
public class Course
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [Column("instructor_id")]
    public long InstructorId { get; set; }

    [Column("category_id")]
    public long CategoryId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("short_description")]
    public string? ShortDescription { get; set; }

    [Column("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }

    [Column("video_preview_url")]
    public string? VideoPreviewUrl { get; set; }

    [Column("price")]
    public decimal Price { get; set; } = 0.0m;

    [Column("discount_price")]
    public decimal? DiscountPrice { get; set; }

    [Column("level")]
    public string Level { get; set; } = "Beginner"; // Beginner, Intermediate, Advanced, All Levels

    [Column("language")]
    public string Language { get; set; } = "English";

    [Column("duration_hours")]
    public decimal DurationHours { get; set; } = 0.0m;

    [Column("total_lessons")]
    public int TotalLessons { get; set; } = 0;

    [Column("enrolled_students_count")]
    public int EnrolledStudentsCount { get; set; } = 0;

    [Column("average_rating")]
    public decimal AverageRating { get; set; } = 0.0m;

    [Column("total_reviews")]
    public int TotalReviews { get; set; } = 0;

    [Column("status")]
    public string Status { get; set; } = "Draft"; // Draft, Published, Archived

    [Column("is_featured")]
    public bool IsFeatured { get; set; } = false;

    [Column("published_at")]
    public DateTime? PublishedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"Course[{Id}] {Title} by Instructor#{InstructorId} - ${Price} ({EnrolledStudentsCount} students, {AverageRating:F1}★)";
    }
}
