using NPA.Core.Annotations;
using NPA.Core.Repositories;
using UdemyCloneSaaS.Entities;

namespace UdemyCloneSaaS.Repositories;

/// <summary>
/// Repository for managing students.
/// </summary>
[Repository]
public interface IStudentRepository : IRepository<Student, long>
{
    [Query("SELECT s FROM Student s WHERE s.Email = :email")]
    Task<Student?> FindByEmailAsync(string email);

    [Query("SELECT s FROM Student s ORDER BY s.EnrolledCoursesCount DESC LIMIT :limit")]
    Task<IEnumerable<Student>> GetMostActiveStudentsAsync(int limit);

    [Query("SELECT COUNT(s) FROM Student s")]
    Task<long> CountAllStudentsAsync();
}
