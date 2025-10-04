using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Core.Tests.TestEntities;
using System.Data;
using Xunit;

namespace NPA.Core.Tests.Core;

/// <summary>
/// Unit tests for the EntityManager class.
/// </summary>
public class EntityManagerTests : IDisposable
{
    private readonly MockDbConnection _mockConnection;
    private readonly IMetadataProvider _metadataProvider;
    private readonly IEntityManager _entityManager;

    public EntityManagerTests()
    {
        _mockConnection = new MockDbConnection();
        _metadataProvider = new MetadataProvider();
        var mockLogger = new Mock<ILogger<TestEntityManager>>();
        _entityManager = new TestEntityManager(_mockConnection, _metadataProvider, mockLogger.Object);
    }

    [Fact]
    public void EntityManager_WithValidDependencies_ShouldCreateInstance()
    {
        // Act
        var entityManager = new EntityManager(_mockConnection, _metadataProvider);

        // Assert
        entityManager.Should().NotBeNull();
        entityManager.MetadataProvider.Should().Be(_metadataProvider);
        entityManager.ChangeTracker.Should().NotBeNull();
    }

    [Fact]
    public void EntityManager_WithNullConnection_ShouldThrowException()
    {
        // Act & Assert
        var action = () => new EntityManager(null!, _metadataProvider);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("connection");
    }

    [Fact]
    public void EntityManager_WithNullMetadataProvider_ShouldThrowException()
    {
        // Act & Assert
        var action = () => new EntityManager(_mockConnection, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("metadataProvider");
    }

    [Fact]
    public async Task PersistAsync_WithValidEntity_ShouldPersistEntity()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act
        await _entityManager.PersistAsync(user);

        // Assert
        user.Id.Should().Be(123L); // Mock return value
        _entityManager.ChangeTracker.GetState(user).Should().Be(EntityState.Added);
        _mockConnection.ExecutedCommands.Should().HaveCount(1);
        
        var command = _mockConnection.ExecutedCommands[0];
        command.CommandText.Should().Contain("INSERT INTO users");
        command.CommandText.Should().Contain("username");
        command.CommandText.Should().Contain("email");
    }

