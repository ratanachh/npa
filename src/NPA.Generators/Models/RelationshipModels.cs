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
    
    /// <summary>Cascade types as bit flags (matches NPA.Core.Annotations.CascadeType)</summary>
    public int CascadeTypes { get; set; }
    
    /// <summary>Fetch type (0=Eager, 1=Lazy, matches NPA.Core.Annotations.FetchType)</summary>
    public int FetchType { get; set; } = 1; // Lazy by default
    
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
    public bool IsEager => FetchType == 0;
    
    /// <summary>True if cascade persist is enabled</summary>
    public bool HasCascadePersist => (CascadeTypes & (1 << 0)) != 0;
    
    /// <summary>True if cascade merge is enabled</summary>
    public bool HasCascadeMerge => (CascadeTypes & (1 << 1)) != 0;
    
    /// <summary>True if cascade remove is enabled</summary>
    public bool HasCascadeRemove => (CascadeTypes & (1 << 2)) != 0;
    
    /// <summary>True if cascade refresh is enabled</summary>
    public bool HasCascadeRefresh => (CascadeTypes & (1 << 3)) != 0;
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
