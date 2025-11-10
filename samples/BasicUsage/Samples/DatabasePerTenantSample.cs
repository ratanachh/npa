using NPA.Core.Core;
using NPA.Core.MultiTenancy;
using NPA.Extensions.MultiTenancy;
using NPA.Providers.PostgreSql.Extensions;
using NPA.Samples.Entities;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace NPA.Samples;

/// <summary>
/// Demonstrates Database Per Tenant isolation strategy (Phase 5.5).
/// 
/// Key Features:
/// - Complete database isolation - each tenant has their own database
/// - No TenantId columns needed in tables
/// - Maximum security and compliance
/// - Independent scaling and backup/restore per tenant
/// - Data residency compliance (GDPR, SOC2, etc.)
/// - Easy tenant migration and offboarding
/// 
/// Use Cases:
/// - Enterprise customers with strict compliance requirements
/// - Multi-national tenants with data residency laws
/// - Tenants requiring independent scaling
/// - SaaS with premium tier offering dedicated infrastructure
/// </summary>
public class DatabasePerTenantSample
{
    private readonly TenantManager _tenantManager;
    private readonly Dictionary<string, string> _connectionStrings;

    public DatabasePerTenantSample(
        TenantManager tenantManager,
        Dictionary<string, string> connectionStrings)
    {
        _tenantManager = tenantManager;
        _connectionStrings = connectionStrings;
    }

    public async Task RunAllDemosAsync()
    {
        Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║      Database Per Tenant Isolation Strategy (Phase 5.5)      ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

        await Demo1_CompleteDatabaseIsolationAsync();
        await Demo2_NoTenantIdColumnsAsync();
        await Demo3_IndependentScalingAsync();
        await Demo4_DataResidencyComplianceAsync();
        await Demo5_TenantMigrationAsync();
        await Demo6_PerformanceIsolationAsync();
        await Demo7_IndependentBackupRestoreAsync();

        Console.WriteLine("\n✅ All database-per-tenant demos completed successfully!\n");
    }

    /// <summary>
    /// Demo 1: Complete database isolation
    /// Shows: Each tenant has a completely separate database
    /// </summary>
    private async Task Demo1_CompleteDatabaseIsolationAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 1: Complete Database Isolation");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        // Create products in different tenant databases
        foreach (var tenantId in _connectionStrings.Keys)
        {
            await _tenantManager.SetCurrentTenantAsync(tenantId);
            var tenant = await GetCurrentTenantContextAsync();
            
            var entityManager = CreateEntityManagerForTenant(tenant.ConnectionString!);

            var product = new Product
            {
                Name = $"Product for {tenantId}",
                Description = $"Stored in dedicated database for {tenantId}",
                Price = 99.99m,
                StockQuantity = 50,
                CreatedAt = DateTime.UtcNow
            };

            await entityManager.PersistAsync(product);
            
            Console.WriteLine($"✓ Created product in {tenantId} database:");
            Console.WriteLine($"  └─ Database: {GetDatabaseName(tenant.ConnectionString!)}");
            Console.WriteLine($"  └─ Product: {product.Name}");
        }

        Console.WriteLine("\n✅ Each tenant's data is in a completely separate database!");
        Console.WriteLine("   Maximum isolation - no shared tables, no shared infrastructure");
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 2: Simplified schema (tenant_id optional)
    /// Shows: In pure database-per-tenant, tenant_id columns are redundant
    /// Note: Our sample includes tenant_id for entity compatibility, but it's not required
    /// </summary>
    private async Task Demo2_NoTenantIdColumnsAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 2: Simplified Schema (TenantId Optional)");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        await _tenantManager.SetCurrentTenantAsync("acme-corp");
        var tenant = await GetCurrentTenantContextAsync();
        
        await using var connection = new NpgsqlConnection(tenant.ConnectionString);
        await connection.OpenAsync();

        // Check schema
        var checkSchema = @"
            SELECT column_name, data_type 
            FROM information_schema.columns 
            WHERE table_name = 'products'
            ORDER BY ordinal_position;
        ";

        await using var command = connection.CreateCommand();
        command.CommandText = checkSchema;

