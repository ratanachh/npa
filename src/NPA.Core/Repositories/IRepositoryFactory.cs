namespace NPA.Core.Repositories;

/// <summary>
/// Defines the contract for creating repository instances.
/// </summary>
public interface IRepositoryFactory
{
    /// <summary>
    /// Creates a repository for the specified entity type and key type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The primary key type.</typeparam>
    /// <returns>A repository instance.</returns>
    IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>() where TEntity : class;
    
    /// <summary>
    /// Creates a repository for the specified entity type with object key type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>A repository instance.</returns>
    IRepository<TEntity> CreateRepository<TEntity>() where TEntity : class;
}

