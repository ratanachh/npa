using FluentAssertions;
using NPA.Extensions;

namespace NPA.Extensions.Tests;

public class EntityExtensionsTests
{
    [Fact]
    public void IsValid_WithNullEntity_ShouldReturnFalse()
    {
        // Arrange
        TestEntity? entity = null;

        // Act
        var result = entity.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithValidEntity_ShouldReturnTrue()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };

        // Act
        var result = entity.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ToDictionary_WithValidEntity_ShouldReturnDictionary()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };

        // Act
        var result = entity.ToDictionary();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Dictionary<string, object?>>();
    }

    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}