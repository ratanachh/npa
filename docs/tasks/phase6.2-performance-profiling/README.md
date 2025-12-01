# Phase 6.3: Performance Profiling

## 📋 Task Overview

**Objective**: Implement comprehensive performance profiling tools that help developers identify performance bottlenecks, analyze query execution plans, and optimize their NPA applications.

**Priority**: Low  
**Estimated Time**: 4-5 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.6, Phase 3.1-3.5, Phase 4.1-4.6, Phase 5.1-5.5, Phase 6.1 (All previous phases)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] IPerformanceProfiler interface is complete
- [ ] Query execution plan analysis works
- [ ] Performance bottleneck detection works
- [ ] Profiling reports are generated
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

## 📝 Detailed Requirements

### 1. IPerformanceProfiler Interface
- **Purpose**: Defines the contract for performance profiling
- **Methods**:
  - `Task StartProfilingAsync(string sessionName)` - Start profiling session
  - `Task StopProfilingAsync(string sessionId)` - Stop profiling session
  - `Task<ProfilingSession> GetSessionAsync(string sessionId)` - Get profiling session
  - `Task<ProfilingReport> GenerateReportAsync(string sessionId)` - Generate profiling report
  - `Task<QueryExecutionPlan> AnalyzeQueryAsync(string sql)` - Analyze query execution plan
  - `Task<PerformanceBottleneck[]> DetectBottlenecksAsync(string sessionId)` - Detect performance bottlenecks

### 2. Query Execution Plan Analysis
- **Plan Parsing**: Parse database execution plans
- **Plan Analysis**: Analyze execution plan efficiency
- **Index Recommendations**: Recommend index optimizations
- **Query Optimization**: Suggest query optimizations

### 3. Performance Bottleneck Detection
- **Slow Query Detection**: Detect slow queries
- **Resource Usage Analysis**: Analyze resource usage
- **Connection Pool Analysis**: Analyze connection pool usage
- **Memory Usage Analysis**: Analyze memory usage patterns

### 4. Profiling Reports
- **Session Reports**: Generate profiling session reports
- **Query Reports**: Generate query performance reports
- **Bottleneck Reports**: Generate bottleneck analysis reports
- **Optimization Reports**: Generate optimization recommendations

### 5. Integration Points
- **EntityManager Integration**: Profile EntityManager operations
- **Repository Integration**: Profile repository operations
- **Query Integration**: Profile query execution
- **Connection Integration**: Profile connection usage

## 🏗️ Implementation Plan

### Step 1: Create Performance Profiling Interfaces
1. Create `IPerformanceProfiler` interface
2. Create `IQueryAnalyzer` interface
3. Create `IBottleneckDetector` interface
4. Create `IProfilingReporter` interface

### Step 2: Implement Query Execution Plan Analysis
1. Create `QueryAnalyzer` class
2. Implement plan parsing
3. Implement plan analysis
4. Implement index recommendations

### Step 3: Implement Performance Bottleneck Detection
1. Create `BottleneckDetector` class
2. Implement slow query detection
3. Implement resource usage analysis
4. Implement connection pool analysis

### Step 4: Implement Profiling Reports
1. Create `ProfilingReporter` class
2. Implement session reports
3. Implement query reports
4. Implement bottleneck reports

### Step 5: Add Integration Points
1. Integrate with EntityManager
2. Integrate with repositories
3. Integrate with queries
4. Integrate with connections

### Step 6: Create Unit Tests
1. Test performance profiling
2. Test query analysis
3. Test bottleneck detection
4. Test profiling reports

### Step 7: Add Documentation
1. XML documentation comments
2. Usage examples
3. Performance profiling guide
4. Best practices

## 📁 File Structure

```
src/NPA.Core/Profiling/
├── IPerformanceProfiler.cs
├── PerformanceProfiler.cs
├── IQueryAnalyzer.cs
├── QueryAnalyzer.cs
├── IBottleneckDetector.cs
├── BottleneckDetector.cs
├── IProfilingReporter.cs
├── ProfilingReporter.cs
├── ProfilingSession.cs
├── ProfilingReport.cs
├── QueryExecutionPlan.cs
├── PerformanceBottleneck.cs
└── ProfilingOptions.cs

tests/NPA.Core.Tests/Profiling/
├── PerformanceProfilerTests.cs
├── QueryAnalyzerTests.cs
├── BottleneckDetectorTests.cs
├── ProfilingReporterTests.cs
└── ProfilingIntegrationTests.cs
```

