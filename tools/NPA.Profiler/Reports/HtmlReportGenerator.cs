using System.Text;
using NPA.Profiler.Analysis;

namespace NPA.Profiler.Reports;

/// <summary>
/// Generates performance reports in HTML format with charts.
/// </summary>
public class HtmlReportGenerator : IReportGenerator
{
    public Task<string> GenerateAsync(AnalysisReport report)
    {
        var html = GenerateHtml(report);
        return Task.FromResult(html);
    }

    private string GenerateHtml(AnalysisReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='en'>");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset='UTF-8'>");
        sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        sb.AppendLine("    <title>NPA Performance Analysis Report</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine(GetStyles());
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine("    <div class='container'>");
        sb.AppendLine("        <header>");
        sb.AppendLine("            <h1>NPA Performance Analysis Report</h1>");
        sb.AppendLine($"            <p>Session Duration: {report.Duration.TotalSeconds:F2}s | Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine("        </header>");

        // Score Card
        var scoreClass = report.PerformanceScore >= 70 ? "score-good" : report.PerformanceScore >= 50 ? "score-fair" : "score-poor";
        sb.AppendLine($"        <div class='score-card {scoreClass}'>");
        sb.AppendLine($"            <div class='score'>{report.PerformanceScore}</div>");
        sb.AppendLine("            <div class='score-label'>Performance Score</div>");
        sb.AppendLine("        </div>");

        // Statistics Section
        sb.AppendLine("        <section class='statistics'>");
        sb.AppendLine("            <h2>Query Statistics</h2>");
        sb.AppendLine("            <div class='stats-grid'>");
        sb.AppendLine($"                <div class='stat-box'><div class='stat-value'>{report.Statistics.TotalQueries}</div><div class='stat-label'>Total Queries</div></div>");
        sb.AppendLine($"                <div class='stat-box'><div class='stat-value'>{report.Statistics.TotalDuration:F2}ms</div><div class='stat-label'>Total Duration</div></div>");
        sb.AppendLine($"                <div class='stat-box'><div class='stat-value'>{report.Statistics.AverageDuration:F2}ms</div><div class='stat-label'>Avg Duration</div></div>");
        sb.AppendLine($"                <div class='stat-box'><div class='stat-value'>{report.Statistics.P95Duration:F2}ms</div><div class='stat-label'>P95 Duration</div></div>");
        sb.AppendLine($"                <div class='stat-box'><div class='stat-value'>{report.Statistics.CacheHitRate:P0}</div><div class='stat-label'>Cache Hit Rate</div></div>");
        sb.AppendLine($"                <div class='stat-box'><div class='stat-value'>{report.Statistics.SlowQueries.Count}</div><div class='stat-label'>Slow Queries</div></div>");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </section>");

        // Issues Sections
        if (report.NPlusOneIssues.Any())
        {
            sb.AppendLine("        <section class='issues'>");
            sb.AppendLine($"            <h2>N+1 Query Issues ({report.NPlusOneIssues.Count})</h2>");
            foreach (var issue in report.NPlusOneIssues.Take(10))
            {
                sb.AppendLine("            <div class='issue-card issue-high'>");
                sb.AppendLine($"                <h3>{issue.Occurrences} similar queries detected</h3>");
                sb.AppendLine($"                <p><strong>Entity:</strong> {issue.EntityType ?? "Unknown"}</p>");
                sb.AppendLine($"                <p><strong>Total Duration:</strong> {issue.TotalDuration:F2}ms</p>");
                sb.AppendLine($"                <p><strong>Time Window:</strong> {(issue.LastOccurrence - issue.FirstOccurrence).TotalSeconds:F2}s</p>");
                sb.AppendLine($"                <pre><code>{System.Web.HttpUtility.HtmlEncode(issue.SqlPattern)}</code></pre>");
                sb.AppendLine("            </div>");
            }
            sb.AppendLine("        </section>");
        }

        if (report.MissingIndexes.Any())
        {
            sb.AppendLine("        <section class='issues'>");
            sb.AppendLine($"            <h2>Missing Index Issues ({report.MissingIndexes.Count})</h2>");
            foreach (var issue in report.MissingIndexes.Take(10))
            {
                sb.AppendLine("            <div class='issue-card issue-medium'>");
                sb.AppendLine($"                <h3>Slow query on {issue.EntityType ?? "Unknown"}</h3>");
                sb.AppendLine($"                <p><strong>Duration:</strong> {issue.Duration:F2}ms</p>");
                if (issue.SuggestedColumns.Any())
                {
                    sb.AppendLine($"                <p><strong>Suggested Index Columns:</strong> {string.Join(", ", issue.SuggestedColumns)}</p>");
                }
                sb.AppendLine($"                <pre><code>{System.Web.HttpUtility.HtmlEncode(issue.Sql)}</code></pre>");
                sb.AppendLine("            </div>");
            }
            sb.AppendLine("        </section>");
        }

        if (report.LargeResultSets.Any())
        {
            sb.AppendLine("        <section class='issues'>");
            sb.AppendLine($"            <h2>Large Result Set Issues ({report.LargeResultSets.Count})</h2>");
            foreach (var issue in report.LargeResultSets.Take(10))
            {
                sb.AppendLine("            <div class='issue-card issue-medium'>");
                sb.AppendLine($"                <h3>{issue.RowCount} rows without pagination</h3>");
                sb.AppendLine($"                <p><strong>Entity:</strong> {issue.EntityType ?? "Unknown"}</p>");
                sb.AppendLine($"                <p><strong>Duration:</strong> {issue.Duration:F2}ms</p>");
                sb.AppendLine($"                <pre><code>{System.Web.HttpUtility.HtmlEncode(issue.Sql)}</code></pre>");
                sb.AppendLine("            </div>");
            }
            sb.AppendLine("        </section>");
        }

        // Optimization Suggestions
        if (report.Suggestions.Any())
        {
            sb.AppendLine("        <section class='suggestions'>");
            sb.AppendLine($"            <h2>Optimization Suggestions ({report.Suggestions.Count})</h2>");
            foreach (var suggestion in report.Suggestions.Take(15))
            {
                var priorityClass = suggestion.Priority switch
                {
                    Priority.High => "priority-high",
                    Priority.Medium => "priority-medium",
                    _ => "priority-low"
                };

                sb.AppendLine($"            <div class='suggestion-card {priorityClass}'>");
                sb.AppendLine($"                <div class='suggestion-header'>");
                sb.AppendLine($"                    <span class='category'>{suggestion.Category}</span>");
                sb.AppendLine($"                    <span class='priority'>{suggestion.Priority}</span>");
                sb.AppendLine($"                </div>");
                sb.AppendLine($"                <h3>{System.Web.HttpUtility.HtmlEncode(suggestion.Title)}</h3>");
                sb.AppendLine($"                <p>{System.Web.HttpUtility.HtmlEncode(suggestion.Description)}</p>");
                sb.AppendLine($"                <p class='impact'><strong>Impact:</strong> {System.Web.HttpUtility.HtmlEncode(suggestion.Impact)}</p>");
                if (!string.IsNullOrEmpty(suggestion.Example))
                {
                    sb.AppendLine($"                <pre><code>{System.Web.HttpUtility.HtmlEncode(suggestion.Example)}</code></pre>");
                }
                sb.AppendLine("            </div>");
            }
            sb.AppendLine("        </section>");
        }

        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private string GetStyles()
    {
        return @"
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background: #f5f7fa; color: #333; line-height: 1.6; }
        .container { max-width: 1200px; margin: 0 auto; padding: 20px; }
        header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px; border-radius: 10px; margin-bottom: 30px; text-align: center; }
        header h1 { font-size: 2.5em; margin-bottom: 10px; }
        .score-card { background: white; padding: 40px; border-radius: 10px; margin-bottom: 30px; text-align: center; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        .score { font-size: 5em; font-weight: bold; }
        .score-label { font-size: 1.2em; color: #666; margin-top: 10px; }
        .score-good .score { color: #10b981; }
        .score-fair .score { color: #f59e0b; }
        .score-poor .score { color: #ef4444; }
        section { background: white; padding: 30px; border-radius: 10px; margin-bottom: 30px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        section h2 { color: #667eea; margin-bottom: 20px; border-bottom: 2px solid #667eea; padding-bottom: 10px; }
        .stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 20px; }
        .stat-box { text-align: center; padding: 20px; background: #f8fafc; border-radius: 8px; }
        .stat-value { font-size: 2em; font-weight: bold; color: #667eea; }
        .stat-label { color: #666; margin-top: 5px; font-size: 0.9em; }
        .issue-card, .suggestion-card { background: #f8fafc; padding: 20px; border-radius: 8px; margin-bottom: 15px; border-left: 4px solid #667eea; }
        .issue-high { border-left-color: #ef4444; }
        .issue-medium { border-left-color: #f59e0b; }
        .issue-card h3 { color: #333; margin-bottom: 10px; }
        pre { background: #1e293b; color: #e2e8f0; padding: 15px; border-radius: 5px; overflow-x: auto; margin-top: 10px; }
        code { font-family: 'Courier New', monospace; font-size: 0.9em; }
        .suggestion-header { display: flex; justify-content: space-between; margin-bottom: 10px; }
        .category { background: #667eea; color: white; padding: 5px 10px; border-radius: 5px; font-size: 0.85em; }
        .priority { padding: 5px 10px; border-radius: 5px; font-size: 0.85em; font-weight: bold; }
        .priority-high .priority { background: #fee2e2; color: #dc2626; }
        .priority-medium .priority { background: #fef3c7; color: #d97706; }
        .priority-low .priority { background: #d1fae5; color: #059669; }
        .impact { color: #666; font-size: 0.9em; }
        ";
    }
}
