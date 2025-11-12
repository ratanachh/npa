using System.Text;
using NPA.Profiler.Analysis;

namespace NPA.Profiler.Reports;

/// <summary>
/// Generates performance reports in CSV format for spreadsheet analysis.
/// </summary>
public class CsvReportGenerator : IReportGenerator
{
    public Task<string> GenerateAsync(AnalysisReport report)
    {
        var sb = new StringBuilder();

        // Summary Section
        sb.AppendLine("PERFORMANCE SUMMARY");
        sb.AppendLine("Metric,Value");
        sb.AppendLine($"Performance Score,{report.PerformanceScore}");
        sb.AppendLine($"Session Duration (s),{report.Duration.TotalSeconds:F2}");
        sb.AppendLine($"Total Queries,{report.Statistics.TotalQueries}");
        sb.AppendLine($"Total Duration (ms),{report.Statistics.TotalDuration:F2}");
        sb.AppendLine($"Average Duration (ms),{report.Statistics.AverageDuration:F2}");
        sb.AppendLine($"Min Duration (ms),{report.Statistics.MinDuration:F2}");
        sb.AppendLine($"Max Duration (ms),{report.Statistics.MaxDuration:F2}");
        sb.AppendLine($"P95 Duration (ms),{report.Statistics.P95Duration:F2}");
        sb.AppendLine($"P99 Duration (ms),{report.Statistics.P99Duration:F2}");
        sb.AppendLine($"Cache Hit Rate,{report.Statistics.CacheHitRate:P2}");
        sb.AppendLine($"Slow Queries Count,{report.Statistics.SlowQueries.Count}");
        sb.AppendLine();

        // N+1 Issues
        if (report.NPlusOneIssues.Any())
        {
            sb.AppendLine("N+1 QUERY ISSUES");
            sb.AppendLine("Entity Type,Occurrences,Total Duration (ms),Time Window (s),SQL Pattern");
            foreach (var issue in report.NPlusOneIssues)
            {
                sb.AppendLine($"\"{issue.EntityType ?? "Unknown"}\",{issue.Occurrences},{issue.TotalDuration:F2},{(issue.LastOccurrence - issue.FirstOccurrence).TotalSeconds:F2},\"{EscapeCsv(issue.SqlPattern)}\"");
            }
            sb.AppendLine();
        }

        // Missing Indexes
        if (report.MissingIndexes.Any())
        {
            sb.AppendLine("MISSING INDEX ISSUES");
            sb.AppendLine("Entity Type,Duration (ms),Suggested Columns,SQL");
            foreach (var issue in report.MissingIndexes)
            {
                sb.AppendLine($"\"{issue.EntityType ?? "Unknown"}\",{issue.Duration:F2},\"{string.Join(", ", issue.SuggestedColumns)}\",\"{EscapeCsv(issue.Sql)}\"");
            }
            sb.AppendLine();
        }

        // Large Result Sets
        if (report.LargeResultSets.Any())
        {
            sb.AppendLine("LARGE RESULT SET ISSUES");
            sb.AppendLine("Entity Type,Row Count,Duration (ms),SQL");
            foreach (var issue in report.LargeResultSets)
            {
                sb.AppendLine($"\"{issue.EntityType ?? "Unknown"}\",{issue.RowCount},{issue.Duration:F2},\"{EscapeCsv(issue.Sql)}\"");
            }
            sb.AppendLine();
        }

        // Slow Queries
        if (report.SlowQueries.Any())
        {
            sb.AppendLine("SLOW QUERIES");
            sb.AppendLine("Timestamp,Duration (ms),Query Type,Entity Type,SQL");
            foreach (var issue in report.SlowQueries)
            {
                sb.AppendLine($"{issue.Timestamp:yyyy-MM-dd HH:mm:ss},{issue.Duration:F2},{issue.QueryType},\"{issue.EntityType ?? "Unknown"}\",\"{EscapeCsv(issue.Sql)}\"");
            }
            sb.AppendLine();
        }

        // Optimization Suggestions
        if (report.Suggestions.Any())
        {
            sb.AppendLine("OPTIMIZATION SUGGESTIONS");
            sb.AppendLine("Priority,Category,Title,Description,Impact");
            foreach (var suggestion in report.Suggestions)
            {
                sb.AppendLine($"{suggestion.Priority},\"{suggestion.Category}\",\"{EscapeCsv(suggestion.Title)}\",\"{EscapeCsv(suggestion.Description)}\",\"{EscapeCsv(suggestion.Impact)}\"");
            }
            sb.AppendLine();
        }

        return Task.FromResult(sb.ToString());
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Replace double quotes with double double quotes
        return value.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", "");
    }
}
