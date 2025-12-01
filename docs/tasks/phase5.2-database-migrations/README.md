# Phase 5.2: Database Migrations

## 📋 Task Overview

**Objective**: Implement a comprehensive database migration system that allows developers to version and manage database schema changes over time.

**Priority**: Medium  
**Estimated Time**: 4-5 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.6, Phase 3.1-3.5, Phase 4.1-4.6, Phase 5.1 (All previous phases)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] IMigration interface is complete
- [ ] MigrationRunner class is implemented
- [ ] Migration generation works
- [ ] Migration execution works
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

## 📝 Detailed Requirements

### 1. IMigration Interface
- **Purpose**: Defines the contract for database migrations
- **Methods**:
  - `Task UpAsync(IDbConnection connection)` - Apply migration
  - `Task DownAsync(IDbConnection connection)` - Rollback migration
  - `string Name` - Migration name
  - `int Version` - Migration version
  - `DateTime CreatedAt` - Creation timestamp
  - `string Description` - Migration description

### 2. MigrationRunner Class
- **Purpose**: Executes and manages database migrations
- **Methods**:
  - `Task RunMigrationsAsync()` - Run all pending migrations
  - `Task RollbackMigrationAsync(int version)` - Rollback to specific version
  - `Task<List<MigrationInfo>> GetPendingMigrationsAsync()` - Get pending migrations
- **Features**:
  - Migration tracking
  - Version management
  - Error handling
  - Transaction support

### 3. Migration Generation
- **Code Generation**: Generate migration classes from entity changes
- **Schema Comparison**: Compare current schema with target schema
- **SQL Generation**: Generate SQL for schema changes
- **Validation**: Validate migration before execution

### 4. Migration Types
- **CreateTable**: Create new tables
- **DropTable**: Drop existing tables
- **AlterTable**: Modify existing tables
- **CreateIndex**: Create database indexes
- **DropIndex**: Drop database indexes
- **AddColumn**: Add new columns
- **DropColumn**: Remove columns
- **AlterColumn**: Modify existing columns
- **Custom**: Custom SQL migrations

### 5. Migration Management
- **Version Control**: Track migration versions
- **Dependency Management**: Handle migration dependencies
- **Rollback Support**: Support for rolling back migrations
- **Validation**: Validate migrations before execution

## 🏗️ Implementation Plan

### Step 1: Create Migration Interfaces
1. Create `IMigration` interface
2. Create `IMigrationRunner` interface
3. Create `IMigrationGenerator` interface
4. Create `IMigrationValidator` interface

### Step 2: Implement Core Classes
1. Create `Migration` base class
2. Create `MigrationRunner` class
3. Create `MigrationGenerator` class
4. Create `MigrationValidator` class

### Step 3: Implement Migration Types
1. Create `CreateTableMigration` class
2. Create `DropTableMigration` class
3. Create `AlterTableMigration` class
4. Create `CreateIndexMigration` class
5. Create `CustomMigration` class

### Step 4: Add Migration Generation
1. Implement schema comparison
2. Implement SQL generation
3. Implement migration class generation
4. Add validation

### Step 5: Add Migration Execution
1. Implement migration running
2. Implement rollback support
3. Add transaction support
4. Add error handling

### Step 6: Create Unit Tests
1. Test migration interfaces
2. Test migration execution
3. Test migration generation
4. Test error scenarios

### Step 7: Add Documentation
1. XML documentation comments
2. Usage examples
3. Migration guide
4. Best practices

## 📁 File Structure

```
src/NPA.Core/Migrations/
├── IMigration.cs
├── Migration.cs
├── IMigrationRunner.cs
├── MigrationRunner.cs
├── IMigrationGenerator.cs
├── MigrationGenerator.cs
├── IMigrationValidator.cs
├── MigrationValidator.cs
├── MigrationInfo.cs
├── MigrationContext.cs
└── MigrationTypes/
    ├── CreateTableMigration.cs
    ├── DropTableMigration.cs
    ├── AlterTableMigration.cs
    ├── CreateIndexMigration.cs
    ├── DropIndexMigration.cs
    ├── AddColumnMigration.cs
    ├── DropColumnMigration.cs
    ├── AlterColumnMigration.cs
    └── CustomMigration.cs

tests/NPA.Core.Tests/Migrations/
├── MigrationTests.cs
├── MigrationRunnerTests.cs
├── MigrationGeneratorTests.cs
├── MigrationValidatorTests.cs
└── MigrationTypeTests/
    ├── CreateTableMigrationTests.cs
    ├── DropTableMigrationTests.cs
    ├── AlterTableMigrationTests.cs
    └── CustomMigrationTests.cs
```

