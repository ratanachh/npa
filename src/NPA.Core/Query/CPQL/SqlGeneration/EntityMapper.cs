namespace NPA.Core.Query.CPQL.SqlGeneration;

/// <summary>
/// Default implementation of entity mapper.
/// </summary>
public sealed class EntityMapper : IEntityMapper
{
    private readonly IEntityResolver _entityResolver;
    private readonly Dictionary<string, string> _aliasToEntityMap = new();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityMapper"/> class.
    /// </summary>
    /// <param name="entityResolver">The entity resolver.</param>
    public EntityMapper(IEntityResolver entityResolver)
    {
        _entityResolver = entityResolver ?? throw new ArgumentNullException(nameof(entityResolver));
    }
    
    /// <summary>
    /// Registers an alias to entity mapping.
    /// </summary>
    /// <param name="alias">The alias.</param>
    /// <param name="entityName">The entity name.</param>
    public void RegisterAlias(string alias, string entityName)
    {
        _aliasToEntityMap[alias] = entityName;
    }
    
    /// <inheritdoc />
    public string GetTableName(string entityName)
    {
        return _entityResolver.GetTableName(entityName);
    }
    
    /// <inheritdoc />
    public string GetColumnName(string? entityAlias, string propertyName)
    {
        if (entityAlias != null && _aliasToEntityMap.TryGetValue(entityAlias, out var entityName))
        {
            var columnName = _entityResolver.GetColumnName(entityName, propertyName);
            return $"{entityAlias}.{columnName}";
        }
        
        // If no alias mapping, return as-is (this shouldn't happen in proper usage)
        return entityAlias != null ? $"{entityAlias}.{propertyName}" : propertyName;
    }
}

