using FluentAssertions;
using NPA.Core.Caching;
using Xunit;

namespace NPA.Core.Tests.Caching;

public class CacheKeyGeneratorTests
{
    [Fact]
    public void GenerateEntityKey_ShouldGenerateCorrectKey()
    {
        // Arrange
        var generator = new CacheKeyGenerator("npa:");

        // Act
        var key = generator.GenerateEntityKey<User, int>(123);

        // Assert
        key.Should().Be("npa:entity:user:123");
    }

    [Fact]
    public void GenerateQueryKey_WithoutParameters_ShouldGenerateCorrectKey()
    {
        // Arrange
        var generator = new CacheKeyGenerator("npa:");

        // Act
        var key = generator.GenerateQueryKey<User>("GetActiveUsers");

        // Assert
        key.Should().Be("npa:query:user:GetActiveUsers");
    }

    [Fact]
    public void GenerateQueryKey_WithParameters_ShouldIncludeParameters()
    {
        // Arrange
        var generator = new CacheKeyGenerator("npa:");

        // Act
        var key = generator.GenerateQueryKey<User>("GetUsersByRole", "admin", true);

        // Assert
        key.Should().Be("npa:query:user:GetUsersByRole:admin:True");
    }

    [Fact]
    public void GenerateEntityPattern_ShouldGenerateWildcardPattern()
    {
        // Arrange
        var generator = new CacheKeyGenerator("npa:");

        // Act
        var pattern = generator.GenerateEntityPattern<User>();

        // Assert
        pattern.Should().Be("npa:entity:user:*");
    }

    [Fact]
    public void GenerateRegionPattern_ShouldGenerateCorrectPattern()
    {
        // Arrange
        var generator = new CacheKeyGenerator("npa:");

        // Act
        var pattern = generator.GenerateRegionPattern("users");

        // Assert
        pattern.Should().Be("npa:region:users:*");
    }

    [Fact]
    public void GenerateRegionPattern_WithNullRegion_ShouldThrowException()
    {
        // Arrange
        var generator = new CacheKeyGenerator("npa:");

        // Act
        Action act = () => generator.GenerateRegionPattern(null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Region cannot be null or empty*");
    }

    [Fact]
    public void GenerateKey_WithCustomParts_ShouldCombineParts()
    {
        // Arrange
        var generator = new CacheKeyGenerator("npa:");

        // Act
        var key = generator.GenerateKey("custom", "part1", "part2");

        // Assert
        key.Should().Be("npa:custom:part1:part2");
    }

    [Fact]
    public void GenerateKey_WithNullParts_ShouldThrowException()
    {
        // Arrange
        var generator = new CacheKeyGenerator("npa:");

        // Act
        Action act = () => generator.GenerateKey(null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Key parts cannot be null or empty*");
    }

    [Fact]
    public void Constructor_WithNullPrefix_ShouldThrowException()
    {
        // Act
        Action act = () => new CacheKeyGenerator(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // Test entity class
    private class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
