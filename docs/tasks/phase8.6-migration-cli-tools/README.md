# Phase 8.6: Migration CLI Tools

## Overview
Comprehensive command-line interface for managing database migrations with developer-friendly commands and output formatting.

## Objectives
- Intuitive CLI commands for all migration operations
- Rich console output with colors and formatting
- Interactive prompts for dangerous operations
- Progress indicators for long-running tasks
- Configuration management
- Multiple output formats (text, JSON, table)

## Tasks

### 1. Core CLI Commands
- [ ] `migration add` - Create new migration
- [ ] `migration list` - List all migrations
- [ ] `migration status` - Show migration status
- [ ] `migration up` - Apply migrations
- [ ] `migration down` - Rollback migrations
- [ ] `migration remove` - Remove last migration
- [ ] `migration script` - Generate SQL script

### 2. Database Commands
- [ ] `database create` - Create database
- [ ] `database drop` - Drop database
- [ ] `database seed` - Seed data
- [ ] `database validate` - Validate schema
- [ ] `schema diff` - Compare schemas
- [ ] `schema sync` - Sync schema with entities

### 3. Interactive Features
- [ ] Confirmation prompts for destructive operations
- [ ] Interactive migration selection
- [ ] Migration preview before execution
- [ ] Colorized output with severity levels
- [ ] Progress bars for long operations
- [ ] Spinner for pending operations

### 4. Output Formatting
- [ ] Table output for migration lists
- [ ] JSON output for automation
- [ ] Verbose mode for debugging
- [ ] Quiet mode for scripts
- [ ] Summary statistics
- [ ] Error formatting with stack traces

### 5. Configuration Management
- [ ] Global configuration file
- [ ] Project-level configuration
- [ ] Environment variable support
- [ ] Connection string management
- [ ] Provider-specific settings
- [ ] Custom migration templates

## CLI Command Reference

### Migration Commands

```bash
# Create new migration
dotnet npa migration add AddUserTable
dotnet npa migration add AddUserTable --output ./Migrations
dotnet npa migration add AddUserTable --namespace MyApp.Data.Migrations

# List migrations
dotnet npa migration list
dotnet npa migration list --json
dotnet npa migration list --pending
dotnet npa migration list --applied

# Show migration status
dotnet npa migration status
# Output:
# Migration Status
# ================
# Database: ProductionDB
# Provider: SQL Server
# 
# Total Migrations: 15
# Applied: 12
# Pending: 3
# 
# Last Applied: 20251119143000_AddOrderItems
# Applied At: 2025-11-19 14:35:22

# Apply migrations
dotnet npa migrate up
dotnet npa migrate up --target 20251119143000_AddOrderItems
dotnet npa migrate up --dry-run
dotnet npa migrate up --verbose

# Rollback migrations
dotnet npa migrate down
dotnet npa migrate down --target 20251119140000_AddUsers
dotnet npa migrate down --steps 3
dotnet npa migrate down --all

# Remove last migration
dotnet npa migration remove
dotnet npa migration remove --force

# Generate SQL script
dotnet npa migration script
dotnet npa migration script --from 20251119140000 --to 20251119143000
dotnet npa migration script --output migration.sql
dotnet npa migration script --idempotent
```

### Database Commands

```bash
# Create database
dotnet npa database create
dotnet npa database create --connection "Server=localhost;Database=MyDB"

# Drop database
dotnet npa database drop
dotnet npa database drop --force

# Seed data
dotnet npa database seed
dotnet npa database seed --environment Production
dotnet npa database seed --class InitialDataSeeder

# Validate schema
dotnet npa database validate
dotnet npa database validate --strict

# Compare schemas
dotnet npa schema diff
dotnet npa schema diff --output diff.txt
dotnet npa schema diff --json

# Sync schema
dotnet npa schema sync
dotnet npa schema sync --dry-run
```

## CLI Implementation

### Command Structure

