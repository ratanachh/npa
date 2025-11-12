using FluentAssertions;
using NPA.Core.Configuration;
using Xunit;

namespace NPA.Core.Tests.Configuration;

/// <summary>
/// Tests for ConnectionPoolOptions configuration class.
/// </summary>
public class ConnectionPoolOptionsTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var options = new ConnectionPoolOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.MinPoolSize.Should().Be(5);
        options.MaxPoolSize.Should().Be(100);
        options.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.ConnectionLifetime.Should().BeNull();
        options.IdleTimeout.Should().Be(TimeSpan.FromMinutes(5));
        options.ResetOnReturn.Should().BeTrue();
        options.ValidateOnAcquire.Should().BeTrue();
    }

    [Fact]
    public void Enabled_ShouldBeSettable()
    {
        // Arrange
        var options = new ConnectionPoolOptions();

        // Act
        options.Enabled = false;

        // Assert
        options.Enabled.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    public void MinPoolSize_ShouldBeSettable(int minSize)
    {
        // Arrange
        var options = new ConnectionPoolOptions();

        // Act
        options.MinPoolSize = minSize;

        // Assert
        options.MinPoolSize.Should().Be(minSize);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(500)]
    public void MaxPoolSize_ShouldBeSettable(int maxSize)
    {
        // Arrange
        var options = new ConnectionPoolOptions();

        // Act
        options.MaxPoolSize = maxSize;

        // Assert
        options.MaxPoolSize.Should().Be(maxSize);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    public void ConnectionTimeout_ShouldBeSettable(int seconds)
    {
        // Arrange
        var options = new ConnectionPoolOptions();
        var timeout = TimeSpan.FromSeconds(seconds);

        // Act
        options.ConnectionTimeout = timeout;

        // Assert
        options.ConnectionTimeout.Should().Be(timeout);
    }

    [Theory]
    [InlineData(60)]
    [InlineData(300)]
    [InlineData(600)]
    [InlineData(1800)]
    public void ConnectionLifetime_ShouldBeSettable(int seconds)
    {
        // Arrange
        var options = new ConnectionPoolOptions();
        var lifetime = TimeSpan.FromSeconds(seconds);

        // Act
        options.ConnectionLifetime = lifetime;

        // Assert
        options.ConnectionLifetime.Should().Be(lifetime);
    }

    [Fact]
    public void ConnectionLifetime_ShouldBeNullableAndDefaultToNull()
    {
        // Arrange & Act
        var options = new ConnectionPoolOptions();

        // Assert
        options.ConnectionLifetime.Should().BeNull();
    }

    [Theory]
    [InlineData(60)]
    [InlineData(180)]
    [InlineData(300)]
    [InlineData(600)]
    public void IdleTimeout_ShouldBeSettable(int seconds)
    {
        // Arrange
        var options = new ConnectionPoolOptions();
        var timeout = TimeSpan.FromSeconds(seconds);

        // Act
        options.IdleTimeout = timeout;

        // Assert
        options.IdleTimeout.Should().Be(timeout);
    }

    [Fact]
    public void ResetOnReturn_ShouldBeSettable()
    {
        // Arrange
        var options = new ConnectionPoolOptions();

        // Act
        options.ResetOnReturn = false;

        // Assert
        options.ResetOnReturn.Should().BeFalse();
    }

    [Fact]
    public void ValidateOnAcquire_ShouldBeSettable()
    {
        // Arrange
        var options = new ConnectionPoolOptions();

        // Act
        options.ValidateOnAcquire = false;

        // Assert
        options.ValidateOnAcquire.Should().BeFalse();
    }

    [Fact]
    public void AllProperties_ShouldBeIndependentlySettable()
    {
        // Arrange
        var options = new ConnectionPoolOptions
        {
            Enabled = false,
            MinPoolSize = 2,
            MaxPoolSize = 50,
            ConnectionTimeout = TimeSpan.FromSeconds(15),
            ConnectionLifetime = TimeSpan.FromMinutes(10),
            IdleTimeout = TimeSpan.FromMinutes(2),
            ResetOnReturn = false,
            ValidateOnAcquire = false
        };

        // Assert
        options.Enabled.Should().BeFalse();
        options.MinPoolSize.Should().Be(2);
        options.MaxPoolSize.Should().Be(50);
        options.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(15));
        options.ConnectionLifetime.Should().Be(TimeSpan.FromMinutes(10));
        options.IdleTimeout.Should().Be(TimeSpan.FromMinutes(2));
        options.ResetOnReturn.Should().BeFalse();
        options.ValidateOnAcquire.Should().BeFalse();
    }

    [Fact]
    public void ProductionConfiguration_Example()
    {
        // Arrange & Act - Typical production configuration
        var options = new ConnectionPoolOptions
        {
            MinPoolSize = 10,
            MaxPoolSize = 200,
            ConnectionTimeout = TimeSpan.FromSeconds(60),
            ConnectionLifetime = TimeSpan.FromMinutes(30),
            IdleTimeout = TimeSpan.FromMinutes(10)
        };

        // Assert
        options.Enabled.Should().BeTrue();
        options.MinPoolSize.Should().Be(10);
        options.MaxPoolSize.Should().Be(200);
        options.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(60));
        options.ConnectionLifetime.Should().Be(TimeSpan.FromMinutes(30));
        options.IdleTimeout.Should().Be(TimeSpan.FromMinutes(10));
        options.ResetOnReturn.Should().BeTrue();
        options.ValidateOnAcquire.Should().BeTrue();
    }

    [Fact]
    public void DevelopmentConfiguration_Example()
    {
        // Arrange & Act - Typical development configuration (smaller pool)
        var options = new ConnectionPoolOptions
        {
            MinPoolSize = 1,
            MaxPoolSize = 10,
            ConnectionTimeout = TimeSpan.FromSeconds(15)
        };

        // Assert
        options.Enabled.Should().BeTrue();
        options.MinPoolSize.Should().Be(1);
        options.MaxPoolSize.Should().Be(10);
        options.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void PoolingDisabled_Configuration()
    {
        // Arrange & Act - Pooling completely disabled
        var options = new ConnectionPoolOptions
        {
            Enabled = false
        };

        // Assert
        options.Enabled.Should().BeFalse();
        // Other properties should still have defaults
        options.MinPoolSize.Should().Be(5);
        options.MaxPoolSize.Should().Be(100);
    }
}
