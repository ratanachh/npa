using System;

namespace NPA.Migrations;

/// <summary>
/// Contains information about a migration.
/// </summary>
public class MigrationInfo
{
    /// <summary>
    /// Gets or sets the migration name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the migration version.
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// Gets or sets the migration description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date the migration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date the migration was applied.
    /// </summary>
    public DateTime? AppliedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the migration has been applied.
    /// </summary>
    public bool IsApplied { get; set; }

    /// <summary>
    /// Gets or sets the execution time in milliseconds.
    /// </summary>
    public long? ExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets any error message if the migration failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets whether the migration operation was successful (regardless of whether it was applied or rolled back).
    /// </summary>
    public bool IsSuccessful => string.IsNullOrEmpty(ErrorMessage);
}
