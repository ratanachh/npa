using NPA.Core.Query.CPQL.AST;

namespace NPA.Core.Query.CPQL;

/// <summary>
/// Main entry point for parsing CPQL queries.
/// </summary>
public sealed class CPQLParser
{
    /// <summary>
    /// Parses a CPQL query string into an AST.
    /// </summary>
    /// <param name="cpql">The CPQL query string.</param>
    /// <returns>The parsed query node.</returns>
    /// <exception cref="ArgumentException">Thrown when the CPQL is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when parsing fails.</exception>
    public QueryNode Parse(string cpql)
    {
        if (string.IsNullOrWhiteSpace(cpql))
            throw new ArgumentException("CPQL query cannot be null or empty", nameof(cpql));
        
        var lexer = new Lexer(cpql);
        var parser = new Parser(lexer);
        
        return parser.Parse();
    }
}

