using System.CommandLine;
using Microsoft.Extensions.Logging;
using NPA.CLI.Services;

namespace NPA.CLI.Commands;

/// <summary>
/// Command for creating new projects.
/// </summary>
public class NewCommand : BaseCommand
{
    private readonly IProjectTemplateService _templateService;
    private readonly IConfigurationService _configService;

    public NewCommand(
        IProjectTemplateService templateService,
        IConfigurationService configService,
        ILogger<NewCommand> logger)
        : base("new", "Create a new NPA project from template", logger)
    {
        _templateService = templateService;
        _configService = configService;

        var templateArgument = new Argument<string>(
            "template",
            "Project template (console, webapi, classlib)");
        
        var nameArgument = new Argument<string>(
            "name",
            "Project name");

        var outputOption = new Option<string>(
            new[] { "--output", "-o" },
            () => Directory.GetCurrentDirectory(),
            "Output directory");

        var databaseOption = new Option<string>(
            new[] { "--database", "-db" },
            () => "sqlserver",
            "Database provider (sqlserver, postgresql, mysql, sqlite)");

        var connectionOption = new Option<string?>(
            new[] { "--connection-string", "-cs" },
            "Database connection string");

        var samplesOption = new Option<bool>(
            new[] { "--with-samples", "-s" },
            () => false,
            "Include sample code");

        AddArgument(templateArgument);
        AddArgument(nameArgument);
        AddOption(outputOption);
        AddOption(databaseOption);
        AddOption(connectionOption);
        AddOption(samplesOption);

        this.SetHandler(
            HandleAsync,
            templateArgument,
            nameArgument,
            outputOption,
            databaseOption,
            connectionOption,
            samplesOption);
    }

    private async Task HandleAsync(
        string template,
        string name,
        string output,
        string database,
        string? connectionString,
        bool withSamples)
    {
        try
        {
            var projectPath = Path.Combine(output, name);

            WriteInfo($"Creating new {template} project '{name}' in '{projectPath}'...");

            if (Directory.Exists(projectPath))
            {
                if (!Confirm($"Directory '{projectPath}' already exists. Overwrite?", false))
                {
                    WriteWarning("Operation cancelled.");
                    return;
                }

                Directory.Delete(projectPath, true);
            }

            // Generate connection string if not provided
            var connStr = connectionString ?? GenerateDefaultConnectionString(database, name);

            var options = new ProjectOptions
            {
                DatabaseProvider = database,
                ConnectionString = connStr,
                IncludeSamples = withSamples
            };

            await _templateService.CreateProjectAsync(template, name, projectPath, options);

            // Create NPA configuration file
            await _configService.InitializeConfigurationAsync(projectPath);

            WriteSuccess($"Successfully created project '{name}'");
            WriteInfo("");
            WriteInfo("Next steps:");
            WriteInfo($"  cd {name}");
            WriteInfo("  dotnet restore");
            WriteInfo("  dotnet build");
            
            if (template.ToLower() == "webapi")
            {
                WriteInfo("  dotnet run");
                WriteInfo("  Open https://localhost:5001/swagger in your browser");
            }
        }
        catch (Exception ex)
        {
            WriteError($"Failed to create project: {ex.Message}");
            Logger.LogError(ex, "Error creating project {Template} {Name}", template, name);
        }
    }

    private string GenerateDefaultConnectionString(string provider, string dbName)
    {
        return provider.ToLower() switch
        {
            "sqlserver" => $"Server=localhost;Database={dbName};Trusted_Connection=true;TrustServerCertificate=true;",
            "postgresql" => $"Host=localhost;Database={dbName};Username=postgres;Password=postgres;",
            "mysql" => $"Server=localhost;Database={dbName};User=root;Password=root;",
            "sqlite" => $"Data Source={dbName}.db",
            _ => $"Server=localhost;Database={dbName};Trusted_Connection=true;"
        };
    }
}
