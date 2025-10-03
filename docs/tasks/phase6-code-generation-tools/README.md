# Phase 6.2: Code Generation Tools

## 📋 Task Overview

**Objective**: Create command-line tools and utilities for code generation, project scaffolding, and development workflow automation for the NPA library.

**Priority**: Low  
**Estimated Time**: 3-4 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.6, Phase 3.1-3.5, Phase 4.1-4.7, Phase 5.1-5.5, Phase 6.1 (All previous phases)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] CLI tools are implemented
- [ ] Code generation works
- [ ] Project scaffolding works
- [ ] Configuration management works
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

## 📝 Detailed Requirements

### 1. CLI Tools
- **NPA CLI**: Main command-line interface
- **Code Generation**: Generate repositories, entities, migrations
- **Project Scaffolding**: Create new NPA projects
- **Configuration**: Manage NPA configuration
- **Database Operations**: Database management commands

### 2. Code Generation Commands
- **Generate Repository**: Create repository interfaces and implementations
- **Generate Entity**: Create entity classes from database schema
- **Generate Migration**: Create migration classes
- **Generate Configuration**: Create NPA configuration files
- **Generate Tests**: Create unit test templates

### 3. Project Scaffolding
- **New Project**: Create new NPA projects
- **Add Package**: Add NPA packages to existing projects
- **Update Project**: Update existing projects to use NPA
- **Project Templates**: Various project templates

### 4. Configuration Management
- **Init**: Initialize NPA configuration
- **Validate**: Validate NPA configuration
- **Update**: Update NPA configuration
- **Export**: Export configuration to file
- **Import**: Import configuration from file

### 5. Database Operations
- **Create Database**: Create database from configuration
- **Drop Database**: Drop database
- **Migrate**: Run database migrations
- **Rollback**: Rollback database migrations
- **Seed**: Seed database with initial data

## 🏗️ Implementation Plan

### Step 1: Create CLI Project
1. Create console application project
2. Set up command-line parsing
3. Configure dependency injection
4. Add logging support

### Step 2: Implement Core Commands
1. Create base command class
2. Implement help command
3. Implement version command
4. Implement configuration commands

### Step 3: Implement Code Generation
1. Create code generation service
2. Implement repository generation
3. Implement entity generation
4. Implement migration generation

### Step 4: Implement Project Scaffolding
1. Create project template service
2. Implement project creation
3. Implement package management
4. Implement project updates

### Step 5: Implement Database Operations
1. Create database service
2. Implement database creation
3. Implement migration operations
4. Implement seeding operations

### Step 6: Create Unit Tests
1. Test CLI commands
2. Test code generation
3. Test project scaffolding
4. Test database operations

### Step 7: Add Documentation
1. XML documentation comments
2. Usage examples
3. CLI guide
4. Best practices

## 📁 File Structure

```
src/NPA.CLI/
├── NPA.CLI.csproj
├── Program.cs
├── Commands/
│   ├── ICommand.cs
│   ├── BaseCommand.cs
│   ├── HelpCommand.cs
│   ├── VersionCommand.cs
│   ├── InitCommand.cs
│   ├── GenerateCommand.cs
│   ├── NewCommand.cs
│   ├── AddCommand.cs
│   ├── UpdateCommand.cs
│   ├── MigrateCommand.cs
│   └── SeedCommand.cs
├── Services/
│   ├── ICodeGenerationService.cs
│   ├── CodeGenerationService.cs
│   ├── IProjectTemplateService.cs
│   ├── ProjectTemplateService.cs
│   ├── IDatabaseService.cs
│   ├── DatabaseService.cs
│   ├── IConfigurationService.cs
│   └── ConfigurationService.cs
├── Templates/
│   ├── RepositoryTemplate.cs
│   ├── EntityTemplate.cs
│   ├── MigrationTemplate.cs
│   └── ProjectTemplate.cs
└── Utils/
    ├── ConsoleHelper.cs
    ├── FileHelper.cs
    └── ValidationHelper.cs

tests/NPA.CLI.Tests/
├── Commands/
│   ├── HelpCommandTests.cs
│   ├── VersionCommandTests.cs
│   ├── InitCommandTests.cs
│   ├── GenerateCommandTests.cs
│   ├── NewCommandTests.cs
│   └── MigrateCommandTests.cs
├── Services/
│   ├── CodeGenerationServiceTests.cs
│   ├── ProjectTemplateServiceTests.cs
│   ├── DatabaseServiceTests.cs
│   └── ConfigurationServiceTests.cs
└── Utils/
    ├── ConsoleHelperTests.cs
    ├── FileHelperTests.cs
    └── ValidationHelperTests.cs
```

