using NPA.Core.Annotations;
using NPA.Core.Repositories;
using UdemyCloneSaaS.Entities;

namespace UdemyCloneSaaS.Repositories;

/// <summary>
/// Enhanced repository demonstrating Spring Data JPA-style query methods.
/// </summary>
[Repository]
public interface IStudentRepositoryEnhanced : IRepository<Student, long>
{
    // Simple equality
    Task<Student?> FindByEmailAsync(string email);
    
    // Greater than / Less than
    Task<IEnumerable<Student>> FindByEnrolledCoursesCountGreaterThanAsync(int count);
    Task<IEnumerable<Student>> FindByTotalLearningHoursLessThanAsync(decimal hours);
    
    // Between
    Task<IEnumerable<Student>> FindByEnrolledCoursesCountBetweenAsync(int min, int max);
    
    // Like / Containing
    Task<IEnumerable<Student>> FindByNameContainingAsync(string namePart);
    Task<IEnumerable<Student>> FindByEmailStartingWithAsync(string prefix);
    Task<IEnumerable<Student>> FindByNameEndingWithAsync(string suffix);
    
    // In / NotIn
    Task<IEnumerable<Student>> FindByIdInAsync(long[] ids);
    Task<IEnumerable<Student>> FindByEmailNotInAsync(string[] emails);
    
    // IsNull / IsNotNull
    Task<IEnumerable<Student>> FindByLastActiveAtIsNullAsync();
    Task<IEnumerable<Student>> FindByAvatarUrlIsNotNullAsync();
    
    // Multiple conditions with And
    Task<Student?> FindByEmailAndTenantIdAsync(string email, string tenantId);
    Task<IEnumerable<Student>> FindByTenantIdAndEnrolledCoursesCountGreaterThanAsync(string tenantId, int count);
    
    // With ordering
    Task<IEnumerable<Student>> FindByTenantIdOrderByNameAscAsync(string tenantId);
    Task<IEnumerable<Student>> FindByTenantIdOrderByEnrolledCoursesCountDescAsync(string tenantId);
    Task<IEnumerable<Student>> FindByTenantIdOrderByNameAscThenEmailDescAsync(string tenantId);
    
    // Count methods
    Task<long> CountByTenantIdAsync(string tenantId);
    Task<long> CountByEnrolledCoursesCountGreaterThanAsync(int count);
    
    // Exists methods
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByTenantIdAndEmailAsync(string tenantId, string email);
    
    // Delete methods
    Task DeleteByEmailAsync(string email);
    Task DeleteByLastActiveAtIsNullAsync();
}
