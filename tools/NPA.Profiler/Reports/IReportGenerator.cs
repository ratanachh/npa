using NPA.Profiler.Analysis;

namespace NPA.Profiler.Reports;

/// <summary>
/// Interface for generating performance reports in different formats.
/// </summary>
public interface IReportGenerator
{
    Task<string> GenerateAsync(AnalysisReport report);
}
