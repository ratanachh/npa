using NPA.Core.Metadata;

namespace NPA.Core.Query.CPQL;

/// <summary>
/// Default implementation of entity resolver.
/// </summary>
public sealed class EntityResolver : IEntityResolver
{
    private readonly IMetadataProvider _metadataProvider;
    private readonly Dictionary<string, Type> _entityTypeCache = new(StringComparer.OrdinalIgnoreCase);
    
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityResolver"/> class.
    /// </summary>
    /// <param name="metadataProvider">The metadata provider.</param>
    public EntityResolver(IMetadataProvider metadataProvider)
    {
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
    }
    
    /// <summary>
    /// Registers an entity type with its name for resolution.
    /// </summary>
    /// <param name="entityName">The entity name.</param>
    /// <param name="entityType">The entity type.</param>
    public void RegisterEntity(string entityName, Type entityType)
    {
        _entityTypeCache[entityName] = entityType;
    }
    
    /// <inheritdoc />
    public EntityMetadata GetEntityMetadata(string entityName)
    {
        if (!_entityTypeCache.TryGetValue(entityName, out var entityType))
        {
            throw new InvalidOperationException($"Entity '{entityName}' is not registered");
        }
        
        var method = typeof(IMetadataProvider).GetMethod(nameof(IMetadataProvider.GetEntityMetadata))!
            .MakeGenericMethod(entityType);
        
        var metadata = method.Invoke(_metadataProvider, null);
        
        return (EntityMetadata)metadata!;
    }
    
    /// <inheritdoc />
    public string GetTableName(string entityName)
    {
        var metadata = GetEntityMetadata(entityName);
        return metadata.FullTableName;
    }
    
    /// <inheritdoc />
    public string GetColumnName(string entityName, string propertyName)
    {
        var metadata = GetEntityMetadata(entityName);
        
        if (metadata.Properties.TryGetValue(propertyName, out var propertyMetadata))
        {
            return propertyMetadata.ColumnName;
        }
        
        throw new InvalidOperationException($"Property '{propertyName}' not found on entity '{entityName}'");
    }
    
    /// <inheritdoc />
    public bool IsRelationshipProperty(string entityName, string propertyName)
    {
        var metadata = GetEntityMetadata(entityName);
        return metadata.Relationships.ContainsKey(propertyName);
    }
    
    /// <inheritdoc />
    public RelationshipMetadata? GetRelationshipMetadata(string entityName, string propertyName)
    {
        var metadata = GetEntityMetadata(entityName);
        
        if (metadata.Relationships.TryGetValue(propertyName, out var relationshipMetadata))
        {
            return relationshipMetadata;
        }
        
        return null;
    }
}

