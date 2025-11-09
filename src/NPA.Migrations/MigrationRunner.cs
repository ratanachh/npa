using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NPA.Migrations;

/// <summary>
/// Executes and manages database migrations.
/// </summary>
public class MigrationRunner
{
    private readonly ILogger<MigrationRunner> _logger;
    private readonly List<IMigration> _migrations;
    private const string MigrationTableName = "__MigrationHistory";

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationRunner"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public MigrationRunner(ILogger<MigrationRunner> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _migrations = new List<IMigration>();
    }

    /// <summary>
    /// Registers a migration to be executed.
    /// </summary>
    /// <param name="migration">Migration to register.</param>
    public void RegisterMigration(IMigration migration)
    {
        if (migration == null)
            throw new ArgumentNullException(nameof(migration));

        if (_migrations.Any(m => m.Version == migration.Version))
            throw new InvalidOperationException($"Migration with version {migration.Version} is already registered.");

        _migrations.Add(migration);
        _logger.LogDebug("Registered migration: {Name} (v{Version})", migration.Name, migration.Version);
    }

    /// <summary>
    /// Registers multiple migrations.
    /// </summary>
    /// <param name="migrations">Migrations to register.</param>
    public void RegisterMigrations(IEnumerable<IMigration> migrations)
    {
        foreach (var migration in migrations)
        {
            RegisterMigration(migration);
        }
    }

    /// <summary>
    /// Runs all pending migrations.
    /// </summary>
    /// <param name="connection">Database connection.</param>
    /// <param name="useTransaction">Whether to use a transaction (default: true).</param>
    /// <returns>List of applied migration information.</returns>
    public async Task<List<MigrationInfo>> RunMigrationsAsync(
        IDbConnection connection,
        bool useTransaction = true)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        _logger.LogInformation("Starting migration process...");

        // Ensure migration history table exists
        await EnsureMigrationHistoryTableAsync(connection);

        // Get pending migrations
        var pendingMigrations = await GetPendingMigrationsAsync(connection);

        if (!pendingMigrations.Any())
        {
            _logger.LogInformation("No pending migrations to apply.");
            return new List<MigrationInfo>();
        }

        _logger.LogInformation("Found {Count} pending migration(s)", pendingMigrations.Count);

        var appliedMigrations = new List<MigrationInfo>();

        foreach (var migration in pendingMigrations)
        {
            var info = await RunMigrationAsync(connection, migration, useTransaction);
            appliedMigrations.Add(info);

            if (!info.IsSuccessful)
            {
                _logger.LogError("Migration failed: {Name}. Stopping migration process.", migration.Name);
                break;
            }
        }

        _logger.LogInformation("Migration process completed. Applied {Count} migration(s).", 
            appliedMigrations.Count(m => m.IsSuccessful));

        return appliedMigrations;
    }

    /// <summary>
    /// Runs a specific migration.
    /// </summary>
    /// <param name="connection">Database connection.</param>
    /// <param name="migration">Migration to run.</param>
    /// <param name="useTransaction">Whether to use a transaction.</param>
    /// <returns>Migration information.</returns>
    private async Task<MigrationInfo> RunMigrationAsync(
        IDbConnection connection,
        IMigration migration,
        bool useTransaction)
    {
        var info = new MigrationInfo
        {
            Name = migration.Name,
            Version = migration.Version,
            Description = migration.Description,
            CreatedAt = migration.CreatedAt
        };

        var sw = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Applying migration: {Name} (v{Version})", migration.Name, migration.Version);

            // Set logger if migration is a Migration base class
            if (migration is Migration migrationBase)
            {
                migrationBase.SetLogger(_logger);
            }

            if (useTransaction)
            {
                using var transaction = connection.BeginTransaction();
                try
                {
                    await migration.UpAsync(connection);
                    await RecordMigrationAsync(connection, migration, transaction);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            else
            {
                await migration.UpAsync(connection);
                await RecordMigrationAsync(connection, migration, null);
            }

            sw.Stop();
            info.AppliedAt = DateTime.UtcNow;
            info.ExecutionTimeMs = sw.ElapsedMilliseconds;
            info.IsApplied = true;

            _logger.LogInformation("Migration {Name} applied successfully in {Ms}ms",
                migration.Name, info.ExecutionTimeMs);
        }
        catch (Exception ex)
        {
            sw.Stop();
            info.ErrorMessage = ex.Message;
            info.ExecutionTimeMs = sw.ElapsedMilliseconds;

            _logger.LogError(ex, "Migration {Name} failed after {Ms}ms: {Error}",
                migration.Name, sw.ElapsedMilliseconds, ex.Message);
        }

        return info;
    }

    /// <summary>
    /// Rolls back the last applied migration.
    /// </summary>
    /// <param name="connection">Database connection.</param>
    /// <param name="useTransaction">Whether to use a transaction.</param>
    /// <returns>Migration information.</returns>
    public async Task<MigrationInfo> RollbackLastMigrationAsync(
        IDbConnection connection,
        bool useTransaction = true)
    {
        var appliedMigrations = await GetAppliedMigrationsAsync(connection);

        if (!appliedMigrations.Any())
        {
            _logger.LogWarning("No migrations to rollback.");
            return new MigrationInfo { ErrorMessage = "No migrations to rollback" };
        }

        var lastMigration = appliedMigrations.OrderByDescending(m => m.Version).First();
        var migration = _migrations.FirstOrDefault(m => m.Version == lastMigration.Version);

        if (migration == null)
        {
            throw new InvalidOperationException(
                $"Migration {lastMigration.Name} (v{lastMigration.Version}) is not registered.");
        }

        return await RollbackMigrationAsync(connection, migration, useTransaction);
    }

    /// <summary>
    /// Rolls back a specific migration.
    /// </summary>
    /// <param name="connection">Database connection.</param>
    /// <param name="migration">Migration to rollback.</param>
    /// <param name="useTransaction">Whether to use a transaction.</param>
    /// <returns>Migration information.</returns>
    private async Task<MigrationInfo> RollbackMigrationAsync(
        IDbConnection connection,
        IMigration migration,
        bool useTransaction)
    {
        var info = new MigrationInfo
        {
            Name = migration.Name,
            Version = migration.Version,
            Description = migration.Description,
            CreatedAt = migration.CreatedAt
        };

        var sw = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Rolling back migration: {Name} (v{Version})", migration.Name, migration.Version);

            // Set logger if migration is a Migration base class
            if (migration is Migration migrationBase)
            {
                migrationBase.SetLogger(_logger);
            }

            if (useTransaction)
            {
                using var transaction = connection.BeginTransaction();
                try
                {
                    await migration.DownAsync(connection);
                    await RemoveMigrationRecordAsync(connection, migration, transaction);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            else
            {
                await migration.DownAsync(connection);
                await RemoveMigrationRecordAsync(connection, migration, null);
            }

            sw.Stop();
            info.AppliedAt = DateTime.UtcNow;
            info.ExecutionTimeMs = sw.ElapsedMilliseconds;
            info.IsApplied = false;

            _logger.LogInformation("Migration {Name} rolled back successfully in {Ms}ms",
                migration.Name, info.ExecutionTimeMs);
        }
        catch (Exception ex)
        {
            sw.Stop();
            info.ErrorMessage = ex.Message;
            info.ExecutionTimeMs = sw.ElapsedMilliseconds;

            _logger.LogError(ex, "Migration rollback {Name} failed after {Ms}ms: {Error}",
                migration.Name, sw.ElapsedMilliseconds, ex.Message);
        }

        return info;
    }

    /// <summary>
    /// Gets all pending migrations.
    /// </summary>
    /// <param name="connection">Database connection.</param>
    /// <returns>List of pending migrations.</returns>
    public async Task<List<IMigration>> GetPendingMigrationsAsync(IDbConnection connection)
    {
        var appliedMigrations = await GetAppliedMigrationsAsync(connection);
        var appliedVersions = new HashSet<long>(appliedMigrations.Select(m => m.Version));

        return _migrations
            .Where(m => !appliedVersions.Contains(m.Version))
            .OrderBy(m => m.Version)
            .ToList();
    }

    /// <summary>
    /// Gets all applied migrations.
    /// </summary>
    /// <param name="connection">Database connection.</param>
    /// <returns>List of applied migration information.</returns>
    public async Task<List<MigrationInfo>> GetAppliedMigrationsAsync(IDbConnection connection)
    {
        await EnsureMigrationHistoryTableAsync(connection);

        var sql = $@"
            SELECT 
                Version,
                Name,
                Description,
                AppliedAt
            FROM {MigrationTableName}
            ORDER BY Version";

        var records = await connection.QueryAsync<MigrationHistoryRecord>(sql);

        return records.Select(r => new MigrationInfo
        {
            Version = r.Version,
            Name = r.Name,
            Description = r.Description,
            AppliedAt = r.AppliedAt,
            IsApplied = true
        }).ToList();
    }

    /// <summary>
    /// Gets the current database version (latest applied migration).
    /// </summary>
    /// <param name="connection">Database connection.</param>
    /// <returns>Current version, or 0 if no migrations applied.</returns>
    public async Task<long> GetCurrentVersionAsync(IDbConnection connection)
    {
        var appliedMigrations = await GetAppliedMigrationsAsync(connection);
        return appliedMigrations.Any() ? appliedMigrations.Max(m => m.Version) : 0;
    }

    /// <summary>
    /// Ensures the migration history table exists.
    /// </summary>
    private async Task EnsureMigrationHistoryTableAsync(IDbConnection connection)
    {
        // Try to detect database type based on connection type
        var connectionTypeName = connection.GetType().FullName ?? string.Empty;
        var isSqlite = connectionTypeName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase);

        string sql;
        if (isSqlite)
        {
            // SQLite syntax
            sql = $@"
                CREATE TABLE IF NOT EXISTS {MigrationTableName} (
                    Version INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    AppliedAt TEXT NOT NULL
                )";
        }
        else
        {
            // SQL Server syntax (default)
            sql = $@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{MigrationTableName}')
                BEGIN
                    CREATE TABLE {MigrationTableName} (
                        Version BIGINT PRIMARY KEY,
                        Name NVARCHAR(255) NOT NULL,
                        Description NVARCHAR(MAX),
                        AppliedAt DATETIME2 NOT NULL
                    )
                END";
        }

        await connection.ExecuteAsync(sql);
    }

    /// <summary>
    /// Records a migration in the history table.
    /// </summary>
    private async Task RecordMigrationAsync(
        IDbConnection connection,
        IMigration migration,
        IDbTransaction? transaction)
    {
        var sql = $@"
            INSERT INTO {MigrationTableName} (Version, Name, Description, AppliedAt)
            VALUES (@Version, @Name, @Description, @AppliedAt)";

        await connection.ExecuteAsync(sql, new
        {
            migration.Version,
            migration.Name,
            migration.Description,
            AppliedAt = DateTime.UtcNow
        }, transaction);
    }

    /// <summary>
    /// Removes a migration record from the history table.
    /// </summary>
    private async Task RemoveMigrationRecordAsync(
        IDbConnection connection,
        IMigration migration,
        IDbTransaction? transaction)
    {
        var sql = $"DELETE FROM {MigrationTableName} WHERE Version = @Version";
        await connection.ExecuteAsync(sql, new { migration.Version }, transaction);
    }

    /// <summary>
    /// Internal class for mapping migration history records.
    /// </summary>
    private class MigrationHistoryRecord
    {
        public long Version { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
    }
}
