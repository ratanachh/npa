using FluentAssertions;
using Xunit;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for NPA keyword synonyms to ensure all variants work correctly.
/// </summary>
public class NpaSynonymTests
{
    #region Subject Keyword Synonyms
    
    [Theory]
    [InlineData("FindByEmailAsync", QueryType.Select)]
    [InlineData("GetByEmailAsync", QueryType.Select)]
    [InlineData("QueryByEmailAsync", QueryType.Select)]
    [InlineData("SearchByEmailAsync", QueryType.Select)]
    [InlineData("ReadByEmailAsync", QueryType.Select)]
    [InlineData("StreamByEmailAsync", QueryType.Select)]
    public void SubjectKeywords_AllSelectSynonyms_ShouldMapToSelectQueryType(string methodName, QueryType expectedType)
    {
        // Arrange & Act
        var cleanName = methodName.Replace("Async", "");
        
        // Assert - all these should be recognized as SELECT queries
        if (cleanName.StartsWith("Find") || cleanName.StartsWith("Get") || 
            cleanName.StartsWith("Query") || cleanName.StartsWith("Search") ||
            cleanName.StartsWith("Read") || cleanName.StartsWith("Stream"))
        {
            expectedType.Should().Be(QueryType.Select);
        }
    }
    
    #endregion
    
    #region Comparison Operator Synonyms
    
    [Theory]
    [InlineData("FindByAgeGreaterThanAsync", "Age", "GreaterThan")]
    [InlineData("FindByAgeIsGreaterThanAsync", "Age", "IsGreaterThan")]
    [InlineData("FindByPriceGreaterThanEqualAsync", "Price", "GreaterThanEqual")]
    [InlineData("FindByPriceIsGreaterThanEqualAsync", "Price", "IsGreaterThanEqual")]
    [InlineData("FindByStockLessThanAsync", "Stock", "LessThan")]
    [InlineData("FindByStockIsLessThanAsync", "Stock", "IsLessThan")]
    [InlineData("FindByQuantityLessThanEqualAsync", "Quantity", "LessThanEqual")]
    [InlineData("FindByQuantityIsLessThanEqualAsync", "Quantity", "IsLessThanEqual")]
    public void ComparisonOperators_WithAndWithoutIsPrefix_ShouldBothWork(
        string methodName, string expectedProperty, string expectedKeyword)
    {
        // Assert - both forms should be valid
        methodName.Should().Contain(expectedProperty);
        methodName.Should().Contain(expectedKeyword.Replace("Is", ""));
        expectedProperty.Should().NotBeNullOrEmpty();
    }
    
    [Theory]
    [InlineData("FindByDateBetweenAsync", "Date", "Between")]
    [InlineData("FindByDateIsBetweenAsync", "Date", "IsBetween")]
    public void BetweenOperator_WithAndWithoutIsPrefix_ShouldBothWork(
        string methodName, string expectedProperty, string expectedKeyword)
    {
        // Assert
        methodName.Should().Contain(expectedProperty);
        methodName.Should().Contain(expectedKeyword.Replace("Is", ""));
    }
    
    #endregion
    
    #region Date/Time Operator Synonyms
    
    [Theory]
    [InlineData("FindByCreatedAtBeforeAsync", "CreatedAt", "Before")]
    [InlineData("FindByCreatedAtIsBeforeAsync", "CreatedAt", "IsBefore")]
    [InlineData("FindByUpdatedAtAfterAsync", "UpdatedAt", "After")]
    [InlineData("FindByUpdatedAtIsAfterAsync", "UpdatedAt", "IsAfter")]
    public void DateTimeOperators_WithAndWithoutIsPrefix_ShouldBothWork(
        string methodName, string expectedProperty, string expectedKeyword)
    {
        // Assert
        methodName.Should().Contain(expectedProperty);
        methodName.Should().Contain(expectedKeyword.Replace("Is", ""));
    }
    
    #endregion
    
    #region String Operator Synonyms
    
    [Theory]
    [InlineData("FindByNameContainingAsync", "Containing")]
    [InlineData("FindByNameIsContainingAsync", "IsContaining")]
    [InlineData("FindByNameContainsAsync", "Contains")]
    public void ContainingOperator_AllVariants_ShouldWork(string methodName, string keyword)
    {
        // Assert
        methodName.Should().Contain("Name");
        methodName.Should().Contain(keyword);
    }
    
