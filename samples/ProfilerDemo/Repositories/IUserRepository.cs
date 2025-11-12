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
    // Indexed queries - should be fast
    [Query("SELECT u FROM User u WHERE u.Email = :email")]
    Task<User?> FindByEmailAsync(string email);

    [Query("SELECT u FROM User u WHERE u.Username = :username")]
    Task<User?> FindByUsernameAsync(string username);

    // Batch query - efficient way to fetch multiple users (PostgreSQL array syntax)
    [Query("SELECT u FROM User u WHERE u.Id = ANY(:ids)")]
    Task<IEnumerable<User>> FindByIdsAsync(int[] ids);

    // Full table scan - intentionally slow for comparison
    [Query("SELECT u FROM User u WHERE u.IsActive = :isActive")]
    Task<IEnumerable<User>> FindActiveUsersAsync(bool isActive = true);

    // Aggregate query
    [Query("SELECT u.Country, COUNT(u), AVG(u.AccountBalance), SUM(u.AccountBalance) FROM User u GROUP BY u.Country")]
    Task<IEnumerable<UserStatistics>> GetUserStatisticsByCountryAsync();

    // Pagination
    [Query("SELECT u FROM User u ORDER BY u.Id LIMIT :pageSize OFFSET :offset")]
    Task<IEnumerable<User>> GetUsersPageAsync(int offset, int pageSize);

    // Bulk update - using JPQL-style query
    [Query("UPDATE User u SET u.AccountBalance = u.AccountBalance + :amount WHERE u.Country = :country")]
    Task<int> BulkUpdateAccountBalanceAsync(string country, decimal amount);

    // Bulk delete - using JPQL-style query
    [Query("DELETE FROM User u WHERE u.IsActive = false AND u.CreatedAt < :date")]
    Task<int> DeleteInactiveUsersOlderThanAsync(DateTime date);
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
