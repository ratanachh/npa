using NPA.Core.Core;
using NPA.Core.MultiTenancy;
using NPA.Extensions.MultiTenancy;
using NPA.Providers.PostgreSql.Extensions;
using NPA.Samples.Entities;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using static NPA.Samples.RegionPerTenantSampleRunner;

namespace NPA.Samples;

/// <summary>
/// Demonstrates Region Per Tenant isolation strategy (Phase 5.5).
/// 
/// Key Features:
/// - Geographic isolation - tenants in region-specific databases
/// - Data residency compliance (GDPR, PDPA, etc.)
/// - Reduced latency - data close to users
/// - Regional failover and disaster recovery
/// - Compliance with data sovereignty laws
/// - Multi-region deployment strategy
/// 
/// Use Cases:
/// - Global SaaS with data residency requirements
/// - Compliance with GDPR, CCPA, PDPA regulations
/// - Latency-sensitive applications
/// - Multi-national enterprises
/// - Financial services with regional regulations
/// </summary>
public class RegionPerTenantSample
{
    private readonly TenantManager _tenantManager;
    private readonly Dictionary<string, RegionInfo> _regions;

    public RegionPerTenantSample(
        TenantManager tenantManager,
        Dictionary<string, RegionInfo> regions)
    {
        _tenantManager = tenantManager;
        _regions = regions;
    }

    public async Task RunAllDemosAsync()
    {
        Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║      Region Per Tenant Isolation Strategy (Phase 5.5)        ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

        await Demo1_GeographicIsolationAsync();
        await Demo2_DataResidencyComplianceAsync();
        await Demo3_LatencyOptimizationAsync();
        await Demo4_RegionalFailoverAsync();
        await Demo5_MultiRegionDeploymentAsync();
        await Demo6_CrossRegionReportingAsync();
        await Demo7_RegionalScalingAsync();

        Console.WriteLine("\n✅ All region-per-tenant demos completed successfully!\n");
    }

    /// <summary>
    /// Demo 1: Geographic isolation
    /// Shows: Tenants are placed in region-specific databases
    /// </summary>
    private async Task Demo1_GeographicIsolationAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 1: Geographic Isolation");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        Console.WriteLine("Creating tenant data in regional databases:\n");

