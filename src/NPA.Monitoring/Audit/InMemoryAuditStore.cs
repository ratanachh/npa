using Microsoft.Extensions.Logging;

namespace NPA.Monitoring.Audit;

/// <summary>
/// In-memory implementation of <see cref="IAuditStore"/>.
/// Suitable for development and testing. For production, use a persistent store.
/// </summary>
public class InMemoryAuditStore : IAuditStore
{
    private readonly ILogger<InMemoryAuditStore> _logger;
    private readonly List<AuditEntry> _entries = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryAuditStore"/> class.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public InMemoryAuditStore(ILogger<InMemoryAuditStore> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task WriteAsync(AuditEntry entry)
    {
        lock (_lock)
        {
            _entries.Add(entry);
        }

        _logger.LogInformation("Audit entry recorded: {Action} on {EntityType} by {User}",
            entry.Action, entry.EntityType, entry.User ?? "Unknown");

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<AuditEntry>> QueryAsync(AuditFilter filter)
    {
        List<AuditEntry> query;
        
        lock (_lock)
        {
            query = _entries.ToList();
        }

        if (filter.StartDate.HasValue)
            query = query.Where(e => e.Timestamp >= filter.StartDate.Value).ToList();

        if (filter.EndDate.HasValue)
            query = query.Where(e => e.Timestamp <= filter.EndDate.Value).ToList();

        if (!string.IsNullOrEmpty(filter.User))
            query = query.Where(e => e.User == filter.User).ToList();

        if (!string.IsNullOrEmpty(filter.EntityType))
            query = query.Where(e => e.EntityType == filter.EntityType).ToList();

        if (!string.IsNullOrEmpty(filter.Action))
            query = query.Where(e => e.Action == filter.Action).ToList();

        if (!string.IsNullOrEmpty(filter.Category))
            query = query.Where(e => e.Category == filter.Category).ToList();

        if (filter.Severity.HasValue)
            query = query.Where(e => e.Severity == filter.Severity.Value).ToList();

        if (filter.MaxResults.HasValue)
            query = query.Take(filter.MaxResults.Value).ToList();

        return Task.FromResult<IEnumerable<AuditEntry>>(query);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AuditEntry>> GetByEntityAsync(string entityType, string entityId)
    {
        List<AuditEntry> results;
        
        lock (_lock)
        {
            results = _entries
                .Where(e => e.EntityType == entityType && e.EntityId == entityId)
                .OrderByDescending(e => e.Timestamp)
                .ToList();
        }

        return Task.FromResult<IEnumerable<AuditEntry>>(results);
    }

    /// <inheritdoc />
    public Task ClearAsync()
    {
        lock (_lock)
        {
            _entries.Clear();
        }

        _logger.LogWarning("All audit entries cleared");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the total count of audit entries.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _entries.Count;
            }
        }
    }
}
