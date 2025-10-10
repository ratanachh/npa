using NPA.Core.Metadata;

namespace BasicUsage.Features;

/// <summary>
/// Demonstrates Phase 2.1 relationship mapping features.
/// </summary>
public static class RelationshipDemo
{
    public static void ShowRelationshipMetadata()
    {
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  Phase 2.1: Relationship Mapping Demonstration");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine();

        var metadataProvider = new MetadataProvider();

        // User entity with OneToMany relationship
        var userMetadata = metadataProvider.GetEntityMetadata<User>();
        Console.WriteLine("📊 User Entity Relationships:");
        Console.WriteLine($"  Table: {userMetadata.TableName}");
        Console.WriteLine($"  Relationships: {userMetadata.Relationships.Count}");
        
        if (userMetadata.Relationships.TryGetValue("Orders", out var ordersRel))
        {
            Console.WriteLine();
            Console.WriteLine("  OneToMany: User → Orders");
            Console.WriteLine($"    Type: {ordersRel.RelationshipType}");
            Console.WriteLine($"    Target: {ordersRel.TargetEntityType.Name}");
            Console.WriteLine($"    MappedBy: {ordersRel.MappedBy}");
            Console.WriteLine($"    Cascade: {ordersRel.CascadeType}");
            Console.WriteLine($"    Fetch: {ordersRel.FetchType}");
            Console.WriteLine($"    ✅ One user can have many orders");
        }

        Console.WriteLine();

        // Order entity with ManyToOne relationship
        var orderMetadata = metadataProvider.GetEntityMetadata<Entities.Order>();
        Console.WriteLine("📊 Order Entity Relationships:");
        Console.WriteLine($"  Table: {orderMetadata.TableName}");
        Console.WriteLine($"  Relationships: {orderMetadata.Relationships.Count}");
        
        if (orderMetadata.Relationships.TryGetValue("User", out var userRel))
        {
            Console.WriteLine();
            Console.WriteLine("  ManyToOne: Order → User");
            Console.WriteLine($"    Type: {userRel.RelationshipType}");
            Console.WriteLine($"    Target: {userRel.TargetEntityType.Name}");
            Console.WriteLine($"    Fetch: {userRel.FetchType}");
            Console.WriteLine($"    Join Column: {userRel.JoinColumn?.Name}");
            Console.WriteLine($"    Referenced: {userRel.JoinColumn?.ReferencedColumnName}");
            Console.WriteLine($"    Nullable: {userRel.JoinColumn?.Nullable}");
            Console.WriteLine($"    ✅ Many orders belong to one user");
        }

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  ✅ Relationship metadata automatically detected!");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine();
    }
}