```csharp
using System.CommandLine;
using NPA.Migrations;
using Spectre.Console;

public class MigrationCli
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("NPA Migration Tool - Database migration management");
        
        // Migration commands
        var migrationCommand = new Command("migration", "Manage database migrations");
        migrationCommand.AddCommand(CreateAddCommand());
        migrationCommand.AddCommand(CreateListCommand());
        migrationCommand.AddCommand(CreateStatusCommand());
        migrationCommand.AddCommand(CreateRemoveCommand());
        migrationCommand.AddCommand(CreateScriptCommand());
        
        // Migrate commands
        var migrateCommand = new Command("migrate", "Apply or rollback migrations");
        migrateCommand.AddCommand(CreateUpCommand());
        migrateCommand.AddCommand(CreateDownCommand());
        
        // Database commands
        var databaseCommand = new Command("database", "Manage database");
        databaseCommand.AddCommand(CreateDatabaseCreateCommand());
        databaseCommand.AddCommand(CreateDatabaseDropCommand());
        databaseCommand.AddCommand(CreateSeedCommand());
        databaseCommand.AddCommand(CreateValidateCommand());
        
        // Schema commands
        var schemaCommand = new Command("schema", "Schema management");
        schemaCommand.AddCommand(CreateDiffCommand());
        schemaCommand.AddCommand(CreateSyncCommand());
        
        rootCommand.AddCommand(migrationCommand);
        rootCommand.AddCommand(migrateCommand);
        rootCommand.AddCommand(databaseCommand);
        rootCommand.AddCommand(schemaCommand);
        
        return await rootCommand.InvokeAsync(args);
    }
}
```

### Add Migration Command

```csharp
private static Command CreateAddCommand()
{
    var command = new Command("add", "Create a new migration");
    
    var nameArgument = new Argument<string>("name", "Name of the migration");
    var outputOption = new Option<string?>("--output", "Output directory for migration files");
    var namespaceOption = new Option<string?>("--namespace", "Namespace for migration class");
    var dataOption = new Option<bool>("--data", "Create data migration");
    
    command.AddArgument(nameArgument);
    command.AddOption(outputOption);
    command.AddOption(namespaceOption);
    command.AddOption(dataOption);
    
    command.SetHandler(async (string name, string? output, string? ns, bool isData) =>
    {
        AnsiConsole.MarkupLine("[bold blue]Creating migration...[/]");
        
        var generator = new MigrationGenerator();
        var migration = await generator.GenerateAsync(name, output, ns, isData);
        
        AnsiConsole.MarkupLine($"[green]✓[/] Migration created: [cyan]{migration.FileName}[/]");
        AnsiConsole.MarkupLine($"  Location: [dim]{migration.FilePath}[/]");
        
    }, nameArgument, outputOption, namespaceOption, dataOption);
    
    return command;
}
```

### List Migrations Command

```csharp
private static Command CreateListCommand()
{
    var command = new Command("list", "List all migrations");
    
    var jsonOption = new Option<bool>("--json", "Output as JSON");
    var pendingOption = new Option<bool>("--pending", "Show only pending migrations");
    var appliedOption = new Option<bool>("--applied", "Show only applied migrations");
    
    command.AddOption(jsonOption);
    command.AddOption(pendingOption);
    command.AddOption(appliedOption);
    
    command.SetHandler(async (bool json, bool pending, bool applied) =>
    {
        var repository = new MigrationHistoryRepository();
        var appliedMigrations = await repository.GetAppliedMigrationsAsync();
        
        var scanner = new MigrationScanner();
        var allMigrations = scanner.ScanMigrations();
        
        var migrations = allMigrations.Select(m => new
        {
            Migration = m,
            IsApplied = appliedMigrations.Any(a => a.MigrationId == m.Id),
            AppliedAt = appliedMigrations.FirstOrDefault(a => a.MigrationId == m.Id)?.AppliedAt
        }).ToList();
        
        if (pending)
            migrations = migrations.Where(m => !m.IsApplied).ToList();
        if (applied)
            migrations = migrations.Where(m => m.IsApplied).ToList();
        
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(migrations, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            }));
        }
        else
        {
            PrintMigrationsTable(migrations);
        }
        
    }, jsonOption, pendingOption, appliedOption);
    
    return command;
}

private static void PrintMigrationsTable(List<dynamic> migrations)
{
    var table = new Table();
    table.AddColumn("Status");
    table.AddColumn("Migration ID");
    table.AddColumn("Name");
    table.AddColumn("Applied At");
    
    foreach (var m in migrations)
    {
        var status = m.IsApplied 
            ? "[green]✓ Applied[/]" 
            : "[yellow]○ Pending[/]";
        
        var appliedAt = m.AppliedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
        
        table.AddRow(
            status,
            m.Migration.Id,
            m.Migration.Name,
            appliedAt
        );
    }
    
    AnsiConsole.Write(table);
    
    var totalCount = migrations.Count;
    var appliedCount = migrations.Count(m => m.IsApplied);
    var pendingCount = totalCount - appliedCount;
    
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"Total: {totalCount}, Applied: [green]{appliedCount}[/], Pending: [yellow]{pendingCount}[/]");
}
```

