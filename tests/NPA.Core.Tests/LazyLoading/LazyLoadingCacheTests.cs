using NPA.Core.LazyLoading;
using Xunit;

namespace NPA.Core.Tests.LazyLoading;

public class LazyLoadingCacheTests
{
    private readonly LazyLoadingCache _cache;

    public LazyLoadingCacheTests()
    {
        _cache = new LazyLoadingCache();
    }

    [Fact]
    public void Add_ShouldCacheValue()
    {
        // Arrange
        var entity = new TestEntity { Id = 1 };
        var value = "test value";

        // Act
        _cache.Add(entity, "Property1", value);

        // Assert
        Assert.True(_cache.Contains(entity, "Property1"));
    }

    [Fact]
    public void Add_WithNullEntity_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _cache.Add<string>(null!, "Property1", "value"));
    }

    [Fact]
    public void Add_WithEmptyPropertyName_ShouldThrowArgumentException()
    {
        // Arrange
        var entity = new TestEntity { Id = 1 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _cache.Add(entity, "", "value"));
    }

    [Fact]
    public void Get_ShouldReturnCachedValue()
    {
        // Arrange
        var entity = new TestEntity { Id = 1 };
        var value = "test value";
        _cache.Add(entity, "Property1", value);

        // Act
        var result = _cache.Get<string>(entity, "Property1");

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void Get_WithNonExistentValue_ShouldReturnDefault()
    {
        // Arrange
        var entity = new TestEntity { Id = 1 };

        // Act
        var result = _cache.Get<string>(entity, "Property1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryGet_WithExistingValue_ShouldReturnTrueAndValue()
    {
        // Arrange
        var entity = new TestEntity { Id = 1 };
        var value = "test value";
        _cache.Add(entity, "Property1", value);

        // Act
        var result = _cache.TryGet<string>(entity, "Property1", out var cachedValue);

        // Assert
        Assert.True(result);
        Assert.Equal(value, cachedValue);
    }

    [Fact]
    public void TryGet_WithNonExistentValue_ShouldReturnFalse()
    {
        // Arrange
        var entity = new TestEntity { Id = 1 };

        // Act
        var result = _cache.TryGet<string>(entity, "Property1", out var cachedValue);

        // Assert
        Assert.False(result);
        Assert.Null(cachedValue);
    }

    [Fact]
    public void Remove_WithPropertyName_ShouldRemoveValue()
    {
        // Arrange
        var entity = new TestEntity { Id = 1 };
        _cache.Add(entity, "Property1", "value1");
        _cache.Add(entity, "Property2", "value2");

        // Act
        _cache.Remove(entity, "Property1");

        // Assert
        Assert.False(_cache.Contains(entity, "Property1"));
        Assert.True(_cache.Contains(entity, "Property2"));
    }

    [Fact]
    public void Remove_WithEntity_ShouldRemoveAllValuesForEntity()
    {
        // Arrange
        var entity = new TestEntity { Id = 1 };
        _cache.Add(entity, "Property1", "value1");
        _cache.Add(entity, "Property2", "value2");

        // Act
        _cache.Remove(entity);

        // Assert
        Assert.False(_cache.Contains(entity, "Property1"));
        Assert.False(_cache.Contains(entity, "Property2"));
    }

    [Fact]
    public void Clear_ShouldRemoveAllValues()
    {
        // Arrange
        var entity1 = new TestEntity { Id = 1 };
        var entity2 = new TestEntity { Id = 2 };
        _cache.Add(entity1, "Property1", "value1");
        _cache.Add(entity2, "Property2", "value2");

        // Act
        _cache.Clear();

        // Assert
        Assert.False(_cache.Contains(entity1, "Property1"));
        Assert.False(_cache.Contains(entity2, "Property2"));
    }

    [Fact]
    public void Contains_WithExistingValue_ShouldReturnTrue()
    {
        // Arrange
        var entity = new TestEntity { Id = 1 };
        _cache.Add(entity, "Property1", "value");

        // Act
        var result = _cache.Contains(entity, "Property1");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Contains_WithNonExistentValue_ShouldReturnFalse()
    {
        // Arrange
        var entity = new TestEntity { Id = 1 };

        // Act
        var result = _cache.Contains(entity, "Property1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Add_WithComplexType_ShouldCacheValue()
    {
        // Arrange
        var entity = new TestEntity { Id = 1 };
        var value = new List<string> { "item1", "item2" };

        // Act
        _cache.Add(entity, "Property1", value);
        var result = _cache.Get<List<string>>(entity, "Property1");

        // Assert
        Assert.Equal(value, result);
    }

    private class TestEntity
    {
        public int Id { get; set; }
    }
}
