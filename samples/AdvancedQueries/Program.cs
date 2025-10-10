using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Core.Providers;
using NPA.Providers.PostgreSql;
using System.Data;

namespace AdvancedQueries;

/// <summary>
/// Advanced CPQL Query Examples - Phase 2.3
/// Demonstrates JOINs, GROUP BY, HAVING, aggregate functions, and complex expressions.
/// Shows SQL and parameter logging for debugging and learning.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("NPA Advanced CPQL Queries Sample - Phase 2.3 ✅");
        Console.WriteLine("Demonstrates enhanced CPQL with JOINs, GROUP BY, and advanced features\n");
        
        // Check if verbose logging is requested
        bool showSql = args.Any(a => a.ToLowerInvariant() == "--show-sql" || a.ToLowerInvariant() == "-v");

        await using var dbManager = new DatabaseManager();
        var connection = await dbManager.StartAsync();

        // Setup Dependency Injection
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            // Enable Debug logging to see SQL and parameters
            builder.SetMinimumLevel(showSql ? LogLevel.Debug : LogLevel.Information);
        });

        // Register NPA services with PostgreSQL provider
        services.AddSingleton<IMetadataProvider, MetadataProvider>();
        services.AddSingleton<IDatabaseProvider, PostgreSqlProvider>();
        services.AddSingleton<IDbConnection>(connection);
        services.AddScoped<EntityManager>();

        var serviceProvider = services.BuildServiceProvider();

        // Run advanced query examples
        Console.WriteLine(showSql 
            ? "Running with SQL logging enabled (use --show-sql or -v to see SQL and parameters)\n" 
            : "Running in normal mode (use --show-sql or -v to see SQL and parameters)\n");
        
        var examples = new AdvancedQueryExamples(serviceProvider);
        await examples.RunAllExamples();

        Console.WriteLine("\n" + "=".PadRight(80, '='));
        Console.WriteLine("💡 Tip: Run with --show-sql or -v flag to see generated SQL and parameter values");
        Console.WriteLine("   Example: dotnet run -- --show-sql");
        Console.WriteLine("=".PadRight(80, '='));
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}