using System.CommandLine;
using Microsoft.Extensions.Logging;
using NPA.CLI.Services;

namespace NPA.CLI.Commands;

/// <summary>
/// Command for managing NPA configuration.
/// </summary>
public class ConfigCommand : BaseCommand
{
    private readonly IConfigurationService _configService;

    public ConfigCommand(IConfigurationService configService, ILogger<ConfigCommand> logger)
        : base("config", "Manage NPA configuration", logger)
    {
        _configService = configService;

        var actionArgument = new Argument<string>(
            "action",
            "Configuration action (init, validate, show)");

        var outputOption = new Option<string>(
            new[] { "--output", "-o" },
            () => Directory.GetCurrentDirectory(),
            "Output directory for configuration file");

        AddArgument(actionArgument);
        AddOption(outputOption);

        this.SetHandler(HandleAsync, actionArgument, outputOption);
    }

    private async Task HandleAsync(string action, string output)
    {
        try
        {
            switch (action.ToLower())
            {
                case "init":
                    await InitConfigurationAsync(output);
                    break;
                case "validate":
                    await ValidateConfigurationAsync(output);
                    break;
                case "show":
                    await ShowConfigurationAsync(output);
                    break;
                default:
                    WriteError($"Unknown action '{action}'. Supported actions: init, validate, show");
                    break;
            }
        }
        catch (Exception ex)
        {
            WriteError($"Configuration operation failed: {ex.Message}");
            Logger.LogError(ex, "Error in config command with action {Action}", action);
        }
    }

    private async Task InitConfigurationAsync(string outputPath)
    {
        var configPath = Path.Combine(outputPath, "npa.config.json");

        if (File.Exists(configPath))
        {
            if (!Confirm($"Configuration file already exists at '{configPath}'. Overwrite?", false))
            {
                WriteWarning("Operation cancelled.");
                return;
            }
        }

        await _configService.InitializeConfigurationAsync(outputPath);
        WriteSuccess($"Initialized NPA configuration: {configPath}");
        WriteInfo("Edit the configuration file to match your database settings.");
    }

    private async Task ValidateConfigurationAsync(string outputPath)
    {
        var configPath = Path.Combine(outputPath, "npa.config.json");

        if (!File.Exists(configPath))
        {
            WriteError($"Configuration file not found: {configPath}");
            WriteInfo("Run 'npa config init' to create a configuration file.");
            return;
        }

        var config = await _configService.LoadConfigurationAsync(configPath);
        var isValid = await _configService.ValidateConfigurationAsync(config);

        if (isValid)
        {
            WriteSuccess("Configuration is valid.");
            WriteInfo($"Database Provider: {config.DatabaseProvider}");
            WriteInfo($"Connection String: {MaskConnectionString(config.ConnectionString)}");
        }
        else
        {
            WriteError("Configuration is invalid.");
            WriteInfo("Please check the following:");
            WriteInfo("- Connection string is not empty");
            WriteInfo("- Database provider is specified");
        }
    }

    private async Task ShowConfigurationAsync(string outputPath)
    {
        var configPath = Path.Combine(outputPath, "npa.config.json");

        if (!File.Exists(configPath))
        {
            WriteError($"Configuration file not found: {configPath}");
            WriteInfo("Run 'npa config init' to create a configuration file.");
            return;
        }

        var config = await _configService.LoadConfigurationAsync(configPath);

        WriteInfo("Current NPA Configuration:");
        WriteInfo($"  Connection String: {MaskConnectionString(config.ConnectionString)}");
        WriteInfo($"  Database Provider: {config.DatabaseProvider}");
        WriteInfo($"  Migrations Namespace: {config.MigrationsNamespace}");
        WriteInfo($"  Entities Namespace: {config.EntitiesNamespace}");
        WriteInfo($"  Repositories Namespace: {config.RepositoriesNamespace}");
    }

    private string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "(empty)";
        }

        // Mask password in connection string
        var parts = connectionString.Split(';');
        var masked = parts.Select(part =>
        {
            if (part.Trim().StartsWith("Password=", StringComparison.OrdinalIgnoreCase) ||
                part.Trim().StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase))
            {
                return part.Split('=')[0] + "=****";
            }
            return part;
        });

        return string.Join(';', masked);
    }
}
