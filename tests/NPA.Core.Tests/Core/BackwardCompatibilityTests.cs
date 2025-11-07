using Dapper;
using Microsoft.Extensions.Logging;
using Moq;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Core.Tests.TestEntities;
using NPA.Providers.PostgreSql;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace NPA.Core.Tests.Core;

/// <summary>
/// Tests to ensure backward compatibility when operations are executed without transactions.
/// All operations should execute immediately when no transaction is active.
/// </summary>
public class BackwardCompatibilityTests : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    private NpgsqlConnection _connection = null!;
    private IMetadataProvider _metadataProvider = null!;
    private IEntityManager _entityManager = null!;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .Build();

        await _container.StartAsync();

        _connection = new NpgsqlConnection(_container.GetConnectionString());
        await _connection.OpenAsync();

        // Create User table (matching User entity schema)
        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS users (
                id SERIAL PRIMARY KEY,
                username VARCHAR(100) NOT NULL UNIQUE,
                email VARCHAR(255) NOT NULL UNIQUE,
                created_at TIMESTAMP NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT true
            )
        ");

        _metadataProvider = new MetadataProvider();
        var databaseProvider = new PostgreSqlProvider();
        var logger = new Mock<ILogger<EntityManager>>();
        _entityManager = new EntityManager(_connection, _metadataProvider, databaseProvider, logger.Object);
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task PersistAsync_WithoutTransaction_ShouldExecuteImmediately()
    {
        // Arrange
        var user = new User
        {
            Username = $"user_{Guid.NewGuid()}",
            Email = $"user_{Guid.NewGuid()}@test.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act
        await _entityManager.PersistAsync(user);

        // Assert - Should execute immediately
        Assert.False(_entityManager.HasActiveTransaction);
        var queueCount = _entityManager.ChangeTracker.GetQueuedOperationCount();
        Assert.Equal(0, queueCount);

        // Verify in database immediately
        var dbUser = await _connection.QuerySingleOrDefaultAsync<User>(
            @"SELECT id, username, email, created_at, is_active FROM users WHERE username = @Username",
            new { user.Username });

        Assert.NotNull(dbUser);
        Assert.Equal(user.Username, dbUser.Username);
        Assert.Equal(user.Email, dbUser.Email);
        Assert.True(user.Id > 0); // ID should be assigned immediately
    }

    [Fact]
    public void Persist_WithoutTransaction_ShouldExecuteImmediately()
    {
        // Arrange
        var user = new User
        {
            Username = $"user_{Guid.NewGuid()}",
            Email = $"user_{Guid.NewGuid()}@test.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act
        _entityManager.Persist(user);

        // Assert - Should execute immediately
        Assert.False(_entityManager.HasActiveTransaction);
        var queueCount = _entityManager.ChangeTracker.GetQueuedOperationCount();
        Assert.Equal(0, queueCount);

        // Verify in database immediately
        var dbUser = _connection.QuerySingleOrDefault<User>(
            @"SELECT id, username, email, created_at, is_active FROM users WHERE username = @Username",
            new { user.Username });

        Assert.NotNull(dbUser);
        Assert.Equal(user.Username, dbUser.Username);
        Assert.Equal(user.Email, dbUser.Email);
        Assert.True(user.Id > 0); // ID should be assigned immediately
    }

    [Fact]
    public async Task MergeAsync_WithoutTransaction_ShouldExecuteImmediately()
    {
        // Arrange - Create user first
        var user = new User
        {
            Username = $"user_{Guid.NewGuid()}",
            Email = $"user_{Guid.NewGuid()}@test.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        await _entityManager.PersistAsync(user);

        // Modify user
        var newEmail = $"updated_{Guid.NewGuid()}@test.com";
        user.Email = newEmail;

        // Act
        await _entityManager.MergeAsync(user);

        // Assert - Should execute immediately
        Assert.False(_entityManager.HasActiveTransaction);
        var queueCount = _entityManager.ChangeTracker.GetQueuedOperationCount();
        Assert.Equal(0, queueCount);

        // Verify in database immediately
        var dbUser = await _connection.QuerySingleOrDefaultAsync<User>(
            @"SELECT id, username, email, created_at, is_active FROM users WHERE id = @Id",
            new { user.Id });

        Assert.NotNull(dbUser);
        Assert.Equal(newEmail, dbUser.Email);
    }

    [Fact]
    public void Merge_WithoutTransaction_ShouldExecuteImmediately()
    {
        // Arrange - Create user first
        var user = new User
        {
            Username = $"user_{Guid.NewGuid()}",
            Email = $"user_{Guid.NewGuid()}@test.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _entityManager.Persist(user);

        // Modify user
        var newEmail = $"updated_{Guid.NewGuid()}@test.com";
        user.Email = newEmail;

        // Act
        _entityManager.Merge(user);

        // Assert - Should execute immediately
        Assert.False(_entityManager.HasActiveTransaction);
        var queueCount = _entityManager.ChangeTracker.GetQueuedOperationCount();
        Assert.Equal(0, queueCount);

        // Verify in database immediately
        var dbUser = _connection.QuerySingleOrDefault<User>(
            @"SELECT id, username, email, created_at, is_active FROM users WHERE id = @Id",
            new { user.Id });

        Assert.NotNull(dbUser);
        Assert.Equal(newEmail, dbUser.Email);
    }

    [Fact]
    public async Task RemoveAsync_WithoutTransaction_ShouldExecuteImmediately()
    {
        // Arrange - Create user first
        var user = new User
        {
            Username = $"user_{Guid.NewGuid()}",
            Email = $"user_{Guid.NewGuid()}@test.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        await _entityManager.PersistAsync(user);
        var userId = user.Id;

        // Act
        await _entityManager.RemoveAsync(user);

        // Assert - Should execute immediately
        Assert.False(_entityManager.HasActiveTransaction);
        var queueCount = _entityManager.ChangeTracker.GetQueuedOperationCount();
        Assert.Equal(0, queueCount);

        // Verify in database immediately
        var dbUser = await _connection.QuerySingleOrDefaultAsync<User>(
            @"SELECT id, username, email, created_at, is_active FROM users WHERE id = @Id",
            new { Id = userId });

        Assert.Null(dbUser); // Should be deleted immediately
    }

    [Fact]
    public void Remove_WithoutTransaction_ShouldExecuteImmediately()
    {
        // Arrange - Create user first
        var user = new User
        {
            Username = $"user_{Guid.NewGuid()}",
            Email = $"user_{Guid.NewGuid()}@test.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _entityManager.Persist(user);
        var userId = user.Id;

        // Act
        _entityManager.Remove(user);

        // Assert - Should execute immediately
        Assert.False(_entityManager.HasActiveTransaction);
        var queueCount = _entityManager.ChangeTracker.GetQueuedOperationCount();
        Assert.Equal(0, queueCount);

        // Verify in database immediately
        var dbUser = _connection.QuerySingleOrDefault<User>(
            @"SELECT id, username, email, created_at, is_active FROM users WHERE id = @Id",
            new { Id = userId });

        Assert.Null(dbUser); // Should be deleted immediately
    }

    [Fact]
    public void HasActiveTransaction_WithoutTransaction_ShouldReturnFalse()
    {
        // Arrange & Act
        var hasActiveTransaction = _entityManager.HasActiveTransaction;

        // Assert
        Assert.False(hasActiveTransaction);
    }

    [Fact]
    public void GetCurrentTransaction_WithoutTransaction_ShouldReturnNull()
    {
        // Arrange & Act
        var transaction = _entityManager.GetCurrentTransaction();

        // Assert
        Assert.Null(transaction);
    }

    [Fact]
    public async Task MultipleOperations_WithoutTransaction_ShouldAllExecuteImmediately()
    {
        // Arrange
        var user1 = new User
        {
            Username = $"user1_{Guid.NewGuid()}",
            Email = $"user1_{Guid.NewGuid()}@test.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        var user2 = new User
        {
            Username = $"user2_{Guid.NewGuid()}",
            Email = $"user2_{Guid.NewGuid()}@test.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        var user3 = new User
        {
            Username = $"user3_{Guid.NewGuid()}",
            Email = $"user3_{Guid.NewGuid()}@test.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act - Execute multiple operations without transaction
        await _entityManager.PersistAsync(user1);
        await _entityManager.PersistAsync(user2);
        await _entityManager.PersistAsync(user3);

        // Modify user2
        user2.Email = $"updated_{Guid.NewGuid()}@test.com";
        await _entityManager.MergeAsync(user2);

        // Delete user3
        await _entityManager.RemoveAsync(user3);

        // Assert - Queue should always be empty
        var queueCount = _entityManager.ChangeTracker.GetQueuedOperationCount();
        Assert.Equal(0, queueCount);

        // Verify all operations executed immediately
        var user1InDb = await _connection.QuerySingleOrDefaultAsync<User>(
            @"SELECT id, username, email, created_at, is_active FROM users WHERE id = @Id",
            new { user1.Id });
        Assert.NotNull(user1InDb);

        var user2InDb = await _connection.QuerySingleOrDefaultAsync<User>(
            @"SELECT id, username, email, created_at, is_active FROM users WHERE id = @Id",
            new { user2.Id });
        Assert.NotNull(user2InDb);
        Assert.Equal(user2.Email, user2InDb.Email); // Should reflect update

        var user3InDb = await _connection.QuerySingleOrDefaultAsync<User>(
            @"SELECT id, username, email, created_at, is_active FROM users WHERE id = @Id",
            new { user3.Id });
        Assert.Null(user3InDb); // Should be deleted
    }
}
