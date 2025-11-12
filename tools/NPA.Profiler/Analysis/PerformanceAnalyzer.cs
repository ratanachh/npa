namespace NPA.Profiler.Analysis;

/// <summary>
/// Analyzes profiling data to detect performance issues and provide optimization suggestions.
/// </summary>
public class PerformanceAnalyzer
{
    private readonly PerformanceAnalyzerOptions _options;

    public PerformanceAnalyzer(PerformanceAnalyzerOptions? options = null)
    {
        _options = options ?? new PerformanceAnalyzerOptions();
    }

    /// <summary>
    /// Analyzes a profiling session and generates a comprehensive report.
    /// </summary>
    public AnalysisReport Analyze(Profiling.ProfilingSession session)
    {
        var report = new AnalysisReport
        {
            SessionId = session.SessionId,
            Duration = session.Duration,
            Statistics = session.GetStatistics()
        };

        // Detect N+1 queries
        report.NPlusOneIssues = DetectNPlusOne(session);

        // Detect missing indexes (full table scans)
        report.MissingIndexes = DetectMissingIndexes(session);

        // Detect large result sets
        report.LargeResultSets = DetectLargeResultSets(session);

        // Detect slow queries
        report.SlowQueries = DetectSlowQueries(session);

        // Detect excessive queries
        report.ExcessiveQueries = DetectExcessiveQueries(session);

        // Generate optimization suggestions
        report.Suggestions = GenerateOptimizationSuggestions(report);

        // Calculate overall score
        report.PerformanceScore = CalculatePerformanceScore(report);

        return report;
    }

    /// <summary>
    /// Detects N+1 query patterns.
    /// </summary>
    private List<NPlusOneIssue> DetectNPlusOne(Profiling.ProfilingSession session)
    {
        var issues = new List<NPlusOneIssue>();
        var queries = session.Queries.ToList();

        // Group queries by normalized SQL (removing parameter values)
        var queryGroups = queries
            .GroupBy(q => NormalizeSql(q.Sql))
            .Where(g => g.Count() >= _options.NPlusOneThreshold)
            .ToList();

        foreach (var group in queryGroups)
        {
            // Check if queries are sequential and similar
            var groupQueries = group.OrderBy(q => q.Timestamp).ToList();
            var timeSpan = groupQueries.Last().Timestamp - groupQueries.First().Timestamp;

            // If many similar queries occur within a short time span, it's likely N+1
            if (timeSpan.TotalSeconds < _options.NPlusOneTimeWindowSeconds)
            {
                issues.Add(new NPlusOneIssue
                {
                    SqlPattern = group.Key,
                    Occurrences = group.Count(),
                    TotalDuration = group.Sum(q => q.Duration.TotalMilliseconds),
                    FirstOccurrence = groupQueries.First().Timestamp,
                    LastOccurrence = groupQueries.Last().Timestamp,
                    EntityType = groupQueries.First().EntityType,
                    Queries = groupQueries
                });
            }
        }

        return issues;
    }

    /// <summary>
    /// Detects queries that may benefit from indexes.
    /// </summary>
    private List<MissingIndexIssue> DetectMissingIndexes(Profiling.ProfilingSession session)
    {
        var issues = new List<MissingIndexIssue>();

        // Look for SELECT queries with WHERE clauses that are slow
        var selectQueries = session.Queries
            .Where(q => q.QueryType == Profiling.QueryType.Select)
            .Where(q => q.Duration.TotalMilliseconds > _options.SlowQueryThresholdMs)
            .Where(q => q.Sql.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var query in selectQueries)
        {
            // Extract potential columns from WHERE clause (simplified heuristic)
            var whereColumns = ExtractWhereColumns(query.Sql);

            issues.Add(new MissingIndexIssue
            {
                Sql = query.Sql,
                Duration = query.Duration.TotalMilliseconds,
                EntityType = query.EntityType,
                SuggestedColumns = whereColumns,
                RowsAffected = query.RowsAffected
            });
        }

        return issues;
    }

