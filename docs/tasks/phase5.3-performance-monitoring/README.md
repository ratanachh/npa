# Phase 5.3: Performance Monitoring

## 📋 Task Overview

**Objective**: Implement comprehensive performance monitoring and metrics collection for the NPA library to help developers identify bottlenecks and optimize their applications.

**Priority**: Medium  
**Estimated Time**: 4-5 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.6, Phase 3.1-3.5, Phase 4.1-4.7, Phase 5.1-5.2 (All previous phases)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] IPerformanceMonitor interface is complete
- [ ] Performance metrics collection works
- [ ] Performance reporting is implemented
- [ ] Performance alerts work
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

## 📝 Detailed Requirements

### 1. IPerformanceMonitor Interface
- **Purpose**: Defines the contract for performance monitoring
- **Methods**:
  - `Task StartOperationAsync(string operationName)` - Start monitoring an operation
  - `Task EndOperationAsync(string operationId)` - End monitoring an operation
  - `Task RecordMetricAsync(string metricName, double value)` - Record a metric
  - `Task RecordMetricAsync(string metricName, double value, Dictionary<string, string> tags)` - Record a metric with tags
  - `Task<PerformanceMetrics> GetMetricsAsync()` - Get performance metrics
  - `Task<PerformanceReport> GenerateReportAsync()` - Generate performance report

### 2. Performance Metrics Collection
- **Operation Timing**: Track operation execution times
- **Query Performance**: Track SQL query performance
- **Connection Usage**: Track database connection usage
- **Memory Usage**: Track memory consumption
- **Error Rates**: Track error rates and types

### 3. Performance Reporting
- **Real-time Metrics**: Display real-time performance metrics
- **Historical Reports**: Generate historical performance reports
- **Performance Trends**: Analyze performance trends
- **Performance Alerts**: Send performance alerts

### 4. Performance Optimization
- **Slow Query Detection**: Detect slow queries
- **Performance Recommendations**: Provide performance recommendations
- **Automatic Optimization**: Automatically optimize performance
- **Performance Profiling**: Profile application performance

### 5. Integration Points
- **EntityManager Integration**: Monitor EntityManager operations
- **Repository Integration**: Monitor repository operations
- **Query Integration**: Monitor query execution
- **Connection Integration**: Monitor connection usage

## 🏗️ Implementation Plan

### Step 1: Create Performance Monitoring Interfaces
1. Create `IPerformanceMonitor` interface
2. Create `IPerformanceMetrics` interface
3. Create `IPerformanceReporter` interface
4. Create `IPerformanceAlert` interface

### Step 2: Implement Performance Metrics Collection
1. Create `PerformanceMonitor` class
2. Create `PerformanceMetrics` class
3. Implement operation timing
4. Implement query performance tracking

### Step 3: Implement Performance Reporting
1. Create `PerformanceReporter` class
2. Implement real-time metrics
3. Implement historical reports
4. Implement performance trends

### Step 4: Add Performance Optimization
1. Implement slow query detection
2. Implement performance recommendations
3. Implement automatic optimization
4. Implement performance profiling

### Step 5: Add Integration Points
1. Integrate with EntityManager
2. Integrate with repositories
3. Integrate with queries
4. Integrate with connections

### Step 6: Create Unit Tests
1. Test performance monitoring
2. Test metrics collection
3. Test performance reporting
4. Test performance optimization

### Step 7: Add Documentation
1. XML documentation comments
2. Usage examples
3. Performance monitoring guide
4. Best practices

## 📁 File Structure

```
src/NPA.Core/Performance/
├── IPerformanceMonitor.cs
├── PerformanceMonitor.cs
├── IPerformanceMetrics.cs
├── PerformanceMetrics.cs
├── IPerformanceReporter.cs
├── PerformanceReporter.cs
├── IPerformanceAlert.cs
├── PerformanceAlert.cs
├── PerformanceOptions.cs
├── PerformanceReport.cs
├── OperationMetrics.cs
├── QueryMetrics.cs
├── ConnectionMetrics.cs
└── MemoryMetrics.cs

tests/NPA.Core.Tests/Performance/
├── PerformanceMonitorTests.cs
├── PerformanceMetricsTests.cs
├── PerformanceReporterTests.cs
├── PerformanceAlertTests.cs
└── PerformanceIntegrationTests.cs
```

## 💻 Code Examples

