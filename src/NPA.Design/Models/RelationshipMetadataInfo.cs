namespace NPA.Design.Models;

/// <summary>
/// Contains metadata information about entity relationships.
/// </summary>
public class RelationshipMetadataInfo
{
    /// <summary>
    /// Gets or sets the property name that defines the relationship.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the relationship type (e.g., ManyToOne, OneToMany, OneToOne).
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the target entity name.
    /// </summary>
    public string TargetEntity { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the join column name (for ManyToOne/OneToOne).
    /// </summary>
    public string? JoinColumnName { get; set; }
    
    /// <summary>
    /// Gets or sets the referenced column name (for ManyToOne/OneToOne).
    /// </summary>
    public string? ReferencedColumnName { get; set; }
    
    /// <summary>
    /// Gets or sets the mapped-by property name (for OneToMany/OneToOne).
    /// </summary>
    public string? MappedBy { get; set; }
    
    /// <summary>
    /// Gets or sets whether the join column is nullable.
    /// </summary>
    public bool IsNullable { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether the join column should have a UNIQUE constraint.
    /// </summary>
    public bool IsUnique { get; set; } = false;
    
    /// <summary>
    /// Gets or sets whether the join column should be included in INSERT statements.
    /// </summary>
    public bool IsInsertable { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether the join column should be included in UPDATE statements.
    /// </summary>
    public bool IsUpdatable { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether the entity is the owner side of the relationship.
    /// </summary>
    public bool IsOwner { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the join table name (for ManyToMany relationships).
    /// </summary>
    public string? JoinTableName { get; set; }
    
    /// <summary>
    /// Gets or sets the join table schema (for ManyToMany relationships).
    /// </summary>
    public string? JoinTableSchema { get; set; }
    
    /// <summary>
    /// Gets or sets the join columns (for ManyToMany relationships).
    /// </summary>
    public List<string>? JoinColumns { get; set; }
    
    /// <summary>
    /// Gets or sets the inverse join columns (for ManyToMany relationships).
    /// </summary>
    public List<string>? InverseJoinColumns { get; set; }
}

