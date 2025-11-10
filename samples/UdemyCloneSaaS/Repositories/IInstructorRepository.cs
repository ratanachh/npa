using NPA.Core.Annotations;
using NPA.Core.Repositories;
using UdemyCloneSaaS.Entities;

namespace UdemyCloneSaaS.Repositories;

/// <summary>
/// Repository for managing instructors.
/// </summary>
[Repository]
public interface IInstructorRepository : IRepository<Instructor, long>
{
    [Query("SELECT i FROM Instructor i WHERE i.Email = :email")]
    Task<Instructor?> FindByEmailAsync(string email);

    [Query("SELECT i FROM Instructor i WHERE i.IsVerified = true ORDER BY i.TotalStudents DESC LIMIT :limit")]
    Task<IEnumerable<Instructor>> GetTopInstructorsAsync(int limit);

    [Query("SELECT i FROM Instructor i ORDER BY i.AverageRating DESC LIMIT :limit")]
    Task<IEnumerable<Instructor>> GetHighestRatedInstructorsAsync(int limit);

    [Query("SELECT COUNT(i) FROM Instructor i WHERE i.IsVerified = true")]
    Task<long> CountVerifiedInstructorsAsync();
}
