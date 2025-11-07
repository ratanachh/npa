using System.Data;
using Moq;
using NPA.Core.Annotations;
using NPA.Core.LazyLoading;
using NPA.Core.Metadata;
using Xunit;

namespace NPA.Core.Tests.LazyLoading;

public class LazyLoaderTests
{
    private readonly Mock<IDbConnection> _mockConnection;
    private readonly Mock<ILazyLoadingContext> _mockContext;
    private readonly Mock<ILazyLoadingCache> _mockCache;
    private readonly Mock<IMetadataProvider> _mockMetadataProvider;
    private readonly LazyLoader _lazyLoader;

    public LazyLoaderTests()
    {
        _mockConnection = new Mock<IDbConnection>();
        _mockContext = new Mock<ILazyLoadingContext>();
        _mockCache = new Mock<ILazyLoadingCache>();
        _mockMetadataProvider = new Mock<IMetadataProvider>();

        _mockContext.Setup(c => c.Connection).Returns(_mockConnection.Object);
        _mockContext.Setup(c => c.MetadataProvider).Returns(_mockMetadataProvider.Object);
        _mockContext.Setup(c => c.Transaction).Returns((IDbTransaction?)null);

        _lazyLoader = new LazyLoader(_mockContext.Object, _mockCache.Object);
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LazyLoader(null!, _mockCache.Object));
    }

    [Fact]
    public void Constructor_WithNullCache_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LazyLoader(_mockContext.Object, null!));
    }

    [Fact]
    public async Task LoadAsync_WithNullEntity_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _lazyLoader.LoadAsync<Customer>(null!, "Orders"));
    }

    [Fact]
    public async Task LoadAsync_WithEmptyPropertyName_ShouldThrowArgumentException()
    {
        // Arrange
        var entity = new Order { Id = 1 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _lazyLoader.LoadAsync<Customer>(entity, ""));
    }

    [Fact]
    public async Task LoadAsync_WithCachedValue_ShouldReturnCachedValue()
    {
        // Arrange
        var order = new Order { Id = 1, CustomerId = 10 };
        var cachedCustomer = new Customer { Id = 10, Name = "John Doe" };

        Customer? cachedCustomerRef = cachedCustomer;
        _mockCache.Setup(c => c.TryGet<Customer>(order, "Customer", out cachedCustomerRef))
            .Returns(true);
        _mockCache.Setup(c => c.Contains(order, "Customer")).Returns(true);

        // Act
        var result = await _lazyLoader.LoadAsync<Customer>(order, "Customer");

        // Assert
        Assert.Equal(cachedCustomer, result);
    }

    [Fact]
    public async Task LoadCollectionAsync_WithNullEntity_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _lazyLoader.LoadCollectionAsync<Order>(null!, "Orders"));
    }

    [Fact]
    public async Task LoadCollectionAsync_WithEmptyPropertyName_ShouldThrowArgumentException()
    {
        // Arrange
        var entity = new Customer { Id = 1 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _lazyLoader.LoadCollectionAsync<Order>(entity, ""));
    }

    [Fact]
    public async Task LoadCollectionAsync_WithCachedValue_ShouldReturnCachedValue()
    {
        // Arrange
        var customer = new Customer { Id = 1 };
        var cachedOrders = new List<Order>
        {
            new Order { Id = 1, CustomerId = 1 },
            new Order { Id = 2, CustomerId = 1 }
        };

        IEnumerable<Order>? cachedOrdersEnumerable = cachedOrders;
        _mockCache.Setup(c => c.TryGet<IEnumerable<Order>>(customer, "Orders", out cachedOrdersEnumerable))
            .Returns(true);
        _mockCache.Setup(c => c.Contains(customer, "Orders")).Returns(true);

        // Act
        var result = await _lazyLoader.LoadCollectionAsync<Order>(customer, "Orders");

        // Assert
        Assert.Equal(cachedOrders, result);
    }

    [Fact]
    public void IsLoaded_WithNullEntity_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _lazyLoader.IsLoaded(null!, "Property"));
    }

    [Fact]
    public void IsLoaded_WithEmptyPropertyName_ShouldThrowArgumentException()
    {
        // Arrange
        var entity = new Order { Id = 1 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _lazyLoader.IsLoaded(entity, ""));
    }

    [Fact]
    public void IsLoaded_WithLoadedProperty_ShouldReturnTrue()
    {
        // Arrange
        var entity = new Order { Id = 1 };
        _mockCache.Setup(c => c.Contains(entity, "Customer")).Returns(true);

        // Act
        var result = _lazyLoader.IsLoaded(entity, "Customer");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsLoaded_WithNotLoadedProperty_ShouldReturnFalse()
    {
        // Arrange
        var entity = new Order { Id = 1 };
        _mockCache.Setup(c => c.Contains(entity, "Customer")).Returns(false);

        // Act
        var result = _lazyLoader.IsLoaded(entity, "Customer");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void MarkAsLoaded_WithNullEntity_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _lazyLoader.MarkAsLoaded(null!, "Property"));
    }

    [Fact]
    public void MarkAsLoaded_WithEmptyPropertyName_ShouldThrowArgumentException()
    {
        // Arrange
        var entity = new Order { Id = 1 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _lazyLoader.MarkAsLoaded(entity, ""));
    }

    [Fact]
    public void MarkAsNotLoaded_WithNullEntity_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _lazyLoader.MarkAsNotLoaded(null!, "Property"));
    }

    [Fact]
    public void MarkAsNotLoaded_ShouldRemoveFromCache()
    {
        // Arrange
        var entity = new Order { Id = 1 };

        // Act
        _lazyLoader.MarkAsNotLoaded(entity, "Customer");

        // Assert
        _mockCache.Verify(c => c.Remove(entity, "Customer"), Times.Once);
    }

    [Fact]
    public void ClearCache_ShouldClearAllCache()
    {
        // Act
        _lazyLoader.ClearCache();

        // Assert
        _mockCache.Verify(c => c.Clear(), Times.Once);
    }

    [Fact]
    public void ClearCache_WithEntity_ShouldClearEntityCache()
    {
        // Arrange
        var entity = new Order { Id = 1 };

        // Act
        _lazyLoader.ClearCache(entity);

        // Assert
        _mockCache.Verify(c => c.Remove(entity), Times.Once);
    }

    [Fact]
    public void ClearCache_WithEntityAndProperty_ShouldClearPropertyCache()
    {
        // Arrange
        var entity = new Order { Id = 1 };

        // Act
        _lazyLoader.ClearCache(entity, "Customer");

        // Assert
        _mockCache.Verify(c => c.Remove(entity, "Customer"), Times.Once);
    }

    public class Order
    {
        public long Id { get; set; }
        public long CustomerId { get; set; }
        public Customer? Customer { get; set; }
    }

    public class Customer
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<Order>? Orders { get; set; }
    }
}