        Console.WriteLine("Products table schema:");
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var columnName = reader.GetString(0);
            var dataType = reader.GetString(1);
            Console.WriteLine($"  └─ {columnName} ({dataType})");
        }

        Console.WriteLine("\n✅ Key insight: tenant_id column is optional!");
        Console.WriteLine("   Our sample includes it for entity compatibility");
        Console.WriteLine("   In pure database-per-tenant, you could omit it entirely");
        Console.WriteLine("   Database-level isolation is already complete");
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 3: Independent scaling
    /// Shows: Each tenant database can be scaled independently
    /// </summary>
    private async Task Demo3_IndependentScalingAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 3: Independent Scaling per Tenant");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        Console.WriteLine("Tenant database deployment strategies:\n");

        var scalingInfo = new Dictionary<string, string>
        {
            ["acme-corp"] = "Large instance (8 vCPU, 32GB RAM) - High transaction volume",
            ["contoso-ltd"] = "Medium instance (4 vCPU, 16GB RAM) - Moderate usage",
            ["fabrikam-inc"] = "Small instance (2 vCPU, 8GB RAM) - Low usage"
        };

        foreach (var kvp in scalingInfo)
        {
            await _tenantManager.SetCurrentTenantAsync(kvp.Key);
            var tenant = await GetCurrentTenantContextAsync();
            
            Console.WriteLine($"✓ {tenant.Name}:");
            Console.WriteLine($"  └─ Database: {GetDatabaseName(tenant.ConnectionString!)}");
            Console.WriteLine($"  └─ Scaling: {kvp.Value}");
        }

        Console.WriteLine("\n✅ Each tenant can have different database resources!");
        Console.WriteLine("   Scale up/down based on tenant size and SLA");
        Console.WriteLine("   No noisy neighbor problems");
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 4: Data residency and compliance
    /// Shows: Tenants can have databases in specific geographic regions
    /// </summary>
    private async Task Demo4_DataResidencyComplianceAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 4: Data Residency & Compliance");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        Console.WriteLine("Tenant data residency and compliance information:\n");

        foreach (var tenantId in _connectionStrings.Keys)
        {
            await _tenantManager.SetCurrentTenantAsync(tenantId);
            var tenant = await GetCurrentTenantContextAsync();
            
            var tenantInfo = await GetTenantMetadataAsync(tenant.ConnectionString!);
            
            Console.WriteLine($"✓ {tenant.Name}:");
            Console.WriteLine($"  └─ Region: {tenantInfo.Region}");
            Console.WriteLine($"  └─ Compliance: {tenantInfo.ComplianceLevel}");
            Console.WriteLine($"  └─ Database: {GetDatabaseName(tenant.ConnectionString!)}");
        }

        Console.WriteLine("\n✅ Database per tenant enables data residency compliance!");
        Console.WriteLine("   GDPR: EU tenant data stays in EU");
        Console.WriteLine("   SOC2: US tenant data in compliant US data centers");
        Console.WriteLine("   ISO27001: APAC tenant data in certified APAC regions");
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 5: Easy tenant migration
    /// Shows: Moving a tenant to different infrastructure is simple
    /// </summary>
    private async Task Demo5_TenantMigrationAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 5: Easy Tenant Migration");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        await _tenantManager.SetCurrentTenantAsync("fabrikam-inc");
        var tenant = await GetCurrentTenantContextAsync();

        Console.WriteLine("Migration scenario: Moving Fabrikam Inc to premium tier");
        Console.WriteLine($"\n1. Current location:");
        Console.WriteLine($"   └─ Database: {GetDatabaseName(tenant.ConnectionString!)}");
        Console.WriteLine($"   └─ Tier: Standard (shared server)");

        Console.WriteLine($"\n2. Migration steps:");
        Console.WriteLine($"   ✓ Dump database: pg_dump {GetDatabaseName(tenant.ConnectionString!)}");
        Console.WriteLine($"   ✓ Create new database on premium server");
        Console.WriteLine($"   ✓ Restore: pg_restore to new location");
        Console.WriteLine($"   ✓ Update tenant connection string");
        Console.WriteLine($"   ✓ Verify data integrity");
        Console.WriteLine($"   ✓ Switch traffic to new database");

        Console.WriteLine($"\n3. New location:");
        Console.WriteLine($"   └─ Database: fabrikam_inc_premium_db");
        Console.WriteLine($"   └─ Tier: Premium (dedicated server)");
        Console.WriteLine($"   └─ Downtime: < 5 minutes");

        Console.WriteLine("\n✅ Database per tenant makes migration straightforward!");
        Console.WriteLine("   Self-contained database with all tenant data");
        Console.WriteLine("   No risk of affecting other tenants");
        Console.WriteLine("   Standard database backup/restore tools work");
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 6: Performance isolation
    /// Shows: One tenant's load doesn't affect others
    /// </summary>
    private async Task Demo6_PerformanceIsolationAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 6: Performance Isolation");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        Console.WriteLine("Simulating different load patterns:\n");

        // Acme Corp - high load
        await _tenantManager.SetCurrentTenantAsync("acme-corp");
        var acmeTenant = await GetCurrentTenantContextAsync();
        var acmeManager = CreateEntityManagerForTenant(acmeTenant.ConnectionString!);
        
        Console.WriteLine("✓ Acme Corp - High load simulation:");
        var acmeProducts = Enumerable.Range(1, 100).Select(i => new Product
        {
            Name = $"High-Volume Product {i}",
            Price = 49.99m,
            StockQuantity = i,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        foreach (var product in acmeProducts)
        {
            await acmeManager.PersistAsync(product);
        }
        Console.WriteLine($"  └─ Created 100 products (heavy write load)");

        // Contoso - normal load  
        await _tenantManager.SetCurrentTenantAsync("contoso-ltd");
        var contosoTenant = await GetCurrentTenantContextAsync();
        var contosoManager = CreateEntityManagerForTenant(contosoTenant.ConnectionString!);
        
        Console.WriteLine("\n✓ Contoso Ltd - Normal load:");
        var contosoProduct = new Product
        {
            Name = "Normal Product",
            Price = 99.99m,
            StockQuantity = 10,
            CreatedAt = DateTime.UtcNow
        };
        await contosoManager.PersistAsync(contosoProduct);
        Console.WriteLine($"  └─ Created 1 product (light write load)");

        Console.WriteLine("\n✅ Performance is completely isolated!");
        Console.WriteLine("   Acme's heavy load DOES NOT impact Contoso");
        Console.WriteLine("   Separate databases = separate resources");
        Console.WriteLine("   No query queue contention, no table locking conflicts");
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 7: Independent backup and restore
    /// Shows: Each tenant can have their own backup schedule and retention
    /// </summary>
    private async Task Demo7_IndependentBackupRestoreAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 7: Independent Backup & Restore");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        Console.WriteLine("Tenant-specific backup strategies:\n");

        var backupStrategies = new Dictionary<string, string>
        {
            ["acme-corp"] = "Hourly backups, 30-day retention (Enterprise SLA)",
            ["contoso-ltd"] = "Daily backups, 14-day retention (Business SLA)",
            ["fabrikam-inc"] = "Weekly backups, 7-day retention (Standard SLA)"
        };

        foreach (var kvp in backupStrategies)
        {
            await _tenantManager.SetCurrentTenantAsync(kvp.Key);
            var tenant = await GetCurrentTenantContextAsync();
            
            Console.WriteLine($"✓ {tenant.Name}:");
            Console.WriteLine($"  └─ Database: {GetDatabaseName(tenant.ConnectionString!)}");
            Console.WriteLine($"  └─ Strategy: {kvp.Value}");
            Console.WriteLine($"  └─ Point-in-time recovery: Available");
        }

        Console.WriteLine("\n✅ Backup/restore is tenant-specific!");
        Console.WriteLine("   Different SLAs = different backup frequencies");
        Console.WriteLine("   Restore single tenant without affecting others");
        Console.WriteLine("   Easy tenant offboarding - just drop the database");
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

    private async Task<TenantMetadata> GetTenantMetadataAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT data_residency_region, compliance_level FROM tenant_info LIMIT 1";
        
        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new TenantMetadata
            {
                Region = reader.GetString(0),
                ComplianceLevel = reader.GetString(1)
            };
        }

        return new TenantMetadata { Region = "Unknown", ComplianceLevel = "Unknown" };
    }

    private class TenantMetadata
    {
        public string Region { get; set; } = string.Empty;
        public string ComplianceLevel { get; set; } = string.Empty;
    }
}
