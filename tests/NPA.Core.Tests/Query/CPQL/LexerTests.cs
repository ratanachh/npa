using NPA.Core.Query.CPQL;
using Xunit;

namespace NPA.Core.Tests.Query.CPQL;

/// <summary>
/// Tests for the CPQL Lexer.
/// </summary>
public class LexerTests
{
    [Fact]
    public void Lexer_ShouldTokenizeKeywords()
    {
        // Arrange
        var lexer = new Lexer("SELECT FROM WHERE ORDER BY GROUP BY HAVING JOIN");
        
        // Act & Assert
        Assert.Equal(TokenType.Select, lexer.NextToken().Type);
        Assert.Equal(TokenType.From, lexer.NextToken().Type);
        Assert.Equal(TokenType.Where, lexer.NextToken().Type);
        Assert.Equal(TokenType.OrderBy, lexer.NextToken().Type);
        Assert.Equal(TokenType.OrderBy, lexer.NextToken().Type); // BY
        Assert.Equal(TokenType.GroupBy, lexer.NextToken().Type);
        Assert.Equal(TokenType.OrderBy, lexer.NextToken().Type); // BY
        Assert.Equal(TokenType.Having, lexer.NextToken().Type);
        Assert.Equal(TokenType.Join, lexer.NextToken().Type);
        Assert.Equal(TokenType.Eof, lexer.NextToken().Type);
    }
    
    [Fact]
    public void Lexer_ShouldTokenizeIdentifiers()
    {
        // Arrange
        var lexer = new Lexer("user username User123 _private");
        
        // Act
        var token1 = lexer.NextToken();
        var token2 = lexer.NextToken();
        var token3 = lexer.NextToken();
        var token4 = lexer.NextToken();
        
        // Assert
        Assert.Equal(TokenType.Identifier, token1.Type);
        Assert.Equal("user", token1.Lexeme);
        
        Assert.Equal(TokenType.Identifier, token2.Type);
        Assert.Equal("username", token2.Lexeme);
        
        Assert.Equal(TokenType.Identifier, token3.Type);
        Assert.Equal("User123", token3.Lexeme);
        
        Assert.Equal(TokenType.Identifier, token4.Type);
        Assert.Equal("_private", token4.Lexeme);
    }
    
    [Fact]
    public void Lexer_ShouldTokenizeStringLiterals()
    {
        // Arrange
        var lexer = new Lexer("'hello' \"world\" 'John\\'s'");
        
        // Act
        var token1 = lexer.NextToken();
        var token2 = lexer.NextToken();
        var token3 = lexer.NextToken();
        
        // Assert
        Assert.Equal(TokenType.StringLiteral, token1.Type);
        Assert.Equal("hello", token1.Literal);
        
        Assert.Equal(TokenType.StringLiteral, token2.Type);
        Assert.Equal("world", token2.Literal);
        
        Assert.Equal(TokenType.StringLiteral, token3.Type);
        Assert.Equal("John's", token3.Literal);
    }
    
    [Fact]
    public void Lexer_ShouldTokenizeNumberLiterals()
    {
        // Arrange
        var lexer = new Lexer("123 45.67 0 999.999");
        
        // Act
        var token1 = lexer.NextToken();
        var token2 = lexer.NextToken();
        var token3 = lexer.NextToken();
        var token4 = lexer.NextToken();
        
        // Assert
        Assert.Equal(TokenType.NumberLiteral, token1.Type);
        Assert.Equal(123L, token1.Literal);
        
        Assert.Equal(TokenType.NumberLiteral, token2.Type);
        Assert.Equal(45.67, token2.Literal);
        
        Assert.Equal(TokenType.NumberLiteral, token3.Type);
        Assert.Equal(0L, token3.Literal);
        
        Assert.Equal(TokenType.NumberLiteral, token4.Type);
        Assert.Equal(999.999, token4.Literal);
    }
    
