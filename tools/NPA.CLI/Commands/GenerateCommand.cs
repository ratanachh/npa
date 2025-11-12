using System.CommandLine;
using Microsoft.Extensions.Logging;
using NPA.CLI.Services;

namespace NPA.CLI.Commands;

/// <summary>
/// Command for generating code from templates.
/// </summary>
public class GenerateCommand : BaseCommand
{
    private readonly ICodeGenerationService _codeGenService;

    public GenerateCommand(ICodeGenerationService codeGenService, ILogger<GenerateCommand> logger)
        : base("generate", "Generate code from templates (entity, repository, migration, test)", logger)
    {
        _codeGenService = codeGenService;

        var typeArgument = new Argument<string>("type", "Type of code to generate (entity, repository, migration, test)");
        var nameArgument = new Argument<string>("name", "Name for the generated code");
        
        var outputOption = new Option<string>(
            new[] { "--output", "-o" },
            () => Directory.GetCurrentDirectory(),
            "Output directory for generated files");
        
        var namespaceOption = new Option<string>(
            new[] { "--namespace", "-n" },
            () => "Generated",
            "Namespace for generated code");
        
        var tableOption = new Option<string>(
            new[] { "--table", "-t" },
            "Table name for entity (defaults to entity name in lowercase)");
        
        var forceOption = new Option<bool>(
            new[] { "--force", "-f" },
            () => false,
            "Force overwrite existing files");

        AddArgument(typeArgument);
        AddArgument(nameArgument);
        AddOption(outputOption);
        AddOption(namespaceOption);
        AddOption(tableOption);
        AddOption(forceOption);

        this.SetHandler(HandleAsync, typeArgument, nameArgument, outputOption, namespaceOption, tableOption, forceOption);
    }

    private async Task HandleAsync(string type, string name, string output, string namespaceName, string? tableName, bool force)
    {
        try
        {
            WriteInfo($"Generating {type} '{name}' in '{output}'...");

            switch (type.ToLower())
            {
                case "entity":
                    await GenerateEntityAsync(name, output, namespaceName, tableName ?? name, force);
                    break;
                case "repository":
                    await GenerateRepositoryAsync(name, output, namespaceName, force);
                    break;
                case "migration":
                    await GenerateMigrationAsync(name, output, namespaceName, force);
                    break;
                case "test":
                    await GenerateTestAsync(name, output, namespaceName, force);
                    break;
                default:
                    WriteError($"Unknown type '{type}'. Supported types: entity, repository, migration, test");
                    return;
            }
        }
        catch (Exception ex)
        {
            WriteError($"Failed to generate {type}: {ex.Message}");
            Logger.LogError(ex, "Error generating {Type} {Name}", type, name);
        }
    }

    private async Task GenerateEntityAsync(string name, string outputDir, string namespaceName, string tableName, bool force)
    {
        var entityCode = await _codeGenService.GenerateEntityAsync(name, namespaceName, tableName);
        var entityPath = Path.Combine(outputDir, $"{name}.cs");

        if (!force && File.Exists(entityPath))
        {
            WriteWarning($"File '{entityPath}' already exists. Use --force to overwrite.");
            return;
        }

        Directory.CreateDirectory(outputDir);
        await File.WriteAllTextAsync(entityPath, entityCode);
        WriteSuccess($"Generated entity: {entityPath}");
    }

    private async Task GenerateRepositoryAsync(string name, string outputDir, string namespaceName, bool force)
    {
        var interfaceCode = await _codeGenService.GenerateRepositoryInterfaceAsync(name, namespaceName);
        var implementationCode = await _codeGenService.GenerateRepositoryImplementationAsync(name, namespaceName);

        var interfacePath = Path.Combine(outputDir, $"I{name}Repository.cs");
        var implementationPath = Path.Combine(outputDir, $"{name}Repository.cs");

        if (!force && (File.Exists(interfacePath) || File.Exists(implementationPath)))
        {
            WriteWarning("Repository files already exist. Use --force to overwrite.");
            return;
        }

        Directory.CreateDirectory(outputDir);
        await File.WriteAllTextAsync(interfacePath, interfaceCode);
        await File.WriteAllTextAsync(implementationPath, implementationCode);

        WriteSuccess($"Generated repository interface: {interfacePath}");
        WriteSuccess($"Generated repository implementation: {implementationPath}");
    }

    private async Task GenerateMigrationAsync(string name, string outputDir, string namespaceName, bool force)
    {
        var migrationCode = await _codeGenService.GenerateMigrationAsync(name, namespaceName);
        var migrationPath = Path.Combine(outputDir, $"{name}Migration.cs");

        if (!force && File.Exists(migrationPath))
        {
            WriteWarning($"File '{migrationPath}' already exists. Use --force to overwrite.");
            return;
        }

        Directory.CreateDirectory(outputDir);
        await File.WriteAllTextAsync(migrationPath, migrationCode);
        WriteSuccess($"Generated migration: {migrationPath}");
    }

    private async Task GenerateTestAsync(string name, string outputDir, string namespaceName, bool force)
    {
        var testCode = await _codeGenService.GenerateTestAsync(name, namespaceName);
        var testPath = Path.Combine(outputDir, $"{name}Tests.cs");

        if (!force && File.Exists(testPath))
        {
            WriteWarning($"File '{testPath}' already exists. Use --force to overwrite.");
            return;
        }

        Directory.CreateDirectory(outputDir);
        await File.WriteAllTextAsync(testPath, testCode);
        WriteSuccess($"Generated test: {testPath}");
    }
}
