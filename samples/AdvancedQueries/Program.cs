using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Core.Providers;
using NPA.Providers.PostgreSql;
using System.Data;

namespace AdvancedQueries;

/// <summary>
/// Advanced CPQL Query Examples - Phase 1.3
/// Demonstrates complex WHERE clauses, aggregations, and parameter binding.
/// 
/// Note: This sample uses current CPQL implementation (Phase 1.3).
/// Advanced features like JOIN, subqueries, and CTEs require Phase 2.3 (JPQL) - not yet implemented.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("NPA Advanced CPQL Queries Sample");
        Console.WriteLine("Using Phase 1.3 features (Entity Mapping + CRUD + CPQL)\n");

        await using var dbManager = new DatabaseManager();
        var connection = await dbManager.StartAsync();

        // Setup Dependency Injection
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning); // Reduce noise
        });

        // Register NPA services with PostgreSQL provider
        services.AddSingleton<IMetadataProvider, MetadataProvider>();
        services.AddSingleton<IDatabaseProvider, PostgreSqlProvider>();
        services.AddSingleton<IDbConnection>(connection);
        services.AddScoped<EntityManager>();

        var serviceProvider = services.BuildServiceProvider();

        // Run advanced query examples
        var examples = new AdvancedQueryExamples(serviceProvider);
        await examples.RunAllExamples();

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}