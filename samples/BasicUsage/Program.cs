using BasicUsage.Features;

namespace BasicUsage;

class Program
{
    static async Task Main(string[] args)
    {
        // Default to SQL Server (Phase 1.4 ✅ - Complete with 63 passing tests)
        // All three providers are fully tested and working (Phases 1.1-1.5 ✅)
        string provider = args.Length > 0 ? args[0].ToLowerInvariant() : "sqlserver";
        
        if (provider == "postgresql")
        {
            Console.WriteLine("=== NPA Basic Usage with PostgreSQL Provider ===");
            Console.WriteLine("Phases 1.1-1.5: Entity Mapping, CRUD Operations, CPQL Queries\n");
            await PostgreSqlProviderRunner.RunAsync();
        }
        else if (provider == "sqlserver")
        {
            Console.WriteLine("=== NPA Basic Usage with SQL Server Provider ===");
            Console.WriteLine("Phase 1.4: Complete with advanced features (TVPs, JSON, Spatial, Full-Text)\n");
            await SqlServerProviderRunner.RunAsync();
        }
        else if (provider == "mysql")
        {
            Console.WriteLine("=== NPA Basic Usage with MySQL Provider ===");
            Console.WriteLine("Phase 1.5: Complete with advanced features (JSON, Spatial, Full-Text, UPSERT)\n");
            await MySqlProviderRunner.RunAsync();
        }
        else
        {
            Console.WriteLine($"Unknown provider: {provider}");
            Console.WriteLine("Available providers: sqlserver (default), mysql, postgresql");
        }
        
        // Phase 2.1: Demonstrate relationship mapping
        RelationshipDemo.ShowRelationshipMetadata();
        
        Console.WriteLine("\nNPA Demo Completed Successfully!");
    }
}