## 💻 Code Examples

### IMigration Interface
```csharp
public interface IMigration
{
    string Name { get; }
    int Version { get; }
    DateTime CreatedAt { get; }
    string Description { get; }
    Task UpAsync(IDbConnection connection);
    Task DownAsync(IDbConnection connection);
}
```

### Migration Base Class
```csharp
public abstract class Migration : IMigration
{
    public abstract string Name { get; }
    public abstract int Version { get; }
    public abstract DateTime CreatedAt { get; }
    public abstract string Description { get; }
    
    public abstract Task UpAsync(IDbConnection connection);
    public abstract Task DownAsync(IDbConnection connection);
    
    protected virtual void Log(string message)
    {
        Console.WriteLine($"[Migration {Name}] {message}");
    }
    
    protected virtual async Task ExecuteSqlAsync(IDbConnection connection, string sql)
    {
        Log($"Executing SQL: {sql}");
        await connection.ExecuteAsync(sql);
    }
}
```

### CreateTableMigration Class
```csharp
public class CreateTableMigration : Migration
{
    private readonly EntityMetadata _entityMetadata;
    
    public CreateTableMigration(EntityMetadata entityMetadata)
    {
        _entityMetadata = entityMetadata ?? throw new ArgumentNullException(nameof(entityMetadata));
    }
    
    public override string Name => $"CreateTable_{_entityMetadata.TableName}";
    public override int Version => GenerateVersion();
    public override DateTime CreatedAt => DateTime.UtcNow;
    public override string Description => $"Create table {_entityMetadata.TableName}";
    
    public override async Task UpAsync(IDbConnection connection)
    {
        var sql = GenerateCreateTableSql();
        await ExecuteSqlAsync(connection, sql);
        
        // Create indexes
        foreach (var index in _entityMetadata.Indexes)
        {
            var indexSql = GenerateCreateIndexSql(index);
            await ExecuteSqlAsync(connection, indexSql);
        }
    }
    
    public override async Task DownAsync(IDbConnection connection)
    {
        var sql = $"DROP TABLE IF EXISTS {_entityMetadata.TableName}";
        await ExecuteSqlAsync(connection, sql);
    }
    
    private string GenerateCreateTableSql()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE {_entityMetadata.TableName} (");
        
        var columns = new List<string>();
        foreach (var property in _entityMetadata.Properties)
        {
            var columnDef = GenerateColumnDefinition(property);
            columns.Add($"    {columnDef}");
        }
        
        sb.AppendLine(string.Join(",\n", columns));
        sb.AppendLine(")");
        
        return sb.ToString();
    }
    
    private string GenerateColumnDefinition(PropertyMetadata property)
    {
        var sb = new StringBuilder();
        sb.Append($"{property.ColumnName} {GetSqlType(property)}");
        
        if (property.IsPrimaryKey)
        {
            sb.Append(" PRIMARY KEY");
        }
        
        if (property.IsIdentity)
        {
            sb.Append(" IDENTITY(1,1)");
        }
        
        if (!property.IsNullable)
        {
            sb.Append(" NOT NULL");
        }
        
        if (property.IsUnique)
        {
            sb.Append(" UNIQUE");
        }
        
        return sb.ToString();
    }
    
    private string GetSqlType(PropertyMetadata property)
    {
        return property.Type switch
        {
            Type t when t == typeof(string) => $"NVARCHAR({property.Length ?? 255})",
            Type t when t == typeof(int) => "INT",
            Type t when t == typeof(long) => "BIGINT",
            Type t when t == typeof(decimal) => "DECIMAL(18,2)",
            Type t when t == typeof(DateTime) => "DATETIME2",
            Type t when t == typeof(bool) => "BIT",
            _ => "NVARCHAR(MAX)"
        };
    }
}
```