## 💻 Code Examples

### IPerformanceProfiler Interface
```csharp
public interface IPerformanceProfiler : IDisposable
{
    Task<string> StartProfilingAsync(string sessionName);
    Task<string> StartProfilingAsync(string sessionName, ProfilingOptions options);
    Task StopProfilingAsync(string sessionId);
    Task<ProfilingSession> GetSessionAsync(string sessionId);
    Task<ProfilingReport> GenerateReportAsync(string sessionId);
    Task<QueryExecutionPlan> AnalyzeQueryAsync(string sql);
    Task<QueryExecutionPlan> AnalyzeQueryAsync(string sql, Dictionary<string, object> parameters);
    Task<PerformanceBottleneck[]> DetectBottlenecksAsync(string sessionId);
    Task<PerformanceBottleneck[]> DetectBottlenecksAsync(string sessionId, BottleneckDetectionOptions options);
    Task<ProfilingSession[]> GetActiveSessionsAsync();
    Task<ProfilingSession[]> GetAllSessionsAsync();
    Task ClearSessionsAsync();
    bool IsEnabled { get; }
    void SetEnabled(bool enabled);
}

public class ProfilingSession
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;
    public bool IsActive => !EndTime.HasValue;
    public ProfilingOptions Options { get; set; }
    public List<ProfilingEvent> Events { get; set; } = new();
    public List<QueryExecution> Queries { get; set; } = new();
    public List<ConnectionUsage> Connections { get; set; } = new();
    public MemoryUsage Memory { get; set; }
    public CpuUsage Cpu { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ProfilingEvent
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public string Category { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public Exception Exception { get; set; }
    public bool Success { get; set; }
}

public class QueryExecution
{
    public string Id { get; set; }
    public string Sql { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public int RowsAffected { get; set; }
    public int RowsReturned { get; set; }
    public QueryExecutionPlan ExecutionPlan { get; set; }
    public Exception Exception { get; set; }
    public bool Success { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}

public class ConnectionUsage
{
    public string Id { get; set; }
    public string ConnectionString { get; set; }
    public DateTime OpenTime { get; set; }
    public DateTime? CloseTime { get; set; }
    public TimeSpan Duration => (CloseTime ?? DateTime.UtcNow) - OpenTime;
    public bool IsActive => !CloseTime.HasValue;
    public int QueryCount { get; set; }
    public TimeSpan TotalQueryTime { get; set; }
    public Exception Exception { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}

public class MemoryUsage
{
    public long TotalMemory { get; set; }
    public long UsedMemory { get; set; }
    public long AvailableMemory { get; set; }
    public long GarbageCollections { get; set; }
    public TimeSpan GarbageCollectionTime { get; set; }
    public DateTime Timestamp { get; set; }
}

public class CpuUsage
{
    public double ProcessorTime { get; set; }
    public double UserTime { get; set; }
    public double PrivilegedTime { get; set; }
    public DateTime Timestamp { get; set; }
}

public class QueryExecutionPlan
{
    public string Sql { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<ExecutionStep> Steps { get; set; } = new();
    public TimeSpan EstimatedDuration { get; set; }
    public int EstimatedRows { get; set; }
    public double EstimatedCost { get; set; }
    public List<IndexRecommendation> IndexRecommendations { get; set; } = new();
    public List<QueryOptimization> Optimizations { get; set; } = new();
    public PlanQuality Quality { get; set; }
}

public class ExecutionStep
{
    public string Operation { get; set; }
    public string Table { get; set; }
    public string Index { get; set; }
    public int Rows { get; set; }
    public double Cost { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class IndexRecommendation
{
    public string Table { get; set; }
    public string[] Columns { get; set; }
    public string Type { get; set; }
    public string Reason { get; set; }
    public double ExpectedImprovement { get; set; }
    public string Sql { get; set; }
}

public class QueryOptimization
{
    public string Type { get; set; }
    public string Description { get; set; }
    public string OriginalSql { get; set; }
    public string OptimizedSql { get; set; }
    public double ExpectedImprovement { get; set; }
    public string Reason { get; set; }
}

public enum PlanQuality
{
    Excellent,
    Good,
    Fair,
    Poor,
    Critical
}

public class PerformanceBottleneck
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public string Severity { get; set; }
    public double Impact { get; set; }
    public string Location { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime DetectedAt { get; set; }
}

public class ProfilingOptions
{
    public bool EnableQueryProfiling { get; set; } = true;
    public bool EnableConnectionProfiling { get; set; } = true;
    public bool EnableMemoryProfiling { get; set; } = true;
    public bool EnableCpuProfiling { get; set; } = true;
    public bool EnableExecutionPlanAnalysis { get; set; } = true;
    public TimeSpan MaxSessionDuration { get; set; } = TimeSpan.FromHours(1);
    public int MaxEventsPerSession { get; set; } = 10000;
    public int MaxQueriesPerSession { get; set; } = 1000;
    public double SlowQueryThreshold { get; set; } = 1000; // milliseconds
    public bool EnableBottleneckDetection { get; set; } = true;
    public BottleneckDetectionOptions BottleneckOptions { get; set; } = new();
}

public class BottleneckDetectionOptions
{
    public double SlowQueryThreshold { get; set; } = 1000; // milliseconds
    public double HighMemoryUsageThreshold { get; set; } = 0.8; // 80%
    public double HighCpuUsageThreshold { get; set; } = 0.8; // 80%
    public int MaxConnectionPoolUsageThreshold { get; set; } = 80; // 80%
    public double LowSuccessRateThreshold { get; set; } = 0.95; // 95%
    public bool EnableIndexAnalysis { get; set; } = true;
    public bool EnableQueryOptimization { get; set; } = true;
}
```

