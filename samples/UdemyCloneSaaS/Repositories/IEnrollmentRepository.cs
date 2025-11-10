using NPA.Core.Annotations;
using NPA.Core.Repositories;
using UdemyCloneSaaS.Entities;

namespace UdemyCloneSaaS.Repositories;

/// <summary>
/// Repository for managing enrollments with progress tracking.
/// </summary>
[Repository]
public interface IEnrollmentRepository : IRepository<Enrollment, long>
{
    [Query("SELECT e FROM Enrollment e WHERE e.StudentId = :studentId")]
    Task<IEnumerable<Enrollment>> FindByStudentIdAsync(long studentId);

    [Query("SELECT e FROM Enrollment e WHERE e.CourseId = :courseId")]
    Task<IEnumerable<Enrollment>> FindByCourseIdAsync(long courseId);

    [Query("SELECT e FROM Enrollment e WHERE e.StudentId = :studentId AND e.CourseId = :courseId")]
    Task<Enrollment?> FindByStudentAndCourseAsync(long studentId, long courseId);

    [Query("SELECT e FROM Enrollment e WHERE e.StudentId = :studentId AND e.IsCompleted = true")]
    Task<IEnumerable<Enrollment>> GetCompletedEnrollmentsByStudentAsync(long studentId);

    [Query("SELECT e FROM Enrollment e WHERE e.StudentId = :studentId AND e.IsCompleted = false ORDER BY e.LastAccessedAt DESC")]
    Task<IEnumerable<Enrollment>> GetActiveEnrollmentsByStudentAsync(long studentId);

    [Query("SELECT COUNT(e) FROM Enrollment e WHERE e.CourseId = :courseId")]
    Task<long> CountEnrollmentsByCourseAsync(long courseId);

    [Query("SELECT AVG(e.ProgressPercentage) FROM Enrollment e WHERE e.CourseId = :courseId")]
    Task<decimal> GetAverageProgressByCourseAsync(long courseId);
}
