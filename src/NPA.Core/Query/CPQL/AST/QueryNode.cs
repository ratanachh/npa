namespace NPA.Core.Query.CPQL.AST;

/// <summary>
/// Base class for all query AST nodes.
/// </summary>
public abstract class QueryNode
{
}

/// <summary>
/// Represents a complete SELECT query.
/// </summary>
public sealed class SelectQuery : QueryNode
{
    /// <summary>Gets or sets the SELECT clause.</summary>
    public SelectClause? SelectClause { get; set; }
    
    /// <summary>Gets or sets the FROM clause.</summary>
    public FromClause? FromClause { get; set; }
    
    /// <summary>Gets or sets the WHERE clause.</summary>
    public WhereClause? WhereClause { get; set; }
    
    /// <summary>Gets or sets the GROUP BY clause.</summary>
    public GroupByClause? GroupByClause { get; set; }
    
    /// <summary>Gets or sets the HAVING clause.</summary>
    public HavingClause? HavingClause { get; set; }
    
    /// <summary>Gets or sets the ORDER BY clause.</summary>
    public OrderByClause? OrderByClause { get; set; }
}

/// <summary>
/// Represents an UPDATE query.
/// </summary>
public sealed class UpdateQuery : QueryNode
{
    /// <summary>Gets or sets the entity name.</summary>
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>Gets or sets the entity alias.</summary>
    public string Alias { get; set; } = string.Empty;
    
    /// <summary>Gets or sets the list of SET assignments.</summary>
    public List<SetAssignment> Assignments { get; set; } = new();
    
    /// <summary>Gets or sets the WHERE clause.</summary>
    public WhereClause? WhereClause { get; set; }
}

/// <summary>
/// Represents a DELETE query.
/// </summary>
public sealed class DeleteQuery : QueryNode
{
    /// <summary>Gets or sets the entity name.</summary>
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>Gets or sets the entity alias.</summary>
    public string Alias { get; set; } = string.Empty;
    
    /// <summary>Gets or sets the WHERE clause.</summary>
    public WhereClause? WhereClause { get; set; }
}

/// <summary>
/// Represents a SET assignment in an UPDATE query.
/// </summary>
public sealed class SetAssignment
{
    /// <summary>Gets or sets the property name to update.</summary>
    public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>Gets or sets the value expression.</summary>
    public Expression Value { get; set; } = null!;
}
