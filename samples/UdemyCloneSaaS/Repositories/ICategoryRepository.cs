using NPA.Core.Annotations;
using NPA.Core.Repositories;
using UdemyCloneSaaS.Entities;

namespace UdemyCloneSaaS.Repositories;

/// <summary>
/// Repository for managing course categories.
/// </summary>
[Repository]
public interface ICategoryRepository : IRepository<Category, long>
{
    // [Query("SELECT c FROM Category c WHERE c.Slug = :slug")]
    Task<Category?> FindBySlugAsync(string slug);

    [Query("SELECT c FROM Category c WHERE c.IsActive = true ORDER BY c.CourseCount DESC")]
    Task<IEnumerable<Category>> GetActiveOrderedByPopularityAsync();

    [Query("SELECT c FROM Category c WHERE c.ParentCategoryId IS NULL AND c.IsActive = true")]
    Task<IEnumerable<Category>> GetRootCategoriesAsync();

    [Query("SELECT c FROM Category c WHERE c.ParentCategoryId = :parentId")]
    Task<IEnumerable<Category>> GetSubcategoriesAsync(long parentId);
}
