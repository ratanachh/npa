using System;

namespace NPA.Generators.Models;

/// <summary>
/// Relationship types for entity associations.
/// Mirrors NPA.Core.Metadata.RelationshipType for source generation compatibility.
/// </summary>
public enum RelationshipType
{
    /// <summary>One-to-one relationship</summary>
    OneToOne,
    
    /// <summary>One-to-many relationship</summary>
    OneToMany,
    
    /// <summary>Many-to-one relationship</summary>
    ManyToOne,
    
    /// <summary>Many-to-many relationship</summary>
    ManyToMany
}

/// <summary>
/// Defines the cascade operations that should be applied to related entities.
/// Mirrors NPA.Core.Annotations.CascadeType for source generation compatibility.
/// </summary>
[Flags]
public enum CascadeType
{
    /// <summary>No cascade operations.</summary>
    None = 0,
    
    /// <summary>Cascade persist (save) operations.</summary>
    Persist = 1 << 0,
    
    /// <summary>Cascade merge (update) operations.</summary>
    Merge = 1 << 1,
    
    /// <summary>Cascade remove (delete) operations.</summary>
    Remove = 1 << 2,
    
    /// <summary>Cascade refresh operations.</summary>
    Refresh = 1 << 3,
    
    /// <summary>Cascade detach operations.</summary>
    Detach = 1 << 4,
    
    /// <summary>Cascade all operations.</summary>
    All = Persist | Merge | Remove | Refresh | Detach
}

/// <summary>
/// Defines the loading strategy for relationship data.
/// Mirrors NPA.Core.Annotations.FetchType for source generation compatibility.
/// </summary>
public enum FetchType
{
    /// <summary>Load the relationship eagerly (immediately with the owning entity).</summary>
    Eager = 0,
    
    /// <summary>Load the relationship lazily (on-demand when accessed).</summary>
    Lazy = 1
}

/// <summary>
/// Lightweight relationship metadata for source generation.
/// Mirrors NPA.Core.Metadata.RelationshipMetadata but optimized for code generation.
/// </summary>
public class RelationshipMetadata
{
    /// <summary>Property name (e.g., "Customer", "Orders")</summary>
    public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>Property type as string</summary>
    public string PropertyType { get; set; } = string.Empty;
    
    /// <summary>Relationship type</summary>
    public RelationshipType Type { get; set; }
    
    /// <summary>Target entity type name (e.g., "Customer", "Order")</summary>
    public string TargetEntityType { get; set; } = string.Empty;
    
    /// <summary>Target entity full type (e.g., "MyApp.Customer")</summary>
    public string TargetEntityFullType { get; set; } = string.Empty;
    
    /// <summary>MappedBy property for bidirectional relationships</summary>
    public string? MappedBy { get; set; }
    
    /// <summary>Cascade types (matches NPA.Core.Annotations.CascadeType)</summary>
    public CascadeType CascadeTypes { get; set; }
    
    /// <summary>Fetch type (matches NPA.Core.Annotations.FetchType)</summary>
    public FetchType FetchType { get; set; } = FetchType.Lazy;
    
    /// <summary>Whether to remove orphaned entities</summary>
    public bool OrphanRemoval { get; set; }
    
    /// <summary>Whether the relationship is optional</summary>
    public bool Optional { get; set; } = true;
    
    /// <summary>Join column configuration</summary>
    public JoinColumnInfo? JoinColumn { get; set; }
    
    /// <summary>Join table configuration for ManyToMany</summary>
    public JoinTableInfo? JoinTable { get; set; }
    
    /// <summary>Whether property is a collection</summary>
    public bool IsCollection { get; set; }
    
    /// <summary>Whether this side owns the relationship</summary>
    public bool IsOwner { get; set; }
    
    // Helper properties
    /// <summary>True if fetch type is Eager</summary>
    public bool IsEager => FetchType == FetchType.Eager;
    
    /// <summary>True if cascade persist is enabled</summary>
    public bool HasCascadePersist => (CascadeTypes & CascadeType.Persist) != 0;
    
    /// <summary>True if cascade merge is enabled</summary>
    public bool HasCascadeMerge => (CascadeTypes & CascadeType.Merge) != 0;
    
    /// <summary>True if cascade remove is enabled</summary>
    public bool HasCascadeRemove => (CascadeTypes & CascadeType.Remove) != 0;
    
    /// <summary>True if cascade refresh is enabled</summary>
    public bool HasCascadeRefresh => (CascadeTypes & CascadeType.Refresh) != 0;
}

/// <summary>
/// Join column information for foreign key mapping.
/// Mirrors NPA.Core.Metadata.JoinColumnMetadata.
/// </summary>
public class JoinColumnInfo
{
    /// <summary>Column name</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>Referenced column name</summary>
    public string ReferencedColumnName { get; set; } = "id";
    
    /// <summary>Whether column is nullable</summary>
    public bool Nullable { get; set; } = true;
    
    /// <summary>Whether column is unique</summary>
    public bool Unique { get; set; }
    
    /// <summary>Whether column is insertable</summary>
    public bool Insertable { get; set; } = true;
    
    /// <summary>Whether column is updatable</summary>
    public bool Updatable { get; set; } = true;
}

/// <summary>
/// Join table information for many-to-many relationships.
/// Mirrors NPA.Core.Metadata.JoinTableMetadata.
/// </summary>
public class JoinTableInfo
{
    /// <summary>Table name</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>Schema name</summary>
    public string? Schema { get; set; }
    
    /// <summary>Join columns (owner side)</summary>
    public string[] JoinColumns { get; set; } = Array.Empty<string>();
    
    /// <summary>Inverse join columns (target side)</summary>
    public string[] InverseJoinColumns { get; set; } = Array.Empty<string>();
}
