using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace SourceGeneratorDemo;

class Program
{
    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting Source Generator Demo...");

        await RunSourceGeneratorDemo(host.Services);
        
        logger.LogInformation("Source Generator Demo completed.");
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddLogging();
            });

    static async Task RunSourceGeneratorDemo(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Demonstrating source generator capabilities...");
        
        // TODO: This will demonstrate how the source generator creates repository code
        // The generator should create repositories for entities like Product below
        
        var product = new Product
        {
            Id = 1,
            Name = "Sample Product",
            Price = 29.99m,
            CreatedAt = DateTime.UtcNow
        };
        
        logger.LogInformation("Created product: {ProductName} with ID {ProductId}", product.Name, product.Id);
        
        // TODO: Use generated repository
        // var repository = new Generated.ProductRepository();
        // await repository.SaveAsync(product);
        
        await Task.CompletedTask;
    }
}

// This entity should trigger the source generator to create a repository
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}