using BasicUsage.Features;

namespace BasicUsage;

class Program
{
    static async Task Main(string[] args)
    {
        // Parse command line arguments
        string provider = args.Length > 0 ? args[0].ToLowerInvariant() : "sqlserver";
        bool showSyncAsync = args.Any(a => a.ToLowerInvariant() == "--sync-async" || a.ToLowerInvariant() == "-sa");
        
        // Show sync/async comparison if requested
        if (showSyncAsync)
        {
            await SyncAsyncComparisonDemo.RunAsync();
            return;
        }
        
        // Default provider demos
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
            Console.WriteLine("\nOptions:");
            Console.WriteLine("  --sync-async, -sa    Show synchronous vs asynchronous methods comparison");
        }
        
        // Phase 2.1: Demonstrate relationship mapping
        RelationshipDemo.ShowRelationshipMetadata();
        
        Console.WriteLine("\nNPA Demo Completed Successfully!");
        Console.WriteLine("\nTip: Run with '--sync-async' to see sync vs async comparison");
    }
}

