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

    private string GenerateFromAst(object ast, ParsedQuery parsedQuery, EntityMetadata primaryEntityMetadata)
    {
        return ast switch
        {
            SelectQuery selectQuery => GenerateSelectFromAst(selectQuery, parsedQuery, primaryEntityMetadata),
            UpdateQuery updateQuery => GenerateUpdateFromAst(updateQuery, parsedQuery, primaryEntityMetadata),
            DeleteQuery deleteQuery => GenerateDeleteFromAst(deleteQuery, parsedQuery, primaryEntityMetadata),
            _ => throw new NotSupportedException($"AST type {ast.GetType().Name} is not supported")
        };
    }

    private string GenerateSelectFromAst(SelectQuery query, ParsedQuery parsedQuery, EntityMetadata primaryEntityMetadata)
    {
        var sql = new StringBuilder();
        var aliasMetadataMap = new Dictionary<string, EntityMetadata>();

        // 1. Build the alias-to-metadata map
        if (query.FromClause != null && query.FromClause.Items.Count > 0)
        {
            var primaryAlias = query.FromClause.Items[0].Alias ?? query.FromClause.Items[0].EntityName;
            aliasMetadataMap[primaryAlias] = primaryEntityMetadata;

            foreach (var join in query.FromClause.Joins)
            {
                var joinAlias = join.Alias ?? join.EntityName;
                if (primaryEntityMetadata.Relationships.TryGetValue(join.EntityName, out var relMetadata))
                {
                    if (_metadataProvider == null) throw new InvalidOperationException("MetadataProvider is required for relationship joins.");
                    var targetMetadata = _metadataProvider.GetEntityMetadata(relMetadata.TargetEntityType);
                    aliasMetadataMap[joinAlias] = targetMetadata;
                }
                else
                {
                    throw new InvalidOperationException($"Could not find relationship property '{join.EntityName}' on entity '{primaryEntityMetadata.EntityType.Name}'.");
                }
            }
        }

        // SELECT clause
        sql.Append("SELECT ");
        if (query.SelectClause?.IsDistinct == true)
        {
            sql.Append("DISTINCT ");
        }

        if (query.SelectClause == null || query.SelectClause.Items.Count == 0)
        {
            // Default: SELECT all columns from all known aliases
            var allColumns = aliasMetadataMap.Select(kvp => GenerateSelectColumns(kvp.Value, kvp.Key));
            sql.Append(string.Join(", ", allColumns));
        }
        else
        {
            var selectItems = query.SelectClause.Items.Select(item =>
                GenerateSelectItem(item, primaryEntityMetadata, parsedQuery.Alias, aliasMetadataMap));
            sql.Append(string.Join(", ", selectItems));
        }
        
        // FROM clause
        if (query.FromClause != null && query.FromClause.Items.Count > 0)
        {
            sql.Append(" FROM ");
            var fromItems = query.FromClause.Items.Select(item =>
            {
                var tableName = GetTableName(primaryEntityMetadata);
                var alias = item.Alias ?? item.EntityName;
                return $"{tableName} AS {alias}";
            });
            sql.Append(string.Join(", ", fromItems));
            
            // JOIN clauses
            if (query.FromClause.Joins.Count > 0)
            {
                foreach (var join in query.FromClause.Joins)
                {
                    sql.Append(GenerateJoinClause(join, primaryEntityMetadata, parsedQuery.Alias));
                }
            }
        }
        
        // WHERE clause
        if (query.WhereClause != null)
        {
            sql.Append(" WHERE ");
            sql.Append(GenerateExpression(query.WhereClause.Condition, primaryEntityMetadata, parsedQuery.Alias, aliasMetadataMap));
        }
        
        // GROUP BY clause
        if (query.GroupByClause != null && query.GroupByClause.Items.Count > 0)
        {
            sql.Append(" GROUP BY ");
            var groupByItems = query.GroupByClause.Items.Select(expr => 
                GenerateExpression(expr, primaryEntityMetadata, parsedQuery.Alias, aliasMetadataMap));
            sql.Append(string.Join(", ", groupByItems));
        }
        
        // HAVING clause
        if (query.HavingClause != null)
        {
            sql.Append(" HAVING ");
            sql.Append(GenerateExpression(query.HavingClause.Condition, primaryEntityMetadata, parsedQuery.Alias, aliasMetadataMap));
        }
        
        // ORDER BY clause
        if (query.OrderByClause != null && query.OrderByClause.Items.Count > 0)
        {
            sql.Append(" ORDER BY ");
            var orderByItems = query.OrderByClause.Items.Select(item =>
            {
                var expr = GenerateExpression(item.Expression, primaryEntityMetadata, parsedQuery.Alias, aliasMetadataMap);
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
            // For UPDATE SET clause, don't use alias - just column name
            var value = GenerateExpressionWithoutAlias(assignment.Value, entityMetadata);
            return $"{columnName} = {value}";
        });
        sql.Append(string.Join(", ", assignments));
        
        if (query.WhereClause != null)
        {
            sql.Append(" WHERE ");
            // For UPDATE WHERE clause, don't use table alias (PostgreSQL compatibility)
            sql.Append(GenerateExpressionWithoutAlias(query.WhereClause.Condition, entityMetadata));
        }
        
        return sql.ToString();
    }
    
    private string GenerateExpressionWithoutAlias(Expression expression, EntityMetadata entityMetadata)
    {
        return expression switch
        {
            PropertyExpression prop => GetColumnName(entityMetadata, prop.PropertyName),
            LiteralExpression literal => GenerateLiteralExpression(literal),
            ParameterExpression param => GenerateParameterExpression(param),
            BinaryExpression binary => GenerateBinaryExpressionWithoutAlias(binary, entityMetadata),
            UnaryExpression unary => GenerateUnaryExpressionWithoutAlias(unary, entityMetadata),
            FunctionExpression func => GenerateFunctionExpressionWithoutAlias(func, entityMetadata),
            AggregateExpression agg => throw new NotSupportedException("Aggregate functions in UPDATE are not supported"),
            _ => throw new NotSupportedException($"Expression type {expression.GetType().Name} is not supported in UPDATE")
        };
    }
    
    private string GenerateBinaryExpressionWithoutAlias(BinaryExpression binary, EntityMetadata entityMetadata)
    {
        var left = GenerateExpressionWithoutAlias(binary.Left, entityMetadata);
        var right = GenerateExpressionWithoutAlias(binary.Right, entityMetadata);
        
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
            BinaryOperator.And => "AND",
            BinaryOperator.Or => "OR",
            _ => throw new NotSupportedException($"Binary operator {binary.Operator} is not supported in UPDATE")
        };
        
        return $"({left} {op} {right})";
    }
    
    private string GenerateUnaryExpressionWithoutAlias(UnaryExpression unary, EntityMetadata entityMetadata)
    {
        var operand = GenerateExpressionWithoutAlias(unary.Operand, entityMetadata);
        
        var op = unary.Operator switch
        {
            UnaryOperator.Plus => "+",
            UnaryOperator.Minus => "-",
            UnaryOperator.Not => "NOT",
            _ => throw new NotSupportedException($"Unary operator {unary.Operator} is not supported in UPDATE")
        };
        
        return $"{op} {operand}";
    }
    
    private string GenerateFunctionExpressionWithoutAlias(FunctionExpression func, EntityMetadata entityMetadata)
    {
        var functionRegistry = new FunctionRegistry();
        var sqlFunctionName = functionRegistry.GetSqlFunction(func.FunctionName, _dialect);
        
        if (sqlFunctionName.EndsWith("()"))
        {
            return sqlFunctionName;
        }
        
        var arguments = func.Arguments.Select(arg => GenerateExpressionWithoutAlias(arg, entityMetadata));
        return $"{sqlFunctionName}({string.Join(", ", arguments)})";
    }

    private string GenerateDeleteFromAst(DeleteQuery query, ParsedQuery parsedQuery, EntityMetadata entityMetadata)
    {
        var sql = new StringBuilder();
        
        sql.Append("DELETE FROM ");
        sql.Append(GetTableName(entityMetadata));
        
        if (query.WhereClause != null)
        {
            sql.Append(" WHERE ");
            sql.Append(GenerateExpression(query.WhereClause.Condition, entityMetadata, query.Alias, new Dictionary<string, EntityMetadata>()));
        }
        
        return sql.ToString();
    }

    private string GenerateSelectItem(SelectItem item, EntityMetadata primaryEntityMetadata, string primaryAlias, IReadOnlyDictionary<string, EntityMetadata> aliasMap)
    {
        if (item.Expression is PropertyExpression prop && prop.EntityAlias == null && aliasMap.ContainsKey(prop.PropertyName))
        {
            // This is "SELECT u" or "SELECT o", meaning "select all columns for this alias"
            var alias = prop.PropertyName;
            var entityMetadata = aliasMap[alias];
            return GenerateSelectColumns(entityMetadata, alias);
        }
        
        var expression = GenerateExpression(item.Expression, primaryEntityMetadata, primaryAlias, aliasMap);
        
        if (!string.IsNullOrEmpty(item.Alias))
        {
            return $"{expression} AS {item.Alias}";
        }
        
        return expression;
    }

    private string GenerateJoinClause(CPQL.AST.JoinClause join, EntityMetadata sourceEntityMetadata, string primaryAlias)
    {
        var sql = new StringBuilder();

        var relationshipPropertyName = join.EntityName;

        if (!sourceEntityMetadata.Relationships.TryGetValue(relationshipPropertyName, out var relMetadata))
        {
            throw new InvalidOperationException($"Could not find relationship property '{relationshipPropertyName}' on entity '{sourceEntityMetadata.EntityType.Name}'.");
        }

        if (_metadataProvider == null)
        {
            throw new InvalidOperationException("MetadataProvider is required for relationship joins.");
        }
        var targetEntityMetadata = _metadataProvider.GetEntityMetadata(relMetadata.TargetEntityType);
        var joinAlias = join.Alias ?? join.EntityName;

        var joinTypeString = join.JoinType switch
        {
            CPQL.AST.JoinType.Inner => " INNER JOIN ",
            CPQL.AST.JoinType.Left => " LEFT JOIN ",
            CPQL.AST.JoinType.Right => " RIGHT JOIN ",
            CPQL.AST.JoinType.Full => " FULL OUTER JOIN ",
            _ => throw new NotSupportedException($"Join type {join.JoinType} is not supported")
        };

        if (relMetadata.RelationshipType == RelationshipType.ManyToMany)
        {
            if (relMetadata.JoinTable == null)
                throw new InvalidOperationException($"JoinTable metadata is missing for ManyToMany relationship '{relMetadata.PropertyName}' on entity '{sourceEntityMetadata.EntityType.Name}'.");

            var joinTableName = relMetadata.JoinTable.Name;
            var joinTableAlias = $"{primaryAlias}_{joinAlias}_jt"; // A unique alias for the join table

            var sourceKeyColumn = sourceEntityMetadata.Properties[sourceEntityMetadata.PrimaryKeyProperty].ColumnName;
            var targetKeyColumn = targetEntityMetadata.Properties[targetEntityMetadata.PrimaryKeyProperty].ColumnName;

            var sourceJoinColumn = relMetadata.JoinTable.JoinColumns.FirstOrDefault() ?? sourceEntityMetadata.TableName + "_id";
            var inverseJoinColumn = relMetadata.JoinTable.InverseJoinColumns.FirstOrDefault() ?? targetEntityMetadata.TableName + "_id";

            // First join: Source Table -> Join Table
            sql.Append(joinTypeString);
            sql.Append($"{joinTableName} AS {joinTableAlias}");
            sql.Append($" ON {primaryAlias}.{sourceKeyColumn} = {joinTableAlias}.{sourceJoinColumn}");

            // Second join: Join Table -> Target Table
            sql.Append(joinTypeString);
            sql.Append($"{GetTableName(targetEntityMetadata)} AS {joinAlias}");
            sql.Append($" ON {joinTableAlias}.{inverseJoinColumn} = {joinAlias}.{targetKeyColumn}");
        }
        else
        {
            sql.Append(joinTypeString);
            sql.Append($"{GetTableName(targetEntityMetadata)} AS {joinAlias}");

            if (join.OnCondition != null)
            {
                sql.Append(" ON ");
                sql.Append(GenerateExpression(join.OnCondition, sourceEntityMetadata, primaryAlias, new Dictionary<string, EntityMetadata>()));
            }
            else
            {
                string sourceColumn, targetColumn;
                if (relMetadata.RelationshipType == RelationshipType.ManyToOne || (relMetadata.RelationshipType == RelationshipType.OneToOne && relMetadata.IsOwner))
                {
                    sourceColumn = $"{primaryAlias}.{relMetadata.JoinColumn!.Name}";
                    targetColumn = $"{joinAlias}.{targetEntityMetadata.Properties[targetEntityMetadata.PrimaryKeyProperty].ColumnName}";
                }
                else // OneToMany or inverse OneToOne
                {
                    if (string.IsNullOrEmpty(relMetadata.MappedBy))
                    {
                        throw new InvalidOperationException($"The [{relMetadata.RelationshipType}] relationship '{relMetadata.PropertyName}' on '{sourceEntityMetadata.EntityType.Name}' must have the 'MappedBy' property set to define the bidirectional relationship.");
                    }

                    var inverseProperty = targetEntityMetadata.Relationships.Values
                        .FirstOrDefault(r => r.PropertyName == relMetadata.MappedBy);

                    if (inverseProperty == null || inverseProperty.JoinColumn == null)
                    {
                        throw new InvalidOperationException($"Cannot infer join condition for {relMetadata.RelationshipType} relationship '{relMetadata.PropertyName}'. The target entity '{targetEntityMetadata.EntityType.Name}' must have a corresponding relationship named '{relMetadata.MappedBy}' with a [JoinColumn].");
                    }

                    sourceColumn = $"{primaryAlias}.{sourceEntityMetadata.Properties[sourceEntityMetadata.PrimaryKeyProperty].ColumnName}";
                    targetColumn = $"{joinAlias}.{inverseProperty.JoinColumn.Name}";
                }
                sql.Append($" ON {sourceColumn} = {targetColumn}");
            }
        }

        return sql.ToString();
    }

    private string GenerateExpression(Expression expression, EntityMetadata primaryEntityMetadata, string primaryAlias, IReadOnlyDictionary<string, EntityMetadata> aliasMap)
    {
        return expression switch
        {
            PropertyExpression prop => GeneratePropertyExpression(prop, primaryEntityMetadata, primaryAlias, aliasMap),
            LiteralExpression literal => GenerateLiteralExpression(literal),
            ParameterExpression param => GenerateParameterExpression(param),
            BinaryExpression binary => GenerateBinaryExpression(binary, primaryEntityMetadata, primaryAlias, aliasMap),
            UnaryExpression unary => GenerateUnaryExpression(unary, primaryEntityMetadata, primaryAlias, aliasMap),
            FunctionExpression func => GenerateFunctionExpression(func, primaryEntityMetadata, primaryAlias, aliasMap),
            AggregateExpression agg => GenerateAggregateExpression(agg, primaryEntityMetadata, primaryAlias, aliasMap),
            WildcardExpression wildcard => GenerateWildcardExpression(wildcard, primaryAlias),
            _ => throw new NotSupportedException($"Expression type {expression.GetType().Name} is not supported")
        };
    }

    private string GeneratePropertyExpression(PropertyExpression prop, EntityMetadata primaryEntityMetadata, string primaryAlias, IReadOnlyDictionary<string, EntityMetadata> aliasMap)
    {
        var entityAlias = prop.EntityAlias ?? primaryAlias;
        
        if (!aliasMap.TryGetValue(entityAlias, out var entityMetadata))
        {
            entityMetadata = primaryEntityMetadata;
        }

        var columnName = GetColumnName(entityMetadata, prop.PropertyName);
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

    private string GenerateBinaryExpression(BinaryExpression binary, EntityMetadata primaryEntityMetadata, string primaryAlias, IReadOnlyDictionary<string, EntityMetadata> aliasMap)
    {
        var left = GenerateExpression(binary.Left, primaryEntityMetadata, primaryAlias, aliasMap);
        var right = GenerateExpression(binary.Right, primaryEntityMetadata, primaryAlias, aliasMap);
        
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

    private string GenerateUnaryExpression(UnaryExpression unary, EntityMetadata primaryEntityMetadata, string primaryAlias, IReadOnlyDictionary<string, EntityMetadata> aliasMap)
    {
        var operand = GenerateExpression(unary.Operand, primaryEntityMetadata, primaryAlias, aliasMap);
        
        var op = unary.Operator switch
        {
            UnaryOperator.Plus => "+",
            UnaryOperator.Minus => "-",
            UnaryOperator.Not => "NOT",
            _ => throw new NotSupportedException($"Unary operator {unary.Operator} is not supported")
        };
        
        return $"{op} {operand}";
    }

    private string GenerateFunctionExpression(FunctionExpression func, EntityMetadata primaryEntityMetadata, string primaryAlias, IReadOnlyDictionary<string, EntityMetadata> aliasMap)
    {
        var functionRegistry = new FunctionRegistry();
        var sqlFunctionName = functionRegistry.GetSqlFunction(func.FunctionName, _dialect);
        
        // Special handling for NOW() which doesn't take arguments in SQL
        if (sqlFunctionName.EndsWith("()"))
        {
            return sqlFunctionName;
        }
        
        var arguments = func.Arguments.Select(arg => GenerateExpression(arg, primaryEntityMetadata, primaryAlias, aliasMap));
        return $"{sqlFunctionName}({string.Join(", ", arguments)})";
    }

    private string GenerateAggregateExpression(AggregateExpression agg, EntityMetadata primaryEntityMetadata, string primaryAlias, IReadOnlyDictionary<string, EntityMetadata> aliasMap)
    {
        var functionName = agg.FunctionName.ToUpperInvariant();
        var distinct = agg.IsDistinct ? "DISTINCT " : "";
        
        if (agg.Argument is PropertyExpression prop && aliasMap.ContainsKey(prop.PropertyName))
        {
            var alias = prop.PropertyName;
            var entityMetadata = aliasMap[alias];
            var primaryKeyColumn = entityMetadata.Properties[entityMetadata.PrimaryKeyProperty].ColumnName;
            return $"{functionName}({distinct}{alias}.{primaryKeyColumn})";
        }
        
        var argument = GenerateExpression(agg.Argument, primaryEntityMetadata, primaryAlias, aliasMap);
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
            .Select(p => $"{alias}.{p.ColumnName} AS {EscapeIdentifier(p.PropertyName)}")
            .ToList();

        return string.Join(", ", columns);
    }
    
    private string EscapeIdentifier(string identifier)
    {
        // For different database dialects:
        // - SQL Server: Use brackets or no quotes for simple identifiers
        // - PostgreSQL: Use double quotes to preserve case sensitivity (SQL standard)
        // - SQLite: Use double quotes (SQL standard)
        // - MySQL/MariaDB: Use backticks (MySQL-specific syntax)
        return _dialect.ToLowerInvariant() switch
        {
            "mysql" => $"`{identifier.Replace("`", "``")}`",
            "mariadb" => $"`{identifier.Replace("`", "``")}`",
            "sqlserver" => identifier, // SQL Server doesn't require quotes for simple identifiers
            "postgresql" => $"\"{identifier.Replace("\"", "\"\"")}\"", // PostgreSQL needs quotes for case sensitivity
            "sqlite" => $"\"{identifier.Replace("\"", "\"\"")}\"", // SQLite uses double quotes (SQL standard)
            _ => identifier // Default: no quotes for simple identifiers
        };
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
