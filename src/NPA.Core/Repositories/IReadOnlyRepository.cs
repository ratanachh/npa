using System.Linq.Expressions;

namespace NPA.Core.Repositories;

/// <summary>
/// Defines the contract for read-only repository operations on entities.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public interface IReadOnlyRepository<T, TKey> where T : class
{
    /// <summary>
    /// Gets an entity by its primary key asynchronously.
    /// </summary>
    /// <param name="id">The primary key value.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    Task<T?> GetByIdAsync(TKey id);
    
    /// <summary>
    /// Gets all entities asynchronously.
    /// </summary>
    /// <returns>A collection of all entities.</returns>
    Task<IEnumerable<T>> GetAllAsync();
    
    /// <summary>
    /// Checks if an entity exists by its primary key asynchronously.
    /// </summary>
    /// <param name="id">The primary key value.</param>
    /// <returns>True if the entity exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(TKey id);
    
    /// <summary>
    /// Counts the total number of entities asynchronously.
    /// </summary>
    /// <returns>The total count of entities.</returns>
    Task<int> CountAsync();
    
    /// <summary>
    /// Finds entities matching a predicate asynchronously.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>A collection of matching entities.</returns>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Finds a single entity matching a predicate asynchronously.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>The first matching entity if found; otherwise, null.</returns>
    Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate);
}

/// <summary>
/// Defines the contract for read-only repository operations on entities with an object key type.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IReadOnlyRepository<T> : IReadOnlyRepository<T, object> where T : class
{
}

