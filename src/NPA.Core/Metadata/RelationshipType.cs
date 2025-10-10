namespace NPA.Core.Metadata;

/// <summary>
/// Defines the type of relationship between entities.
/// </summary>
public enum RelationshipType
{
    /// <summary>
    /// One-to-one relationship: One entity instance is associated with exactly one instance of another entity.
    /// </summary>
    OneToOne,

    /// <summary>
    /// One-to-many relationship: One entity instance can be associated with many instances of another entity.
    /// </summary>
    OneToMany,

    /// <summary>
    /// Many-to-one relationship: Many entity instances can be associated with one instance of another entity.
    /// </summary>
    ManyToOne,

    /// <summary>
    /// Many-to-many relationship: Many entity instances can be associated with many instances of another entity.
    /// Requires a join table.
    /// </summary>
    ManyToMany
}