### IPerformanceMonitor Interface
```csharp
public interface IPerformanceMonitor : IDisposable
{
    Task<string> StartOperationAsync(string operationName);
    Task<string> StartOperationAsync(string operationName, Dictionary<string, string> tags);
    Task EndOperationAsync(string operationId);
    Task EndOperationAsync(string operationId, bool success);
    Task RecordMetricAsync(string metricName, double value);
    Task RecordMetricAsync(string metricName, double value, Dictionary<string, string> tags);
    Task RecordCounterAsync(string counterName, long increment = 1);
    Task RecordCounterAsync(string counterName, long increment, Dictionary<string, string> tags);
    Task RecordGaugeAsync(string gaugeName, double value);
    Task RecordGaugeAsync(string gaugeName, double value, Dictionary<string, string> tags);
    Task<PerformanceMetrics> GetMetricsAsync();
    Task<PerformanceReport> GenerateReportAsync();
    Task ClearMetricsAsync();
    bool IsEnabled { get; }
    void SetEnabled(bool enabled);
}

public interface IPerformanceMetrics
{
    Dictionary<string, OperationMetrics> Operations { get; }
    Dictionary<string, QueryMetrics> Queries { get; }
    Dictionary<string, ConnectionMetrics> Connections { get; }
    MemoryMetrics Memory { get; }
    DateTime StartTime { get; }
    DateTime EndTime { get; }
    TimeSpan Duration { get; }
    int TotalOperations { get; }
    int SuccessfulOperations { get; }
    int FailedOperations { get; }
    double SuccessRate { get; }
    double AverageOperationTime { get; }
    double AverageQueryTime { get; }
    double AverageConnectionTime { get; }
}

public class OperationMetrics
{
    public string Name { get; set; }
    public int Count { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan TotalTime { get; set; }
    public TimeSpan AverageTime { get; set; }
    public TimeSpan MinTime { get; set; }
    public TimeSpan MaxTime { get; set; }
    public TimeSpan P50Time { get; set; }
    public TimeSpan P95Time { get; set; }
    public TimeSpan P99Time { get; set; }
    public DateTime FirstExecution { get; set; }
    public DateTime LastExecution { get; set; }
    public List<Exception> Exceptions { get; set; } = new();
    public Dictionary<string, string> Tags { get; set; } = new();
}

public class QueryMetrics
{
    public string Sql { get; set; }
    public int ExecutionCount { get; set; }
    public TimeSpan TotalTime { get; set; }
    public TimeSpan AverageTime { get; set; }
    public TimeSpan MinTime { get; set; }
    public TimeSpan MaxTime { get; set; }
    public TimeSpan P50Time { get; set; }
    public TimeSpan P95Time { get; set; }
    public TimeSpan P99Time { get; set; }
    public int RowsAffected { get; set; }
    public int RowsReturned { get; set; }
    public DateTime FirstExecution { get; set; }
    public DateTime LastExecution { get; set; }
    public List<Exception> Exceptions { get; set; } = new();
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public class ConnectionMetrics
{
    public string ConnectionString { get; set; }
    public int TotalConnections { get; set; }
    public int ActiveConnections { get; set; }
    public int IdleConnections { get; set; }
    public int FailedConnections { get; set; }
    public TimeSpan TotalConnectionTime { get; set; }
    public TimeSpan AverageConnectionTime { get; set; }
    public TimeSpan MinConnectionTime { get; set; }
    public TimeSpan MaxConnectionTime { get; set; }
    public DateTime FirstConnection { get; set; }
    public DateTime LastConnection { get; set; }
    public List<Exception> Exceptions { get; set; } = new();
}

public class MemoryMetrics
{
    public long TotalMemory { get; set; }
    public long UsedMemory { get; set; }
    public long AvailableMemory { get; set; }
    public long GarbageCollections { get; set; }
    public TimeSpan GarbageCollectionTime { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### PerformanceMonitor Class
```csharp
public class PerformanceMonitor : IPerformanceMonitor
{
    private readonly PerformanceOptions _options;
    private readonly ConcurrentDictionary<string, OperationMetrics> _operations = new();
    private readonly ConcurrentDictionary<string, QueryMetrics> _queries = new();
    private readonly ConcurrentDictionary<string, ConnectionMetrics> _connections = new();
    private readonly ConcurrentDictionary<string, OperationContext> _activeOperations = new();
    private readonly Timer _metricsTimer;
    private readonly Timer _memoryTimer;
    private bool _disposed;
    private bool _enabled = true;
    
    public bool IsEnabled => _enabled;
    
    public PerformanceMonitor(PerformanceOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        
        if (options.EnableMetrics)
        {
            _metricsTimer = new Timer(CollectMetrics, null, TimeSpan.Zero, options.MetricsInterval);
        }
        
        if (options.EnableMemoryMonitoring)
        {
            _memoryTimer = new Timer(CollectMemoryMetrics, null, TimeSpan.Zero, options.MemoryInterval);
        }
    }
    
