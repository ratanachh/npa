namespace NPA.Generators.Models;

/// <summary>
/// Contains metadata information about an entity including table mapping and properties.
/// </summary>
public class EntityMetadataInfo
{
    /// <summary>
    /// Gets or sets the entity name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the entity namespace.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the fully qualified entity name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the database table name.
    /// </summary>
    public string TableName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the database schema name.
    /// </summary>
    public string? SchemaName { get; set; }
    
    /// <summary>
    /// Gets or sets the list of property metadata.
    /// </summary>
    public List<PropertyMetadataInfo> Properties { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the list of relationship metadata.
    /// </summary>
    public List<RelationshipMetadataInfo> Relationships { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the list of named queries defined on this entity.
    /// </summary>
    public List<NamedQueryInfo> NamedQueries { get; set; } = new();
}

