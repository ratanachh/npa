using BasicUsage.Features;

namespace BasicUsage;

class Program
{
    static async Task Main(string[] args)
    {
        string provider = args.Length > 0 ? args[0].ToLowerInvariant() : "sqlserver";
        if (provider == "sqlserver")
        {
            Console.WriteLine("=== NPA Basic Usage with SQL Server Provider ===");
            await SqlServerProviderRunner.RunAsync();
        }
        else if (provider == "postgresql")
        {
            Console.WriteLine("=== NPA Basic Usage with PostgreSQL Provider ===");
            // ...existing code for PostgreSQL setup (if implemented)...
            Console.WriteLine("PostgreSQL provider not implemented in this sample.");
        }
        else
        {
            Console.WriteLine($"Unknown provider: {provider}");
        }
        Console.WriteLine("NPA Demo Completed Successfully!");
    }
}