        // Create products for tenants in each region
        foreach (var region in _regions.Values)
        {
            Console.WriteLine($"Region: {region.Name} ({region.Location})");
            
            foreach (var tenantId in region.Tenants)
            {
                await _tenantManager.SetCurrentTenantAsync(tenantId);
                var tenant = await GetCurrentTenantContextAsync();
                
                var entityManager = CreateEntityManagerForTenant(tenant.ConnectionString!);

                var product = new Product
                {
                    Name = $"Regional Product - {tenantId}",
                    Description = $"Stored in {region.Name} for compliance",
                    Price = 199.99m,
                    StockQuantity = 100,
                    CreatedAt = DateTime.UtcNow
                };

                await entityManager.PersistAsync(product);
                
                Console.WriteLine($"  ✓ {GetTenantName(tenantId)}: Product created in {region.Name}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("✅ Tenants isolated by geographic region!");
        Console.WriteLine("   US tenants → US-East database");
        Console.WriteLine("   EU tenants → EU-West database");
        Console.WriteLine("   APAC tenants → APAC-Singapore database");
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 2: Data residency compliance
    /// Shows: Ensuring data stays in required jurisdictions
    /// </summary>
    private async Task Demo2_DataResidencyComplianceAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 2: Data Residency Compliance");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        Console.WriteLine("Tenant data residency status:\n");

        foreach (var region in _regions.Values)
        {
            Console.WriteLine($"📍 {region.Name} - {region.Location}");
            Console.WriteLine($"   Compliance Framework: {region.ComplianceFramework}");
            
            foreach (var tenantId in region.Tenants)
            {
                await _tenantManager.SetCurrentTenantAsync(tenantId);
                var tenant = await GetCurrentTenantContextAsync();
                
                var residencyInfo = await GetTenantResidencyInfoAsync(tenant.ConnectionString!, tenantId);
                
                var residencyStatus = residencyInfo.DataResidencyRequired ? "✓ Required" : "○ Optional";
                Console.WriteLine($"   • {GetTenantName(tenantId)}: {residencyStatus}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("✅ Data residency compliance enforced!");
        Console.WriteLine("   GDPR: EU tenant data never leaves EU region");
        Console.WriteLine("   PDPA: Singapore tenant data stays in APAC");
        Console.WriteLine("   SOC2: US tenant data in compliant US data centers");
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 3: Latency optimization
    /// Shows: Regional databases reduce network latency for users
    /// </summary>
    private async Task Demo3_LatencyOptimizationAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 3: Latency Optimization");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        Console.WriteLine("Simulated latency for different deployment strategies:\n");

        // Simulate latency measurements
        var latencyComparison = new Dictionary<string, Dictionary<string, int>>
        {
            ["US-East Users"] = new()
            {
                ["Single US Database"] = 10,
                ["Single EU Database"] = 150,
                ["Regional Databases"] = 10
            },
            ["EU-West Users"] = new()
            {
                ["Single US Database"] = 140,
                ["Single EU Database"] = 15,
                ["Regional Databases"] = 15
            },
            ["APAC Users"] = new()
            {
                ["Single US Database"] = 250,
                ["Single EU Database"] = 200,
                ["Regional Databases"] = 20
            }
        };

        foreach (var userRegion in latencyComparison)
        {
            Console.WriteLine($"📊 {userRegion.Key}:");
            foreach (var strategy in userRegion.Value)
            {
                var indicator = strategy.Key == "Regional Databases" ? "✓" : " ";
                var latency = strategy.Value;
                var rating = latency < 50 ? "Excellent" : latency < 100 ? "Good" : "Poor";
                Console.WriteLine($"   {indicator} {strategy.Key}: {latency}ms ({rating})");
            }
            Console.WriteLine();
        }

        Console.WriteLine("✅ Regional deployment dramatically reduces latency!");
        Console.WriteLine("   US users: 10ms vs 250ms (25x faster)");
        Console.WriteLine("   EU users: 15ms vs 140ms (9x faster)");
        Console.WriteLine("   APAC users: 20ms vs 250ms (12x faster)");
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 4: Regional failover
    /// Shows: Disaster recovery and failover within regions
    /// </summary>
    private async Task Demo4_RegionalFailoverAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 4: Regional Failover & Disaster Recovery");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        Console.WriteLine("Disaster recovery strategy per region:\n");

        foreach (var region in _regions.Values)
        {
            Console.WriteLine($"📍 {region.Name}:");
            Console.WriteLine($"   Primary:   {region.Location}");
            Console.WriteLine($"   Secondary: {GetSecondaryLocation(region.Name)}");
            Console.WriteLine($"   Replication: Asynchronous streaming replication");
            Console.WriteLine($"   RPO: < 1 minute");
            Console.WriteLine($"   RTO: < 5 minutes");
            Console.WriteLine($"   Failover: Automatic via health checks");
            Console.WriteLine();
        }

        Console.WriteLine("✅ Regional failover keeps data in jurisdiction!");
        Console.WriteLine("   EU primary fails → EU secondary takes over");
        Console.WriteLine("   Data never crosses regional boundaries during DR");
        Console.WriteLine("   Maintains compliance during outages");
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 5: Multi-region deployment
    /// Shows: Managing a global SaaS with regional databases
    /// </summary>
    private async Task Demo5_MultiRegionDeploymentAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 5: Multi-Region Deployment Architecture");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        Console.WriteLine("Global deployment overview:\n");

        var totalTenants = _regions.Values.Sum(r => r.Tenants.Length);
        
        Console.WriteLine($"🌍 Global SaaS Deployment:");
        Console.WriteLine($"   Total Regions: {_regions.Count}");
        Console.WriteLine($"   Total Tenants: {totalTenants}");
        Console.WriteLine();

        foreach (var region in _regions.Values)
        {
            Console.WriteLine($"   {region.Name}:");
            Console.WriteLine($"   ├─ Location: {region.Location}");
            Console.WriteLine($"   ├─ Tenants: {region.Tenants.Length}");
            Console.WriteLine($"   ├─ Database: {GetDatabaseName(region.ConnectionString)}");
            Console.WriteLine($"   └─ Compliance: {region.ComplianceFramework}");
            Console.WriteLine();
        }

        Console.WriteLine("✅ Multi-region deployment supports global scale!");
        Console.WriteLine("   Each region operates independently");
        Console.WriteLine("   Global tenant directory routes to correct region");
        Console.WriteLine("   Scales horizontally by adding regions");
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 6: Cross-region reporting
    /// Shows: Aggregating data across regions (when permitted)
    /// </summary>
    private async Task Demo6_CrossRegionReportingAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 6: Cross-Region Reporting");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        Console.WriteLine("Global metrics aggregation:\n");

        var globalMetrics = new Dictionary<string, object>
        {
            ["Total Tenants"] = 0,
            ["Total Products"] = 0,
            ["Total Revenue"] = 0m
        };

        foreach (var region in _regions.Values)
        {
            Console.WriteLine($"📊 {region.Name} Metrics:");
            
            var regionMetrics = await GetRegionalMetricsAsync(region.ConnectionString);
            
            Console.WriteLine($"   Tenants: {regionMetrics.TenantCount}");
            Console.WriteLine($"   Products: {regionMetrics.ProductCount}");
            Console.WriteLine($"   Revenue: ${regionMetrics.Revenue:N2}");
            Console.WriteLine();

            globalMetrics["Total Tenants"] = (int)globalMetrics["Total Tenants"] + regionMetrics.TenantCount;
            globalMetrics["Total Products"] = (int)globalMetrics["Total Products"] + regionMetrics.ProductCount;
            globalMetrics["Total Revenue"] = (decimal)globalMetrics["Total Revenue"] + regionMetrics.Revenue;
        }

        Console.WriteLine("🌍 Global Totals:");
        foreach (var metric in globalMetrics)
        {
            var value = metric.Key.Contains("Revenue") 
                ? $"${metric.Value:N2}" 
                : metric.Value.ToString();
            Console.WriteLine($"   {metric.Key}: {value}");
        }

        Console.WriteLine("\n✅ Cross-region reporting for analytics!");
        Console.WriteLine("   Aggregate metrics from all regions");
        Console.WriteLine("   No PII crosses borders - only aggregates");
        Console.WriteLine("   Compliance-safe global dashboards");
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 7: Regional scaling
    /// Shows: Different regions can scale independently
    /// </summary>
    private async Task Demo7_RegionalScalingAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 7: Independent Regional Scaling");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        Console.WriteLine("Regional scaling strategies:\n");

        var scalingInfo = new Dictionary<string, string>
        {
            ["US-East"] = "Large (8 vCPU, 32GB) - High growth market",
            ["EU-West"] = "Medium (4 vCPU, 16GB) - Stable market",
            ["APAC-Singapore"] = "Small (2 vCPU, 8GB) - Emerging market"
        };

        foreach (var kvp in scalingInfo)
        {
            var region = _regions[kvp.Key];
            Console.WriteLine($"📍 {region.Name}:");
            Console.WriteLine($"   Instance Size: {kvp.Value}");
            Console.WriteLine($"   Read Replicas: {GetReadReplicaCount(kvp.Key)}");
            Console.WriteLine($"   Auto-scaling: Enabled");
            Console.WriteLine($"   Peak Capacity: {GetPeakCapacity(kvp.Key)} connections");
            Console.WriteLine();
        }

        Console.WriteLine("✅ Each region scales based on demand!");
        Console.WriteLine("   US-East: Premium tier for high-growth market");
        Console.WriteLine("   EU-West: Business tier for stable operations");
        Console.WriteLine("   APAC: Standard tier scaling with growth");
        Console.WriteLine();
    }

    // Helper methods

    private IEntityManager CreateEntityManagerForTenant(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddPostgreSqlProvider(connectionString);
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IEntityManager>();
    }

    private async Task<TenantContext> GetCurrentTenantContextAsync()
    {
        var tenantProvider = await Task.FromResult(_tenantManager.GetType()
            .GetField("_tenantProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(_tenantManager) as ITenantProvider);
        
        return tenantProvider!.GetCurrentTenant()!;
    }

    private string GetDatabaseName(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        return builder.Database ?? "unknown";
    }

    private string GetTenantName(string tenantId) => tenantId switch
    {
        "acme-corp" => "Acme Corporation",
        "tesla-motors" => "Tesla Motors",
        "contoso-ltd" => "Contoso Ltd",
        "siemens-ag" => "Siemens AG",
        "fabrikam-inc" => "Fabrikam Inc",
        "toyota-apac" => "Toyota APAC",
        _ => tenantId
    };

    private string GetSecondaryLocation(string region) => region switch
    {
        "US-East" => "US-West (Oregon)",
        "EU-West" => "EU-North (Frankfurt)",
        "APAC-Singapore" => "APAC-Tokyo",
        _ => "Unknown"
    };

    private int GetReadReplicaCount(string region) => region switch
    {
        "US-East" => 3,
        "EU-West" => 2,
        "APAC-Singapore" => 1,
        _ => 0
    };

    private string GetPeakCapacity(string region) => region switch
    {
        "US-East" => "10,000",
        "EU-West" => "5,000",
        "APAC-Singapore" => "2,000",
        _ => "0"
    };

    private async Task<ResidencyInfo> GetTenantResidencyInfoAsync(string connectionString, string tenantId)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT data_residency_required FROM tenant_info WHERE tenant_id = @tenantId";
        command.Parameters.AddWithValue("tenantId", tenantId);
        
        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ResidencyInfo
            {
                DataResidencyRequired = reader.GetBoolean(0)
            };
        }

        return new ResidencyInfo { DataResidencyRequired = false };
    }

    private async Task<RegionalMetrics> GetRegionalMetricsAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                (SELECT COUNT(DISTINCT tenant_id) FROM tenant_info) as tenant_count,
                (SELECT COUNT(*) FROM products) as product_count,
                (SELECT COALESCE(SUM(price * stock_quantity), 0) FROM products WHERE is_active = true) as revenue
        ";
        
        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new RegionalMetrics
            {
                TenantCount = reader.GetInt32(0),
                ProductCount = Convert.ToInt32(reader.GetInt64(1)),
                Revenue = reader.GetDecimal(2)
            };
        }

        return new RegionalMetrics();
    }

    private class ResidencyInfo
    {
        public bool DataResidencyRequired { get; set; }
    }

    private class RegionalMetrics
    {
        public int TenantCount { get; set; }
        public int ProductCount { get; set; }
        public decimal Revenue { get; set; }
    }
}
