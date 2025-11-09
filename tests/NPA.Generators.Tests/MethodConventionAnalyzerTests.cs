using FluentAssertions;
using Xunit;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for MethodConventionAnalyzer utility methods.
/// </summary>
public class MethodConventionAnalyzerTests
{
    [Theory]
    [InlineData("Email", "email")]
    [InlineData("FirstName", "first_name")]
    [InlineData("UserId", "user_id")]
    [InlineData("CreatedAt", "created_at")]
    [InlineData("IsActive", "is_active")]
    [InlineData("HTTPResponse", "h_t_t_p_response")]
    [InlineData("ID", "i_d")]
    public void ToSnakeCase_ShouldConvertCorrectly(string input, string expected)
    {
        // Act
        var result = MethodConventionAnalyzer.ToSnakeCase(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToSnakeCase_ShouldHandleEmptyString()
    {
        // Act
        var result = MethodConventionAnalyzer.ToSnakeCase("");

        // Assert
        result.Should().Be("");
    }

    [Fact]
    public void ToSnakeCase_ShouldHandleSingleCharacter()
    {
        // Act
        var result = MethodConventionAnalyzer.ToSnakeCase("A");

        // Assert
        result.Should().Be("a");
    }

    [Fact]
    public void ToSnakeCase_ShouldHandleAllLowercase()
    {
        // Act
        var result = MethodConventionAnalyzer.ToSnakeCase("email");

        // Assert
        result.Should().Be("email");
    }

    [Fact]
    public void ToSnakeCase_ShouldHandleConsecutiveUppercase()
    {
        // Act
        var result = MethodConventionAnalyzer.ToSnakeCase("XMLParser");

        // Assert
        result.Should().Be("x_m_l_parser");
    }

    [Fact]
    public void ToSnakeCase_ShouldHandleNumbersAndUnderscores()
    {
        // Act
        var result = MethodConventionAnalyzer.ToSnakeCase("User123Name");

        // Assert
        result.Should().Be("user123_name");
    }

    // Note: Full integration tests for MethodConventionAnalyzer.AnalyzeMethod
    // would require Roslyn IMethodSymbol instances, which are better tested
    // through the actual source generator compilation tests.
    // These unit tests cover the utility methods that don't require Roslyn symbols.
}
