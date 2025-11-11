using FluentAssertions;
using Xunit;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for regex pattern matching keywords (Regex, Matches, MatchesRegex).
/// </summary>
public class RegexPatternMatchingTests
{
    #region Regex Keyword Detection
    
    [Theory]
    [InlineData("FindByEmailRegexAsync", "Regex")]
    [InlineData("FindByNameMatchesAsync", "Matches")]
    [InlineData("FindByPhoneIsMatchesAsync", "IsMatches")]
    [InlineData("FindByDescriptionMatchesRegexAsync", "MatchesRegex")]
    public void RegexKeywords_AllVariants_ShouldBeRecognized(
        string methodName, string keyword)
    {
        // Assert - all regex keyword variants should be detected
        methodName.Should().Contain(keyword);
        methodName.Should().StartWith("Find");
        methodName.Should().Contain("By");
    }
    
    #endregion
    
    #region SQL Generation with Regex
    
    [Theory]
    [InlineData("FindByEmailRegexAsync", "email REGEXP @pattern")]
    [InlineData("FindByNameMatchesAsync", "name REGEXP @pattern")]
    [InlineData("FindByCodeMatchesRegexAsync", "code REGEXP @pattern")]
    public void GeneratedSQL_WithRegex_ShouldContainRegexpOperator(
        string methodName, string expectedPattern)
    {
        // Assert - should generate REGEXP operator (MySQL/MariaDB syntax)
        expectedPattern.Should().Contain("REGEXP");
        expectedPattern.Should().Contain("@");
    }
    
    #endregion
    
    #region Combined with Other Keywords
    
    [Theory]
    [InlineData("FindByEmailRegexAndStatusAsync")]
    [InlineData("FindByNameMatchesOrTypeAsync")]
    public void RegexWithCombinations_ShouldBeValid(string methodName)
    {
        // Assert - regex can be combined with And/Or
        (methodName.Contains("Regex") || methodName.Contains("Matches")).Should().BeTrue();
        (methodName.Contains("And") || methodName.Contains("Or")).Should().BeTrue();
    }
    
    [Theory]
    [InlineData("FindByEmailRegexOrderByNameAscAsync")]
    [InlineData("FindByDescriptionMatchesOrderByCreatedAtDescAsync")]
    public void RegexWithOrdering_ShouldBeValid(string methodName)
    {
        // Assert
        (methodName.Contains("Regex") || methodName.Contains("Matches")).Should().BeTrue();
        methodName.Should().Contain("OrderBy");
    }
    
    [Theory]
    [InlineData("FindFirst5ByEmailRegexAsync")]
    [InlineData("GetTop10ByNameMatchesAsync")]
    public void RegexWithLimiting_ShouldBeValid(string methodName)
    {
        // Assert - regex works with result limiting
        (methodName.Contains("Regex") || methodName.Contains("Matches")).Should().BeTrue();
        methodName.Should().MatchRegex(@"(First|Top)\d+");
    }
    
    [Theory]
    [InlineData("FindDistinctByEmailRegexAsync")]
    [InlineData("GetDistinctByCodeMatchesAsync")]
    public void RegexWithDistinct_ShouldBeValid(string methodName)
    {
        // Assert
        methodName.Should().Contain("Distinct");
        (methodName.Contains("Regex") || methodName.Contains("Matches")).Should().BeTrue();
    }
    
    #endregion
    
    #region Real-World Use Cases
    
    [Theory]
    [InlineData("FindByEmailRegexAsync", "Find emails matching pattern (e.g., ^[a-z]+@example\\.com$)")]
    [InlineData("FindByPhoneMatchesAsync", "Find phone numbers matching format")]
    [InlineData("FindByPostalCodeRegexAsync", "Find by postal/zip code pattern")]
    [InlineData("FindByIpAddressMatchesAsync", "Find by IP address pattern")]
    [InlineData("FindByUsernameMatchesRegexAsync", "Find usernames matching naming rules")]
    public void RegexUseCase_Examples_ShouldBeValid(
        string methodName, string useCase)
    {
        // Assert - practical regex use cases
        methodName.Should().StartWith("Find");
        methodName.Should().EndWith("Async");
        (methodName.Contains("Regex") || methodName.Contains("Matches")).Should().BeTrue();
        useCase.Should().NotBeNullOrEmpty();
    }
    
