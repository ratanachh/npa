using System.Data;
using Dapper;
using FluentAssertions;
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
/// Tests for deferred execution and batching functionality.
/// </summary>
[Trait("Category", "Integration")]
public class DeferredExecutionTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly NpgsqlConnection _connection;
    private readonly IMetadataProvider _metadataProvider;
    private IEntityManager _entityManager = null!;

    public DeferredExecutionTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("npadb")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithPortBinding(5432, true)
            .Build();
        _connection = new NpgsqlConnection();
        _metadataProvider = new MetadataProvider();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        _connection.ConnectionString = _postgresContainer.GetConnectionString();
        await _connection.OpenAsync();

        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS users (
                id SERIAL PRIMARY KEY,
                username VARCHAR(100) NOT NULL UNIQUE,
                email VARCHAR(255) NOT NULL UNIQUE,
                created_at TIMESTAMP NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT true
            );
        ");

        // Clean the table for test isolation
        await _connection.ExecuteAsync("TRUNCATE TABLE users RESTART IDENTITY CASCADE");

        var databaseProvider = new PostgreSqlProvider();
        var logger = new Mock<ILogger<EntityManager>>();
        _entityManager = new EntityManager(_connection, _metadataProvider, databaseProvider, logger.Object);
    }

    public async Task DisposeAsync()
    {
        _entityManager?.Dispose();
        await _connection.CloseAsync();
        await _connection.DisposeAsync();
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task Persist_WithTransaction_ShouldDeferExecution()
    {
        var tx = await _entityManager.BeginTransactionAsync();
        var user = new User { Username = $"u_{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@ex.com", CreatedAt = DateTime.UtcNow, IsActive = true };

        await _entityManager.PersistAsync(user);

        // Not in DB yet - operation deferred
        var countBefore = await _connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM users");
        countBefore.Should().Be(0, "operation should be deferred");

        // Queue should have 1 operation
        _entityManager.ChangeTracker.GetQueuedOperationCount().Should().Be(1);

        await tx.CommitAsync();

        // Now persisted
        var countAfter = await _connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM users");
        countAfter.Should().Be(1);
    }

    [Fact]
    public async Task Persist_WithoutTransaction_ShouldExecuteImmediately()
    {
        var user = new User { Username = $"u_{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@ex.com", CreatedAt = DateTime.UtcNow, IsActive = true };

        await _entityManager.PersistAsync(user);

        // Immediately in DB
        var count = await _connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM users");
        count.Should().Be(1, "operation should execute immediately without transaction");

        // Queue empty
        _entityManager.ChangeTracker.GetQueuedOperationCount().Should().Be(0);
    }

    [Fact]
    public async Task Merge_WithTransaction_ShouldDeferExecution()
    {
        // Insert first
        var user = new User { Username = $"u_{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@ex.com", CreatedAt = DateTime.UtcNow, IsActive = true };
        await _entityManager.PersistAsync(user);
        var userId = user.Id;
        var originalEmail = user.Email;

        // Start transaction and update
        var tx = await _entityManager.BeginTransactionAsync();
        user.Email = $"{Guid.NewGuid():N}@updated.com";
        await _entityManager.MergeAsync(user);

        // Still old value in DB
        var dbEmail = await _connection.QuerySingleOrDefaultAsync<string>("SELECT email FROM users WHERE id = @Id", new { Id = userId });
        dbEmail.Should().Be(originalEmail, "update should be deferred");

        // Queue has 1 operation
        _entityManager.ChangeTracker.GetQueuedOperationCount().Should().Be(1);

        await tx.CommitAsync();

        // Now updated
        dbEmail = await _connection.QuerySingleOrDefaultAsync<string>("SELECT email FROM users WHERE id = @Id", new { Id = userId });
        dbEmail.Should().Be(user.Email);
    }

    [Fact]
    public async Task Remove_WithTransaction_ShouldDeferExecution()
    {
        // Insert first
        var user = new User { Username = $"u_{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@ex.com", CreatedAt = DateTime.UtcNow, IsActive = true };
        await _entityManager.PersistAsync(user);
        var userId = user.Id;

        // Start transaction and remove
        var tx = await _entityManager.BeginTransactionAsync();
        await _entityManager.RemoveAsync(user);

        // Still exists in DB
        var countBefore = await _connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM users WHERE id = @Id", new { Id = userId });
        countBefore.Should().Be(1, "deletion should be deferred");

        _entityManager.ChangeTracker.GetQueuedOperationCount().Should().Be(1);

        await tx.CommitAsync();

        // Now deleted
        var countAfter = await _connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM users WHERE id = @Id", new { Id = userId });
        countAfter.Should().Be(0);
    }

    [Fact]
    public async Task MultipleOperations_WithTransaction_ShouldBatch()
    {
        // Clean table for test isolation
        await _connection.ExecuteAsync("TRUNCATE TABLE users RESTART IDENTITY CASCADE");

        var tx = await _entityManager.BeginTransactionAsync();

        var user1 = new User { Username = $"u1_{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@ex.com", CreatedAt = DateTime.UtcNow, IsActive = true };
        var user2 = new User { Username = $"u2_{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@ex.com", CreatedAt = DateTime.UtcNow, IsActive = true };
        var user3 = new User { Username = $"u3_{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@ex.com", CreatedAt = DateTime.UtcNow, IsActive = true };

        await _entityManager.PersistAsync(user1);
        await _entityManager.PersistAsync(user2);
        await _entityManager.PersistAsync(user3);

        // Nothing in DB yet
        var countBefore = await _connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM users");
        countBefore.Should().Be(0, "all operations should be deferred");

        // Queue has 3 operations
        _entityManager.ChangeTracker.GetQueuedOperationCount().Should().Be(3);

        await tx.CommitAsync();

        // All persisted
        var countAfter = await _connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM users");
        countAfter.Should().Be(3);
    }

    [Fact]
    public async Task MixedOperations_WithTransaction_ShouldExecuteInOrder()
    {
        // Insert initial user
        var user = new User { Username = $"u_{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@ex.com", CreatedAt = DateTime.UtcNow, IsActive = true };
        await _entityManager.PersistAsync(user);
        var userId = user.Id;

        // Start transaction with mixed operations
        var tx = await _entityManager.BeginTransactionAsync();

        // INSERT
        var newUser = new User { Username = $"nu_{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@ex.com", CreatedAt = DateTime.UtcNow, IsActive = true };
        await _entityManager.PersistAsync(newUser);

        // UPDATE
        user.Email = $"{Guid.NewGuid():N}@updated.com";
        await _entityManager.MergeAsync(user);

        // DELETE
        await _entityManager.RemoveAsync(user);

        // Queue has 3 operations
        _entityManager.ChangeTracker.GetQueuedOperationCount().Should().Be(3);

        await tx.CommitAsync();

        // Verify: original user deleted, new user exists
        var count = await _connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM users WHERE id = @Id", new { Id = userId });
        count.Should().Be(0, "original user should be deleted");

        count = await _connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM users");
        count.Should().Be(1, "new user should exist");
    }

    [Fact]
    public async Task Rollback_ShouldClearQueue()
    {
        var tx = await _entityManager.BeginTransactionAsync();
        var user = new User { Username = $"u_{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@ex.com", CreatedAt = DateTime.UtcNow, IsActive = true };

        await _entityManager.PersistAsync(user);
        _entityManager.ChangeTracker.GetQueuedOperationCount().Should().Be(1);

        await tx.RollbackAsync();

        // Queue cleared
        _entityManager.ChangeTracker.GetQueuedOperationCount().Should().Be(0);

        // Nothing in DB
        var count = await _connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM users");
        count.Should().Be(0);
    }

    [Fact]
    public async Task Clear_ShouldClearQueue()
    {
        var tx = await _entityManager.BeginTransactionAsync();
        var user = new User { Username = $"u_{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@ex.com", CreatedAt = DateTime.UtcNow, IsActive = true };

        await _entityManager.PersistAsync(user);
        _entityManager.ChangeTracker.GetQueuedOperationCount().Should().Be(1);

        await _entityManager.ClearAsync();

        // Queue cleared
        _entityManager.ChangeTracker.GetQueuedOperationCount().Should().Be(0);

        await tx.RollbackAsync();
    }

    [Fact]
    public async Task ExplicitFlush_WithTransaction_ShouldExecuteQueuedOperations()
    {
        // Clean table for test isolation
        await _connection.ExecuteAsync("TRUNCATE TABLE users RESTART IDENTITY CASCADE");

        var tx = await _entityManager.BeginTransactionAsync();
        var user1 = new User { Username = $"u1_{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@ex.com", CreatedAt = DateTime.UtcNow, IsActive = true };
        var user2 = new User { Username = $"u2_{Guid.NewGuid():N}", Email = $"{Guid.NewGuid():N}@ex.com", CreatedAt = DateTime.UtcNow, IsActive = true };

        await _entityManager.PersistAsync(user1);
        await _entityManager.PersistAsync(user2);

        // Explicit flush
        await _entityManager.FlushAsync();

        // Queue cleared after flush
        _entityManager.ChangeTracker.GetQueuedOperationCount().Should().Be(0);

        // Visible within transaction
        var countInsideTx = await _connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM users", transaction: tx.DbTransaction);
        countInsideTx.Should().Be(2);

        await tx.CommitAsync();
    }
}
