using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NPA.Core;

namespace AdvancedQueries;

class Program
{
    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting Advanced Queries Demo...");

        // TODO: Demonstrate advanced query capabilities
        // - Complex joins
        // - Subqueries  
        // - Aggregations
        // - Window functions
        // - CTEs (Common Table Expressions)
        
        await RunAdvancedQueriesDemo(host.Services);
        
        logger.LogInformation("Advanced Queries Demo completed.");
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // TODO: Configure NPA services
                services.AddLogging();
            });

    static async Task RunAdvancedQueriesDemo(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        // Example: Complex join query
        logger.LogInformation("Demonstrating complex joins...");
        
        // Example: Subquery
        logger.LogInformation("Demonstrating subqueries...");
        
        // Example: Aggregation
        logger.LogInformation("Demonstrating aggregations...");
        
        await Task.CompletedTask;
    }
}