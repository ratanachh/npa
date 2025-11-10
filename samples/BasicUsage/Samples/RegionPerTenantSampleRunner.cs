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
/// Sample wrapper for Region Per Tenant multi-tenancy strategy.
/// Demonstrates geographic tenant isolation using separate databases per region.
/// Implements ISample for automatic discovery by SampleRunner.
/// </summary>
public class RegionPerTenantSampleRunner : ISample
{
    public string Name => "Region Per Tenant (Phase 5.5)";

    public string Description => "Demonstrates geographic isolation with separate databases per region for data residency compliance and latency optimization";

    public async Task RunAsync()
    {
        // Simulate different regional databases
        // In production, these would be in different geographic locations
        var usEastContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("npa_region_us_east")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithCleanUp(true)
            .Build();

        var euWestContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("npa_region_eu_west")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithCleanUp(true)
            .Build();

        var apacSingaporeContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("npa_region_apac_singapore")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithCleanUp(true)
            .Build();

        await using (usEastContainer)
        await using (euWestContainer)
        await using (apacSingaporeContainer)
        {
            Console.WriteLine("Starting PostgreSQL containers for each region...");
            Console.WriteLine("  (In production, these would be in different geographic locations)");
            await Task.WhenAll(
                usEastContainer.StartAsync(),
                euWestContainer.StartAsync(),
                apacSingaporeContainer.StartAsync()
            );
            Console.WriteLine("All regional databases started.\n");

            // Map regions to connection strings
            var regions = new Dictionary<string, RegionInfo>
            {
                ["US-East"] = new RegionInfo
                {
                    Name = "US-East",
                    Location = "Virginia, USA",
                    ConnectionString = usEastContainer.GetConnectionString(),
                    ComplianceFramework = "SOC2, HIPAA",
                    Tenants = new[] { "acme-corp", "tesla-motors" }
                },
                ["EU-West"] = new RegionInfo
                {
                    Name = "EU-West",
                    Location = "Ireland, EU",
                    ConnectionString = euWestContainer.GetConnectionString(),
                    ComplianceFramework = "GDPR",
                    Tenants = new[] { "contoso-ltd", "siemens-ag" }
                },
                ["APAC-Singapore"] = new RegionInfo
                {
                    Name = "APAC-Singapore",
                    Location = "Singapore",
                    ConnectionString = apacSingaporeContainer.GetConnectionString(),
                    ComplianceFramework = "PDPA, ISO27001",
                    Tenants = new[] { "fabrikam-inc", "toyota-apac" }
                }
            };

            // Setup dependency injection
            var services = new ServiceCollection();
            services.AddMultiTenancy();
            services.AddLogging();

            var serviceProvider = services.BuildServiceProvider();
            var tenantManager = serviceProvider.GetRequiredService<TenantManager>();

            // Initialize each regional database and register tenants
            foreach (var region in regions.Values)
            {
                await InitializeRegionalDatabaseAsync(region);

                foreach (var tenantId in region.Tenants)
                {
                    // Register tenant with Database strategy and regional connection
                    await tenantManager.CreateTenantAsync(
                        tenantId: tenantId,
                        name: GetTenantDisplayName(tenantId),
                        isolationStrategy: TenantIsolationStrategy.Database,
                        connectionString: region.ConnectionString
                    );
                }
            }

            // Run the sample
            var sample = new RegionPerTenantSample(tenantManager, regions);
            await sample.RunAllDemosAsync();

            // Wait for user input before returning to menu
            Console.WriteLine("Press any key to return to the menu...");
            Console.ReadKey();
        }
    }

