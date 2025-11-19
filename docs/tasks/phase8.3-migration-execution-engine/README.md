# Phase 8.3: Migration Execution Engine

## Overview
Execute migrations against databases, manage transaction boundaries, handle errors, and ensure database schema stays synchronized with entity definitions.

## Objectives
- Execute migrations in correct order
- Transaction management for migrations
- Rollback support on failures
- Concurrent migration protection
- Migration validation before execution

## Tasks

### 1. Migration Executor
- [ ] Create `MigrationExecutor` to run migrations
- [ ] Implement transaction management
- [ ] Support batch execution
- [ ] Handle execution errors and rollback
- [ ] Provide execution progress reporting

### 2. Migration Discovery
- [ ] Scan assemblies for migration classes
- [ ] Order migrations by version/timestamp
- [ ] Validate migration consistency
- [ ] Detect missing migrations
- [ ] Handle migration dependencies

### 3. Database Lock Mechanism
- [ ] Implement advisory locks to prevent concurrent migrations
- [ ] Support distributed lock for multi-instance scenarios
- [ ] Timeout handling for locks
- [ ] Lock cleanup on failures

### 4. Migration Validation
- [ ] Validate migration history consistency
- [ ] Checksum verification
- [ ] Detect schema drift
- [ ] Pre-execution validation
- [ ] Post-execution verification

### 5. SQL Generation from Operations
- [ ] Generate SQL from `MigrationOperation` objects
- [ ] Support all database providers
- [ ] Handle provider-specific syntax
- [ ] Optimize SQL statement ordering
- [ ] Support custom SQL execution

## Example Usage

### Executing Migrations

```csharp
using NPA.Migrations;

// Setup
var migrationExecutor = new MigrationExecutor(connection, providerType);

// Execute all pending migrations
var result = await migrationExecutor.ExecuteAsync();

Console.WriteLine($"Executed {result.MigrationsApplied} migrations");
Console.WriteLine($"Time taken: {result.Duration}");

// Execute specific migration
await migrationExecutor.ExecuteAsync("20251119143022_AddProductDescription");

// Rollback last migration
await migrationExecutor.RollbackAsync();

// Rollback to specific migration
await migrationExecutor.RollbackToAsync("20251119120000_InitialCreate");
```

### Migration Executor Configuration

```csharp
var options = new MigrationExecutorOptions
{
    TransactionMode = TransactionMode.PerMigration, // or AllInOne, None
    TimeoutSeconds = 300,
    ValidateChecksums = true,
    EnsureCreated = true, // Create database if not exists
    AcquireLock = true,
    LockTimeoutSeconds = 30,
    OnProgress = (migration, step) => 
    {
        Console.WriteLine($"Executing: {migration.Name} - {step}");
    }
};

var executor = new MigrationExecutor(connection, provider, options);
```

### CLI Integration

```bash
# Apply all pending migrations
dotnet npa migrate up

# Output:
# Acquiring migration lock...
# ✓ Lock acquired
# 
# Pending migrations:
#   1. 20251119143022_AddProductDescription
#   2. 20251119150000_AddUserRoles
# 
# Executing migration 1/2: AddProductDescription
#   ✓ Adding column 'description'
#   ✓ Creating index 'IX_products_description'
# 
# Executing migration 2/2: AddUserRoles
#   ✓ Creating table 'user_roles'
#   ✓ Adding foreign key 'FK_user_roles_users'
# 
# ✓ 2 migrations applied successfully
# Time: 1.234s

# Rollback last migration
dotnet npa migrate down

# Rollback to specific version
dotnet npa migrate down --target 20251119120000_InitialCreate

# List migration status
dotnet npa migrate status

# Output:
# Database: ProductionDB
# Current Version: 20251119150000_AddUserRoles
# 
# Applied Migrations:
#   ✓ 20251119120000_InitialCreate (2025-11-19 12:00:00)
#   ✓ 20251119143022_AddProductDescription (2025-11-19 14:30:22)
#   ✓ 20251119150000_AddUserRoles (2025-11-19 15:00:00)
# 
# Pending Migrations:
#   □ 20251119160000_AddOrderTracking
#   □ 20251119170000_AddPaymentMethods
```

## Migration Executor Implementation

```csharp
public class MigrationExecutor
{
    private readonly IDbConnection _connection;
    private readonly IDatabaseProvider _provider;
    private readonly MigrationExecutorOptions _options;
    
    public async Task<MigrationResult> ExecuteAsync(CancellationToken ct = default)
    {
        // 1. Ensure migrations history table exists
        await EnsureMigrationHistoryTableAsync();
        
        // 2. Acquire lock to prevent concurrent migrations
        using var migrationLock = await AcquireMigrationLockAsync();
        
        // 3. Discover all migrations
        var allMigrations = DiscoverMigrations();
        
        // 4. Get applied migrations
        var appliedMigrations = await GetAppliedMigrationsAsync();
        
        // 5. Calculate pending migrations
        var pendingMigrations = GetPendingMigrations(allMigrations, appliedMigrations);
        
        // 6. Validate checksums of applied migrations
        await ValidateChecksumsAsync(appliedMigrations);
        
        // 7. Execute pending migrations
        var result = new MigrationResult();
        foreach (var migration in pendingMigrations)
        {
            try
            {
                await ExecuteMigrationAsync(migration, MigrationDirection.Up);
                result.MigrationsApplied++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new MigrationError(migration.Id, ex));
                
                if (_options.StopOnError)
                    break;
            }
        }
        
        return result;
    }
    
    private async Task ExecuteMigrationAsync(MigrationInfo migration, MigrationDirection direction)
    {
        using var transaction = _options.TransactionMode == TransactionMode.PerMigration
            ? _connection.BeginTransaction()
            : null;
        
        try
        {
            _options.OnProgress?.Invoke(migration, "Starting");
            
            // Create migration instance
            var migrationInstance = CreateMigrationInstance(migration);
            var builder = new MigrationBuilder(_provider);
            
            // Execute Up or Down
            if (direction == MigrationDirection.Up)
                migrationInstance.Up(builder);
            else
                migrationInstance.Down(builder);
            
            // Generate and execute SQL
            var operations = builder.GetOperations();
            foreach (var operation in operations)
            {
                var sql = _provider.GenerateSql(operation);
                await _connection.ExecuteAsync(sql, transaction: transaction);
            }
            
            // Update migration history
            if (direction == MigrationDirection.Up)
                await RecordMigrationAsync(migration, transaction);
            else
                await RemoveMigrationAsync(migration, transaction);
            
            transaction?.Commit();
            
            _options.OnProgress?.Invoke(migration, "Completed");
        }
        catch
        {
            transaction?.Rollback();
            throw;
        }
    }
}
```