    public async Task<string> StartOperationAsync(string operationName)
    {
        return await StartOperationAsync(operationName, new Dictionary<string, string>());
    }
    
    public async Task<string> StartOperationAsync(string operationName, Dictionary<string, string> tags)
    {
        if (!_enabled) return string.Empty;
        if (string.IsNullOrEmpty(operationName)) throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));
        
        var operationId = Guid.NewGuid().ToString();
        var context = new OperationContext
        {
            Id = operationId,
            Name = operationName,
            StartTime = DateTime.UtcNow,
            Tags = tags ?? new Dictionary<string, string>()
        };
        
        _activeOperations[operationId] = context;
        
        return await Task.FromResult(operationId);
    }
    
    public async Task EndOperationAsync(string operationId)
    {
        await EndOperationAsync(operationId, true);
    }
    
    public async Task EndOperationAsync(string operationId, bool success)
    {
        if (!_enabled) return;
        if (string.IsNullOrEmpty(operationId)) throw new ArgumentException("Operation ID cannot be null or empty", nameof(operationId));
        
        if (_activeOperations.TryRemove(operationId, out var context))
        {
            var duration = DateTime.UtcNow - context.StartTime;
            
            var operationMetrics = _operations.GetOrAdd(context.Name, _ => new OperationMetrics
            {
                Name = context.Name,
                Tags = context.Tags,
                FirstExecution = context.StartTime
            });
            
            lock (operationMetrics)
            {
                operationMetrics.Count++;
                if (success)
                {
                    operationMetrics.SuccessCount++;
                }
                else
                {
                    operationMetrics.FailureCount++;
                }
                
                operationMetrics.TotalTime += duration;
                operationMetrics.AverageTime = TimeSpan.FromTicks(operationMetrics.TotalTime.Ticks / operationMetrics.Count);
                
                if (operationMetrics.Count == 1)
                {
                    operationMetrics.MinTime = duration;
                    operationMetrics.MaxTime = duration;
                }
                else
                {
                    if (duration < operationMetrics.MinTime)
                        operationMetrics.MinTime = duration;
                    if (duration > operationMetrics.MaxTime)
                        operationMetrics.MaxTime = duration;
                }
                
                operationMetrics.LastExecution = DateTime.UtcNow;
                operationMetrics.SuccessRate = (double)operationMetrics.SuccessCount / operationMetrics.Count;
            }
        }
        
        await Task.CompletedTask;
    }
    
    public async Task RecordMetricAsync(string metricName, double value)
    {
        await RecordMetricAsync(metricName, value, new Dictionary<string, string>());
    }
    
    public async Task RecordMetricAsync(string metricName, double value, Dictionary<string, string> tags)
    {
        if (!_enabled) return;
        if (string.IsNullOrEmpty(metricName)) throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));
        
        // Implementation for recording metrics
        await Task.CompletedTask;
    }
    
    public async Task RecordCounterAsync(string counterName, long increment = 1)
    {
        await RecordCounterAsync(counterName, increment, new Dictionary<string, string>());
    }
    
    public async Task RecordCounterAsync(string counterName, long increment, Dictionary<string, string> tags)
    {
        if (!_enabled) return;
        if (string.IsNullOrEmpty(counterName)) throw new ArgumentException("Counter name cannot be null or empty", nameof(counterName));
        
        // Implementation for recording counters
        await Task.CompletedTask;
    }
    
    public async Task RecordGaugeAsync(string gaugeName, double value)
    {
        await RecordGaugeAsync(gaugeName, value, new Dictionary<string, string>());
    }
    
    public async Task RecordGaugeAsync(string gaugeName, double value, Dictionary<string, string> tags)
    {
        if (!_enabled) return;
        if (string.IsNullOrEmpty(gaugeName)) throw new ArgumentException("Gauge name cannot be null or empty", nameof(gaugeName));
        
        // Implementation for recording gauges
        await Task.CompletedTask;
    }
    
    public async Task<PerformanceMetrics> GetMetricsAsync()
    {
        if (!_enabled) return new PerformanceMetrics();
        
        var metrics = new PerformanceMetrics
        {
            Operations = new Dictionary<string, OperationMetrics>(_operations),
            Queries = new Dictionary<string, QueryMetrics>(_queries),
            Connections = new Dictionary<string, ConnectionMetrics>(_connections),
            Memory = GetMemoryMetrics(),
            StartTime = DateTime.UtcNow.AddHours(-1), // Last hour
            EndTime = DateTime.UtcNow
        };
        
        // Calculate derived metrics
        metrics.TotalOperations = metrics.Operations.Values.Sum(o => o.Count);
        metrics.SuccessfulOperations = metrics.Operations.Values.Sum(o => o.SuccessCount);
        metrics.FailedOperations = metrics.Operations.Values.Sum(o => o.FailureCount);
        metrics.SuccessRate = metrics.TotalOperations > 0 ? (double)metrics.SuccessfulOperations / metrics.TotalOperations : 0;
        metrics.AverageOperationTime = metrics.Operations.Values.Any() ? 
            TimeSpan.FromTicks((long)metrics.Operations.Values.Average(o => o.AverageTime.Ticks)) : TimeSpan.Zero;
        metrics.AverageQueryTime = metrics.Queries.Values.Any() ? 
            TimeSpan.FromTicks((long)metrics.Queries.Values.Average(q => q.AverageTime.Ticks)) : TimeSpan.Zero;
        metrics.AverageConnectionTime = metrics.Connections.Values.Any() ? 
            TimeSpan.FromTicks((long)metrics.Connections.Values.Average(c => c.AverageConnectionTime.Ticks)) : TimeSpan.Zero;
        
        return await Task.FromResult(metrics);
    }
    
    public async Task<PerformanceReport> GenerateReportAsync()
    {
        if (!_enabled) return new PerformanceReport();
        
        var metrics = await GetMetricsAsync();
        var report = new PerformanceReport
        {
            GeneratedAt = DateTime.UtcNow,
            Metrics = metrics,
            Summary = GenerateSummary(metrics),
            Recommendations = GenerateRecommendations(metrics),
            Alerts = GenerateAlerts(metrics)
        };
        
        return await Task.FromResult(report);
    }
    
    public async Task ClearMetricsAsync()
    {
        _operations.Clear();
        _queries.Clear();
        _connections.Clear();
        _activeOperations.Clear();
        
        await Task.CompletedTask;
    }
    
    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
    }
    
    private void CollectMetrics(object state)
    {
        if (!_enabled) return;
        
        // Collect and process metrics
        ProcessMetrics();
    }
    
    private void CollectMemoryMetrics(object state)
    {
        if (!_enabled) return;
        
        // Collect memory metrics
        var memoryMetrics = GetMemoryMetrics();
        // Store or process memory metrics
    }
    
    private MemoryMetrics GetMemoryMetrics()
    {
        var process = Process.GetCurrentProcess();
        var workingSet = process.WorkingSet64;
        var privateMemory = process.PrivateMemorySize64;
        var gcMemory = GC.GetTotalMemory(false);
        
        return new MemoryMetrics
        {
            TotalMemory = workingSet,
            UsedMemory = privateMemory,
            AvailableMemory = workingSet - privateMemory,
            GarbageCollections = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2),
            GarbageCollectionTime = TimeSpan.Zero, // Would need to track this separately
            Timestamp = DateTime.UtcNow
        };
    }
    
    private void ProcessMetrics()
    {
        // Process and aggregate metrics
        foreach (var operation in _operations.Values)
        {
            // Calculate percentiles
            CalculatePercentiles(operation);
        }
        
        foreach (var query in _queries.Values)
        {
            // Calculate percentiles
            CalculatePercentiles(query);
        }
    }
    
    private void CalculatePercentiles(OperationMetrics operation)
    {
        // Implementation for calculating percentiles
        // This would require storing individual execution times
    }
    
    private void CalculatePercentiles(QueryMetrics query)
    {
        // Implementation for calculating percentiles
        // This would require storing individual execution times
    }
    
    private string GenerateSummary(PerformanceMetrics metrics)
    {
        var summary = new StringBuilder();
        
        summary.AppendLine($"Performance Summary ({metrics.StartTime:yyyy-MM-dd HH:mm:ss} - {metrics.EndTime:yyyy-MM-dd HH:mm:ss})");
        summary.AppendLine($"Total Operations: {metrics.TotalOperations}");
        summary.AppendLine($"Successful Operations: {metrics.SuccessfulOperations}");
        summary.AppendLine($"Failed Operations: {metrics.FailedOperations}");
        summary.AppendLine($"Success Rate: {metrics.SuccessRate:P2}");
        summary.AppendLine($"Average Operation Time: {metrics.AverageOperationTime.TotalMilliseconds:F2}ms");
        summary.AppendLine($"Average Query Time: {metrics.AverageQueryTime.TotalMilliseconds:F2}ms");
        summary.AppendLine($"Average Connection Time: {metrics.AverageConnectionTime.TotalMilliseconds:F2}ms");
        
        return summary.ToString();
    }
    
    private List<string> GenerateRecommendations(PerformanceMetrics metrics)
    {
        var recommendations = new List<string>();
        
        // Check for slow operations
        var slowOperations = metrics.Operations.Values.Where(o => o.AverageTime.TotalMilliseconds > 1000).ToList();
        if (slowOperations.Any())
        {
            recommendations.Add($"Consider optimizing slow operations: {string.Join(", ", slowOperations.Select(o => o.Name))}");
        }
        
        // Check for slow queries
        var slowQueries = metrics.Queries.Values.Where(q => q.AverageTime.TotalMilliseconds > 500).ToList();
        if (slowQueries.Any())
        {
            recommendations.Add($"Consider optimizing slow queries: {string.Join(", ", slowQueries.Select(q => q.Sql))}");
        }
        
        // Check for high failure rate
        var highFailureOperations = metrics.Operations.Values.Where(o => o.SuccessRate < 0.95).ToList();
        if (highFailureOperations.Any())
        {
            recommendations.Add($"Consider investigating high failure rate operations: {string.Join(", ", highFailureOperations.Select(o => o.Name))}");
        }
        
        return recommendations;
    }
    
    private List<string> GenerateAlerts(PerformanceMetrics metrics)
    {
        var alerts = new List<string>();
        
        // Check for critical performance issues
        if (metrics.AverageOperationTime.TotalMilliseconds > 5000)
        {
            alerts.Add("CRITICAL: Average operation time exceeds 5 seconds");
        }
        
        if (metrics.SuccessRate < 0.9)
        {
            alerts.Add("CRITICAL: Success rate below 90%");
        }
        
        if (metrics.AverageQueryTime.TotalMilliseconds > 2000)
        {
            alerts.Add("WARNING: Average query time exceeds 2 seconds");
        }
        
        return alerts;
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        _metricsTimer?.Dispose();
        _memoryTimer?.Dispose();
    }
}

