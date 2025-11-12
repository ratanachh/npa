using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NPA.CLI.Commands;
using NPA.CLI.Services;

namespace NPA.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var host = CreateHost();

        // Build root command with all subcommands
        var rootCommand = new RootCommand("NPA CLI - Code generation and project management tools for NPA ORM");

        // Add commands using DI
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        rootCommand.AddCommand(services.GetRequiredService<NewCommand>());
        rootCommand.AddCommand(services.GetRequiredService<GenerateCommand>());
        rootCommand.AddCommand(services.GetRequiredService<ConfigCommand>());
        rootCommand.AddCommand(services.GetRequiredService<VersionCommand>());

        return await rootCommand.InvokeAsync(args);
    }

    static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // Register services
                services.AddSingleton<ICodeGenerationService, CodeGenerationService>();
                services.AddSingleton<IProjectTemplateService, ProjectTemplateService>();
                services.AddSingleton<IConfigurationService, ConfigurationService>();

                // Register commands
                services.AddSingleton<NewCommand>();
                services.AddSingleton<GenerateCommand>();
                services.AddSingleton<ConfigCommand>();
                services.AddSingleton<VersionCommand>();

                // Configure logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Warning);
                });
            })
            .Build();
    }
}