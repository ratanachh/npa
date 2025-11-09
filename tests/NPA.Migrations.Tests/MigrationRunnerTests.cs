using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using NPA.Migrations;
using System.Data;
using Xunit;

namespace NPA.Migrations.Tests;

public class MigrationRunnerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly MigrationRunner _runner;

    public MigrationRunnerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _runner = new MigrationRunner(NullLogger<MigrationRunner>.Instance);
    }

    [Fact]
    public void RegisterMigration_ShouldAddMigration()
    {
        // Arrange
        var migration = new TestMigration(1, "Test");

        // Act
        _runner.RegisterMigration(migration);

        // Assert - Should not throw
    }

    [Fact]
    public void RegisterMigration_WithDuplicateVersion_ShouldThrow()
    {
        // Arrange
        var migration1 = new TestMigration(1, "Test1");
        var migration2 = new TestMigration(1, "Test2");
        _runner.RegisterMigration(migration1);

        // Act
        Action act = () => _runner.RegisterMigration(migration2);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public async Task RunMigrationsAsync_WithNoPendingMigrations_ShouldReturnEmpty()
    {
        // Act
        var result = await _runner.RunMigrationsAsync(_connection);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RunMigrationsAsync_ShouldApplyPendingMigrations()
    {
        // Arrange
        var migration1 = new TestMigration(1, "Migration1");
        var migration2 = new TestMigration(2, "Migration2");
        _runner.RegisterMigration(migration1);
        _runner.RegisterMigration(migration2);

        // Act
        var result = await _runner.RunMigrationsAsync(_connection);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(m => m.IsSuccessful.Should().BeTrue());
        migration1.WasUpCalled.Should().BeTrue();
        migration2.WasUpCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RunMigrationsAsync_WithFailedMigration_ShouldStopProcessing()
    {
        // Arrange
        var migration1 = new TestMigration(1, "Migration1");
        var migration2 = new FailingMigration(2, "Migration2");
        var migration3 = new TestMigration(3, "Migration3");

        _runner.RegisterMigration(migration1);
        _runner.RegisterMigration(migration2);
        _runner.RegisterMigration(migration3);

        // Act
        var result = await _runner.RunMigrationsAsync(_connection);

        // Assert
        result.Should().HaveCount(2); // Should stop after failure
        result[0].IsSuccessful.Should().BeTrue();
        result[1].IsSuccessful.Should().BeFalse();
        migration3.WasUpCalled.Should().BeFalse();
    }

    [Fact]
    public async Task GetPendingMigrationsAsync_ShouldReturnOnlyUnappliedMigrations()
    {
        // Arrange
        var migration1 = new TestMigration(1, "Migration1");
        var migration2 = new TestMigration(2, "Migration2");
        var migration3 = new TestMigration(3, "Migration3");

        _runner.RegisterMigration(migration1);
        _runner.RegisterMigration(migration2);
        _runner.RegisterMigration(migration3);

        // Apply first migration
        await _runner.RunMigrationsAsync(_connection);

        // Register a new migration
        var migration4 = new TestMigration(4, "Migration4");
        _runner.RegisterMigration(migration4);

        // Act
        var pending = await _runner.GetPendingMigrationsAsync(_connection);

        // Assert
        pending.Should().HaveCount(1);
        pending[0].Version.Should().Be(4);
    }

    [Fact]
    public async Task GetAppliedMigrationsAsync_ShouldReturnAppliedMigrations()
    {
        // Arrange
        var migration1 = new TestMigration(1, "Migration1");
        var migration2 = new TestMigration(2, "Migration2");
        _runner.RegisterMigration(migration1);
        _runner.RegisterMigration(migration2);

        await _runner.RunMigrationsAsync(_connection);

        // Act
        var applied = await _runner.GetAppliedMigrationsAsync(_connection);

        // Assert
        applied.Should().HaveCount(2);
        applied.Should().AllSatisfy(m => m.IsApplied.Should().BeTrue());
    }

    [Fact]
    public async Task GetCurrentVersionAsync_WithNoMigrations_ShouldReturnZero()
    {
        // Act
        var version = await _runner.GetCurrentVersionAsync(_connection);

        // Assert
        version.Should().Be(0);
    }

    [Fact]
    public async Task GetCurrentVersionAsync_ShouldReturnLatestVersion()
    {
        // Arrange
        var migration1 = new TestMigration(1, "Migration1");
        var migration2 = new TestMigration(2, "Migration2");
        _runner.RegisterMigration(migration1);
        _runner.RegisterMigration(migration2);

        await _runner.RunMigrationsAsync(_connection);

        // Act
        var version = await _runner.GetCurrentVersionAsync(_connection);

        // Assert
        version.Should().Be(2);
    }

    [Fact]
    public async Task RollbackLastMigrationAsync_ShouldRevertMigration()
    {
        // Arrange
        var migration = new TestMigration(1, "Migration1");
        _runner.RegisterMigration(migration);
        await _runner.RunMigrationsAsync(_connection);

        // Act
        var result = await _runner.RollbackLastMigrationAsync(_connection);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        migration.WasDownCalled.Should().BeTrue();

        var currentVersion = await _runner.GetCurrentVersionAsync(_connection);
        currentVersion.Should().Be(0);
    }

    [Fact]
    public async Task RunMigrationsAsync_WithTransaction_ShouldRollbackOnFailure()
    {
        // Arrange
        var migration = new FailingMigration(1, "FailingMigration");
        _runner.RegisterMigration(migration);

        // Act
        var result = await _runner.RunMigrationsAsync(_connection, useTransaction: true);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsSuccessful.Should().BeFalse();

        // Migration should not be recorded
        var applied = await _runner.GetAppliedMigrationsAsync(_connection);
        applied.Should().BeEmpty();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    // Test migration classes
    private class TestMigration : Migration
    {
        private readonly string _name;

        public TestMigration(long version, string name)
        {
            Version = version;
            _name = name;
        }

        public override string Name => _name;
        public override long Version { get; }
        public override DateTime CreatedAt => DateTime.UtcNow;
        public override string Description => $"Test migration {_name}";

        public bool WasUpCalled { get; private set; }
        public bool WasDownCalled { get; private set; }

        public override Task UpAsync(IDbConnection connection)
        {
            WasUpCalled = true;
            return Task.CompletedTask;
        }

        public override Task DownAsync(IDbConnection connection)
        {
            WasDownCalled = true;
            return Task.CompletedTask;
        }
    }

    private class FailingMigration : Migration
    {
        private readonly string _name;

        public FailingMigration(long version, string name)
        {
            Version = version;
            _name = name;
        }

        public override string Name => _name;
        public override long Version { get; }
        public override DateTime CreatedAt => DateTime.UtcNow;
        public override string Description => "Failing migration";

        public override Task UpAsync(IDbConnection connection)
        {
            throw new InvalidOperationException("Migration failed intentionally");
        }

        public override Task DownAsync(IDbConnection connection)
        {
            throw new InvalidOperationException("Rollback failed intentionally");
        }
    }
}
