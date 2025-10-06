using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Core.Providers;
using NPA.Core.Tests.TestEntities;
using System.Data;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace NPA.Core.Tests.Integration;

/// <summary>
/// Integration tests for EntityManager using real PostgreSQL container.
/// </summary>
[Trait("Category", "Integration")]
public class EntityManagerIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("npadb")
        .WithUsername("npa_user")
        .WithPassword("npa_password")
        .WithPortBinding(5432, true)
        .Build();
    private IEntityManager _entityManager = null!;
    private readonly NpgsqlConnection _connection = new();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        
        var connectionString = _postgresContainer.GetConnectionString();
        _connection.ConnectionString = connectionString;
        
        await _connection.OpenAsync();
        
        // Create test tables
        await CreateTestTables();
        
        var metadataProvider = new MetadataProvider();
        var logger = new Mock<ILogger<EntityManager>>();
        var mockDatabaseProvider = new Mock<IDatabaseProvider>();
        _entityManager = new EntityManager(_connection, metadataProvider, mockDatabaseProvider.Object, logger.Object);
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
    public async Task PersistAsync_WithRealDatabase_ShouldPersistEntity()
    {
        // Arrange
        var user = new User
        {
            Username = "integration_test_user",
            Email = "integration@test.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act
        await _entityManager.PersistAsync(user);

        // Assert
        user.Id.Should().BeGreaterThan(0);
        
        // Verify in database
        var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM users WHERE id = @id";
        command.Parameters.Add(new NpgsqlParameter("@id", user.Id));
        
        var count = await (command).ExecuteScalarAsync();
        ((long)(count ?? 0)).Should().Be(1);
    }

    [Fact]
    public async Task FindAsync_WithRealDatabase_ShouldReturnEntity()
    {
        // Arrange - Insert a user directly
        var insertCommand = _connection.CreateCommand();
        insertCommand.CommandText = @"
            INSERT INTO users (username, email, created_at, is_active) 
            VALUES (@username, @email, @created_at, @is_active)
            RETURNING id;";
        insertCommand.Parameters.Add(new NpgsqlParameter("@username", "find_test_user"));
        insertCommand.Parameters.Add(new NpgsqlParameter("@email", "find@test.com"));
        insertCommand.Parameters.Add(new NpgsqlParameter("@created_at", DateTime.UtcNow));
        insertCommand.Parameters.Add(new NpgsqlParameter("@is_active", true));
        
        var id = await (insertCommand).ExecuteScalarAsync();
        var userId = Convert.ToInt64(id);

        // Act
        var foundUser = await _entityManager.FindAsync<User>(userId);

        // Assert
        foundUser.Should().NotBeNull();
        foundUser!.Id.Should().Be(userId);
        foundUser.Username.Should().Be("find_test_user");
        foundUser.Email.Should().Be("find@test.com");
    }

    [Fact]
    public async Task MergeAsync_WithRealDatabase_ShouldUpdateEntity()
    {
        // Arrange - Insert a user directly
        var insertCommand = _connection.CreateCommand();
        insertCommand.CommandText = @"
            INSERT INTO users (username, email, created_at, is_active) 
            VALUES (@username, @email, @created_at, @is_active)
            RETURNING id;";
        insertCommand.Parameters.Add(new NpgsqlParameter("@username", "merge_test_user"));
        insertCommand.Parameters.Add(new NpgsqlParameter("@email", "merge@test.com"));
        insertCommand.Parameters.Add(new NpgsqlParameter("@created_at", DateTime.UtcNow));
        insertCommand.Parameters.Add(new NpgsqlParameter("@is_active", true));
        
        var id = await insertCommand.ExecuteScalarAsync();
        var userId = Convert.ToInt64(id);

        // Act
        var user = new User
        {
            Id = userId,
            Username = "merged_user",
            Email = "merged@test.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = false
        };
        
        await _entityManager.MergeAsync(user);

        // Assert
        var selectCommand = _connection.CreateCommand();
        selectCommand.CommandText = "SELECT username, email, is_active FROM users WHERE id = @id";
        selectCommand.Parameters.Add(new NpgsqlParameter("@id", userId));
        
        using var reader = selectCommand.ExecuteReader();
        reader.Read();
        
        reader.GetString("username").Should().Be("merged_user");
        reader.GetString("email").Should().Be("merged@test.com");
        reader.GetBoolean("is_active").Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAsync_WithRealDatabase_ShouldDeleteEntity()
    {
        // Arrange - Insert a user directly
        var insertCommand = _connection.CreateCommand();
        insertCommand.CommandText = @"
            INSERT INTO users (username, email, created_at, is_active) 
            VALUES (@username, @email, @created_at, @is_active)
            RETURNING id;";
        insertCommand.Parameters.Add(new NpgsqlParameter("@username", "delete_test_user"));
        insertCommand.Parameters.Add(new NpgsqlParameter("@email", "delete@test.com"));
        insertCommand.Parameters.Add(new NpgsqlParameter("@created_at", DateTime.UtcNow));
        insertCommand.Parameters.Add(new NpgsqlParameter("@is_active", true));
        
        var id = await (insertCommand).ExecuteScalarAsync();
        var userId = Convert.ToInt64(id);

        // Act
        await _entityManager.RemoveAsync<User>(userId);

        // Assert
        var countCommand = _connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM users WHERE id = @id";
        countCommand.Parameters.Add(new NpgsqlParameter("@id", userId));
        
        var count = await countCommand.ExecuteScalarAsync();
        ((long)(count ?? 0)).Should().Be(0);
    }

    [Fact]
    public async Task FlushAsync_WithRealDatabase_ShouldExecuteBatchOperations()
    {
        // Arrange
        var user1 = new User
        {
            Username = "batch_user_1",
            Email = "batch1@test.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        
        var user2 = new User
        {
            Username = "batch_user_2",
            Email = "batch2@test.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act
        await _entityManager.PersistAsync(user1);
        await _entityManager.PersistAsync(user2);

        // Assert
        user1.Id.Should().BeGreaterThan(0);
        user2.Id.Should().BeGreaterThan(0);
        
        var countCommand = _connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM users WHERE username LIKE 'batch_user_%'";
        
        var count = await countCommand.ExecuteScalarAsync();
        ((long)(count ?? 0)).Should().Be(2);
    }

    private async Task CreateTestTables()
    {
        var createTableCommand = _connection.CreateCommand();
        createTableCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS users (
                id BIGSERIAL PRIMARY KEY,
                username VARCHAR(100) NOT NULL,
                email VARCHAR(255) NOT NULL,
                created_at TIMESTAMP NOT NULL,
                is_active BOOLEAN NOT NULL
            );";
        
        await createTableCommand.ExecuteNonQueryAsync();
    }
}
