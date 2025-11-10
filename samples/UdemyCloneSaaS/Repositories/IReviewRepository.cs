using NPA.Core.Annotations;
using NPA.Core.Repositories;
using UdemyCloneSaaS.Entities;

namespace UdemyCloneSaaS.Repositories;

/// <summary>
/// Repository for managing course reviews and ratings.
/// </summary>
[Repository]
public interface IReviewRepository : IRepository<Review, long>
{
    [Query("SELECT r FROM Review r WHERE r.CourseId = :courseId ORDER BY r.CreatedAt DESC")]
    Task<IEnumerable<Review>> FindByCourseIdAsync(long courseId);

    [Query("SELECT r FROM Review r WHERE r.StudentId = :studentId")]
    Task<IEnumerable<Review>> FindByStudentIdAsync(long studentId);

    [Query("SELECT r FROM Review r WHERE r.CourseId = :courseId AND r.StudentId = :studentId")]
    Task<Review?> FindByStudentAndCourseAsync(long studentId, long courseId);

    [Query("SELECT AVG(r.Rating) FROM Review r WHERE r.CourseId = :courseId")]
    Task<decimal> GetAverageRatingByCourseAsync(long courseId);

    [Query("SELECT COUNT(r) FROM Review r WHERE r.CourseId = :courseId")]
    Task<long> CountReviewsByCourseAsync(long courseId);

    [Query("SELECT r FROM Review r WHERE r.CourseId = :courseId ORDER BY r.HelpfulCount DESC LIMIT :limit")]
    Task<IEnumerable<Review>> GetMostHelpfulReviewsAsync(long courseId, int limit);
}