### Migrate Up Command

```csharp
private static Command CreateUpCommand()
{
    var command = new Command("up", "Apply pending migrations");
    
    var targetOption = new Option<string?>("--target", "Target migration ID");
    var dryRunOption = new Option<bool>("--dry-run", "Preview changes without applying");
    var verboseOption = new Option<bool>("--verbose", "Show detailed output");
    
    command.AddOption(targetOption);
    command.AddOption(dryRunOption);
    command.AddOption(verboseOption);
    
    command.SetHandler(async (string? target, bool dryRun, bool verbose) =>
    {
        var executor = new MigrationExecutor();
        var scanner = new MigrationScanner();
        var migrations = scanner.ScanMigrations();
        
        var pendingMigrations = await executor.GetPendingMigrationsAsync(migrations);
        
        if (target != null)
        {
            pendingMigrations = pendingMigrations
                .Where(m => string.Compare(m.Id, target) <= 0)
                .ToList();
        }
        
        if (!pendingMigrations.Any())
        {
            AnsiConsole.MarkupLine("[green]✓[/] Database is up to date");
            return;
        }
        
        if (dryRun)
        {
            AnsiConsole.MarkupLine("[yellow]Dry run mode - no changes will be applied[/]");
            AnsiConsole.WriteLine();
        }
        
        AnsiConsole.MarkupLine($"[bold]Migrations to apply:[/] {pendingMigrations.Count}");
        foreach (var migration in pendingMigrations)
        {
            AnsiConsole.MarkupLine($"  • [cyan]{migration.Name}[/]");
        }
        
        if (!dryRun)
        {
            AnsiConsole.WriteLine();
            if (!AnsiConsole.Confirm("Apply these migrations?"))
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
                return;
            }
        }
        
        AnsiConsole.WriteLine();
        
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[blue]Applying migrations...[/]");
                task.MaxValue = pendingMigrations.Count;
                
                foreach (var migration in pendingMigrations)
                {
                    task.Description = $"[blue]Applying {migration.Name}...[/]";
                    
                    if (!dryRun)
                    {
                        await executor.UpAsync(migration, verbose);
                    }
                    
                    task.Increment(1);
                    
                    var status = dryRun ? "[dim](would apply)[/]" : "[green]✓[/]";
                    AnsiConsole.MarkupLine($"{status} {migration.Name}");
                }
            });
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]✓ Migrations completed successfully[/]");
        
    }, targetOption, dryRunOption, verboseOption);
    
    return command;
}
```

### Schema Diff Command

