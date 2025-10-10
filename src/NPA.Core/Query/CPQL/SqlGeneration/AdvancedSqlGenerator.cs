using System.Text;
using NPA.Core.Query.CPQL.AST;

namespace NPA.Core.Query.CPQL.SqlGeneration;

/// <summary>
/// Advanced SQL generator that supports full CPQL features.
/// </summary>
public sealed class AdvancedSqlGenerator
{
    private readonly IEntityResolver _entityResolver;
    private readonly IFunctionRegistry _functionRegistry;
    private readonly string _dialect;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AdvancedSqlGenerator"/> class.
    /// </summary>
    /// <param name="entityResolver">The entity resolver.</param>
    /// <param name="functionRegistry">The function registry.</param>
    /// <param name="dialect">The database dialect.</param>
    public AdvancedSqlGenerator(
        IEntityResolver entityResolver,
        IFunctionRegistry functionRegistry,
        string dialect = "default")
    {
        _entityResolver = entityResolver ?? throw new ArgumentNullException(nameof(entityResolver));
        _functionRegistry = functionRegistry ?? throw new ArgumentNullException(nameof(functionRegistry));
        _dialect = dialect;
    }
    
    /// <summary>
    /// Generates SQL from a query node.
    /// </summary>
    /// <param name="query">The query node.</param>
    /// <returns>The generated SQL.</returns>
    public string Generate(QueryNode query)
    {
        return query switch
        {
            SelectQuery selectQuery => GenerateSelect(selectQuery),
            UpdateQuery updateQuery => GenerateUpdate(updateQuery),
            DeleteQuery deleteQuery => GenerateDelete(deleteQuery),
            _ => throw new NotSupportedException($"Query type {query.GetType().Name} is not supported")
        };
    }
    
    private string GenerateSelect(SelectQuery query)
    {
        if (query.FromClause == null || query.FromClause.Items.Count == 0)
            throw new InvalidOperationException("SELECT query must have a FROM clause");
        
        // Build entity mapper with alias registrations
        var entityMapper = new EntityMapper(_entityResolver);
        foreach (var fromItem in query.FromClause.Items)
        {
            var alias = fromItem.Alias ?? fromItem.EntityName;
            entityMapper.RegisterAlias(alias, fromItem.EntityName);
        }
        
        foreach (var join in query.FromClause.Joins)
        {
            var alias = join.Alias ?? join.EntityName;
            entityMapper.RegisterAlias(alias, join.EntityName);
        }
        
        var expressionGenerator = new ExpressionGenerator(entityMapper, _functionRegistry, _dialect);
        var joinGenerator = new JoinGenerator(entityMapper, expressionGenerator);
        var orderByGenerator = new OrderByGenerator(expressionGenerator);
        
        var sql = new StringBuilder();
        
        // SELECT clause
        sql.Append("SELECT ");
        if (query.SelectClause?.IsDistinct == true)
        {
            sql.Append("DISTINCT ");
        }
        
        if (query.SelectClause == null || query.SelectClause.Items.Count == 0)
        {
            sql.Append("*");
        }
        else
        {
            var selectItems = query.SelectClause.Items.Select(item =>
            {
                var expression = expressionGenerator.Generate(item.Expression);
                if (!string.IsNullOrEmpty(item.Alias))
                {
                    return $"{expression} AS {item.Alias}";
                }
                return expression;
            });
            sql.Append(string.Join(", ", selectItems));
        }
        
        // FROM clause
        sql.Append(" FROM ");
        var fromItems = query.FromClause.Items.Select(item =>
        {
            var tableName = entityMapper.GetTableName(item.EntityName);
            if (!string.IsNullOrEmpty(item.Alias))
            {
                return $"{tableName} AS {item.Alias}";
            }
            return tableName;
        });
        sql.Append(string.Join(", ", fromItems));
        
        // JOIN clauses
        if (query.FromClause.Joins.Count > 0)
        {
            sql.Append(joinGenerator.Generate(query.FromClause.Joins));
        }
        
        // WHERE clause
        if (query.WhereClause != null)
        {
            sql.Append(" WHERE ");
            sql.Append(expressionGenerator.Generate(query.WhereClause.Condition));
        }
        
        // GROUP BY clause
        if (query.GroupByClause != null && query.GroupByClause.Items.Count > 0)
        {
            sql.Append(" GROUP BY ");
            var groupByItems = query.GroupByClause.Items.Select(expressionGenerator.Generate);
            sql.Append(string.Join(", ", groupByItems));
        }
        
        // HAVING clause
        if (query.HavingClause != null)
        {
            sql.Append(" HAVING ");
            sql.Append(expressionGenerator.Generate(query.HavingClause.Condition));
        }
        
        // ORDER BY clause
        if (query.OrderByClause != null && query.OrderByClause.Items.Count > 0)
        {
            sql.Append(" ORDER BY ");
            sql.Append(orderByGenerator.Generate(query.OrderByClause));
        }
        
        return sql.ToString();
    }
    
    private string GenerateUpdate(UpdateQuery query)
    {
        var entityMapper = new EntityMapper(_entityResolver);
        entityMapper.RegisterAlias(query.Alias, query.EntityName);
        
        var expressionGenerator = new ExpressionGenerator(entityMapper, _functionRegistry, _dialect);
        
        var sql = new StringBuilder();
        
        sql.Append("UPDATE ");
        sql.Append(entityMapper.GetTableName(query.EntityName));
        sql.Append(" SET ");
        
        var assignments = query.Assignments.Select(assignment =>
        {
            var columnName = _entityResolver.GetColumnName(query.EntityName, assignment.PropertyName);
            var value = expressionGenerator.Generate(assignment.Value);
            return $"{columnName} = {value}";
        });
        sql.Append(string.Join(", ", assignments));
        
        if (query.WhereClause != null)
        {
            sql.Append(" WHERE ");
            sql.Append(expressionGenerator.Generate(query.WhereClause.Condition));
        }
        
        return sql.ToString();
    }
    
    private string GenerateDelete(DeleteQuery query)
    {
        var entityMapper = new EntityMapper(_entityResolver);
        entityMapper.RegisterAlias(query.Alias, query.EntityName);
        
        var expressionGenerator = new ExpressionGenerator(entityMapper, _functionRegistry, _dialect);
        
        var sql = new StringBuilder();
        
        sql.Append("DELETE FROM ");
        sql.Append(entityMapper.GetTableName(query.EntityName));
        
        if (query.WhereClause != null)
        {
            sql.Append(" WHERE ");
            sql.Append(expressionGenerator.Generate(query.WhereClause.Condition));
        }
        
        return sql.ToString();
    }
}