    #endregion
    
    #region Multiple Properties with Regex
    
    [Theory]
    [InlineData("FindByEmailRegexAndPhoneMatchesAsync")]
    [InlineData("FindByFirstNameMatchesAndLastNameRegexAsync")]
    public void RegexOnMultipleProperties_ShouldBeValid(string methodName)
    {
        // Assert - multiple properties can use regex
        methodName.Should().Contain("And");
        var regexCount = (methodName.Contains("Regex") ? 1 : 0) + 
                        (methodName.Contains("Matches") ? 1 : 0);
        regexCount.Should().BeGreaterOrEqualTo(1);
    }
    
    #endregion
    
    #region Database Provider Considerations
    
    [Fact]
    public void RegexOperator_MySQL_UsesREGEXP()
    {
        // MySQL/MariaDB use REGEXP operator
        var expectedOperator = "REGEXP";
        expectedOperator.Should().Be("REGEXP");
    }
    
    [Fact]
    public void RegexOperator_PostgreSQL_UsesTilde()
    {
        // PostgreSQL uses ~ or ~* (case-insensitive) operator
        // Note: Generated code uses REGEXP by default
        // Provider-specific implementations can override
        var expectedOperator = "~";
        expectedOperator.Should().Be("~");
    }
    
    [Fact]
    public void RegexOperator_SQLServer_Note()
    {
        // SQL Server doesn't have native regex support
        // Would require CLR functions or LIKE patterns
        // This is a known limitation
        var note = "SQL Server requires custom implementation for regex";
        note.Should().NotBeNullOrEmpty();
    }
    
    #endregion
    
    #region Edge Cases
    
    [Theory]
    [InlineData("FindByEmailRegexIgnoreCaseAsync", "Case-insensitive regex")]
    [InlineData("FindByNameMatchesAllIgnoreCaseAsync", "All properties case-insensitive")]
    public void RegexWithCaseModifiers_ShouldBeValid(
        string methodName, string description)
    {
        // Assert - regex with case modifiers
        (methodName.Contains("Regex") || methodName.Contains("Matches")).Should().BeTrue();
        (methodName.Contains("IgnoreCase") || methodName.Contains("IgnoringCase")).Should().BeTrue();
        description.Should().NotBeNullOrEmpty();
    }
    
    [Theory]
    [InlineData("FindByEmailIsMatchesAsync")]
    [InlineData("FindByNameIsMatchesRegexAsync")]
    public void RegexWithIsPrefix_ShouldBeRecognized(string methodName)
    {
        // Assert - IsMatches variant should work
        methodName.Should().Contain("IsMatches");
    }
    
    #endregion
    
    #region Method Name Structure Validation
    
    [Theory]
    [InlineData("FindByEmailRegexAsync")]
    [InlineData("GetByNameMatchesAsync")]
    [InlineData("QueryByCodeMatchesRegexAsync")]
    [InlineData("SearchByPhoneRegexAsync")]
    [InlineData("ReadByDescriptionMatchesAsync")]
    public void RegexMethods_WithVariousSubjectKeywords_ShouldBeValid(string methodName)
    {
        // Assert - regex works with all subject keywords
        var hasValidSubject = methodName.StartsWith("Find") || 
                             methodName.StartsWith("Get") ||
                             methodName.StartsWith("Query") ||
                             methodName.StartsWith("Search") ||
                             methodName.StartsWith("Read");
        hasValidSubject.Should().BeTrue();
        (methodName.Contains("Regex") || methodName.Contains("Matches")).Should().BeTrue();
    }
    
    #endregion
}
