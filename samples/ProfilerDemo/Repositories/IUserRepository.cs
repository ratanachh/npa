using NPA.Core.Annotations;
using NPA.Core.Repositories;
using ProfilerDemo.Entities;

namespace ProfilerDemo.Repositories;

/// <summary>
/// Repository for User entity with custom query methods for performance testing.
/// Uses NPA source generators to auto-implement repository methods.
/// 
/// Demonstrates three query approaches:
/// 1. NamedQuery (auto-detected) - queries defined on entity, matched by method name
/// 2. [Query] attribute - inline queries on repository methods
/// 3. Convention-based - queries derived from method names
/// </summary>
[Repository]
public interface IUserRepository : IRepository<User, int>
{
    // ===== NamedQuery Examples (Auto-Detected) =====
    // These methods automatically use named queries defined on the User entity
    // No attributes needed - matched by method name!
    
    /// <summary>Auto-matches User.FindActiveUsersAsync named query</summary>
    Task<IEnumerable<User>> FindActiveUsersAsync();
    
    /// <summary>Auto-matches User.FindByCountryAsync named query</summary>
    Task<IEnumerable<User>> FindByCountryAsync(string country);
    
    /// <summary>Auto-matches User.FindHighBalanceUsersAsync named query</summary>
    Task<IEnumerable<User>> FindHighBalanceUsersAsync(decimal minBalance);
    
    /// <summary>Auto-matches User.FindRecentlyActiveAsync named query</summary>
    Task<IEnumerable<User>> FindRecentlyActiveAsync(DateTime since);
    
    // ===== Convention-Based Queries =====
    // These are derived from method names - no Query attribute needed
    // Indexed queries - should be fast
    
    Task<User?> FindByEmailAsync(string email);
    
    Task<User?> FindByUsernameAsync(string username);
    
    Task<IEnumerable<User>> FindByIsActiveAsync(bool isActive);

    // ===== Explicit [Query] Attribute Examples =====
    // Use when you need fine-grained control over the SQL
    
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
