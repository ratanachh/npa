using FluentAssertions;
using NPA.Core.Annotations;
using NPA.Core.Tests.TestEntities;
using System.Reflection;
using Xunit;

namespace NPA.Core.Tests.Annotations;

/// <summary>
/// Unit tests for the GeneratedValueAttribute class.
/// </summary>
public class GeneratedValueAttributeTests
{
    [Theory]
    [InlineData(GenerationType.Identity)]
    [InlineData(GenerationType.Sequence)]
    [InlineData(GenerationType.Table)]
    [InlineData(GenerationType.None)]
    public void GeneratedValueAttribute_WithValidStrategy_ShouldCreateAttribute(GenerationType strategy)
    {
        // Arrange & Act
        var attribute = new GeneratedValueAttribute(strategy);

        // Assert
        attribute.Strategy.Should().Be(strategy);
        attribute.Generator.Should().BeNull();
    }

    [Fact]
    public void GeneratedValueAttribute_WithGenerator_ShouldCreateAttribute()
    {
        // Arrange & Act
        var attribute = new GeneratedValueAttribute(GenerationType.Sequence)
        {
            Generator = "seq_user_id"
        };

        // Assert
        attribute.Strategy.Should().Be(GenerationType.Sequence);
        attribute.Generator.Should().Be("seq_user_id");
    }

    [Fact]
    public void GeneratedValueAttribute_CanBeAppliedToIdProperty()
    {
        // Arrange & Act
        var property = typeof(User).GetProperty(nameof(User.Id));
        var attribute = property?.GetCustomAttribute<GeneratedValueAttribute>();

        // Assert
        attribute.Should().NotBeNull();
        attribute!.Strategy.Should().Be(GenerationType.Identity);
    }
}
