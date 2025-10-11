using NPA.Core.Query.CPQL;
using NPA.Core.Query.CPQL.AST;

namespace NPA.Core.Query;

/// <summary>
/// Parses CPQL queries into structured representations with support for advanced features.
/// </summary>
public class QueryParser : IQueryParser
{
    private readonly CPQLParser _cpqlParser;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryParser"/> class.
    /// </summary>
    public QueryParser()
    {
        _cpqlParser = new CPQLParser();
    }

    /// <inheritdoc />
    public ParsedQuery Parse(string cpql)
    {
        if (string.IsNullOrWhiteSpace(cpql))
            throw new ArgumentException("CPQL query cannot be null or empty", nameof(cpql));

        try
        {
            // Use the enhanced CPQL parser
            var ast = _cpqlParser.Parse(cpql);
            
            // Convert AST to ParsedQuery for backward compatibility
            var parsedQuery = ConvertAstToParsedQuery(ast, cpql);
            parsedQuery.OriginalCpql = cpql;
            
            return parsedQuery;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid CPQL syntax: {ex.Message}", nameof(cpql), ex);
        }
    }

    private ParsedQuery ConvertAstToParsedQuery(QueryNode ast, string originalCpql)
    {
        return ast switch
        {
            SelectQuery selectQuery => ConvertSelectQuery(selectQuery, originalCpql),
            UpdateQuery updateQuery => ConvertUpdateQuery(updateQuery, originalCpql),
            DeleteQuery deleteQuery => ConvertDeleteQuery(deleteQuery, originalCpql),
            _ => throw new NotSupportedException($"Query type {ast.GetType().Name} is not supported")
        };
    }

    private ParsedQuery ConvertSelectQuery(SelectQuery selectQuery, string originalCpql)
    {
        var parsed = new ParsedQuery
        {
            Type = QueryType.Select,
            OriginalCpql = originalCpql
        };

        // Extract entity name and alias from FROM clause
        if (selectQuery.FromClause != null && selectQuery.FromClause.Items.Count > 0)
        {
            var firstFrom = selectQuery.FromClause.Items[0];
            parsed.EntityName = firstFrom.EntityName;
            parsed.Alias = firstFrom.Alias ?? firstFrom.EntityName;
        }

        // Store WHERE clause (simplified for now)
        if (selectQuery.WhereClause != null)
        {
            parsed.WhereClause = selectQuery.WhereClause.Condition.ToString();
        }

        // Store ORDER BY clause
        if (selectQuery.OrderByClause != null && selectQuery.OrderByClause.Items.Count > 0)
        {
            var orderByParts = selectQuery.OrderByClause.Items.Select(item =>
                $"{item.Expression} {(item.Direction == OrderDirection.Descending ? "DESC" : "ASC")}");
            parsed.OrderByClause = string.Join(", ", orderByParts);
        }

        // Store JOINs
        if (selectQuery.FromClause?.Joins != null)
        {
            parsed.Joins = selectQuery.FromClause.Joins.Select(j => new JoinClause
            {
                Type = j.JoinType.ToString().ToUpper(),
                EntityName = j.EntityName,
                Alias = j.Alias ?? j.EntityName,
                Condition = j.OnCondition?.ToString() ?? string.Empty
            }).ToList();
        }

        // Store the full AST for advanced SQL generation
        parsed.Ast = selectQuery;

        // Extract parameter names
        ExtractParametersFromAst(parsed, selectQuery);

        return parsed;
    }

    private ParsedQuery ConvertUpdateQuery(UpdateQuery updateQuery, string originalCpql)
    {
        var parsed = new ParsedQuery
        {
            Type = QueryType.Update,
            EntityName = updateQuery.EntityName,
            Alias = updateQuery.Alias,
            OriginalCpql = originalCpql
        };

        // Store SET clause
        if (updateQuery.Assignments.Count > 0)
        {
            var assignments = updateQuery.Assignments.Select(a => $"{a.PropertyName} = {a.Value}");
            parsed.SetClause = string.Join(", ", assignments);
        }

        // Store WHERE clause
        if (updateQuery.WhereClause != null)
        {
            parsed.WhereClause = updateQuery.WhereClause.Condition.ToString();
        }

        // Store the full AST for advanced SQL generation
        parsed.Ast = updateQuery;

        // Extract parameter names
        ExtractParametersFromAst(parsed, updateQuery);

        return parsed;
    }

    private ParsedQuery ConvertDeleteQuery(DeleteQuery deleteQuery, string originalCpql)
    {
        var parsed = new ParsedQuery
        {
            Type = QueryType.Delete,
            EntityName = deleteQuery.EntityName,
            Alias = deleteQuery.Alias,
            OriginalCpql = originalCpql
        };

        // Store WHERE clause
        if (deleteQuery.WhereClause != null)
        {
            parsed.WhereClause = deleteQuery.WhereClause.Condition.ToString();
        }

        // Store the full AST for advanced SQL generation
        parsed.Ast = deleteQuery;

        // Extract parameter names
        ExtractParametersFromAst(parsed, deleteQuery);

        return parsed;
    }

    private void ExtractParametersFromAst(ParsedQuery parsed, QueryNode ast)
    {
        var parameters = new HashSet<string>();
        ExtractParametersRecursive(ast, parameters);
        parsed.ParameterNames = parameters.ToList();
    }

    private static void ExtractParametersRecursive(object? node, HashSet<string> parameters)
    {
        if (node == null) return;

        switch (node)
        {
            case ParameterExpression param:
                parameters.Add(param.ParameterName);
                break;
            
            case BinaryExpression binary:
                ExtractParametersRecursive(binary.Left, parameters);
                ExtractParametersRecursive(binary.Right, parameters);
                break;
            
            case UnaryExpression unary:
                ExtractParametersRecursive(unary.Operand, parameters);
                break;
            
            case FunctionExpression func:
                foreach (var arg in func.Arguments)
                    ExtractParametersRecursive(arg, parameters);
                break;
            
            case AggregateExpression agg:
                ExtractParametersRecursive(agg.Argument, parameters);
                break;
            
            case SelectQuery select:
                if (select.WhereClause != null)
                    ExtractParametersRecursive(select.WhereClause, parameters);
                if (select.HavingClause != null)
                    ExtractParametersRecursive(select.HavingClause, parameters);
                if (select.SelectClause != null)
                {
                    foreach (var item in select.SelectClause.Items)
                        ExtractParametersRecursive(item.Expression, parameters);
                }
                if (select.OrderByClause != null)
                {
                    foreach (var item in select.OrderByClause.Items)
                        ExtractParametersRecursive(item.Expression, parameters);
                }
                break;
            
            case UpdateQuery update:
                if (update.WhereClause != null)
                    ExtractParametersRecursive(update.WhereClause, parameters);
                foreach (var assignment in update.Assignments)
                    ExtractParametersRecursive(assignment.Value, parameters);
                break;
            
            case DeleteQuery delete:
                if (delete.WhereClause != null)
                    ExtractParametersRecursive(delete.WhereClause, parameters);
                break;
            
            case WhereClause where:
                ExtractParametersRecursive(where.Condition, parameters);
                break;
            
            case HavingClause having:
                ExtractParametersRecursive(having.Condition, parameters);
                break;
        }
    }
}
