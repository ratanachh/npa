using System.Linq.Expressions;

namespace NPA.Core.Repositories;

/// <summary>
/// Defines the contract for repository operations on entities with a specific key type.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public interface IRepository<T, TKey> where T : class
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
    /// Adds a new entity asynchronously.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>The added entity with generated keys populated.</returns>
    Task<T> AddAsync(T entity);
    
    /// <summary>
    /// Updates an existing entity asynchronously.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    Task UpdateAsync(T entity);
    
    /// <summary>
    /// Deletes an entity by its primary key asynchronously.
    /// </summary>
    /// <param name="id">The primary key value.</param>
    Task DeleteAsync(TKey id);
    
    /// <summary>
    /// Deletes an entity asynchronously.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    Task DeleteAsync(T entity);
    
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
    
    /// <summary>
    /// Finds entities matching a predicate with ordering asynchronously.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <param name="orderBy">The property to order by.</param>
    /// <param name="descending">True for descending order; false for ascending.</param>
    /// <returns>A collection of matching entities.</returns>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderBy, bool descending = false);
    
    /// <summary>
    /// Finds entities matching a predicate with paging asynchronously.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <param name="skip">The number of entities to skip.</param>
    /// <param name="take">The number of entities to take.</param>
    /// <returns>A collection of matching entities.</returns>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, int skip, int take);
    
    /// <summary>
    /// Finds entities matching a predicate with ordering and paging asynchronously.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <param name="orderBy">The property to order by.</param>
    /// <param name="descending">True for descending order; false for ascending.</param>
    /// <param name="skip">The number of entities to skip.</param>
    /// <param name="take">The number of entities to take.</param>
    /// <returns>A collection of matching entities.</returns>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderBy, bool descending, int skip, int take);
}

/// <summary>
/// Defines the contract for repository operations on entities with an object key type.
/// This is a convenience interface for entities with various key types.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IRepository<T> : IRepository<T, object> where T : class
{
}

