namespace NPA.Core.Query;

/// <summary>
/// Represents a parsed query structure.
/// </summary>
public class ParsedQuery
{
    /// <summary>
    /// Gets or sets the query type (SELECT, UPDATE, DELETE).
    /// </summary>
    public QueryType Type { get; set; }

    /// <summary>
    /// Gets or sets the entity name.
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alias for the entity.
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the WHERE clause.
    /// </summary>
    public string? WhereClause { get; set; }

    /// <summary>
    /// Gets or sets the ORDER BY clause.
    /// </summary>
    public string? OrderByClause { get; set; }

    /// <summary>
    /// Gets or sets the SET clause for UPDATE queries.
    /// </summary>
    public string? SetClause { get; set; }

    /// <summary>
    /// Gets or sets the JOIN clauses.
    /// </summary>
    public List<JoinClause> Joins { get; set; } = new();

    /// <summary>
    /// Gets or sets the parameter names found in the query.
    /// </summary>
    public List<string> ParameterNames { get; set; } = new();

    /// <summary>
    /// Gets or sets the original CPQL query string.
    /// </summary>
    public string OriginalCpql { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the full AST (Abstract Syntax Tree) for advanced SQL generation.
    /// This contains the complete parsed query structure for advanced features.
    /// </summary>
    public object? Ast { get; set; }
}

/// <summary>
/// Represents a JOIN clause in a query.
/// </summary>
public class JoinClause
{
    /// <summary>
    /// Gets or sets the join type (INNER, LEFT, RIGHT, OUTER).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity name to join.
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alias for the joined entity.
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the join condition.
    /// </summary>
    public string Condition { get; set; } = string.Empty;
}

/// <summary>
/// Represents the type of query.
/// </summary>
public enum QueryType
{
    /// <summary>
    /// A SELECT query.
    /// </summary>
    Select,

    /// <summary>
    /// An UPDATE query.
    /// </summary>
    Update,

    /// <summary>
    /// A DELETE query.
    /// </summary>
    Delete
}

/// <summary>
/// Parses CPQL-like queries into structured representations.
/// </summary>
public interface IQueryParser
{
    /// <summary>
    /// Parses a CPQL query string into a structured representation.
    /// </summary>
    /// <param name="cpql">The CPQL query string.</param>
    /// <returns>The parsed query structure.</returns>
    /// <exception cref="ArgumentException">Thrown when the CPQL syntax is invalid.</exception>
    ParsedQuery Parse(string cpql);
}
