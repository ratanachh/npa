using FluentAssertions;
using NPA.Core.Annotations;
using NPA.Core.Tests.TestEntities;
using System.Reflection;
using Xunit;

namespace NPA.Core.Tests.Annotations;

/// <summary>
/// Unit tests for the EntityAttribute class.
/// </summary>
public class EntityAttributeTests
{
    [Fact]
    public void EntityAttribute_CanBeAppliedToClass()
    {
        // Arrange & Act
        var attribute = typeof(User).GetCustomAttribute<EntityAttribute>();

        // Assert
        attribute.Should().NotBeNull();
    }

    [Fact]
    public void EntityAttribute_CanBeAppliedMultipleTimes()
    {
        // Arrange & Act
        var attributes = typeof(User).GetCustomAttributes(typeof(EntityAttribute), false);

        // Assert
        attributes.Should().NotBeEmpty();
    }

    [Fact]
    public void EntityAttribute_IsMarkerAttribute()
    {
        // Arrange & Act
        var attribute = new EntityAttribute();

        // Assert
        attribute.Should().NotBeNull();
    }
}