    /// <summary>
    /// Detects queries returning large result sets without pagination.
    /// </summary>
    private List<LargeResultSetIssue> DetectLargeResultSets(Profiling.ProfilingSession session)
    {
        var issues = new List<LargeResultSetIssue>();

        var largeQueries = session.Queries
            .Where(q => q.QueryType == Profiling.QueryType.Select)
            .Where(q => q.RowsAffected > _options.LargeResultSetThreshold)
            .Where(q => !q.Sql.Contains("LIMIT", StringComparison.OrdinalIgnoreCase) &&
                       !q.Sql.Contains("TOP", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var query in largeQueries)
        {
            issues.Add(new LargeResultSetIssue
            {
                Sql = query.Sql,
                RowCount = query.RowsAffected,
                Duration = query.Duration.TotalMilliseconds,
                EntityType = query.EntityType
            });
        }

        return issues;
    }

    /// <summary>
    /// Detects individual slow queries.
    /// </summary>
    private List<SlowQueryIssue> DetectSlowQueries(Profiling.ProfilingSession session)
    {
        return session.Queries
            .Where(q => q.Duration.TotalMilliseconds > _options.SlowQueryThresholdMs)
            .Select(q => new SlowQueryIssue
            {
                Sql = q.Sql,
                Duration = q.Duration.TotalMilliseconds,
                QueryType = q.QueryType,
                EntityType = q.EntityType,
                Timestamp = q.Timestamp
            })
            .ToList();
    }

    /// <summary>
    /// Detects excessive query counts that might indicate chatty operations.
    /// </summary>
    private List<ExcessiveQueryIssue> DetectExcessiveQueries(Profiling.ProfilingSession session)
    {
        var issues = new List<ExcessiveQueryIssue>();

        if (session.TotalQueries > _options.ExcessiveQueryThreshold)
        {
            issues.Add(new ExcessiveQueryIssue
            {
                TotalQueries = session.TotalQueries,
                Duration = session.Duration,
                QueriesPerSecond = session.TotalQueries / session.Duration.TotalSeconds
            });
        }

        return issues;
    }

    /// <summary>
    /// Generates optimization suggestions based on detected issues.
    /// </summary>
    private List<OptimizationSuggestion> GenerateOptimizationSuggestions(AnalysisReport report)
    {
        var suggestions = new List<OptimizationSuggestion>();

        // N+1 suggestions
        foreach (var issue in report.NPlusOneIssues)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Category = "N+1 Query",
                Priority = Priority.High,
                Title = $"N+1 query detected: {issue.Occurrences} similar queries",
                Description = $"Found {issue.Occurrences} similar queries for {issue.EntityType}. " +
                             "Consider using eager loading with Include() or LoadWith() to fetch related entities in a single query.",
                Impact = $"Potential savings: {issue.TotalDuration:F2}ms",
                Example = $"// Instead of multiple queries:\nforeach(var item in items) {{ var related = item.Related; }}\n\n" +
                         $"// Use eager loading:\nvar items = repository.Query().Include(x => x.Related).ToList();"
            });
        }

        // Missing index suggestions
        foreach (var issue in report.MissingIndexes)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Category = "Missing Index",
                Priority = Priority.Medium,
                Title = $"Slow query detected on {issue.EntityType}",
                Description = $"Query took {issue.Duration:F2}ms. Consider adding an index on columns used in WHERE clause.",
                Impact = $"Query duration: {issue.Duration:F2}ms",
                Example = issue.SuggestedColumns.Any()
                    ? $"CREATE INDEX IX_{issue.EntityType}_{string.Join("_", issue.SuggestedColumns)} ON {issue.EntityType} ({string.Join(", ", issue.SuggestedColumns)});"
                    : "Analyze the WHERE clause and add appropriate indexes."
            });
        }

        // Large result set suggestions
        foreach (var issue in report.LargeResultSets)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Category = "Large Result Set",
                Priority = Priority.Medium,
                Title = $"Large result set: {issue.RowCount} rows",
                Description = $"Query returned {issue.RowCount} rows without pagination. Consider implementing pagination for better performance.",
                Impact = $"Rows: {issue.RowCount}, Duration: {issue.Duration:F2}ms",
                Example = "// Add pagination:\nvar results = repository.Query()\n    .Skip((page - 1) * pageSize)\n    .Take(pageSize)\n    .ToList();"
            });
        }

        // Slow query suggestions
        foreach (var issue in report.SlowQueries.Take(5)) // Limit to top 5
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Category = "Slow Query",
                Priority = Priority.High,
                Title = $"Slow {issue.QueryType} query: {issue.Duration:F2}ms",
                Description = "Query execution time exceeds threshold. Consider optimizing the query or adding indexes.",
                Impact = $"Duration: {issue.Duration:F2}ms",
                Example = issue.Sql
            });
        }

        // Excessive query suggestions
        foreach (var issue in report.ExcessiveQueries)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Category = "Excessive Queries",
                Priority = Priority.High,
                Title = $"Excessive query count: {issue.TotalQueries} queries",
                Description = $"Operation executed {issue.TotalQueries} queries ({issue.QueriesPerSecond:F2} queries/sec). " +
                             "Consider batching operations or using eager loading.",
                Impact = $"Total queries: {issue.TotalQueries}",
                Example = "// Use batch operations:\nrepository.BulkInsert(entities);\n\n// Or eager loading:\nrepository.Query().Include(x => x.Related).ToList();"
            });
        }

        return suggestions.OrderByDescending(s => s.Priority).ToList();
    }

    /// <summary>
    /// Calculates an overall performance score (0-100).
    /// </summary>
    private int CalculatePerformanceScore(AnalysisReport report)
    {
        var score = 100;

        // Deduct points for N+1 issues
        score -= report.NPlusOneIssues.Count * 10;

        // Deduct points for missing indexes
        score -= report.MissingIndexes.Count * 5;

        // Deduct points for large result sets
        score -= report.LargeResultSets.Count * 5;

        // Deduct points for slow queries
        score -= Math.Min(report.SlowQueries.Count * 2, 20);

        // Deduct points for excessive queries
        score -= report.ExcessiveQueries.Count * 15;

        return Math.Max(0, Math.Min(100, score));
    }

    private string NormalizeSql(string sql)
    {
        // Remove parameter values and normalize whitespace
        var normalized = System.Text.RegularExpressions.Regex.Replace(sql, @"@\w+|\?\d*|:\w+", "?");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");
        return normalized.Trim();
    }

    private List<string> ExtractWhereColumns(string sql)
    {
        var columns = new List<string>();

        // Simple heuristic: extract column names after WHERE
        var whereIndex = sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
        if (whereIndex == -1) return columns;

        var whereClause = sql.Substring(whereIndex);
        var matches = System.Text.RegularExpressions.Regex.Matches(whereClause, @"\b(\w+)\s*[=<>]");

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var column = match.Groups[1].Value;
            if (!IsKeyword(column))
            {
                columns.Add(column);
            }
        }

        return columns.Distinct().ToList();
    }

    private bool IsKeyword(string word)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AND", "OR", "NOT", "NULL", "TRUE", "FALSE", "SELECT", "FROM", "WHERE", "ORDER", "BY"
        };
        return keywords.Contains(word);
    }
}

