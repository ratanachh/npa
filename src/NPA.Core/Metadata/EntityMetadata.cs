namespace NPA.Core.Metadata;

/// <summary>
/// Contains metadata information about an entity type.
/// </summary>
public sealed class EntityMetadata
{
    /// <summary>
    /// Gets or sets the entity type.
    /// </summary>
    public Type EntityType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema name.
    /// </summary>
    public string? SchemaName { get; set; }

    /// <summary>
    /// Gets or sets the primary key property name.
    /// </summary>
    public string PrimaryKeyProperty { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the properties metadata.
    /// </summary>
    public Dictionary<string, PropertyMetadata> Properties { get; set; } = new();

    /// <summary>
    /// Gets or sets the relationship metadata.
    /// </summary>
    public Dictionary<string, RelationshipMetadata> Relationships { get; set; } = new();

    /// <summary>
    /// Gets the full table name including schema if specified.
    /// </summary>
    public string FullTableName => string.IsNullOrEmpty(SchemaName) ? TableName : $"{SchemaName}.{TableName}";
}
