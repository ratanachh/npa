using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NPA.Migrations;

namespace NPA.Migrate;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("NPA Database Migration Tool")
        {
            CreateApplyCommand(),
            CreateStatusCommand(),
            CreateCreateCommand()
        };

        return await rootCommand.InvokeAsync(args);
    }

    static Command CreateApplyCommand()
    {
        var applyCommand = new Command("apply", "Apply pending migrations")
        {
            new Option<string>("--connection", "Database connection string") { IsRequired = true },
            new Option<string>("--provider", "Database provider") { IsRequired = true }
        };

        applyCommand.SetHandler(async (string connection, string provider) =>
        {
            var host = CreateHost();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var migrationManager = host.Services.GetRequiredService<MigrationManager>();
            
            logger.LogInformation("Applying migrations to {Provider} database...", provider);
            
            try
            {
                await migrationManager.ApplyMigrationsAsync(connection);
                logger.LogInformation("Migrations applied successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to apply migrations.");
            }
        }, 
        new Argument<string>("connection"), 
        new Argument<string>("provider"));

        return applyCommand;
    }

    static Command CreateStatusCommand()
    {
        var statusCommand = new Command("status", "Check migration status")
        {
            new Option<string>("--connection", "Database connection string") { IsRequired = true }
        };

        statusCommand.SetHandler(async (string connection) =>
        {
            var host = CreateHost();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var migrationManager = host.Services.GetRequiredService<MigrationManager>();
            
            logger.LogInformation("Checking migration status...");
            
            try
            {
                var version = await migrationManager.GetCurrentVersionAsync(connection);
                logger.LogInformation("Current migration version: {Version}", version);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to check migration status.");
            }
        }, 
        new Argument<string>("connection"));

        return statusCommand;
    }

    static Command CreateCreateCommand()
    {
        var createCommand = new Command("create", "Create a new migration")
        {
            new Option<string>("--name", "Migration name") { IsRequired = true },
            new Option<string>("--output", "Output directory")
        };

        createCommand.SetHandler(async (string name, string output) =>
        {
            var host = CreateHost();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("Creating migration: {Name}", name);
            
            // TODO: Implement migration creation logic
            await Task.CompletedTask;
        }, 
        new Argument<string>("name"), 
        new Argument<string>("output"));

        return createCommand;
    }

    static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddScoped<MigrationManager>();
            })
            .Build();
    }
}