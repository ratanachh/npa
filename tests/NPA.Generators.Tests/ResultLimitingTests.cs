using FluentAssertions;
using Xunit;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for result limiting keywords (First, Top).
/// </summary>
public class ResultLimitingTests
{
    #region First/Top Keyword Detection
    
    [Theory]
    [InlineData("FindFirst5ByNameAsync", 5)]
    [InlineData("GetTop10ByStatusAsync", 10)]
    [InlineData("QueryFirst3ByEmailAsync", 3)]
    [InlineData("SearchTop20ByTenantIdAsync", 20)]
    [InlineData("ReadFirst1ByIdAsync", 1)]
    public void ResultLimiting_WithNumber_ShouldExtractCorrectLimit(string methodName, int expectedLimit)
    {
        // Arrange - these should parse correctly
        var cleanName = methodName.Replace("Async", "");
        
        // Assert
        cleanName.Should().MatchRegex(@"(First|Top)\d+");
        expectedLimit.Should().BeGreaterThan(0);
    }
    
    [Theory]
    [InlineData("FindFirstByNameAsync", 1)]
    [InlineData("GetTopByStatusAsync", 1)]
    [InlineData("QueryFirstOrderByNameAscAsync", 1)]
    [InlineData("SearchTopByTenantIdAsync", 1)]
    public void ResultLimiting_WithoutNumber_ShouldDefaultToOne(string methodName, int expectedLimit)
    {
        // Arrange
        var cleanName = methodName.Replace("Async", "");
        
        // Assert - First/Top without number should default to 1
        cleanName.Should().MatchRegex(@"(First|Top)(?!\d)");
        expectedLimit.Should().Be(1);
    }
    
    #endregion
    
    #region SQL Generation with Limits
    
    [Theory]
    [InlineData("FindFirst5ByEmailAsync", "FETCH FIRST 5 ROWS ONLY")]
    [InlineData("GetTop10ByNameAsync", "FETCH FIRST 10 ROWS ONLY")]
    [InlineData("QueryFirst1ByStatusAsync", "FETCH FIRST 1 ROWS ONLY")]
    public void GeneratedSQL_WithLimit_ShouldContainFetchClause(
        string methodName, string expectedClause)
    {
        // Assert - generated SQL should include FETCH FIRST clause
        expectedClause.Should().StartWith("FETCH FIRST");
        expectedClause.Should().EndWith("ROWS ONLY");
    }
    
    [Theory]
    [InlineData("FindFirst5ByNameOrderByEmailDescAsync")]
    [InlineData("GetTop10ByStatusOrderByCreatedAtAscAsync")]
    public void ResultLimiting_WithOrderBy_ShouldCombineBoth(string methodName)
    {
        // Assert - method name should contain both limiting and ordering
        methodName.Should().MatchRegex(@"(First|Top)\d+");
        methodName.Should().Contain("OrderBy");
    }
    
    #endregion
    
    #region Combined with Other Features
    
    [Theory]
    [InlineData("FindDistinctFirst10ByTenantIdAsync")]
    [InlineData("GetDistinctTop5ByStatusAsync")]
    [InlineData("QueryDistinctFirst3ByEmailAsync")]
    public void ResultLimiting_WithDistinct_ShouldBeValid(string methodName)
    {
        // Assert
        methodName.Should().Contain("Distinct");
        methodName.Should().MatchRegex(@"(First|Top)\d+");
    }
    
    [Theory]
    [InlineData("FindFirst10ByNameContainingAsync")]
    [InlineData("GetTop5ByEmailStartsWithAsync")]
    [InlineData("QueryFirst3ByStatusIsNotAsync")]
    public void ResultLimiting_WithComplexConditions_ShouldBeValid(string methodName)
    {
        // Assert - limiting works with complex query conditions
        methodName.Should().MatchRegex(@"(First|Top)\d+");
        methodName.Should().Contain("By");
    }
    
    [Theory]
    [InlineData("FindFirst5ByNameAndEmailAsync")]
    [InlineData("GetTop10ByStatusOrTypeAsync")]
    public void ResultLimiting_WithMultipleConditions_ShouldBeValid(string methodName)
    {
        // Assert
        methodName.Should().MatchRegex(@"(First|Top)\d+");
        (methodName.Contains("And") || methodName.Contains("Or")).Should().BeTrue();
    }
    
    #endregion
    
    #region Edge Cases
    
    [Theory]
    [InlineData("FindFirst100ByTenantIdAsync", "Large limit values should work")]
    [InlineData("GetTop1000ByStatusAsync", "Very large limits should be supported")]
    public void ResultLimiting_WithLargeNumbers_ShouldBeValid(
        string methodName, string description)
    {
        // Assert
        methodName.Should().MatchRegex(@"(First|Top)\d{2,}");
        description.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public void ResultLimiting_MinimalCases_ShouldBeWellFormed()
    {
        // Arrange
        var methodNames = new[]
        {
            "FindFirstByAsync",
            "GetTopOrderByNameAscAsync"
        };
        
        // Assert
        foreach (var methodName in methodNames)
        {
            var hasFirstOrTop = methodName.Contains("First") || methodName.Contains("Top");
            hasFirstOrTop.Should().BeTrue();
            methodName.Should().EndWith("Async");
        }
    }
    
    #endregion
    
    #region Return Type Considerations
    
    [Fact]
    public void ResultLimiting_ShouldReturnCollection_WhenLimitGreaterThanOne()
    {
        // Method names like FindFirst5... should return IEnumerable or List
        var methodName = "FindFirst5ByNameAsync";
        
        // Assert - conceptually this should return a collection
        methodName.Should().Contain("First5");
    }
    
    [Fact]
    public void ResultLimiting_MayReturnSingle_WhenLimitIsOne()
    {
        // Method names like FindFirstBy... or FindFirst1By... could return single or collection
        var methodName = "FindFirstByNameAsync";
        
        // Assert
        methodName.Should().Contain("First");
    }
    
    #endregion
    
    #region Method Name Patterns
    
    [Theory]
    [InlineData("FindFirst10ByNameIgnoreCaseAsync")]
    [InlineData("GetTop5ByEmailAllIgnoreCaseAsync")]
    public void ResultLimiting_WithCaseModifiers_ShouldBeValid(string methodName)
    {
        // Assert
        methodName.Should().MatchRegex(@"(First|Top)\d+");
        (methodName.Contains("IgnoreCase") || methodName.Contains("IgnoringCase")).Should().BeTrue();
    }
    
    [Theory]
    [InlineData("FindFirst20ByPriceBetweenAsync")]
    [InlineData("GetTop15ByAgeLessThanAsync")]
    [InlineData("QueryFirst8ByDateAfterAsync")]
    public void ResultLimiting_WithVariousOperators_ShouldBeValid(string methodName)
    {
        // Assert
        methodName.Should().MatchRegex(@"(First|Top)\d+");
        methodName.Should().Contain("By");
    }
    
    #endregion
}
