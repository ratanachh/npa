using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NPA.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("NPA Command Line Interface")
        {
            CreateGenerateCommand(),
            CreateMigrateCommand(),
            CreateScaffoldCommand()
        };

        return await rootCommand.InvokeAsync(args);
    }

    static Command CreateGenerateCommand()
    {
        var generateCommand = new Command("generate", "Generate code using NPA generators")
        {
            new Option<string>("--project", "Target project path"),
            new Option<string>("--output", "Output directory")
        };

        generateCommand.SetHandler(async (string project, string output) =>
        {
            var host = CreateHost();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("Generating code for project: {Project}", project);
            logger.LogInformation("Output directory: {Output}", output);
            
            // TODO: Implement code generation logic
            await Task.CompletedTask;
        }, 
        new Argument<string>("project"), 
        new Argument<string>("output"));

        return generateCommand;
    }

    static Command CreateMigrateCommand()
    {
        var migrateCommand = new Command("migrate", "Run database migrations")
        {
            new Option<string>("--connection", "Database connection string"),
            new Option<string>("--provider", "Database provider (sqlserver, mysql, postgresql)")
        };

        migrateCommand.SetHandler(async (string connection, string provider) =>
        {
            var host = CreateHost();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("Running migrations for {Provider}", provider);
            
            // TODO: Implement migration logic
            await Task.CompletedTask;
        }, 
        new Argument<string>("connection"), 
        new Argument<string>("provider"));

        return migrateCommand;
    }

    static Command CreateScaffoldCommand()
    {
        var scaffoldCommand = new Command("scaffold", "Scaffold entities from existing database")
        {
            new Option<string>("--connection", "Database connection string"),
            new Option<string>("--output", "Output directory"),
            new Option<string>("--namespace", "Target namespace")
        };

        scaffoldCommand.SetHandler(async (string connection, string output, string ns) =>
        {
            var host = CreateHost();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("Scaffolding entities to {Output} with namespace {Namespace}", output, ns);
            
            // TODO: Implement scaffolding logic
            await Task.CompletedTask;
        }, 
        new Argument<string>("connection"), 
        new Argument<string>("output"),
        new Argument<string>("ns"));

        return scaffoldCommand;
    }

    static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                // TODO: Add NPA services
            })
            .Build();
    }
}