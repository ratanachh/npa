using System.Data;
using Dapper;
using NPA.Core.Annotations;
using NPA.Core.Metadata;

namespace NPA.Core.LazyLoading;

/// <summary>
/// Implementation of lazy loader.
/// Handles lazy loading of related entities and collections using metadata and SQL generation.
/// </summary>
public class LazyLoader : ILazyLoader
{
    private readonly ILazyLoadingContext _context;
    private readonly ILazyLoadingCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="LazyLoader"/> class.
    /// </summary>
    /// <param name="context">The lazy loading context.</param>
    /// <param name="cache">The lazy loading cache.</param>
    /// <exception cref="ArgumentNullException">Thrown when context or cache is null.</exception>
    public LazyLoader(ILazyLoadingContext context, ILazyLoadingCache cache)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc />
    public async Task<T?> LoadAsync<T>(object entity, string propertyName, CancellationToken cancellationToken = default) where T : class
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

        // Check if already loaded
        if (IsLoaded(entity, propertyName))
        {
            if (_cache.TryGet<T>(entity, propertyName, out var cachedValue))
            {
                return cachedValue;
            }
        }

        // Load the related entity
        var relatedEntity = await LoadRelatedEntityAsync<T>(entity, propertyName, cancellationToken);

        // Cache the loaded entity
        if (relatedEntity != null)
        {
            _cache.Add(entity, propertyName, relatedEntity);
        }

        // Mark as loaded
        MarkAsLoaded(entity, propertyName);

        return relatedEntity;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> LoadCollectionAsync<T>(object entity, string propertyName, CancellationToken cancellationToken = default) where T : class
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

        // Check if already loaded
        if (IsLoaded(entity, propertyName))
        {
            if (_cache.TryGet<IEnumerable<T>>(entity, propertyName, out var cachedValue))
            {
                return cachedValue ?? Enumerable.Empty<T>();
            }
        }

        // Load the related collection
        var relatedCollection = await LoadRelatedCollectionAsync<T>(entity, propertyName, cancellationToken);

        // Cache the loaded collection
        var collectionList = relatedCollection?.ToList() ?? new List<T>();
        _cache.Add(entity, propertyName, collectionList);

        // Mark as loaded
        MarkAsLoaded(entity, propertyName);

