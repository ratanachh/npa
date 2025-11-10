using System.Data;
using Microsoft.Data.SqlClient;
using NPA.Core.Annotations;
using NPA.Core.MultiTenancy;

namespace NPA.Samples.MultiTenancy.DatabasePerTenant;

/// <summary>
/// Database-per-tenant strategy: Each tenant has their own isolated database.
/// Connection string changes based on current tenant context.
/// </summary>
public class DatabasePerTenantConnectionFactory : IDbConnectionFactory
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ITenantDatabaseMappingStore _mappingStore;
    private readonly string _masterConnectionString;

    public DatabasePerTenantConnectionFactory(
        ITenantProvider tenantProvider,
        ITenantDatabaseMappingStore mappingStore,
        string masterConnectionString)
    {
        _tenantProvider = tenantProvider;
        _mappingStore = mappingStore;
        _masterConnectionString = masterConnectionString;
    }

    public IDbConnection CreateConnection()
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        
        if (string.IsNullOrEmpty(tenantId))
        {
            // No tenant context - use master database
            return new SqlConnection(_masterConnectionString);
        }

        // Get tenant-specific database name
        var databaseName = _mappingStore.GetDatabaseName(tenantId);
        
        if (string.IsNullOrEmpty(databaseName))
        {
            throw new InvalidOperationException($"No database found for tenant '{tenantId}'");
        }

        // Build connection string for tenant's database
        var builder = new SqlConnectionStringBuilder(_masterConnectionString)
        {
            InitialCatalog = databaseName
        };

        return new SqlConnection(builder.ConnectionString);
    }
}

/// <summary>
/// Maps tenant IDs to their database names.
/// In production, this would typically be backed by a configuration store or database.
/// </summary>
public interface ITenantDatabaseMappingStore
{
    string? GetDatabaseName(string tenantId);
    void RegisterTenant(string tenantId, string databaseName);
}

/// <summary>
/// In-memory implementation for demonstration.
/// In production, use a database or distributed cache.
/// </summary>
public class InMemoryTenantDatabaseMappingStore : ITenantDatabaseMappingStore
{
    private readonly Dictionary<string, string> _mappings = new();

    public string? GetDatabaseName(string tenantId)
    {
        return _mappings.TryGetValue(tenantId, out var dbName) ? dbName : null;
    }

    public void RegisterTenant(string tenantId, string databaseName)
    {
        _mappings[tenantId] = databaseName;
    }
}

/// <summary>
/// Simple async-local tenant provider implementation.
/// In production, use NPA.Extensions.MultiTenancy.AsyncLocalTenantProvider
/// </summary>
public class AsyncLocalTenantProvider : ITenantProvider
{
    private static readonly AsyncLocal<string?> _currentTenantId = new();

    public string? GetCurrentTenantId() => _currentTenantId.Value;

    public TenantContext? GetCurrentTenant()
    {
        var tenantId = _currentTenantId.Value;
        return tenantId != null ? new TenantContext { TenantId = tenantId } : null;
    }

    public void SetCurrentTenant(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
        
        _currentTenantId.Value = tenantId;
    }

    public void ClearCurrentTenant()
    {
        _currentTenantId.Value = null;
    }
}

