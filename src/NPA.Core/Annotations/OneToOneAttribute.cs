namespace NPA.Core.Annotations;

/// <summary>
/// Defines a one-to-one association to another entity.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class OneToOneAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the property on the target entity that owns the relationship.
    /// This is used for bidirectional relationships.
    /// </summary>
    public string? MappedBy { get; set; }

    /// <summary>
    /// Gets or sets the cascade operations that should be applied to the associated entity.
    /// </summary>
    public CascadeType Cascade { get; set; } = CascadeType.None;

    /// <summary>
    /// Gets or sets the fetch strategy for the association.
    /// Defaults to Eager for one-to-one relationships.
    /// </summary>
    public FetchType Fetch { get; set; } = FetchType.Eager;

    /// <summary>
    /// Gets or sets whether the association is optional (nullable).
    /// If false, the foreign key column will be marked as NOT NULL.
    /// </summary>
    public bool Optional { get; set; } = true;
}