        return collectionList;
    }

    /// <inheritdoc />
    public bool IsLoaded(object entity, string propertyName)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

        if (entity is ILazyLoadingProxy proxy)
        {
            return proxy.IsLoaded(propertyName);
        }

        return _cache.Contains(entity, propertyName);
    }

    /// <inheritdoc />
    public void MarkAsLoaded(object entity, string propertyName)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

        if (entity is ILazyLoadingProxy proxy)
        {
            proxy.MarkAsLoaded(propertyName);
        }
    }

    /// <inheritdoc />
    public void MarkAsNotLoaded(object entity, string propertyName)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

        if (entity is ILazyLoadingProxy proxy)
        {
            proxy.MarkAsNotLoaded(propertyName);
        }

        _cache.Remove(entity, propertyName);
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        _cache.Clear();
    }

    /// <inheritdoc />
    public void ClearCache(object entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        _cache.Remove(entity);
    }

    /// <inheritdoc />
    public void ClearCache(object entity, string propertyName)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        _cache.Remove(entity, propertyName);
    }

    private async Task<T?> LoadRelatedEntityAsync<T>(object entity, string propertyName, CancellationToken cancellationToken) where T : class
    {
        var entityType = entity.GetType();
        var metadata = _context.MetadataProvider.GetEntityMetadata(entityType);

        if (!metadata.Relationships.TryGetValue(propertyName, out var relationship))
        {
            throw new InvalidOperationException($"Relationship '{propertyName}' not found on entity '{entityType.Name}'");
        }

        var sql = GenerateLoadSql(relationship, entity, metadata);
        var parameters = CreateLoadParameters(relationship, entity, metadata);

        var commandDefinition = new CommandDefinition(sql, parameters, _context.Transaction, cancellationToken: cancellationToken);
        return await _context.Connection.QueryFirstOrDefaultAsync<T>(commandDefinition);
    }

    private async Task<IEnumerable<T>> LoadRelatedCollectionAsync<T>(object entity, string propertyName, CancellationToken cancellationToken) where T : class
    {
        var entityType = entity.GetType();
        var metadata = _context.MetadataProvider.GetEntityMetadata(entityType);

        if (!metadata.Relationships.TryGetValue(propertyName, out var relationship))
        {
            throw new InvalidOperationException($"Relationship '{propertyName}' not found on entity '{entityType.Name}'");
        }

        var sql = GenerateLoadCollectionSql(relationship, entity, metadata);
        var parameters = CreateLoadParameters(relationship, entity, metadata);

        var commandDefinition = new CommandDefinition(sql, parameters, _context.Transaction, cancellationToken: cancellationToken);
        return await _context.Connection.QueryAsync<T>(commandDefinition);
    }

    private string GenerateLoadSql(RelationshipMetadata relationship, object entity, EntityMetadata entityMetadata)
    {
        var relatedMetadata = _context.MetadataProvider.GetEntityMetadata(relationship.TargetEntityType);
        var columns = string.Join(", ", relatedMetadata.Properties.Values.Select(p => p.ColumnName));

        string whereClause;
        
        if (relationship.RelationshipType == RelationshipType.ManyToOne || 
            (relationship.RelationshipType == RelationshipType.OneToOne && relationship.IsOwner))
        {
            // For ManyToOne and owning side of OneToOne, join column is in the current entity
            if (relationship.JoinColumn == null)
            {
                throw new InvalidOperationException($"Join column not found for relationship '{relationship.PropertyName}'");
            }

            var relatedPrimaryKey = relatedMetadata.Properties.Values.FirstOrDefault(p => p.IsPrimaryKey);
            if (relatedPrimaryKey == null)
            {
                throw new InvalidOperationException($"Primary key not found for entity '{relationship.TargetEntityType.Name}'");
            }

            whereClause = $"{relatedPrimaryKey.ColumnName} = @JoinColumnValue";
        }
        else
        {
            // For OneToMany and inverse side of OneToOne, join column is in the related entity
            var primaryKey = entityMetadata.Properties.Values.FirstOrDefault(p => p.IsPrimaryKey);
            if (primaryKey == null)
            {
                throw new InvalidOperationException($"Primary key not found for entity '{entityMetadata.EntityType.Name}'");
            }

            var joinColumnName = relationship.JoinColumn?.Name ?? $"{entityMetadata.TableName}_id";
            whereClause = $"{joinColumnName} = @PrimaryKeyValue";
        }

        return $"SELECT {columns} FROM {relatedMetadata.TableName} WHERE {whereClause}";
    }

    private string GenerateLoadCollectionSql(RelationshipMetadata relationship, object entity, EntityMetadata entityMetadata)
    {
        var relatedMetadata = _context.MetadataProvider.GetEntityMetadata(relationship.TargetEntityType);
        var columns = string.Join(", ", relatedMetadata.Properties.Values.Select(p => p.ColumnName));

        string whereClause;

        if (relationship.RelationshipType == RelationshipType.OneToMany)
        {
            // For OneToMany, join column is in the related entity
            var primaryKey = entityMetadata.Properties.Values.FirstOrDefault(p => p.IsPrimaryKey);
            if (primaryKey == null)
            {
                throw new InvalidOperationException($"Primary key not found for entity '{entityMetadata.EntityType.Name}'");
            }

            var joinColumnName = relationship.JoinColumn?.Name ?? $"{entityMetadata.TableName}_id";
            whereClause = $"{joinColumnName} = @PrimaryKeyValue";
        }
        else if (relationship.RelationshipType == RelationshipType.ManyToMany)
        {
            // For ManyToMany, use join table
            if (relationship.JoinTable == null)
            {
                throw new InvalidOperationException($"Join table not found for relationship '{relationship.PropertyName}'");
            }

            var primaryKey = entityMetadata.Properties.Values.FirstOrDefault(p => p.IsPrimaryKey);
            if (primaryKey == null)
            {
                throw new InvalidOperationException($"Primary key not found for entity '{entityMetadata.EntityType.Name}'");
            }

            var relatedPrimaryKey = relatedMetadata.Properties.Values.FirstOrDefault(p => p.IsPrimaryKey);
            if (relatedPrimaryKey == null)
            {
                throw new InvalidOperationException($"Primary key not found for entity '{relationship.TargetEntityType.Name}'");
            }

            var joinColumn = relationship.JoinTable.JoinColumns.FirstOrDefault() ?? "id";
            var inverseJoinColumn = relationship.JoinTable.InverseJoinColumns.FirstOrDefault() ?? "id";

            return $@"SELECT {string.Join(", ", relatedMetadata.Properties.Values.Select(p => $"t.{p.ColumnName}"))}
                     FROM {relatedMetadata.TableName} t
                     INNER JOIN {relationship.JoinTable.Name} jt ON t.{relatedPrimaryKey.ColumnName} = jt.{inverseJoinColumn}
                     WHERE jt.{joinColumn} = @PrimaryKeyValue";
        }
        else
        {
            throw new InvalidOperationException($"Unexpected relationship type '{relationship.RelationshipType}' for collection loading");
        }

        return $"SELECT {columns} FROM {relatedMetadata.TableName} WHERE {whereClause}";
    }

    private object CreateLoadParameters(RelationshipMetadata relationship, object entity, EntityMetadata entityMetadata)
    {
        var primaryKey = entityMetadata.Properties.Values.FirstOrDefault(p => p.IsPrimaryKey);
        if (primaryKey == null)
        {
            throw new InvalidOperationException($"Primary key not found for entity '{entityMetadata.EntityType.Name}'");
        }

        var primaryKeyValue = primaryKey.PropertyInfo?.GetValue(entity);

        if (relationship.RelationshipType == RelationshipType.ManyToOne || 
            (relationship.RelationshipType == RelationshipType.OneToOne && relationship.IsOwner))
        {
            // For ManyToOne and owning side of OneToOne, we need the value of the join column property
            if (relationship.JoinColumn == null)
            {
                throw new InvalidOperationException($"Join column not found for relationship '{relationship.PropertyName}'");
            }

            var joinColumnProperty = entityMetadata.Properties.Values.FirstOrDefault(p => p.ColumnName == relationship.JoinColumn.Name);
            if (joinColumnProperty == null)
            {
                throw new InvalidOperationException($"Join column property '{relationship.JoinColumn.Name}' not found in entity '{entityMetadata.EntityType.Name}'");
            }

            var joinColumnValue = joinColumnProperty.PropertyInfo?.GetValue(entity);
            return new { JoinColumnValue = joinColumnValue };
        }

        return new { PrimaryKeyValue = primaryKeyValue };
    }
}
