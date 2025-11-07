using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NPA.Core.Annotations;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Core.Providers;
using System.Data;
using Xunit;

namespace NPA.Core.Tests.Bulk;

public class BulkOperationsTests
{
    private readonly Mock<IDbConnection> _mockConnection;
    private readonly Mock<IMetadataProvider> _mockMetadataProvider;
    private readonly Mock<IDatabaseProvider> _mockDatabaseProvider;
    private readonly Mock<ILogger<EntityManager>> _mockLogger;
    private readonly EntityMetadata _productMetadata;

    public BulkOperationsTests()
    {
        _mockConnection = new Mock<IDbConnection>();
        _mockMetadataProvider = new Mock<IMetadataProvider>();
        _mockDatabaseProvider = new Mock<IDatabaseProvider>();
        _mockLogger = new Mock<ILogger<EntityManager>>();

        // Setup connection to be open
        _mockConnection.Setup(c => c.State).Returns(ConnectionState.Open);

        // Setup product metadata
        _productMetadata = new EntityMetadata
        {
            EntityType = typeof(Product),
            TableName = "products",
            PrimaryKeyProperty = "Id",
            Properties = new Dictionary<string, PropertyMetadata>
            {
                {
                    "Id",
                    new PropertyMetadata
                    {
                        PropertyName = "Id",
                        ColumnName = "id",
                        PropertyType = typeof(long),
                        IsPrimaryKey = true,
                        IsNullable = false,
                        GenerationType = GenerationType.Identity
                    }
                },
                {
                    "Name",
                    new PropertyMetadata
                    {
                        PropertyName = "Name",
                        ColumnName = "name",
                        PropertyType = typeof(string),
                        IsNullable = false
                    }
                },
                {
                    "Price",
                    new PropertyMetadata
                    {
                        PropertyName = "Price",
                        ColumnName = "price",
                        PropertyType = typeof(decimal),
                        IsNullable = false
                    }
                }
            },
            Relationships = new Dictionary<string, RelationshipMetadata>()
        };

        _mockMetadataProvider
            .Setup(m => m.GetEntityMetadata<Product>())
            .Returns(_productMetadata);
    }

