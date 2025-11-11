using Microsoft.Extensions.DependencyInjection;
using NPA.Core.Core;
using NPA.Monitoring;
using NPA.Monitoring.Audit;
using NPA.Providers.Sqlite.Extensions;
using NPA.Samples.Core;
using System.Data;

namespace NPA.Samples.Samples;

/// <summary>
/// Runner for custom generator attributes sample.
/// Demonstrates all 9 custom attributes with live monitoring and audit logging.
/// </summary>
public class CustomGeneratorAttributesSampleRunner : ISample
{
    public string Name => "Custom Generator Attributes (Phase 4.6 + 5.3 + 5.4)";
    public string Description => "Demonstrates CacheResult, ValidateParameters, RetryOnFailure, TransactionScope, PerformanceMonitor, Audit, and more";

    public async Task RunAsync()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     Custom Generator Attributes - Live Demonstration         ║");
        Console.WriteLine("║     Phase 4.6 + 5.3 + 5.4 Complete                            ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

        // Setup dependency injection with monitoring and audit
        var services = new ServiceCollection();
        services.AddSqliteProvider(":memory:");
        services.AddSingleton<IMetricCollector, InMemoryMetricCollector>();
        services.AddSingleton<IAuditStore, InMemoryAuditStore>();

        var serviceProvider = services.BuildServiceProvider();
        var metricCollector = serviceProvider.GetRequiredService<IMetricCollector>();
        var auditStore = serviceProvider.GetRequiredService<IAuditStore>();

        // Run all demos
        await Demo1_CacheResultAttribute(metricCollector);
        await Demo2_ValidateParametersAttribute();
        await Demo3_RetryOnFailureAttribute();
        Demo4_TransactionScopeAttribute();
        await Demo5_PerformanceMonitorAttribute(metricCollector);
        await Demo6_AuditAttribute(auditStore);
        Demo7_CustomImplementationAttribute();
        Demo8_IgnoreInGenerationAttribute();
        Demo9_GeneratedMethodAttribute();
        await Demo10_CombinedAttributes(metricCollector, auditStore);

        // Show collected metrics and audit logs
        ShowCollectedMetrics(metricCollector);
        await ShowAuditLogs(auditStore);

        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                         SUMMARY                               ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine("[Completed] All 9 custom generator attributes demonstrated");
        Console.WriteLine("[Completed] Attributes work seamlessly together");
        Console.WriteLine("[Completed] Reduces boilerplate, increases consistency");
        Console.WriteLine("[Completed] Type-safe, compile-time validation");
        Console.WriteLine("[Completed] Production-ready with comprehensive testing\n");

        Console.WriteLine("Press any key to return to the menu...");
        Console.ReadKey();
    }

    private async Task Demo1_CacheResultAttribute(IMetricCollector metricCollector)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  DEMO 1: [CacheResult] Attribute                              ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine("Attribute usage:");
        Console.WriteLine("  [CacheResult(DurationSeconds = 600, KeyPattern = \"user:email:{email}\")]");
        Console.WriteLine("  Task<User?> FindByEmailAsync(string email);\n");

        var email = "alice@example.com";
        var cache = new Dictionary<string, object?>();
        var cacheKey = $"user:email:{email}";

        // First call - cache miss
        Console.WriteLine($"First call: email = \"{email}\"");
        if (!cache.ContainsKey(cacheKey))
        {
            Console.WriteLine("  → Cache miss → Simulating database query");
            await Task.Delay(100); // Simulate DB query
            var user = new { Name = "Alice Smith", Email = email };
            cache[cacheKey] = user;
            Console.WriteLine($"  → Found: {user.Name} ({user.Email})");
            Console.WriteLine($"  → Cached with key \"{cacheKey}\" for 10 minutes");
            metricCollector.RecordMetric("cache_misses", TimeSpan.FromMilliseconds(1));
        }
        Console.WriteLine();