### PerformanceProfiler Class
```csharp
public class PerformanceProfiler : IPerformanceProfiler
{
    private readonly ConcurrentDictionary<string, ProfilingSession> _sessions = new();
    private readonly IQueryAnalyzer _queryAnalyzer;
    private readonly IBottleneckDetector _bottleneckDetector;
    private readonly IProfilingReporter _profilingReporter;
    private readonly ProfilingOptions _defaultOptions;
    private bool _disposed;
    private bool _enabled = true;
    
    public bool IsEnabled => _enabled;
    
    public PerformanceProfiler(
        IQueryAnalyzer queryAnalyzer,
        IBottleneckDetector bottleneckDetector,
        IProfilingReporter profilingReporter,
        ProfilingOptions defaultOptions)
    {
        _queryAnalyzer = queryAnalyzer ?? throw new ArgumentNullException(nameof(queryAnalyzer));
        _bottleneckDetector = bottleneckDetector ?? throw new ArgumentNullException(nameof(bottleneckDetector));
        _profilingReporter = profilingReporter ?? throw new ArgumentNullException(nameof(profilingReporter));
        _defaultOptions = defaultOptions ?? throw new ArgumentNullException(nameof(defaultOptions));
    }
    
    public async Task<string> StartProfilingAsync(string sessionName)
    {
        return await StartProfilingAsync(sessionName, _defaultOptions);
    }
    
    public async Task<string> StartProfilingAsync(string sessionName, ProfilingOptions options)
    {
        if (!_enabled) return string.Empty;
        if (string.IsNullOrEmpty(sessionName)) throw new ArgumentException("Session name cannot be null or empty", nameof(sessionName));
        if (options == null) throw new ArgumentNullException(nameof(options));
        
        var sessionId = Guid.NewGuid().ToString();
        var session = new ProfilingSession
        {
            Id = sessionId,
            Name = sessionName,
            StartTime = DateTime.UtcNow,
            Options = options,
            Memory = GetCurrentMemoryUsage(),
            Cpu = GetCurrentCpuUsage()
        };
        
        _sessions[sessionId] = session;
        
        return await Task.FromResult(sessionId);
    }
    
    public async Task StopProfilingAsync(string sessionId)
    {
        if (!_enabled) return;
        if (string.IsNullOrEmpty(sessionId)) throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.EndTime = DateTime.UtcNow;
            session.Memory = GetCurrentMemoryUsage();
            session.Cpu = GetCurrentCpuUsage();
        }
        
        await Task.CompletedTask;
    }
    
    public async Task<ProfilingSession> GetSessionAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId)) throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return await Task.FromResult(session);
        }
        
        throw new InvalidOperationException($"Profiling session '{sessionId}' not found");
    }
    
    public async Task<ProfilingReport> GenerateReportAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId)) throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        
        var session = await GetSessionAsync(sessionId);
        var report = await _profilingReporter.GenerateReportAsync(session);
        
        return report;
    }
    
    public async Task<QueryExecutionPlan> AnalyzeQueryAsync(string sql)
    {
        return await AnalyzeQueryAsync(sql, new Dictionary<string, object>());
    }
    
    public async Task<QueryExecutionPlan> AnalyzeQueryAsync(string sql, Dictionary<string, object> parameters)
    {
        if (string.IsNullOrEmpty(sql)) throw new ArgumentException("SQL cannot be null or empty", nameof(sql));
        
        return await _queryAnalyzer.AnalyzeQueryAsync(sql, parameters);
    }
    
    public async Task<PerformanceBottleneck[]> DetectBottlenecksAsync(string sessionId)
    {
        return await DetectBottlenecksAsync(sessionId, new BottleneckDetectionOptions());
    }
    
    public async Task<PerformanceBottleneck[]> DetectBottlenecksAsync(string sessionId, BottleneckDetectionOptions options)
    {
        if (string.IsNullOrEmpty(sessionId)) throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        
        var session = await GetSessionAsync(sessionId);
        return await _bottleneckDetector.DetectBottlenecksAsync(session, options);
    }
    
    public async Task<ProfilingSession[]> GetActiveSessionsAsync()
    {
        var activeSessions = _sessions.Values.Where(s => s.IsActive).ToArray();
        return await Task.FromResult(activeSessions);
    }
    
    public async Task<ProfilingSession[]> GetAllSessionsAsync()
    {
        var allSessions = _sessions.Values.ToArray();
        return await Task.FromResult(allSessions);
    }
    
    public async Task ClearSessionsAsync()
    {
        _sessions.Clear();
        await Task.CompletedTask;
    }
    
    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
    }
    
    public void RecordEvent(string sessionId, ProfilingEvent profilingEvent)
    {
        if (!_enabled) return;
        if (string.IsNullOrEmpty(sessionId)) return;
        
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Events.Add(profilingEvent);
            
            // Limit events per session
            if (session.Events.Count > session.Options.MaxEventsPerSession)
            {
                session.Events.RemoveAt(0);
            }
        }
    }
    
    public void RecordQuery(string sessionId, QueryExecution queryExecution)
    {
        if (!_enabled) return;
        if (string.IsNullOrEmpty(sessionId)) return;
        
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Queries.Add(queryExecution);
            
            // Limit queries per session
            if (session.Queries.Count > session.Options.MaxQueriesPerSession)
            {
                session.Queries.RemoveAt(0);
            }
        }
    }
    
    public void RecordConnection(string sessionId, ConnectionUsage connectionUsage)
    {
        if (!_enabled) return;
        if (string.IsNullOrEmpty(sessionId)) return;
        
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Connections.Add(connectionUsage);
        }
    }
    
    private MemoryUsage GetCurrentMemoryUsage()
    {
        var process = Process.GetCurrentProcess();
        var workingSet = process.WorkingSet64;
        var privateMemory = process.PrivateMemorySize64;
        var gcMemory = GC.GetTotalMemory(false);
        
        return new MemoryUsage
        {
            TotalMemory = workingSet,
            UsedMemory = privateMemory,
            AvailableMemory = workingSet - privateMemory,
            GarbageCollections = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2),
            GarbageCollectionTime = TimeSpan.Zero, // Would need to track this separately
            Timestamp = DateTime.UtcNow
        };
    }
    
    private CpuUsage GetCurrentCpuUsage()
    {
        var process = Process.GetCurrentProcess();
        var totalProcessorTime = process.TotalProcessorTime;
        var userTime = process.UserProcessorTime;
        var privilegedTime = process.PrivilegedProcessorTime;
        
        return new CpuUsage
        {
            ProcessorTime = totalProcessorTime.TotalMilliseconds,
            UserTime = userTime.TotalMilliseconds,
            PrivilegedTime = privilegedTime.TotalMilliseconds,
            Timestamp = DateTime.UtcNow
        };
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        _sessions.Clear();
    }
}
```

