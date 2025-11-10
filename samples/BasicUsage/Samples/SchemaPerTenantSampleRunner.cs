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
/// Sample wrapper for Schema Per Tenant multi-tenancy strategy.
/// Demonstrates tenant isolation using separate schemas within a single database.
/// Implements ISample for automatic discovery by SampleRunner.
/// </summary>
public class SchemaPerTenantSampleRunner : ISample
{
    public string Name => "Schema Per Tenant (Phase 5.5)";

    public string Description => "Demonstrates tenant isolation using separate database schemas, balancing isolation with infrastructure efficiency";

    public async Task RunAsync()
    {
        var postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("npa_schema_multitenancy")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithCleanUp(true)
            .Build();

        await using (postgresContainer)
        {
            Console.WriteLine("Starting PostgreSQL container...");
            await postgresContainer.StartAsync();
            Console.WriteLine("PostgreSQL container started.\n");

            var connectionString = postgresContainer.GetConnectionString();

            // Setup dependency injection
            var services = new ServiceCollection();
            
            // Register TenantManager and tenant store
            services.AddMultiTenancy();
            
            // Register logging
            services.AddLogging();

            var serviceProvider = services.BuildServiceProvider();
            var tenantManager = serviceProvider.GetRequiredService<TenantManager>();

            // Define tenants with their schemas
            var tenants = new Dictionary<string, string>
            {
                ["acme-corp"] = "acme_schema",
                ["contoso-ltd"] = "contoso_schema",
                ["fabrikam-inc"] = "fabrikam_schema"
            };

            // Initialize database and create schemas for each tenant
            await InitializeDatabaseAsync(connectionString, tenants);

            // Register tenants with Schema strategy
            foreach (var kvp in tenants)
            {
                await tenantManager.CreateTenantAsync(
                    tenantId: kvp.Key,
                    name: GetTenantDisplayName(kvp.Key),
                    isolationStrategy: TenantIsolationStrategy.Schema,
                    connectionString: connectionString,
                    schema: kvp.Value
                );
            }

            // Run the sample
            var sample = new SchemaPerTenantSample(tenantManager, connectionString, tenants);
            await sample.RunAllDemosAsync();

            // Wait for user input before returning to menu
            Console.WriteLine("Press any key to return to the menu...");
            Console.ReadKey();
        }
    }

    /// <summary>
    /// Initializes the database and creates a schema for each tenant.
    /// Each tenant gets their own schema with complete table set.
    /// </summary>
    private async Task InitializeDatabaseAsync(string connectionString, Dictionary<string, string> tenants)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        Console.WriteLine("Creating schemas for each tenant...\n");

        foreach (var kvp in tenants)
        {
            var tenantId = kvp.Key;
            var schemaName = kvp.Value;

            await using var command = connection.CreateCommand();

            // Create schema
            command.CommandText = $"CREATE SCHEMA IF NOT EXISTS {schemaName};";
            await command.ExecuteNonQueryAsync();

            // Create products table in tenant schema
            // Note: tenant_id column included for compatibility with Product entity
            command.CommandText = $@"
                CREATE TABLE IF NOT EXISTS {schemaName}.products (
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

                CREATE INDEX IF NOT EXISTS idx_products_active ON {schemaName}.products(is_active);
                CREATE INDEX IF NOT EXISTS idx_products_category ON {schemaName}.products(category_id);
            ";
            await command.ExecuteNonQueryAsync();

            // Create categories table in tenant schema
            // Note: tenant_id column included for compatibility with Category entity
            command.CommandText = $@"
                CREATE TABLE IF NOT EXISTS {schemaName}.categories (
                    id BIGSERIAL PRIMARY KEY,
                    tenant_id VARCHAR(100) DEFAULT 'default',
                    name VARCHAR(255) NOT NULL,
                    description TEXT,
                    parent_category_id BIGINT,
                    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (parent_category_id) REFERENCES {schemaName}.categories(id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_categories_parent ON {schemaName}.categories(parent_category_id);
            ";
            await command.ExecuteNonQueryAsync();

            // Create tenant_info table in tenant schema
            command.CommandText = $@"
                CREATE TABLE IF NOT EXISTS {schemaName}.tenant_info (
                    id BIGSERIAL PRIMARY KEY,
                    tenant_id VARCHAR(100) NOT NULL,
                    tenant_name VARCHAR(255) NOT NULL,
                    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    max_users INTEGER NOT NULL DEFAULT 100,
                    storage_quota_gb INTEGER NOT NULL DEFAULT 50
                );

                INSERT INTO {schemaName}.tenant_info (tenant_id, tenant_name, created_at, max_users, storage_quota_gb)
                VALUES (@tenantId, @tenantName, @createdAt, @maxUsers, @storageQuota)
                ON CONFLICT DO NOTHING;
            ";
            command.Parameters.AddWithValue("tenantId", tenantId);
            command.Parameters.AddWithValue("tenantName", GetTenantDisplayName(tenantId));
            command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("maxUsers", GetMaxUsers(tenantId));
            command.Parameters.AddWithValue("storageQuota", GetStorageQuota(tenantId));
            await command.ExecuteNonQueryAsync();

            Console.WriteLine($"✓ Schema created: {schemaName}");
            Console.WriteLine($"  └─ Tenant: {tenantId}");
            Console.WriteLine($"  └─ Tables: products, categories, tenant_info");
        }

        Console.WriteLine("\n✓ All schemas initialized successfully\n");
    }

    private string GetTenantDisplayName(string tenantId) => tenantId switch
    {
        "acme-corp" => "Acme Corporation",
        "contoso-ltd" => "Contoso Ltd",
        "fabrikam-inc" => "Fabrikam Inc",
        _ => tenantId
    };

    private int GetMaxUsers(string tenantId) => tenantId switch
    {
        "acme-corp" => 500,
        "contoso-ltd" => 200,
        "fabrikam-inc" => 100,
        _ => 50
    };

    private int GetStorageQuota(string tenantId) => tenantId switch
    {
        "acme-corp" => 200,
        "contoso-ltd" => 100,
        "fabrikam-inc" => 50,
        _ => 25
    };
}
