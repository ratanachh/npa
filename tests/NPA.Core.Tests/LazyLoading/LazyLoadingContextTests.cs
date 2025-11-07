using System.Data;
using Moq;
using NPA.Core.Core;
using NPA.Core.LazyLoading;
using NPA.Core.Metadata;
using Xunit;

namespace NPA.Core.Tests.LazyLoading;

public class LazyLoadingContextTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeProperties()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockEntityManager = new Mock<IEntityManager>();
        var mockMetadataProvider = new Mock<IMetadataProvider>();
        var mockTransaction = new Mock<IDbTransaction>();

        // Act
        var context = new LazyLoadingContext(
            mockConnection.Object,
            mockEntityManager.Object,
            mockMetadataProvider.Object,
            mockTransaction.Object);

        // Assert
        Assert.Equal(mockConnection.Object, context.Connection);
        Assert.Equal(mockEntityManager.Object, context.EntityManager);
        Assert.Equal(mockMetadataProvider.Object, context.MetadataProvider);
        Assert.Equal(mockTransaction.Object, context.Transaction);
    }

    [Fact]
    public void Constructor_WithoutTransaction_ShouldInitializeWithNullTransaction()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockEntityManager = new Mock<IEntityManager>();
        var mockMetadataProvider = new Mock<IMetadataProvider>();

        // Act
        var context = new LazyLoadingContext(
            mockConnection.Object,
            mockEntityManager.Object,
            mockMetadataProvider.Object);

        // Assert
        Assert.Null(context.Transaction);
    }

    [Fact]
    public void Constructor_WithNullConnection_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockEntityManager = new Mock<IEntityManager>();
        var mockMetadataProvider = new Mock<IMetadataProvider>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LazyLoadingContext(
            null!,
            mockEntityManager.Object,
            mockMetadataProvider.Object));
    }

    [Fact]
    public void Constructor_WithNullEntityManager_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockMetadataProvider = new Mock<IMetadataProvider>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LazyLoadingContext(
            mockConnection.Object,
            null!,
            mockMetadataProvider.Object));
    }

    [Fact]
    public void Constructor_WithNullMetadataProvider_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockConnection = new Mock<IDbConnection>();
        var mockEntityManager = new Mock<IEntityManager>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LazyLoadingContext(
            mockConnection.Object,
            mockEntityManager.Object,
            null!));
    }
}
