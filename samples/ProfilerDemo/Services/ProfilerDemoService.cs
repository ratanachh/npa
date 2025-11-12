using Bogus;
using Microsoft.Extensions.Logging;
using NPA.Core.Core;
using NPA.Monitoring;
using ProfilerDemo.Entities;
using ProfilerDemo.Repositories;
using System.Diagnostics;

namespace ProfilerDemo.Services;

/// <summary>
/// Service that orchestrates the profiler demo with 3 phases:
/// 1. Data Generation (10K records using Faker)
/// 2. Performance Testing (8 different scenarios)
/// 3. Performance Reporting (comprehensive analysis)
/// </summary>
public class ProfilerDemoService
{
    private const int TOTAL_RECORDS = 10_000;
    private const int BATCH_SIZE = 1_000;

    private readonly IEntityManager _entityManager;
    private readonly IUserRepository _userRepository;
    private readonly PerformanceMonitor _monitor;
    private readonly ILogger<ProfilerDemoService> _logger;

    public ProfilerDemoService(
        IEntityManager entityManager,
        IUserRepository userRepository,
        PerformanceMonitor monitor,
        ILogger<ProfilerDemoService> logger)
    {
        _entityManager = entityManager;
        _userRepository = userRepository;
        _monitor = monitor;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("\n=== Phase 1: Data Generation ===\n");
        await GenerateDataAsync();

        Console.WriteLine("\n=== Phase 2: Performance Testing ===\n");
        await RunPerformanceTestsAsync();

        Console.WriteLine("\n=== Phase 3: Performance Report ===\n");
        DisplayPerformanceReport();
    }

    private async Task GenerateDataAsync()
    {
        _logger.LogInformation($"Generating {TOTAL_RECORDS:N0} realistic user records using Faker...");

        var faker = new Faker<User>()
            .RuleFor(u => u.Username, f => f.Internet.UserName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.Age, f => f.Random.Int(18, 80))
            .RuleFor(u => u.Country, f => f.Address.Country())
            .RuleFor(u => u.City, f => f.Address.City())
            .RuleFor(u => u.CreatedAt, f => f.Date.Past(2))
            .RuleFor(u => u.LastLogin, f => f.Random.Bool(0.8f) ? f.Date.Recent(30) : null)
            .RuleFor(u => u.IsActive, f => f.Random.Bool(0.85f))
            .RuleFor(u => u.AccountBalance, f => f.Finance.Amount(0, 10000));

        var totalInserted = 0;
        var overallSw = Stopwatch.StartNew();

        while (totalInserted < TOTAL_RECORDS)
        {
            var users = faker.Generate(BATCH_SIZE);

            // Bulk insert using EntityManager - this will be profiled by PerformanceMonitor
            var batchSw = Stopwatch.StartNew();
            
            foreach (var user in users)
            {
                await _entityManager.PersistAsync(user);
            }
            
            batchSw.Stop();
            _monitor.RecordMetric("BULK_INSERT_BATCH", batchSw.Elapsed, users.Count);

            totalInserted += users.Count;

            if (totalInserted % 1_000 == 0)
            {
                Console.WriteLine($"  Progress: {totalInserted:N0} / {TOTAL_RECORDS:N0} records ({(double)totalInserted / TOTAL_RECORDS:P1})");
            }
        }

        overallSw.Stop();
        _monitor.RecordMetric("BULK_INSERT_TOTAL", overallSw.Elapsed, TOTAL_RECORDS);

        var recordsPerSec = TOTAL_RECORDS / overallSw.Elapsed.TotalSeconds;
        Console.WriteLine($"✓ Generated {TOTAL_RECORDS:N0} records in {overallSw.Elapsed.TotalSeconds:F2}s ({recordsPerSec:F0} records/sec)\n");
    }

    private async Task RunPerformanceTestsAsync()
    {
        // Test 1: Indexed Queries
        await TestIndexedQueriesAsync();

        // Test 2: N+1 Problem
        await TestN1ProblemAsync();

        // Test 3: Optimized Batch Query
        await TestOptimizedBatchQueryAsync();

        // Test 4: Full Table Scan
        await TestFullTableScanAsync();

        // Test 5: Aggregate Queries
        await TestAggregateQueriesAsync();

        // Test 6: Pagination
        await TestPaginationPerformanceAsync();

        // Test 7: Bulk Operations
        await TestBulkOperationsAsync();
    }

