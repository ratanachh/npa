using NPA.Core.Core;
using NPA.Core.MultiTenancy;
using NPA.Extensions.MultiTenancy;
using NPA.Providers.PostgreSql.Extensions;
using NPA.Samples.Entities;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace NPA.Samples;

/// <summary>
/// Demonstrates Schema Per Tenant isolation strategy (Phase 5.5).
/// 
/// Key Features:
/// - Schema-level isolation - each tenant has their own schema
/// - No TenantId columns needed in tables
/// - Good balance between isolation and cost
/// - Shared database infrastructure
/// - Easy backup/restore per tenant schema
/// - Better resource utilization than database-per-tenant
/// 
/// Use Cases:
/// - Medium-sized SaaS with 10-1000 tenants
/// - Compliance requirements for logical isolation
/// - Cost-effective alternative to database-per-tenant
/// - Tenants need customizable schemas
/// </summary>
public class SchemaPerTenantSample
{
    private readonly TenantManager _tenantManager;
    private readonly string _connectionString;
    private readonly Dictionary<string, string> _tenantSchemas;

    public SchemaPerTenantSample(
        TenantManager tenantManager,
        string connectionString,
        Dictionary<string, string> tenantSchemas)
    {
        _tenantManager = tenantManager;
        _connectionString = connectionString;
        _tenantSchemas = tenantSchemas;
    }

    public async Task RunAllDemosAsync()
    {
        Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║      Schema Per Tenant Isolation Strategy (Phase 5.5)        ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

        await Demo1_SchemaIsolationAsync();
        await Demo2_NoTenantIdColumnsAsync();
        await Demo3_SharedInfrastructureAsync();
        await Demo4_SchemaBasedBackupAsync();
        await Demo5_TenantQuotasAsync();
        await Demo6_SchemaLevelSecurityAsync();
        await Demo7_CostEfficiencyAsync();

        Console.WriteLine("\n[Completed] All schema-per-tenant demos completed successfully!\n");
    }

    /// <summary>
    /// Demo 1: Schema-level isolation
    /// Shows: Each tenant has their own schema in the same database
    /// </summary>
    private async Task Demo1_SchemaIsolationAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 1: Schema-Level Isolation");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        // Create products in different tenant schemas
        foreach (var kvp in _tenantSchemas)
        {
            await _tenantManager.SetCurrentTenantAsync(kvp.Key);
            var tenant = await GetCurrentTenantContextAsync();
            
            var entityManager = CreateEntityManagerForTenant(_connectionString);

            var product = new Product
            {
                Name = $"Product for {kvp.Key}",
                Description = $"Stored in schema: {kvp.Value}",
                Price = 149.99m,
                StockQuantity = 75,
                CreatedAt = DateTime.UtcNow
            };

            await entityManager.PersistAsync(product);
            
            Console.WriteLine($"✓ Created product for {kvp.Key}:");
            Console.WriteLine($"  └─ Schema: {kvp.Value}");
            Console.WriteLine($"  └─ Table: {kvp.Value}.products");
            Console.WriteLine($"  └─ Product: {product.Name}");
        }