## 💻 Code Examples

### Main Program
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NPA.CLI.Commands;
using NPA.CLI.Services;
using System.CommandLine;

namespace NPA.CLI
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            var rootCommand = new RootCommand("NPA CLI - Command-line tools for NPA ORM");
            
            // Add commands
            rootCommand.AddCommand(new HelpCommand());
            rootCommand.AddCommand(new VersionCommand());
            rootCommand.AddCommand(new InitCommand());
            rootCommand.AddCommand(new GenerateCommand());
            rootCommand.AddCommand(new NewCommand());
            rootCommand.AddCommand(new AddCommand());
            rootCommand.AddCommand(new UpdateCommand());
            rootCommand.AddCommand(new MigrateCommand());
            rootCommand.AddCommand(new SeedCommand());
            
            return await rootCommand.InvokeAsync(args);
        }
        
        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<ICodeGenerationService, CodeGenerationService>();
                    services.AddSingleton<IProjectTemplateService, ProjectTemplateService>();
                    services.AddSingleton<IDatabaseService, DatabaseService>();
                    services.AddSingleton<IConfigurationService, ConfigurationService>();
                });
    }
}
```

### Base Command Class
```csharp
public abstract class BaseCommand : Command
{
    protected readonly ICodeGenerationService _codeGenerationService;
    protected readonly IProjectTemplateService _projectTemplateService;
    protected readonly IDatabaseService _databaseService;
    protected readonly IConfigurationService _configurationService;
    protected readonly ILogger<BaseCommand> _logger;
    
    protected BaseCommand(
        string name, 
        string description,
        ICodeGenerationService codeGenerationService,
        IProjectTemplateService projectTemplateService,
        IDatabaseService databaseService,
        IConfigurationService configurationService,
        ILogger<BaseCommand> logger) : base(name, description)
    {
        _codeGenerationService = codeGenerationService;
        _projectTemplateService = projectTemplateService;
        _databaseService = databaseService;
        _configurationService = configurationService;
        _logger = logger;
    }
    
    protected virtual void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ {message}");
        Console.ResetColor();
    }
    
    protected virtual void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ {message}");
        Console.ResetColor();
    }
    
    protected virtual void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠ {message}");
        Console.ResetColor();
    }
    
    protected virtual void WriteInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"ℹ {message}");
        Console.ResetColor();
    }
}
```

### Generate Command
```csharp
public class GenerateCommand : BaseCommand
{
    public GenerateCommand(
        ICodeGenerationService codeGenerationService,
        IProjectTemplateService projectTemplateService,
        IDatabaseService databaseService,
        IConfigurationService configurationService,
        ILogger<GenerateCommand> logger) 
        : base("generate", "Generate code from templates", codeGenerationService, projectTemplateService, databaseService, configurationService, logger)
    {
        var typeArgument = new Argument<string>("type", "Type of code to generate (repository, entity, migration, test)");
        var nameArgument = new Argument<string>("name", "Name of the generated code");
        var outputOption = new Option<string>("--output", "Output directory");
        var namespaceOption = new Option<string>("--namespace", "Namespace for generated code");
        var forceOption = new Option<bool>("--force", "Force overwrite existing files");
        
        AddArgument(typeArgument);
        AddArgument(nameArgument);
        AddOption(outputOption);
        AddOption(namespaceOption);
        AddOption(forceOption);
        
        this.SetHandler(HandleAsync, typeArgument, nameArgument, outputOption, namespaceOption, forceOption);
    }
    
