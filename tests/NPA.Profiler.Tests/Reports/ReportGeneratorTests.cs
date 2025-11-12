using FluentAssertions;
using NPA.Profiler.Analysis;
using NPA.Profiler.Profiling;
using NPA.Profiler.Reports;
using Xunit;

namespace NPA.Profiler.Tests.Reports;

public class ReportGeneratorTests
{
    private static AnalysisReport CreateSampleReport()
    {
        var session = new ProfilingSession();
        session.Start();
        
        session.AddQuery(new QueryProfile
        {
            Sql = "SELECT * FROM Users WHERE Id = 1",
            QueryType = QueryType.Select,
            Duration = TimeSpan.FromMilliseconds(50),
            RowsAffected = 1
        });

        session.AddQuery(new QueryProfile
        {
            Sql = "SELECT * FROM Orders WHERE UserId = 1",
            QueryType = QueryType.Select,
            Duration = TimeSpan.FromMilliseconds(150),
            RowsAffected = 100
        });

        session.Stop();

        var analyzer = new PerformanceAnalyzer();
        return analyzer.Analyze(session);
    }

    [Fact]
    public async Task ConsoleReportGenerator_ShouldGenerateReport()
    {
        // Arrange
        var report = CreateSampleReport();
        var generator = new ConsoleReportGenerator();

        // Act
        var result = await generator.GenerateAsync(report);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("NPA PERFORMANCE ANALYSIS REPORT");
        result.Should().Contain("Performance Score");
        result.Should().Contain("QUERY STATISTICS");
    }

    [Fact]
    public async Task ConsoleReportGenerator_ShouldIncludeStatistics()
    {
        // Arrange
        var report = CreateSampleReport();
        var generator = new ConsoleReportGenerator();

        // Act
        var result = await generator.GenerateAsync(report);

        // Assert
        result.Should().Contain("Total Queries:");
        result.Should().Contain("Total Duration:");
        result.Should().Contain("Average Duration:");
        result.Should().Contain("P95 Duration:");
    }

    [Fact]
    public async Task JsonReportGenerator_ShouldGenerateValidJson()
    {
        // Arrange
        var report = CreateSampleReport();
        var generator = new JsonReportGenerator();

        // Act
        var result = await generator.GenerateAsync(report);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("\"sessionId\":");
        result.Should().Contain("\"performanceScore\":");
        result.Should().Contain("\"statistics\":");
    }

    [Fact]
    public async Task HtmlReportGenerator_ShouldGenerateValidHtml()
    {
        // Arrange
        var report = CreateSampleReport();
        var generator = new HtmlReportGenerator();

        // Act
        var result = await generator.GenerateAsync(report);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("<!DOCTYPE html>");
        result.Should().Contain("<html");
        result.Should().Contain("NPA Performance Analysis Report");
        result.Should().Contain("Performance Score");
        result.Should().Contain("</html>");
    }

    [Fact]
    public async Task HtmlReportGenerator_ShouldIncludeStyles()
    {
        // Arrange
        var report = CreateSampleReport();
        var generator = new HtmlReportGenerator();

        // Act
        var result = await generator.GenerateAsync(report);

        // Assert
        result.Should().Contain("<style>");
        result.Should().Contain("</style>");
    }

    [Fact]
    public async Task CsvReportGenerator_ShouldGenerateValidCsv()
    {
        // Arrange
        var report = CreateSampleReport();
        var generator = new CsvReportGenerator();

        // Act
        var result = await generator.GenerateAsync(report);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("PERFORMANCE SUMMARY");
        result.Should().Contain("Metric,Value");
        result.Should().Contain("Performance Score,");
    }

    [Fact]
    public async Task CsvReportGenerator_ShouldEscapeSpecialCharacters()
    {
        // Arrange
        var report = CreateSampleReport();
        report.Suggestions.Add(new OptimizationSuggestion
        {
            Title = "Test \"quoted\" text",
            Description = "Line 1\nLine 2",
            Category = "Test",
            Priority = Priority.High,
            Impact = "High impact",
            Example = "Sample code"
        });

        var generator = new CsvReportGenerator();

        // Act
        var result = await generator.GenerateAsync(report);

        // Assert
        result.Should().Contain("\"\"quoted\"\""); // CSV-escaped quotes
        // Newlines are removed/replaced with spaces in CSV escaping
    }

    [Fact]
    public async Task AllGenerators_ShouldHandleEmptyReport()
    {
        // Arrange
        var session = new ProfilingSession();
        session.Start();
        session.Stop();
        var analyzer = new PerformanceAnalyzer();
        var report = analyzer.Analyze(session);

        var generators = new IReportGenerator[]
        {
            new ConsoleReportGenerator(),
            new JsonReportGenerator(),
            new HtmlReportGenerator(),
            new CsvReportGenerator()
        };

        // Act & Assert
        foreach (var generator in generators)
        {
            var result = await generator.GenerateAsync(report);
            result.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task HtmlReportGenerator_ShouldShowScoreClass()
    {
        // Arrange
        var report = CreateSampleReport();
        var generator = new HtmlReportGenerator();

        // Act
        var result = await generator.GenerateAsync(report);

        // Assert
        result.Should().Contain("score-card");
        // Should have one of the score classes
        result.Should().Match(r => 
            r.Contains("score-good") || 
            r.Contains("score-fair") || 
            r.Contains("score-poor"));
    }
}