    [Fact]
    public void Lexer_ShouldTokenizeParameters()
    {
        // Arrange
        var lexer = new Lexer(":username :id :value");
        
        // Act
        var token1 = lexer.NextToken();
        var token2 = lexer.NextToken();
        var token3 = lexer.NextToken();
        
        // Assert
        Assert.Equal(TokenType.Parameter, token1.Type);
        Assert.Equal("username", token1.Literal);
        
        Assert.Equal(TokenType.Parameter, token2.Type);
        Assert.Equal("id", token2.Literal);
        
        Assert.Equal(TokenType.Parameter, token3.Type);
        Assert.Equal("value", token3.Literal);
    }
    
    [Fact]
    public void Lexer_ShouldTokenizeOperators()
    {
        // Arrange
        var lexer = new Lexer("= <> != < <= > >= + - * / %");
        
        // Act & Assert
        Assert.Equal(TokenType.Equal, lexer.NextToken().Type);
        Assert.Equal(TokenType.NotEqual, lexer.NextToken().Type);
        Assert.Equal(TokenType.NotEqual, lexer.NextToken().Type);
        Assert.Equal(TokenType.LessThan, lexer.NextToken().Type);
        Assert.Equal(TokenType.LessThanOrEqual, lexer.NextToken().Type);
        Assert.Equal(TokenType.GreaterThan, lexer.NextToken().Type);
        Assert.Equal(TokenType.GreaterThanOrEqual, lexer.NextToken().Type);
        Assert.Equal(TokenType.Plus, lexer.NextToken().Type);
        Assert.Equal(TokenType.Minus, lexer.NextToken().Type);
        Assert.Equal(TokenType.Multiply, lexer.NextToken().Type);
        Assert.Equal(TokenType.Divide, lexer.NextToken().Type);
        Assert.Equal(TokenType.Modulo, lexer.NextToken().Type);
    }
    
    [Fact]
    public void Lexer_ShouldTokenizePunctuation()
    {
        // Arrange
        var lexer = new Lexer("( ) , . ; :");
        
        // Act & Assert
        Assert.Equal(TokenType.LeftParenthesis, lexer.NextToken().Type);
        Assert.Equal(TokenType.RightParenthesis, lexer.NextToken().Type);
        Assert.Equal(TokenType.Comma, lexer.NextToken().Type);
        Assert.Equal(TokenType.Dot, lexer.NextToken().Type);
        Assert.Equal(TokenType.Semicolon, lexer.NextToken().Type);
        Assert.Equal(TokenType.Colon, lexer.NextToken().Type);
    }
    
    [Fact]
    public void Lexer_ShouldTokenizeAggregateFunctions()
    {
        // Arrange
        var lexer = new Lexer("COUNT SUM AVG MIN MAX");
        
        // Act & Assert
        Assert.Equal(TokenType.Count, lexer.NextToken().Type);
        Assert.Equal(TokenType.Sum, lexer.NextToken().Type);
        Assert.Equal(TokenType.Avg, lexer.NextToken().Type);
        Assert.Equal(TokenType.Min, lexer.NextToken().Type);
        Assert.Equal(TokenType.Max, lexer.NextToken().Type);
    }
    
    [Fact]
    public void Lexer_ShouldSkipWhitespace()
    {
        // Arrange
        var lexer = new Lexer("  SELECT   FROM   WHERE  ");
        
        // Act & Assert
        Assert.Equal(TokenType.Select, lexer.NextToken().Type);
        Assert.Equal(TokenType.From, lexer.NextToken().Type);
        Assert.Equal(TokenType.Where, lexer.NextToken().Type);
        Assert.Equal(TokenType.Eof, lexer.NextToken().Type);
    }
    
    [Fact]
    public void Lexer_ShouldSkipLineComments()
    {
        // Arrange
        var lexer = new Lexer("SELECT -- this is a comment\nFROM");
        
        // Act & Assert
        Assert.Equal(TokenType.Select, lexer.NextToken().Type);
        Assert.Equal(TokenType.From, lexer.NextToken().Type);
    }
    
