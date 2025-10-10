using System.Globalization;
using System.Text;

namespace NPA.Core.Query.CPQL;

/// <summary>
/// Lexical analyzer for CPQL queries.
/// </summary>
public sealed class Lexer
{
    private readonly string _source;
    private int _position;
    private int _line;
    private int _column;
    
    private static readonly Dictionary<string, TokenType> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        { "SELECT", TokenType.Select },
        { "FROM", TokenType.From },
        { "WHERE", TokenType.Where },
        { "ORDER", TokenType.OrderBy }, // ORDER keyword (parser will handle "Order" entity in context)
        { "BY", TokenType.OrderBy }, // BY is also OrderBy for GROUP BY / ORDER BY
        { "GROUP", TokenType.GroupBy },
        { "HAVING", TokenType.Having },
        { "JOIN", TokenType.Join },
        { "INNER", TokenType.InnerJoin },
        { "LEFT", TokenType.LeftJoin },
        { "RIGHT", TokenType.RightJoin },
        { "FULL", TokenType.FullJoin },
        { "ON", TokenType.On },
        { "AS", TokenType.As },
        { "DISTINCT", TokenType.Distinct },
        { "UPDATE", TokenType.Update },
        { "SET", TokenType.Set },
        { "DELETE", TokenType.Delete },
        { "INSERT", TokenType.Insert },
        { "INTO", TokenType.Into },
        { "VALUES", TokenType.Values },
        { "AND", TokenType.And },
        { "OR", TokenType.Or },
        { "NOT", TokenType.Not },
        { "LIKE", TokenType.Like },
        { "IN", TokenType.In },
        { "BETWEEN", TokenType.Between },
        { "IS", TokenType.Is },
        { "NULL", TokenType.Null },
        { "COUNT", TokenType.Count },
        { "SUM", TokenType.Sum },
        { "AVG", TokenType.Avg },
        { "MIN", TokenType.Min },
        { "MAX", TokenType.Max },
        { "UPPER", TokenType.Upper },
        { "LOWER", TokenType.Lower },
        { "LENGTH", TokenType.Length },
        { "SUBSTRING", TokenType.Substring },
        { "TRIM", TokenType.Trim },
        { "CONCAT", TokenType.Concat },
        { "YEAR", TokenType.Year },
        { "MONTH", TokenType.Month },
        { "DAY", TokenType.Day },
        { "HOUR", TokenType.Hour },
        { "MINUTE", TokenType.Minute },
        { "SECOND", TokenType.Second },
        { "NOW", TokenType.Now },
        { "ASC", TokenType.Asc },
        { "DESC", TokenType.Desc },
        { "TRUE", TokenType.BooleanLiteral },
        { "FALSE", TokenType.BooleanLiteral }
    };
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Lexer"/> class.
    /// </summary>
    /// <param name="source">The source CPQL text.</param>
    public Lexer(string source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _position = 0;
        _line = 1;
        _column = 1;
    }
    
    /// <summary>
    /// Gets the next token from the source.
    /// </summary>
    /// <returns>The next token.</returns>
    public Token NextToken()
    {
        SkipWhitespace();
        
        if (IsAtEnd())
            return new Token(TokenType.Eof, string.Empty, null, _position);
        
        var start = _position;
        var c = Advance();
        
        // Single-character tokens
        switch (c)
        {
            case '(': return new Token(TokenType.LeftParenthesis, "(", null, start);
            case ')': return new Token(TokenType.RightParenthesis, ")", null, start);
            case ',': return new Token(TokenType.Comma, ",", null, start);
            case '.': return new Token(TokenType.Dot, ".", null, start);
            case ';': return new Token(TokenType.Semicolon, ";", null, start);
            case '+': return new Token(TokenType.Plus, "+", null, start);
            case '-': return new Token(TokenType.Minus, "-", null, start);
            case '*': return new Token(TokenType.Multiply, "*", null, start);
            case '/': return new Token(TokenType.Divide, "/", null, start);
            case '%': return new Token(TokenType.Modulo, "%", null, start);
        }
        
        // Two-character tokens
        if (c == ':')
        {
            if (IsAlpha(Peek()))
                return ScanParameter(start);
            return new Token(TokenType.Colon, ":", null, start);
        }
        
        if (c == '=')
            return new Token(TokenType.Equal, "=", null, start);
        
        if (c == '<')
        {
            if (Match('='))
                return new Token(TokenType.LessThanOrEqual, "<=", null, start);
            if (Match('>'))
                return new Token(TokenType.NotEqual, "<>", null, start);
            return new Token(TokenType.LessThan, "<", null, start);
        }
        
        if (c == '>')
        {
            if (Match('='))
                return new Token(TokenType.GreaterThanOrEqual, ">=", null, start);
            return new Token(TokenType.GreaterThan, ">", null, start);
        }
        
        if (c == '!')
        {
            if (Match('='))
                return new Token(TokenType.NotEqual, "!=", null, start);
            throw new InvalidOperationException($"Unexpected character '!' at position {start}");
        }
        
        // String literals
        if (c == '\'' || c == '"')
            return ScanString(c, start);
        
        // Number literals
        if (IsDigit(c))
            return ScanNumber(start);
        
        // Identifiers and keywords
        if (IsAlpha(c) || c == '_')
            return ScanIdentifier(start);
        
        throw new InvalidOperationException($"Unexpected character '{c}' at position {start}");
    }
    
    private Token ScanParameter(int start)
    {
        while (IsAlphaNumeric(Peek()))
            Advance();
        
        var lexeme = _source.Substring(start, _position - start);
        var paramName = lexeme.Substring(1); // Remove the ':'
        
        return new Token(TokenType.Parameter, lexeme, paramName, start);
    }
    
    private Token ScanString(char quote, int start)
    {
        var sb = new StringBuilder();
        
        while (!IsAtEnd() && Peek() != quote)
        {
            if (Peek() == '\\')
            {
                Advance(); // Skip the backslash
                if (!IsAtEnd())
                {
                    var escaped = Advance();
                    sb.Append(escaped switch
                    {
                        'n' => '\n',
                        't' => '\t',
                        'r' => '\r',
                        '\\' => '\\',
                        '\'' => '\'',
                        '"' => '"',
                        _ => escaped
                    });
                }
            }
            else
            {
                sb.Append(Advance());
            }
        }
        
        if (IsAtEnd())
            throw new InvalidOperationException($"Unterminated string at position {start}");
        
        Advance(); // Closing quote
        
        var lexeme = _source.Substring(start, _position - start);
        return new Token(TokenType.StringLiteral, lexeme, sb.ToString(), start);
    }
    
    private Token ScanNumber(int start)
    {
        while (IsDigit(Peek()))
            Advance();
        
        // Look for a decimal part
        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            Advance(); // Consume the '.'
            
            while (IsDigit(Peek()))
                Advance();
        }
        
        var lexeme = _source.Substring(start, _position - start);
        
        if (lexeme.Contains('.'))
        {
            if (double.TryParse(lexeme, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
                return new Token(TokenType.NumberLiteral, lexeme, doubleValue, start);
        }
        else
        {
            if (long.TryParse(lexeme, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
                return new Token(TokenType.NumberLiteral, lexeme, longValue, start);
        }
        
        throw new InvalidOperationException($"Invalid number format '{lexeme}' at position {start}");
    }
    
    private Token ScanIdentifier(int start)
    {
        while (IsAlphaNumeric(Peek()) || Peek() == '_')
            Advance();
        
        var lexeme = _source.Substring(start, _position - start);
        
        // Check if it's a keyword
        if (Keywords.TryGetValue(lexeme, out var tokenType))
        {
            // Special handling for boolean literals
            if (tokenType == TokenType.BooleanLiteral)
            {
                var value = lexeme.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                return new Token(tokenType, lexeme, value, start);
            }
            
            return new Token(tokenType, lexeme, null, start);
        }
        
        return new Token(TokenType.Identifier, lexeme, null, start);
    }
    
    private void SkipWhitespace()
    {
        while (!IsAtEnd())
        {
            var c = Peek();
            if (char.IsWhiteSpace(c))
            {
                if (c == '\n')
                {
                    _line++;
                    _column = 0;
                }
                Advance();
            }
            else if (c == '-' && PeekNext() == '-')
            {
                // Skip line comment
                while (!IsAtEnd() && Peek() != '\n')
                    Advance();
            }
            else if (c == '/' && PeekNext() == '*')
            {
                // Skip block comment
                Advance(); // /
                Advance(); // *
                while (!IsAtEnd())
                {
                    if (Peek() == '*' && PeekNext() == '/')
                    {
                        Advance(); // *
                        Advance(); // /
                        break;
                    }
                    Advance();
                }
            }
            else
            {
                break;
            }
        }
    }
    
    private bool IsAtEnd() => _position >= _source.Length;
    
    private char Peek() => IsAtEnd() ? '\0' : _source[_position];
    
    private char PeekNext() => _position + 1 >= _source.Length ? '\0' : _source[_position + 1];
    
    private char Advance()
    {
        _column++;
        return _source[_position++];
    }
    
    private bool Match(char expected)
    {
        if (IsAtEnd() || _source[_position] != expected)
            return false;
        
        _position++;
        _column++;
        return true;
    }
    
    private bool IsDigit(char c) => c >= '0' && c <= '9';
    
    private bool IsAlpha(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
    
    private bool IsAlphaNumeric(char c) => IsAlpha(c) || IsDigit(c);
}