/// <summary>
/// Example usage of database-per-tenant strategy with NPA EntityManager.
/// </summary>
public class DatabasePerTenantExample
{
    public void ShowExample()
    {
        // Setup
        var tenantProvider = new AsyncLocalTenantProvider();
        var mappingStore = new InMemoryTenantDatabaseMappingStore();
        var masterConnectionString = "Server=.;Database=Master;Integrated Security=true;";
        
        // Register tenants with their databases
        mappingStore.RegisterTenant("acme-corp", "TenantDB_AcmeCorp");
        mappingStore.RegisterTenant("contoso", "TenantDB_Contoso");
        
        var connectionFactory = new DatabasePerTenantConnectionFactory(
            tenantProvider,
            mappingStore,
            masterConnectionString);

        // Configure NPA to use the tenant-aware connection factory
        // services.AddSingleton<IDbConnectionFactory>(connectionFactory);
        // services.AddScoped<ITenantProvider>(tenantProvider);

        // Usage: Acme Corp tenant
        tenantProvider.SetCurrentTenant("acme-corp");
        using (var connection = connectionFactory.CreateConnection())
        {
            connection.Open();
            
            // SQL generated: SELECT * FROM Products WHERE Price > 100
            // Database: TenantDB_AcmeCorp
            // No WHERE TenantId clause needed - entire database is for this tenant!
            
            Console.WriteLine($"Connected to: {connection.Database}");
            // Output: Connected to: TenantDB_AcmeCorp
            
            // With NPA EntityManager:
            // var products = await entityManager
            //     .CreateQuery<Product>("SELECT p FROM Product p WHERE p.Price > :minPrice")
            //     .SetParameter("minPrice", 100)
            //     .GetResultListAsync();
        }

        // Usage: Contoso tenant
        tenantProvider.SetCurrentTenant("contoso");
        using (var connection = connectionFactory.CreateConnection())
        {
            connection.Open();
            
            // SQL: SELECT * FROM Products WHERE Price > 100
            // Database: TenantDB_Contoso
            // Different database, same table structure
            
            Console.WriteLine($"Connected to: {connection.Database}");
            // Output: Connected to: TenantDB_Contoso
        }
    }
}

/// <summary>
/// Sample entity - NO TenantId property and NO [MultiTenant] attribute needed!
/// Each database contains only one tenant's data.
/// The database itself provides the isolation, not a TenantId column.
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
    
    // Note: No TenantId property needed! The database itself is the isolation boundary.
    // This is the KEY DIFFERENCE from Discriminator Column strategy where you would have:
    // [Column("TenantId")] public string TenantId { get; set; }
}

/// <summary>
/// Repository implementation for database-per-tenant strategy.
/// Much simpler than Discriminator Column strategy - no tenant filtering needed!
/// 
/// COMPARISON:
/// 
/// Discriminator Column (with [MultiTenant]):
///   - Entity has: [MultiTenant] attribute + TenantId property
///   - Repository: Auto-filters with WHERE TenantId = 'current-tenant'
///   - SQL: SELECT * FROM Products WHERE TenantId = 'acme-corp'
/// 
/// Database Per Tenant (NO [MultiTenant]):
///   - Entity has: NO [MultiTenant] attribute, NO TenantId property
///   - Repository: No filtering needed, connection is already tenant-specific
///   - SQL: SELECT * FROM Products (in TenantDB_AcmeCorp database)
/// </summary>
public class ProductRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ProductRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Get all products for current tenant.
    /// No WHERE TenantId filter needed - the database itself provides isolation!
    /// </summary>
    public IEnumerable<Product> GetAll()
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        
        // Simple query - no WHERE TenantId clause!
        // The connection is already pointing to the tenant-specific database
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Price FROM Products";
        
        var products = new List<Product>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            products.Add(new Product
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Price = reader.GetDecimal(2)
            });
        }
        
        return products;
    }

    public Product? GetById(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Price FROM Products WHERE Id = @Id";
        
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@Id";
        parameter.Value = id;
        command.Parameters.Add(parameter);
        
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new Product
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Price = reader.GetDecimal(2)
            };
        }
        
        return null;
    }

    public void Insert(Product product)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        
        // No TenantId column to populate!
        // Compare with Discriminator Column strategy:
        // INSERT INTO Products (TenantId, Name, Price) VALUES ('acme-corp', 'Widget', 99.99)
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Products (Name, Price)
            VALUES (@Name, @Price);
            SELECT CAST(SCOPE_IDENTITY() as int)";
        
        var nameParam = command.CreateParameter();
        nameParam.ParameterName = "@Name";
        nameParam.Value = product.Name;
        command.Parameters.Add(nameParam);
        
        var priceParam = command.CreateParameter();
        priceParam.ParameterName = "@Price";
        priceParam.Value = product.Price;
        command.Parameters.Add(priceParam);
        
        product.Id = (int)command.ExecuteScalar()!;
    }
}

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
