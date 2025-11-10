using Microsoft.Extensions.DependencyInjection;
using NPA.Core.Core;
using NPA.Core.MultiTenancy;
using NPA.Extensions.MultiTenancy;
using NPA.Providers.PostgreSql.Extensions;
using NPA.Samples.Core;
using Npgsql;
using Testcontainers.PostgreSql;

namespace NPA.Samples;

/// <summary>
/// Sample wrapper for Database Per Tenant multi-tenancy strategy.
/// Demonstrates complete database isolation with separate databases per tenant.
/// Implements ISample for automatic discovery by SampleRunner.
/// </summary>
public class DatabasePerTenantSampleRunner : ISample
{
    public string Name => "Database Per Tenant (Phase 5.5)";

    public string Description => "Demonstrates complete tenant isolation using separate databases, ideal for enterprise customers with strict compliance requirements";

    public async Task RunAsync()
    {
        // We'll create multiple PostgreSQL containers, one per tenant
        var tenant1Container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("tenant_acme_db")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithCleanUp(true)
            .Build();

        var tenant2Container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("tenant_contoso_db")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithCleanUp(true)
            .Build();

        var tenant3Container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("tenant_fabrikam_db")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithCleanUp(true)
            .Build();

        await using (tenant1Container)
        await using (tenant2Container)
        await using (tenant3Container)
        {
            Console.WriteLine("Starting PostgreSQL containers for each tenant...");
            await Task.WhenAll(
                tenant1Container.StartAsync(),
                tenant2Container.StartAsync(),
                tenant3Container.StartAsync()
            );
            Console.WriteLine("All tenant databases started.\n");

            var connectionStrings = new Dictionary<string, string>
            {
                ["acme-corp"] = tenant1Container.GetConnectionString(),
                ["contoso-ltd"] = tenant2Container.GetConnectionString(),
                ["fabrikam-inc"] = tenant3Container.GetConnectionString()
            };

            // Setup dependency injection - we'll use a connection string provider
            var services = new ServiceCollection();
            
            // Register TenantManager and tenant store
            services.AddMultiTenancy();
            
            // Register logging
            services.AddLogging();

            var serviceProvider = services.BuildServiceProvider();
            var tenantManager = serviceProvider.GetRequiredService<TenantManager>();

            // Initialize each tenant's database
            foreach (var kvp in connectionStrings)
            {
                await InitializeTenantDatabaseAsync(kvp.Value, kvp.Key);
                
                // Register tenant with Database strategy
                await tenantManager.CreateTenantAsync(
                    tenantId: kvp.Key,
                    name: GetTenantDisplayName(kvp.Key),
                    isolationStrategy: TenantIsolationStrategy.Database,
                    connectionString: kvp.Value
                );
            }

            // Run the sample
            var sample = new DatabasePerTenantSample(tenantManager, connectionStrings);
            await sample.RunAllDemosAsync();

            // Wait for user input before returning to menu
            Console.WriteLine("Press any key to return to the menu...");
            Console.ReadKey();
        }
    }

    /// <summary>
    /// Initializes a tenant's database schema.
    /// Each tenant gets their own complete database with full schema.
    /// </summary>
    private async Task InitializeTenantDatabaseAsync(string connectionString, string tenantId)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Create products table
        // Note: tenant_id column included for compatibility with Product entity
        // In pure database-per-tenant, this column is redundant but harmless
        var createProductsTable = @"
            CREATE TABLE IF NOT EXISTS products (
                id BIGSERIAL PRIMARY KEY,
                tenant_id VARCHAR(100) DEFAULT 'default',
                name VARCHAR(255) NOT NULL,
                description TEXT,
                category_name VARCHAR(100),
                price DECIMAL(10, 2) NOT NULL,
                stock_quantity INTEGER NOT NULL DEFAULT 0,
                category_id BIGINT,
                is_active BOOLEAN NOT NULL DEFAULT true,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            CREATE INDEX IF NOT EXISTS idx_products_active ON products(is_active);
            CREATE INDEX IF NOT EXISTS idx_products_category ON products(category_id);
        ";

        // Create categories table
        // Note: tenant_id column included for compatibility with Category entity
        var createCategoriesTable = @"
            CREATE TABLE IF NOT EXISTS categories (
                id BIGSERIAL PRIMARY KEY,
                tenant_id VARCHAR(100) DEFAULT 'default',
                name VARCHAR(255) NOT NULL,
                description TEXT,
                parent_category_id BIGINT,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (parent_category_id) REFERENCES categories(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_categories_parent ON categories(parent_category_id);
        ";

        // Create tenant_info table for metadata
        var createTenantInfoTable = @"
            CREATE TABLE IF NOT EXISTS tenant_info (
                id BIGSERIAL PRIMARY KEY,
                tenant_id VARCHAR(100) NOT NULL UNIQUE,
                tenant_name VARCHAR(255) NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                data_residency_region VARCHAR(100),
                compliance_level VARCHAR(50)
            );
        ";

        await using var command = connection.CreateCommand();

        command.CommandText = createProductsTable;
        await command.ExecuteNonQueryAsync();

        command.CommandText = createCategoriesTable;
        await command.ExecuteNonQueryAsync();

        command.CommandText = createTenantInfoTable;
        await command.ExecuteNonQueryAsync();

        // Insert tenant metadata
        command.CommandText = @"
            INSERT INTO tenant_info (tenant_id, tenant_name, created_at, data_residency_region, compliance_level)
            VALUES (@tenantId, @tenantName, @createdAt, @region, @compliance)
            ON CONFLICT (tenant_id) DO NOTHING;
        ";
        command.Parameters.AddWithValue("tenantId", tenantId);
        command.Parameters.AddWithValue("tenantName", GetTenantDisplayName(tenantId));
        command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("region", GetTenantRegion(tenantId));
        command.Parameters.AddWithValue("compliance", GetComplianceLevel(tenantId));
        await command.ExecuteNonQueryAsync();

        Console.WriteLine($"✓ Database initialized for tenant: {tenantId}");
        Console.WriteLine($"  └─ Database: {connection.Database}");
        Console.WriteLine($"  └─ Tables: products, categories, tenant_info");
        Console.WriteLine($"  └─ Note: tenant_id columns included for entity compatibility");
    }

    private string GetTenantDisplayName(string tenantId) => tenantId switch
    {
        "acme-corp" => "Acme Corporation",
        "contoso-ltd" => "Contoso Ltd",
        "fabrikam-inc" => "Fabrikam Inc",
        _ => tenantId
    };

    private string GetTenantRegion(string tenantId) => tenantId switch
    {
        "acme-corp" => "US-East",
        "contoso-ltd" => "EU-West",
        "fabrikam-inc" => "APAC-Singapore",
        _ => "Unknown"
    };

    private string GetComplianceLevel(string tenantId) => tenantId switch
    {
        "acme-corp" => "SOC2",
        "contoso-ltd" => "GDPR",
        "fabrikam-inc" => "ISO27001",
        _ => "Standard"
    };
}
