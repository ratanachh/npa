namespace NPA.Core.Query.CPQL.AST;

/// <summary>
/// Represents a SELECT clause.
/// </summary>
public sealed class SelectClause
{
    /// <summary>Gets or sets whether DISTINCT should be applied.</summary>
    public bool IsDistinct { get; set; }
    
    /// <summary>Gets or sets the list of items to select.</summary>
    public List<SelectItem> Items { get; set; } = new();
}

/// <summary>
/// Represents a single item in a SELECT clause.
/// </summary>
public sealed class SelectItem
{
    /// <summary>Gets or sets the expression to select.</summary>
    public Expression Expression { get; set; } = null!;
    
    /// <summary>Gets or sets the alias for the selected item.</summary>
    public string? Alias { get; set; }
}

/// <summary>
/// Represents a FROM clause.
/// </summary>
public sealed class FromClause
{
    /// <summary>Gets or sets the list of tables/entities to select from.</summary>
    public List<FromItem> Items { get; set; } = new();
    
    /// <summary>Gets or sets the list of JOIN clauses.</summary>
    public List<JoinClause> Joins { get; set; } = new();
}

/// <summary>
/// Represents a single item in a FROM clause.
/// </summary>
public sealed class FromItem
{
    /// <summary>Gets or sets the entity name.</summary>
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>Gets or sets the alias for the entity.</summary>
    public string? Alias { get; set; }
}

/// <summary>
/// Represents a JOIN clause.
/// </summary>
public sealed class JoinClause
{
    /// <summary>Gets or sets the type of join.</summary>
    public JoinType JoinType { get; set; }
    
    /// <summary>Gets or sets the entity name to join.</summary>
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>Gets or sets the alias for the joined entity.</summary>
    public string? Alias { get; set; }
    
    /// <summary>Gets or sets the ON condition for the join.</summary>
    public Expression? OnCondition { get; set; }
}

/// <summary>
/// Types of JOIN operations.
/// </summary>
public enum JoinType
{
    /// <summary>INNER JOIN.</summary>
    Inner,
    
    /// <summary>LEFT JOIN (LEFT OUTER JOIN).</summary>
    Left,
    
    /// <summary>RIGHT JOIN (RIGHT OUTER JOIN).</summary>
    Right,
    
    /// <summary>FULL JOIN (FULL OUTER JOIN).</summary>
    Full
}

/// <summary>
/// Represents a WHERE clause.
/// </summary>
public sealed class WhereClause
{
    /// <summary>Gets or sets the condition expression.</summary>
    public Expression Condition { get; set; } = null!;
}

/// <summary>
/// Represents a GROUP BY clause.
/// </summary>
public sealed class GroupByClause
{
    /// <summary>Gets or sets the list of expressions to group by.</summary>
    public List<Expression> Items { get; set; } = new();
}

/// <summary>
/// Represents a HAVING clause.
/// </summary>
public sealed class HavingClause
{
    /// <summary>Gets or sets the condition expression.</summary>
    public Expression Condition { get; set; } = null!;
}

/// <summary>
/// Represents an ORDER BY clause.
/// </summary>
public sealed class OrderByClause
{
    /// <summary>Gets or sets the list of order by items.</summary>
    public List<OrderByItem> Items { get; set; } = new();
}

/// <summary>
/// Represents a single item in an ORDER BY clause.
/// </summary>
public sealed class OrderByItem
{
    /// <summary>Gets or sets the expression to order by.</summary>
    public Expression Expression { get; set; } = null!;
    
    /// <summary>Gets or sets the sort direction.</summary>
    public OrderDirection Direction { get; set; } = OrderDirection.Ascending;
}

/// <summary>
/// Order direction for ORDER BY.
/// </summary>
public enum OrderDirection
{
    /// <summary>Ascending order (ASC).</summary>
    Ascending,
    
    /// <summary>Descending order (DESC).</summary>
    Descending
}
