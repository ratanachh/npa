using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NPA.Core.Configuration;
using NPA.Providers.SqlServer.Extensions;
using System.Data;
using Xunit;

namespace NPA.Providers.SqlServer.Tests.Configuration;

/// <summary>
/// Tests for SQL Server connection string builder with pooling configuration.
/// </summary>
public class ConnectionStringBuilderTests
{
    private const string BaseConnectionString = "Server=localhost;Database=TestDB;User Id=sa;Password=YourPassword123;";

    [Fact]
    public void BuildConnectionString_WithDefaultPooling_ShouldApplyDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new SqlServerOptions();

        // Act
        services.AddSqlServerProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.Enabled = options.Pooling.Enabled;
            opts.Pooling.MinPoolSize = options.Pooling.MinPoolSize;
            opts.Pooling.MaxPoolSize = options.Pooling.MaxPoolSize;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var sqlConnection = connection.Should().BeOfType<SqlConnection>().Subject;
        var builder = new SqlConnectionStringBuilder(sqlConnection.ConnectionString);
        
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
        services.AddSqlServerProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.Enabled = false;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var sqlConnection = connection.Should().BeOfType<SqlConnection>().Subject;
        var builder = new SqlConnectionStringBuilder(sqlConnection.ConnectionString);
        
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
        services.AddSqlServerProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.MinPoolSize = minSize;
            opts.Pooling.MaxPoolSize = maxSize;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var sqlConnection = connection.Should().BeOfType<SqlConnection>().Subject;
        var builder = new SqlConnectionStringBuilder(sqlConnection.ConnectionString);
        
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
        services.AddSqlServerProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.ConnectionTimeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var sqlConnection = connection.Should().BeOfType<SqlConnection>().Subject;
        var builder = new SqlConnectionStringBuilder(sqlConnection.ConnectionString);
        
        builder.ConnectTimeout.Should().Be(timeoutSeconds);
    }

    [Theory]
    [InlineData(60)]
    [InlineData(300)]
    [InlineData(600)]
    [InlineData(1800)]
    public void BuildConnectionString_WithConnectionLifetime_ShouldApplyLoadBalanceTimeout(int lifetimeSeconds)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqlServerProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.ConnectionLifetime = TimeSpan.FromSeconds(lifetimeSeconds);
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var sqlConnection = connection.Should().BeOfType<SqlConnection>().Subject;
        var builder = new SqlConnectionStringBuilder(sqlConnection.ConnectionString);
        
        builder.LoadBalanceTimeout.Should().Be(lifetimeSeconds);
    }

    [Fact]
    public void BuildConnectionString_WithMarsEnabled_ShouldEnableMars()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqlServerProvider(BaseConnectionString, opts =>
        {
            opts.EnableMars = true;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var sqlConnection = connection.Should().BeOfType<SqlConnection>().Subject;
        var builder = new SqlConnectionStringBuilder(sqlConnection.ConnectionString);
        
        builder.MultipleActiveResultSets.Should().BeTrue();
    }

    [Fact]
    public void BuildConnectionString_WithEncryption_ShouldSetEncrypt()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqlServerProvider(BaseConnectionString, opts =>
        {
            opts.Encrypt = true;
            opts.TrustServerCertificate = true;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var sqlConnection = connection.Should().BeOfType<SqlConnection>().Subject;
        var builder = new SqlConnectionStringBuilder(sqlConnection.ConnectionString);
        
        // Encrypt is SqlConnectionEncryptOption enum in newer versions
        builder.Encrypt.ToString().Should().Be("True");
        builder.TrustServerCertificate.Should().BeTrue();
    }

    [Fact]
    public void BuildConnectionString_ProductionConfiguration_ShouldCombineAllSettings()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqlServerProvider(BaseConnectionString, opts =>
        {
            opts.Pooling.Enabled = true;
            opts.Pooling.MinPoolSize = 10;
            opts.Pooling.MaxPoolSize = 200;
            opts.Pooling.ConnectionTimeout = TimeSpan.FromSeconds(60);
            opts.Pooling.ConnectionLifetime = TimeSpan.FromMinutes(30);
            opts.EnableMars = true;
            opts.Encrypt = true;
            opts.CommandTimeout = 120;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var sqlConnection = connection.Should().BeOfType<SqlConnection>().Subject;
        var builder = new SqlConnectionStringBuilder(sqlConnection.ConnectionString);
        
        builder.Pooling.Should().BeTrue();
        builder.MinPoolSize.Should().Be(10);
        builder.MaxPoolSize.Should().Be(200);
        builder.ConnectTimeout.Should().Be(60);
        builder.LoadBalanceTimeout.Should().Be(1800); // 30 minutes
        builder.MultipleActiveResultSets.Should().BeTrue();
        // Encrypt is SqlConnectionEncryptOption enum in newer versions
        builder.Encrypt.ToString().Should().Be("True");
    }

    [Fact]
    public void BuildConnectionString_ShouldPreserveExistingConnectionStringParameters()
    {
        // Arrange
        var services = new ServiceCollection();
        var customConnectionString = "Server=localhost;Database=TestDB;User Id=sa;Password=Pass123;Application Name=MyApp;";

        // Act
        services.AddSqlServerProvider(customConnectionString, opts =>
        {
            opts.Pooling.MinPoolSize = 5;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert
        var sqlConnection = connection.Should().BeOfType<SqlConnection>().Subject;
        var builder = new SqlConnectionStringBuilder(sqlConnection.ConnectionString);
        
        builder.DataSource.Should().Be("localhost");
        builder.InitialCatalog.Should().Be("TestDB");
        builder.UserID.Should().Be("sa");
        builder.ApplicationName.Should().Be("MyApp");
        builder.MinPoolSize.Should().Be(5);
    }

    [Fact]
    public void BuildConnectionString_WithoutConfiguration_ShouldUseDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqlServerProvider(BaseConnectionString);

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IDbConnection>();

        // Assert - Should use .NET defaults when no options provided
        var sqlConnection = connection.Should().BeOfType<SqlConnection>().Subject;
        var builder = new SqlConnectionStringBuilder(sqlConnection.ConnectionString);
        
        // Connection string should contain basic parameters
        builder.DataSource.Should().Be("localhost");
        builder.InitialCatalog.Should().Be("TestDB");
    }
}
