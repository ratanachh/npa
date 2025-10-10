using System.Data;
using Microsoft.Extensions.DependencyInjection;
using NPA.Core.Core;
using NPA.Core.Metadata;

namespace NPA.Core.Repositories;

/// <summary>
/// Factory for creating repository instances with support for custom repositories.
/// </summary>
public class RepositoryFactory : IRepositoryFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, Type> _repositoryTypes;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    public RepositoryFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _repositoryTypes = new Dictionary<Type, Type>();
    }
    
    /// <summary>
    /// Registers a custom repository implementation for an entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The primary key type.</typeparam>
    /// <typeparam name="TRepository">The repository implementation type.</typeparam>
    public void RegisterRepository<TEntity, TKey, TRepository>()
        where TEntity : class
        where TRepository : class, IRepository<TEntity, TKey>
    {
        var entityType = typeof(TEntity);
        _repositoryTypes[entityType] = typeof(TRepository);
    }
    
    /// <inheritdoc />
    public IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>()
        where TEntity : class
    {
        var entityType = typeof(TEntity);
        
        // Check if a custom repository is registered
        if (_repositoryTypes.TryGetValue(entityType, out var repositoryType))
        {
            var repository = _serviceProvider.GetService(repositoryType);
            if (repository != null)
            {
                return (IRepository<TEntity, TKey>)repository;
            }
        }
        
        // Create default repository
        var connection = _serviceProvider.GetRequiredService<IDbConnection>();
        var entityManager = _serviceProvider.GetRequiredService<IEntityManager>();
        var metadataProvider = _serviceProvider.GetRequiredService<IMetadataProvider>();
        
        return new BaseRepository<TEntity, TKey>(connection, entityManager, metadataProvider);
    }
    
    /// <inheritdoc />
    public IRepository<TEntity> CreateRepository<TEntity>()
        where TEntity : class
    {
        return (IRepository<TEntity>)CreateRepository<TEntity, object>();
    }
}

