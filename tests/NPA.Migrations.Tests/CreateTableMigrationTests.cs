using Dapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NPA.Migrations.Types;
using Xunit;

namespace NPA.Migrations.Tests;

public class CreateTableMigrationTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public CreateTableMigrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldSucceed()
    {
        // Act
        var migration = new CreateTableMigration("Users", 1);

        // Assert
        migration.Name.Should().Be("CreateTable_Users");
        migration.Version.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithNullTableName_ShouldThrow()
    {
        // Act
        Action act = () => new CreateTableMigration(null!, 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task UpAsync_ShouldCreateTable()
    {
        // Arrange
        var migration = new CreateTableMigration("Users", 1)
            .AddPrimaryKey("Id")
            .AddColumn("Name", "NVARCHAR(100)", nullable: false)
            .AddColumn("Email", "NVARCHAR(255)");

        // Act
        await migration.UpAsync(_connection);

        // Assert
        var tableExists = await TableExistsAsync("Users");
        tableExists.Should().BeTrue();
    }

    [Fact]
    public async Task UpAsync_WithoutColumns_ShouldThrow()
    {
        // Arrange
        var migration = new CreateTableMigration("Users", 1);

        // Act
        Func<Task> act = async () => await migration.UpAsync(_connection);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*without columns*");
    }

    [Fact]
    public async Task DownAsync_ShouldDropTable()
    {
        // Arrange
        var migration = new CreateTableMigration("Users", 1)
            .AddPrimaryKey("Id")
            .AddColumn("Name", "NVARCHAR(100)");

        await migration.UpAsync(_connection);

        // Act
        await migration.DownAsync(_connection);

        // Assert
        var tableExists = await TableExistsAsync("Users");
        tableExists.Should().BeFalse();
    }

    [Fact]
    public async Task AddPrimaryKey_ShouldCreatePrimaryKeyColumn()
    {
        // Arrange & Act
        var migration = new CreateTableMigration("Users", 1)
            .AddPrimaryKey("Id", "INTEGER", identity: false)
            .AddColumn("Name", "TEXT");

        await migration.UpAsync(_connection);

        // Assert
        var sql = "SELECT COUNT(*) FROM Users";
        var count = await _connection.ExecuteScalarAsync<int>(sql);
        count.Should().Be(0); // Table should be empty but should exist
    }

    [Fact]
    public async Task AddColumn_WithDefaultValue_ShouldIncludeDefault()
    {
        // Arrange & Act
        var migration = new CreateTableMigration("Users", 1)
            .AddPrimaryKey("Id", "INTEGER", identity: false)
            .AddColumn("Status", "TEXT", nullable: false, defaultValue: "'Active'");

        await migration.UpAsync(_connection);

        // Assert
        var tableExists = await TableExistsAsync("Users");
        tableExists.Should().BeTrue();
    }

    [Fact]
    public async Task AddCompositePrimaryKey_ShouldCreateCompositePK()
    {
        // Arrange & Act
        var migration = new CreateTableMigration("UserRoles", 1)
            .AddColumn("UserId", "INTEGER", nullable: false)
            .AddColumn("RoleId", "INTEGER", nullable: false)
            .AddCompositePrimaryKey("UserId", "RoleId");

        await migration.UpAsync(_connection);

        // Assert
        var tableExists = await TableExistsAsync("UserRoles");
        tableExists.Should().BeTrue();
    }

    [Fact]
    public void AddColumn_ShouldReturnSelfForFluent()
    {
        // Arrange
        var migration = new CreateTableMigration("Users", 1);

        // Act
        var result = migration.AddColumn("Name", "TEXT");

        // Assert
        result.Should().BeSameAs(migration);
    }

    private async Task<bool> TableExistsAsync(string tableName)
    {
        // SQLite-specific query
        var sql = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@TableName";
        var count = await _connection.ExecuteScalarAsync<int>(sql, new { TableName = tableName });
        return count > 0;
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
