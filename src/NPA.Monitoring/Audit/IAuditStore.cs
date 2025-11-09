using NPA.Core.Annotations;

namespace NPA.Monitoring.Audit;

/// <summary>
/// Defines the contract for storing audit log entries.
/// </summary>
public interface IAuditStore
{
    /// <summary>
    /// Writes an audit entry to the store.
    /// </summary>
    /// <param name="entry">The audit entry to write</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task WriteAsync(AuditEntry entry);

    /// <summary>
    /// Queries audit entries by various filters.
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Matching audit entries</returns>
    Task<IEnumerable<AuditEntry>> QueryAsync(AuditFilter filter);

    /// <summary>
    /// Gets audit entries for a specific entity.
    /// </summary>
    /// <param name="entityType">Type of entity</param>
    /// <param name="entityId">Entity identifier</param>
    /// <returns>Audit entries for the entity</returns>
    Task<IEnumerable<AuditEntry>> GetByEntityAsync(string entityType, string entityId);

    /// <summary>
    /// Clears all audit entries (use with caution).
    /// </summary>
    Task ClearAsync();
}

/// <summary>
/// Represents a single audit log entry.
/// </summary>
public class AuditEntry
{
    /// <summary>
    /// Gets or sets the unique identifier for this audit entry.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the timestamp when the action occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user who performed the action.
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// Gets or sets the action performed (e.g., "Create", "Update", "Delete").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity type (e.g., "User", "Product").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the category of the audit entry.
    /// </summary>
    public string Category { get; set; } = "Data";

    /// <summary>
    /// Gets or sets the severity level.
    /// </summary>
    public AuditSeverity Severity { get; set; } = AuditSeverity.Normal;

    /// <summary>
    /// Gets or sets the description of the action.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the old value (for update operations).
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// Gets or sets the new value.
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Gets or sets additional parameters.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the caller.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Filter criteria for querying audit entries.
/// </summary>
public class AuditFilter
{
    /// <summary>
    /// Gets or sets the start date for filtering.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for filtering.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the user filter.
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// Gets or sets the entity type filter.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the action filter.
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Gets or sets the category filter.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the severity filter.
    /// </summary>
    public AuditSeverity? Severity { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// </summary>
    public int? MaxResults { get; set; }
}
