using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NPA.Core.Caching;
using Xunit;

namespace NPA.Core.Tests.Caching;

public class MemoryCacheProviderTests : IDisposable
{
    private readonly MemoryCacheProvider _cacheProvider;
    private readonly IMemoryCache _memoryCache;

    public MemoryCacheProviderTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new CacheOptions
        {
            KeyPrefix = "test:",
            DefaultExpiration = TimeSpan.FromMinutes(5)
        });
        _cacheProvider = new MemoryCacheProvider(_memoryCache, options);
    }

    [Fact]
    public async Task GetAsync_WithNonExistentKey_ShouldReturnDefault()
    {
        // Act
        var result = await _cacheProvider.GetAsync<string>("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_AndGet_ShouldRetrieveValue()
    {
        // Arrange
        const string key = "test-key";
        const string value = "test-value";

        // Act
        await _cacheProvider.SetAsync(key, value);
        var result = await _cacheProvider.GetAsync<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_WithExpiration_ShouldExpire()
    {
        // Arrange
        const string key = "expire-test";
        const string value = "test-value";

        // Act
        await _cacheProvider.SetAsync(key, value, TimeSpan.FromMilliseconds(100));
        await Task.Delay(200);
        var result = await _cacheProvider.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveValue()
    {
        // Arrange
        const string key = "remove-test";
        const string value = "test-value";
        await _cacheProvider.SetAsync(key, value);

        // Act
        await _cacheProvider.RemoveAsync(key);
        var result = await _cacheProvider.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingKey_ShouldReturnTrue()
    {
        // Arrange
        const string key = "exists-test";
        await _cacheProvider.SetAsync(key, "value");

        // Act
        var exists = await _cacheProvider.ExistsAsync(key);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentKey_ShouldReturnFalse()
    {
        // Act
        var exists = await _cacheProvider.ExistsAsync("nonexistent");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveByPatternAsync_ShouldRemoveMatchingKeys()
    {
        // Arrange
        await _cacheProvider.SetAsync("user:1", "User 1");
        await _cacheProvider.SetAsync("user:2", "User 2");
        await _cacheProvider.SetAsync("product:1", "Product 1");

        // Act
        await _cacheProvider.RemoveByPatternAsync("user:*");

        // Assert
        (await _cacheProvider.ExistsAsync("user:1")).Should().BeFalse();
        (await _cacheProvider.ExistsAsync("user:2")).Should().BeFalse();
        (await _cacheProvider.ExistsAsync("product:1")).Should().BeTrue();
    }

    [Fact]
    public async Task GetKeysAsync_ShouldReturnAllKeys()
    {
        // Arrange
        await _cacheProvider.SetAsync("key1", "value1");
        await _cacheProvider.SetAsync("key2", "value2");
        await _cacheProvider.SetAsync("key3", "value3");

        // Act
        var keys = await _cacheProvider.GetKeysAsync();

        // Assert
        keys.Should().Contain(new[] { "key1", "key2", "key3" });
    }

    [Fact]
    public async Task GetKeysAsync_WithPattern_ShouldReturnMatchingKeys()
    {
        // Arrange
        await _cacheProvider.SetAsync("user:1", "User 1");
        await _cacheProvider.SetAsync("user:2", "User 2");
        await _cacheProvider.SetAsync("product:1", "Product 1");

        // Act
        var keys = await _cacheProvider.GetKeysAsync("user:*");

        // Assert
        keys.Should().Contain(new[] { "user:1", "user:2" });
        keys.Should().NotContain("product:1");
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveAllKeys()
    {
        // Arrange
        await _cacheProvider.SetAsync("key1", "value1");
        await _cacheProvider.SetAsync("key2", "value2");

        // Act
        await _cacheProvider.ClearAsync();

        // Assert
        (await _cacheProvider.ExistsAsync("key1")).Should().BeFalse();
        (await _cacheProvider.ExistsAsync("key2")).Should().BeFalse();
    }

    [Fact]
    public async Task SetAsync_WithNullKey_ShouldThrowException()
    {
        // Act
        Func<Task> act = async () => await _cacheProvider.SetAsync<string>(null!, "value");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetAsync_WithNullKey_ShouldThrowException()
    {
        // Act
        Func<Task> act = async () => await _cacheProvider.GetAsync<string>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetAsync_ComplexObject_ShouldStoreAndRetrieve()
    {
        // Arrange
        var user = new TestUser { Id = 1, Name = "John Doe", Email = "john@example.com" };

        // Act
        await _cacheProvider.SetAsync("user:1", user);
        var result = await _cacheProvider.GetAsync<TestUser>("user:1");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Name.Should().Be(user.Name);
        result.Email.Should().Be(user.Email);
    }

    [Fact]
    public void Dispose_AfterDisposed_OperationsShouldThrow()
    {
        // Arrange
        var provider = new MemoryCacheProvider(_memoryCache);
        provider.Dispose();

        // Act
        Func<Task> act = async () => await provider.GetAsync<string>("key");

        // Assert
        act.Should().ThrowAsync<ObjectDisposedException>();
    }

    public void Dispose()
    {
        _cacheProvider?.Dispose();
        _memoryCache?.Dispose();
    }

    private class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
