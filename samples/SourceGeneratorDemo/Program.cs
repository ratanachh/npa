using NPA.Core.Annotations;

namespace SourceGeneratorDemo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== NPA Source Generator Demo ===");
        Console.WriteLine("This demo shows how the [Repository] attribute generates implementation code.");
        Console.WriteLine();
        
        Console.WriteLine("✅ Repository Generator (Phase 1.6) - IMPLEMENTED");
        Console.WriteLine();
        
        Console.WriteLine("Example Interface:");
        Console.WriteLine("---");
        Console.WriteLine("  [Repository(typeof(User))]");
        Console.WriteLine("  public interface IUserRepository");
        Console.WriteLine("  {");
        Console.WriteLine("      Task<IEnumerable<User>> GetAllAsync();");
        Console.WriteLine("      Task<User?> GetByIdAsync(int id);");
        Console.WriteLine("      Task<IEnumerable<User>> FindByUsernameAsync(string username);");
        Console.WriteLine("      Task SaveAsync(User entity);");
        Console.WriteLine("      Task UpdateAsync(User entity);");
        Console.WriteLine("      Task DeleteAsync(int id);");
        Console.WriteLine("  }");
        Console.WriteLine();
        
        Console.WriteLine("Generated Implementation:");
        Console.WriteLine("---");
        Console.WriteLine("  public partial class UserRepository : IUserRepository");
        Console.WriteLine("  {");
        Console.WriteLine("      private readonly IEntityManager _entityManager;");
        Console.WriteLine("      // ... generated method implementations");
        Console.WriteLine("  }");
        Console.WriteLine();
        
        Console.WriteLine("📋 Convention-Based Method Generation:");
        Console.WriteLine("  - GetAllAsync()          → SELECT * FROM users");
        Console.WriteLine("  - GetByIdAsync(id)       → SELECT * FROM users WHERE id = @id");
        Console.WriteLine("  - FindBy{Property}Async  → WHERE {property} = @{property}");
        Console.WriteLine("  - SaveAsync(entity)      → EntityManager.PersistAsync");
        Console.WriteLine("  - UpdateAsync(entity)    → EntityManager.MergeAsync");
        Console.WriteLine("  - DeleteAsync(id)        → EntityManager.RemoveAsync");
        Console.WriteLine();
        
        Console.WriteLine("✅ The repository generator creates type-safe, optimized implementations");
        Console.WriteLine("✅ No reflection or runtime code generation");
        Console.WriteLine("✅ Full IntelliSense and compile-time validation");
        Console.WriteLine();
        
        Console.WriteLine("To see generated code:");
        Console.WriteLine("  1. Mark your interface with [Repository(typeof(YourEntity))]");
        Console.WriteLine("  2. Build the project");
        Console.WriteLine("  3. Check obj/Debug/net8.0/generated folder");
        Console.WriteLine();
        
        Console.WriteLine("NPA Source Generator Demo Completed!");
        
        await Task.CompletedTask;
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
