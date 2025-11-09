using FluentAssertions;
using NPA.Core.Caching;
using Xunit;

namespace NPA.Core.Tests.Caching;

public class NullCacheProviderTests
{
    private readonly NullCacheProvider _cacheProvider;

    public NullCacheProviderTests()
    {
        _cacheProvider = new NullCacheProvider();
    }

    [Fact]
    public async Task GetAsync_ShouldAlwaysReturnDefault()
    {
        // Act
        var result = await _cacheProvider.GetAsync<string>("any-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldNotThrow()
    {
        // Act
        Func<Task> act = async () => await _cacheProvider.SetAsync("key", "value");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveAsync_ShouldNotThrow()
    {
        // Act
        Func<Task> act = async () => await _cacheProvider.RemoveAsync("key");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveByPatternAsync_ShouldNotThrow()
    {
        // Act
        Func<Task> act = async () => await _cacheProvider.RemoveByPatternAsync("*");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ClearAsync_ShouldNotThrow()
    {
        // Act
        Func<Task> act = async () => await _cacheProvider.ClearAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExistsAsync_ShouldAlwaysReturnFalse()
    {
        // Act
        var exists = await _cacheProvider.ExistsAsync("any-key");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetKeysAsync_ShouldReturnEmptyCollection()
    {
        // Act
        var keys = await _cacheProvider.GetKeysAsync();

        // Assert
        keys.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Act
        Action act = () => _cacheProvider.Dispose();

        // Assert
        act.Should().NotThrow();
    }
}