### QueryAnalyzer Class
```csharp
public class QueryAnalyzer : IQueryAnalyzer
{
    private readonly IDbConnection _connection;
    
    public QueryAnalyzer(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
    
    public async Task<QueryExecutionPlan> AnalyzeQueryAsync(string sql, Dictionary<string, object> parameters)
    {
        if (string.IsNullOrEmpty(sql)) throw new ArgumentException("SQL cannot be null or empty", nameof(sql));
        
        var executionPlan = new QueryExecutionPlan
        {
            Sql = sql,
            Parameters = parameters ?? new Dictionary<string, object>()
        };
        
        try
        {
            // Get execution plan from database
            var planData = await GetExecutionPlanFromDatabaseAsync(sql, parameters);
            
            // Parse execution plan
            executionPlan.Steps = ParseExecutionPlan(planData);
            
            // Analyze execution plan
            AnalyzeExecutionPlan(executionPlan);
            
            // Generate recommendations
            executionPlan.IndexRecommendations = GenerateIndexRecommendations(executionPlan);
            executionPlan.Optimizations = GenerateQueryOptimizations(executionPlan);
            
            // Determine plan quality
            executionPlan.Quality = DeterminePlanQuality(executionPlan);
        }
        catch (Exception ex)
        {
            // Handle analysis errors
            executionPlan.Quality = PlanQuality.Critical;
        }
        
        return executionPlan;
    }
    
    private async Task<object> GetExecutionPlanFromDatabaseAsync(string sql, Dictionary<string, object> parameters)
    {
        // This would be database-specific implementation
        // For SQL Server, you might use SET SHOWPLAN_ALL ON
        // For PostgreSQL, you might use EXPLAIN (ANALYZE, BUFFERS)
        // For MySQL, you might use EXPLAIN FORMAT=JSON
        
        var planQuery = GetExecutionPlanQuery(sql);
        var planData = await _connection.QueryFirstOrDefaultAsync(planQuery, parameters);
        
        return planData;
    }
    
    private string GetExecutionPlanQuery(string sql)
    {
        // This would be database-specific
        // For SQL Server:
        return $"SET SHOWPLAN_ALL ON; {sql}; SET SHOWPLAN_ALL OFF";
        
        // For PostgreSQL:
        // return $"EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) {sql}";
        
        // For MySQL:
        // return $"EXPLAIN FORMAT=JSON {sql}";
    }
    
    private List<ExecutionStep> ParseExecutionPlan(object planData)
    {
        var steps = new List<ExecutionStep>();
        
        // This would be database-specific parsing
        // Parse the execution plan data and convert to ExecutionStep objects
        
        return steps;
    }
    
    private void AnalyzeExecutionPlan(QueryExecutionPlan executionPlan)
    {
        // Analyze the execution plan for performance issues
        foreach (var step in executionPlan.Steps)
        {
            // Calculate estimated duration and cost
            step.Duration = CalculateStepDuration(step);
            step.Cost = CalculateStepCost(step);
        }
        
        // Calculate overall plan metrics
        executionPlan.EstimatedDuration = TimeSpan.FromTicks(executionPlan.Steps.Sum(s => s.Duration.Ticks));
        executionPlan.EstimatedRows = executionPlan.Steps.Sum(s => s.Rows);
        executionPlan.EstimatedCost = executionPlan.Steps.Sum(s => s.Cost);
    }
    
    private TimeSpan CalculateStepDuration(ExecutionStep step)
    {
        // Calculate estimated duration based on step properties
        // This would be database-specific
        return TimeSpan.Zero;
    }
    
    private double CalculateStepCost(ExecutionStep step)
    {
        // Calculate estimated cost based on step properties
        // This would be database-specific
        return 0.0;
    }
    
    private List<IndexRecommendation> GenerateIndexRecommendations(QueryExecutionPlan executionPlan)
    {
        var recommendations = new List<IndexRecommendation>();
        
        // Analyze execution plan for missing indexes
        foreach (var step in executionPlan.Steps)
        {
            if (step.Operation == "Table Scan" || step.Operation == "Clustered Index Scan")
            {
                var recommendation = new IndexRecommendation
                {
                    Table = step.Table,
                    Columns = ExtractColumnsFromStep(step),
                    Type = "Non-Clustered",
                    Reason = "Table scan detected - consider adding index",
                    ExpectedImprovement = CalculateExpectedImprovement(step),
                    Sql = GenerateIndexSql(step)
                };
                
                recommendations.Add(recommendation);
            }
        }
        
        return recommendations;
    }
    
    private List<QueryOptimization> GenerateQueryOptimizations(QueryExecutionPlan executionPlan)
    {
        var optimizations = new List<QueryOptimization>();
        
        // Analyze execution plan for optimization opportunities
        foreach (var step in executionPlan.Steps)
        {
            if (step.Operation == "Nested Loops" && step.Cost > 1000)
            {
                var optimization = new QueryOptimization
                {
                    Type = "Join Optimization",
                    Description = "Consider using hash join instead of nested loops",
                    OriginalSql = executionPlan.Sql,
                    OptimizedSql = OptimizeJoin(executionPlan.Sql),
                    ExpectedImprovement = 0.5,
                    Reason = "Nested loops join with high cost detected"
                };
                
                optimizations.Add(optimization);
            }
        }
        
        return optimizations;
    }
    
    private PlanQuality DeterminePlanQuality(QueryExecutionPlan executionPlan)
    {
        // Determine plan quality based on various factors
        var score = 0.0;
        
        // Check for table scans
        var tableScans = executionPlan.Steps.Count(s => s.Operation == "Table Scan");
        if (tableScans > 0) score -= tableScans * 0.2;
        
        // Check for nested loops with high cost
        var expensiveNestedLoops = executionPlan.Steps.Count(s => s.Operation == "Nested Loops" && s.Cost > 1000);
        if (expensiveNestedLoops > 0) score -= expensiveNestedLoops * 0.1;
        
        // Check for missing indexes
        var missingIndexes = executionPlan.IndexRecommendations.Count;
        if (missingIndexes > 0) score -= missingIndexes * 0.15;
        
        // Check for high estimated cost
        if (executionPlan.EstimatedCost > 10000) score -= 0.3;
        
        // Determine quality based on score
        if (score >= 0.8) return PlanQuality.Excellent;
        if (score >= 0.6) return PlanQuality.Good;
        if (score >= 0.4) return PlanQuality.Fair;
        if (score >= 0.2) return PlanQuality.Poor;
        return PlanQuality.Critical;
    }
    
    private string[] ExtractColumnsFromStep(ExecutionStep step)
    {
        // Extract columns from execution step
        // This would be database-specific
        return new string[0];
    }
    
    private double CalculateExpectedImprovement(ExecutionStep step)
    {
        // Calculate expected improvement from adding index
        // This would be database-specific
        return 0.5;
    }
    
    private string GenerateIndexSql(ExecutionStep step)
    {
        // Generate SQL for creating index
        // This would be database-specific
        return $"CREATE INDEX IX_{step.Table}_{string.Join("_", step.Properties.Keys)} ON {step.Table} ({string.Join(", ", step.Properties.Keys)})";
    }
    
    private string OptimizeJoin(string sql)
    {
        // Optimize join in SQL
        // This would be database-specific
        return sql;
    }
}
```

