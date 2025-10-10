using NPA.Core.Annotations;

namespace NPA.Core.Metadata;

/// <summary>
/// Contains metadata about a relationship between two entities.
/// </summary>
public class RelationshipMetadata
{
    /// <summary>
    /// Gets or sets the property name in the source entity.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of relationship.
    /// </summary>
    public RelationshipType RelationshipType { get; set; }

    /// <summary>
    /// Gets or sets the target entity type.
    /// </summary>
    public Type TargetEntityType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the property name in the target entity that maps back to the source (for bidirectional relationships).
    /// </summary>
    public string? MappedBy { get; set; }

    /// <summary>
    /// Gets or sets the cascade operations that should be applied.
    /// </summary>
    public CascadeType CascadeType { get; set; } = CascadeType.None;

    /// <summary>
    /// Gets or sets the fetch strategy for loading the relationship.
    /// </summary>
    public FetchType FetchType { get; set; } = FetchType.Lazy;

    /// <summary>
    /// Gets or sets whether the relationship is optional (nullable).
    /// </summary>
    public bool IsOptional { get; set; } = true;

    /// <summary>
    /// Gets or sets whether orphan removal is enabled.
    /// Only applicable for OneToMany relationships.
    /// </summary>
    public bool OrphanRemoval { get; set; } = false;

    /// <summary>
    /// Gets or sets the join column information (for ManyToOne and OneToOne relationships).
    /// </summary>
    public JoinColumnMetadata? JoinColumn { get; set; }

    /// <summary>
    /// Gets or sets the join table information (for ManyToMany relationships).
    /// </summary>
    public JoinTableMetadata? JoinTable { get; set; }

    /// <summary>
    /// Gets or sets whether this is the owning side of the relationship.
    /// The owning side defines the join column or join table.
    /// </summary>
    public bool IsOwner { get; set; } = true;
}