    /// <summary>
    /// Initializes a regional database with schema and metadata.
    /// </summary>
    private async Task InitializeRegionalDatabaseAsync(RegionInfo region)
    {
        await using var connection = new NpgsqlConnection(region.ConnectionString);
        await connection.OpenAsync();

        // Create products table
        var createProductsTable = @"
            CREATE TABLE IF NOT EXISTS products (
                id BIGSERIAL PRIMARY KEY,
                tenant_id VARCHAR(100) NOT NULL,
                name VARCHAR(255) NOT NULL,
                description TEXT,
                category_name VARCHAR(100),
                price DECIMAL(10, 2) NOT NULL,
                stock_quantity INTEGER NOT NULL DEFAULT 0,
                category_id BIGINT,
                is_active BOOLEAN NOT NULL DEFAULT true,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            CREATE INDEX IF NOT EXISTS idx_products_tenant ON products(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_products_active ON products(is_active);
            CREATE INDEX IF NOT EXISTS idx_products_category ON products(category_id);
        ";

        // Create categories table
        var createCategoriesTable = @"
            CREATE TABLE IF NOT EXISTS categories (
                id BIGSERIAL PRIMARY KEY,
                tenant_id VARCHAR(100) NOT NULL,
                name VARCHAR(255) NOT NULL,
                description TEXT,
                parent_category_id BIGINT,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (parent_category_id) REFERENCES categories(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_categories_tenant ON categories(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_categories_parent ON categories(parent_category_id);
        ";

        // Create regional metadata table
        var createRegionInfoTable = @"
            CREATE TABLE IF NOT EXISTS region_info (
                id BIGSERIAL PRIMARY KEY,
                region_name VARCHAR(100) NOT NULL UNIQUE,
                location VARCHAR(255) NOT NULL,
                compliance_framework VARCHAR(255),
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                last_health_check TIMESTAMP,
                status VARCHAR(50) DEFAULT 'active'
            );
        ";

        // Create tenant metadata table
        var createTenantInfoTable = @"
            CREATE TABLE IF NOT EXISTS tenant_info (
                id BIGSERIAL PRIMARY KEY,
                tenant_id VARCHAR(100) NOT NULL UNIQUE,
                tenant_name VARCHAR(255) NOT NULL,
                region_name VARCHAR(100) NOT NULL,
                data_residency_required BOOLEAN DEFAULT false,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
        ";

        await using var command = connection.CreateCommand();

        command.CommandText = createProductsTable;
        await command.ExecuteNonQueryAsync();

        command.CommandText = createCategoriesTable;
        await command.ExecuteNonQueryAsync();

        command.CommandText = createRegionInfoTable;
        await command.ExecuteNonQueryAsync();

        command.CommandText = createTenantInfoTable;
        await command.ExecuteNonQueryAsync();

        // Insert region metadata
        command.CommandText = @"
            INSERT INTO region_info (region_name, location, compliance_framework, created_at, last_health_check, status)
            VALUES (@regionName, @location, @compliance, @createdAt, @healthCheck, @status)
            ON CONFLICT (region_name) DO UPDATE 
            SET last_health_check = @healthCheck;
        ";
        command.Parameters.AddWithValue("regionName", region.Name);
        command.Parameters.AddWithValue("location", region.Location);
        command.Parameters.AddWithValue("compliance", region.ComplianceFramework);
        command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("healthCheck", DateTime.UtcNow);
        command.Parameters.AddWithValue("status", "active");
        await command.ExecuteNonQueryAsync();

        // Insert tenant metadata for each tenant in this region
        foreach (var tenantId in region.Tenants)
        {
            command.Parameters.Clear();
            command.CommandText = @"
                INSERT INTO tenant_info (tenant_id, tenant_name, region_name, data_residency_required, created_at)
                VALUES (@tenantId, @tenantName, @regionName, @dataResidency, @createdAt)
                ON CONFLICT (tenant_id) DO NOTHING;
            ";
            command.Parameters.AddWithValue("tenantId", tenantId);
            command.Parameters.AddWithValue("tenantName", GetTenantDisplayName(tenantId));
            command.Parameters.AddWithValue("regionName", region.Name);
            command.Parameters.AddWithValue("dataResidency", IsDataResidencyRequired(tenantId));
            command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            await command.ExecuteNonQueryAsync();
        }

        Console.WriteLine($"✓ Regional database initialized: {region.Name}");
        Console.WriteLine($"  └─ Location: {region.Location}");
        Console.WriteLine($"  └─ Compliance: {region.ComplianceFramework}");
        Console.WriteLine($"  └─ Tables: products, categories, region_info, tenant_info");
        Console.WriteLine($"  └─ Tenants: {string.Join(", ", region.Tenants)}");
    }

    private string GetTenantDisplayName(string tenantId) => tenantId switch
    {
        "acme-corp" => "Acme Corporation",
        "tesla-motors" => "Tesla Motors",
        "contoso-ltd" => "Contoso Ltd",
        "siemens-ag" => "Siemens AG",
        "fabrikam-inc" => "Fabrikam Inc",
        "toyota-apac" => "Toyota APAC",
        _ => tenantId
    };

    private bool IsDataResidencyRequired(string tenantId) => tenantId switch
    {
        "contoso-ltd" => true,  // GDPR requirement
        "siemens-ag" => true,   // GDPR requirement
        _ => false
    };

    public class RegionInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public string ComplianceFramework { get; set; } = string.Empty;
        public string[] Tenants { get; set; } = Array.Empty<string>();
    }
}
