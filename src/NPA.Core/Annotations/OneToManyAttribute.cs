namespace NPA.Core.Annotations;

/// <summary>
/// Marks a property as a one-to-many relationship.
/// This is the "one" side of a bidirectional relationship where one entity can have many related entities.
/// </summary>
/// <example>
/// <code>
/// [Entity]
/// public class User
/// {
///     [Id]
///     public int Id { get; set; }
///     
///     [OneToMany(mappedBy: "User", cascade: CascadeType.All)]
///     public ICollection&lt;Order&gt; Orders { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property)]
public sealed class OneToManyAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the property name on the target entity that owns the relationship.
    /// This is the inverse side of the bidirectional relationship.
    /// </summary>
    public string MappedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cascade operations that should be applied to related entities.
    /// Default is CascadeType.None.
    /// </summary>
    public CascadeType Cascade { get; set; } = CascadeType.None;

    /// <summary>
    /// Gets or sets the fetch strategy for loading related entities.
    /// Default is FetchType.Lazy.
    /// </summary>
    public FetchType Fetch { get; set; } = FetchType.Lazy;

    /// <summary>
    /// Gets or sets whether orphan removal should be applied.
    /// If true, when a child entity is removed from the collection, it will be deleted from the database.
    /// Default is false.
    /// </summary>
    public bool OrphanRemoval { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="OneToManyAttribute"/> class.
    /// </summary>
    public OneToManyAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OneToManyAttribute"/> class with the specified mapped by property.
    /// </summary>
    /// <param name="mappedBy">The property name on the target entity that owns the relationship.</param>
    public OneToManyAttribute(string mappedBy)
    {
        MappedBy = mappedBy ?? throw new ArgumentNullException(nameof(mappedBy));
    }
}

