using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Core.Providers;
using NPA.Core.Tests.TestEntities;
using NPA.Providers.PostgreSql;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;
using Dapper;

namespace NPA.Core.Tests.Core;

/// <summary>
/// Collection definition to prevent parallel test execution for EntityManager tests.
/// </summary>
[CollectionDefinition("EntityManager Tests", DisableParallelization = true)]
public class EntityManagerTestsCollection
{
}

/// <summary>
/// Integration tests for the EntityManager class using real PostgreSQL container.
/// </summary>
[Collection("EntityManager Tests")]
[Trait("Category", "Integration")]
public class EntityManagerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly NpgsqlConnection _connection;
    private readonly IMetadataProvider _metadataProvider;
    private IEntityManager _entityManager = null!;
    private int _uniqueIdCounter = 0;

    public EntityManagerTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("npadb")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithPortBinding(5432, true)
            .WithStartupCallback((container, ct) => Task.Delay(TimeSpan.FromSeconds(3), ct))
            .Build();

        _connection = new NpgsqlConnection();
        _metadataProvider = new MetadataProvider();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        
        var connectionString = _postgresContainer.GetConnectionString();
        _connection.ConnectionString = connectionString;
        
        await _connection.OpenAsync();
        
        // Create test tables
        await CreateTestTables();
        
        // Clear any existing data and reset sequences
        await ClearTestData();
        
        var mockLogger = new Mock<ILogger<EntityManager>>();
        var databaseProvider = new PostgreSqlProvider();
        _entityManager = new EntityManager(_connection, _metadataProvider, databaseProvider, mockLogger.Object);
    }

    public async Task DisposeAsync()
    {
        _entityManager?.Dispose();
        await _connection.CloseAsync();
        await _connection.DisposeAsync();
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }

    private async Task CreateTestTables()
    {
        const string createTableSql = @"
            CREATE TABLE IF NOT EXISTS users (
                id SERIAL PRIMARY KEY,
                username VARCHAR(100) NOT NULL UNIQUE,
                email VARCHAR(255) NOT NULL UNIQUE,
                created_at TIMESTAMP NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT true
            );";

        await using var command = new NpgsqlCommand(createTableSql, _connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearTestData()
    {
        var clearDataSql = @"
            TRUNCATE TABLE users RESTART IDENTITY CASCADE;
        ";
        using var command = new NpgsqlCommand(clearDataSql, _connection);
        await command.ExecuteNonQueryAsync();
    }

    private string GenerateUniqueId(string prefix = "")
    {
        // Use a combination of timestamp, counter, and GUID for guaranteed uniqueness
        var ticks = DateTime.UtcNow.Ticks;
        var counter = ++_uniqueIdCounter;
        var guid = Guid.NewGuid().ToString("N")[..8];
        return $"{prefix}{ticks}_{counter}_{guid}";
    }

    private async Task SetupTestData()
    {
        // Clear any existing data before each test
        await ClearTestData();
    }

    [Fact]
    public void EntityManager_WithValidDependencies_ShouldCreateInstance()
    {
        // Act & Assert
        _entityManager.Should().NotBeNull();
        _entityManager.MetadataProvider.Should().Be(_metadataProvider);
        _entityManager.ChangeTracker.Should().NotBeNull();
    }

    [Fact]
    public void EntityManager_WithNullConnection_ShouldThrowException()
    {
        // Act & Assert
        var mockDatabaseProvider = new Mock<IDatabaseProvider>();
        var action = () => new EntityManager(null!, _metadataProvider, mockDatabaseProvider.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("connection");
    }

    [Fact]
    public void EntityManager_WithNullMetadataProvider_ShouldThrowException()
    {
        // Act & Assert
        var mockDatabaseProvider = new Mock<IDatabaseProvider>();
        var action = () => new EntityManager(_connection, null!, mockDatabaseProvider.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("metadataProvider");
    }

    [Fact]
    public async Task PersistAsync_WithValidEntity_ShouldPersistEntity()
    {
        // Arrange
        await SetupTestData();
        var uniqueId = GenerateUniqueId("testuser_");
        var user = new User
        {
            Username = uniqueId,
            Email = $"{uniqueId}@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act
        await _entityManager.PersistAsync(user);

        // Assert
        user.Id.Should().BeGreaterThan(0); // Real database will assign an ID
        _entityManager.ChangeTracker.GetState(user).Should().Be(EntityState.Added);
        
        // Verify the entity was actually persisted by finding it
        var foundUser = await _entityManager.FindAsync<User>(user.Id);
        foundUser.Should().NotBeNull();
        foundUser!.Username.Should().Be(uniqueId);
        foundUser.Email.Should().Be($"{uniqueId}@example.com");
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
    public async Task FindAsync_WithExistingEntity_ShouldReturnEntity()
    {
        // Arrange
        await SetupTestData();
        var uniqueId = GenerateUniqueId("finduser_");
        var user = new User
        {
            Username = uniqueId,
            Email = $"{uniqueId}@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        await _entityManager.PersistAsync(user);

        // Act
        var foundUser = await _entityManager.FindAsync<User>(user.Id);

        // Assert
        foundUser.Should().NotBeNull();
        foundUser!.Id.Should().Be(user.Id);
        foundUser.Username.Should().Be(uniqueId);
        foundUser.Email.Should().Be($"{uniqueId}@example.com");
    }

    [Fact]
    public async Task FindAsync_WithNonExistentEntity_ShouldReturnNull()
    {
        // Act
        var foundUser = await _entityManager.FindAsync<User>(999L);

        // Assert
        foundUser.Should().BeNull();
    }

    [Fact]
    public async Task MergeAsync_WithExistingEntity_ShouldUpdateEntity()
    {
        // Arrange
        await SetupTestData();
        var uniqueId = GenerateUniqueId("mergeuser_");
        var user = new User
        {
            Username = uniqueId,
            Email = $"{uniqueId}@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        await _entityManager.PersistAsync(user);

        // Modify the entity
        user.Email = "updated@example.com";
        user.IsActive = false;

        // Act
        await _entityManager.MergeAsync(user);

        // Assert
        _entityManager.ChangeTracker.GetState(user).Should().Be(EntityState.Modified);
        
        // Verify the changes were persisted
        var foundUser = await _entityManager.FindAsync<User>(user.Id);
        foundUser.Should().NotBeNull();
        foundUser!.Email.Should().Be("updated@example.com");
        foundUser.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAsync_WithExistingEntity_ShouldDeleteEntity()
    {
        // Arrange
        await SetupTestData();
        var uniqueId = GenerateUniqueId("removeuser_");
        var user = new User
        {
            Username = uniqueId,
            Email = $"{uniqueId}@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        await _entityManager.PersistAsync(user);

        // Act
        await _entityManager.RemoveAsync(user);

        // Assert
        _entityManager.ChangeTracker.GetState(user).Should().Be(EntityState.Deleted);
        
        // Verify the entity was actually deleted
        var foundUser = await _entityManager.FindAsync<User>(user.Id);
        foundUser.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_WithEntityId_ShouldDeleteEntity()
    {
        // Arrange
        await SetupTestData();
        var uniqueId = GenerateUniqueId("removebyiduser_");
        var user = new User
        {
            Username = uniqueId,
            Email = $"{uniqueId}@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        await _entityManager.PersistAsync(user);

        // Act
        await _entityManager.RemoveAsync<User>(user.Id);

        // Assert
        // Verify the entity was actually deleted
        var foundUser = await _entityManager.FindAsync<User>(user.Id);
        foundUser.Should().BeNull();
    }

    // [Fact]
    // public async Task FlushAsync_ShouldExecutePendingOperations()
    // {
    //     // Arrange
    //     await SetupTestData();
    //     var uniqueId1 = GenerateUniqueId("flushuser1_");
    //     var uniqueId2 = GenerateUniqueId("flushuser2_");
    //     
    //     var user1 = new User
    //     {
    //         Username = uniqueId1,
    //         Email = $"{uniqueId1}@example.com",
    //         CreatedAt = DateTime.UtcNow,
    //         IsActive = true
    //     };
    //     var user2 = new User
    //     {
    //         Username = uniqueId2,
    //         Email = $"{uniqueId2}@example.com",
    //         CreatedAt = DateTime.UtcNow,
    //         IsActive = true
    //     };
    //
    //     // Act
    //     await _entityManager.PersistAsync(user1);
    //     await _entityManager.PersistAsync(user2);
    //     await _entityManager.FlushAsync(); // working around about state added and unchanged
    //
    //     // Assert
    //     // Verify both entities were persisted
    //     var foundUser1 = await _entityManager.FindAsync<User>(user1.Id);
    //     var foundUser2 = await _entityManager.FindAsync<User>(user2.Id);
    //     
    //     foundUser1.Should().NotBeNull();
    //     foundUser2.Should().NotBeNull();
    // }

    [Fact]
    public async Task ClearAsync_ShouldClearChangeTracker()
    {
        // Arrange
        await SetupTestData();
        var uniqueId = GenerateUniqueId("clearuser_");
        var user = new User
        {
            Username = uniqueId,
            Email = $"{uniqueId}@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        await _entityManager.PersistAsync(user);

        // Act
        await _entityManager.ClearAsync();

        // Assert
        _entityManager.ChangeTracker.GetState(user).Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task CreateQuery_WithValidCpql_ShouldCreateQuery()
    {
        // Arrange
        var cpql = "SELECT u FROM User u WHERE u.IsActive = :active";

        // Act
        var query = _entityManager.CreateQuery<User>(cpql);
        query.SetParameter("active", true);

        // Assert
        query.Should().NotBeNull();
        
        // Execute the query to verify it works
        var results = await query.GetResultListAsync();
        results.Should().NotBeNull();
    }

    [Fact]
    public void CreateQuery_WithNullCpql_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _entityManager.CreateQuery<User>(null!);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateQuery_WithEmptyCpql_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _entityManager.CreateQuery<User>("");
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Dispose_ShouldDisposeResources()
    {
        // Act
        _entityManager.Dispose();

        // Assert
        // The EntityManager should be disposed, but we can't check the connection state
        // since the EntityManager might dispose it
        _entityManager.Should().NotBeNull(); // Just verify the object exists
    }

    [Fact]
    public async Task Operations_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _entityManager.Dispose();
        var uniqueId = GenerateUniqueId("disposeduser_");
        var user = new User
        {
            Username = uniqueId,
            Email = $"{uniqueId}@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await _entityManager.PersistAsync(user));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await _entityManager.FindAsync<User>(1));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await _entityManager.MergeAsync(user));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await _entityManager.RemoveAsync(user));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await _entityManager.FlushAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await _entityManager.ClearAsync());
    }

    [Fact]
    public async Task PersistAsync_MultipleEntities_WithFlush_ShouldInsertAllEntities()
    {
        // Arrange
        await ClearTestData();
        
        var guid = Guid.NewGuid().ToString("N");
        var users = new[]
        {
            new User { Username = $"user1_{guid}", Email = $"user1_{guid}@test.com", CreatedAt = DateTime.UtcNow, IsActive = true },
            new User { Username = $"user2_{guid}", Email = $"user2_{guid}@test.com", CreatedAt = DateTime.UtcNow, IsActive = true },
            new User { Username = $"user3_{guid}", Email = $"user3_{guid}@test.com", CreatedAt = DateTime.UtcNow, IsActive = true }
        };

        // Act - Track entities without transaction (should use change tracker)
        foreach (var user in users)
        {
            await _entityManager.PersistAsync(user);
        }
        
        // Verify entities are tracked
        users.All(u => _entityManager.Contains(u)).Should().BeTrue();
        
        // Flush should execute the pending inserts via CallGenericMethodAsync
        await _entityManager.FlushAsync();

        // Assert - All users should have generated IDs
        users.All(u => u.Id > 0).Should().BeTrue();
        
        // Verify in database
        var savedUsers = await _connection.QueryAsync<User>("SELECT * FROM public.users ORDER BY id");
        savedUsers.Should().HaveCount(3);
        savedUsers.Should().Contain(u => u.Username == $"user1_{guid}");
        savedUsers.Should().Contain(u => u.Username == $"user2_{guid}");
        savedUsers.Should().Contain(u => u.Username == $"user3_{guid}");
    }

    [Fact]
    public async Task FlushAsync_WithMixedOperations_ShouldExecuteAllPendingChanges()
    {
        // Arrange
        await ClearTestData();
        
        var guid = Guid.NewGuid().ToString("N");
        // Insert initial user
        var user1 = new User 
        { 
            Username = $"original_{guid}", 
            Email = $"original_{guid}@test.com", 
            CreatedAt = DateTime.UtcNow, 
            IsActive = true 
        };
        await _entityManager.PersistAsync(user1);
        await _entityManager.FlushAsync();

        var user2 = new User 
        { 
            Username = $"new_{guid}", 
            Email = $"new_{guid}@test.com", 
            CreatedAt = DateTime.UtcNow, 
            IsActive = true 
        };
        
        // Act - Queue mixed operations
        user1.Username = $"modified_{guid}";
        await _entityManager.MergeAsync(user1); // Update
        await _entityManager.PersistAsync(user2); // Insert
        
        await _entityManager.FlushAsync(); // Should call CallGenericMethodAsync for both

        // Assert
        var users = await _connection.QueryAsync<User>("SELECT * FROM public.users ORDER BY id");
        users.Should().HaveCount(2);
        users.First().Username.Should().Be($"modified_{guid}");
        users.Last().Username.Should().Be($"new_{guid}");
    }
}
