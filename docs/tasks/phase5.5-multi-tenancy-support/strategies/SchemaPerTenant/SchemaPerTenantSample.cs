using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using NPA.Core.Annotations;
using NPA.Core.MultiTenancy;
using NPA.Core.Providers;

namespace NPA.Samples.MultiTenancy.SchemaPerTenant;

/// <summary>
/// Schema-per-tenant strategy: Each tenant has their own schema in a shared database.
/// Table names are prefixed with schema name based on current tenant context.
/// </summary>
public class SchemaPerTenantDatabaseProvider : IDatabaseProvider
{
    private readonly IDatabaseProvider _baseProvider;
    private readonly ITenantProvider _tenantProvider;

    public SchemaPerTenantDatabaseProvider(
        IDatabaseProvider baseProvider,
        ITenantProvider tenantProvider)
    {
        _baseProvider = baseProvider;
        _tenantProvider = tenantProvider;
    }

    public string GetTableName(Type entityType)
    {
        var baseTableName = _baseProvider.GetTableName(entityType);
        var tenantId = _tenantProvider.GetCurrentTenantId();
        
        if (string.IsNullOrEmpty(tenantId))
        {
            // No tenant context - use public schema
            return $"public.{baseTableName}";
        }

        // Prefix table with tenant schema
        // Example: "tenant1.Products" instead of just "Products"
        return $"{SanitizeSchemaName(tenantId)}.{baseTableName}";
    }

    public string GetColumnName(string propertyName)
    {
        return _baseProvider.GetColumnName(propertyName);
    }

    public string GetParameterPrefix()
    {
        return _baseProvider.GetParameterPrefix();
    }

    public string BuildInsertStatement(Type entityType, string[] columns)
    {
        var tableName = GetTableName(entityType);
        return _baseProvider.BuildInsertStatement(entityType, columns)
            .Replace($"INSERT INTO {_baseProvider.GetTableName(entityType)}", 
                     $"INSERT INTO {tableName}");
    }

    public string BuildUpdateStatement(Type entityType, string[] columns, string whereClause)
    {
        var tableName = GetTableName(entityType);
        return _baseProvider.BuildUpdateStatement(entityType, columns, whereClause)
            .Replace($"UPDATE {_baseProvider.GetTableName(entityType)}", 
                     $"UPDATE {tableName}");
    }

    public string BuildDeleteStatement(Type entityType, string whereClause)
    {
        var tableName = GetTableName(entityType);
        return _baseProvider.BuildDeleteStatement(entityType, whereClause)
            .Replace($"DELETE FROM {_baseProvider.GetTableName(entityType)}", 
                     $"DELETE FROM {tableName}");
    }

    public string BuildSelectStatement(Type entityType, string whereClause = "", string orderBy = "")
    {
        var tableName = GetTableName(entityType);
        return _baseProvider.BuildSelectStatement(entityType, whereClause, orderBy)
            .Replace($"FROM {_baseProvider.GetTableName(entityType)}", 
                     $"FROM {tableName}");
    }

    private static string SanitizeSchemaName(string tenantId)
    {
        // Convert tenant ID to valid SQL schema name
        // Example: "acme-corp" -> "tenant_acme_corp"
        return "tenant_" + tenantId.Replace("-", "_").Replace(" ", "_").ToLowerInvariant();
    }
}

/// <summary>
/// Example usage of schema-per-tenant strategy.
/// </summary>
public class SchemaPerTenantExample
{
    public async Task RunExample()
    {
        var connectionString = "Server=.;Database=MultiTenantDB;Integrated Security=true;";
        var tenantProvider = new AsyncLocalTenantProvider();
        
        // Setup: Create schemas for each tenant (one-time setup)
        await CreateTenantSchemas(connectionString, new[] { "acme-corp", "contoso" });
        
        // Usage: Acme Corp tenant
        tenantProvider.SetCurrentTenant("acme-corp");
        
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            
            // SQL: SELECT * FROM tenant_acme_corp.Products WHERE Price > @MinPrice
            // Same database, different schema
            var products = await connection.QueryAsync<Product>(
                "SELECT * FROM tenant_acme_corp.Products WHERE Price > @MinPrice",
                new { MinPrice = 100 });
        }

        // Usage: Contoso tenant
        tenantProvider.SetCurrentTenant("contoso");
        
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            
            // SQL: SELECT * FROM tenant_contoso.Products WHERE Price > @MinPrice
            // Same database, same table name, different schema
            var products = await connection.QueryAsync<Product>(
                "SELECT * FROM tenant_contoso.Products WHERE Price > @MinPrice",
                new { MinPrice = 100 });
        }
    }

    private async Task CreateTenantSchemas(string connectionString, string[] tenantIds)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        foreach (var tenantId in tenantIds)
        {
            var schemaName = "tenant_" + tenantId.Replace("-", "_");
            
            // Create schema if it doesn't exist
            await connection.ExecuteAsync($@"
                IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'{schemaName}')
                BEGIN
                    EXEC('CREATE SCHEMA [{schemaName}]');
                END");

            // Create tables in this schema
            await connection.ExecuteAsync($@"
                IF NOT EXISTS (SELECT * FROM sys.objects 
                              WHERE object_id = OBJECT_ID(N'[{schemaName}].[Products]') 
                              AND type in (N'U'))
                BEGIN
                    CREATE TABLE [{schemaName}].[Products] (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Name NVARCHAR(200) NOT NULL,
                        Price DECIMAL(18,2) NOT NULL
                    );
                END");
        }
    }
}

