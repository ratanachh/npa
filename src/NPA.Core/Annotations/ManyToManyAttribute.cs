namespace NPA.Core.Annotations;

/// <summary>
/// Marks a property as a many-to-many relationship.
/// This relationship requires a join table to store the associations between entities.
/// </summary>
/// <example>
/// <code>
/// [Entity]
/// public class User
/// {
///     [Id]
///     public int Id { get; set; }
///     
///     [ManyToMany]
///     [JoinTable("user_roles", 
///         JoinColumns = new[] { "user_id" }, 
///         InverseJoinColumns = new[] { "role_id" })]
///     public ICollection&lt;Role&gt; Roles { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ManyToManyAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the property name on the target entity that owns the relationship.
    /// This is used for bidirectional relationships. The owner side defines the join table.
    /// </summary>
    public string MappedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cascade operations that should be applied to related entities.
    /// Default is CascadeType.None.
    /// </summary>
    public CascadeType Cascade { get; set; } = CascadeType.None;

    /// <summary>
    /// Gets or sets the fetch strategy for loading related entities.
    /// Default is FetchType.Lazy (many-to-many relationships are typically loaded on demand).
    /// </summary>
    public FetchType Fetch { get; set; } = FetchType.Lazy;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManyToManyAttribute"/> class.
    /// </summary>
    public ManyToManyAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManyToManyAttribute"/> class with the specified mapped by property.
    /// </summary>
    /// <param name="mappedBy">The property name on the target entity that owns the relationship.</param>
    public ManyToManyAttribute(string mappedBy)
    {
        MappedBy = mappedBy ?? throw new ArgumentNullException(nameof(mappedBy));
    }
}

