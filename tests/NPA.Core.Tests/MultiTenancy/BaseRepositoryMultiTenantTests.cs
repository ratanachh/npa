using System.Data;
using Dapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Core.MultiTenancy;
using NPA.Core.Providers;
using NPA.Core.Repositories;
using Xunit;

namespace NPA.Core.Tests.MultiTenancy;

public class BaseRepositoryMultiTenantTests : IDisposable
{
    private readonly IDbConnection _connection;
    private readonly IMetadataProvider _metadataProvider;
    private readonly IDatabaseProvider _databaseProvider;
    private readonly IEntityManager _entityManager;
    private readonly TestTenantProvider _tenantProvider;

    public BaseRepositoryMultiTenantTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        
        _metadataProvider = new MetadataProvider();
        _databaseProvider = new NPA.Providers.Sqlite.SqliteProvider();
        _tenantProvider = new TestTenantProvider();
        _entityManager = new EntityManager(_connection, _metadataProvider, _databaseProvider, null, _tenantProvider);
        
        // Create tables
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
            CREATE TABLE GlobalSettings (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TenantId TEXT NOT NULL,
                Key TEXT NOT NULL,
                Value TEXT NOT NULL
            )");
        
        _connection.Execute(@"
            CREATE TABLE SharedData (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Value TEXT NOT NULL
            )");
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByTenant_WhenTenantContextSet()
    {
        // Arrange
        _tenantProvider.SetCurrentTenant("tenant1");
        var repository = new BaseRepository<Product>(_connection, _entityManager, _metadataProvider, _tenantProvider);
        
        // Insert test data for multiple tenants
        _connection.Execute("INSERT INTO Products (TenantId, Name, Price) VALUES ('tenant1', 'Product A', 10.0)");
        _connection.Execute("INSERT INTO Products (TenantId, Name, Price) VALUES ('tenant1', 'Product B', 20.0)");
        _connection.Execute("INSERT INTO Products (TenantId, Name, Price) VALUES ('tenant2', 'Product C', 30.0)");

        // Act
        var results = await repository.GetAllAsync();

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(p => p.TenantId == "tenant1");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmpty_WhenNoDataForCurrentTenant()
    {
        // Arrange
        _tenantProvider.SetCurrentTenant("tenant3");
        var repository = new BaseRepository<Product>(_connection, _entityManager, _metadataProvider, _tenantProvider);
        
        _connection.Execute("INSERT INTO Products (TenantId, Name, Price) VALUES ('tenant1', 'Product A', 10.0)");

        // Act
        var results = await repository.GetAllAsync();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task FindAsync_ShouldFilterByTenantAndPredicate()
    {
        // Arrange
        _tenantProvider.SetCurrentTenant("tenant1");
        var repository = new BaseRepository<Product>(_connection, _entityManager, _metadataProvider, _tenantProvider);
        
        _connection.Execute("INSERT INTO Products (TenantId, Name, Price) VALUES ('tenant1', 'Product A', 10.0)");
        _connection.Execute("INSERT INTO Products (TenantId, Name, Price) VALUES ('tenant1', 'Product B', 20.0)");
        _connection.Execute("INSERT INTO Products (TenantId, Name, Price) VALUES ('tenant2', 'Product A', 15.0)");

        // Act
        var results = await repository.FindAsync(p => p.Name == "Product A");

        // Assert
        results.Should().HaveCount(1);
        results.First().TenantId.Should().Be("tenant1");
        results.First().Price.Should().Be(10.0m);
    }

    [Fact]
    public async Task CountAsync_ShouldCountOnlyCurrentTenantEntities()
    {
        // Arrange
        _tenantProvider.SetCurrentTenant("tenant1");
        var repository = new BaseRepository<Product>(_connection, _entityManager, _metadataProvider, _tenantProvider);
        
        _connection.Execute("INSERT INTO Products (TenantId, Name, Price) VALUES ('tenant1', 'Product A', 10.0)");
        _connection.Execute("INSERT INTO Products (TenantId, Name, Price) VALUES ('tenant1', 'Product B', 20.0)");
        _connection.Execute("INSERT INTO Products (TenantId, Name, Price) VALUES ('tenant2', 'Product C', 30.0)");

        // Act
        var count = await repository.CountAsync();

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task WithoutTenantFilterAsync_ShouldReturnAllRecords_WhenAllowed()
    {
        // Arrange
        _tenantProvider.SetCurrentTenant("tenant1");
        var repository = new BaseRepository<GlobalSetting>(_connection, _entityManager, _metadataProvider, _tenantProvider);
        
        _connection.Execute("INSERT INTO GlobalSettings (TenantId, Key, Value) VALUES ('tenant1', 'Key1', 'Value1')");
        _connection.Execute("INSERT INTO GlobalSettings (TenantId, Key, Value) VALUES ('tenant2', 'Key2', 'Value2')");
        _connection.Execute("INSERT INTO GlobalSettings (TenantId, Key, Value) VALUES ('tenant3', 'Key3', 'Value3')");

        // Act
        var results = await repository.WithoutTenantFilterAsync(async () => await repository.GetAllAsync());

        // Assert
        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task WithoutTenantFilterAsync_ShouldThrow_WhenCrossTenantQueriesNotAllowed()
    {
        // Arrange
        _tenantProvider.SetCurrentTenant("tenant1");
        var repository = new BaseRepository<Product>(_connection, _entityManager, _metadataProvider, _tenantProvider);

        // Act & Assert
        var act = async () => await repository.WithoutTenantFilterAsync(async () => await repository.GetAllAsync());
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cross-tenant queries are not allowed*");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAll_WhenNoTenantProviderSet()
    {
        // Arrange - Repository without tenant provider
        var repository = new BaseRepository<Product>(_connection, _entityManager, _metadataProvider, null);
        
        _connection.Execute("INSERT INTO Products (TenantId, Name, Price) VALUES ('tenant1', 'Product A', 10.0)");
        _connection.Execute("INSERT INTO Products (TenantId, Name, Price) VALUES ('tenant2', 'Product B', 20.0)");

        // Act
        var results = await repository.GetAllAsync();

        // Assert - Should return all since no tenant filtering
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAll_ForNonMultiTenantEntity()
    {
        // Arrange
        _tenantProvider.SetCurrentTenant("tenant1");
        var repository = new BaseRepository<SharedData>(_connection, _entityManager, _metadataProvider, _tenantProvider);
        
        _connection.Execute("INSERT INTO SharedData (Value) VALUES ('Data1')");
        _connection.Execute("INSERT INTO SharedData (Value) VALUES ('Data2')");

        // Act
        var results = await repository.GetAllAsync();

        // Assert - Should return all since entity doesn't have [MultiTenant]
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task TenantSwitch_ShouldFilterDifferentData()
    {
        // Arrange
        var repository = new BaseRepository<Product>(_connection, _entityManager, _metadataProvider, _tenantProvider);
        
        _connection.Execute("INSERT INTO Products (TenantId, Name, Price) VALUES ('tenant1', 'Product A', 10.0)");
        _connection.Execute("INSERT INTO Products (TenantId, Name, Price) VALUES ('tenant2', 'Product B', 20.0)");

        // Act & Assert - Tenant 1
        _tenantProvider.SetCurrentTenant("tenant1");
        var results1 = await repository.GetAllAsync();
        results1.Should().HaveCount(1);
        results1.First().Name.Should().Be("Product A");

        // Act & Assert - Tenant 2
        _tenantProvider.SetCurrentTenant("tenant2");
        var results2 = await repository.GetAllAsync();
        results2.Should().HaveCount(1);
        results2.First().Name.Should().Be("Product B");
    }

    [Fact]
    public async Task FindAsync_WithCustomTenantProperty_ShouldFilterCorrectly()
    {
        // Arrange
        _tenantProvider.SetCurrentTenant("org1");
        var repository = new BaseRepository<Order>(_connection, _entityManager, _metadataProvider, _tenantProvider);
        
        _connection.Execute("INSERT INTO Orders (OrganizationId, OrderNumber, Total) VALUES ('org1', 'ORD-001', 100.0)");
        _connection.Execute("INSERT INTO Orders (OrganizationId, OrderNumber, Total) VALUES ('org2', 'ORD-002', 200.0)");

        // Act
        var results = await repository.GetAllAsync();

        // Assert
        results.Should().HaveCount(1);
        results.First().OrganizationId.Should().Be("org1");
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}

/// <summary>
/// Simple test implementation of ITenantProvider for testing.
/// </summary>
public class TestTenantProvider : ITenantProvider
{
    private string? _currentTenantId;

    public string? GetCurrentTenantId() => _currentTenantId;

    public TenantContext? GetCurrentTenant() => _currentTenantId != null
        ? new TenantContext { TenantId = _currentTenantId }
        : null;

    public void SetCurrentTenant(string tenantId) => _currentTenantId = tenantId;

    public void ClearCurrentTenant() => _currentTenantId = null;
}
