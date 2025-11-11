using NPA.Core.Annotations;
using NPA.Core.Repositories;
using NPA.Samples.Core;
using NPA.Samples.Entities;

namespace NPA.Samples.Features;

public class SourceGeneratorSample : ISample
{
    public string Name => "Source Generators (Descriptive)";
    public string Description => "Explains the compile-time code generation for repositories and metadata.";

    public Task RunAsync()
    {
        Console.WriteLine("--- Source Generator Demonstration ---");
        Console.WriteLine("NOTE: This sample is descriptive. Source generation is a compile-time process, not a runtime one.");
        Console.WriteLine("It automatically writes code for you before the application is even run.");

        Console.WriteLine("\n--- 1. Repository Generator ---");
        Console.WriteLine("The Repository Generator creates concrete implementations of your repository interfaces.");
        Console.WriteLine("For example, if you define this interface:");
        Console.WriteLine("\n  [Repository]");
        Console.WriteLine("  public interface IUserRepository : IRepository<User, long>");
        Console.WriteLine("  {");
        Console.WriteLine("      Task<User> FindByEmailAsync(string email);");
        Console.WriteLine("  }");
        Console.WriteLine("\nThe generator automatically writes this class for you during compilation:");
        Console.WriteLine("  public class UserRepository : BaseRepository<User, long>, IUserRepository");
        Console.WriteLine("  {");
        Console.WriteLine("      public async Task<User> FindByEmailAsync(string email) { /* generated code */ }");
        Console.WriteLine("  }");
        Console.WriteLine("\nThis saves you from writing boilerplate data access code for every entity.");

        Console.WriteLine("\n--- 2. Metadata Generator ---");
        Console.WriteLine("The Metadata Generator creates a high-performance metadata provider at compile time.");
        Console.WriteLine("It reads your entity attributes ([Table], [Column], etc.) and generates a static metadata cache.");
        Console.WriteLine("This avoids slow runtime reflection and can make metadata access over 100x faster.");

        Console.WriteLine("\n--------------------------------------------------");
        Console.WriteLine("HOW TO SEE THE GENERATED CODE:");
        Console.WriteLine("1. Build the solution.");
        Console.WriteLine("2. In the Solution Explorer, navigate to: ");
        Console.WriteLine("   BasicUsage > Dependencies > Analyzers > NPA.Generators");
        Console.WriteLine("3. Here you will find the generated files, such as 'GeneratedMetadataProvider.g.cs'.");
        Console.WriteLine("--------------------------------------------------");

        return Task.CompletedTask;
    }
    
    
    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {
        Task<IEnumerable<User>> FindByEmail(string email, long id);
    }
}
