using System.Data;
using Dapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Core.Providers;
using Xunit;

namespace NPA.Core.Tests.MultiTenancy;

public class EntityManagerMultiTenantTests : IDisposable
{
    private readonly IDbConnection _connection;
    private readonly IMetadataProvider _metadataProvider;
    private readonly IDatabaseProvider _databaseProvider;
    private readonly TestTenantProvider _tenantProvider;
    private readonly IEntityManager _entityManager;

    public EntityManagerMultiTenantTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        
        _metadataProvider = new MetadataProvider();
        _databaseProvider = new NPA.Providers.Sqlite.SqliteProvider();
        _tenantProvider = new TestTenantProvider();
        _entityManager = new EntityManager(_connection, _metadataProvider, _databaseProvider, null, _tenantProvider);
        
        CreateTables();
    }

    private void CreateTables()
    {
        _connection.Execute(@"
            CREATE TABLE Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TenantId TEXT NOT NULL,
                Name TEXT NOT NULL,
                Price REAL NOT NULL
            )");
        
        _connection.Execute(@"
            CREATE TABLE Orders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OrganizationId TEXT NOT NULL,
                OrderNumber TEXT NOT NULL,
                Total REAL NOT NULL
            )");
        
        _connection.Execute(@"
            CREATE TABLE LogEntries (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TenantId TEXT NOT NULL,
                Message TEXT NOT NULL,
                Timestamp TEXT NOT NULL
            )");
        
        _connection.Execute(@"
            CREATE TABLE ManualTenantEntities (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TenantId TEXT NOT NULL,
                Data TEXT NOT NULL
            )");
        
        _connection.Execute(@"
            CREATE TABLE SharedData (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Value TEXT NOT NULL
            )");
    }

    [Fact]
    public async Task PersistAsync_ShouldAutoPopulateTenantId_WhenNotSet()
    {
        // Arrange
        _tenantProvider.SetCurrentTenant("tenant1");
        var product = new Product { Name = "Test Product", Price = 99.99m };

        // Act
        await _entityManager.PersistAsync(product);

        // Assert
        product.TenantId.Should().Be("tenant1");
        product.Id.Should().BeGreaterThan(0);
        
        // Verify in database
        var saved = _connection.QuerySingle<Product>("SELECT * FROM Products WHERE Id = @Id", new { product.Id });
        saved.TenantId.Should().Be("tenant1");
    }

    [Fact]
    public async Task PersistAsync_ShouldAutoPopulateCustomTenantProperty()
    {
        // Arrange
        _tenantProvider.SetCurrentTenant("org1");
        var order = new Order { OrderNumber = "ORD-001", Total = 500.0m };

        // Act
        await _entityManager.PersistAsync(order);

        // Assert
        order.OrganizationId.Should().Be("org1");
        
        var saved = _connection.QuerySingle<Order>("SELECT * FROM Orders WHERE Id = @Id", new { order.Id });
        saved.OrganizationId.Should().Be("org1");
    }

    [Fact]
    public async Task PersistAsync_ShouldNotOverwriteExistingTenantId()
    {
        // Arrange
        _tenantProvider.SetCurrentTenant("tenant1");
        var product = new Product { TenantId = "tenant2", Name = "Test Product", Price = 99.99m };

        // Act
        await _entityManager.PersistAsync(product);

        // Assert - Should keep the explicitly set tenant
        product.TenantId.Should().Be("tenant2");
    }

    [Fact]
    public async Task PersistAsync_ShouldNotPopulateTenantId_WhenAutoPopulateDisabled()
    {
        // Arrange
        _tenantProvider.SetCurrentTenant("tenant1");
        var entity = new ManualTenantEntity { TenantId = "manual-tenant", Data = "Test Data" };

        // Act
        await _entityManager.PersistAsync(entity);

        // Assert - Should keep the manually set value, not auto-populate
        entity.TenantId.Should().Be("manual-tenant");
        entity.Id.Should().BeGreaterThan(0);
        
        var saved = _connection.QuerySingle<ManualTenantEntity>("SELECT * FROM ManualTenantEntities WHERE Id = @Id", new { entity.Id });
        saved.TenantId.Should().Be("manual-tenant");
    }

    [Fact]
    public async Task PersistAsync_ShouldWork_ForNonMultiTenantEntity()
    {
        // Arrange
        _tenantProvider.SetCurrentTenant("tenant1");
        var data = new SharedData { Value = "Shared Value" };

        // Act
        await _entityManager.PersistAsync(data);

        // Assert
        data.Id.Should().BeGreaterThan(0);
        var saved = _connection.QuerySingle<SharedData>("SELECT * FROM SharedData WHERE Id = @Id", new { data.Id });
        saved.Value.Should().Be("Shared Value");
    }

    [Fact]
    public async Task MergeAsync_ShouldValidateTenantMatch_WhenEnabled()
    {
        // Arrange
        _tenantProvider.SetCurrentTenant("tenant1");
        var product = new Product { Name = "Test Product", Price = 99.99m };
        await _entityManager.PersistAsync(product);
        
        // Switch tenant
        _tenantProvider.SetCurrentTenant("tenant2");
        product.Price = 199.99m;

        // Act & Assert
        var act = async () => await _entityManager.MergeAsync(product);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cross-tenant modification*");
    }

    [Fact]
    public async Task MergeAsync_ShouldSucceed_WhenTenantMatches()
    {
        // Arrange
        _tenantProvider.SetCurrentTenant("tenant1");
        var product = new Product { Name = "Test Product", Price = 99.99m };
        await _entityManager.PersistAsync(product);
        
        product.Price = 199.99m;

        // Act
        await _entityManager.MergeAsync(product);

        // Assert
        var updated = _connection.QuerySingle<Product>("SELECT * FROM Products WHERE Id = @Id", new { product.Id });
        updated.Price.Should().Be(199.99m);
    }

    [Fact]
    public async Task MergeAsync_ShouldAllowCrossTenantUpdate_WhenValidationDisabled()
    {
        // Arrange
        _tenantProvider.SetCurrentTenant("tenant1");
        var logEntry = new LogEntry { Message = "Initial message", Timestamp = DateTime.UtcNow };
        await _entityManager.PersistAsync(logEntry);
        
        // Switch tenant
        _tenantProvider.SetCurrentTenant("tenant2");
        logEntry.Message = "Updated message";

        // Act - Should succeed because ValidateTenantOnWrite = false
        await _entityManager.MergeAsync(logEntry);

        // Assert
        var updated = _connection.QuerySingle<LogEntry>("SELECT * FROM LogEntries WHERE Id = @Id", new { logEntry.Id });
        updated.Message.Should().Be("Updated message");
    }

    [Fact]
    public async Task MergeAsync_ShouldWork_ForNonMultiTenantEntity()
    {
        // Arrange
        _tenantProvider.SetCurrentTenant("tenant1");
        var data = new SharedData { Value = "Original" };
        await _entityManager.PersistAsync(data);
        
        data.Value = "Updated";

        // Act
        await _entityManager.MergeAsync(data);

        // Assert
        var updated = _connection.QuerySingle<SharedData>("SELECT * FROM SharedData WHERE Id = @Id", new { data.Id });
        updated.Value.Should().Be("Updated");
    }

    [Fact]
    public async Task PersistAsync_ShouldThrow_WhenNoTenantContextAndAutoPopulateEnabled()
    {
        // Arrange
        _tenantProvider.ClearCurrentTenant();
        var product = new Product { Name = "Test Product", Price = 99.99m };

        // Act & Assert - Should fail due to NOT NULL constraint
        var act = async () => await _entityManager.PersistAsync(product);
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task EntityManager_WithoutTenantProvider_ShouldWork()
    {
        // Arrange - Create EntityManager without tenant provider
        var managerWithoutTenant = new EntityManager(_connection, _metadataProvider, _databaseProvider, null, null);
        var product = new Product { TenantId = "manual", Name = "Test Product", Price = 99.99m };

        // Act
        await managerWithoutTenant.PersistAsync(product);

        // Assert - Should work but not auto-populate
        product.Id.Should().BeGreaterThan(0);
        product.TenantId.Should().Be("manual");
    }

    [Fact]
    public async Task MultipleEntities_DifferentTenants_ShouldIsolateCorrectly()
    {
        // Arrange & Act
        _tenantProvider.SetCurrentTenant("tenant1");
        var product1 = new Product { Name = "Product 1", Price = 10.0m };
        await _entityManager.PersistAsync(product1);
        
        _tenantProvider.SetCurrentTenant("tenant2");
        var product2 = new Product { Name = "Product 2", Price = 20.0m };
        await _entityManager.PersistAsync(product2);

        // Assert
        product1.TenantId.Should().Be("tenant1");
        product2.TenantId.Should().Be("tenant2");
        
        var all = _connection.Query<Product>("SELECT * FROM Products").ToList();
        all.Should().HaveCount(2);
        all.Should().Contain(p => p.TenantId == "tenant1");
        all.Should().Contain(p => p.TenantId == "tenant2");
    }

    [Fact]
    public async Task RemoveAsync_ShouldWorkRegardlessOfTenant()
    {
        // Arrange
        _tenantProvider.SetCurrentTenant("tenant1");
        var product = new Product { Name = "Test Product", Price = 99.99m };
        await _entityManager.PersistAsync(product);
        
        _tenantProvider.SetCurrentTenant("tenant2");

        // Act - Remove should work even from different tenant (no validation on delete)
        await _entityManager.RemoveAsync<Product>(product.Id);

        // Assert
        var exists = _connection.QuerySingleOrDefault<Product>("SELECT * FROM Products WHERE Id = @Id", new { product.Id });
        exists.Should().BeNull();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