    [Fact]
    public void Lexer_ShouldSkipBlockComments()
    {
        // Arrange
        var lexer = new Lexer("SELECT /* this is a block comment */ FROM");
        
        // Act & Assert
        Assert.Equal(TokenType.Select, lexer.NextToken().Type);
        Assert.Equal(TokenType.From, lexer.NextToken().Type);
    }
    
    [Fact]
    public void Lexer_ShouldTokenizeComplexQuery()
    {
        // Arrange
        var query = "SELECT u.Username FROM User u WHERE u.Age > :minAge ORDER BY u.CreatedAt DESC";
        var lexer = new Lexer(query);
        
        // Act & Assert
        Assert.Equal(TokenType.Select, lexer.NextToken().Type);
        Assert.Equal(TokenType.Identifier, lexer.NextToken().Type); // u
        Assert.Equal(TokenType.Dot, lexer.NextToken().Type);
        Assert.Equal(TokenType.Identifier, lexer.NextToken().Type); // Username
        Assert.Equal(TokenType.From, lexer.NextToken().Type);
        Assert.Equal(TokenType.Identifier, lexer.NextToken().Type); // User
        Assert.Equal(TokenType.Identifier, lexer.NextToken().Type); // u
        Assert.Equal(TokenType.Where, lexer.NextToken().Type);
        Assert.Equal(TokenType.Identifier, lexer.NextToken().Type); // u
        Assert.Equal(TokenType.Dot, lexer.NextToken().Type);
        Assert.Equal(TokenType.Identifier, lexer.NextToken().Type); // Age
        Assert.Equal(TokenType.GreaterThan, lexer.NextToken().Type);
        Assert.Equal(TokenType.Parameter, lexer.NextToken().Type); // :minAge
        Assert.Equal(TokenType.OrderBy, lexer.NextToken().Type);
        Assert.Equal(TokenType.OrderBy, lexer.NextToken().Type); // BY
        Assert.Equal(TokenType.Identifier, lexer.NextToken().Type); // u
        Assert.Equal(TokenType.Dot, lexer.NextToken().Type);
        Assert.Equal(TokenType.Identifier, lexer.NextToken().Type); // CreatedAt
        Assert.Equal(TokenType.Desc, lexer.NextToken().Type);
    }
    
    [Fact]
    public void Lexer_ShouldThrowOnUnterminatedString()
    {
        // Arrange
        var lexer = new Lexer("'unterminated");
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => lexer.NextToken());
    }
    
    [Fact]
    public void Lexer_ShouldHandleEscapeSequences()
    {
        // Arrange
        var lexer = new Lexer("'Line1\\nLine2\\tTabbed'");
        
        // Act
        var token = lexer.NextToken();
        
        // Assert
        Assert.Equal(TokenType.StringLiteral, token.Type);
        Assert.Equal("Line1\nLine2\tTabbed", token.Literal);
    }
    
    [Fact]
    public void Lexer_ShouldTokenizeBooleanLiterals()
    {
        // Arrange
        var lexer = new Lexer("TRUE FALSE true false");
        
        // Act
        var token1 = lexer.NextToken();
        var token2 = lexer.NextToken();
        var token3 = lexer.NextToken();
        var token4 = lexer.NextToken();
        
        // Assert
        Assert.Equal(TokenType.BooleanLiteral, token1.Type);
        Assert.Equal(true, token1.Literal);
        
        Assert.Equal(TokenType.BooleanLiteral, token2.Type);
        Assert.Equal(false, token2.Literal);
        
        Assert.Equal(TokenType.BooleanLiteral, token3.Type);
        Assert.Equal(true, token3.Literal);
        
        Assert.Equal(TokenType.BooleanLiteral, token4.Type);
        Assert.Equal(false, token4.Literal);
    }
}

