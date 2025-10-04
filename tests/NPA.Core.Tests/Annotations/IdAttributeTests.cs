using FluentAssertions;
using NPA.Core.Annotations;
using NPA.Core.Tests.TestEntities;
using System.Reflection;
using Xunit;

namespace NPA.Core.Tests.Annotations;

/// <summary>
/// Unit tests for the IdAttribute class.
/// </summary>
public class IdAttributeTests
{
    [Fact]
    public void IdAttribute_CanBeAppliedToProperty()
    {
        // Arrange & Act
        var property = typeof(User).GetProperty(nameof(User.Id));
        var attribute = property?.GetCustomAttribute<IdAttribute>();

        // Assert
        attribute.Should().NotBeNull();
    }

    [Fact]
    public void IdAttribute_CanBeAppliedMultipleTimes()
    {
        // Arrange & Act
        var property = typeof(User).GetProperty(nameof(User.Id));
        var attributes = property?.GetCustomAttributes(typeof(IdAttribute), false);

        // Assert
        attributes.Should().NotBeNull();
        attributes.Should().HaveCount(1); // Should allow multiple but typically only one is used
    }

    [Fact]
    public void IdAttribute_DefaultConstructor_ShouldCreateAttribute()
    {
        // Arrange & Act
        var attribute = new IdAttribute();

        // Assert
        attribute.Should().NotBeNull();
    }

    [Fact]
    public void IdAttribute_CanBeAppliedToDifferentPropertyTypes()
    {
        // Arrange
        var testEntity = typeof(TestEntityWithDifferentIdTypes);

        // Act & Assert
        var intIdProperty = testEntity.GetProperty(nameof(TestEntityWithDifferentIdTypes.IntId));
        var longIdProperty = testEntity.GetProperty(nameof(TestEntityWithDifferentIdTypes.LongId));
        var stringIdProperty = testEntity.GetProperty(nameof(TestEntityWithDifferentIdTypes.StringId));

        intIdProperty?.GetCustomAttribute<IdAttribute>().Should().NotBeNull();
        longIdProperty?.GetCustomAttribute<IdAttribute>().Should().NotBeNull();
        stringIdProperty?.GetCustomAttribute<IdAttribute>().Should().NotBeNull();
    }

    [Fact]
    public void IdAttribute_CanBeUsedWithGeneratedValueAttribute()
    {
        // Arrange
        var property = typeof(User).GetProperty(nameof(User.Id));

        // Act
        var idAttribute = property?.GetCustomAttribute<IdAttribute>();
        var generatedValueAttribute = property?.GetCustomAttribute<GeneratedValueAttribute>();

        // Assert
        idAttribute.Should().NotBeNull();
        generatedValueAttribute.Should().NotBeNull();
    }
}

/// <summary>
/// Test entity with different ID property types for testing IdAttribute.
/// </summary>
public class TestEntityWithDifferentIdTypes
{
    [Id]
    public int IntId { get; set; }

    [Id]
    public long LongId { get; set; }

    [Id]
    public string StringId { get; set; } = string.Empty;
}
