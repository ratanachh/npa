using NPA.Samples.Core;

namespace NPA.Samples.Features;

public class SourceGeneratorSample : ISample
{
    public string Name => "Source Generators";
    public string Description => "Explains the benefits of the Repository and Metadata source generators.";

    public Task RunAsync()
    {
        Console.WriteLine("--- Source Generator Demonstration ---");

        Console.WriteLine("\nThis sample is descriptive. It explains what the source generators do behind the scenes.");
        Console.WriteLine("To see the generated code, build the solution and look in the 'obj/Debug/net8.0/generated' folder of this project.");

        Console.WriteLine("\n--- 1. Repository Generator ---");
        Console.WriteLine("The Repository Generator automatically creates concrete implementations of your repository interfaces.");
        Console.WriteLine("\n  [Repository(typeof(User))]\n  public interface IUserRepository { ... }\n");
        Console.WriteLine("  ...generates...\n");
        Console.WriteLine("  public class UserRepository : IUserRepository { ... }\n");
        Console.WriteLine("This saves you from writing boilerplate data access code.");

        Console.WriteLine("\n--- 2. Metadata Generator ---");
        Console.WriteLine("The Metadata Generator creates a high-performance metadata provider at compile time.");
        Console.WriteLine("It reads your entity attributes ([Table], [Column], etc.) and generates a static metadata cache.");
        Console.WriteLine("This avoids slow runtime reflection and can make metadata access over 100x faster.");

        return Task.CompletedTask;
    }
}
