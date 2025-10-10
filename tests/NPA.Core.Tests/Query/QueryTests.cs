using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Core.Providers;
using NPA.Core.Query;
using NPA.Core.Tests.TestEntities;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace NPA.Core.Tests.Query;

/// <summary>
/// Collection definition to prevent parallel test execution for Query tests.
/// </summary>
[CollectionDefinition("Query Tests", DisableParallelization = true)]
public class QueryTestsCollection
{
}

/// <summary>
/// Integration tests for the Query class using real PostgreSQL container.
/// </summary>
[Collection("Query Tests")]
[Trait("Category", "Integration")]
public class QueryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private IEntityManager _entityManager = null!;
    private readonly NpgsqlConnection _connection;
    private readonly IMetadataProvider _metadataProvider;
    private readonly IQueryParser _parser;
    private readonly ISqlGenerator _sqlGenerator;
    private readonly IParameterBinder _parameterBinder;
    private readonly Mock<ILogger<Query<User>>> _mockLogger;

    public QueryTests()
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
        _parser = new QueryParser();
        _sqlGenerator = new SqlGenerator();
        _parameterBinder = new ParameterBinder();
        _mockLogger = new Mock<ILogger<Query<User>>>();
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
        
        // Insert test data
        await InsertTestData();
        
        var entityLogger = new Mock<ILogger<EntityManager>>();
        var mockDatabaseProvider = new Mock<IDatabaseProvider>();
        _entityManager = new EntityManager(_connection, _metadataProvider, mockDatabaseProvider.Object, entityLogger.Object);
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
        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS users (
                id SERIAL PRIMARY KEY,
                username VARCHAR(100) NOT NULL UNIQUE,
                email VARCHAR(255) NOT NULL UNIQUE,
                created_at TIMESTAMP NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT true
            );";

        using var command = new NpgsqlCommand(createTableSql, _connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearTestData()
    {
        var clearDataSql = "TRUNCATE TABLE users RESTART IDENTITY CASCADE;";
        using var command = new NpgsqlCommand(clearDataSql, _connection);
        await command.ExecuteNonQueryAsync();
    }
    
    private async Task InsertTestData()
    {
        var insertSql = @"
            INSERT INTO users (username, email, created_at, is_active) 
            VALUES 
                ('testuser1', 'test1@example.com', NOW(), true),
                ('testuser2', 'test2@example.com', NOW(), false),
                ('testuser3', 'test3@example.com', NOW(), true)
            ON CONFLICT (username) DO NOTHING;";

        using var command = new NpgsqlCommand(insertSql, _connection);
        await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public void Query_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT u FROM User u",
            _mockLogger.Object);

        // Assert
        query.Should().NotBeNull();
    }

    [Fact]
    public void Query_WithNullConnection_ShouldThrowException()
    {
        // Arrange & Act & Assert
        var action = () => new Query<User>(
            null!,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT u FROM User u",
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("connection");
    }

    [Fact]
    public void Query_WithNullCpql_ShouldThrowException()
    {
        // Arrange & Act & Assert
        var action = () => new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            null!,
            _mockLogger.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("cpql");
    }

    [Fact]
    public void SetParameter_WithValidName_ShouldSetParameter()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT u FROM User u WHERE u.Username = :username",
            _mockLogger.Object);

        // Act
        var result = query.SetParameter("username", "testuser1");

        // Assert
        result.Should().BeSameAs(query); // Should return the same instance for chaining
    }

    [Fact]
    public void SetParameter_WithValidIndex_ShouldSetParameter()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT u FROM User u WHERE u.IsActive = ?1",
            _mockLogger.Object);

        // Act
        var result = query.SetParameter(0, true);

        // Assert
        result.Should().BeSameAs(query); // Should return the same instance for chaining
    }

    [Fact]
    public void SetParameter_WithNullName_ShouldThrowException()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT u FROM User u",
            _mockLogger.Object);

        // Act & Assert
        var action = () => query.SetParameter(null!, "value");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void SetParameter_WithEmptyName_ShouldThrowException()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT u FROM User u",
            _mockLogger.Object);

        // Act & Assert
        var action = () => query.SetParameter("", "value");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void SetParameter_WithNegativeIndex_ShouldThrowException()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT u FROM User u",
            _mockLogger.Object);

        // Act & Assert
        var action = () => query.SetParameter(-1, "value");
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("index");
    }

    [Fact]
    public async Task GetResultListAsync_WithValidQuery_ShouldReturnResults()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT u FROM User u",
            _mockLogger.Object);

        // Act
        var results = await query.GetResultListAsync();

        // Assert
        var enumerable = results as User[] ?? results.ToArray();
        enumerable.Should().NotBeNull();
        enumerable.Should().HaveCount(3); // We inserted 3 test users
        enumerable.Should().AllSatisfy(user => user.Id.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task GetResultListAsync_WithWhereClause_ShouldReturnFilteredResults()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT u FROM User u WHERE u.IsActive = :active",
            _mockLogger.Object);
        query.SetParameter("active", true);

        // Act
        var results = await query.GetResultListAsync();

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(2); // Only 2 active users
        results.Should().AllSatisfy(user => user.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task GetSingleResultAsync_WithValidQuery_ShouldReturnResult()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT u FROM User u WHERE u.Username = :username",
            _mockLogger.Object);
        query.SetParameter("username", "testuser1");

        // Act
        var result = await query.GetSingleResultAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser1");
        result.Email.Should().Be("test1@example.com");
    }

    [Fact]
    public async Task GetSingleResultAsync_WithNonExistentUser_ShouldReturnNull()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT u FROM User u WHERE u.Username = :username",
            _mockLogger.Object);
        query.SetParameter("username", "nonexistent");

        // Act
        var result = await query.GetSingleResultAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSingleResultRequiredAsync_WithValidQuery_ShouldReturnResult()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT u FROM User u WHERE u.Username = :username",
            _mockLogger.Object);
        query.SetParameter("username", "testuser1");

        // Act
        var result = await query.GetSingleResultRequiredAsync();

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("testuser1");
    }

    [Fact]
    public async Task GetSingleResultRequiredAsync_WithNoResults_ShouldThrowException()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT u FROM User u WHERE u.Username = :username",
            _mockLogger.Object);
        query.SetParameter("username", "nonexistent");

        // Act & Assert
        var action = async () => await query.GetSingleResultRequiredAsync();
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteUpdateAsync_WithValidQuery_ShouldUpdateRecords()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "UPDATE User u SET u.IsActive = :active WHERE u.Username = :username",
            _mockLogger.Object);
        query.SetParameter("active", false);
        query.SetParameter("username", "testuser3");

        // Act
        var affectedRows = await query.ExecuteUpdateAsync();

        // Assert
        affectedRows.Should().Be(1);

        // Verify the update worked
        var verifyQuery = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT u FROM User u WHERE u.Username = :username",
            _mockLogger.Object);
        verifyQuery.SetParameter("username", "testuser3");

        var updatedUser = await verifyQuery.GetSingleResultAsync();
        updatedUser.Should().NotBeNull();
        updatedUser!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteScalarAsync_WithCountQuery_ShouldReturnCount()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT COUNT(u.Id) FROM User u",
            _mockLogger.Object);

        // Act
        var result = await query.ExecuteScalarAsync();

        // Assert
        result.Should().NotBeNull();
        Convert.ToInt64(result).Should().Be(3); // We have 3 test users
    }

    [Fact]
    public async Task ExecuteScalarAsync_WithConditionalCount_ShouldReturnFilteredCount()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT COUNT(u.Id) FROM User u WHERE u.IsActive = :active",
            _mockLogger.Object);
        query.SetParameter("active", true);

        // Act
        var result = await query.ExecuteScalarAsync();

        // Assert
        result.Should().NotBeNull();
        Convert.ToInt64(result).Should().Be(2); // 2 active users
    }

    [Fact]
    public void Dispose_ShouldClearParameters()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT u FROM User u WHERE u.Username = :username",
            _mockLogger.Object);
        query.SetParameter("username", "testuser1");

        // Act
        query.Dispose();

        // Assert - Should not throw when disposed
        var action = () => query.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public async Task Operations_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT u FROM User u",
            _mockLogger.Object);
        query.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await query.GetResultListAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => query.SetParameter("test", 1));
    }

    [Fact]
    public async Task Query_WithInvalidCpql_ShouldThrowException()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "INVALID CPQL SYNTAX",
            _mockLogger.Object);

        // Act & Assert
        var action = async () => await query.GetResultListAsync();
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Query_WithOrderByClause_ShouldReturnOrderedResults()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT u FROM User u ORDER BY u.Username",
            _mockLogger.Object);

        // Act
        var results = await query.GetResultListAsync();

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(3);
        
        // Verify ordering (should be alphabetical by username)
        var usernames = results.Select(u => u.Username).ToList();
        usernames.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Query_WithComplexWhereClause_ShouldReturnCorrectResults()
    {
        // Arrange
        var query = new Query<User>(
            _connection,
            _parser,
            _sqlGenerator,
            _parameterBinder,
            _metadataProvider,
            "SELECT u FROM User u WHERE u.IsActive = :active AND u.Username LIKE :pattern",
            _mockLogger.Object);
        query.SetParameter("active", true);
        query.SetParameter("pattern", "testuser%");

        // Act
        var results = await query.GetResultListAsync();

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(2); // testuser1 and testuser3 are active
        results.Should().AllSatisfy(user => user.IsActive.Should().BeTrue());
        results.Should().AllSatisfy(user => user.Username.Should().StartWith("testuser"));
    }
}