    private async Task HandleAsync(string type, string name, string output, string namespaceName, bool force)
    {
        try
        {
            var outputDir = output ?? Directory.GetCurrentDirectory();
            var ns = namespaceName ?? "Generated";
            
            WriteInfo($"Generating {type} '{name}' in '{outputDir}'...");
            
            switch (type.ToLower())
            {
                case "repository":
                    await GenerateRepositoryAsync(name, outputDir, ns, force);
                    break;
                case "entity":
                    await GenerateEntityAsync(name, outputDir, ns, force);
                    break;
                case "migration":
                    await GenerateMigrationAsync(name, outputDir, ns, force);
                    break;
                case "test":
                    await GenerateTestAsync(name, outputDir, ns, force);
                    break;
                default:
                    WriteError($"Unknown type '{type}'. Supported types: repository, entity, migration, test");
                    return;
            }
            
            WriteSuccess($"Successfully generated {type} '{name}'");
        }
        catch (Exception ex)
        {
            WriteError($"Failed to generate {type}: {ex.Message}");
            _logger.LogError(ex, "Error generating {Type} {Name}", type, name);
        }
    }
    
    private async Task GenerateRepositoryAsync(string name, string outputDir, string namespaceName, bool force)
    {
        var repositoryInterface = await _codeGenerationService.GenerateRepositoryInterfaceAsync(name, namespaceName);
        var repositoryImplementation = await _codeGenerationService.GenerateRepositoryImplementationAsync(name, namespaceName);
        
        var interfacePath = Path.Combine(outputDir, $"I{name}Repository.cs");
        var implementationPath = Path.Combine(outputDir, $"{name}Repository.cs");
        
        if (!force && (File.Exists(interfacePath) || File.Exists(implementationPath)))
        {
            WriteWarning("Files already exist. Use --force to overwrite.");
            return;
        }
        
        await File.WriteAllTextAsync(interfacePath, repositoryInterface);
        await File.WriteAllTextAsync(implementationPath, repositoryImplementation);
        
        WriteSuccess($"Generated repository interface: {interfacePath}");
        WriteSuccess($"Generated repository implementation: {implementationPath}");
    }
    
    private async Task GenerateEntityAsync(string name, string outputDir, string namespaceName, bool force)
    {
        var entity = await _codeGenerationService.GenerateEntityAsync(name, namespaceName);
        var entityPath = Path.Combine(outputDir, $"{name}.cs");
        
        if (!force && File.Exists(entityPath))
        {
            WriteWarning("File already exists. Use --force to overwrite.");
            return;
        }
        
        await File.WriteAllTextAsync(entityPath, entity);
        WriteSuccess($"Generated entity: {entityPath}");
    }
    
    private async Task GenerateMigrationAsync(string name, string outputDir, string namespaceName, bool force)
    {
        var migration = await _codeGenerationService.GenerateMigrationAsync(name, namespaceName);
        var migrationPath = Path.Combine(outputDir, $"{name}Migration.cs");
        
        if (!force && File.Exists(migrationPath))
        {
            WriteWarning("File already exists. Use --force to overwrite.");
            return;
        }
        
        await File.WriteAllTextAsync(migrationPath, migration);
        WriteSuccess($"Generated migration: {migrationPath}");
    }
    
    private async Task GenerateTestAsync(string name, string outputDir, string namespaceName, bool force)
    {
        var test = await _codeGenerationService.GenerateTestAsync(name, namespaceName);
        var testPath = Path.Combine(outputDir, $"{name}Tests.cs");
        
        if (!force && File.Exists(testPath))
        {
            WriteWarning("File already exists. Use --force to overwrite.");
            return;
        }
        
        await File.WriteAllTextAsync(testPath, test);
        WriteSuccess($"Generated test: {testPath}");
    }
}
```

### New Command
```csharp
public class NewCommand : BaseCommand
{
    public NewCommand(
        ICodeGenerationService codeGenerationService,
        IProjectTemplateService projectTemplateService,
        IDatabaseService databaseService,
        IConfigurationService configurationService,
        ILogger<NewCommand> logger) 
        : base("new", "Create a new NPA project", codeGenerationService, projectTemplateService, databaseService, configurationService, logger)
    {
        var templateArgument = new Argument<string>("template", "Project template (console, webapi, blazor, classlib)");
        var nameArgument = new Argument<string>("name", "Project name");
        var outputOption = new Option<string>("--output", "Output directory");
        var databaseOption = new Option<string>("--database", "Database provider (sqlserver, postgresql, mysql, sqlite)");
        var connectionStringOption = new Option<string>("--connection-string", "Database connection string");
        
        AddArgument(templateArgument);
        AddArgument(nameArgument);
        AddOption(outputOption);
        AddOption(databaseOption);
        AddOption(connectionStringOption);
        
        this.SetHandler(HandleAsync, templateArgument, nameArgument, outputOption, databaseOption, connectionStringOption);
    }
    