```csharp
private static Command CreateDiffCommand()
{
    var command = new Command("diff", "Compare database schema with entity definitions");
    
    var outputOption = new Option<string?>("--output", "Output file for diff report");
    var jsonOption = new Option<bool>("--json", "Output as JSON");
    
    command.AddOption(outputOption);
    command.AddOption(jsonOption);
    
    command.SetHandler(async (string? output, bool json) =>
    {
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Analyzing schema...", ctx =>
            {
                ctx.Status("Reading database schema...");
                var dbSchema = ReadDatabaseSchema();
                
                ctx.Status("Generating schema from entities...");
                var entitySchema = GenerateEntitySchema();
                
                ctx.Status("Comparing schemas...");
                var comparer = new SchemaComparer();
                var differences = comparer.Compare(dbSchema, entitySchema);
                
                if (json)
                {
                    var jsonOutput = JsonSerializer.Serialize(differences, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    
                    if (output != null)
                        File.WriteAllText(output, jsonOutput);
                    else
                        Console.WriteLine(jsonOutput);
                }
                else
                {
                    var report = new SchemaReport(differences);
                    var reportText = report.ToString();
                    
                    if (output != null)
                        File.WriteAllText(output, reportText);
                    else
                        AnsiConsole.Write(new Panel(reportText).Header("Schema Comparison"));
                }
            });
        
    }, outputOption, jsonOption);
    
    return command;
}
```

## Configuration File

```json
{
  "migrations": {
    "directory": "./Migrations",
    "namespace": "MyApp.Data.Migrations",
    "template": "default",
    "historyTable": "__MigrationsHistory"
  },
  "database": {
    "provider": "SqlServer",
    "connectionString": "Server=localhost;Database=MyDB;Trusted_Connection=True;",
    "commandTimeout": 30,
    "lockTimeout": 300
  },
  "output": {
    "verbose": false,
    "colored": true,
    "format": "table"
  },
  "seedData": {
    "directory": "./Seeds",
    "environment": "Development"
  }
}
```

## CLI Output Examples

### Migration Status Output
```
Migration Status
================
Database: ProductionDB
Provider: SQL Server
Connection: Server=prod.example.com;Database=ProductionDB

Total Migrations: 15
✓ Applied: 12
○ Pending: 3

Last Applied: 20251119143000_AddOrderItems
Applied At: 2025-11-19 14:35:22
Applied By: john.doe@example.com

Pending Migrations:
  • 20251119150000_AddProductCategories
  • 20251119151000_AddOrderStatuses
  • 20251119152000_AddUserPreferences
```

### Migration List Output
```
┌────────┬─────────────────────────────┬─────────────────────────────┬─────────────────────┐
│ Status │ Migration ID                 │ Name                        │ Applied At          │
├────────┼─────────────────────────────┼─────────────────────────────┼─────────────────────┤
│ ✓      │ 20251119140000_InitialCreate │ Initial Create              │ 2025-11-19 14:00:00 │
│ ✓      │ 20251119141000_AddUsers      │ Add Users                   │ 2025-11-19 14:10:00 │
│ ✓      │ 20251119142000_AddProducts   │ Add Products                │ 2025-11-19 14:20:00 │
│ ○      │ 20251119150000_AddCategories │ Add Categories              │ -                   │
│ ○      │ 20251119151000_AddOrders     │ Add Orders                  │ -                   │
└────────┴─────────────────────────────┴─────────────────────────────┴─────────────────────┘

Total: 5, Applied: 3, Pending: 2
```

## Acceptance Criteria
- [ ] All core CLI commands implemented
- [ ] Interactive prompts for destructive operations
- [ ] Rich console output with colors
- [ ] Progress indicators for long operations
- [ ] JSON output support for automation
- [ ] Configuration file support
- [ ] Comprehensive help documentation
- [ ] Cross-platform compatibility
- [ ] Error messages with actionable guidance
- [ ] Dry-run mode for preview

## Dependencies
- Phase 8.1: Schema Generation from Entities
- Phase 8.2: Migration Generation
- Phase 8.3: Migration Execution Engine
- Phase 8.4: Schema Comparison and Diff
- Phase 8.5: Data Migration Support

## Testing Requirements
- Unit tests for all commands
- Integration tests with real databases
- Test interactive prompts
- Test output formatting
- Test error scenarios
- Test configuration loading
- Test cross-platform compatibility

## Required NuGet Packages
- `System.CommandLine` - CLI framework
- `Spectre.Console` - Rich console output
- `Microsoft.Extensions.Configuration` - Configuration management

## Documentation
- CLI command reference
- Configuration guide
- Output format examples
- Interactive mode guide
- Automation guide (CI/CD integration)
- Troubleshooting guide
