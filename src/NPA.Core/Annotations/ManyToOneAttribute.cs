namespace NPA.Core.Annotations;

/// <summary>
/// Marks a property as a many-to-one relationship.
/// This is the "many" side of a bidirectional relationship where many entities can reference one entity.
/// </summary>
/// <example>
/// <code>
/// [Entity]
/// public class Order
/// {
///     [Id]
///     public int Id { get; set; }
///     
///     [ManyToOne(fetch: FetchType.Eager)]
///     [JoinColumn("user_id")]
///     public User User { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ManyToOneAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the cascade operations that should be applied to the related entity.
    /// Default is CascadeType.None.
    /// </summary>
    public CascadeType Cascade { get; set; } = CascadeType.None;

    /// <summary>
    /// Gets or sets the fetch strategy for loading the related entity.
    /// Default is FetchType.Eager (many-to-one relationships are typically loaded immediately).
    /// </summary>
    public FetchType Fetch { get; set; } = FetchType.Eager;

    /// <summary>
    /// Gets or sets whether the relationship is optional (nullable).
    /// If false, the relationship must always be present (NOT NULL constraint).
    /// Default is true.
    /// </summary>
    public bool Optional { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManyToOneAttribute"/> class.
    /// </summary>
    public ManyToOneAttribute()
    {
    }
}