    private async Task HandleAsync(string template, string name, string output, string database, string connectionString)
    {
        try
        {
            var outputDir = output ?? Directory.GetCurrentDirectory();
            var projectPath = Path.Combine(outputDir, name);
            
            WriteInfo($"Creating new {template} project '{name}' in '{projectPath}'...");
            
            if (Directory.Exists(projectPath))
            {
                WriteError($"Directory '{projectPath}' already exists.");
                return;
            }
            
            Directory.CreateDirectory(projectPath);
            
            await _projectTemplateService.CreateProjectAsync(template, name, projectPath, new ProjectOptions
            {
                DatabaseProvider = database ?? "sqlserver",
                ConnectionString = connectionString ?? $"Server=localhost;Database={name};Trusted_Connection=true;"
            });
            
            WriteSuccess($"Successfully created project '{name}'");
            WriteInfo($"To get started:");
            WriteInfo($"  cd {projectPath}");
            WriteInfo($"  dotnet restore");
            WriteInfo($"  dotnet build");
        }
        catch (Exception ex)
        {
            WriteError($"Failed to create project: {ex.Message}");
            _logger.LogError(ex, "Error creating project {Template} {Name}", template, name);
        }
    }
}
```

### Migrate Command
```csharp
public class MigrateCommand : BaseCommand
{
    public MigrateCommand(
        ICodeGenerationService codeGenerationService,
        IProjectTemplateService projectTemplateService,
        IDatabaseService databaseService,
        IConfigurationService configurationService,
        ILogger<MigrateCommand> logger) 
        : base("migrate", "Run database migrations", codeGenerationService, projectTemplateService, databaseService, configurationService, logger)
    {
        var actionArgument = new Argument<string>("action", "Migration action (up, down, list, create)");
        var versionOption = new Option<int?>("--version", "Target version for up/down");
        var connectionStringOption = new Option<string>("--connection-string", "Database connection string");
        
        AddArgument(actionArgument);
        AddOption(versionOption);
        AddOption(connectionStringOption);
        
        this.SetHandler(HandleAsync, actionArgument, versionOption, connectionStringOption);
    }
    
    private async Task HandleAsync(string action, int? version, string connectionString)
    {
        try
        {
            var config = await _configurationService.LoadConfigurationAsync();
            if (connectionString != null)
            {
                config.ConnectionString = connectionString;
            }
            
            switch (action.ToLower())
            {
                case "up":
                    await _databaseService.RunMigrationsAsync(config);
                    WriteSuccess("Migrations completed successfully");
                    break;
                case "down":
                    if (version == null)
                    {
                        WriteError("Version is required for down migration");
                        return;
                    }
                    await _databaseService.RollbackMigrationAsync(config, version.Value);
                    WriteSuccess($"Rolled back to version {version}");
                    break;
                case "list":
                    var migrations = await _databaseService.GetMigrationsAsync(config);
                    WriteInfo("Available migrations:");
                    foreach (var migration in migrations)
                    {
                        WriteInfo($"  {migration.Version}: {migration.Name} - {migration.Description}");
                    }
                    break;
                case "create":
                    WriteInfo("Creating new migration...");
                    var migrationName = ConsoleHelper.Prompt("Migration name");
                    var description = ConsoleHelper.Prompt("Description");
                    await _codeGenerationService.GenerateMigrationAsync(migrationName, description);
                    WriteSuccess($"Created migration: {migrationName}");
                    break;
                default:
                    WriteError($"Unknown action '{action}'. Supported actions: up, down, list, create");
                    return;
            }
        }
        catch (Exception ex)
        {
            WriteError($"Migration failed: {ex.Message}");
            _logger.LogError(ex, "Error running migration {Action}", action);
        }
    }
}
```

### Code Generation Service
```csharp
public class CodeGenerationService : ICodeGenerationService
{
    private readonly ILogger<CodeGenerationService> _logger;
    
