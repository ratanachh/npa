namespace NPA.Core.Metadata;

/// <summary>
/// Contains metadata about a join column (foreign key).
/// </summary>
public class JoinColumnMetadata
{
    /// <summary>
    /// Gets or sets the name of the foreign key column.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the referenced column in the target table.
    /// </summary>
    public string ReferencedColumnName { get; set; } = "id";

    /// <summary>
    /// Gets or sets whether the foreign key column should have a UNIQUE constraint.
    /// </summary>
    public bool Unique { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the foreign key column can be NULL.
    /// </summary>
    public bool Nullable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the foreign key column should be included in INSERT statements.
    /// </summary>
    public bool Insertable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the foreign key column should be included in UPDATE statements.
    /// </summary>
    public bool Updatable { get; set; } = true;
}

