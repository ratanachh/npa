using System.Text;
using NPA.Profiler.Analysis;

namespace NPA.Profiler.Reports;

/// <summary>
/// Generates performance reports in console-friendly text format.
/// </summary>
public class ConsoleReportGenerator : IReportGenerator
{
    public Task<string> GenerateAsync(AnalysisReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine("═══════════════════════════════════════════════════════════════════════");
        sb.AppendLine("                    NPA PERFORMANCE ANALYSIS REPORT");
        sb.AppendLine("═══════════════════════════════════════════════════════════════════════");
        sb.AppendLine();

        // Overall Score
        sb.AppendLine($"Performance Score: {report.PerformanceScore}/100 {GetScoreEmoji(report.PerformanceScore)}");
        sb.AppendLine($"Session Duration: {report.Duration.TotalSeconds:F2}s");
        sb.AppendLine();

        // Statistics
        sb.AppendLine("─────────────────────────────────────────────────────────────────────");
        sb.AppendLine("QUERY STATISTICS");
        sb.AppendLine("─────────────────────────────────────────────────────────────────────");
        var stats = report.Statistics;
        sb.AppendLine($"Total Queries:        {stats.TotalQueries}");
        sb.AppendLine($"Total Duration:       {stats.TotalDuration:F2}ms");
        sb.AppendLine($"Average Duration:     {stats.AverageDuration:F2}ms");
        sb.AppendLine($"Min Duration:         {stats.MinDuration:F2}ms");
        sb.AppendLine($"Max Duration:         {stats.MaxDuration:F2}ms");
        sb.AppendLine($"P95 Duration:         {stats.P95Duration:F2}ms");
        sb.AppendLine($"P99 Duration:         {stats.P99Duration:F2}ms");
        sb.AppendLine($"Cache Hit Rate:       {stats.CacheHitRate:P2}");
        sb.AppendLine($"Slow Queries (>100ms): {stats.SlowQueries.Count}");
        sb.AppendLine();

        // Issues
        if (report.NPlusOneIssues.Any())
        {
            sb.AppendLine("─────────────────────────────────────────────────────────────────────");
            sb.AppendLine($"N+1 QUERY ISSUES ({report.NPlusOneIssues.Count})");
            sb.AppendLine("─────────────────────────────────────────────────────────────────────");
            foreach (var issue in report.NPlusOneIssues.Take(5))
            {
                sb.AppendLine($"  • {issue.Occurrences} occurrences in {(issue.LastOccurrence - issue.FirstOccurrence).TotalSeconds:F2}s");
                sb.AppendLine($"    Entity: {issue.EntityType ?? "Unknown"}");
                sb.AppendLine($"    Total Time: {issue.TotalDuration:F2}ms");
                sb.AppendLine($"    Pattern: {TruncateSql(issue.SqlPattern)}");
                sb.AppendLine();
            }
        }

        if (report.MissingIndexes.Any())
        {
            sb.AppendLine("─────────────────────────────────────────────────────────────────────");
            sb.AppendLine($"MISSING INDEX ISSUES ({report.MissingIndexes.Count})");
            sb.AppendLine("─────────────────────────────────────────────────────────────────────");
            foreach (var issue in report.MissingIndexes.Take(5))
            {
                sb.AppendLine($"  • {issue.EntityType ?? "Unknown"} - {issue.Duration:F2}ms");
                if (issue.SuggestedColumns.Any())
                {
                    sb.AppendLine($"    Suggested Columns: {string.Join(", ", issue.SuggestedColumns)}");
                }
                sb.AppendLine($"    SQL: {TruncateSql(issue.Sql)}");
                sb.AppendLine();
            }
        }

        if (report.LargeResultSets.Any())
        {
            sb.AppendLine("─────────────────────────────────────────────────────────────────────");
            sb.AppendLine($"LARGE RESULT SET ISSUES ({report.LargeResultSets.Count})");
            sb.AppendLine("─────────────────────────────────────────────────────────────────────");
            foreach (var issue in report.LargeResultSets.Take(5))
            {
                sb.AppendLine($"  • {issue.RowCount} rows returned - {issue.Duration:F2}ms");
                sb.AppendLine($"    Entity: {issue.EntityType ?? "Unknown"}");
                sb.AppendLine($"    SQL: {TruncateSql(issue.Sql)}");
                sb.AppendLine();
            }
        }

        if (report.ExcessiveQueries.Any())
        {
            sb.AppendLine("─────────────────────────────────────────────────────────────────────");
            sb.AppendLine($"EXCESSIVE QUERY ISSUES ({report.ExcessiveQueries.Count})");
            sb.AppendLine("─────────────────────────────────────────────────────────────────────");
            foreach (var issue in report.ExcessiveQueries)
            {
                sb.AppendLine($"  • {issue.TotalQueries} queries in {issue.Duration.TotalSeconds:F2}s");
                sb.AppendLine($"    Rate: {issue.QueriesPerSecond:F2} queries/second");
                sb.AppendLine();
            }
        }

        // Top Optimization Suggestions
        if (report.Suggestions.Any())
        {
            sb.AppendLine("─────────────────────────────────────────────────────────────────────");
            sb.AppendLine($"TOP OPTIMIZATION SUGGESTIONS ({report.Suggestions.Count})");
            sb.AppendLine("─────────────────────────────────────────────────────────────────────");
            foreach (var suggestion in report.Suggestions.Take(10))
            {
                var prioritySymbol = suggestion.Priority switch
                {
                    Priority.High => "🔴 HIGH",
                    Priority.Medium => "🟡 MEDIUM",
                    _ => "🟢 LOW"
                };

                sb.AppendLine($"{prioritySymbol} | {suggestion.Category}");
                sb.AppendLine($"  Title: {suggestion.Title}");
                sb.AppendLine($"  {suggestion.Description}");
                sb.AppendLine($"  Impact: {suggestion.Impact}");
                if (!string.IsNullOrEmpty(suggestion.Example))
                {
                    sb.AppendLine($"  Example:");
                    foreach (var line in suggestion.Example.Split('\n').Take(5))
                    {
                        sb.AppendLine($"    {line}");
                    }
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine("═══════════════════════════════════════════════════════════════════════");

        return Task.FromResult(sb.ToString());
    }

    private string GetScoreEmoji(int score)
    {
        return score switch
        {
            >= 90 => "🟢 Excellent",
            >= 70 => "🟡 Good",
            >= 50 => "🟠 Fair",
            _ => "🔴 Poor"
        };
    }

    private string TruncateSql(string sql, int maxLength = 80)
    {
        if (sql.Length <= maxLength)
            return sql;

        return sql.Substring(0, maxLength - 3) + "...";
    }
}
