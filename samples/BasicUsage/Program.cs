using BasicUsage.Features;

namespace BasicUsage;

class Program
{
    static async Task Main(string[] args)
    {
        // Default to SQL Server (Phase 1.4 🚧 - in progress, may have issues)
        // PostgreSQL is fully tested and working (Phase 1.1-1.3 ✅)
        // Recommended: Use "postgresql" argument for stable experience
        string provider = args.Length > 0 ? args[0].ToLowerInvariant() : "sqlserver";
        
        if (provider == "postgresql")
        {
            Console.WriteLine("=== NPA Basic Usage with PostgreSQL Provider ===");
            Console.WriteLine("Phases 1.1-1.3: Entity Mapping, CRUD Operations, CPQL Queries\n");
            await PostgreSqlProviderRunner.RunAsync();
        }
        else if (provider == "sqlserver")
        {
            Console.WriteLine("=== NPA Basic Usage with SQL Server Provider ===");
            Console.WriteLine("Note: SQL Server provider is in progress (Phase 1.4)\n");
            await SqlServerProviderRunner.RunAsync();
        }
        else
        {
            Console.WriteLine($"Unknown provider: {provider}");
            Console.WriteLine("Available providers: postgresql (default), sqlserver");
        }
        
        Console.WriteLine("\nNPA Demo Completed Successfully!");
    }
}

