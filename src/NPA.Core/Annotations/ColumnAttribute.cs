namespace NPA.Core.Annotations;

/// <summary>
/// Maps a property to a database column.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ColumnAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the database column.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the database-specific type name.
    /// </summary>
    public string? TypeName { get; set; }

    /// <summary>
    /// Gets or sets the maximum length of the column.
    /// </summary>
    public int? Length { get; set; }

    /// <summary>
    /// Gets or sets the precision for numeric columns.
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// Gets or sets the scale for numeric columns.
    /// </summary>
    public int? Scale { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column can be null.
    /// </summary>
    public bool IsNullable { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the column has a unique constraint.
    /// </summary>
    public bool IsUnique { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnAttribute"/> class with the specified column name.
    /// </summary>
    /// <param name="name">The name of the database column.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
    public ColumnAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Column name cannot be null or empty.", nameof(name));

        Name = name;
    }
}
