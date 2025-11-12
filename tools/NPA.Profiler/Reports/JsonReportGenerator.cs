using System.Text.Json;
using NPA.Profiler.Analysis;

namespace NPA.Profiler.Reports;

/// <summary>
/// Generates performance reports in JSON format.
/// </summary>
public class JsonReportGenerator : IReportGenerator
{
    public Task<string> GenerateAsync(AnalysisReport report)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(report, options);
        return Task.FromResult(json);
    }
}
