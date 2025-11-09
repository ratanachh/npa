namespace NPA.Core.Annotations;

/// <summary>
/// Indicates that a method should be automatically instrumented for performance monitoring.
/// The generator will wrap the method implementation with performance tracking logic.
/// </summary>
/// <example>
/// <code>
/// [PerformanceMonitor]  // Use default settings
/// Task&lt;User?&gt; GetByIdAsync(int id);
/// 
/// [PerformanceMonitor(IncludeParameters = true, WarnThresholdMs = 1000)]
/// Task&lt;IEnumerable&lt;User&gt;&gt; SearchAsync(string query);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class PerformanceMonitorAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether to include parameter values in the performance log.
    /// Default is false (only log method name and duration).
    /// </summary>
    public bool IncludeParameters { get; set; }

    /// <summary>
    /// Gets or sets the warning threshold in milliseconds.
    /// If the method execution exceeds this threshold, a warning will be logged.
    /// Default is 0 (no threshold).
    /// </summary>
    public int WarnThresholdMs { get; set; }

    /// <summary>
    /// Gets or sets the category for grouping metrics.
    /// Default is the repository name.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets whether to track memory allocation.
    /// Default is false.
    /// </summary>
    public bool TrackMemory { get; set; }

    /// <summary>
    /// Gets or sets whether to track database query count.
    /// Default is false.
    /// </summary>
    public bool TrackQueryCount { get; set; }

    /// <summary>
    /// Gets or sets the metric name override.
    /// If not specified, the method name will be used.
    /// </summary>
    public string? MetricName { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceMonitorAttribute"/> class.
    /// </summary>
    public PerformanceMonitorAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceMonitorAttribute"/> class with a warning threshold.
    /// </summary>
    /// <param name="warnThresholdMs">Warning threshold in milliseconds.</param>
    public PerformanceMonitorAttribute(int warnThresholdMs)
    {
        WarnThresholdMs = warnThresholdMs;
    }
}
