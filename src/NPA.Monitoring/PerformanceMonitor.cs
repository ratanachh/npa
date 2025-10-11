using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace NPA.Monitoring;

/// <summary>
/// Monitors performance metrics for NPA operations.
/// </summary>
public class PerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly List<PerformanceMetric> _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceMonitor"/> class.
    /// </summary>
    /// <param name="logger">Logger for performance monitoring</param>
    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger;
        _metrics = new List<PerformanceMetric>();
    }

    /// <summary>
    /// Records a performance metric for a database operation.
    /// </summary>
    /// <param name="operationType">Type of operation (SELECT, INSERT, etc.)</param>
    /// <param name="duration">Duration of the operation</param>
    /// <param name="recordCount">Number of records affected</param>
    public void RecordMetric(string operationType, TimeSpan duration, int recordCount = 0)
    {
        var metric = new PerformanceMetric
        {
            OperationType = operationType,
            Duration = duration,
            RecordCount = recordCount,
            Timestamp = DateTime.UtcNow
        };

        _metrics.Add(metric);
        _logger.LogDebug("Recorded metric: {OperationType} took {Duration}ms for {RecordCount} records", 
            operationType, duration.TotalMilliseconds, recordCount);
    }

    /// <summary>
    /// Gets performance statistics for a specific operation type.
    /// </summary>
    /// <param name="operationType">Type of operation to analyze</param>
    /// <returns>Performance statistics</returns>
    public PerformanceStats GetStats(string operationType)
    {
        var relevantMetrics = _metrics.Where(m => m.OperationType == operationType).ToList();
        
        if (!relevantMetrics.Any())
            return new PerformanceStats();

        return new PerformanceStats
        {
            AverageDuration = TimeSpan.FromMilliseconds(relevantMetrics.Average(m => m.Duration.TotalMilliseconds)),
            MaxDuration = relevantMetrics.Max(m => m.Duration),
            MinDuration = relevantMetrics.Min(m => m.Duration),
            TotalOperations = relevantMetrics.Count
        };
    }
}

/// <summary>
/// Represents a single performance metric for a database operation.
/// </summary>
public class PerformanceMetric
{
    /// <summary>
    /// Gets or sets the type of database operation (e.g., SELECT, INSERT, UPDATE).
    /// </summary>
    public string OperationType { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the duration of the operation.
    /// </summary>
    public TimeSpan Duration { get; set; }
    
    /// <summary>
    /// Gets or sets the number of records affected by the operation.
    /// </summary>
    public int RecordCount { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the metric was recorded.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Represents aggregated performance statistics for a specific operation type.
/// </summary>
public class PerformanceStats
{
    /// <summary>
    /// Gets or sets the average duration of operations.
    /// </summary>
    public TimeSpan AverageDuration { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum duration of operations.
    /// </summary>
    public TimeSpan MaxDuration { get; set; }
    
    /// <summary>
    /// Gets or sets the minimum duration of operations.
    /// </summary>
    public TimeSpan MinDuration { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of operations recorded.
    /// </summary>
    public int TotalOperations { get; set; }
}