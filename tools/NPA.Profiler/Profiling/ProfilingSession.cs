using System.Diagnostics;

namespace NPA.Profiler.Profiling;

/// <summary>
/// Represents a single profiling session with timing and metadata.
/// </summary>
public class ProfilingSession
{
    private readonly Stopwatch _stopwatch;
    private readonly List<QueryProfile> _queries;

    public ProfilingSession()
    {
        _stopwatch = new Stopwatch();
        _queries = new List<QueryProfile>();
        SessionId = Guid.NewGuid();
        StartTime = DateTime.UtcNow;
    }

    public Guid SessionId { get; }
    public DateTime StartTime { get; }
    public DateTime? EndTime { get; private set; }
    public TimeSpan Duration => EndTime.HasValue 
        ? EndTime.Value - StartTime 
        : DateTime.UtcNow - StartTime;

    public IReadOnlyList<QueryProfile> Queries => _queries.AsReadOnly();
    public int TotalQueries => _queries.Count;
    public int CacheHits => _queries.Count(q => q.FromCache);
    public double CacheHitRate => TotalQueries > 0 ? (double)CacheHits / TotalQueries : 0;

    public void Start()
    {
        _stopwatch.Start();
    }

    public void Stop()
    {
        _stopwatch.Stop();
        EndTime = DateTime.UtcNow;
    }

    public void AddQuery(QueryProfile query)
    {
        _queries.Add(query);
    }

    public QueryStatistics GetStatistics()
    {
        return new QueryStatistics
        {
            TotalQueries = TotalQueries,
            TotalDuration = _queries.Sum(q => q.Duration.TotalMilliseconds),
            AverageDuration = _queries.Any() ? _queries.Average(q => q.Duration.TotalMilliseconds) : 0,
            MinDuration = _queries.Any() ? _queries.Min(q => q.Duration.TotalMilliseconds) : 0,
            MaxDuration = _queries.Any() ? _queries.Max(q => q.Duration.TotalMilliseconds) : 0,
            P95Duration = CalculatePercentile(95),
            P99Duration = CalculatePercentile(99),
            CacheHits = CacheHits,
            CacheHitRate = CacheHitRate,
            TotalRowsAffected = _queries.Sum(q => q.RowsAffected),
            SlowQueries = _queries.Where(q => q.Duration.TotalMilliseconds > 100).ToList()
        };
    }

    private double CalculatePercentile(int percentile)
    {
        if (!_queries.Any()) return 0;

        var sorted = _queries.OrderBy(q => q.Duration.TotalMilliseconds).ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))].Duration.TotalMilliseconds;
    }
}

/// <summary>
/// Profile information for a single query execution.
/// </summary>
public class QueryProfile
{
    public Guid QueryId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Sql { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public int RowsAffected { get; set; }
    public bool FromCache { get; set; }
    public string? EntityType { get; set; }
    public QueryType QueryType { get; set; }
    public Dictionary<string, object?> Parameters { get; set; } = new();
    public string? StackTrace { get; set; }
    public string? CallerMember { get; set; }
    public string? CallerFilePath { get; set; }
    public int CallerLineNumber { get; set; }
}

/// <summary>
/// Statistical summary of query performance.
/// </summary>
public class QueryStatistics
{
    public int TotalQueries { get; set; }
    public double TotalDuration { get; set; }
    public double AverageDuration { get; set; }
    public double MinDuration { get; set; }
    public double MaxDuration { get; set; }
    public double P95Duration { get; set; }
    public double P99Duration { get; set; }
    public int CacheHits { get; set; }
    public double CacheHitRate { get; set; }
    public int TotalRowsAffected { get; set; }
    public List<QueryProfile> SlowQueries { get; set; } = new();
}

/// <summary>
/// Types of database queries.
/// </summary>
public enum QueryType
{
    Select,
    Insert,
    Update,
    Delete,
    Other
}
