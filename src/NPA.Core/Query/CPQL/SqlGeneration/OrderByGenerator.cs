using NPA.Core.Query.CPQL.AST;

namespace NPA.Core.Query.CPQL.SqlGeneration;

/// <summary>
/// Generates SQL ORDER BY clauses.
/// </summary>
public sealed class OrderByGenerator : IOrderByGenerator
{
    private readonly IExpressionGenerator _expressionGenerator;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderByGenerator"/> class.
    /// </summary>
    /// <param name="expressionGenerator">The expression generator.</param>
    public OrderByGenerator(IExpressionGenerator expressionGenerator)
    {
        _expressionGenerator = expressionGenerator ?? throw new ArgumentNullException(nameof(expressionGenerator));
    }
    
    /// <inheritdoc />
    public string Generate(OrderByClause orderBy)
    {
        if (orderBy == null || orderBy.Items.Count == 0)
            return string.Empty;
        
        var items = orderBy.Items.Select(item =>
        {
            var expression = _expressionGenerator.Generate(item.Expression);
            var direction = item.Direction == OrderDirection.Descending ? " DESC" : " ASC";
            return expression + direction;
        });
        
        return string.Join(", ", items);
    }
}