## Transaction Modes

```csharp
public enum TransactionMode
{
    /// <summary>
    /// Each migration executes in its own transaction
    /// </summary>
    PerMigration,
    
    /// <summary>
    /// All migrations execute in a single transaction (all or nothing)
    /// </summary>
    AllInOne,
    
    /// <summary>
    /// No transaction (each statement auto-commits)
    /// </summary>
    None
}
```

## Lock Mechanism

```csharp
public interface IMigrationLock : IDisposable
{
    Task<bool> TryAcquireAsync(TimeSpan timeout);
    Task ReleaseAsync();
}

// SQL Server implementation using sp_getapplock
public class SqlServerMigrationLock : IMigrationLock
{
    public async Task<bool> TryAcquireAsync(TimeSpan timeout)
    {
        var result = await _connection.ExecuteScalarAsync<int>(
            "EXEC sp_getapplock @Resource, @LockMode, @LockOwner, @LockTimeout",
            new 
            {
                Resource = "NPA_Migrations",
                LockMode = "Exclusive",
                LockOwner = "Session",
                LockTimeout = (int)timeout.TotalMilliseconds
            });
        
        return result >= 0; // 0 or positive means success
    }
}

// PostgreSQL implementation using advisory locks
public class PostgreSqlMigrationLock : IMigrationLock
{
    public async Task<bool> TryAcquireAsync(TimeSpan timeout)
    {
        return await _connection.ExecuteScalarAsync<bool>(
            "SELECT pg_try_advisory_lock(1234567890)");
    }
}
```

## Migration History Queries

```csharp
public class MigrationHistoryRepository
{
    public async Task<List<AppliedMigration>> GetAppliedMigrationsAsync()
    {
        const string sql = @"
            SELECT MigrationId, ProductVersion, AppliedDate, Checksum
            FROM __MigrationsHistory
            ORDER BY MigrationId";
        
        return (await _connection.QueryAsync<AppliedMigration>(sql)).ToList();
    }
    
    public async Task RecordMigrationAsync(string migrationId, string checksum)
    {
        const string sql = @"
            INSERT INTO __MigrationsHistory (MigrationId, ProductVersion, AppliedDate, Checksum)
            VALUES (@MigrationId, @ProductVersion, @AppliedDate, @Checksum)";
        
        await _connection.ExecuteAsync(sql, new
        {
            MigrationId = migrationId,
            ProductVersion = GetProductVersion(),
            AppliedDate = DateTime.UtcNow,
            Checksum = checksum
        });
    }
    
    public async Task RemoveMigrationAsync(string migrationId)
    {
        const string sql = "DELETE FROM __MigrationsHistory WHERE MigrationId = @MigrationId";
        await _connection.ExecuteAsync(sql, new { MigrationId = migrationId });
    }
}
```

## Error Handling

```csharp
public class MigrationResult
{
    public int MigrationsApplied { get; set; }
    public TimeSpan Duration { get; set; }
    public List<MigrationError> Errors { get; set; } = new();
    public bool Success => !Errors.Any();
}

public class MigrationError
{
    public string MigrationId { get; set; }
    public Exception Exception { get; set; }
    public string Message { get; set; }
    public string StackTrace { get; set; }
}

public class MigrationException : Exception
{
    public string MigrationId { get; }
    
    public MigrationException(string migrationId, string message, Exception? inner = null)
        : base(message, inner)
    {
        MigrationId = migrationId;
    }
}
```

## Acceptance Criteria
- [ ] Migrations execute in correct order
- [ ] Transaction boundaries properly managed
- [ ] Concurrent migration execution prevented
- [ ] Failed migrations properly rolled back
- [ ] Migration history accurately maintained
- [ ] Checksums validated before execution
- [ ] Progress reporting works correctly
- [ ] All database providers supported
- [ ] Rollback functionality works
- [ ] Error messages are clear and actionable

## Dependencies
- Phase 8.1: Schema Generation from Entities
- Phase 8.2: Migration Generation
- Existing: NPA.Migrations project
- Existing: Database provider implementations

## Testing Requirements
- Unit tests for migration executor logic
- Unit tests for lock mechanism
- Integration tests for each database provider
- Tests for transaction modes
- Tests for rollback scenarios
- Concurrent execution tests
- Error handling tests
- Performance tests for large migrations

## Documentation
- Migration execution guide
- Transaction mode selection guide
- Lock mechanism explanation
- Error handling and recovery
- Performance optimization tips
- Troubleshooting common issues
