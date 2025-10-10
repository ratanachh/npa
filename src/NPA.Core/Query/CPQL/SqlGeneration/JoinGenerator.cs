using System.Text;
using NPA.Core.Query.CPQL.AST;

namespace NPA.Core.Query.CPQL.SqlGeneration;

/// <summary>
/// Generates SQL JOIN clauses.
/// </summary>
public sealed class JoinGenerator : IJoinGenerator
{
    private readonly IEntityMapper _entityMapper;
    private readonly IExpressionGenerator _expressionGenerator;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="JoinGenerator"/> class.
    /// </summary>
    /// <param name="entityMapper">The entity mapper.</param>
    /// <param name="expressionGenerator">The expression generator.</param>
    public JoinGenerator(IEntityMapper entityMapper, IExpressionGenerator expressionGenerator)
    {
        _entityMapper = entityMapper ?? throw new ArgumentNullException(nameof(entityMapper));
        _expressionGenerator = expressionGenerator ?? throw new ArgumentNullException(nameof(expressionGenerator));
    }
    
    /// <inheritdoc />
    public string Generate(List<AST.JoinClause> joins)
    {
        if (joins == null || joins.Count == 0)
            return string.Empty;
        
        var sql = new StringBuilder();
        
        foreach (var join in joins)
        {
            sql.Append(join.JoinType switch
            {
                AST.JoinType.Inner => " INNER JOIN ",
                AST.JoinType.Left => " LEFT JOIN ",
                AST.JoinType.Right => " RIGHT JOIN ",
                AST.JoinType.Full => " FULL OUTER JOIN ",
                _ => throw new NotSupportedException($"Join type {join.JoinType} is not supported")
            });
            
            var tableName = _entityMapper.GetTableName(join.EntityName);
            sql.Append(tableName);
            
            if (!string.IsNullOrEmpty(join.Alias))
            {
                sql.Append(" AS ");
                sql.Append(join.Alias);
            }
            
            if (join.OnCondition != null)
            {
                sql.Append(" ON ");
                sql.Append(_expressionGenerator.Generate(join.OnCondition));
            }
        }
        
        return sql.ToString();
    }
}

