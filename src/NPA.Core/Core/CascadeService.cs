using System.Collections;
using NPA.Core.Annotations;
using NPA.Core.Metadata;

namespace NPA.Core.Core;

/// <summary>
/// Implementation of cascade operations for related entities.
/// Handles automatic propagation of entity state changes based on cascade configuration.
/// </summary>
public sealed class CascadeService : ICascadeService
{
    /// <inheritdoc />
    public async Task CascadePersistAsync(object entity, EntityMetadata metadata, IEntityManager entityManager, HashSet<object> visited)
    {
        if (entity == null || visited.Contains(entity))
            return;

        visited.Add(entity);

        foreach (var relationship in metadata.Relationships.Values)
        {
            // Skip if persist cascade is not enabled
            if (!relationship.CascadeType.HasFlag(CascadeType.Persist))
                continue;

            var relatedValue = GetRelatedValue(entity, relationship);
            if (relatedValue == null)
                continue;

            // Handle collections (OneToMany, ManyToMany)
            if (relatedValue is IEnumerable enumerable and not string)
            {
                foreach (var relatedEntity in enumerable)
                {
                    if (relatedEntity != null && !visited.Contains(relatedEntity))
                    {
                        await entityManager.PersistAsync(relatedEntity);
                        
                        // Recursively cascade to related entities
                        var relatedMetadata = entityManager.MetadataProvider.GetEntityMetadata(relatedEntity.GetType());
                        await CascadePersistAsync(relatedEntity, relatedMetadata, entityManager, visited);
                    }
                }
            }
            // Handle single entity (ManyToOne, OneToOne)
            else if (!visited.Contains(relatedValue))
            {
                await entityManager.PersistAsync(relatedValue);
                
                // Recursively cascade to related entities
                var relatedMetadata = entityManager.MetadataProvider.GetEntityMetadata(relatedValue.GetType());
                await CascadePersistAsync(relatedValue, relatedMetadata, entityManager, visited);
            }
        }
    }

    /// <inheritdoc />
    public async Task CascadeMergeAsync(object entity, EntityMetadata metadata, IEntityManager entityManager, HashSet<object> visited)
    {
        if (entity == null || visited.Contains(entity))
            return;

        visited.Add(entity);

        foreach (var relationship in metadata.Relationships.Values)
        {
            // Skip if merge cascade is not enabled
            if (!relationship.CascadeType.HasFlag(CascadeType.Merge))
                continue;

            var relatedValue = GetRelatedValue(entity, relationship);
            if (relatedValue == null)
                continue;

            // Handle collections (OneToMany, ManyToMany)
            if (relatedValue is IEnumerable enumerable and not string)
            {
                foreach (var relatedEntity in enumerable)
                {
                    if (relatedEntity != null && !visited.Contains(relatedEntity))
                    {
                        await entityManager.MergeAsync(relatedEntity);
                        
                        // Recursively cascade to related entities
                        var relatedMetadata = entityManager.MetadataProvider.GetEntityMetadata(relatedEntity.GetType());
                        await CascadeMergeAsync(relatedEntity, relatedMetadata, entityManager, visited);
                    }
                }
            }
            // Handle single entity (ManyToOne, OneToOne)
            else if (!visited.Contains(relatedValue))
            {
                await entityManager.MergeAsync(relatedValue);
                
                // Recursively cascade to related entities
                var relatedMetadata = entityManager.MetadataProvider.GetEntityMetadata(relatedValue.GetType());
                await CascadeMergeAsync(relatedValue, relatedMetadata, entityManager, visited);
            }
        }
    }

    /// <inheritdoc />
    public async Task CascadeRemoveAsync(object entity, EntityMetadata metadata, IEntityManager entityManager, HashSet<object> visited)
    {
        if (entity == null || visited.Contains(entity))
            return;

        visited.Add(entity);

        foreach (var relationship in metadata.Relationships.Values)
        {
            // Handle orphan removal for OneToMany relationships
            var shouldRemove = relationship.CascadeType.HasFlag(CascadeType.Remove) ||
                              (relationship.RelationshipType == RelationshipType.OneToMany && relationship.OrphanRemoval);

            if (!shouldRemove)
                continue;

            var relatedValue = GetRelatedValue(entity, relationship);
            if (relatedValue == null)
                continue;

            // Handle collections (OneToMany, ManyToMany)
            if (relatedValue is IEnumerable enumerable and not string)
            {
                // Collect entities to remove (can't modify collection during iteration)
                var entitiesToRemove = new List<object>();
                foreach (var relatedEntity in enumerable)
                {
                    if (relatedEntity != null && !visited.Contains(relatedEntity))
                    {
                        entitiesToRemove.Add(relatedEntity);
                    }
                }

                // Remove collected entities
                foreach (var relatedEntity in entitiesToRemove)
                {
                    // Recursively cascade before removing (depth-first)
                    var relatedMetadata = entityManager.MetadataProvider.GetEntityMetadata(relatedEntity.GetType());
                    await CascadeRemoveAsync(relatedEntity, relatedMetadata, entityManager, visited);
                    
                    await entityManager.RemoveAsync(relatedEntity);
                }
            }
            // Handle single entity (ManyToOne, OneToOne)
            else if (!visited.Contains(relatedValue))
            {
                // Recursively cascade before removing (depth-first)
                var relatedMetadata = entityManager.MetadataProvider.GetEntityMetadata(relatedValue.GetType());
                await CascadeRemoveAsync(relatedValue, relatedMetadata, entityManager, visited);
                
                await entityManager.RemoveAsync(relatedValue);
            }
        }
    }

    /// <inheritdoc />
    public async Task CascadeDetachAsync(object entity, EntityMetadata metadata, IEntityManager entityManager, HashSet<object> visited)
    {
        if (entity == null || visited.Contains(entity))
            return;

        visited.Add(entity);

        foreach (var relationship in metadata.Relationships.Values)
        {
            // Skip if detach cascade is not enabled
            if (!relationship.CascadeType.HasFlag(CascadeType.Detach))
                continue;

            var relatedValue = GetRelatedValue(entity, relationship);
            if (relatedValue == null)
                continue;

            // Handle collections (OneToMany, ManyToMany)
            if (relatedValue is IEnumerable enumerable and not string)
            {
                foreach (var relatedEntity in enumerable)
                {
                    if (relatedEntity != null && !visited.Contains(relatedEntity))
                    {
                        entityManager.ChangeTracker.Untrack(relatedEntity);
                        
                        // Recursively cascade to related entities
                        var relatedMetadata = entityManager.MetadataProvider.GetEntityMetadata(relatedEntity.GetType());
                        await CascadeDetachAsync(relatedEntity, relatedMetadata, entityManager, visited);
                    }
                }
            }
            // Handle single entity (ManyToOne, OneToOne)
            else if (!visited.Contains(relatedValue))
            {
                entityManager.ChangeTracker.Untrack(relatedValue);
                
                // Recursively cascade to related entities
                var relatedMetadata = entityManager.MetadataProvider.GetEntityMetadata(relatedValue.GetType());
                await CascadeDetachAsync(relatedValue, relatedMetadata, entityManager, visited);
            }
        }
    }

    /// <summary>
    /// Gets the value of a relationship property from an entity.
    /// </summary>
    private object? GetRelatedValue(object entity, RelationshipMetadata relationship)
    {
        var entityType = entity.GetType();
        var property = entityType.GetProperty(relationship.PropertyName);
        
        if (property == null)
            return null;

        return property.GetValue(entity);
    }
}
