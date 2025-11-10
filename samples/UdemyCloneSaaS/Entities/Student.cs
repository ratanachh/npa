using NPA.Core.Annotations;

namespace UdemyCloneSaaS.Entities;

/// <summary>
/// Represents a student who enrolls in and takes courses.
/// Multi-tenant: Each tenant has its own students.
/// </summary>
[Entity]
[Table("students")]
[MultiTenant]
public class Student
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

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("enrolled_courses_count")]
    public int EnrolledCoursesCount { get; set; } = 0;

    [Column("completed_courses_count")]
    public int CompletedCoursesCount { get; set; } = 0;

    [Column("total_learning_hours")]
    public decimal TotalLearningHours { get; set; } = 0.0m;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("last_active_at")]
    public DateTime? LastActiveAt { get; set; }

    public override string ToString()
    {
        return $"Student[{Id}] {Name} ({Email}) - {EnrolledCoursesCount} courses";
    }
}
