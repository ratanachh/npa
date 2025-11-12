# NPA CLI - Code Generation Tools

Command-line interface for NPA ORM project management and code generation.

## Installation

```bash
dotnet tool install --global npa
```

Or run from source:
```bash
dotnet run --project tools/NPA.CLI/NPA.CLI.csproj -- <command> [options]
```

## Commands

### `npa new` - Create New Project

Create a new project from a template.

```bash
npa new <template> <name> [options]
```

**Arguments:**
- `template` - Project template (console, webapi, classlib)
- `name` - Project name

**Options:**
- `-o, --output <path>` - Output directory (default: current directory)
- `-db, --database <provider>` - Database provider (sqlserver, postgresql, mysql, sqlite)
- `-cs, --connection-string <string>` - Database connection string
- `-s, --with-samples` - Include sample code

**Examples:**
```bash
# Create console app with SQL Server
npa new console MyApp --database sqlserver

# Create Web API with PostgreSQL and samples
npa new webapi MyWebApi --database postgresql --with-samples

# Create class library
npa new classlib MyLibrary
```

### `npa generate` - Generate Code

Generate code from templates.

```bash
npa generate <type> <name> [options]
```

**Arguments:**
- `type` - Code type (entity, repository, migration, test)
- `name` - Name for generated code

**Options:**
- `-o, --output <path>` - Output directory
- `-n, --namespace <namespace>` - Namespace for generated code
- `-t, --table <table>` - Table name (for entities)
- `-f, --force` - Force overwrite existing files

**Examples:**
```bash
# Generate entity
npa generate entity Product --namespace MyApp.Entities --table products

# Generate repository
npa generate repository User --namespace MyApp.Repositories --output ./Repositories

# Generate migration
npa generate migration AddProductTable --namespace MyApp.Migrations

# Generate test with force overwrite
npa generate test UserRepository --namespace MyApp.Tests --force
```

### `npa config` - Manage Configuration

Manage NPA configuration files.

```bash
npa config <action> [options]
```

**Arguments:**
- `action` - Configuration action (init, validate, show)

**Options:**
- `-o, --output <path>` - Output directory

**Examples:**
```bash
# Initialize new configuration
npa config init

# Validate existing configuration
npa config validate

# Show current configuration
npa config show
```

### `npa version` - Version Information

Display version and help information.

```bash
npa version
```

## Configuration File

The `npa.config.json` file stores project configuration:

```json
{
  "ConnectionString": "Server=localhost;Database=MyDb;Trusted_Connection=true;",
  "DatabaseProvider": "sqlserver",
  "MigrationsNamespace": "Migrations",
  "EntitiesNamespace": "Entities",
  "RepositoriesNamespace": "Repositories"
}
```

## Quick Start

```bash
# 1. Create a new Web API project
npa new webapi MyECommerce --database postgresql --with-samples

# 2. Navigate to project
cd MyECommerce

# 3. Generate entities
npa generate entity Product --namespace MyECommerce.Entities --table products
npa generate entity Customer --namespace MyECommerce.Entities --table customers

# 4. Generate repositories
npa generate repository Product --namespace MyECommerce.Repositories
npa generate repository Customer --namespace MyECommerce.Repositories

# 5. Build and run
dotnet restore
dotnet build
dotnet run
```

## Project Templates

### Console Application
- Basic console app with NPA integration
- Dependency injection setup
- Entity and Repository folders
- Optional sample entity

### Web API
- ASP.NET Core Web API
- Swagger/OpenAPI documentation
- Controllers, entities, repositories
- appsettings.json configuration
- Optional sample controller and entity

### Class Library
- Reusable library project
- Entity and repository structure
- NPA package references

## Code Templates

### Entity
```csharp
[Entity]
[Table("table_name")]
public class EntityName
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; }
}
```

### Repository
```csharp
[Repository]
public partial interface IEntityRepository : IRepository<Entity, long>
{
}

public partial class EntityRepository : IEntityRepository
{
    private readonly IEntityManager _entityManager;
    
    public EntityRepository(IEntityManager entityManager)
    {
        _entityManager = entityManager;
    }
}
```

### Migration
```csharp
public class MigrationMigration : IMigration
{
    public int Version => 20251112120000;
    public string Description => "Migration description";
    
    public async Task UpAsync(IMigrationContext context)
    {
        // Implementation
    }
    
    public async Task DownAsync(IMigrationContext context)
    {
        // Rollback
    }
}
```

### Test
```csharp
public class ClassNameTests
{
    [Fact]
    public void Test_Creation()
    {
        Assert.True(true);
    }
}
```

## Help

For detailed help on any command:
```bash
npa <command> --help
```

For general help:
```bash
npa --help
```

## Dependencies

- .NET 8.0 or higher
- NPA.Core packages
- System.CommandLine
- Microsoft.Extensions.Hosting

## License

Part of the NPA project. See main repository for license information.
