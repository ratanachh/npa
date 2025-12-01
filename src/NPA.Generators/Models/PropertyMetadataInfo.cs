namespace NPA.Generators.Models;

/// <summary>
/// Contains metadata information about an entity property.
/// </summary>
public class PropertyMetadataInfo
{
    /// <summary>
    /// Gets or sets the property name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the property type name.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the database column name.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets a value indicating whether the property is nullable.
    /// </summary>
    public bool IsNullable { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the property is a primary key.
    /// </summary>
    public bool IsPrimaryKey { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the property is an identity column.
    /// </summary>
    public bool IsIdentity { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the property is required.
    /// </summary>
    public bool IsRequired { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the property has a unique constraint.
    /// </summary>
    public bool IsUnique { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum length for string properties.
    /// </summary>
    public int? Length { get; set; }
    
    /// <summary>
    /// Gets or sets the precision for decimal properties.
    /// </summary>
    public int? Precision { get; set; }
    
    /// <summary>
    /// Gets or sets the scale for decimal properties.
    /// </summary>
    public int? Scale { get; set; }
}

