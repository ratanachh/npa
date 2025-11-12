using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace NPA.CLI.Commands;

/// <summary>
/// Command for displaying CLI version information.
/// </summary>
public class VersionCommand : BaseCommand
{
    public VersionCommand(ILogger<VersionCommand> logger)
        : base("version", "Display NPA CLI version information", logger)
    {
        this.SetHandler(HandleAsync);
    }

    private Task HandleAsync()
    {
        var version = typeof(VersionCommand).Assembly.GetName().Version;
        
        WriteInfo("NPA CLI - Code Generation Tools");
        WriteInfo($"Version: {version?.ToString() ?? "1.0.0"}");
        WriteInfo("Copyright (c) 2025 NPA Project");
        WriteInfo("");
        WriteInfo("Available commands:");
        WriteInfo("  new        - Create a new NPA project");
        WriteInfo("  generate   - Generate code from templates");
        WriteInfo("  config     - Manage configuration");
        WriteInfo("  version    - Show version information");
        WriteInfo("");
        WriteInfo("Use 'npa <command> --help' for more information about a command.");

        return Task.CompletedTask;
    }
}