public class OperationContext
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime StartTime { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}

public class PerformanceOptions
{
    public bool EnableMetrics { get; set; } = true;
    public bool EnableMemoryMonitoring { get; set; } = true;
    public TimeSpan MetricsInterval { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan MemoryInterval { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxOperations { get; set; } = 10000;
    public int MaxQueries { get; set; } = 10000;
    public int MaxConnections { get; set; } = 1000;
    public bool EnableAlerts { get; set; } = true;
    public double SlowOperationThreshold { get; set; } = 1000; // milliseconds
    public double SlowQueryThreshold { get; set; } = 500; // milliseconds
    public double LowSuccessRateThreshold { get; set; } = 0.95; // 95%
}
```

### PerformanceReporter Class
```csharp
public class PerformanceReporter : IPerformanceReporter
{
    private readonly IPerformanceMonitor _monitor;
    private readonly PerformanceOptions _options;
    
    public PerformanceReporter(IPerformanceMonitor monitor, PerformanceOptions options)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }
    
    public async Task<string> GenerateTextReportAsync()
    {
        var report = await _monitor.GenerateReportAsync();
        return GenerateTextReport(report);
    }
    
    public async Task<string> GenerateHtmlReportAsync()
    {
        var report = await _monitor.GenerateReportAsync();
        return GenerateHtmlReport(report);
    }
    
    public async Task<string> GenerateJsonReportAsync()
    {
        var report = await _monitor.GenerateReportAsync();
        return JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
    }
    
    public async Task SaveReportAsync(string filePath, ReportFormat format)
    {
        var report = await _monitor.GenerateReportAsync();
        var content = format switch
        {
            ReportFormat.Text => GenerateTextReport(report),
            ReportFormat.Html => GenerateHtmlReport(report),
            ReportFormat.Json => JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }),
            _ => throw new NotSupportedException($"Report format {format} is not supported")
        };
        
