namespace NPA.Core.Metadata;

/// <summary>
/// Contains metadata about a join table (for many-to-many relationships).
/// </summary>
public class JoinTableMetadata
{
    /// <summary>
    /// Gets or sets the name of the join table.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema of the join table.
    /// </summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the foreign key columns referencing the owning entity.
    /// </summary>
    public List<string> JoinColumns { get; set; } = new();

    /// <summary>
    /// Gets or sets the foreign key columns referencing the target entity.
    /// </summary>
    public List<string> InverseJoinColumns { get; set; } = new();

    /// <summary>
    /// Gets the full table name including schema (if specified).
    /// </summary>
    public string FullName => string.IsNullOrEmpty(Schema) ? Name : $"{Schema}.{Name}";
}