### MigrationRunner Class
```csharp
public class MigrationRunner : IMigrationRunner
{
    private readonly IDbConnection _connection;
    private readonly IList<IMigration> _migrations;
    private readonly IMigrationValidator _validator;
    
    public MigrationRunner(IDbConnection connection, IList<IMigration> migrations, IMigrationValidator validator)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _migrations = migrations ?? throw new ArgumentNullException(nameof(migrations));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }
    
    public async Task RunMigrationsAsync()
    {
        await EnsureMigrationTableExistsAsync();
        
        var executedMigrations = await GetExecutedMigrationsAsync();
        var pendingMigrations = _migrations
            .Where(m => !executedMigrations.Contains(m.Version))
            .OrderBy(m => m.Version)
            .ToList();
        
        foreach (var migration in pendingMigrations)
        {
            await RunMigrationAsync(migration);
        }
    }
    
    public async Task RollbackMigrationAsync(int version)
    {
        await EnsureMigrationTableExistsAsync();
        
        var executedMigrations = await GetExecutedMigrationsAsync();
        var migrationsToRollback = _migrations
            .Where(m => m.Version > version && executedMigrations.Contains(m.Version))
            .OrderByDescending(m => m.Version)
            .ToList();
        
        foreach (var migration in migrationsToRollback)
        {
            await RollbackMigrationAsync(migration);
        }
    }
    
    public async Task<List<MigrationInfo>> GetPendingMigrationsAsync()
    {
        await EnsureMigrationTableExistsAsync();
        
        var executedMigrations = await GetExecutedMigrationsAsync();
        return _migrations
            .Where(m => !executedMigrations.Contains(m.Version))
            .OrderBy(m => m.Version)
            .Select(m => new MigrationInfo
            {
                Name = m.Name,
                Version = m.Version,
                CreatedAt = m.CreatedAt,
                Description = m.Description
            })
            .ToList();
    }
    
    private async Task RunMigrationAsync(IMigration migration)
    {
        using var transaction = _connection.BeginTransaction();
        try
        {
            await _validator.ValidateMigrationAsync(migration);
            await migration.UpAsync(_connection);
            await RecordMigrationAsync(migration, transaction);
            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new MigrationException($"Failed to run migration {migration.Name}", ex);
        }
    }
    
    private async Task RollbackMigrationAsync(IMigration migration)
    {
        using var transaction = _connection.BeginTransaction();
        try
        {
            await migration.DownAsync(_connection);
            await RemoveMigrationRecordAsync(migration, transaction);
            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new MigrationException($"Failed to rollback migration {migration.Name}", ex);
        }
    }
    
    private async Task EnsureMigrationTableExistsAsync()
    {
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='__Migrations' AND xtype='U')
            CREATE TABLE __Migrations (
                Version INT PRIMARY KEY,
                Name NVARCHAR(255) NOT NULL,
                CreatedAt DATETIME2 NOT NULL,
                ExecutedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            )";
        
        await _connection.ExecuteAsync(sql);
    }
    
    private async Task<HashSet<int>> GetExecutedMigrationsAsync()
    {
        var sql = "SELECT Version FROM __Migrations ORDER BY Version";
        var versions = await _connection.QueryAsync<int>(sql);
        return new HashSet<int>(versions);
    }
    
    private async Task RecordMigrationAsync(IMigration migration, IDbTransaction transaction)
    {
        var sql = @"
            INSERT INTO __Migrations (Version, Name, CreatedAt, ExecutedAt)
            VALUES (@Version, @Name, @CreatedAt, @ExecutedAt)";
        
        await _connection.ExecuteAsync(sql, new
        {
            Version = migration.Version,
            Name = migration.Name,
            CreatedAt = migration.CreatedAt,
            ExecutedAt = DateTime.UtcNow
        }, transaction);
    }
    
    private async Task RemoveMigrationRecordAsync(IMigration migration, IDbTransaction transaction)
    {
        var sql = "DELETE FROM __Migrations WHERE Version = @Version";
        await _connection.ExecuteAsync(sql, new { Version = migration.Version }, transaction);
    }
}
```

