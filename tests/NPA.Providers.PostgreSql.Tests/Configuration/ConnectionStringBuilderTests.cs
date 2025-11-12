using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NPA.Core.Configuration;
using NPA.Providers.PostgreSql.Extensions;
using Npgsql;
using System.Data;
using Xunit;

namespace NPA.Providers.PostgreSql.Tests.Configuration;

/// <summary>
/// Tests for PostgreSQL connection string builder with pooling configuration.
/// </summary>
public class ConnectionStringBuilderTests
{
    private const string BaseConnectionString = "Host=localhost;Database=testdb;Username=postgres;Password=postgres;";

    [Fact]
    public void BuildConnectionString_WithDefaultPooling_ShouldApplyDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new PostgreSqlOptions();

        // Act
        services.AddPostgreSqlProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.Enabled = options.Pooling.Enabled;
            opts.Pooling.MinPoolSize = options.Pooling.MinPoolSize;
            opts.Pooling.MaxPoolSize = options.Pooling.MaxPoolSize;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var npgsqlConnection = connection.Should().BeOfType<NpgsqlConnection>().Subject;
        var builder = new NpgsqlConnectionStringBuilder(npgsqlConnection.ConnectionString);
        
        builder.Pooling.Should().BeTrue();
        builder.MinPoolSize.Should().Be(5);
        builder.MaxPoolSize.Should().Be(100);
    }

    [Fact]
    public void BuildConnectionString_WithPoolingDisabled_ShouldDisablePooling()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPostgreSqlProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.Enabled = false;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var npgsqlConnection = connection.Should().BeOfType<NpgsqlConnection>().Subject;
        var builder = new NpgsqlConnectionStringBuilder(npgsqlConnection.ConnectionString);
        
        builder.Pooling.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, 50)]
    [InlineData(5, 100)]
    [InlineData(10, 200)]
    [InlineData(20, 500)]
    public void BuildConnectionString_WithCustomPoolSize_ShouldApplyPoolSize(int minSize, int maxSize)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPostgreSqlProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.MinPoolSize = minSize;
            opts.Pooling.MaxPoolSize = maxSize;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var npgsqlConnection = connection.Should().BeOfType<NpgsqlConnection>().Subject;
        var builder = new NpgsqlConnectionStringBuilder(npgsqlConnection.ConnectionString);
        
        builder.MinPoolSize.Should().Be(minSize);
        builder.MaxPoolSize.Should().Be(maxSize);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    public void BuildConnectionString_WithConnectionTimeout_ShouldApplyTimeout(int timeoutSeconds)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPostgreSqlProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.ConnectionTimeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var npgsqlConnection = connection.Should().BeOfType<NpgsqlConnection>().Subject;
        var builder = new NpgsqlConnectionStringBuilder(npgsqlConnection.ConnectionString);
        
        builder.Timeout.Should().Be(timeoutSeconds);
    }

    [Theory]
    [InlineData(60)]
    [InlineData(300)]
    [InlineData(600)]
    [InlineData(1800)]
    public void BuildConnectionString_WithConnectionLifetime_ShouldApplyConnectionLifetime(int lifetimeSeconds)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPostgreSqlProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.ConnectionLifetime = TimeSpan.FromSeconds(lifetimeSeconds);
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var npgsqlConnection = connection.Should().BeOfType<NpgsqlConnection>().Subject;
        var builder = new NpgsqlConnectionStringBuilder(npgsqlConnection.ConnectionString);
        
        builder.ConnectionLifetime.Should().Be(lifetimeSeconds);
    }

    [Theory]
    [InlineData(60)]
    [InlineData(180)]
    [InlineData(300)]
    [InlineData(600)]
    public void BuildConnectionString_WithIdleTimeout_ShouldApplyConnectionIdleLifetime(int idleTimeoutSeconds)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPostgreSqlProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.IdleTimeout = TimeSpan.FromSeconds(idleTimeoutSeconds);
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var npgsqlConnection = connection.Should().BeOfType<NpgsqlConnection>().Subject;
        var builder = new NpgsqlConnectionStringBuilder(npgsqlConnection.ConnectionString);
        
        builder.ConnectionIdleLifetime.Should().Be(idleTimeoutSeconds);
    }

    [Theory]
    [InlineData("Disable")]
    [InlineData("Allow")]
    [InlineData("Prefer")]
    [InlineData("Require")]
    public void BuildConnectionString_WithSslMode_ShouldApplySslMode(string sslMode)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPostgreSqlProvider(BaseConnectionString, opts =>
        {
            opts.SslMode = sslMode;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var npgsqlConnection = connection.Should().BeOfType<NpgsqlConnection>().Subject;
        var builder = new NpgsqlConnectionStringBuilder(npgsqlConnection.ConnectionString);
        
        builder.SslMode.ToString().Should().Be(sslMode);
    }

    [Fact]
    public void BuildConnectionString_WithApplicationName_ShouldSetApplicationName()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPostgreSqlProvider(BaseConnectionString, opts =>
        {
            opts.ApplicationName = "NPA.Tests";
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var npgsqlConnection = connection.Should().BeOfType<NpgsqlConnection>().Subject;
        var builder = new NpgsqlConnectionStringBuilder(npgsqlConnection.ConnectionString);
        
        builder.ApplicationName.Should().Be("NPA.Tests");
    }

    [Fact]
    public void BuildConnectionString_ProductionConfiguration_ShouldCombineAllSettings()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPostgreSqlProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.Enabled = true;
            opts.Pooling.MinPoolSize = 10;
            opts.Pooling.MaxPoolSize = 200;
            opts.Pooling.ConnectionTimeout = TimeSpan.FromSeconds(60);
            opts.Pooling.ConnectionLifetime = TimeSpan.FromMinutes(30);
            opts.Pooling.IdleTimeout = TimeSpan.FromMinutes(10);
            opts.SslMode = "Require";
            opts.ApplicationName = "NPA.Production";
            opts.CommandTimeout = 120;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var npgsqlConnection = connection.Should().BeOfType<NpgsqlConnection>().Subject;
        var builder = new NpgsqlConnectionStringBuilder(npgsqlConnection.ConnectionString);
        
        builder.Pooling.Should().BeTrue();
        builder.MinPoolSize.Should().Be(10);
        builder.MaxPoolSize.Should().Be(200);
        builder.Timeout.Should().Be(60);
        builder.ConnectionLifetime.Should().Be(1800); // 30 minutes
        builder.ConnectionIdleLifetime.Should().Be(600); // 10 minutes
        builder.SslMode.Should().Be(SslMode.Require);
        builder.ApplicationName.Should().Be("NPA.Production");
        builder.CommandTimeout.Should().Be(120);
    }

    [Fact]
    public void BuildConnectionString_WithKeepAlive_ShouldSetKeepAlive()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPostgreSqlProvider(BaseConnectionString, opts =>
        {
            opts.KeepAlive = 60;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var npgsqlConnection = connection.Should().BeOfType<NpgsqlConnection>().Subject;
        var builder = new NpgsqlConnectionStringBuilder(npgsqlConnection.ConnectionString);
        
        builder.KeepAlive.Should().Be(60);
    }

    [Fact]
    public void BuildConnectionString_ShouldPreserveExistingConnectionStringParameters()
    {
        // Arrange
        var services = new ServiceCollection();
        var customConnectionString = "Host=localhost;Port=5432;Database=testdb;Username=admin;Password=Pass123;";

        // Act
        services.AddPostgreSqlProvider(customConnectionString, opts =>
        {
            opts.Pooling.MinPoolSize = 5;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var npgsqlConnection = connection.Should().BeOfType<NpgsqlConnection>().Subject;
        var builder = new NpgsqlConnectionStringBuilder(npgsqlConnection.ConnectionString);
        
        builder.Host.Should().Be("localhost");
        builder.Port.Should().Be(5432);
        builder.Database.Should().Be("testdb");
        builder.Username.Should().Be("admin");
        builder.MinPoolSize.Should().Be(5);
    }

    [Fact]
    public void BuildConnectionString_WithoutConfiguration_ShouldUseDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPostgreSqlProvider(BaseConnectionString);

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert - Should use Npgsql defaults when no options provided
        var npgsqlConnection = connection.Should().BeOfType<NpgsqlConnection>().Subject;
        var builder = new NpgsqlConnectionStringBuilder(npgsqlConnection.ConnectionString);
        
        // Connection string should contain basic parameters
        builder.Host.Should().Be("localhost");
        builder.Database.Should().Be("testdb");
    }

    [Fact]
    public void BuildConnectionString_PgBouncerConfiguration_ShouldMinimizePooling()
    {
        // Arrange - When using PgBouncer, minimize client pooling
        var services = new ServiceCollection();

        // Act
        services.AddPostgreSqlProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.Enabled = true;
            opts.Pooling.MinPoolSize = 0; // No minimum
            opts.Pooling.MaxPoolSize = 10; // Small max
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var npgsqlConnection = connection.Should().BeOfType<NpgsqlConnection>().Subject;
        var builder = new NpgsqlConnectionStringBuilder(npgsqlConnection.ConnectionString);
        
        builder.Pooling.Should().BeTrue();
        builder.MinPoolSize.Should().Be(0);
        builder.MaxPoolSize.Should().Be(10);
    }
}
