using NPA.Core.Annotations;
using NPA.Core.Repositories;
using UdemyCloneSaaS.Entities;

namespace UdemyCloneSaaS.Repositories;

/// <summary>
/// Repository for managing lessons within courses.
/// </summary>
[Repository]
public interface ILessonRepository : IRepository<Lesson, long>
{
    [Query("SELECT l FROM Lesson l WHERE l.CourseId = :courseId ORDER BY l.OrderIndex ASC")]
    Task<IEnumerable<Lesson>> FindByCourseIdAsync(long courseId);

    [Query("SELECT l FROM Lesson l WHERE l.CourseId = :courseId AND l.IsFreePreview = true")]
    Task<IEnumerable<Lesson>> GetFreePreviewLessonsAsync(long courseId);

    [Query("SELECT COUNT(l) FROM Lesson l WHERE l.CourseId = :courseId")]
    Task<long> CountLessonsByCourseAsync(long courseId);

    [Query("SELECT SUM(l.DurationMinutes) FROM Lesson l WHERE l.CourseId = :courseId")]
    Task<int> GetTotalDurationByCourseAsync(long courseId);
}