### MigrationGenerator Class
```csharp
public class MigrationGenerator : IMigrationGenerator
{
    private readonly IMetadataProvider _metadataProvider;
    private readonly ISqlGenerator _sqlGenerator;
    
    public MigrationGenerator(IMetadataProvider metadataProvider, ISqlGenerator sqlGenerator)
    {
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
        _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
    }
    
    public async Task<IMigration> GenerateMigrationAsync<T>(MigrationType type)
    {
        var metadata = _metadataProvider.GetEntityMetadata<T>();
        
        return type switch
        {
            MigrationType.CreateTable => new CreateTableMigration(metadata),
            MigrationType.DropTable => new DropTableMigration(metadata),
            MigrationType.AlterTable => new AlterTableMigration(metadata),
            _ => throw new NotSupportedException($"Migration type {type} is not supported")
        };
    }
    
    public async Task<IMigration> GenerateMigrationFromSqlAsync(string name, string upSql, string downSql)
    {
        return new CustomMigration(name, upSql, downSql);
    }
    
    public async Task<List<IMigration>> GenerateMigrationsFromEntitiesAsync(IEnumerable<Type> entityTypes)
    {
        var migrations = new List<IMigration>();
        
        foreach (var entityType in entityTypes)
        {
            var metadata = _metadataProvider.GetEntityMetadata(entityType);
            migrations.Add(new CreateTableMigration(metadata));
        }
        
        return migrations;
    }
}
```

### Usage Examples
```csharp
// Migration configuration
services.AddNPA(config =>
{
    config.ConnectionString = "Server=localhost;Database=MyApp;";
    config.EnableMigrations = true;
    config.MigrationAssembly = typeof(Startup).Assembly;
});

// Generate migration from entity
[Entity]
[Table("users")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("username", Length = 50)]
    public string Username { get; set; }
    
    [Column("email", Length = 100)]
    public string Email { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

// Custom migration
public class AddUserIndexMigration : Migration
{
    public override string Name => "AddUserIndex";
    public override int Version => 2024010101;
    public override DateTime CreatedAt => new DateTime(2024, 1, 1);
    public override string Description => "Add index on users.email";
    
    public override async Task UpAsync(IDbConnection connection)
    {
        var sql = "CREATE INDEX IX_Users_Email ON users (email)";
        await ExecuteSqlAsync(connection, sql);
    }
    
    public override async Task DownAsync(IDbConnection connection)
    {
        var sql = "DROP INDEX IX_Users_Email ON users";
        await ExecuteSqlAsync(connection, sql);
    }
}

// Run migrations
public class MigrationService
{
    private readonly IMigrationRunner _migrationRunner;
    
    public MigrationService(IMigrationRunner migrationRunner)
    {
        _migrationRunner = migrationRunner;
    }
    
    public async Task RunMigrationsAsync()
    {
        await _migrationRunner.RunMigrationsAsync();
    }
    
    public async Task RollbackToVersionAsync(int version)
    {
        await _migrationRunner.RollbackMigrationAsync(version);
    }
    
    public async Task<List<MigrationInfo>> GetPendingMigrationsAsync()
    {
        return await _migrationRunner.GetPendingMigrationsAsync();
    }
}
```

## 🧪 Test Cases

### Migration Interface Tests
- [ ] Migration base class functionality
- [ ] Custom migration implementation
- [ ] Migration validation
- [ ] Error handling

### MigrationRunner Tests
- [ ] Run migrations successfully
- [ ] Rollback migrations successfully
- [ ] Handle migration errors
- [ ] Transaction support
- [ ] Version tracking

### MigrationGenerator Tests
- [ ] Generate migrations from entities
- [ ] Generate custom migrations
- [ ] SQL generation
- [ ] Validation

### Migration Type Tests
- [ ] Create table migrations
- [ ] Drop table migrations
- [ ] Alter table migrations
- [ ] Index migrations
- [ ] Custom SQL migrations

### Integration Tests
- [ ] End-to-end migration flow
- [ ] Database schema changes
- [ ] Rollback functionality
- [ ] Error recovery

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Basic migration operations
- [ ] Migration generation
- [ ] Migration execution
- [ ] Rollback procedures
- [ ] Best practices

### Migration Guide
- [ ] Migration types
- [ ] Migration patterns
- [ ] Schema management
- [ ] Version control
- [ ] Common scenarios

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
1. Move to Phase 5.3: Performance Monitoring
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on migration types
- [ ] Performance considerations for migrations
- [ ] Integration with existing features
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
