using NPA.Core.Annotations;
using System.Reflection;

namespace NPA.Core.Metadata;

/// <summary>
/// Contains metadata information about an entity property.
/// </summary>
public sealed class PropertyMetadata
{
    /// <summary>
    /// Gets or sets the reflection PropertyInfo for this property.
    /// </summary>
    public PropertyInfo PropertyInfo { get; set; } = null!;

    /// <summary>
    /// Gets or sets the property name.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the property type.
    /// </summary>
    public Type PropertyType { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether this property is the primary key.
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Gets or sets the generation type for primary keys.
    /// </summary>
    public GenerationType? GenerationType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column can be null.
    /// </summary>
    public bool IsNullable { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the column has a unique constraint.
    /// </summary>
    public bool IsUnique { get; set; }

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
    /// Gets or sets the database-specific type name.
    /// </summary>
    public string? TypeName { get; set; }
}
