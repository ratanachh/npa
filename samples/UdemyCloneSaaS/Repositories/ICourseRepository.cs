using NPA.Core.Annotations;
using NPA.Core.Repositories;
using UdemyCloneSaaS.Entities;

namespace UdemyCloneSaaS.Repositories;

/// <summary>
/// Repository for managing courses with custom query methods.
/// Uses NPA source generators to auto-implement repository methods.
/// </summary>
[Repository]
public interface ICourseRepository : IRepository<Course, long>
{
    // Custom query methods - auto-implemented by source generator
    [Query("SELECT c FROM Course c WHERE c.InstructorId = :instructorId")]
    Task<IEnumerable<Course>> FindByInstructorIdAsync(long instructorId);

    [Query("SELECT c FROM Course c WHERE c.CategoryId = :categoryId AND c.Status = :status")]
    Task<IEnumerable<Course>> FindByCategoryAndStatusAsync(long categoryId, string status);

    [Query("SELECT c FROM Course c WHERE c.Status = 'Published' AND c.IsFeatured = true ORDER BY c.EnrolledStudentsCount DESC")]
    Task<IEnumerable<Course>> GetFeaturedCoursesAsync();

    [Query("SELECT c FROM Course c WHERE c.Status = 'Published' ORDER BY c.CreatedAt DESC LIMIT :limit")]
    Task<IEnumerable<Course>> GetLatestCoursesAsync(int limit);

    [Query("SELECT c FROM Course c WHERE c.Status = 'Published' ORDER BY c.AverageRating DESC, c.TotalReviews DESC LIMIT :limit")]
    Task<IEnumerable<Course>> GetTopRatedCoursesAsync(int limit);

    [Query("SELECT c FROM Course c WHERE c.Status = 'Published' ORDER BY c.EnrolledStudentsCount DESC LIMIT :limit")]
    Task<IEnumerable<Course>> GetMostPopularCoursesAsync(int limit);

    [Query("SELECT c FROM Course c WHERE c.Status = 'Published' AND (c.Title LIKE :searchTerm OR c.Description LIKE :searchTerm)")]
    Task<IEnumerable<Course>> SearchCoursesAsync(string searchTerm);

    [Query("SELECT COUNT(c) FROM Course c WHERE c.InstructorId = :instructorId")]
    Task<long> CountByInstructorAsync(long instructorId);
}