    [Fact]
    public async Task PersistAsync_WithNullEntity_ShouldThrowException()
    {
        // Act & Assert
        var action = async () => await _entityManager.PersistAsync<User>(null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("entity");
    }

    [Fact]
    public async Task FindAsync_WithValidId_ShouldReturnEntity()
    {
        // Arrange
        var user = new User { Id = 1, Username = "testuser" };
        _entityManager.ChangeTracker.Track(user, EntityState.Unchanged);

        // Act
        var result = await _entityManager.FindAsync<User>(1L);

        // Assert
        _mockConnection.ExecutedCommands.Should().HaveCount(1);
        
        var command = _mockConnection.ExecutedCommands[0];
        command.CommandText.Should().Contain("SELECT");
        command.CommandText.Should().Contain("FROM users");
        command.CommandText.Should().Contain("WHERE id = @id");
    }

    [Fact]
    public async Task FindAsync_WithNullId_ShouldThrowException()
    {
        // Act & Assert
        var action = async () => await _entityManager.FindAsync<User>((object)null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("id");
    }

    [Fact]
    public async Task MergeAsync_WithValidEntity_ShouldUpdateEntity()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act
        await _entityManager.MergeAsync(user);

        // Assert
        _entityManager.ChangeTracker.GetState(user).Should().Be(EntityState.Modified);
        _mockConnection.ExecutedCommands.Should().HaveCount(1);
        
        var command = _mockConnection.ExecutedCommands[0];
        command.CommandText.Should().Contain("UPDATE users");
        command.CommandText.Should().Contain("SET");
    }

    [Fact]
    public async Task MergeAsync_WithNullEntity_ShouldThrowException()
    {
        // Act & Assert
        var action = async () => await _entityManager.MergeAsync<User>(null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("entity");
    }

    [Fact]
    public async Task RemoveAsync_WithValidEntity_ShouldDeleteEntity()
    {
        // Arrange
        var user = new User { Id = 1, Username = "testuser" };

        // Act
        await _entityManager.RemoveAsync(user);

        // Assert
        _entityManager.ChangeTracker.GetState(user).Should().Be(EntityState.Deleted);
        _mockConnection.ExecutedCommands.Should().HaveCount(1);
        
        var command = _mockConnection.ExecutedCommands[0];
        command.CommandText.Should().Contain("DELETE FROM users");
        command.CommandText.Should().Contain("WHERE id = @id");
    }

    [Fact]
    public async Task RemoveAsync_WithValidId_ShouldDeleteEntity()
    {
        // Act
        await _entityManager.RemoveAsync<User>(1L);

        // Assert
        _mockConnection.ExecutedCommands.Should().HaveCount(1);
        
        var command = _mockConnection.ExecutedCommands[0];
        command.CommandText.Should().Contain("DELETE FROM users");
        command.CommandText.Should().Contain("WHERE id = @id");
    }

    [Fact]
    public async Task RemoveAsync_WithNullEntity_ShouldThrowException()
    {
        // Act & Assert
        var action = async () => await _entityManager.RemoveAsync<User>(null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("entity");
    }

    [Fact]
    public async Task RemoveAsync_WithNullId_ShouldThrowException()
    {
        // Act & Assert
        var action = async () => await _entityManager.RemoveAsync<User>((object)null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("id");
    }

    [Fact]
    public async Task FlushAsync_WithPendingChanges_ShouldExecuteAllChanges()
    {
        // Arrange
        var user1 = new User { Username = "user1", Email = "user1@example.com", CreatedAt = DateTime.UtcNow };
        var user2 = new User { Id = 2, Username = "user2", Email = "user2@example.com", CreatedAt = DateTime.UtcNow };

        _entityManager.ChangeTracker.Track(user1, EntityState.Added);
        _entityManager.ChangeTracker.Track(user2, EntityState.Modified);

        // Act
        await _entityManager.FlushAsync();

        // Assert
        _mockConnection.ExecutedCommands.Should().HaveCount(2);
    }

    [Fact]
    public async Task ClearAsync_ShouldClearChangeTracker()
    {
        // Arrange
        var user = new User { Id = 1, Username = "testuser" };
        _entityManager.ChangeTracker.Track(user, EntityState.Added);

        // Act
        await _entityManager.ClearAsync();

        // Assert
        _entityManager.ChangeTracker.GetState(user).Should().BeNull();
    }

    [Fact]
    public void Contains_WithTrackedEntity_ShouldReturnTrue()
    {
        // Arrange
        var user = new User { Id = 1, Username = "testuser" };
        _entityManager.ChangeTracker.Track(user, EntityState.Unchanged);

        // Act
        var result = _entityManager.Contains(user);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Contains_WithUntrackedEntity_ShouldReturnFalse()
    {
        // Arrange
        var user = new User { Id = 1, Username = "testuser" };

        // Act
        var result = _entityManager.Contains(user);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Contains_WithNullEntity_ShouldReturnFalse()
    {
        // Act
        var result = _entityManager.Contains<User>(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Detach_WithTrackedEntity_ShouldUntrackEntity()
    {
        // Arrange
        var user = new User { Id = 1, Username = "testuser" };
        _entityManager.ChangeTracker.Track(user, EntityState.Unchanged);

        // Act
        _entityManager.Detach(user);

        // Assert
        _entityManager.ChangeTracker.GetState(user).Should().BeNull();
    }

    [Fact]
    public void Detach_WithNullEntity_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => _entityManager.Detach<User>(null!);
        action.Should().NotThrow();
    }

    [Fact]
    public async Task FindAsync_WithCompositeKey_ShouldReturnEntity()
    {
        // Arrange
        var orderItem = new OrderItem
        {
            OrderId = 1,
            ProductId = 2,
            Quantity = 5,
            Price = 29.99m
        };

        var compositeKey = new CompositeKey();
        compositeKey.SetValue("OrderId", 1L);
        compositeKey.SetValue("ProductId", 2L);

        // Act
        var result = await _entityManager.FindAsync<OrderItem>(compositeKey);

        // Assert
        _mockConnection.ExecutedCommands.Should().HaveCount(1);
        
        var command = _mockConnection.ExecutedCommands[0];
        command.CommandText.Should().Contain("SELECT");
        command.CommandText.Should().Contain("FROM order_items");
        command.CommandText.Should().Contain("WHERE");
        command.CommandText.Should().Contain("OrderId");
        command.CommandText.Should().Contain("ProductId");
    }

    [Fact]
    public async Task FindAsync_WithNullCompositeKey_ShouldThrowException()
    {
        // Act & Assert
        var action = async () => await _entityManager.FindAsync<OrderItem>(null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("key");
    }

    [Fact]
    public void Dispose_ShouldDisposeConnection()
    {
        // Act
        _entityManager.Dispose();

        // Assert
        _mockConnection.State.Should().Be(ConnectionState.Closed);
    }

    [Fact]
    public void Dispose_ShouldClearChangeTracker()
    {
        // Arrange
        var user = new User { Id = 1, Username = "testuser" };
        _entityManager.ChangeTracker.Track(user, EntityState.Added);

        // Act
        _entityManager.Dispose();

        // Assert
        _entityManager.ChangeTracker.GetPendingChanges().Should().BeEmpty();
    }

    [Fact]
    public async Task Operations_AfterDispose_ShouldThrowException()
    {
        // Arrange
        var user = new User { Username = "testuser", Email = "test@example.com", CreatedAt = DateTime.UtcNow };
        _entityManager.Dispose();

        // Act & Assert
        var persistAction = async () => await _entityManager.PersistAsync(user);
        await persistAction.Should().ThrowAsync<ObjectDisposedException>();

        var findAction = async () => await _entityManager.FindAsync<User>(1L);
        await findAction.Should().ThrowAsync<ObjectDisposedException>();

        var mergeAction = async () => await _entityManager.MergeAsync(user);
        await mergeAction.Should().ThrowAsync<ObjectDisposedException>();

        var removeAction = async () => await _entityManager.RemoveAsync(user);
        await removeAction.Should().ThrowAsync<ObjectDisposedException>();

        var flushAction = async () => await _entityManager.FlushAsync();
        await flushAction.Should().ThrowAsync<ObjectDisposedException>();
    }

    public void Dispose()
    {
        _entityManager?.Dispose();
    }
}
