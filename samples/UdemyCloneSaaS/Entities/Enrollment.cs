using NPA.Core.Annotations;

namespace UdemyCloneSaaS.Entities;

/// <summary>
/// Represents a student's enrollment in a course.
/// Tracks progress and completion status.
/// Multi-tenant: Enrollments are scoped to tenants.
/// </summary>
[Entity]
[Table("enrollments")]
[MultiTenant]
public class Enrollment
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [Column("student_id")]
    public long StudentId { get; set; }

    [Column("course_id")]
    public long CourseId { get; set; }

    [Column("enrolled_at")]
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    [Column("progress_percentage")]
    public decimal ProgressPercentage { get; set; } = 0.0m;

    [Column("completed_lessons_count")]
    public int CompletedLessonsCount { get; set; } = 0;

    [Column("is_completed")]
    public bool IsCompleted { get; set; } = false;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("last_accessed_at")]
    public DateTime? LastAccessedAt { get; set; }

    [Column("total_watch_time_minutes")]
    public int TotalWatchTimeMinutes { get; set; } = 0;

    [Column("certificate_issued")]
    public bool CertificateIssued { get; set; } = false;

    public override string ToString()
    {
        var status = IsCompleted ? "Completed" : $"{ProgressPercentage:F0}% progress";
        return $"Enrollment[{Id}] Student#{StudentId} in Course#{CourseId} - {status}";
    }
}
