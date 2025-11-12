using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NPA.Monitoring;
using NPA.Profiler.Profiling;
using NPA.Profiler.Analysis;
using NPA.Profiler.Reports;

namespace NPA.Profiler;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("NPA Performance Profiler - Analyze and optimize database query performance")
        {
            CreateProfileCommand(),
            CreateAnalyzeCommand(),
            CreateReportCommand()
        };

        return await rootCommand.InvokeAsync(args);
    }

    static Command CreateProfileCommand()
    {
        var profileCommand = new Command("profile", "Start performance profiling")
        {
            new Option<string>("--connection", "Database connection string") { IsRequired = true },
            new Option<int>("--duration", () => 60, "Profiling duration in seconds"),
            new Option<string?>("--output", "Output file path for profiling data")
        };

        profileCommand.SetHandler(async (string connection, int duration, string? output) =>
        {
            var host = CreateHost();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var profiler = host.Services.GetRequiredService<NpaProfiler>();
            
            logger.LogInformation("Starting profiling session for {Duration} seconds", duration);
            
            var session = profiler.StartSession();
            
            // Wait for the specified duration
            await Task.Delay(TimeSpan.FromSeconds(duration));
            
            profiler.StopSession();
            
            logger.LogInformation("Profiling session completed. Captured {QueryCount} queries", session.TotalQueries);

            // Save session data if output specified
            if (!string.IsNullOrEmpty(output))
            {
                var json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(output, json);
                logger.LogInformation("Session data saved to {OutputPath}", output);
            }

            // Display quick summary
            var stats = session.GetStatistics();
            Console.WriteLine("\nProfile Summary:");
            Console.WriteLine($"  Total Queries: {stats.TotalQueries}");
            Console.WriteLine($"  Total Duration: {stats.TotalDuration:F2}ms");
            Console.WriteLine($"  Average Duration: {stats.AverageDuration:F2}ms");
            Console.WriteLine($"  P95 Duration: {stats.P95Duration:F2}ms");
            Console.WriteLine($"  Cache Hit Rate: {stats.CacheHitRate:P2}");
            Console.WriteLine($"  Slow Queries (>100ms): {stats.SlowQueries.Count}");
        }, 
        new Argument<string>("connection"), 
        new Argument<int>("duration"),
        new Argument<string?>("output"));

        return profileCommand;
    }

    static Command CreateAnalyzeCommand()
    {
        var analyzeCommand = new Command("analyze", "Analyze performance data")
        {
            new Option<string>("--data", "Performance data file") { IsRequired = true },
            new Option<string?>("--output", "Analysis output file"),
            new Option<string>("--format", () => "console", "Report format (console, html, json)")
        };

        analyzeCommand.SetHandler(async (string data, string? output, string format) =>
        {
            var host = CreateHost();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var analyzer = host.Services.GetRequiredService<PerformanceAnalyzer>();
            
            logger.LogInformation("Loading profiling data from {DataPath}", data);
            
            var json = await File.ReadAllTextAsync(data);
            var session = JsonSerializer.Deserialize<ProfilingSession>(json);
            
            if (session == null)
            {
                logger.LogError("Failed to load profiling session data");
                return;
            }

            logger.LogInformation("Analyzing session data...");
            var report = analyzer.Analyze(session);

            // Generate report based on format
            IReportGenerator reportGenerator = format.ToLowerInvariant() switch
            {
                "html" => new HtmlReportGenerator(),
                "json" => new JsonReportGenerator(),
                _ => new ConsoleReportGenerator()
            };

            var reportContent = await reportGenerator.GenerateAsync(report);

            if (!string.IsNullOrEmpty(output))
            {
                await File.WriteAllTextAsync(output, reportContent);
                logger.LogInformation("Analysis report saved to {OutputPath}", output);
            }
            else
            {
                Console.WriteLine(reportContent);
            }
        }, 
        new Argument<string>("data"), 
        new Argument<string?>("output"),
        new Argument<string>("format"));

        return analyzeCommand;
    }

    static Command CreateReportCommand()
    {
        var reportCommand = new Command("report", "Generate performance report")
        {
            new Option<string>("--data", "Performance data file") { IsRequired = true },
            new Option<string>("--format", () => "html", "Report format (html, json, csv, console)"),
            new Option<string?>("--output", "Output file path for report")
        };

        reportCommand.SetHandler(async (string data, string format, string? output) =>
        {
            var host = CreateHost();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var analyzer = host.Services.GetRequiredService<PerformanceAnalyzer>();
            
            logger.LogInformation("Loading profiling data from {DataPath}", data);
            
            var json = await File.ReadAllTextAsync(data);
            var session = JsonSerializer.Deserialize<ProfilingSession>(json);
            
            if (session == null)
            {
                logger.LogError("Failed to load profiling session data");
                return;
            }

            var report = analyzer.Analyze(session);

            IReportGenerator reportGenerator = format.ToLowerInvariant() switch
            {
                "html" => new HtmlReportGenerator(),
                "json" => new JsonReportGenerator(),
                "csv" => new CsvReportGenerator(),
                _ => new ConsoleReportGenerator()
            };

            var reportContent = await reportGenerator.GenerateAsync(report);

            if (!string.IsNullOrEmpty(output))
            {
                await File.WriteAllTextAsync(output, reportContent);
                logger.LogInformation("Report saved to {OutputPath}", output);
            }
            else
            {
                Console.WriteLine(reportContent);
            }
        }, 
        new Argument<string>("data"), 
        new Argument<string>("format"),
        new Argument<string?>("output"));

        return reportCommand;
    }

    static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddSingleton<PerformanceMonitor>();
                services.AddSingleton<NpaProfiler>();
                services.AddSingleton<PerformanceAnalyzer>();
            })
            .Build();
    }
}