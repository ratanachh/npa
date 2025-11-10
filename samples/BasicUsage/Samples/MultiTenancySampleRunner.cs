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
/// Sample wrapper for Multi-Tenancy demonstration.
/// Implements ISample for automatic discovery by SampleRunner.
/// </summary>
public class MultiTenancySampleRunner : ISample
{
    public string Name => "Multi-Tenancy Support (Phase 5.5)";

    public string Description => "Demonstrates automatic tenant isolation, row-level security, and tenant context management";

    public async Task RunAsync()
    {
        var postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("npa_multitenancy_demo")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithCleanUp(true)
            .Build();

        await using (postgresContainer)
        {
            Console.WriteLine("Starting PostgreSQL container...");
            await postgresContainer.StartAsync();
            Console.WriteLine("PostgreSQL container started.");

            var connectionString = postgresContainer.GetConnectionString();

            // Setup dependency injection with multi-tenancy support
            var services = new ServiceCollection();
            services.AddPostgreSqlProvider(connectionString);
            
            // Register tenant provider (using AsyncLocal for thread-safe tenant context)
            services.AddScoped<ITenantProvider, AsyncLocalTenantProvider>();

            var serviceProvider = services.BuildServiceProvider();
            var entityManager = serviceProvider.GetRequiredService<IEntityManager>();
            var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();

            // Initialize database schema
            await InitializeDatabaseAsync(connectionString);

            // Run multi-tenancy demos
            var multiTenancySample = new MultiTenancySample(entityManager, tenantProvider);
            await multiTenancySample.RunAllDemosAsync();

            // Wait for user input before returning to menu
            Console.WriteLine("Press any key to return to the menu...");
            Console.ReadKey();
        }
    }

    /// <summary>
    /// Initializes the database schema for the multi-tenancy demo.
    /// </summary>
    private async Task InitializeDatabaseAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Create products table with tenant_id column
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

            -- Create index on tenant_id for performance
            CREATE INDEX IF NOT EXISTS idx_products_tenant_id ON products(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_products_tenant_active ON products(tenant_id, is_active);
        ";

        // Create categories table with tenant_id column
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

            -- Create index on tenant_id for performance
            CREATE INDEX IF NOT EXISTS idx_categories_tenant_id ON categories(tenant_id);
        ";

        await using var command = connection.CreateCommand();

        command.CommandText = createProductsTable;
        await command.ExecuteNonQueryAsync();

        command.CommandText = createCategoriesTable;
        await command.ExecuteNonQueryAsync();

        Console.WriteLine("✓ Multi-tenancy database schema initialized");
        Console.WriteLine("  └─ Products table with tenant_id column and indexes");
        Console.WriteLine("  └─ Categories table with tenant_id column and indexes\n");
    }
}