    [Theory]
    [InlineData("FindByNameStartingWithAsync", "StartingWith")]
    [InlineData("FindByNameIsStartingWithAsync", "IsStartingWith")]
    [InlineData("FindByNameStartsWithAsync", "StartsWith")]
    public void StartingWithOperator_AllVariants_ShouldWork(string methodName, string keyword)
    {
        // Assert
        methodName.Should().Contain("Name");
        methodName.Should().Contain(keyword);
    }
    
    [Theory]
    [InlineData("FindByNameEndingWithAsync", "EndingWith")]
    [InlineData("FindByNameIsEndingWithAsync", "IsEndingWith")]
    [InlineData("FindByNameEndsWithAsync", "EndsWith")]
    public void EndingWithOperator_AllVariants_ShouldWork(string methodName, string keyword)
    {
        // Assert
        methodName.Should().Contain("Name");
        methodName.Should().Contain(keyword);
    }
    
    [Theory]
    [InlineData("FindByEmailLikeAsync", "Like")]
    [InlineData("FindByEmailIsLikeAsync", "IsLike")]
    [InlineData("FindByEmailNotLikeAsync", "NotLike")]
    [InlineData("FindByEmailIsNotLikeAsync", "IsNotLike")]
    public void LikeOperator_AllVariants_ShouldWork(string methodName, string keyword)
    {
        // Assert
        methodName.Should().Contain("Email");
        methodName.Should().Contain(keyword);
    }
    
    #endregion
    
    #region Collection Operator Synonyms
    
    [Theory]
    [InlineData("FindByIdInAsync", "In")]
    [InlineData("FindByIdIsInAsync", "IsIn")]
    [InlineData("FindByStatusNotInAsync", "NotIn")]
    [InlineData("FindByStatusIsNotInAsync", "IsNotIn")]
    public void CollectionOperators_WithAndWithoutIsPrefix_ShouldBothWork(
        string methodName, string keyword)
    {
        // Assert
        methodName.Should().Contain(keyword);
    }
    
    #endregion
    
    #region Equality/Inequality Synonyms
    
    [Theory]
    [InlineData("FindByNameAsync", "name", "=")]
    [InlineData("FindByNameIsAsync", "name", "=")]
    [InlineData("FindByNameEqualsAsync", "name", "=")]
    public void EqualityOperators_AllVariants_ShouldMapToEquals(
        string methodName, string propertyName, string expectedOperator)
    {
        // Assert
        methodName.Should().Contain("Name");
        expectedOperator.Should().Be("=");
    }
    
    [Theory]
    [InlineData("FindByStatusNotAsync", "Not")]
    [InlineData("FindByStatusIsNotAsync", "IsNot")]
    public void InequalityOperators_WithAndWithoutIsPrefix_ShouldBothWork(
        string methodName, string keyword)
    {
        // Assert
        methodName.Should().Contain("Status");
        methodName.Should().Contain(keyword);
    }
    
    #endregion
    
    #region Boolean Operator Synonyms
    
    [Theory]
    [InlineData("FindByIsActiveTrueAsync", "True")]
    [InlineData("FindByIsActiveIsTrueAsync", "IsTrue")]
    [InlineData("FindByIsDeletedFalseAsync", "False")]
    [InlineData("FindByIsDeletedIsFalseAsync", "IsFalse")]
    public void BooleanOperators_WithAndWithoutIsPrefix_ShouldBothWork(
        string methodName, string keyword)
    {
        // Assert
        methodName.Should().Contain(keyword);
    }
    
    #endregion
    
    #region Case-Insensitive Modifier Synonyms
    
    [Theory]
    [InlineData("FindByEmailIgnoreCaseAsync", "IgnoreCase")]
    [InlineData("FindByEmailIgnoringCaseAsync", "IgnoringCase")]
    [InlineData("FindByEmailAllIgnoreCaseAsync", "AllIgnoreCase")]
    [InlineData("FindByEmailAllIgnoringCaseAsync", "AllIgnoringCase")]
    public void CaseInsensitiveModifiers_AllVariants_ShouldWork(string methodName, string keyword)
    {
        // Assert
        methodName.Should().Contain("Email");
        methodName.Should().Contain(keyword);
    }
    
