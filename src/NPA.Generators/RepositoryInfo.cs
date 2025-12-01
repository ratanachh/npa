using Microsoft.CodeAnalysis;

namespace NPA.Generators;

internal class RepositoryInfo
{
    public string InterfaceName { get; set; } = string.Empty;
    public string FullInterfaceName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string KeyType { get; set; } = string.Empty;
    public List<MethodInfo> Methods { get; set; } = new();
    public bool HasCompositeKey { get; set; }
    public List<string> CompositeKeyProperties { get; set; } = new();
    public List<ManyToManyRelationshipInfo> ManyToManyRelationships { get; set; } = new();
    public MultiTenantInfo? MultiTenantInfo { get; set; }
    public EntityMetadataInfo? EntityMetadata { get; set; }
    public Dictionary<string, EntityMetadataInfo> EntitiesMetadata { get; set; } = new();

    // Relationship-aware repository generation
    public List<Models.RelationshipMetadata> Relationships { get; set; } = new();
    public bool HasRelationships => Relationships != null && Relationships.Count > 0;
    
    // Compilation for extracting relationships from related entities
    public Compilation? Compilation { get; set; }

    // Eager loading support
    public bool HasEagerRelationships => Relationships != null && Relationships.Any(r => r.FetchType == Models.FetchType.Eager && (r.IsOwner || string.IsNullOrEmpty(r.MappedBy)));
    public List<Models.RelationshipMetadata> EagerRelationships => Relationships?.Where(r => r.FetchType == Models.FetchType.Eager && (r.IsOwner || string.IsNullOrEmpty(r.MappedBy))).ToList() ?? new();

    // Cascade operations
    public bool HasCascadeRelationships => Relationships != null && Relationships.Any(r => r.CascadeTypes != 0);
    public List<Models.RelationshipMetadata> CascadeRelationships => Relationships?.Where(r => r.CascadeTypes != 0).ToList() ?? new();

    // Orphan removal support
    public bool HasOrphanRemovalRelationships => Relationships != null && Relationships.Any(r => r.OrphanRemoval);
    public List<Models.RelationshipMetadata> OrphanRemovalRelationships => Relationships?.Where(r => r.OrphanRemoval).ToList() ?? new();
}