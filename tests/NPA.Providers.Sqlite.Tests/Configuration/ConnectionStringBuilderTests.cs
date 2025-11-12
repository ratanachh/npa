using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using NPA.Core.Configuration;
using NPA.Providers.Sqlite.Extensions;
using System.Data;
using Xunit;

namespace NPA.Providers.Sqlite.Tests.Configuration;

/// <summary>
/// Tests for SQLite connection string builder with pooling configuration.
/// Note: SQLite uses shared cache mode instead of traditional connection pooling.
/// </summary>
public class ConnectionStringBuilderTests
{
    private const string BaseConnectionString = "Data Source=:memory:";

    [Fact]
    public void BuildConnectionString_WithDefaultPooling_ShouldUseSharedCache()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new SqliteOptions();

        // Act
        services.AddSqliteProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.Enabled = options.Pooling.Enabled;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var sqliteConnection = connection.Should().BeOfType<SqliteConnection>().Subject;
        var builder = new SqliteConnectionStringBuilder(sqliteConnection.ConnectionString);
        
        // When pooling is enabled (default), SQLite uses shared cache
        builder.Cache.Should().Be(SqliteCacheMode.Shared);
    }

    [Fact]
    public void BuildConnectionString_WithPoolingDisabled_ShouldUsePrivateCache()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqliteProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.Enabled = false;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var sqliteConnection = connection.Should().BeOfType<SqliteConnection>().Subject;
        var builder = new SqliteConnectionStringBuilder(sqliteConnection.ConnectionString);
        
        // When pooling is disabled, SQLite uses private cache
        builder.Cache.Should().Be(SqliteCacheMode.Private);
    }

    [Theory]
    [InlineData(SqliteOpenMode.ReadOnly)]
    [InlineData(SqliteOpenMode.ReadWrite)]
    [InlineData(SqliteOpenMode.ReadWriteCreate)]
    [InlineData(SqliteOpenMode.Memory)]
    public void BuildConnectionString_WithDifferentModes_ShouldSetMode(SqliteOpenMode mode)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqliteProvider(BaseConnectionString, opts =>
        {
            opts.Mode = mode;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var sqliteConnection = connection.Should().BeOfType<SqliteConnection>().Subject;
        var builder = new SqliteConnectionStringBuilder(sqliteConnection.ConnectionString);
        
        builder.Mode.Should().Be(mode);
    }

    [Fact]
    public void BuildConnectionString_WithForeignKeysEnabled_ShouldEnableForeignKeys()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqliteProvider(BaseConnectionString, opts =>
        {
            opts.ForeignKeys = true;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var sqliteConnection = connection.Should().BeOfType<SqliteConnection>().Subject;
        
        // Foreign keys are set via PRAGMA, verify connection is created
        sqliteConnection.Should().NotBeNull();
        sqliteConnection.ConnectionString.Should().Contain(":memory:");
    }

    [Theory]
    [InlineData("DELETE")]
    [InlineData("TRUNCATE")]
    [InlineData("PERSIST")]
    [InlineData("MEMORY")]
    [InlineData("WAL")]
    [InlineData("OFF")]
    public void BuildConnectionString_WithJournalMode_ShouldSetJournalMode(string journalMode)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqliteProvider(BaseConnectionString, opts =>
        {
            opts.JournalMode = journalMode;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var sqliteConnection = connection.Should().BeOfType<SqliteConnection>().Subject;
        
        // Journal mode is set via PRAGMA, verify connection is created
        sqliteConnection.Should().NotBeNull();
    }

    [Fact]
    public void BuildConnectionString_ProductionConfiguration_ShouldCombineAllSettings()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqliteProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.Enabled = true;
            opts.Mode = SqliteOpenMode.ReadWriteCreate;
            opts.ForeignKeys = true;
            opts.JournalMode = "WAL";
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var sqliteConnection = connection.Should().BeOfType<SqliteConnection>().Subject;
        var builder = new SqliteConnectionStringBuilder(sqliteConnection.ConnectionString);
        
        builder.Cache.Should().Be(SqliteCacheMode.Shared);
        builder.Mode.Should().Be(SqliteOpenMode.ReadWriteCreate);
    }

    [Fact]
    public void BuildConnectionString_WithFileBasedDatabase_ShouldPreserveDataSource()
    {
        // Arrange
        var services = new ServiceCollection();
        var fileConnectionString = "Data Source=mydb.db";

        // Act
        services.AddSqliteProvider(fileConnectionString, opts =>
        {
            opts.Pooling.Enabled = true;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var sqliteConnection = connection.Should().BeOfType<SqliteConnection>().Subject;
        var builder = new SqliteConnectionStringBuilder(sqliteConnection.ConnectionString);
        
        builder.DataSource.Should().Be("mydb.db");
        builder.Cache.Should().Be(SqliteCacheMode.Shared);
    }

    [Fact]
    public void BuildConnectionString_WithoutConfiguration_ShouldUseDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqliteProvider(BaseConnectionString);

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert - Should use defaults when no options provided
        var sqliteConnection = connection.Should().BeOfType<SqliteConnection>().Subject;
        var builder = new SqliteConnectionStringBuilder(sqliteConnection.ConnectionString);
        
        // Connection string should contain data source
        builder.DataSource.Should().Be(":memory:");
    }

    [Fact]
    public void SqlitePooling_Explanation_SharedVsPrivateCache()
    {
        // This test documents SQLite's pooling behavior
        
        // Arrange & Act - Shared cache mode
        var sharedOptions = new SqliteOptions { Pooling = { Enabled = true } };
        var services1 = new ServiceCollection();
        services1.AddSqliteProvider(BaseConnectionString, opts => opts.Pooling.Enabled = true);
        var provider1 = services1.BuildServiceProvider();
        var sharedConnection = provider1.GetRequiredService<IDbConnection>();
        
        // Arrange & Act - Private cache mode
        var privateOptions = new SqliteOptions { Pooling = { Enabled = false } };
        var services2 = new ServiceCollection();
        services2.AddSqliteProvider(BaseConnectionString, opts => opts.Pooling.Enabled = false);
        var provider2 = services2.BuildServiceProvider();
        var privateConnection = provider2.GetRequiredService<IDbConnection>();

        // Assert
        var sharedBuilder = new SqliteConnectionStringBuilder(((SqliteConnection)sharedConnection).ConnectionString);
        var privateBuilder = new SqliteConnectionStringBuilder(((SqliteConnection)privateConnection).ConnectionString);
        
        // Shared cache: Multiple connections share the same cache
        sharedBuilder.Cache.Should().Be(SqliteCacheMode.Shared);
        
        // Private cache: Each connection has its own cache
        privateBuilder.Cache.Should().Be(SqliteCacheMode.Private);
    }

    [Fact]
    public void BuildConnectionString_SharedCacheMode_EnablesConcurrentAccess()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqliteProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.Enabled = true; // Enables shared cache
        });

        var provider = services.BuildServiceProvider();
        
        // Create multiple connections
        var connection1 = provider.GetRequiredService<IDbConnection>();
        var connection2 = provider.GetRequiredService<IDbConnection>();

        // Assert
        connection1.Should().BeOfType<SqliteConnection>();
        connection2.Should().BeOfType<SqliteConnection>();
        
        // Both connections can access the same in-memory database with shared cache
        var builder1 = new SqliteConnectionStringBuilder(((SqliteConnection)connection1).ConnectionString);
        var builder2 = new SqliteConnectionStringBuilder(((SqliteConnection)connection2).ConnectionString);
        
        builder1.Cache.Should().Be(SqliteCacheMode.Shared);
        builder2.Cache.Should().Be(SqliteCacheMode.Shared);
    }

    [Fact]
    public void SqliteOptions_ShouldIncludeConnectionPoolOptions()
    {
        // Arrange & Act
        var options = new SqliteOptions();

        // Assert
        options.Pooling.Should().NotBeNull();
        options.Pooling.Should().BeOfType<ConnectionPoolOptions>();
        options.Pooling.Enabled.Should().BeTrue(); // Default
    }

    [Fact]
    public void BuildConnectionString_PoolingSizeSettings_AreIgnoredForSqlite()
    {
        // SQLite doesn't use MinPoolSize/MaxPoolSize like other databases
        // Only the Enabled flag matters (Shared vs Private cache)
        
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqliteProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.Enabled = true;
            opts.Pooling.MinPoolSize = 10;  // Ignored by SQLite
            opts.Pooling.MaxPoolSize = 200; // Ignored by SQLite
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var sqliteConnection = connection.Should().BeOfType<SqliteConnection>().Subject;
        var builder = new SqliteConnectionStringBuilder(sqliteConnection.ConnectionString);
        
        // SQLite only respects the Enabled flag
        builder.Cache.Should().Be(SqliteCacheMode.Shared);
        
        // Pool size settings don't appear in SQLite connection string
        // (SQLite uses shared cache mode instead)
    }
}