        await File.WriteAllTextAsync(filePath, content);
    }
    
    private string GenerateTextReport(PerformanceReport report)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("=".PadRight(80, '='));
        sb.AppendLine("NPA Performance Report");
        sb.AppendLine("=".PadRight(80, '='));
        sb.AppendLine();
        
        sb.AppendLine($"Generated At: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Report Period: {report.Metrics.StartTime:yyyy-MM-dd HH:mm:ss} - {report.Metrics.EndTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        
        sb.AppendLine("SUMMARY");
        sb.AppendLine("-".PadRight(40, '-'));
        sb.AppendLine(report.Summary);
        sb.AppendLine();
        
        if (report.Recommendations.Any())
        {
            sb.AppendLine("RECOMMENDATIONS");
            sb.AppendLine("-".PadRight(40, '-'));
            foreach (var recommendation in report.Recommendations)
            {
                sb.AppendLine($"• {recommendation}");
            }
            sb.AppendLine();
        }
        
        if (report.Alerts.Any())
        {
            sb.AppendLine("ALERTS");
            sb.AppendLine("-".PadRight(40, '-'));
            foreach (var alert in report.Alerts)
            {
                sb.AppendLine($"⚠ {alert}");
            }
            sb.AppendLine();
        }
        
        sb.AppendLine("OPERATION METRICS");
        sb.AppendLine("-".PadRight(40, '-'));
        foreach (var operation in report.Metrics.Operations.Values.OrderByDescending(o => o.TotalTime))
        {
            sb.AppendLine($"Operation: {operation.Name}");
            sb.AppendLine($"  Count: {operation.Count}");
            sb.AppendLine($"  Success Rate: {operation.SuccessRate:P2}");
            sb.AppendLine($"  Average Time: {operation.AverageTime.TotalMilliseconds:F2}ms");
            sb.AppendLine($"  Min Time: {operation.MinTime.TotalMilliseconds:F2}ms");
            sb.AppendLine($"  Max Time: {operation.MaxTime.TotalMilliseconds:F2}ms");
            sb.AppendLine($"  Total Time: {operation.TotalTime.TotalMilliseconds:F2}ms");
            sb.AppendLine();
        }
        
        sb.AppendLine("QUERY METRICS");
        sb.AppendLine("-".PadRight(40, '-'));
        foreach (var query in report.Metrics.Queries.Values.OrderByDescending(q => q.TotalTime))
        {
            sb.AppendLine($"Query: {query.Sql}");
            sb.AppendLine($"  Execution Count: {query.ExecutionCount}");
            sb.AppendLine($"  Average Time: {query.AverageTime.TotalMilliseconds:F2}ms");
            sb.AppendLine($"  Min Time: {query.MinTime.TotalMilliseconds:F2}ms");
            sb.AppendLine($"  Max Time: {query.MaxTime.TotalMilliseconds:F2}ms");
            sb.AppendLine($"  Total Time: {query.TotalTime.TotalMilliseconds:F2}ms");
            sb.AppendLine($"  Rows Affected: {query.RowsAffected}");
            sb.AppendLine($"  Rows Returned: {query.RowsReturned}");
            sb.AppendLine();
        }
        
        sb.AppendLine("CONNECTION METRICS");
        sb.AppendLine("-".PadRight(40, '-'));
        foreach (var connection in report.Metrics.Connections.Values)
        {
            sb.AppendLine($"Connection: {connection.ConnectionString}");
            sb.AppendLine($"  Total Connections: {connection.TotalConnections}");
            sb.AppendLine($"  Active Connections: {connection.ActiveConnections}");
            sb.AppendLine($"  Idle Connections: {connection.IdleConnections}");
            sb.AppendLine($"  Failed Connections: {connection.FailedConnections}");
            sb.AppendLine($"  Average Connection Time: {connection.AverageConnectionTime.TotalMilliseconds:F2}ms");
            sb.AppendLine();
        }
        
        sb.AppendLine("MEMORY METRICS");
        sb.AppendLine("-".PadRight(40, '-'));
        sb.AppendLine($"Total Memory: {report.Metrics.Memory.TotalMemory / 1024 / 1024:F2} MB");
        sb.AppendLine($"Used Memory: {report.Metrics.Memory.UsedMemory / 1024 / 1024:F2} MB");
        sb.AppendLine($"Available Memory: {report.Metrics.Memory.AvailableMemory / 1024 / 1024:F2} MB");
        sb.AppendLine($"Garbage Collections: {report.Metrics.Memory.GarbageCollections}");
        sb.AppendLine($"Garbage Collection Time: {report.Metrics.Memory.GarbageCollectionTime.TotalMilliseconds:F2}ms");
        sb.AppendLine();
        
        return sb.ToString();
    }
    
    private string GenerateHtmlReport(PerformanceReport report)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<title>NPA Performance Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        sb.AppendLine("h1, h2, h3 { color: #333; }");
        sb.AppendLine("table { border-collapse: collapse; width: 100%; margin-bottom: 20px; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        sb.AppendLine("th { background-color: #f2f2f2; }");
        sb.AppendLine(".alert { background-color: #ffebee; color: #c62828; padding: 10px; margin: 10px 0; }");
        sb.AppendLine(".recommendation { background-color: #e8f5e8; color: #2e7d32; padding: 10px; margin: 10px 0; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        
        sb.AppendLine("<h1>NPA Performance Report</h1>");
        sb.AppendLine($"<p><strong>Generated At:</strong> {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine($"<p><strong>Report Period:</strong> {report.Metrics.StartTime:yyyy-MM-dd HH:mm:ss} - {report.Metrics.EndTime:yyyy-MM-dd HH:mm:ss}</p>");
        
        sb.AppendLine("<h2>Summary</h2>");
        sb.AppendLine($"<pre>{report.Summary}</pre>");
        
        if (report.Alerts.Any())
        {
            sb.AppendLine("<h2>Alerts</h2>");
            foreach (var alert in report.Alerts)
            {
                sb.AppendLine($"<div class=\"alert\">⚠ {alert}</div>");
            }
        }
        
        if (report.Recommendations.Any())
        {
            sb.AppendLine("<h2>Recommendations</h2>");
            foreach (var recommendation in report.Recommendations)
            {
                sb.AppendLine($"<div class=\"recommendation\">• {recommendation}</div>");
            }
        }
        
        sb.AppendLine("<h2>Operation Metrics</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Operation</th><th>Count</th><th>Success Rate</th><th>Average Time</th><th>Min Time</th><th>Max Time</th><th>Total Time</th></tr>");
        foreach (var operation in report.Metrics.Operations.Values.OrderByDescending(o => o.TotalTime))
        {
            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td>{operation.Name}</td>");
            sb.AppendLine($"<td>{operation.Count}</td>");
            sb.AppendLine($"<td>{operation.SuccessRate:P2}</td>");
            sb.AppendLine($"<td>{operation.AverageTime.TotalMilliseconds:F2}ms</td>");
            sb.AppendLine($"<td>{operation.MinTime.TotalMilliseconds:F2}ms</td>");
            sb.AppendLine($"<td>{operation.MaxTime.TotalMilliseconds:F2}ms</td>");
            sb.AppendLine($"<td>{operation.TotalTime.TotalMilliseconds:F2}ms</td>");
            sb.AppendLine($"</tr>");
        }
        sb.AppendLine("</table>");
        
        sb.AppendLine("<h2>Query Metrics</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Query</th><th>Execution Count</th><th>Average Time</th><th>Min Time</th><th>Max Time</th><th>Total Time</th><th>Rows Affected</th><th>Rows Returned</th></tr>");
        foreach (var query in report.Metrics.Queries.Values.OrderByDescending(q => q.TotalTime))
        {
            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td>{query.Sql}</td>");
            sb.AppendLine($"<td>{query.ExecutionCount}</td>");
            sb.AppendLine($"<td>{query.AverageTime.TotalMilliseconds:F2}ms</td>");
            sb.AppendLine($"<td>{query.MinTime.TotalMilliseconds:F2}ms</td>");
            sb.AppendLine($"<td>{query.MaxTime.TotalMilliseconds:F2}ms</td>");
            sb.AppendLine($"<td>{query.TotalTime.TotalMilliseconds:F2}ms</td>");
            sb.AppendLine($"<td>{query.RowsAffected}</td>");
            sb.AppendLine($"<td>{query.RowsReturned}</td>");
            sb.AppendLine($"</tr>");
        }
        sb.AppendLine("</table>");
        
        sb.AppendLine("<h2>Memory Metrics</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Metric</th><th>Value</th></tr>");
        sb.AppendLine($"<tr><td>Total Memory</td><td>{report.Metrics.Memory.TotalMemory / 1024 / 1024:F2} MB</td></tr>");
        sb.AppendLine($"<tr><td>Used Memory</td><td>{report.Metrics.Memory.UsedMemory / 1024 / 1024:F2} MB</td></tr>");
        sb.AppendLine($"<tr><td>Available Memory</td><td>{report.Metrics.Memory.AvailableMemory / 1024 / 1024:F2} MB</td></tr>");
        sb.AppendLine($"<tr><td>Garbage Collections</td><td>{report.Metrics.Memory.GarbageCollections}</td></tr>");
        sb.AppendLine($"<tr><td>Garbage Collection Time</td><td>{report.Metrics.Memory.GarbageCollectionTime.TotalMilliseconds:F2}ms</td></tr>");
        sb.AppendLine("</table>");
        
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        
        return sb.ToString();
    }
}

public enum ReportFormat
{
    Text,
    Html,
    Json
}
```

### Usage Examples
```csharp
// Basic performance monitoring
public class UserService
{
    private readonly IEntityManager _entityManager;
    private readonly IPerformanceMonitor _performanceMonitor;
    
    public UserService(IEntityManager entityManager, IPerformanceMonitor performanceMonitor)
    {
        _entityManager = entityManager;
        _performanceMonitor = performanceMonitor;
    }
    
    public async Task<User> GetUserAsync(long id)
    {
        var operationId = await _performanceMonitor.StartOperationAsync("GetUser");
        
        try
        {
            var user = await _entityManager.FindAsync<User>(id);
            await _performanceMonitor.EndOperationAsync(operationId, user != null);
            return user;
        }
        catch (Exception ex)
        {
            await _performanceMonitor.EndOperationAsync(operationId, false);
            throw;
        }
    }
    
    public async Task<IEnumerable<User>> GetUsersAsync()
    {
        var operationId = await _performanceMonitor.StartOperationAsync("GetUsers");
        
        try
        {
            var users = await _entityManager.FindAllAsync<User>();
            await _performanceMonitor.EndOperationAsync(operationId, true);
            return users;
        }
        catch (Exception ex)
        {
            await _performanceMonitor.EndOperationAsync(operationId, false);
            throw;
        }
    }
}

// Advanced performance monitoring with custom metrics
public class AdvancedUserService
{
    private readonly IEntityManager _entityManager;
    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly IPerformanceReporter _performanceReporter;
    
    public AdvancedUserService(
        IEntityManager entityManager, 
        IPerformanceMonitor performanceMonitor,
        IPerformanceReporter performanceReporter)
    {
        _entityManager = entityManager;
        _performanceMonitor = performanceMonitor;
        _performanceReporter = performanceReporter;
    }
    
    public async Task<User> GetUserWithMetricsAsync(long id)
    {
        var operationId = await _performanceMonitor.StartOperationAsync("GetUser", new Dictionary<string, string>
        {
            ["userId"] = id.ToString(),
            ["operationType"] = "Read"
        });
        
        try
        {
            var user = await _entityManager.FindAsync<User>(id);
            
            // Record custom metrics
            await _performanceMonitor.RecordMetricAsync("user.retrieved", 1);
            await _performanceMonitor.RecordCounterAsync("user.operations", 1);
            
            await _performanceMonitor.EndOperationAsync(operationId, user != null);
            return user;
        }
        catch (Exception ex)
        {
            await _performanceMonitor.RecordCounterAsync("user.errors", 1);
            await _performanceMonitor.EndOperationAsync(operationId, false);
            throw;
        }
    }
    
    public async Task<PerformanceReport> GetPerformanceReportAsync()
    {
        return await _performanceMonitor.GenerateReportAsync();
    }
    
    public async Task<string> GetPerformanceReportAsHtmlAsync()
    {
        return await _performanceReporter.GenerateHtmlReportAsync();
    }
    
    public async Task SavePerformanceReportAsync(string filePath)
    {
        await _performanceReporter.SaveReportAsync(filePath, ReportFormat.Html);
    }
}

// Performance monitoring configuration
public class PerformanceMonitoringService
{
    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly IPerformanceReporter _performanceReporter;
    
    public PerformanceMonitoringService(IPerformanceMonitor performanceMonitor, IPerformanceReporter performanceReporter)
    {
        _performanceMonitor = performanceMonitor;
        _performanceReporter = performanceReporter;
    }
    
    public async Task StartMonitoringAsync()
    {
        // Enable performance monitoring
        _performanceMonitor.SetEnabled(true);
        
        // Start background monitoring
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                
                var report = await _performanceMonitor.GenerateReportAsync();
                var htmlReport = await _performanceReporter.GenerateHtmlReportAsync();
                
                // Save report to file
                var fileName = $"performance-report-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.html";
                await File.WriteAllTextAsync(fileName, htmlReport);
                
                Console.WriteLine($"Performance report saved: {fileName}");
            }
        });
    }
    
    public async Task StopMonitoringAsync()
    {
        _performanceMonitor.SetEnabled(false);
        await _performanceMonitor.ClearMetricsAsync();
    }
}
```

## 🧪 Test Cases

### Performance Monitor Tests
- [ ] Operation timing
- [ ] Metrics collection
- [ ] Performance reporting
- [ ] Error handling

### Performance Metrics Tests
- [ ] Operation metrics
- [ ] Query metrics
- [ ] Connection metrics
- [ ] Memory metrics

### Performance Reporter Tests
- [ ] Text report generation
- [ ] HTML report generation
- [ ] JSON report generation
- [ ] Report saving

### Integration Tests
- [ ] End-to-end performance monitoring
- [ ] Performance report generation
- [ ] Performance alerting
- [ ] Performance optimization

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Basic performance monitoring
- [ ] Advanced performance monitoring
- [ ] Performance reporting
- [ ] Best practices

### Performance Monitoring Guide
- [ ] Performance metrics
- [ ] Performance reporting
- [ ] Performance optimization
- [ ] Common scenarios
- [ ] Troubleshooting

## 🔍 Code Review Checklist

- [ ] Code follows .NET naming conventions
- [ ] All public members have XML documentation
- [ ] Error handling is appropriate
- [ ] Unit tests cover all scenarios
- [ ] Code is readable and maintainable
- [ ] Performance is optimized
- [ ] Memory usage is efficient
- [ ] Thread safety considerations

## 🚀 Next Steps

After completing this task:
1. Move to Phase 5.4: Audit Logging
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on performance monitoring design
- [ ] Performance considerations for monitoring
- [ ] Integration with existing features
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
