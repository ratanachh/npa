using NPA.Core.Annotations;
using NPA.Core.Repositories;
using ProfilerDemo.Entities;

namespace ProfilerDemo.Repositories;

/// <summary>
/// Repository for User entity with custom query methods for performance testing.
/// Uses NPA source generators to auto-implement repository methods.
/// </summary>
[Repository]
public interface IUserRepository : IRepository<User, int>
{
    // Convention-based queries (derived from method names - no Query attribute needed)
    // Indexed queries - should be fast
    Task<User?> FindByEmailAsync(string email);
    
    Task<User?> FindByUsernameAsync(string username);
    
    Task<IEnumerable<User>> FindByIsActiveAsync(bool isActive);
    
    // Method with default parameter - convention-based
    Task<IEnumerable<User>> FindActiveUsersAsync(bool isActive = true);

    // Explicit JPQL queries using [Query] attribute
    // Batch query - efficient way to fetch multiple users (PostgreSQL array syntax)
    [Query("SELECT u FROM User u WHERE u.Id = ANY(:ids)")]
    Task<IEnumerable<User>> FindByIdsAsync(int[] ids);

    // Aggregate query returning DTO
    [Query("SELECT u.Country, COUNT(u), AVG(u.AccountBalance), SUM(u.AccountBalance) FROM User u GROUP BY u.Country")]
    Task<IEnumerable<UserStatistics>> GetUserStatisticsByCountryAsync();

    // Pagination
    [Query("SELECT u FROM User u ORDER BY u.Id LIMIT :pageSize OFFSET :offset")]
    Task<IEnumerable<User>> GetUsersPageAsync(int offset, int pageSize);

    // Modifying queries (UPDATE/DELETE)
    // Bulk update - using JPQL-style query
    [Query("UPDATE User u SET u.AccountBalance = u.AccountBalance + :amount WHERE u.Country = :country")]
    Task<int> BulkUpdateAccountBalanceAsync(string country, decimal amount);

    // Bulk delete - using JPQL-style query
    [Query("DELETE FROM User u WHERE u.IsActive = false AND u.CreatedAt < :date")]
    Task<int> DeleteInactiveUsersOlderThanAsync(DateTime date);
    
    // Derived delete query (convention-based)
    Task DeleteByIsActiveAsync(bool isActive);
}

/// <summary>
/// DTO for aggregate query results.
/// </summary>
public class UserStatistics
{
    public string Country { get; set; } = string.Empty;
    public long UserCount { get; set; }
    public decimal AverageBalance { get; set; }
    public decimal TotalBalance { get; set; }
}
