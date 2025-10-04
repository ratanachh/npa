using FluentAssertions;
using NPA.Core.Annotations;
using NPA.Core.Tests.TestEntities;
using System.Reflection;
using Xunit;

namespace NPA.Core.Tests.Annotations;

/// <summary>
/// Unit tests for the TableAttribute class.
/// </summary>
public class TableAttributeTests
{
    [Fact]
    public void TableAttribute_WithValidTableName_ShouldCreateAttribute()
    {
        // Arrange & Act
        var attribute = new TableAttribute("test_table");

        // Assert
        attribute.Name.Should().Be("test_table");
        attribute.Schema.Should().BeNull();
    }

    [Fact]
    public void TableAttribute_WithTableNameAndSchema_ShouldCreateAttribute()
    {
        // Arrange & Act
        var attribute = new TableAttribute("test_table") { Schema = "dbo" };

        // Assert
        attribute.Name.Should().Be("test_table");
        attribute.Schema.Should().Be("dbo");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TableAttribute_WithInvalidTableName_ShouldThrowException(string? tableName)
    {
        // Act & Assert
        var action = () => new TableAttribute(tableName!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Table name cannot be null or empty*");
    }

    [Fact]
    public void TableAttribute_CanBeAppliedToClass()
    {
        // Arrange & Act
        var attribute = typeof(User).GetCustomAttribute<TableAttribute>();

        // Assert
        attribute.Should().NotBeNull();
        attribute!.Name.Should().Be("users");
    }
}
