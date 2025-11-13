using FluentAssertions;
using NPA.Core.Annotations;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace NPA.Core.Tests.Annotations;

/// <summary>
/// Unit tests for the NamedQueryAttribute class.
/// </summary>
public class NamedQueryAttributeTests
{
    [Fact]
    public void NamedQueryAttribute_WithValidArguments_ShouldSetProperties()
    {
        // Arrange
        var name = "User.findByEmail";
        var query = "SELECT u FROM User u WHERE u.Email = :email";

        // Act
        var attribute = new NamedQueryAttribute(name, query);

        // Assert
        attribute.Name.Should().Be(name);
        attribute.Query.Should().Be(query);
        attribute.NativeQuery.Should().BeFalse(); // default value
        attribute.Buffered.Should().BeTrue(); // default value
        attribute.CommandTimeout.Should().BeNull(); // default value
        attribute.Description.Should().BeNull(); // default value
    }

    [Fact]
    public void NamedQueryAttribute_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        string? nullName = null;
        var query = "SELECT u FROM User u";

        // Act
        Action act = () => new NamedQueryAttribute(nullName!, query);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*name*");
    }

    [Fact]
    public void NamedQueryAttribute_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyName = "";
        var query = "SELECT u FROM User u";

        // Act
        Action act = () => new NamedQueryAttribute(emptyName, query);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*name*");
    }

    [Fact]
    public void NamedQueryAttribute_WithNullQuery_ShouldThrowArgumentException()
    {
        // Arrange
        var name = "User.findAll";
        string? nullQuery = null;

        // Act
        Action act = () => new NamedQueryAttribute(name, nullQuery!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*query*");
    }

    [Fact]
    public void NamedQueryAttribute_WithEmptyQuery_ShouldThrowArgumentException()
    {
        // Arrange
        var name = "User.findAll";
        var emptyQuery = "";

        // Act
        Action act = () => new NamedQueryAttribute(name, emptyQuery);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*query*");
    }

    [Fact]
    public void NamedQueryAttribute_CanBeAppliedToClass()
    {
        // Arrange & Act
        var attributes = typeof(TestEntityWithNamedQuery).GetCustomAttributes<NamedQueryAttribute>().ToList();

        // Assert
        attributes.Should().NotBeEmpty();
        attributes.Should().HaveCount(2);
        attributes[0].Name.Should().Be("TestEntity.findByEmail");
        attributes[0].Query.Should().Be("SELECT t FROM TestEntity t WHERE t.Email = :email");
        attributes[1].Name.Should().Be("TestEntity.findActive");
        attributes[1].Query.Should().Be("SELECT t FROM TestEntity t WHERE t.IsActive = true");
    }

    [Fact]
    public void NamedQueryAttribute_ShouldSupportOptionalProperties()
    {
        // Arrange
        var name = "User.findExpensive";
        var query = "SELECT u FROM User u WHERE u.Price > 1000";

        // Act
        var attribute = new NamedQueryAttribute(name, query)
        {
            NativeQuery = true,
            CommandTimeout = 60,
            Buffered = false,
            Description = "Finds expensive items"
        };

        // Assert
        attribute.NativeQuery.Should().BeTrue();
        attribute.CommandTimeout.Should().Be(60);
        attribute.Buffered.Should().BeFalse();
        attribute.Description.Should().Be("Finds expensive items");
    }

    [Fact]
    public void NamedQueryAttribute_AllowsMultipleApplications()
    {
        // Arrange & Act
        var usageAttribute = typeof(NamedQueryAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        usageAttribute.Should().NotBeNull();
        usageAttribute!.AllowMultiple.Should().BeTrue();
        usageAttribute.ValidOn.Should().Be(AttributeTargets.Class);
    }
}

/// <summary>
/// Test entity with named queries for testing NamedQueryAttribute.
/// </summary>
[Entity]
[Table("test_entities")]
[NamedQuery("TestEntity.findByEmail", "SELECT t FROM TestEntity t WHERE t.Email = :email")]
[NamedQuery("TestEntity.findActive", "SELECT t FROM TestEntity t WHERE t.IsActive = true")]
public class TestEntityWithNamedQuery
{
    [Id]
    public long Id { get; set; }

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; }
}
