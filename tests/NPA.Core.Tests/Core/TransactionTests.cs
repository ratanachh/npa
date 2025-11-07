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

[Trait("Category", "Integration")]
public class TransactionTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly NpgsqlConnection _connection;
    private readonly IMetadataProvider _metadataProvider;
    private IEntityManager _entityManager = null!;

    public TransactionTests()
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
    public async Task BeginTransactionAsync_ShouldCreateActiveTransaction()
    {
        var tx = await _entityManager.BeginTransactionAsync();
        _entityManager.HasActiveTransaction.Should().BeTrue();
        tx.IsActive.Should().BeTrue();
        await tx.RollbackAsync();
    }

    [Fact]
    public void BeginTransaction_ShouldCreateActiveTransaction()
    {
        var tx = _entityManager.BeginTransaction();
        _entityManager.HasActiveTransaction.Should().BeTrue();
        tx.IsActive.Should().BeTrue();
        tx.Rollback();
    }

    [Fact]
    public async Task CommitAsync_ShouldPersistQueuedOperations()
    {
        using var tx = await _entityManager.BeginTransactionAsync();
        var user = new User
        {
            Username = $"user_{Guid.NewGuid():N}",
            Email = $"{Guid.NewGuid():N}@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _entityManager.PersistAsync(user);

        var countBefore = await _connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM users");
        countBefore.Should().Be(0);

        await tx.CommitAsync();

        var countAfter = await _connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM users");
        countAfter.Should().Be(1);
    }

    [Fact]
    public async Task RollbackAsync_ShouldDiscardQueuedOperations()
    {
        using var tx = await _entityManager.BeginTransactionAsync();
        var user = new User
        {
            Username = $"user_{Guid.NewGuid():N}",
            Email = $"{Guid.NewGuid():N}@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        await _entityManager.PersistAsync(user);

        await tx.RollbackAsync();

        var count = await _connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM users");
        count.Should().Be(0);
    }
}
