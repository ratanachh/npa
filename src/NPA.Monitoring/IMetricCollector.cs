namespace NPA.Monitoring;

/// <summary>
/// Defines the contract for collecting performance metrics.
/// </summary>
public interface IMetricCollector
{
    /// <summary>
    /// Records a performance metric for a method execution.
    /// </summary>
    /// <param name="metricName">Name of the metric (typically method name)</param>
    /// <param name="duration">Duration of the operation</param>
    /// <param name="category">Category for grouping metrics (e.g., repository name)</param>
    /// <param name="parameters">Optional parameter values</param>
    void RecordMetric(string metricName, TimeSpan duration, string? category = null, Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Records a warning when a metric exceeds a threshold.
    /// </summary>
    /// <param name="metricName">Name of the metric</param>
    /// <param name="duration">Duration that exceeded the threshold</param>
    /// <param name="thresholdMs">Threshold in milliseconds</param>
    /// <param name="category">Category for grouping</param>
    void RecordWarning(string metricName, TimeSpan duration, int thresholdMs, string? category = null);

    /// <summary>
    /// Gets performance statistics for a specific metric.
    /// </summary>
    /// <param name="metricName">Name of the metric</param>
    /// <param name="category">Optional category filter</param>
    /// <returns>Performance statistics</returns>
    MetricStatistics GetStatistics(string metricName, string? category = null);

    /// <summary>
    /// Gets all metrics recorded.
    /// </summary>
    /// <returns>Collection of all metrics</returns>
    IReadOnlyList<PerformanceMetricEntry> GetAllMetrics();

    /// <summary>
    /// Clears all recorded metrics.
    /// </summary>
    void Clear();
}

/// <summary>
/// Represents a single performance metric entry.
/// </summary>
public class PerformanceMetricEntry
{
    /// <summary>
    /// Gets or sets the metric name.
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the parameters.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }
}

/// <summary>
/// Aggregated statistics for a metric.
/// </summary>
public class MetricStatistics
{
    /// <summary>
    /// Gets or sets the metric name.
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of calls.
    /// </summary>
    public int CallCount { get; set; }

    /// <summary>
    /// Gets or sets the average duration.
    /// </summary>
    public TimeSpan AverageDuration { get; set; }

    /// <summary>
    /// Gets or sets the minimum duration.
    /// </summary>
    public TimeSpan MinDuration { get; set; }

    /// <summary>
    /// Gets or sets the maximum duration.
    /// </summary>
    public TimeSpan MaxDuration { get; set; }

    /// <summary>
    /// Gets or sets the total duration.
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// Gets or sets the 95th percentile duration.
    /// </summary>
    public TimeSpan P95Duration { get; set; }
}
