using Microsoft.Extensions.Logging;

namespace NPA.Monitoring;

/// <summary>
/// In-memory implementation of <see cref="IMetricCollector"/>.
/// Suitable for development and testing. For production, use a persistent collector.
/// </summary>
public class InMemoryMetricCollector : IMetricCollector
{
    private readonly ILogger<InMemoryMetricCollector> _logger;
    private readonly List<PerformanceMetricEntry> _metrics = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryMetricCollector"/> class.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public InMemoryMetricCollector(ILogger<InMemoryMetricCollector> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void RecordMetric(string metricName, TimeSpan duration, string? category = null, Dictionary<string, object>? parameters = null)
    {
        var entry = new PerformanceMetricEntry
        {
            MetricName = metricName,
            Category = category,
            Duration = duration,
            Timestamp = DateTime.UtcNow,
            Parameters = parameters
        };

        lock (_lock)
        {
            _metrics.Add(entry);
        }

        _logger.LogDebug("Metric recorded: {MetricName} ({Category}) took {Duration}ms",
            metricName, category ?? "N/A", duration.TotalMilliseconds);
    }

    /// <inheritdoc />
    public void RecordWarning(string metricName, TimeSpan duration, int thresholdMs, string? category = null)
    {
        _logger.LogWarning("Performance warning: {MetricName} ({Category}) took {Duration}ms, exceeding threshold of {Threshold}ms",
            metricName, category ?? "N/A", duration.TotalMilliseconds, thresholdMs);
    }

    /// <inheritdoc />
    public MetricStatistics GetStatistics(string metricName, string? category = null)
    {
        List<PerformanceMetricEntry> relevantMetrics;
        
        lock (_lock)
        {
            relevantMetrics = _metrics
                .Where(m => m.MetricName == metricName && (category == null || m.Category == category))
                .ToList();
        }

        if (!relevantMetrics.Any())
        {
            return new MetricStatistics
            {
                MetricName = metricName,
                CallCount = 0
            };
        }

        var durations = relevantMetrics.Select(m => m.Duration.TotalMilliseconds).OrderBy(d => d).ToList();
        var p95Index = (int)Math.Ceiling(durations.Count * 0.95) - 1;
        p95Index = Math.Max(0, Math.Min(p95Index, durations.Count - 1));

        return new MetricStatistics
        {
            MetricName = metricName,
            CallCount = relevantMetrics.Count,
            AverageDuration = TimeSpan.FromMilliseconds(durations.Average()),
            MinDuration = TimeSpan.FromMilliseconds(durations.Min()),
            MaxDuration = TimeSpan.FromMilliseconds(durations.Max()),
            TotalDuration = TimeSpan.FromMilliseconds(durations.Sum()),
            P95Duration = TimeSpan.FromMilliseconds(durations[p95Index])
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<PerformanceMetricEntry> GetAllMetrics()
    {
        lock (_lock)
        {
            return _metrics.ToList();
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            _metrics.Clear();
        }
        
        _logger.LogInformation("All metrics cleared");
    }
}
