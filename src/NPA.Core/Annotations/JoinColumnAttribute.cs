namespace NPA.Core.Annotations;

/// <summary>
/// Specifies the foreign key column used for a relationship.
/// Used with ManyToOne and OneToOne relationships.
/// </summary>
/// <example>
/// <code>
/// [ManyToOne]
/// [JoinColumn("user_id", referencedColumnName: "id", nullable: false)]
/// public User User { get; set; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class JoinColumnAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the foreign key column.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the column referenced in the target table.
    /// Default is "id" (the primary key of the target entity).
    /// </summary>
    public string ReferencedColumnName { get; set; } = "id";

    /// <summary>
    /// Gets or sets whether the foreign key column should have a UNIQUE constraint.
    /// Default is false.
    /// </summary>
    public bool Unique { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the foreign key column can be NULL.
    /// Default is true.
    /// </summary>
    public bool Nullable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the foreign key column should be included in INSERT statements.
    /// Default is true.
    /// </summary>
    public bool Insertable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the foreign key column should be included in UPDATE statements.
    /// Default is true.
    /// </summary>
    public bool Updatable { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="JoinColumnAttribute"/> class.
    /// </summary>
    public JoinColumnAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JoinColumnAttribute"/> class with the specified column name.
    /// </summary>
    /// <param name="name">The name of the foreign key column.</param>
    public JoinColumnAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}

