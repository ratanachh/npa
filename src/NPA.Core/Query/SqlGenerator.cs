using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NPA.Core.Metadata;
using NPA.Core.Query.CPQL;
using NPA.Core.Query.CPQL.AST;

namespace NPA.Core.Query;

/// <summary>
/// Generates database-specific SQL from parsed queries with support for advanced CPQL features.
/// </summary>
public class SqlGenerator : ISqlGenerator
{
    private readonly IMetadataProvider? _metadataProvider;
    private readonly string _dialect;
    private readonly ILogger<SqlGenerator>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlGenerator"/> class.
    /// </summary>
    /// <param name="metadataProvider">The metadata provider (optional).</param>
    /// <param name="dialect">The database dialect (default: "default").</param>
    /// <param name="logger">The logger (optional).</param>
    public SqlGenerator(IMetadataProvider? metadataProvider = null, string dialect = "default", ILogger<SqlGenerator>? logger = null)
    {
        _metadataProvider = metadataProvider;
        _dialect = dialect;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Generate(ParsedQuery parsedQuery, EntityMetadata entityMetadata)
    {
        _logger?.LogDebug("Generating SQL for {QueryType} query (Dialect: {Dialect})", parsedQuery.Type, _dialect);
        _logger?.LogDebug("Original CPQL: {Cpql}", parsedQuery.OriginalCpql);
        
        string sql;
        
        // If AST is available, use advanced generation
        if (parsedQuery.Ast != null)
        {
            _logger?.LogDebug("Using advanced SQL generation from AST");
            sql = GenerateFromAst(parsedQuery.Ast, parsedQuery, entityMetadata);
        }
        else
        {
            // Fallback to basic generation (should not happen with new parser)
            _logger?.LogDebug("Using basic SQL generation (fallback)");
            sql = parsedQuery.Type switch
            {
                QueryType.Select => GenerateSelect(parsedQuery, entityMetadata),
                QueryType.Update => GenerateUpdate(parsedQuery, entityMetadata),
                QueryType.Delete => GenerateDelete(parsedQuery, entityMetadata),
                _ => throw new ArgumentException($"Unsupported query type: {parsedQuery.Type}")
            };
        }
        
        _logger?.LogDebug("Generated SQL: {Sql}", sql);
        
        if (parsedQuery.ParameterNames.Count > 0)
        {
            _logger?.LogDebug("Query parameters: [{Parameters}]", string.Join(", ", parsedQuery.ParameterNames));
        }
        
        return sql;
    }

    private string GenerateFromAst(object ast, ParsedQuery parsedQuery, EntityMetadata entityMetadata)
    {
        return ast switch
        {
            SelectQuery selectQuery => GenerateSelectFromAst(selectQuery, parsedQuery, entityMetadata),
            UpdateQuery updateQuery => GenerateUpdateFromAst(updateQuery, parsedQuery, entityMetadata),
            DeleteQuery deleteQuery => GenerateDeleteFromAst(deleteQuery, parsedQuery, entityMetadata),
            _ => throw new NotSupportedException($"AST type {ast.GetType().Name} is not supported")
        };
    }

    private string GenerateSelectFromAst(SelectQuery query, ParsedQuery parsedQuery, EntityMetadata entityMetadata)
    {
        var sql = new StringBuilder();
        
        // SELECT clause
        sql.Append("SELECT ");
        
        if (query.SelectClause?.IsDistinct == true)
        {
            sql.Append("DISTINCT ");
        }

        if (query.SelectClause == null || query.SelectClause.Items.Count == 0)
        {
            // Default: SELECT all columns
            sql.Append(GenerateSelectColumns(entityMetadata, parsedQuery.Alias));
        }
        else
        {
            var selectItems = query.SelectClause.Items.Select(item =>
                GenerateSelectItem(item, entityMetadata, parsedQuery.Alias));
            sql.Append(string.Join(", ", selectItems));
        }
        
        // FROM clause
        if (query.FromClause != null && query.FromClause.Items.Count > 0)
        {
            sql.Append(" FROM ");
            var fromItems = query.FromClause.Items.Select(item =>
            {
                var tableName = GetTableName(entityMetadata);
                if (!string.IsNullOrEmpty(item.Alias))
                {
                    return $"{tableName} AS {item.Alias}";
                }
                return $"{tableName} {item.EntityName}";
            });
            sql.Append(string.Join(", ", fromItems));
            
            // JOIN clauses
            if (query.FromClause.Joins.Count > 0)
            {
                foreach (var join in query.FromClause.Joins)
                {
                    sql.Append(GenerateJoinClause(join, entityMetadata, parsedQuery.Alias));
                }
            }
        }
        
        // WHERE clause
        if (query.WhereClause != null)
        {
            sql.Append(" WHERE ");
            sql.Append(GenerateExpression(query.WhereClause.Condition, entityMetadata, parsedQuery.Alias));
        }
        
        // GROUP BY clause
        if (query.GroupByClause != null && query.GroupByClause.Items.Count > 0)
        {
            sql.Append(" GROUP BY ");
            var groupByItems = query.GroupByClause.Items.Select(expr => 
                GenerateExpression(expr, entityMetadata, parsedQuery.Alias));
            sql.Append(string.Join(", ", groupByItems));
        }
        
        // HAVING clause
        if (query.HavingClause != null)
        {
            sql.Append(" HAVING ");
            sql.Append(GenerateExpression(query.HavingClause.Condition, entityMetadata, parsedQuery.Alias));
        }
        
        // ORDER BY clause
        if (query.OrderByClause != null && query.OrderByClause.Items.Count > 0)
        {
            sql.Append(" ORDER BY ");
            var orderByItems = query.OrderByClause.Items.Select(item =>
            {
                var expr = GenerateExpression(item.Expression, entityMetadata, parsedQuery.Alias);
                var direction = item.Direction == OrderDirection.Descending ? " DESC" : " ASC";
                return expr + direction;
            });
            sql.Append(string.Join(", ", orderByItems));
        }
        
        return sql.ToString();
    }

    private string GenerateUpdateFromAst(UpdateQuery query, ParsedQuery parsedQuery, EntityMetadata entityMetadata)
    {
        var sql = new StringBuilder();
        
        sql.Append("UPDATE ");
        sql.Append(GetTableName(entityMetadata));
        sql.Append(" SET ");
        
        var assignments = query.Assignments.Select(assignment =>
        {
            var columnName = GetColumnName(entityMetadata, assignment.PropertyName);
            var value = GenerateExpression(assignment.Value, entityMetadata, query.Alias);
            return $"{columnName} = {value}";
        });
        sql.Append(string.Join(", ", assignments));
        
        if (query.WhereClause != null)
        {
            sql.Append(" WHERE ");
            sql.Append(GenerateExpression(query.WhereClause.Condition, entityMetadata, query.Alias));
        }
        
        return sql.ToString();
    }

    private string GenerateDeleteFromAst(DeleteQuery query, ParsedQuery parsedQuery, EntityMetadata entityMetadata)
    {
        var sql = new StringBuilder();
        
        sql.Append("DELETE FROM ");
        sql.Append(GetTableName(entityMetadata));
        
        if (query.WhereClause != null)
        {
            sql.Append(" WHERE ");
            sql.Append(GenerateExpression(query.WhereClause.Condition, entityMetadata, query.Alias));
        }
        
        return sql.ToString();
    }

    private string GenerateSelectItem(SelectItem item, EntityMetadata entityMetadata, string alias)
    {
        // Special case: If the expression is just the alias itself (e.g., "SELECT c FROM Customer c"),
        // it means "select all columns", so we should generate the full column list
        if (item.Expression is PropertyExpression prop && 
            prop.EntityAlias == null && 
            prop.PropertyName == alias)
        {
            // This is "SELECT c" meaning "SELECT all columns"
            return GenerateSelectColumns(entityMetadata, alias);
        }
        
        var expression = GenerateExpression(item.Expression, entityMetadata, alias);
        
        if (!string.IsNullOrEmpty(item.Alias))
        {
            return $"{expression} AS {item.Alias}";
        }
        
        return expression;
    }

    private string GenerateJoinClause(CPQL.AST.JoinClause join, EntityMetadata entityMetadata, string primaryAlias)
    {
        var sql = new StringBuilder();
        
        sql.Append(join.JoinType switch
        {
            CPQL.AST.JoinType.Inner => " INNER JOIN ",
            CPQL.AST.JoinType.Left => " LEFT JOIN ",
            CPQL.AST.JoinType.Right => " RIGHT JOIN ",
            CPQL.AST.JoinType.Full => " FULL OUTER JOIN ",
            _ => throw new NotSupportedException($"Join type {join.JoinType} is not supported")
        });
        
        // For now, use the same table (will need entity resolver for multi-table joins)
        sql.Append(GetTableName(entityMetadata));
        
        if (!string.IsNullOrEmpty(join.Alias))
        {
            sql.Append($" AS {join.Alias}");
        }
        
        if (join.OnCondition != null)
        {
            sql.Append(" ON ");
            sql.Append(GenerateExpression(join.OnCondition, entityMetadata, primaryAlias));
        }
        
        return sql.ToString();
    }

    private string GenerateExpression(Expression expression, EntityMetadata entityMetadata, string alias)
    {
        return expression switch
        {
            PropertyExpression prop => GeneratePropertyExpression(prop, entityMetadata, alias),
            LiteralExpression literal => GenerateLiteralExpression(literal),
            ParameterExpression param => GenerateParameterExpression(param),
            BinaryExpression binary => GenerateBinaryExpression(binary, entityMetadata, alias),
            UnaryExpression unary => GenerateUnaryExpression(unary, entityMetadata, alias),
            FunctionExpression func => GenerateFunctionExpression(func, entityMetadata, alias),
            AggregateExpression agg => GenerateAggregateExpression(agg, entityMetadata, alias),
            WildcardExpression wildcard => GenerateWildcardExpression(wildcard, alias),
            _ => throw new NotSupportedException($"Expression type {expression.GetType().Name} is not supported")
        };
    }

    private string GeneratePropertyExpression(PropertyExpression prop, EntityMetadata entityMetadata, string alias)
    {
        var columnName = GetColumnName(entityMetadata, prop.PropertyName);
        var entityAlias = prop.EntityAlias ?? alias;
        return $"{entityAlias}.{columnName}";
    }

    private string GenerateLiteralExpression(LiteralExpression literal)
    {
        if (literal.Value == null)
            return "NULL";
        
        return literal.Value switch
        {
            string str => $"'{str.Replace("'", "''")}'",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            bool b => b ? "1" : "0",
            _ => literal.Value.ToString() ?? "NULL"
        };
    }

    private string GenerateParameterExpression(ParameterExpression param)
    {
        return $"@{param.ParameterName}";
    }

    private string GenerateBinaryExpression(BinaryExpression binary, EntityMetadata entityMetadata, string alias)
    {
        var left = GenerateExpression(binary.Left, entityMetadata, alias);
        var right = GenerateExpression(binary.Right, entityMetadata, alias);
        
        var op = binary.Operator switch
        {
            BinaryOperator.Add => "+",
            BinaryOperator.Subtract => "-",
            BinaryOperator.Multiply => "*",
            BinaryOperator.Divide => "/",
            BinaryOperator.Modulo => "%",
            BinaryOperator.Equal => "=",
            BinaryOperator.NotEqual => "<>",
            BinaryOperator.LessThan => "<",
            BinaryOperator.LessThanOrEqual => "<=",
            BinaryOperator.GreaterThan => ">",
            BinaryOperator.GreaterThanOrEqual => ">=",
            BinaryOperator.Like => "LIKE",
            BinaryOperator.In => "IN",
            BinaryOperator.Between => "BETWEEN",
            BinaryOperator.Is => "IS",
            BinaryOperator.And => "AND",
            BinaryOperator.Or => "OR",
            _ => throw new NotSupportedException($"Binary operator {binary.Operator} is not supported")
        };
        
        return $"({left} {op} {right})";
    }

    private string GenerateUnaryExpression(UnaryExpression unary, EntityMetadata entityMetadata, string alias)
    {
        var operand = GenerateExpression(unary.Operand, entityMetadata, alias);
        
        var op = unary.Operator switch
        {
            UnaryOperator.Plus => "+",
            UnaryOperator.Minus => "-",
            UnaryOperator.Not => "NOT",
            _ => throw new NotSupportedException($"Unary operator {unary.Operator} is not supported")
        };
        
        return $"{op} {operand}";
    }

    private string GenerateFunctionExpression(FunctionExpression func, EntityMetadata entityMetadata, string alias)
    {
        var functionRegistry = new FunctionRegistry();
        var sqlFunctionName = functionRegistry.GetSqlFunction(func.FunctionName, _dialect);
        
        // Special handling for NOW() which doesn't take arguments in SQL
        if (sqlFunctionName.EndsWith("()"))
        {
            return sqlFunctionName;
        }
        
        var arguments = func.Arguments.Select(arg => GenerateExpression(arg, entityMetadata, alias));
        return $"{sqlFunctionName}({string.Join(", ", arguments)})";
    }

    private string GenerateAggregateExpression(AggregateExpression agg, EntityMetadata entityMetadata, string alias)
    {
        var functionName = agg.FunctionName.ToUpperInvariant();
        var argument = GenerateExpression(agg.Argument, entityMetadata, alias);
        var distinct = agg.IsDistinct ? "DISTINCT " : "";
        
        return $"{functionName}({distinct}{argument})";
    }

    private string GenerateWildcardExpression(WildcardExpression wildcard, string defaultAlias)
    {
        var alias = wildcard.EntityAlias ?? defaultAlias;
        return $"{alias}.*";
    }

    /// <inheritdoc />
    public string GenerateSelect(ParsedQuery parsedQuery, EntityMetadata entityMetadata)
    {
        var sql = new StringBuilder();
        
        // SELECT clause
        sql.Append("SELECT ");
        
        // Check if this is a COUNT query by looking at the original CPQL
        if (IsCountQuery(parsedQuery))
        {
            var primaryKeyColumn = entityMetadata.Properties[entityMetadata.PrimaryKeyProperty].ColumnName;
            sql.Append($"COUNT({parsedQuery.Alias}.{primaryKeyColumn})");
        }
        else
        {
            sql.Append(GenerateSelectColumns(entityMetadata, parsedQuery.Alias));
        }
        
        sql.Append(" FROM ");
        sql.Append(GetTableName(entityMetadata));
        sql.Append(" ");
        sql.Append(parsedQuery.Alias);

        // WHERE clause
        if (!string.IsNullOrEmpty(parsedQuery.WhereClause))
        {
            sql.Append(" WHERE ");
            sql.Append(GenerateWhereClause(parsedQuery.WhereClause, entityMetadata, parsedQuery.Alias));
        }

        // ORDER BY clause (only for non-COUNT queries)
        if (!string.IsNullOrEmpty(parsedQuery.OrderByClause) && !IsCountQuery(parsedQuery))
        {
            sql.Append(" ORDER BY ");
            sql.Append(GenerateOrderByClause(parsedQuery.OrderByClause, entityMetadata, parsedQuery.Alias));
        }

        return sql.ToString();
    }

    /// <inheritdoc />
    public string GenerateUpdate(ParsedQuery parsedQuery, EntityMetadata entityMetadata)
    {
        var sql = new StringBuilder();
        
        sql.Append("UPDATE ");
        sql.Append(GetTableName(entityMetadata));
        sql.Append(" SET ");
        
        var setClause = GenerateSetClause(parsedQuery.SetClause!, entityMetadata);
        sql.Append(setClause);

        if (!string.IsNullOrEmpty(parsedQuery.WhereClause))
        {
            sql.Append(" WHERE ");
            sql.Append(GenerateWhereClause(parsedQuery.WhereClause, entityMetadata));
        }

        return sql.ToString();
    }

    /// <inheritdoc />
    public string GenerateDelete(ParsedQuery parsedQuery, EntityMetadata entityMetadata)
    {
        var sql = new StringBuilder();
        
        sql.Append("DELETE FROM ");
        sql.Append(GetTableName(entityMetadata));

        if (!string.IsNullOrEmpty(parsedQuery.WhereClause))
        {
            sql.Append(" WHERE ");
            sql.Append(GenerateWhereClause(parsedQuery.WhereClause, entityMetadata));
        }

        return sql.ToString();
    }

    /// <inheritdoc />
    public string GenerateWhereClause(string? whereClause, EntityMetadata entityMetadata)
    {
        if (string.IsNullOrEmpty(whereClause))
            return string.Empty;

        return ResolvePropertyNames(whereClause, entityMetadata);
    }

    /// <inheritdoc />
    public string GenerateOrderByClause(string? orderByClause, EntityMetadata entityMetadata)
    {
        if (string.IsNullOrEmpty(orderByClause))
            return string.Empty;

        return ResolvePropertyNames(orderByClause, entityMetadata);
    }

    private string GenerateWhereClause(string? whereClause, EntityMetadata entityMetadata, string alias)
    {
        if (string.IsNullOrEmpty(whereClause))
            return string.Empty;

        return ResolvePropertyNames(whereClause, entityMetadata, alias);
    }

    private string GenerateOrderByClause(string? orderByClause, EntityMetadata entityMetadata, string alias)
    {
        if (string.IsNullOrEmpty(orderByClause))
            return string.Empty;

        return ResolvePropertyNames(orderByClause, entityMetadata, alias);
    }

    private string GenerateSelectColumns(EntityMetadata entityMetadata)
    {
        var columns = entityMetadata.Properties.Values
            .Select(p => $"{entityMetadata.TableName}.{p.ColumnName}")
            .ToList();

        return string.Join(", ", columns);
    }

    private string GenerateSelectColumns(EntityMetadata entityMetadata, string alias)
    {
        var columns = entityMetadata.Properties.Values
            .Select(p => $"{alias}.{p.ColumnName} AS {p.PropertyName}")
            .ToList();

        return string.Join(", ", columns);
    }

    private string GenerateSetClause(string setClause, EntityMetadata entityMetadata)
    {
        return ResolvePropertyNames(setClause, entityMetadata);
    }

    private string GetTableName(EntityMetadata entityMetadata)
    {
        return entityMetadata.FullTableName;
    }

    private string GetColumnName(EntityMetadata entityMetadata, string propertyName)
    {
        if (entityMetadata.Properties.TryGetValue(propertyName, out var propertyMetadata))
        {
            return propertyMetadata.ColumnName;
        }
        
        // If not found, return as-is (might be already a column name)
        return propertyName;
    }

    private string ResolvePropertyNames(string clause, EntityMetadata entityMetadata)
    {
        var resolvedClause = clause;

        // Replace alias.property names with column names (for UPDATE/DELETE queries)
        foreach (var property in entityMetadata.Properties.Values)
        {
            // Handle alias.property pattern (e.g., "u.Username" -> "username")
            var aliasPattern = $@"\b\w+\.{property.PropertyName}\b";
            resolvedClause = Regex.Replace(
                resolvedClause, 
                aliasPattern, 
                property.ColumnName, 
                RegexOptions.IgnoreCase);
        }

        // Convert CPQL parameter syntax (:paramName) to SQL parameter syntax (@paramName)
        resolvedClause = Regex.Replace(
            resolvedClause,
            @":(\w+)",
            "@$1",
            RegexOptions.Compiled);

        return resolvedClause;
    }

    private string ResolvePropertyNames(string clause, EntityMetadata entityMetadata, string alias)
    {
        var resolvedClause = clause;

        // Replace alias.property names with alias.column names
        foreach (var property in entityMetadata.Properties.Values)
        {
            var pattern = $@"\b{alias}\.{property.PropertyName}\b";
            resolvedClause = Regex.Replace(
                resolvedClause, 
                pattern, 
                $"{alias}.{property.ColumnName}", 
                RegexOptions.IgnoreCase);
        }

        // Convert CPQL parameter syntax (:paramName) to SQL parameter syntax (@paramName)
        resolvedClause = Regex.Replace(
            resolvedClause,
            @":(\w+)",
            "@$1",
            RegexOptions.Compiled);

        return resolvedClause;
    }

    private bool IsCountQuery(ParsedQuery parsedQuery)
    {
        return !string.IsNullOrEmpty(parsedQuery.OriginalCpql) && 
               parsedQuery.OriginalCpql.ToUpperInvariant().Contains("COUNT(");
    }
}
