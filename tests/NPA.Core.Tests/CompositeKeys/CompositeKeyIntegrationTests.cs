using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Providers.SqlServer;
using Testcontainers.MsSql;
using Xunit;

namespace NPA.Core.Tests.CompositeKeys;

/// <summary>
/// Collection definition to prevent parallel test execution for Composite Key integration tests.
/// </summary>
[CollectionDefinition("Composite Key Integration Tests", DisableParallelization = true)]
public class CompositeKeyIntegrationTestsCollection
{
}

/// <summary>
/// Integration tests for composite key support with actual database.
/// </summary>
[Collection("Composite Key Integration Tests")]
public class CompositeKeyIntegrationTests : IAsyncLifetime
{
    private MsSqlContainer? _container;
    private IDbConnection? _connection;
    private IEntityManager? _entityManager;

    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder()
            .WithPassword("YourStrong@Passw0rd")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();
        _connection = new SqlConnection(connectionString);
        _connection.Open();

        // Create table
        await CreateTestTable();

        // Setup EntityManager
        var metadataProvider = new MetadataProvider();
        var databaseProvider = new SqlServerProvider();
        _entityManager = new EntityManager(_connection, metadataProvider, databaseProvider, NullLogger<EntityManager>.Instance);
    }

    public async Task DisposeAsync()
    {
        _connection?.Dispose();
        
        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    [Fact]
    public async Task EntityManager_FindAsync_CompositeKey_ShouldFindEntity()
    {
        // Arrange
        var entity = new OrderItemTestEntity
        {
            OrderId = 1L,
            ProductId = 100L,
            Quantity = 5,
            UnitPrice = 29.99m
        };

        await _entityManager!.PersistAsync(entity);

        var key = CompositeKeyBuilder.Create()
            .WithKey("OrderId", 1L)
            .WithKey("ProductId", 100L)
            .Build();

        // Act
        var found = await _entityManager.FindAsync<OrderItemTestEntity>(key);

        // Assert
        Assert.NotNull(found);
        Assert.Equal(1L, found.OrderId);
        Assert.Equal(100L, found.ProductId);
        Assert.Equal(5, found.Quantity);
        Assert.Equal(29.99m, found.UnitPrice);
    }

    [Fact]
    public void EntityManager_Find_CompositeKey_Sync_ShouldFindEntity()
    {
        // Arrange
        var entity = new OrderItemTestEntity
        {
            OrderId = 2L,
            ProductId = 200L,
            Quantity = 10,
            UnitPrice = 49.99m
        };

        _entityManager!.Persist(entity);

        var key = CompositeKeyBuilder.Create()
            .WithKey("OrderId", 2L)
            .WithKey("ProductId", 200L)
            .Build();

        // Act
        var found = _entityManager.Find<OrderItemTestEntity>(key);

        // Assert
        Assert.NotNull(found);
        Assert.Equal(2L, found.OrderId);
        Assert.Equal(200L, found.ProductId);
        Assert.Equal(10, found.Quantity);
    }

    [Fact]
    public async Task EntityManager_RemoveAsync_CompositeKey_ShouldDeleteEntity()
    {
        // Arrange
        var entity = new OrderItemTestEntity
        {
            OrderId = 3L,
            ProductId = 300L,
            Quantity = 3,
            UnitPrice = 19.99m
        };

        await _entityManager!.PersistAsync(entity);

        var key = CompositeKeyBuilder.Create()
            .WithKey("OrderId", 3L)
            .WithKey("ProductId", 300L)
            .Build();

        // Act
        await _entityManager.RemoveAsync<OrderItemTestEntity>(key);

        // Assert
        var found = await _entityManager.FindAsync<OrderItemTestEntity>(key);
        Assert.Null(found);
    }

    [Fact]
    public void EntityManager_Remove_CompositeKey_Sync_ShouldDeleteEntity()
    {
        // Arrange
        var entity = new OrderItemTestEntity
        {
            OrderId = 4L,
            ProductId = 400L,
            Quantity = 7,
            UnitPrice = 99.99m
        };

        _entityManager!.Persist(entity);

        var key = CompositeKeyBuilder.Create()
            .WithKey("OrderId", 4L)
            .WithKey("ProductId", 400L)
            .Build();

        // Act
        _entityManager.Remove<OrderItemTestEntity>(key);

        // Assert
        var found = _entityManager.Find<OrderItemTestEntity>(key);
        Assert.Null(found);
    }

    [Fact]
    public async Task EntityManager_Persist_CompositeKeyEntity_ShouldInsert()
    {
        // Arrange
        var entity = new OrderItemTestEntity
        {
            OrderId = 5L,
            ProductId = 500L,
            Quantity = 15,
            UnitPrice = 39.99m,
            Discount = 5.00m
        };

        // Act
        await _entityManager!.PersistAsync(entity);

        // Assert
        var key = CompositeKeyBuilder.Create()
            .WithKey("OrderId", 5L)
            .WithKey("ProductId", 500L)
            .Build();

        var found = await _entityManager.FindAsync<OrderItemTestEntity>(key);
        Assert.NotNull(found);
        Assert.Equal(15, found.Quantity);
        Assert.Equal(39.99m, found.UnitPrice);
        Assert.Equal(5.00m, found.Discount);
    }

    [Fact]
    public async Task EntityManager_Merge_CompositeKeyEntity_ShouldUpdate()
    {
        // Arrange
        var entity = new OrderItemTestEntity
        {
            OrderId = 6L,
            ProductId = 600L,
            Quantity = 2,
            UnitPrice = 15.99m
        };

        await _entityManager!.PersistAsync(entity);

        // Act
        var found = await _entityManager.FindAsync<OrderItemTestEntity>(
            CompositeKeyBuilder.Create()
                .WithKey("OrderId", 6L)
                .WithKey("ProductId", 600L)
                .Build());

        Assert.NotNull(found);
        found.Quantity = 20;
        found.UnitPrice = 12.99m;

        await _entityManager.MergeAsync(found);

        // Assert
        var updated = await _entityManager.FindAsync<OrderItemTestEntity>(
            CompositeKeyBuilder.Create()
                .WithKey("OrderId", 6L)
                .WithKey("ProductId", 600L)
                .Build());

        Assert.NotNull(updated);
        Assert.Equal(20, updated.Quantity);
        Assert.Equal(12.99m, updated.UnitPrice);
    }

    private async Task CreateTestTable()
    {
        var createTableSql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'order_items')
            BEGIN
                CREATE TABLE order_items (
                    order_id BIGINT NOT NULL,
                    product_id BIGINT NOT NULL,
                    quantity INT NOT NULL,
                    unit_price DECIMAL(10, 2) NOT NULL,
                    discount DECIMAL(10, 2) NULL,
                    PRIMARY KEY (order_id, product_id)
                );
            END";

        if (_connection is SqlConnection sqlConnection)
        {
            using var command = new SqlCommand(createTableSql, sqlConnection);
            await command.ExecuteNonQueryAsync();
        }
    }
}