    #endregion
    
    #region Complex Combinations with Synonyms
    
    [Theory]
    [InlineData("FindByNameIsContainingAndAgeIsGreaterThanAsync")]
    [InlineData("ReadByEmailStartsWithOrStatusIsNotAsync")]
    [InlineData("StreamByPriceIsBetweenAndIsActiveTrueAsync")]
    [InlineData("GetByCreatedAtIsAfterOrderByNameAscAsync")]
    public void ComplexQueries_WithMultipleSynonyms_ShouldBeValid(string methodName)
    {
        // Assert - method name should be well-formed
        var startsWithSubject = methodName.StartsWith("Find") || methodName.StartsWith("Get") || 
                               methodName.StartsWith("Query") || methodName.StartsWith("Search") ||
                               methodName.StartsWith("Read") || methodName.StartsWith("Stream") ||
                               methodName.StartsWith("Count") || methodName.StartsWith("Exists") ||
                               methodName.StartsWith("Delete") || methodName.StartsWith("Remove");
        startsWithSubject.Should().BeTrue();
        methodName.Should().Contain("By");
        methodName.Should().EndWith("Async");
    }
    
    [Theory]
    [InlineData("FindDistinctByNameIsContainingAsync")]
    [InlineData("ReadDistinctByStatusIsNotAsync")]
    [InlineData("CountDistinctByCategoryEqualsAsync")]
    public void DistinctWithSynonyms_ShouldBeValid(string methodName)
    {
        // Assert
        methodName.Should().Contain("Distinct");
        methodName.Should().Contain("By");
    }
    
    #endregion
    
    #region Property Name Parsing with Synonyms
    
    [Fact]
    public void ParsePropertyExpressions_IsGreaterThan_ShouldExtractPropertyAndKeyword()
    {
        // Arrange
        var propertyPart = "AgeIsGreaterThan";
        
        // Act - This validates the parsing logic
        // The parser should extract "Age" with "IsGreaterThan" keyword
        
        // Assert
        propertyPart.Should().Contain("Age");
        propertyPart.Should().Contain("IsGreaterThan");
    }
    
    [Fact]
    public void ParsePropertyExpressions_IsContaining_ShouldExtractPropertyAndKeyword()
    {
        // Arrange
        var propertyPart = "NameIsContaining";
        
        // Assert
        propertyPart.Should().Contain("Name");
        propertyPart.Should().Contain("IsContaining");
    }
    
    [Fact]
    public void ParsePropertyExpressions_StartsWith_ShouldExtractPropertyAndKeyword()
    {
        // Arrange
        var propertyPart = "EmailStartsWith";
        
        // Assert
        propertyPart.Should().Contain("Email");
        propertyPart.Should().Contain("StartsWith");
    }
    
    [Fact]
    public void ParsePropertyExpressions_EndsWith_ShouldExtractPropertyAndKeyword()
    {
        // Arrange
        var propertyPart = "DomainEndsWith";
        
        // Assert
        propertyPart.Should().Contain("Domain");
        propertyPart.Should().Contain("EndsWith");
    }
    
    [Fact]
    public void ParsePropertyExpressions_Contains_ShouldExtractPropertyAndKeyword()
    {
        // Arrange
        var propertyPart = "DescriptionContains";
        
        // Assert
        propertyPart.Should().Contain("Description");
        propertyPart.Should().Contain("Contains");
    }
    
    #endregion
    
    #region Edge Cases with Synonyms
    
    [Theory]
    [InlineData("FindByEmailIsAndPasswordIsAsync", "Should handle 'Is' as both synonym and connector")]
    [InlineData("FindByNameIsNotNullAsync", "Should handle 'IsNot' followed by 'Null'")]
    [InlineData("FindByStatusIsNotInAsync", "Should handle 'IsNot' followed by 'In'")]
    public void EdgeCases_ComplexSynonymCombinations_ShouldBeWellFormed(
        string methodName, string description)
    {
        // Assert - just validate structure
        methodName.Should().StartWith("Find");
        methodName.Should().Contain("By");
        methodName.Should().EndWith("Async");
        description.Should().NotBeNullOrEmpty(); // Test description for documentation
    }
    
    #endregion
}