    public CodeGenerationService(ILogger<CodeGenerationService> logger)
    {
        _logger = logger;
    }
    
    public async Task<string> GenerateRepositoryInterfaceAsync(string entityName, string namespaceName)
    {
        var template = new RepositoryInterfaceTemplate();
        return template.Generate(entityName, namespaceName);
    }
    
    public async Task<string> GenerateRepositoryImplementationAsync(string entityName, string namespaceName)
    {
        var template = new RepositoryImplementationTemplate();
        return template.Generate(entityName, namespaceName);
    }
    
    public async Task<string> GenerateEntityAsync(string entityName, string namespaceName)
    {
        var template = new EntityTemplate();
        return template.Generate(entityName, namespaceName);
    }
    
    public async Task<string> GenerateMigrationAsync(string migrationName, string description)
    {
        var template = new MigrationTemplate();
        return template.Generate(migrationName, description);
    }
    
    public async Task<string> GenerateTestAsync(string testName, string namespaceName)
    {
        var template = new TestTemplate();
        return template.Generate(testName, namespaceName);
    }
}
```

### Usage Examples
```bash
# Initialize NPA configuration
npa init

# Generate repository
npa generate repository User --namespace MyApp.Repositories

# Generate entity
npa generate entity Product --namespace MyApp.Entities

# Generate migration
npa generate migration AddUserTable --namespace MyApp.Migrations

# Create new console project
npa new console MyApp --database sqlserver --connection-string "Server=localhost;Database=MyApp;Trusted_Connection=true;"

# Create new Web API project
npa new webapi MyWebApi --database postgresql

# Run migrations
npa migrate up

# Rollback migration
npa migrate down --version 2024010101

# List migrations
npa migrate list

# Create new migration
npa migrate create

# Add NPA package to existing project
npa add package MyProject

# Update project to use NPA
npa update MyProject

# Validate configuration
npa config validate

# Export configuration
npa config export --output npa.config.json

# Import configuration
npa config import --input npa.config.json
```

## 🧪 Test Cases

### CLI Command Tests
- [ ] Help command
- [ ] Version command
- [ ] Init command
- [ ] Generate command
- [ ] New command
- [ ] Add command
- [ ] Update command
- [ ] Migrate command
- [ ] Seed command

### Code Generation Tests
- [ ] Repository generation
- [ ] Entity generation
- [ ] Migration generation
- [ ] Test generation
- [ ] Template processing

### Project Scaffolding Tests
- [ ] Console project creation
- [ ] Web API project creation
- [ ] Blazor project creation
- [ ] Class library creation
- [ ] Package management

### Database Operation Tests
- [ ] Database creation
- [ ] Migration execution
- [ ] Migration rollback
- [ ] Database seeding
- [ ] Configuration validation

### Integration Tests
- [ ] End-to-end workflows
- [ ] Error handling
- [ ] File operations
- [ ] Configuration management

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] CLI installation
- [ ] Basic commands
- [ ] Code generation
- [ ] Project scaffolding
- [ ] Database operations

### CLI Guide
- [ ] Command reference
- [ ] Configuration options
- [ ] Examples
- [ ] Troubleshooting
- [ ] Best practices

## 🔍 Code Review Checklist

- [ ] Code follows .NET naming conventions
- [ ] All public members have XML documentation
- [ ] Error handling is appropriate
- [ ] Unit tests cover all scenarios
- [ ] Code is readable and maintainable
- [ ] Performance is optimized
- [ ] Memory usage is efficient
- [ ] Thread safety considerations

## 🚀 Next Steps

After completing this task:
1. Move to Phase 6.3: Performance Profiling
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on CLI design
- [ ] Performance considerations for code generation
- [ ] Integration with existing features
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