    private async Task TestIndexedQueriesAsync()
    {
        Console.WriteLine("1. Testing Indexed Queries");

        var sw = Stopwatch.StartNew();
        var userByEmail = await _userRepository.FindByEmailAsync("test@example.com");
        sw.Stop();
        _monitor.RecordMetric("SELECT_INDEXED", sw.Elapsed, 1);
        Console.WriteLine($"   Email lookup (indexed): {sw.Elapsed.TotalMilliseconds:F2}ms");

        sw.Restart();
        var userByUsername = await _userRepository.FindByUsernameAsync("testuser");
        sw.Stop();
        _monitor.RecordMetric("SELECT_INDEXED", sw.Elapsed, 1);
        Console.WriteLine($"   Username lookup (indexed): {sw.Elapsed.TotalMilliseconds:F2}ms\n");
    }

    private async Task TestN1ProblemAsync()
    {
        Console.WriteLine("2. Testing N+1 Problem (BAD)");

        var ids = Enumerable.Range(1, 100).ToArray();
        var sw = Stopwatch.StartNew();

        foreach (var id in ids)
        {
            var user = await _userRepository.GetByIdAsync(id);
        }

        sw.Stop();
        _monitor.RecordMetric("SELECT_N1", sw.Elapsed, 100);
        Console.WriteLine($"   100 individual queries: {sw.Elapsed.TotalMilliseconds:F2}ms ({sw.Elapsed.TotalMilliseconds / 100:F2}ms per query)\n");
    }

    private async Task TestOptimizedBatchQueryAsync()
    {
        Console.WriteLine("3. Testing Optimized Batch Query (GOOD)");

        var ids = Enumerable.Range(1, 100).ToArray();
        var sw = Stopwatch.StartNew();

        var users = await _userRepository.FindByIdsAsync(ids);

        sw.Stop();
        _monitor.RecordMetric("SELECT_BATCH", sw.Elapsed, 1);

        var n1Stats = _monitor.GetStats("SELECT_N1");
        var batchStats = _monitor.GetStats("SELECT_BATCH");
        var improvement = n1Stats.AverageDuration.TotalMilliseconds / batchStats.AverageDuration.TotalMilliseconds;

        Console.WriteLine($"   Single batch query for 100 users: {sw.Elapsed.TotalMilliseconds:F2}ms");
        Console.WriteLine($"   ✓ {improvement:F0}x faster than N+1!\n");
    }

    private async Task TestFullTableScanAsync()
    {
        Console.WriteLine("4. Testing Full Table Scan (SLOW)");

        var sw = Stopwatch.StartNew();
        var activeUsers = await _userRepository.FindActiveUsersAsync(true);
        var count = activeUsers.Count();
        sw.Stop();
        _monitor.RecordMetric("SELECT_FULL_SCAN", sw.Elapsed, count);

        Console.WriteLine($"   ⚠️  Full table scan: {sw.Elapsed.TotalMilliseconds:F2}ms ({count:N0} records)\n");
    }

    private async Task TestAggregateQueriesAsync()
    {
        Console.WriteLine("5. Testing Aggregate Queries");

        var sw = Stopwatch.StartNew();
        var statistics = await _userRepository.GetUserStatisticsByCountryAsync();
        var count = statistics.Count();
        sw.Stop();
        _monitor.RecordMetric("SELECT_AGGREGATE", sw.Elapsed, count);

        Console.WriteLine($"   Aggregation by country: {sw.Elapsed.TotalMilliseconds:F2}ms ({count} countries)\n");
    }

    private async Task TestPaginationPerformanceAsync()
    {
        Console.WriteLine("6. Testing Pagination Performance");

        // Page 1 (early pagination - fast)
        var sw = Stopwatch.StartNew();
        var page1 = await _userRepository.GetUsersPageAsync(0, 50);
        sw.Stop();
        _monitor.RecordMetric("SELECT_PAGINATION_EARLY", sw.Elapsed, 50);
        var page1Time = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"   Page 1 (first 50): {page1Time:F2}ms");