    [Fact]
    public async Task BulkInsertAsync_WithValidEntities_ShouldCallProviderBulkInsert()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Product 1", Price = 10.99m },
            new Product { Id = 2, Name = "Product 2", Price = 20.99m },
            new Product { Id = 3, Name = "Product 3", Price = 30.99m }
        };

        _mockDatabaseProvider
            .Setup(p => p.BulkInsertAsync(
                It.IsAny<IDbConnection>(),
                It.IsAny<IEnumerable<Product>>(),
                It.IsAny<EntityMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        using var entityManager = new EntityManager(
            _mockConnection.Object,
            _mockMetadataProvider.Object,
            _mockDatabaseProvider.Object,
            _mockLogger.Object);

        // Act
        var result = await entityManager.BulkInsertAsync(products);

        // Assert
        result.Should().Be(3);
        _mockDatabaseProvider.Verify(
            p => p.BulkInsertAsync(
                _mockConnection.Object,
                It.Is<IEnumerable<Product>>(e => e.Count() == 3),
                _productMetadata,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void BulkInsert_WithValidEntities_ShouldCallProviderBulkInsert()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Product 1", Price = 10.99m },
            new Product { Id = 2, Name = "Product 2", Price = 20.99m }
        };

        _mockDatabaseProvider
            .Setup(p => p.BulkInsert(
                It.IsAny<IDbConnection>(),
                It.IsAny<IEnumerable<Product>>(),
                It.IsAny<EntityMetadata>()))
            .Returns(2);

        using var entityManager = new EntityManager(
            _mockConnection.Object,
            _mockMetadataProvider.Object,
            _mockDatabaseProvider.Object,
            _mockLogger.Object);

        // Act
        var result = entityManager.BulkInsert(products);

        // Assert
        result.Should().Be(2);
        _mockDatabaseProvider.Verify(
            p => p.BulkInsert(
                _mockConnection.Object,
                It.Is<IEnumerable<Product>>(e => e.Count() == 2),
                _productMetadata),
            Times.Once);
    }

    [Fact]
    public async Task BulkInsertAsync_WithEmptyCollection_ShouldReturnZero()
    {
        // Arrange
        var products = new List<Product>();

        using var entityManager = new EntityManager(
            _mockConnection.Object,
            _mockMetadataProvider.Object,
            _mockDatabaseProvider.Object,
            _mockLogger.Object);

        // Act
        var result = await entityManager.BulkInsertAsync(products);

        // Assert
        result.Should().Be(0);
        _mockDatabaseProvider.Verify(
            p => p.BulkInsertAsync(
                It.IsAny<IDbConnection>(),
                It.IsAny<IEnumerable<Product>>(),
                It.IsAny<EntityMetadata>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task BulkInsertAsync_WithNullEntities_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var entityManager = new EntityManager(
            _mockConnection.Object,
            _mockMetadataProvider.Object,
            _mockDatabaseProvider.Object,
            _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => entityManager.BulkInsertAsync<Product>(null!));
    }

    [Fact]
    public async Task BulkUpdateAsync_WithValidEntities_ShouldCallProviderBulkUpdate()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Updated Product 1", Price = 15.99m },
            new Product { Id = 2, Name = "Updated Product 2", Price = 25.99m }
        };

        _mockDatabaseProvider
            .Setup(p => p.BulkUpdateAsync(
                It.IsAny<IDbConnection>(),
                It.IsAny<IEnumerable<Product>>(),
                It.IsAny<EntityMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        using var entityManager = new EntityManager(
            _mockConnection.Object,
            _mockMetadataProvider.Object,
            _mockDatabaseProvider.Object,
            _mockLogger.Object);

        // Act
        var result = await entityManager.BulkUpdateAsync(products);

        // Assert
        result.Should().Be(2);
        _mockDatabaseProvider.Verify(
            p => p.BulkUpdateAsync(
                _mockConnection.Object,
                It.Is<IEnumerable<Product>>(e => e.Count() == 2),
                _productMetadata,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void BulkUpdate_WithValidEntities_ShouldCallProviderBulkUpdate()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Updated Product 1", Price = 15.99m }
        };

        _mockDatabaseProvider
            .Setup(p => p.BulkUpdate(
                It.IsAny<IDbConnection>(),
                It.IsAny<IEnumerable<Product>>(),
                It.IsAny<EntityMetadata>()))
            .Returns(1);

        using var entityManager = new EntityManager(
            _mockConnection.Object,
            _mockMetadataProvider.Object,
            _mockDatabaseProvider.Object,
            _mockLogger.Object);

        // Act
        var result = entityManager.BulkUpdate(products);

        // Assert
        result.Should().Be(1);
        _mockDatabaseProvider.Verify(
            p => p.BulkUpdate(
                _mockConnection.Object,
                It.Is<IEnumerable<Product>>(e => e.Count() == 1),
                _productMetadata),
            Times.Once);
    }

    [Fact]
    public async Task BulkUpdateAsync_WithEmptyCollection_ShouldReturnZero()
    {
        // Arrange
        var products = new List<Product>();

        using var entityManager = new EntityManager(
            _mockConnection.Object,
            _mockMetadataProvider.Object,
            _mockDatabaseProvider.Object,
            _mockLogger.Object);

        // Act
        var result = await entityManager.BulkUpdateAsync(products);

        // Assert
        result.Should().Be(0);
        _mockDatabaseProvider.Verify(
            p => p.BulkUpdateAsync(
                It.IsAny<IDbConnection>(),
                It.IsAny<IEnumerable<Product>>(),
                It.IsAny<EntityMetadata>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task BulkUpdateAsync_WithNullEntities_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var entityManager = new EntityManager(
            _mockConnection.Object,
            _mockMetadataProvider.Object,
            _mockDatabaseProvider.Object,
            _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => entityManager.BulkUpdateAsync<Product>(null!));
    }

    [Fact]
    public async Task BulkDeleteAsync_WithValidIds_ShouldCallProviderBulkDelete()
    {
        // Arrange
        var ids = new List<object> { 1L, 2L, 3L };

        _mockDatabaseProvider
            .Setup(p => p.BulkDeleteAsync(
                It.IsAny<IDbConnection>(),
                It.IsAny<IEnumerable<object>>(),
                It.IsAny<EntityMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        using var entityManager = new EntityManager(
            _mockConnection.Object,
            _mockMetadataProvider.Object,
            _mockDatabaseProvider.Object,
            _mockLogger.Object);

        // Act
        var result = await entityManager.BulkDeleteAsync<Product>(ids);

        // Assert
        result.Should().Be(3);
        _mockDatabaseProvider.Verify(
            p => p.BulkDeleteAsync(
                _mockConnection.Object,
                It.Is<IEnumerable<object>>(e => e.Count() == 3),
                _productMetadata,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void BulkDelete_WithValidIds_ShouldCallProviderBulkDelete()
    {
        // Arrange
        var ids = new List<object> { 1L, 2L };

        _mockDatabaseProvider
            .Setup(p => p.BulkDelete(
                It.IsAny<IDbConnection>(),
                It.IsAny<IEnumerable<object>>(),
                It.IsAny<EntityMetadata>()))
            .Returns(2);

        using var entityManager = new EntityManager(
            _mockConnection.Object,
            _mockMetadataProvider.Object,
            _mockDatabaseProvider.Object,
            _mockLogger.Object);

        // Act
        var result = entityManager.BulkDelete<Product>(ids);

        // Assert
        result.Should().Be(2);
        _mockDatabaseProvider.Verify(
            p => p.BulkDelete(
                _mockConnection.Object,
                It.Is<IEnumerable<object>>(e => e.Count() == 2),
                _productMetadata),
            Times.Once);
    }

    [Fact]
    public async Task BulkDeleteAsync_WithEmptyCollection_ShouldReturnZero()
    {
        // Arrange
        var ids = new List<object>();

        using var entityManager = new EntityManager(
            _mockConnection.Object,
            _mockMetadataProvider.Object,
            _mockDatabaseProvider.Object,
            _mockLogger.Object);

        // Act
        var result = await entityManager.BulkDeleteAsync<Product>(ids);

        // Assert
        result.Should().Be(0);
        _mockDatabaseProvider.Verify(
            p => p.BulkDeleteAsync(
                It.IsAny<IDbConnection>(),
                It.IsAny<IEnumerable<object>>(),
                It.IsAny<EntityMetadata>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task BulkDeleteAsync_WithNullIds_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var entityManager = new EntityManager(
            _mockConnection.Object,
            _mockMetadataProvider.Object,
            _mockDatabaseProvider.Object,
            _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => entityManager.BulkDeleteAsync<Product>(null!));
    }

    [Fact]
    public async Task BulkInsertAsync_WithLargeDataset_ShouldHandleEfficiently()
    {
        // Arrange
        var products = Enumerable.Range(1, 10000)
            .Select(i => new Product
            {
                Id = i,
                Name = $"Product {i}",
                Price = i * 10.99m
            })
            .ToList();

        _mockDatabaseProvider
            .Setup(p => p.BulkInsertAsync(
                It.IsAny<IDbConnection>(),
                It.IsAny<IEnumerable<Product>>(),
                It.IsAny<EntityMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(10000);

        using var entityManager = new EntityManager(
            _mockConnection.Object,
            _mockMetadataProvider.Object,
            _mockDatabaseProvider.Object,
            _mockLogger.Object);

        // Act
        var result = await entityManager.BulkInsertAsync(products);

        // Assert
        result.Should().Be(10000);
        _mockDatabaseProvider.Verify(
            p => p.BulkInsertAsync(
                _mockConnection.Object,
                It.Is<IEnumerable<Product>>(e => e.Count() == 10000),
                _productMetadata,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Test entity
    [Entity]
    [Table("products")]
    private class Product
    {
        [Id]
        [GeneratedValue(GenerationType.Identity)]
        [Column("id")]
        public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("price")]
        public decimal Price { get; set; }
    }
}