/// <summary>
/// Configuration options for performance analysis.
/// </summary>
public class PerformanceAnalyzerOptions
{
    public int NPlusOneThreshold { get; set; } = 3;
    public double NPlusOneTimeWindowSeconds { get; set; } = 5.0;
    public double SlowQueryThresholdMs { get; set; } = 100.0;
    public int LargeResultSetThreshold { get; set; } = 1000;
    public int ExcessiveQueryThreshold { get; set; } = 50;
}

/// <summary>
/// Complete analysis report.
/// </summary>
public class AnalysisReport
{
    public Guid SessionId { get; set; }
    public TimeSpan Duration { get; set; }
    public Profiling.QueryStatistics Statistics { get; set; } = new();
    public List<NPlusOneIssue> NPlusOneIssues { get; set; } = new();
    public List<MissingIndexIssue> MissingIndexes { get; set; } = new();
    public List<LargeResultSetIssue> LargeResultSets { get; set; } = new();
    public List<SlowQueryIssue> SlowQueries { get; set; } = new();
    public List<ExcessiveQueryIssue> ExcessiveQueries { get; set; } = new();
    public List<OptimizationSuggestion> Suggestions { get; set; } = new();
    public int PerformanceScore { get; set; }
}

public class NPlusOneIssue
{
    public string SqlPattern { get; set; } = string.Empty;
    public int Occurrences { get; set; }
    public double TotalDuration { get; set; }
    public DateTime FirstOccurrence { get; set; }
    public DateTime LastOccurrence { get; set; }
    public string? EntityType { get; set; }
    public List<Profiling.QueryProfile> Queries { get; set; } = new();
}

public class MissingIndexIssue
{
    public string Sql { get; set; } = string.Empty;
    public double Duration { get; set; }
    public string? EntityType { get; set; }
    public List<string> SuggestedColumns { get; set; } = new();
    public int RowsAffected { get; set; }
}

public class LargeResultSetIssue
{
    public string Sql { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public double Duration { get; set; }
    public string? EntityType { get; set; }
}

public class SlowQueryIssue
{
    public string Sql { get; set; } = string.Empty;
    public double Duration { get; set; }
    public Profiling.QueryType QueryType { get; set; }
    public string? EntityType { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ExcessiveQueryIssue
{
    public int TotalQueries { get; set; }
    public TimeSpan Duration { get; set; }
    public double QueriesPerSecond { get; set; }
}

public class OptimizationSuggestion
{
    public string Category { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
}

public enum Priority
{
    Low,
    Medium,
    High
}