### Usage Examples
```csharp
// Basic performance profiling
public class UserService
{
    private readonly IEntityManager _entityManager;
    private readonly IPerformanceProfiler _profiler;
    
    public UserService(IEntityManager entityManager, IPerformanceProfiler profiler)
    {
        _entityManager = entityManager;
        _profiler = profiler;
    }
    
    public async Task<User> GetUserAsync(long id)
    {
        var sessionId = await _profiler.StartProfilingAsync("GetUser");
        
        try
        {
            var user = await _entityManager.FindAsync<User>(id);
            await _profiler.StopProfilingAsync(sessionId);
            return user;
        }
        catch (Exception ex)
        {
            await _profiler.StopProfilingAsync(sessionId);
            throw;
        }
    }
}

// Advanced performance profiling with analysis
public class AdvancedUserService
{
    private readonly IEntityManager _entityManager;
    private readonly IPerformanceProfiler _profiler;
    
    public AdvancedUserService(IEntityManager entityManager, IPerformanceProfiler profiler)
    {
        _entityManager = entityManager;
        _profiler = profiler;
    }
    
    public async Task<User> GetUserWithProfilingAsync(long id)
    {
        var sessionId = await _profiler.StartProfilingAsync("GetUserWithProfiling", new ProfilingOptions
        {
            EnableQueryProfiling = true,
            EnableExecutionPlanAnalysis = true,
            EnableBottleneckDetection = true,
            SlowQueryThreshold = 500
        });
        
        try
        {
            var user = await _entityManager.FindAsync<User>(id);
            
            // Generate profiling report
            var report = await _profiler.GenerateReportAsync(sessionId);
            
            // Detect bottlenecks
            var bottlenecks = await _profiler.DetectBottlenecksAsync(sessionId);
            
            if (bottlenecks.Any())
            {
                Console.WriteLine("Performance bottlenecks detected:");
                foreach (var bottleneck in bottlenecks)
                {
                    Console.WriteLine($"- {bottleneck.Type}: {bottleneck.Description}");
                }
            }
            
            await _profiler.StopProfilingAsync(sessionId);
            return user;
        }
        catch (Exception ex)
        {
            await _profiler.StopProfilingAsync(sessionId);
            throw;
        }
    }
    
    public async Task<QueryExecutionPlan> AnalyzeQueryAsync(string sql)
    {
        return await _profiler.AnalyzeQueryAsync(sql);
    }
    
    public async Task<ProfilingReport> GetProfilingReportAsync(string sessionId)
    {
        return await _profiler.GenerateReportAsync(sessionId);
    }
}

// Performance profiling service
public class PerformanceProfilingService
{
    private readonly IPerformanceProfiler _profiler;
    private readonly IProfilingReporter _reporter;
    
    public PerformanceProfilingService(IPerformanceProfiler profiler, IProfilingReporter reporter)
    {
        _profiler = profiler;
        _reporter = reporter;
    }
    
    public async Task StartProfilingSessionAsync(string sessionName)
    {
        var sessionId = await _profiler.StartProfilingAsync(sessionName, new ProfilingOptions
        {
            EnableQueryProfiling = true,
            EnableConnectionProfiling = true,
            EnableMemoryProfiling = true,
            EnableCpuProfiling = true,
            EnableExecutionPlanAnalysis = true,
            EnableBottleneckDetection = true,
            MaxSessionDuration = TimeSpan.FromHours(1),
            SlowQueryThreshold = 1000
        });
        
        Console.WriteLine($"Started profiling session: {sessionId}");
    }
    
    public async Task StopProfilingSessionAsync(string sessionId)
    {
        await _profiler.StopProfilingAsync(sessionId);
        
        // Generate and save report
        var report = await _profiler.GenerateReportAsync(sessionId);
        var htmlReport = await _reporter.GenerateHtmlReportAsync(report);
        
        var fileName = $"profiling-report-{sessionId}.html";
        await File.WriteAllTextAsync(fileName, htmlReport);
        
        Console.WriteLine($"Profiling report saved: {fileName}");
    }
    
    public async Task AnalyzeSlowQueriesAsync()
    {
        var sessions = await _profiler.GetAllSessionsAsync();
        
        foreach (var session in sessions)
        {
            var bottlenecks = await _profiler.DetectBottlenecksAsync(session.Id);
            var slowQueryBottlenecks = bottlenecks.Where(b => b.Type == "SlowQuery").ToArray();
            
            if (slowQueryBottlenecks.Any())
            {
                Console.WriteLine($"Slow queries detected in session {session.Name}:");
                
                foreach (var bottleneck in slowQueryBottlenecks)
                {
                    Console.WriteLine($"- {bottleneck.Description}");
                    Console.WriteLine($"  Impact: {bottleneck.Impact:P2}");
                    Console.WriteLine($"  Recommendations: {string.Join(", ", bottleneck.Recommendations)}");
                }
            }
        }
    }
}
```

## 🧪 Test Cases

### Performance Profiler Tests
- [ ] Profiling session management
- [ ] Event recording
- [ ] Query recording
- [ ] Connection recording

### Query Analyzer Tests
- [ ] Query execution plan analysis
- [ ] Index recommendations
- [ ] Query optimizations
- [ ] Plan quality determination

### Bottleneck Detector Tests
- [ ] Slow query detection
- [ ] Resource usage analysis
- [ ] Connection pool analysis
- [ ] Memory usage analysis

### Profiling Reporter Tests
- [ ] Report generation
- [ ] HTML report generation
- [ ] JSON report generation
- [ ] Report saving

### Integration Tests
- [ ] End-to-end performance profiling
- [ ] Profiling report generation
- [ ] Bottleneck detection
- [ ] Performance analysis

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Basic performance profiling
- [ ] Advanced performance profiling
- [ ] Query analysis
- [ ] Best practices

### Performance Profiling Guide
- [ ] Profiling concepts
- [ ] Query analysis
- [ ] Bottleneck detection
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
1. Move to Phase 6.4: Comprehensive Documentation
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on performance profiling design
- [ ] Performance considerations for profiling
- [ ] Integration with existing features
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