        // Page 100 (deep pagination - slower)
        sw.Restart();
        var page100 = await _userRepository.GetUsersPageAsync(5_000, 50);
        sw.Stop();
        _monitor.RecordMetric("SELECT_PAGINATION_DEEP", sw.Elapsed, 50);
        var page100Time = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"   Page 100 (offset 5k): {page100Time:F2}ms\n");
    }

    private async Task TestBulkOperationsAsync()
    {
        Console.WriteLine("7. Testing Bulk Operations");

        // Bulk update using repository method with generated UPDATE query
        var sw = Stopwatch.StartNew();
        var updateCount = await _userRepository.BulkUpdateAccountBalanceAsync("United States", 100m);
        sw.Stop();
        _monitor.RecordMetric("BULK_UPDATE", sw.Elapsed, updateCount);
        Console.WriteLine($"   Bulk update ({updateCount:N0} records): {sw.Elapsed.TotalMilliseconds:F2}ms");

        // Bulk delete using repository method with generated DELETE query
        var cutoffDate = DateTime.UtcNow.AddYears(-3);
        sw.Restart();
        var deleteCount = await _userRepository.DeleteInactiveUsersOlderThanAsync(cutoffDate);
        sw.Stop();
        _monitor.RecordMetric("BULK_DELETE", sw.Elapsed, deleteCount);
        Console.WriteLine($"   Bulk delete ({deleteCount:N0} records): {sw.Elapsed.TotalMilliseconds:F2}ms\n");
    }

    private void DisplayPerformanceReport()
    {
        Console.WriteLine("======================================================================");
        Console.WriteLine("PERFORMANCE ANALYSIS REPORT");
        Console.WriteLine("======================================================================\n");

        var metrics = new[]
        {
            "BULK_INSERT_BATCH",
            "BULK_INSERT_TOTAL",
            "SELECT_INDEXED",
            "SELECT_N1",
            "SELECT_BATCH",
            "SELECT_FULL_SCAN",
            "SELECT_AGGREGATE",
            "SELECT_PAGINATION_EARLY",
            "SELECT_PAGINATION_DEEP",
            "BULK_UPDATE",
            "BULK_DELETE"
        };

        foreach (var metric in metrics)
        {
            var stats = _monitor.GetStats(metric);
            if (stats.TotalOperations > 0)
            {
                Console.WriteLine($"{metric}:");
                Console.WriteLine($"  Operations: {stats.TotalOperations}");
                Console.WriteLine($"  Avg: {stats.AverageDuration.TotalMilliseconds:F2}ms");
                Console.WriteLine($"  Min: {stats.MinDuration.TotalMilliseconds:F2}ms");
                Console.WriteLine($"  Max: {stats.MaxDuration.TotalMilliseconds:F2}ms");
                Console.WriteLine();
            }
        }

        Console.WriteLine("======================================================================");
        Console.WriteLine("KEY INSIGHTS");
        Console.WriteLine("======================================================================\n");

        // Index performance
        var indexedStats = _monitor.GetStats("SELECT_INDEXED");
        var fullScanStats = _monitor.GetStats("SELECT_FULL_SCAN");
        if (indexedStats.TotalOperations > 0 && fullScanStats.TotalOperations > 0)
        {
            var indexAdvantage = fullScanStats.AverageDuration.TotalMilliseconds / indexedStats.AverageDuration.TotalMilliseconds;
            Console.WriteLine($"✓ Index Performance:");
            Console.WriteLine($"  Indexed queries: {indexedStats.AverageDuration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  Full scan: {fullScanStats.AverageDuration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  → Indexes are {indexAdvantage:F0}x faster!\n");
        }

        // N+1 vs Batch
        var n1Stats = _monitor.GetStats("SELECT_N1");
        var batchStats = _monitor.GetStats("SELECT_BATCH");
        if (n1Stats.TotalOperations > 0 && batchStats.TotalOperations > 0)
        {
            var totalN1 = n1Stats.AverageDuration.TotalMilliseconds * n1Stats.TotalOperations;
            var totalBatch = batchStats.AverageDuration.TotalMilliseconds;
            var improvement = totalN1 / totalBatch;

            Console.WriteLine($"✓ N+1 vs Batch Queries:");
            Console.WriteLine($"  N+1 approach (100 queries): {totalN1:F2}ms");
            Console.WriteLine($"  Batch approach (1 query): {totalBatch:F2}ms");
            Console.WriteLine($"  → Batch queries are {improvement:F0}x faster!\n");
        }

        // Pagination degradation
        var earlyPageStats = _monitor.GetStats("SELECT_PAGINATION_EARLY");
        var deepPageStats = _monitor.GetStats("SELECT_PAGINATION_DEEP");
        if (earlyPageStats.TotalOperations > 0 && deepPageStats.TotalOperations > 0)
        {
            var degradation = deepPageStats.AverageDuration.TotalMilliseconds / earlyPageStats.AverageDuration.TotalMilliseconds;
            Console.WriteLine($"✓ Pagination:");
            Console.WriteLine($"  Early pages: {earlyPageStats.AverageDuration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  Deep pagination (5k offset): {deepPageStats.AverageDuration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  → Use keyset pagination for better performance!\n");
        }

        Console.WriteLine("======================================================================");
    }
}
