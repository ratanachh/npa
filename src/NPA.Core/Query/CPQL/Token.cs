namespace NPA.Core.Query.CPQL;

/// <summary>
/// Represents a token in CPQL.
/// </summary>
public sealed class Token
{
    /// <summary>
    /// Gets the type of the token.
    /// </summary>
    public TokenType Type { get; }
    
    /// <summary>
    /// Gets the lexeme (text) of the token.
    /// </summary>
    public string Lexeme { get; }
    
    /// <summary>
    /// Gets the literal value of the token (for literals).
    /// </summary>
    public object? Literal { get; }
    
    /// <summary>
    /// Gets the position of the token in the source text.
    /// </summary>
    public int Position { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Token"/> class.
    /// </summary>
    /// <param name="type">The token type.</param>
    /// <param name="lexeme">The lexeme.</param>
    /// <param name="literal">The literal value (optional).</param>
    /// <param name="position">The position in source text.</param>
    public Token(TokenType type, string lexeme, object? literal = null, int position = 0)
    {
        Type = type;
        Lexeme = lexeme ?? throw new ArgumentNullException(nameof(lexeme));
        Literal = literal;
        Position = position;
    }
    
    /// <inheritdoc />
    public override string ToString()
    {
        return Literal != null 
            ? $"{Type} '{Lexeme}' ({Literal})" 
            : $"{Type} '{Lexeme}'";
    }
}

