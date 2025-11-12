using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using NPA.Core.Configuration;
using NPA.Providers.MySql.Extensions;
using System.Data;
using Xunit;

namespace NPA.Providers.MySql.Tests.Configuration;

/// <summary>
/// Tests for MySQL connection string builder with pooling configuration.
/// </summary>
public class ConnectionStringBuilderTests
{
    private const string BaseConnectionString = "Server=localhost;Database=testdb;User=root;Password=password;";

    [Fact]
    public void BuildConnectionString_WithDefaultPooling_ShouldApplyDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new MySqlOptions();

        // Act
        services.AddNpaMySql(BaseConnectionString, opts =>
        {
            opts.Pooling.Enabled = options.Pooling.Enabled;
            opts.Pooling.MinPoolSize = options.Pooling.MinPoolSize;
            opts.Pooling.MaxPoolSize = options.Pooling.MaxPoolSize;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var mySqlConnection = connection.Should().BeOfType<MySqlConnection>().Subject;
        var builder = new MySqlConnectionStringBuilder(mySqlConnection.ConnectionString);
        
        builder.Pooling.Should().BeTrue();
        builder.MinimumPoolSize.Should().Be(5);
        builder.MaximumPoolSize.Should().Be(100);
    }

    [Fact]
    public void BuildConnectionString_WithPoolingDisabled_ShouldDisablePooling()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNpaMySql(BaseConnectionString, opts =>
        {
            opts.Pooling.Enabled = false;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var mySqlConnection = connection.Should().BeOfType<MySqlConnection>().Subject;
        var builder = new MySqlConnectionStringBuilder(mySqlConnection.ConnectionString);
        
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
        services.AddNpaMySql(BaseConnectionString, opts =>
        {
            opts.Pooling.MinPoolSize = minSize;
            opts.Pooling.MaxPoolSize = maxSize;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var mySqlConnection = connection.Should().BeOfType<MySqlConnection>().Subject;
        var builder = new MySqlConnectionStringBuilder(mySqlConnection.ConnectionString);
        
        builder.MinimumPoolSize.Should().Be((uint)minSize);
        builder.MaximumPoolSize.Should().Be((uint)maxSize);
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
        services.AddNpaMySql(BaseConnectionString, opts =>
        {
            opts.Pooling.ConnectionTimeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var mySqlConnection = connection.Should().BeOfType<MySqlConnection>().Subject;
        var builder = new MySqlConnectionStringBuilder(mySqlConnection.ConnectionString);
        
        builder.ConnectionTimeout.Should().Be((uint)timeoutSeconds);
    }

    [Theory]
    [InlineData(60)]
    [InlineData(300)]
    [InlineData(600)]
    [InlineData(1800)]
    public void BuildConnectionString_WithConnectionLifetime_ShouldApplyConnectionLifeTime(int lifetimeSeconds)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNpaMySql(BaseConnectionString, opts =>
        {
            opts.Pooling.ConnectionLifetime = TimeSpan.FromSeconds(lifetimeSeconds);
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var mySqlConnection = connection.Should().BeOfType<MySqlConnection>().Subject;
        var builder = new MySqlConnectionStringBuilder(mySqlConnection.ConnectionString);
        
        builder.ConnectionLifeTime.Should().Be((uint)lifetimeSeconds);
    }

    [Theory]
    [InlineData(60)]
    [InlineData(180)]
    [InlineData(300)]
    [InlineData(600)]
    public void BuildConnectionString_WithIdleTimeout_ShouldApplyConnectionIdleTimeout(int idleTimeoutSeconds)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNpaMySql(BaseConnectionString, opts =>
        {
            opts.Pooling.IdleTimeout = TimeSpan.FromSeconds(idleTimeoutSeconds);
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var mySqlConnection = connection.Should().BeOfType<MySqlConnection>().Subject;
        var builder = new MySqlConnectionStringBuilder(mySqlConnection.ConnectionString);
        
        builder.ConnectionIdleTimeout.Should().Be((uint)idleTimeoutSeconds);
    }

    [Fact]
    public void BuildConnectionString_WithResetOnReturn_ShouldSetConnectionReset()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNpaMySql(BaseConnectionString, opts =>
        {
            opts.Pooling.ResetOnReturn = true;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var mySqlConnection = connection.Should().BeOfType<MySqlConnection>().Subject;
        var builder = new MySqlConnectionStringBuilder(mySqlConnection.ConnectionString);
        
        builder.ConnectionReset.Should().BeTrue();
    }

    [Theory]
    [InlineData("None")]
    [InlineData("Preferred")]
    [InlineData("Required")]
    [InlineData("VerifyCA")]
    [InlineData("VerifyFull")]
    public void BuildConnectionString_WithSslMode_ShouldApplySslMode(string sslMode)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNpaMySql(BaseConnectionString, opts =>
        {
            opts.SslMode = sslMode;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var mySqlConnection = connection.Should().BeOfType<MySqlConnection>().Subject;
        var builder = new MySqlConnectionStringBuilder(mySqlConnection.ConnectionString);
        
        builder.SslMode.ToString().Should().Be(sslMode);
    }

    [Fact]
    public void BuildConnectionString_WithCharacterSet_ShouldSetCharacterSet()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNpaMySql(BaseConnectionString, opts =>
        {
            opts.CharacterSet = "utf8mb4";
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var mySqlConnection = connection.Should().BeOfType<MySqlConnection>().Subject;
        var builder = new MySqlConnectionStringBuilder(mySqlConnection.ConnectionString);
        
        builder.CharacterSet.Should().Be("utf8mb4");
    }

    [Fact]
    public void BuildConnectionString_ProductionConfiguration_ShouldCombineAllSettings()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNpaMySql(BaseConnectionString, opts =>
        {
            opts.Pooling.Enabled = true;
            opts.Pooling.MinPoolSize = 10;
            opts.Pooling.MaxPoolSize = 200;
            opts.Pooling.ConnectionTimeout = TimeSpan.FromSeconds(60);
            opts.Pooling.ConnectionLifetime = TimeSpan.FromMinutes(30);
            opts.Pooling.IdleTimeout = TimeSpan.FromMinutes(10);
            opts.Pooling.ResetOnReturn = true;
            opts.SslMode = "Required";
            opts.CharacterSet = "utf8mb4";
            opts.UseCompression = true;
            opts.CommandTimeout = 120;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var mySqlConnection = connection.Should().BeOfType<MySqlConnection>().Subject;
        var builder = new MySqlConnectionStringBuilder(mySqlConnection.ConnectionString);
        
        builder.Pooling.Should().BeTrue();
        builder.MinimumPoolSize.Should().Be(10);
        builder.MaximumPoolSize.Should().Be(200);
        builder.ConnectionTimeout.Should().Be(60);
        builder.ConnectionLifeTime.Should().Be(1800); // 30 minutes
        builder.ConnectionIdleTimeout.Should().Be(600); // 10 minutes
        builder.ConnectionReset.Should().BeTrue();
        builder.SslMode.Should().Be(MySqlSslMode.Required);
        builder.CharacterSet.Should().Be("utf8mb4");
        builder.UseCompression.Should().BeTrue();
        builder.DefaultCommandTimeout.Should().Be(120);
    }

    [Fact]
    public void BuildConnectionString_WithAllowLoadLocalInfile_ShouldSetAllowLoadLocalInfile()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNpaMySql(BaseConnectionString, opts =>
        {
            opts.AllowLoadLocalInfile = true;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var mySqlConnection = connection.Should().BeOfType<MySqlConnection>().Subject;
        var builder = new MySqlConnectionStringBuilder(mySqlConnection.ConnectionString);
        
        builder.AllowLoadLocalInfile.Should().BeTrue();
    }

    [Fact]
    public void BuildConnectionString_WithAllowUserVariables_ShouldSetAllowUserVariables()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNpaMySql(BaseConnectionString, opts =>
        {
            opts.AllowUserVariables = true;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var mySqlConnection = connection.Should().BeOfType<MySqlConnection>().Subject;
        var builder = new MySqlConnectionStringBuilder(mySqlConnection.ConnectionString);
        
        builder.AllowUserVariables.Should().BeTrue();
    }

    [Fact]
    public void BuildConnectionString_ShouldPreserveExistingConnectionStringParameters()
    {
        // Arrange
        var services = new ServiceCollection();
        var customConnectionString = "Server=localhost;Port=3306;Database=testdb;User=admin;Password=Pass123;";

        // Act
        services.AddNpaMySql(customConnectionString, opts =>
        {
            opts.Pooling.MinPoolSize = 5;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var mySqlConnection = connection.Should().BeOfType<MySqlConnection>().Subject;
        var builder = new MySqlConnectionStringBuilder(mySqlConnection.ConnectionString);
        
        builder.Server.Should().Be("localhost");
        builder.Port.Should().Be(3306);
        builder.Database.Should().Be("testdb");
        builder.UserID.Should().Be("admin");
        builder.MinimumPoolSize.Should().Be(5);
    }

    [Fact]
    public void BuildConnectionString_WithoutConfiguration_ShouldUseDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNpaMySql(BaseConnectionString);

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert - Should use MySqlConnector defaults when no options provided
        var mySqlConnection = connection.Should().BeOfType<MySqlConnection>().Subject;
        var builder = new MySqlConnectionStringBuilder(mySqlConnection.ConnectionString);
        
        // Connection string should contain basic parameters
        builder.Server.Should().Be("localhost");
        builder.Database.Should().Be("testdb");
    }

    [Fact]
    public void BuildConnectionString_ProxySqlConfiguration_ShouldMinimizePooling()
    {
        // Arrange - When using ProxySQL, minimize client pooling
        var services = new ServiceCollection();

        // Act
        services.AddNpaMySql(BaseConnectionString, opts =>
        {
            opts.Pooling.Enabled = true;
            opts.Pooling.MinPoolSize = 0; // No minimum
            opts.Pooling.MaxPoolSize = 10; // Small max
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var mySqlConnection = connection.Should().BeOfType<MySqlConnection>().Subject;
        var builder = new MySqlConnectionStringBuilder(mySqlConnection.ConnectionString);
        
        builder.Pooling.Should().BeTrue();
        builder.MinimumPoolSize.Should().Be(0);
        builder.MaximumPoolSize.Should().Be(10);
    }
}
