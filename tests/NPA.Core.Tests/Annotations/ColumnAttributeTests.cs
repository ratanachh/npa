using FluentAssertions;
using NPA.Core.Annotations;
using NPA.Core.Tests.TestEntities;
using System.Reflection;
using Xunit;

namespace NPA.Core.Tests.Annotations;

/// <summary>
/// Unit tests for the ColumnAttribute class.
/// </summary>
public class ColumnAttributeTests
{
    [Fact]
    public void ColumnAttribute_WithValidColumnName_ShouldCreateAttribute()
    {
        // Arrange & Act
        var attribute = new ColumnAttribute("test_column");

        // Assert
        attribute.Name.Should().Be("test_column");
        attribute.IsNullable.Should().BeTrue();
        attribute.IsUnique.Should().BeFalse();
        attribute.Length.Should().BeNull();
        attribute.Precision.Should().BeNull();
        attribute.Scale.Should().BeNull();
        attribute.TypeName.Should().BeNull();
    }

    [Fact]
    public void ColumnAttribute_WithAllProperties_ShouldCreateAttribute()
    {
        // Arrange & Act
        var attribute = new ColumnAttribute("test_column")
        {
            TypeName = "VARCHAR",
            Length = 255,
            Precision = 10,
            Scale = 2,
            IsNullable = false,
            IsUnique = true
        };

        // Assert
        attribute.Name.Should().Be("test_column");
        attribute.TypeName.Should().Be("VARCHAR");
        attribute.Length.Should().Be(255);
        attribute.Precision.Should().Be(10);
        attribute.Scale.Should().Be(2);
        attribute.IsNullable.Should().BeFalse();
        attribute.IsUnique.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ColumnAttribute_WithInvalidColumnName_ShouldThrowException(string? columnName)
    {
        // Act & Assert
        var action = () => new ColumnAttribute(columnName!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Column name cannot be null or empty*");
    }

    [Fact]
    public void ColumnAttribute_CanBeAppliedToProperty()
    {
        // Arrange & Act
        var property = typeof(User).GetProperty(nameof(User.Username));
        var attribute = property?.GetCustomAttribute<ColumnAttribute>();

        // Assert
        attribute.Should().NotBeNull();
        attribute!.Name.Should().Be("username");
        attribute.IsNullable.Should().BeTrue(); // Default value
    }
}
