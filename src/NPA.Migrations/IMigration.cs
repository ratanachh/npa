using System;
using System.Data;
using System.Threading.Tasks;

namespace NPA.Migrations;

/// <summary>
/// Defines the contract for database migrations.
/// </summary>
public interface IMigration
{
    /// <summary>
    /// Gets the unique name of the migration.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version number of the migration.
    /// Version should be in format YYYYMMDDHHMMSS (timestamp-based).
    /// </summary>
    long Version { get; }

    /// <summary>
    /// Gets the creation timestamp of the migration.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the description of what this migration does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Applies the migration (forward migration).
    /// </summary>
    /// <param name="connection">Database connection to use.</param>
    /// <returns>Task representing the async operation.</returns>
    Task UpAsync(IDbConnection connection);

    /// <summary>
    /// Reverts the migration (rollback).
    /// </summary>
    /// <param name="connection">Database connection to use.</param>
    /// <returns>Task representing the async operation.</returns>
    Task DownAsync(IDbConnection connection);
}