        Console.WriteLine("\n[Completed] Each tenant's data is in a separate schema!");
        Console.WriteLine("   Same database, different schemas = good isolation");
        Console.WriteLine("   SQL: SELECT * FROM acme_schema.products");
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 2: Simplified schema (tenant_id optional)
    /// Shows: In schema-per-tenant, tenant_id columns are redundant
    /// Note: Our sample includes tenant_id for entity compatibility, but it's not required
    /// </summary>
    private async Task Demo2_NoTenantIdColumnsAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 2: Simplified Schema (TenantId Optional)");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Check schema across different tenant schemas
        foreach (var kvp in _tenantSchemas.Take(1)) // Just show one example
        {
            var checkSchema = $@"
                SELECT column_name, data_type 
                FROM information_schema.columns 
                WHERE table_schema = '{kvp.Value}' 
                AND table_name = 'products'
                ORDER BY ordinal_position;
            ";

            await using var command = connection.CreateCommand();
            command.CommandText = checkSchema;

            Console.WriteLine($"Schema: {kvp.Value}.products");
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var columnName = reader.GetString(0);
                var dataType = reader.GetString(1);
                Console.WriteLine($"  └─ {columnName} ({dataType})");
            }
        }

        Console.WriteLine("\n[Completed] Key insight: tenant_id column is optional!");
        Console.WriteLine("   Our sample includes it for entity compatibility");
        Console.WriteLine("   In pure schema-per-tenant, you could omit it entirely");
        Console.WriteLine("   Schema-level isolation is already complete");
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 3: Shared infrastructure
    /// Shows: All tenants share same database server resources
    /// </summary>
    private async Task Demo3_SharedInfrastructureAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 3: Shared Infrastructure Benefits");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Get database info
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                (SELECT count(*) FROM information_schema.schemata WHERE schema_name LIKE '%_schema') as schema_count,
                current_database() as database_name,
                version() as db_version;
        ";

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var schemaCount = reader.GetInt64(0);
            var dbName = reader.GetString(1);
            var version = reader.GetString(2);

            Console.WriteLine($"✓ Database: {dbName}");
            Console.WriteLine($"  └─ Tenant schemas: {schemaCount}");
            Console.WriteLine($"  └─ All tenants share:");
            Console.WriteLine($"     • Same database server");
            Console.WriteLine($"     • Same connection pool");
            Console.WriteLine($"     • Same backup infrastructure");
            Console.WriteLine($"     • Same monitoring tools");
        }

        Console.WriteLine("\n[Completed] Shared infrastructure = better resource utilization!");
        Console.WriteLine("   Lower cost than database-per-tenant");
        Console.WriteLine("   Simpler operations than managing multiple databases");
        Console.WriteLine("   Better than discriminator for compliance/isolation needs");
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 4: Schema-based backup and restore
    /// Shows: Can backup/restore individual tenant schemas
    /// </summary>
    private async Task Demo4_SchemaBasedBackupAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 4: Schema-Based Backup & Restore");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        Console.WriteLine("Backup/restore capabilities per tenant:\n");

        foreach (var kvp in _tenantSchemas)
        {
            var tenantId = kvp.Key;
            var schemaName = kvp.Value;

            await _tenantManager.SetCurrentTenantAsync(tenantId);
            var tenant = await GetCurrentTenantContextAsync();

            Console.WriteLine($"✓ {tenant.Name} ({schemaName}):");
            Console.WriteLine($"  └─ Backup:  pg_dump --schema={schemaName}");
            Console.WriteLine($"  └─ Restore: pg_restore --schema={schemaName}");
            Console.WriteLine($"  └─ Export:  COPY {schemaName}.products TO '/backup/{schemaName}_products.csv'");
        }

        Console.WriteLine("\n[Completed] Schema-based operations are efficient!");
        Console.WriteLine("   Backup single tenant without affecting others");
        Console.WriteLine("   Restore to point-in-time for specific tenant");
        Console.WriteLine("   Faster than full database backup");
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 5: Per-tenant quotas and limits
    /// Shows: Different tenants can have different resource quotas
    /// </summary>
    private async Task Demo5_TenantQuotasAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 5: Tenant Quotas & Resource Limits");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        Console.WriteLine("Tenant tier and quota information:\n");

        foreach (var tenantId in _tenantSchemas.Keys)
        {
            await _tenantManager.SetCurrentTenantAsync(tenantId);
            var tenant = await GetCurrentTenantContextAsync();
            
            var quotaInfo = await GetTenantQuotaAsync(tenant.Schema!);
            
            Console.WriteLine($"✓ {tenant.Name}:");
            Console.WriteLine($"  └─ Schema: {tenant.Schema}");
            Console.WriteLine($"  └─ Max Users: {quotaInfo.MaxUsers}");
            Console.WriteLine($"  └─ Storage Quota: {quotaInfo.StorageQuotaGB} GB");
            Console.WriteLine($"  └─ Tier: {GetTier(quotaInfo.MaxUsers)}");
        }

        Console.WriteLine("\n[Completed] Different tenants can have different limits!");
        Console.WriteLine("   Enterprise tier: More users and storage");
        Console.WriteLine("   Standard tier: Moderate limits");
        Console.WriteLine("   Starter tier: Basic limits");
        Console.WriteLine();
    }

    /// <summary>
    /// Demo 6: Schema-level security
    /// Shows: PostgreSQL permissions can enforce schema-level access
    /// </summary>
    private Task Demo6_SchemaLevelSecurityAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 6: Schema-Level Security");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        Console.WriteLine("Security model with schema per tenant:\n");

        foreach (var kvp in _tenantSchemas)
        {
            var tenantId = kvp.Key;
            var schemaName = kvp.Value;

            Console.WriteLine($"✓ {tenantId} security:");
            Console.WriteLine($"  └─ Schema: {schemaName}");
            Console.WriteLine($"  └─ DB User: {tenantId}_user (optional)");
            Console.WriteLine($"  └─ Permissions: GRANT USAGE ON SCHEMA {schemaName} TO {tenantId}_user");
            Console.WriteLine($"  └─ Isolation: Cannot access other schemas");
        }

        Console.WriteLine("\n[Completed] Database-level security enforcement!");
        Console.WriteLine("   PostgreSQL GRANT/REVOKE controls schema access");
        Console.WriteLine("   Optional: Create dedicated DB user per tenant");
        Console.WriteLine("   Defense in depth: Application + DB level security");
        Console.WriteLine();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Demo 7: Cost efficiency comparison
    /// Shows: Schema per tenant is more cost-effective than database per tenant
    /// </summary>
    private async Task Demo7_CostEfficiencyAsync()
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine("Demo 7: Cost Efficiency Analysis");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Get counts
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                (SELECT count(*) FROM information_schema.schemata WHERE schema_name LIKE '%_schema') as schemas,
                (SELECT count(*) FROM pg_tables WHERE schemaname LIKE '%_schema') as total_tables;
        ";

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var schemaCount = reader.GetInt64(0);
            var totalTables = reader.GetInt64(1);

            Console.WriteLine($"Current deployment:");
            Console.WriteLine($"  ✓ Database servers: 1");
            Console.WriteLine($"  ✓ Tenant schemas: {schemaCount}");
            Console.WriteLine($"  ✓ Total tables: {totalTables}");
            Console.WriteLine($"\nComparison with alternatives:");
            Console.WriteLine($"\n1. Database Per Tenant:");
            Console.WriteLine($"   • Database servers needed: {schemaCount}");
            Console.WriteLine($"   • Estimated cost: ${schemaCount * 100}/month");
            Console.WriteLine($"   • Isolation: Maximum ⭐⭐⭐⭐⭐");
            Console.WriteLine($"\n2. Schema Per Tenant (Current):");
            Console.WriteLine($"   • Database servers needed: 1");
            Console.WriteLine($"   • Estimated cost: $150/month");
            Console.WriteLine($"   • Isolation: Good ⭐⭐⭐⭐");
            Console.WriteLine($"\n3. Discriminator Column:");
            Console.WriteLine($"   • Database servers needed: 1");
            Console.WriteLine($"   • Estimated cost: $100/month");
            Console.WriteLine($"   • Isolation: Basic ⭐⭐⭐");
        }

        Console.WriteLine("\n[Completed] Schema per tenant: Sweet spot for most SaaS!");
        Console.WriteLine("   60-75% cheaper than database-per-tenant");
        Console.WriteLine("   Better isolation than discriminator column");
        Console.WriteLine("   Scales to 100-1000 tenants efficiently");
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

    private async Task<QuotaInfo> GetTenantQuotaAsync(string schemaName)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT max_users, storage_quota_gb FROM {schemaName}.tenant_info LIMIT 1";
        
        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new QuotaInfo
            {
                MaxUsers = reader.GetInt32(0),
                StorageQuotaGB = reader.GetInt32(1)
            };
        }

        return new QuotaInfo { MaxUsers = 0, StorageQuotaGB = 0 };
    }

    private string GetTier(int maxUsers) => maxUsers switch
    {
        >= 500 => "Enterprise",
        >= 200 => "Business",
        >= 100 => "Professional",
        _ => "Starter"
    };

    private class QuotaInfo
    {
        public int MaxUsers { get; set; }
        public int StorageQuotaGB { get; set; }
    }
}