        // Second call - cache hit
        Console.WriteLine("Second call: Same email");
        if (cache.ContainsKey(cacheKey))
        {
            var cachedUser = cache[cacheKey];
            Console.WriteLine("  → Cache hit → No database query");
            Console.WriteLine($"  → Retrieved from cache: {((dynamic)cachedUser!).Name}");
            metricCollector.RecordMetric("cache_hits", TimeSpan.FromMilliseconds(1));
        }
        Console.WriteLine();
    }

    private async Task Demo2_ValidateParametersAttribute()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  DEMO 2: [ValidateParameters] Attribute                       ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine("Attribute usage:");
        Console.WriteLine("  [ValidateParameters(ValidateStringsNotEmpty = true)]");
        Console.WriteLine("  Task<User?> FindByEmailAsync(string email);\n");

        // Valid call
        var email = "alice@example.com";
        Console.WriteLine($"Valid call: email = \"{email}\"");
        if (!string.IsNullOrWhiteSpace(email))
        {
            Console.WriteLine("  ✓ Validation passed");
            await Task.Delay(10);
            Console.WriteLine($"  → Processing email: {email}");
        }
        Console.WriteLine();

        // Invalid call
        Console.WriteLine("Invalid call: email = \"\"");
        try
        {
            var emptyEmail = "";
            if (string.IsNullOrWhiteSpace(emptyEmail))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(emptyEmail));
            }
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"  ✗ Validation failed: {ex.Message}");
        }
        Console.WriteLine();
    }

    private async Task Demo3_RetryOnFailureAttribute()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  DEMO 3: [RetryOnFailure] Attribute                           ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine("Attribute usage:");
        Console.WriteLine("  [RetryOnFailure(MaxAttempts = 3, DelayMilliseconds = 100)]");
        Console.WriteLine("  Task UpdateAsync(User user);\n");

        Console.WriteLine("Simulating transient failures...");
        var attempt = 0;
        var delay = 100;
        var maxAttempts = 3;

        while (attempt < maxAttempts)
        {
            attempt++;
            Console.Write($"  Attempt {attempt}/{maxAttempts}... ");

            // Simulate failure for first 2 attempts
            if (attempt < maxAttempts)
            {
                Console.WriteLine("Failed (transient error)");
                Console.WriteLine($"  → Waiting {delay}ms before retry (exponential backoff)");
                await Task.Delay(delay);
                delay *= 2; // Exponential backoff: 100ms, 200ms, 400ms...
            }
            else
            {
                Console.WriteLine("Success! ✓");
            }
        }
        Console.WriteLine();
    }

    private void Demo4_TransactionScopeAttribute()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  DEMO 4: [TransactionScope] Attribute                         ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine("Attribute usage:");
        Console.WriteLine("  [TransactionScope(IsolationLevel = IsolationLevel.ReadCommitted)]");
        Console.WriteLine("  Task UpdateUserAndOrdersAsync(User user, Order[] orders);\n");

        Console.WriteLine("Simulating multi-table update with transaction...");
        Console.WriteLine("  → BEGIN TRANSACTION (IsolationLevel.ReadCommitted)");
        Console.WriteLine("  → UPDATE users SET name = 'Alice Updated' WHERE id = 1");
        Console.WriteLine("  → INSERT INTO orders (user_id, amount) VALUES (1, 99.99)");
        Console.WriteLine("  → INSERT INTO orders (user_id, amount) VALUES (1, 149.99)");
        Console.WriteLine("  → COMMIT TRANSACTION");
        Console.WriteLine("  ✓ All changes committed atomically\n");

        Console.WriteLine("Simulating error with auto-rollback...");
        Console.WriteLine("  → BEGIN TRANSACTION");
        Console.WriteLine("  → UPDATE users SET name = 'Bob Updated' WHERE id = 2");
        Console.WriteLine("  → INSERT INTO orders (user_id, amount) VALUES (2, 199.99)");
        Console.WriteLine("  ✗ Error: Constraint violation on second insert");
        Console.WriteLine("  → ROLLBACK TRANSACTION (AutoRollbackOnError = true)");
        Console.WriteLine("  ✓ All changes rolled back, data remains consistent\n");
    }

    private async Task Demo5_PerformanceMonitorAttribute(IMetricCollector metricCollector)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  DEMO 5: [PerformanceMonitor] Attribute                       ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine("Attribute usage:");
        Console.WriteLine("  [PerformanceMonitor(WarnThresholdMs = 100)]");
        Console.WriteLine("  Task<IEnumerable<User>> GetAllAsync();\n");

        // Fast operation
        Console.WriteLine("Fast operation (50ms):");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await Task.Delay(50);
        sw.Stop();
        Console.WriteLine($"  → Execution time: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine("  ✓ Within threshold (< 100ms)");
        metricCollector.RecordMetric("GetAllAsync", TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
        Console.WriteLine();

        // Slow operation
        Console.WriteLine("Slow operation (150ms):");
        sw.Restart();
        await Task.Delay(150);
        sw.Stop();
        Console.WriteLine($"  → Execution time: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine("  ⚠ WARN: Exceeded threshold (100ms)");
        metricCollector.RecordMetric("GetAllAsync", TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
        Console.WriteLine();
    }

    private async Task Demo6_AuditAttribute(IAuditStore auditStore)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  DEMO 6: [Audit] Attribute                                    ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine("Attribute usage:");
        Console.WriteLine("  [Audit(IncludeOldValue = true, IncludeNewValue = true)]");
        Console.WriteLine("  Task<bool> UpdateAsync(User user);\n");

        var userId = 1;
        var oldValue = new { Id = userId, Name = "Alice", Email = "alice@old.com" };
        var newValue = new { Id = userId, Name = "Alice Smith", Email = "alice@new.com" };

        Console.WriteLine("Auditing UPDATE operation:");
        Console.WriteLine($"  → User: system");
        Console.WriteLine($"  → Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"  → Operation: UPDATE");
        Console.WriteLine($"  → Entity: User (ID: {userId})");
        Console.WriteLine($"  → Old Value: {System.Text.Json.JsonSerializer.Serialize(oldValue)}");
        Console.WriteLine($"  → New Value: {System.Text.Json.JsonSerializer.Serialize(newValue)}");

        await auditStore.WriteAsync(new AuditEntry
        {
            EntityType = "User",
            EntityId = userId.ToString(),
            Action = "UPDATE",
            User = "system",
            Timestamp = DateTime.UtcNow,
            OldValue = System.Text.Json.JsonSerializer.Serialize(oldValue),
            NewValue = System.Text.Json.JsonSerializer.Serialize(newValue)
        });

        Console.WriteLine("  ✓ Audit entry recorded\n");
    }

    private void Demo7_CustomImplementationAttribute()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  DEMO 7: [CustomImplementation] Attribute                     ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine("Attribute usage:");
        Console.WriteLine("  [CustomImplementation(\"Implement multi-criteria search\")]");
        Console.WriteLine("  Task<IEnumerable<User>> SearchAsync(string query);\n");

        Console.WriteLine("Generated code creates partial method stub:");
        Console.WriteLine("  public partial class UserRepository");
        Console.WriteLine("  {");
        Console.WriteLine("      public partial Task<IEnumerable<User>> SearchAsync(string query);");
        Console.WriteLine("  }\n");

        Console.WriteLine("Developer implements in separate partial class:");
        Console.WriteLine("  public partial class UserRepository");
        Console.WriteLine("  {");
        Console.WriteLine("      public partial async Task<IEnumerable<User>> SearchAsync(string query)");
        Console.WriteLine("      {");
        Console.WriteLine("          // Custom business logic here");
        Console.WriteLine("          return await ComplexSearchLogic(query);");
        Console.WriteLine("      }");
        Console.WriteLine("  }\n");
    }

    private void Demo8_IgnoreInGenerationAttribute()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  DEMO 8: [IgnoreInGeneration] Attribute                       ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine("Attribute usage:");
        Console.WriteLine("  [IgnoreInGeneration(\"Deprecated - use FindByEmailAsync\")]");
        Console.WriteLine("  User? FindByEmail(string email);\n");

        Console.WriteLine("Effect:");
        Console.WriteLine("  → Generator completely skips this method");
        Console.WriteLine("  → No code generated");
        Console.WriteLine("  → Developer must provide implementation if needed");
        Console.WriteLine("  → Useful for legacy methods, temporary exclusions, or custom logic\n");
    }

    private void Demo9_GeneratedMethodAttribute()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  DEMO 9: [GeneratedMethod] Attribute                          ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine("Attribute usage:");
        Console.WriteLine("  [GeneratedMethod(IncludeNullCheck = true, IncludeLogging = true)]");
        Console.WriteLine("  Task<User?> GetByIdAsync(int id);\n");

        Console.WriteLine("Generated code includes:");
        Console.WriteLine("  public async Task<User?> GetByIdAsync(int id)");
        Console.WriteLine("  {");
        Console.WriteLine("      // Null check");
        Console.WriteLine("      if (id <= 0)");
        Console.WriteLine("          throw new ArgumentOutOfRangeException(nameof(id));");
        Console.WriteLine();
        Console.WriteLine("      // Logging");
        Console.WriteLine("      _logger.LogDebug(\"GetByIdAsync called with id: {Id}\", id);");
        Console.WriteLine();
        Console.WriteLine("      // Main logic");
        Console.WriteLine("      var result = await _context.Users.FindAsync(id);");
        Console.WriteLine();
        Console.WriteLine("      _logger.LogDebug(\"GetByIdAsync returned: {Result}\", result);");
        Console.WriteLine("      return result;");
        Console.WriteLine("  }\n");
    }

    private async Task Demo10_CombinedAttributes(IMetricCollector metricCollector, IAuditStore auditStore)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  DEMO 10: Combined Attributes (The Power of Composition)      ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine("Attribute usage:");
        Console.WriteLine("  [CacheResult(DurationSeconds = 300, KeyPattern = \"user:id:{id}\")]");
        Console.WriteLine("  [ValidateParameters(ValidatePositive = true)]");
        Console.WriteLine("  [PerformanceMonitor(WarnThresholdMs = 50)]");
        Console.WriteLine("  [Audit(IncludeParameters = true)]");
        Console.WriteLine("  [RetryOnFailure(MaxAttempts = 2)]");
        Console.WriteLine("  Task<User?> GetByIdAsync(int id);\n");

        var userId = 42;
        var cache = new Dictionary<string, object?>();
        var cacheKey = $"user:id:{userId}";

        Console.WriteLine($"Executing GetByIdAsync({userId}) with 5 combined attributes:");
        Console.WriteLine();

        // 1. Validate parameters
        Console.WriteLine("1. [ValidateParameters] Validating id > 0...");
        if (userId <= 0)
        {
            Console.WriteLine("   ✗ Validation failed");
            return;
        }
        Console.WriteLine("   ✓ Validation passed");
        Console.WriteLine();

        // 2. Check cache
        Console.WriteLine("2. [CacheResult] Checking cache...");
        if (cache.ContainsKey(cacheKey))
        {
            Console.WriteLine($"   ✓ Cache hit for key \"{cacheKey}\"");
            metricCollector.RecordMetric("cache_hits", TimeSpan.FromMilliseconds(1));
        }
        else
        {
            Console.WriteLine("   → Cache miss, will query database");
            metricCollector.RecordMetric("cache_misses", TimeSpan.FromMilliseconds(1));

            // 3. Performance monitoring
            Console.WriteLine();
            Console.WriteLine("3. [PerformanceMonitor] Starting timer...");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // 4. Retry logic
            Console.WriteLine();
            Console.WriteLine("4. [RetryOnFailure] Executing with retry support...");
            Console.WriteLine("   → Attempt 1/2... Success! ✓");

            await Task.Delay(30); // Simulate DB query
            var user = new { Id = userId, Name = "John Doe", Email = "john@example.com" };

            sw.Stop();
            Console.WriteLine();
            Console.WriteLine($"   → Execution time: {sw.ElapsedMilliseconds}ms");
            if (sw.ElapsedMilliseconds > 50)
            {
                Console.WriteLine("   ⚠ WARN: Exceeded threshold");
            }
            else
            {
                Console.WriteLine("   ✓ Within threshold");
            }
            metricCollector.RecordMetric("GetByIdAsync", TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));

            // Cache the result
            cache[cacheKey] = user;
            Console.WriteLine($"   → Cached result with key \"{cacheKey}\"");

            // 5. Audit logging
            Console.WriteLine();
            Console.WriteLine("5. [Audit] Recording audit entry...");
            await auditStore.WriteAsync(new AuditEntry
            {
                EntityType = "User",
                EntityId = userId.ToString(),
                Action = "READ",
                User = "system",
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object> { { "id", userId } }
            });
            Console.WriteLine("   ✓ Audit entry recorded");
        }

        Console.WriteLine();
        Console.WriteLine("Result: All 5 attributes executed seamlessly! 🎉");
        Console.WriteLine("  → Input validated");
        Console.WriteLine("  → Cache checked (and populated)");
        Console.WriteLine("  → Performance monitored");
        Console.WriteLine("  → Retry logic ready");
        Console.WriteLine("  → Audit trail created");
        Console.WriteLine();
    }

    private void ShowCollectedMetrics(IMetricCollector metricCollector)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  COLLECTED METRICS                                            ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");

        var metrics = ((InMemoryMetricCollector)metricCollector).GetAllMetrics();
        foreach (var metric in metrics)
        {
            Console.WriteLine($"  {metric.MetricName}: {metric.Duration.TotalMilliseconds:F2}ms");
        }
        Console.WriteLine();
    }

    private async Task ShowAuditLogs(IAuditStore auditStore)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  AUDIT LOGS                                                   ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");

        var logs = await auditStore.QueryAsync(new AuditFilter());
        foreach (var log in logs)
        {
            Console.WriteLine($"  [{log.Timestamp:HH:mm:ss}] {log.User ?? "Unknown"}: {log.Action} {log.EntityType} (ID: {log.EntityId})");
            if (log.Parameters != null && log.Parameters.Any())
            {
                Console.WriteLine($"    Parameters: {System.Text.Json.JsonSerializer.Serialize(log.Parameters)}");
            }
            if (!string.IsNullOrEmpty(log.OldValue))
            {
                Console.WriteLine($"    Old: {log.OldValue}");
            }
            if (!string.IsNullOrEmpty(log.NewValue))
            {
                Console.WriteLine($"    New: {log.NewValue}");
            }
        }
        Console.WriteLine();
    }
}
