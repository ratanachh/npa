namespace NPA.Core.Annotations;

/// <summary>
/// Specifies the join table used for a many-to-many relationship.
/// Defines the intermediate table that stores the associations between two entities.
/// </summary>
/// <example>
/// <code>
/// [ManyToMany]
/// [JoinTable("user_roles", 
///     schema: "public",
///     JoinColumns = new[] { "user_id" }, 
///     InverseJoinColumns = new[] { "role_id" })]
/// public ICollection&lt;Role&gt; Roles { get; set; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property)]
public sealed class JoinTableAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the join table.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema of the join table.
    /// If not specified, uses the default schema.
    /// </summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the names of the foreign key columns referencing the owning entity.
    /// These columns link to the entity on which the attribute is placed.
    /// </summary>
    public string[] JoinColumns { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the names of the foreign key columns referencing the target entity.
    /// These columns link to the related entity.
    /// </summary>
    public string[] InverseJoinColumns { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="JoinTableAttribute"/> class.
    /// </summary>
    public JoinTableAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JoinTableAttribute"/> class with the specified table name.
    /// </summary>
    /// <param name="name">The name of the join table.</param>
    public JoinTableAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}

