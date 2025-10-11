using NPA.Core.Annotations;
using NPA.Core.Metadata;

namespace SourceGeneratorDemo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║       NPA Source Generator Demo - Phase 1.6 & 2.6         ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        DemoRepositoryGenerator();
        Console.WriteLine();
        DemoMetadataGenerator();
        
        await Task.CompletedTask;
    }

    static void DemoRepositoryGenerator()
    {
        Console.WriteLine("┌─ 1️⃣  Repository Generator (Phase 1.6) ─────────────────┐");
        Console.WriteLine("│ Generates repository implementations from interfaces  │");
        Console.WriteLine("└───────────────────────────────────────────────────────┘");
        Console.WriteLine();
        
        Console.WriteLine("📝 Example Interface:");
        Console.WriteLine("   [Repository(typeof(User))]");
        Console.WriteLine("   public interface IUserRepository");
        Console.WriteLine("   {");
        Console.WriteLine("       Task<IEnumerable<User>> GetAllAsync();");
        Console.WriteLine("       Task<User?> GetByIdAsync(int id);");
        Console.WriteLine("       Task<IEnumerable<User>> FindByUsernameAsync(string username);");
        Console.WriteLine("   }");
        Console.WriteLine();
        
        Console.WriteLine("⚙️  Generated Implementation:");
        Console.WriteLine("   public partial class UserRepository : IUserRepository");
        Console.WriteLine("   {");
        Console.WriteLine("       private readonly IEntityManager _entityManager;");
        Console.WriteLine("       // ... auto-generated CRUD methods");
        Console.WriteLine("   }");
        Console.WriteLine();
        
        Console.WriteLine("📋 Convention-Based Generation:");
        Console.WriteLine("   • GetAllAsync()          → SELECT * FROM users");
        Console.WriteLine("   • GetByIdAsync(id)       → SELECT * WHERE id = @id");
        Console.WriteLine("   • FindBy{Property}Async  → WHERE {property} = @value");
        Console.WriteLine("   • SaveAsync(entity)      → EntityManager.PersistAsync");
        Console.WriteLine("   • UpdateAsync(entity)    → EntityManager.MergeAsync");
        Console.WriteLine("   • DeleteAsync(id)        → EntityManager.RemoveAsync");
        Console.WriteLine();
        
        Console.WriteLine("✅ Type-safe implementations");
        Console.WriteLine("✅ Zero runtime overhead");
        Console.WriteLine("✅ Full IntelliSense support");
    }

    static void DemoMetadataGenerator()
    {
        Console.WriteLine("┌─ 2️⃣  Metadata Generator (Phase 2.6) ───────────────────┐");
        Console.WriteLine("│ Generates compile-time entity metadata (0 reflection) │");
        Console.WriteLine("└───────────────────────────────────────────────────────┘");
        Console.WriteLine();
        
        Console.WriteLine("📝 Entity Definition:");
        Console.WriteLine("   [Entity]");
        Console.WriteLine("   [Table(\"users\")]");
        Console.WriteLine("   public class User");
        Console.WriteLine("   {");
        Console.WriteLine("       [Id]");
        Console.WriteLine("       [GeneratedValue(GenerationType.Identity)]");
        Console.WriteLine("       public int Id { get; set; }");
        Console.WriteLine("       ");
        Console.WriteLine("       [Column(\"username\")]");
        Console.WriteLine("       public string Username { get; set; }");
        Console.WriteLine("   }");
        Console.WriteLine();
        
        Console.WriteLine("⚙️  Generated Metadata Provider:");
        Console.WriteLine("   namespace NPA.Generated;");
        Console.WriteLine("   ");
        Console.WriteLine("   public static class GeneratedMetadataProvider");
        Console.WriteLine("   {");
        Console.WriteLine("       public static EntityMetadata? GetMetadata(Type type)");
        Console.WriteLine("       public static IEnumerable<EntityMetadata> GetAllMetadata()");
        Console.WriteLine("   }");
        Console.WriteLine();

        // Try to access generated metadata
        Console.WriteLine("🔍 Accessing Generated Metadata:");
        try
        {
            // Use reflection to check if the generated type exists
            var generatedType = Type.GetType("NPA.Generated.GeneratedMetadataProvider, SourceGeneratorDemo");
            if (generatedType != null)
            {
                var getMetadataMethod = generatedType.GetMethod("GetMetadata");
                var getAllMetadataMethod = generatedType.GetMethod("GetAllMetadata");
                
                if (getMetadataMethod != null && getAllMetadataMethod != null)
                {
                    // Get metadata for User entity
                    var userMetadata = getMetadataMethod.Invoke(null, new object[] { typeof(User) });
                    
                    if (userMetadata is EntityMetadata metadata)
                    {
                        Console.WriteLine($"   ✅ User Metadata:");
                        Console.WriteLine($"      • Entity Type: {metadata.EntityType.Name}");
                        Console.WriteLine($"      • Table Name: {metadata.TableName}");
                        Console.WriteLine($"      • Primary Key: {metadata.PrimaryKeyProperty}");
                        Console.WriteLine($"      • Properties: {metadata.Properties.Count}");
                        Console.WriteLine();
                        
                        Console.WriteLine("   📊 Property Details:");
                        foreach (var prop in metadata.Properties.Values.Take(3))
                        {
                            Console.WriteLine($"      • {prop.PropertyName} ({prop.PropertyType.Name})");
                            Console.WriteLine($"        - Column: {prop.ColumnName}");
                            Console.WriteLine($"        - Nullable: {prop.IsNullable}");
                            Console.WriteLine($"        - Primary Key: {prop.IsPrimaryKey}");
                        }
                        Console.WriteLine();
                    }
                    
                    // Get all metadata
                    var allMetadata = getAllMetadataMethod.Invoke(null, null);
                    if (allMetadata is System.Collections.IEnumerable enumerable)
                    {
                        var count = enumerable.Cast<object>().Count();
                        Console.WriteLine($"   📦 Total Entities Discovered: {count}");
                    }
                }
            }
            else
            {
                Console.WriteLine("   ℹ️  Metadata provider will be generated after build");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ℹ️  Metadata generation in progress: {ex.Message}");
        }
        
        Console.WriteLine();
        Console.WriteLine("⚡ Performance Benefits:");
        Console.WriteLine("   • 10-100x faster than reflection");
        Console.WriteLine("   • Zero runtime overhead");
        Console.WriteLine("   • O(1) dictionary lookup");
        Console.WriteLine("   • No GC pressure from attribute scanning");
        Console.WriteLine();
        
        Console.WriteLine("✅ Compile-time metadata generation");
        Console.WriteLine("✅ Type-safe property access");
        Console.WriteLine("✅ Automatic entity discovery");
        Console.WriteLine();
        
        Console.WriteLine("─────────────────────────────────────────────────────────");
        Console.WriteLine();
        Console.WriteLine("📂 To view generated code:");
        Console.WriteLine("   1. Build the project (dotnet build)");
        Console.WriteLine("   2. Check: obj/Debug/net8.0/generated/");
        Console.WriteLine("      • NPA.Generators.RepositoryGenerator/");
        Console.WriteLine("        └─ UserRepositoryImplementation.g.cs");
        Console.WriteLine("      • NPA.Generators.EntityMetadataGenerator/");
        Console.WriteLine("        └─ GeneratedMetadataProvider.g.cs");
        Console.WriteLine();
        Console.WriteLine("✨ NPA Source Generator Demo Completed!");
    }
}

/// <summary>
/// Example entity for demonstration.
/// </summary>
[Entity]
[Table("users")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Example repository interface that will trigger source generation.
/// The generator will create a UserRepository class with implementations.
/// </summary>
[Repository(typeof(User))]
public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> FindByUsernameAsync(string username);
    Task SaveAsync(User entity);
    Task UpdateAsync(User entity);
    Task DeleteAsync(int id);
    Task<int> CountAsync();
}