/// <summary>
/// Product entity for Schema-Per-Tenant strategy.
/// 
/// KEY DIFFERENCES FROM OTHER STRATEGIES:
/// 
/// 1. NO [MultiTenant] attribute - Schema isolation provides multi-tenancy
/// 2. NO TenantId property - Not needed since schema separates tenants
/// 3. Table name stays simple "Products" - Schema prefix added dynamically
/// 
/// SQL Generated:
///   - Discriminator: SELECT * FROM Products WHERE TenantId = 'acme-corp'
///   - Schema Per Tenant: SELECT * FROM tenant_acme_corp.Products
///   - Database Per Tenant: SELECT * FROM Products (in TenantDB_AcmeCorp)
/// 
/// Schema isolation provides the multi-tenancy through table name prefixing.
/// </summary>
[Entity]
[Table("Products")]
public class Product
{
    [Id]
    [Column("Id")]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }
    
    [Column("Name")]
    public string Name { get; set; } = string.Empty;
    
    [Column("Price")]
    public decimal Price { get; set; }
    
    // Note: NO TenantId property needed! Schema name provides isolation.
    // Compare with Discriminator Column strategy where you would have:
    // [Column("TenantId")] public string TenantId { get; set; }
}

/// <summary>
/// Repository with automatic schema switching.
/// </summary>
public class SchemaAwareProductRepository
{
    private readonly IDbConnection _connection;
    private readonly ITenantProvider _tenantProvider;

    public SchemaAwareProductRepository(
        IDbConnection connection,
        ITenantProvider tenantProvider)
    {
        _connection = connection;
        _tenantProvider = tenantProvider;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        var schema = GetCurrentSchema();
        
        // SQL dynamically includes schema prefix
        var sql = $"SELECT * FROM {schema}.Products";
        return await _connection.QueryAsync<Product>(sql);
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        var schema = GetCurrentSchema();
        
        var sql = $"SELECT * FROM {schema}.Products WHERE Id = @Id";
        return await _connection.QueryFirstOrDefaultAsync<Product>(sql, new { Id = id });
    }

    public async Task InsertAsync(Product product)
    {
        var schema = GetCurrentSchema();
        
        // Insert into tenant-specific schema
        var sql = $@"
            INSERT INTO {schema}.Products (Name, Price)
            VALUES (@Name, @Price);
            SELECT CAST(SCOPE_IDENTITY() as int)";
        
        product.Id = await _connection.ExecuteScalarAsync<int>(sql, product);
    }

    public async Task UpdateAsync(Product product)
    {
        var schema = GetCurrentSchema();
        
        var sql = $@"
            UPDATE {schema}.Products
            SET Name = @Name, Price = @Price
            WHERE Id = @Id";
        
        await _connection.ExecuteAsync(sql, product);
    }

    public async Task DeleteAsync(int id)
    {
        var schema = GetCurrentSchema();
        
        var sql = $"DELETE FROM {schema}.Products WHERE Id = @Id";
        await _connection.ExecuteAsync(sql, new { Id = id });
    }

    private string GetCurrentSchema()
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        
        if (string.IsNullOrEmpty(tenantId))
        {
            return "public"; // Default schema
        }

        // Convert tenant ID to schema name
        return "tenant_" + tenantId.Replace("-", "_").Replace(" ", "_").ToLowerInvariant();
    }
}

/// <summary>
/// SQL examples generated for each strategy
/// </summary>
public class SqlGenerationExamples
{
    public void ShowExamples()
    {
        Console.WriteLine("=== Discriminator Column Strategy (Current Implementation) ===");
        Console.WriteLine("SELECT * FROM Products WHERE TenantId = 'acme-corp'");
        Console.WriteLine("INSERT INTO Products (TenantId, Name, Price) VALUES ('acme-corp', 'Widget', 99.99)");
        Console.WriteLine();
        
        Console.WriteLine("=== Database Per Tenant Strategy ===");
        Console.WriteLine("-- Connection String: Server=.;Database=TenantDB_AcmeCorp;...");
        Console.WriteLine("SELECT * FROM Products");
        Console.WriteLine("INSERT INTO Products (Name, Price) VALUES ('Widget', 99.99)");
        Console.WriteLine();
        
        Console.WriteLine("=== Schema Per Tenant Strategy ===");
        Console.WriteLine("-- Connection String: Server=.;Database=MultiTenantDB;...");
        Console.WriteLine("SELECT * FROM tenant_acme_corp.Products");
        Console.WriteLine("INSERT INTO tenant_acme_corp.Products (Name, Price) VALUES ('Widget', 99.99)");
    }
}
