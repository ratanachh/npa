using NPA.Core.Query.CPQL.AST;

namespace NPA.Core.Query.CPQL;

/// <summary>
/// Recursive descent parser for CPQL.
/// </summary>
public sealed class Parser
{
    private readonly Lexer _lexer;
    private Token _currentToken;
    private readonly List<string> _errors = new();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Parser"/> class.
    /// </summary>
    /// <param name="lexer">The lexer.</param>
    public Parser(Lexer lexer)
    {
        _lexer = lexer ?? throw new ArgumentNullException(nameof(lexer));
        _currentToken = _lexer.NextToken();
    }
    
    /// <summary>
    /// Parses the CPQL query.
    /// </summary>
    /// <returns>The parsed query node.</returns>
    public QueryNode Parse()
    {
        try
        {
            return ParseQuery();
        }
        catch (Exception ex)
        {
            _errors.Add(ex.Message);
            throw new InvalidOperationException($"Parse error: {string.Join("; ", _errors)}", ex);
        }
    }
    
    private QueryNode ParseQuery()
    {
        return _currentToken.Type switch
        {
            TokenType.Select => ParseSelectQuery(),
            TokenType.Update => ParseUpdateQuery(),
            TokenType.Delete => ParseDeleteQuery(),
            _ => throw new InvalidOperationException($"Unexpected token: {_currentToken.Type}")
        };
    }
    
    private SelectQuery ParseSelectQuery()
    {
        var query = new SelectQuery
        {
            SelectClause = ParseSelectClause()
        };
        
        if (_currentToken.Type == TokenType.From)
        {
            query.FromClause = ParseFromClause();
        }
        
        if (_currentToken.Type == TokenType.Where)
        {
            query.WhereClause = ParseWhereClause();
        }
        
        if (_currentToken.Type == TokenType.GroupBy)
        {
            query.GroupByClause = ParseGroupByClause();
        }
        
        if (_currentToken.Type == TokenType.Having)
        {
            query.HavingClause = ParseHavingClause();
        }
        
        if (_currentToken.Type == TokenType.OrderBy)
        {
            query.OrderByClause = ParseOrderByClause();
        }
        
        return query;
    }
    
    private UpdateQuery ParseUpdateQuery()
    {
        Consume(TokenType.Update);
        
        var entityName = ConsumeIdentifier();
        var alias = ConsumeIdentifier();
        
        Consume(TokenType.Set);
        
        var assignments = new List<SetAssignment>();
        do
        {
            var propertyName = ConsumeIdentifier();
            Consume(TokenType.Equal);
            var value = ParseExpression();
            
            assignments.Add(new SetAssignment
            {
                PropertyName = propertyName,
                Value = value
            });
            
            if (_currentToken.Type == TokenType.Comma)
            {
                Consume(TokenType.Comma);
            }
            else
            {
                break;
            }
        } while (true);
        
        var query = new UpdateQuery
        {
            EntityName = entityName,
            Alias = alias,
            Assignments = assignments
        };
        
        if (_currentToken.Type == TokenType.Where)
        {
            query.WhereClause = ParseWhereClause();
        }
        
        return query;
    }
    
    private DeleteQuery ParseDeleteQuery()
    {
        Consume(TokenType.Delete);
        Consume(TokenType.From);
        
        var entityName = ConsumeIdentifier();
        var alias = ConsumeIdentifier();
        
        var query = new DeleteQuery
        {
            EntityName = entityName,
            Alias = alias
        };
        
        if (_currentToken.Type == TokenType.Where)
        {
            query.WhereClause = ParseWhereClause();
        }
        
        return query;
    }
    
    private SelectClause ParseSelectClause()
    {
        Consume(TokenType.Select);
        
        var clause = new SelectClause();
        
        if (_currentToken.Type == TokenType.Distinct)
        {
            clause.IsDistinct = true;
            Consume(TokenType.Distinct);
        }
        
        do
        {
            clause.Items.Add(ParseSelectItem());
            
            if (_currentToken.Type == TokenType.Comma)
            {
                Consume(TokenType.Comma);
            }
            else
            {
                break;
            }
        } while (true);
        
        return clause;
    }
    
    private SelectItem ParseSelectItem()
    {
        var item = new SelectItem
        {
            Expression = ParseExpression()
        };
        
        if (_currentToken.Type == TokenType.As)
        {
            Consume(TokenType.As);
            item.Alias = ConsumeIdentifier();
        }
        
        return item;
    }
    
    private FromClause ParseFromClause()
    {
        Consume(TokenType.From);
        
        var clause = new FromClause();
        
        do
        {
            clause.Items.Add(ParseFromItem());
            
            if (_currentToken.Type == TokenType.Comma)
            {
                Consume(TokenType.Comma);
            }
            else
            {
                break;
            }
        } while (true);
        
        // Parse JOIN clauses
        while (IsJoinToken(_currentToken.Type))
        {
            clause.Joins.Add(ParseJoinClause());
        }
        
        return clause;
    }
    
    private FromItem ParseFromItem()
    {
        var entityName = ConsumeIdentifier();
        
        string? alias = null;
        if (_currentToken.Type == TokenType.As)
        {
            Consume(TokenType.As);
            alias = ConsumeIdentifier();
        }
        else if (_currentToken.Type == TokenType.Identifier)
        {
            alias = ConsumeIdentifier();
        }
        
        return new FromItem
        {
            EntityName = entityName,
            Alias = alias
        };
    }
    
    private AST.JoinClause ParseJoinClause()
    {
        var joinType = ParseJoinType();
        
        var entityName = ConsumeIdentifier();
        
        string? alias = null;
        if (_currentToken.Type == TokenType.As)
        {
            Consume(TokenType.As);
            alias = ConsumeIdentifier();
        }
        else if (_currentToken.Type == TokenType.Identifier)
        {
            alias = ConsumeIdentifier();
        }
        
        Expression? onCondition = null;
        if (_currentToken.Type == TokenType.On)
        {
            Consume(TokenType.On);
            onCondition = ParseExpression();
        }
        
        return new AST.JoinClause
        {
            JoinType = joinType,
            EntityName = entityName,
            Alias = alias,
            OnCondition = onCondition
        };
    }
    
    private AST.JoinType ParseJoinType()
    {
        return _currentToken.Type switch
        {
            TokenType.Join => ConsumeAndReturn(TokenType.Join, JoinType.Inner),
            TokenType.InnerJoin => ConsumeInnerJoinAndReturn(),
            TokenType.LeftJoin => ConsumeLeftJoinAndReturn(),
            TokenType.RightJoin => ConsumeRightJoinAndReturn(),
            TokenType.FullJoin => ConsumeFullJoinAndReturn(),
            _ => throw new InvalidOperationException($"Expected JOIN keyword, got {_currentToken.Type}")
        };
    }
    
    private JoinType ConsumeInnerJoinAndReturn()
    {
        Consume(TokenType.InnerJoin);
        if (_currentToken.Type == TokenType.Join)
            Consume(TokenType.Join);
        return JoinType.Inner;
    }
    
    private JoinType ConsumeLeftJoinAndReturn()
    {
        Consume(TokenType.LeftJoin);
        if (_currentToken.Type == TokenType.Join)
            Consume(TokenType.Join);
        return JoinType.Left;
    }
    
    private JoinType ConsumeRightJoinAndReturn()
    {
        Consume(TokenType.RightJoin);
        if (_currentToken.Type == TokenType.Join)
            Consume(TokenType.Join);
        return JoinType.Right;
    }
    
    private JoinType ConsumeFullJoinAndReturn()
    {
        Consume(TokenType.FullJoin);
        if (_currentToken.Type == TokenType.Join)
            Consume(TokenType.Join);
        return JoinType.Full;
    }
    
    private T ConsumeAndReturn<T>(TokenType type, T value)
    {
        Consume(type);
        return value;
    }
    
    private WhereClause ParseWhereClause()
    {
        Consume(TokenType.Where);
        
        return new WhereClause
        {
            Condition = ParseExpression()
        };
    }
    
    private GroupByClause ParseGroupByClause()
    {
        Consume(TokenType.GroupBy);
        if (_currentToken.Type == TokenType.OrderBy) // Skip "BY" if it's there
            Consume(TokenType.OrderBy);
        
        var clause = new GroupByClause();
        
        do
        {
            clause.Items.Add(ParseExpression());
            
            if (_currentToken.Type == TokenType.Comma)
            {
                Consume(TokenType.Comma);
            }
            else
            {
                break;
            }
        } while (true);
        
        return clause;
    }
    
    private HavingClause ParseHavingClause()
    {
        Consume(TokenType.Having);
        
        return new HavingClause
        {
            Condition = ParseExpression()
        };
    }
    
    private OrderByClause ParseOrderByClause()
    {
        Consume(TokenType.OrderBy);
        if (_currentToken.Type == TokenType.OrderBy) // Skip "BY" if it's there
            Consume(TokenType.OrderBy);
        
        var clause = new OrderByClause();
        
        do
        {
            var expression = ParseExpression();
            var direction = OrderDirection.Ascending;
            
            if (_currentToken.Type == TokenType.Asc)
            {
                Consume(TokenType.Asc);
                direction = OrderDirection.Ascending;
            }
            else if (_currentToken.Type == TokenType.Desc)
            {
                Consume(TokenType.Desc);
                direction = OrderDirection.Descending;
            }
            
            clause.Items.Add(new OrderByItem
            {
                Expression = expression,
                Direction = direction
            });
            
            if (_currentToken.Type == TokenType.Comma)
            {
                Consume(TokenType.Comma);
            }
            else
            {
                break;
            }
        } while (true);
        
        return clause;
    }
    
    #region Expression Parsing
    
    private Expression ParseExpression()
    {
        return ParseOrExpression();
    }
    
    private Expression ParseOrExpression()
    {
        var left = ParseAndExpression();
        
        while (_currentToken.Type == TokenType.Or)
        {
            Consume(TokenType.Or);
            var right = ParseAndExpression();
            left = new BinaryExpression(left, BinaryOperator.Or, right);
        }
        
        return left;
    }
    
    private Expression ParseAndExpression()
    {
        var left = ParseEqualityExpression();
        
        while (_currentToken.Type == TokenType.And)
        {
            Consume(TokenType.And);
            var right = ParseEqualityExpression();
            left = new BinaryExpression(left, BinaryOperator.And, right);
        }
        
        return left;
    }
    
    private Expression ParseEqualityExpression()
    {
        var left = ParseRelationalExpression();
        
        while (_currentToken.Type is TokenType.Equal or TokenType.NotEqual or TokenType.Like or TokenType.In or TokenType.Is)
        {
            var op = _currentToken.Type switch
            {
                TokenType.Equal => BinaryOperator.Equal,
                TokenType.NotEqual => BinaryOperator.NotEqual,
                TokenType.Like => BinaryOperator.Like,
                TokenType.In => BinaryOperator.In,
                TokenType.Is => BinaryOperator.Is,
                _ => throw new InvalidOperationException()
            };
            
            Consume(_currentToken.Type);
            var right = ParseRelationalExpression();
            left = new BinaryExpression(left, op, right);
        }
        
        return left;
    }
    
    private Expression ParseRelationalExpression()
    {
        var left = ParseAdditiveExpression();
        
        while (_currentToken.Type is TokenType.LessThan or TokenType.LessThanOrEqual or 
               TokenType.GreaterThan or TokenType.GreaterThanOrEqual)
        {
            var op = _currentToken.Type switch
            {
                TokenType.LessThan => BinaryOperator.LessThan,
                TokenType.LessThanOrEqual => BinaryOperator.LessThanOrEqual,
                TokenType.GreaterThan => BinaryOperator.GreaterThan,
                TokenType.GreaterThanOrEqual => BinaryOperator.GreaterThanOrEqual,
                _ => throw new InvalidOperationException()
            };
            
            Consume(_currentToken.Type);
            var right = ParseAdditiveExpression();
            left = new BinaryExpression(left, op, right);
        }
        
        return left;
    }
    
    private Expression ParseAdditiveExpression()
    {
        var left = ParseMultiplicativeExpression();
        
        while (_currentToken.Type is TokenType.Plus or TokenType.Minus)
        {
            var op = _currentToken.Type == TokenType.Plus ? BinaryOperator.Add : BinaryOperator.Subtract;
            Consume(_currentToken.Type);
            var right = ParseMultiplicativeExpression();
            left = new BinaryExpression(left, op, right);
        }
        
        return left;
    }
    
    private Expression ParseMultiplicativeExpression()
    {
        var left = ParseUnaryExpression();
        
        while (_currentToken.Type is TokenType.Multiply or TokenType.Divide or TokenType.Modulo)
        {
            var op = _currentToken.Type switch
            {
                TokenType.Multiply => BinaryOperator.Multiply,
                TokenType.Divide => BinaryOperator.Divide,
                TokenType.Modulo => BinaryOperator.Modulo,
                _ => throw new InvalidOperationException()
            };
            
            Consume(_currentToken.Type);
            var right = ParseUnaryExpression();
            left = new BinaryExpression(left, op, right);
        }
        
        return left;
    }
    
    private Expression ParseUnaryExpression()
    {
        if (_currentToken.Type is TokenType.Plus or TokenType.Minus or TokenType.Not)
        {
            var op = _currentToken.Type switch
            {
                TokenType.Plus => UnaryOperator.Plus,
                TokenType.Minus => UnaryOperator.Minus,
                TokenType.Not => UnaryOperator.Not,
                _ => throw new InvalidOperationException()
            };
            
            Consume(_currentToken.Type);
            return new UnaryExpression(op, ParsePrimaryExpression());
        }
        
        return ParsePrimaryExpression();
    }
    
    private Expression ParsePrimaryExpression()
    {
        switch (_currentToken.Type)
        {
            case TokenType.Identifier:
                return ParseIdentifierExpression();
            
            case TokenType.StringLiteral:
            case TokenType.NumberLiteral:
            case TokenType.BooleanLiteral:
            case TokenType.Null:
                return ParseLiteralExpression();
            
            case TokenType.Parameter:
                return ParseParameterExpression();
            
            case TokenType.LeftParenthesis:
                return ParseParenthesizedExpression();
            
            case TokenType.Multiply:
                // This could be a wildcard (*)
                Consume(TokenType.Multiply);
                return new WildcardExpression();
            
            // Aggregate functions
            case TokenType.Count:
            case TokenType.Sum:
            case TokenType.Avg:
            case TokenType.Min:
            case TokenType.Max:
                return ParseAggregateExpression();
            
            // String functions
            case TokenType.Upper:
            case TokenType.Lower:
            case TokenType.Length:
            case TokenType.Substring:
            case TokenType.Trim:
            case TokenType.Concat:
            // Date functions
            case TokenType.Year:
            case TokenType.Month:
            case TokenType.Day:
            case TokenType.Hour:
            case TokenType.Minute:
            case TokenType.Second:
            case TokenType.Now:
                return ParseFunctionExpression();
            
            default:
                throw new InvalidOperationException($"Unexpected token: {_currentToken.Type}");
        }
    }
    
    private Expression ParseIdentifierExpression()
    {
        var identifier = ConsumeIdentifier();
        
        // Check if it's a qualified property (alias.property)
        if (_currentToken.Type == TokenType.Dot)
        {
            Consume(TokenType.Dot);
            
            // Check for wildcard (alias.*)
            if (_currentToken.Type == TokenType.Multiply)
            {
                Consume(TokenType.Multiply);
                return new WildcardExpression { EntityAlias = identifier };
            }
            
            var propertyName = ConsumeIdentifier();
            return new PropertyExpression
            {
                EntityAlias = identifier,
                PropertyName = propertyName
            };
        }
        
        // Check if it's a function call
        if (_currentToken.Type == TokenType.LeftParenthesis)
        {
            return ParseFunctionCall(identifier);
        }
        
        // It's just a simple property reference
        return new PropertyExpression
        {
            PropertyName = identifier
        };
    }
    
    private Expression ParseLiteralExpression()
    {
        var value = _currentToken.Literal;
        Consume(_currentToken.Type);
        
        return new LiteralExpression { Value = value };
    }
    
    private Expression ParseParameterExpression()
    {
        var paramName = (string)_currentToken.Literal!;
        Consume(TokenType.Parameter);
        
        return new ParameterExpression { ParameterName = paramName };
    }
    
    private Expression ParseParenthesizedExpression()
    {
        Consume(TokenType.LeftParenthesis);
        
        // Check if it's a subquery
        if (_currentToken.Type == TokenType.Select)
        {
            var subquery = ParseSelectQuery();
            Consume(TokenType.RightParenthesis);
            return new SubqueryExpression { Query = subquery };
        }
        
        var expression = ParseExpression();
        Consume(TokenType.RightParenthesis);
        
        return expression;
    }
    
    private Expression ParseAggregateExpression()
    {
        var functionName = _currentToken.Lexeme;
        Consume(_currentToken.Type);
        
        Consume(TokenType.LeftParenthesis);
        
        var isDistinct = false;
        if (_currentToken.Type == TokenType.Distinct)
        {
            isDistinct = true;
            Consume(TokenType.Distinct);
        }
        
        var argument = ParseExpression();
        
        Consume(TokenType.RightParenthesis);
        
        return new AggregateExpression
        {
            FunctionName = functionName,
            Argument = argument,
            IsDistinct = isDistinct
        };
    }
    
    private Expression ParseFunctionExpression()
    {
        var functionName = _currentToken.Lexeme;
        Consume(_currentToken.Type);
        
        Consume(TokenType.LeftParenthesis);
        
        var arguments = new List<Expression>();
        
        if (_currentToken.Type != TokenType.RightParenthesis)
        {
            do
            {
                arguments.Add(ParseExpression());
                
                if (_currentToken.Type == TokenType.Comma)
                {
                    Consume(TokenType.Comma);
                }
                else
                {
                    break;
                }
            } while (true);
        }
        
        Consume(TokenType.RightParenthesis);
        
        return new FunctionExpression
        {
            FunctionName = functionName,
            Arguments = arguments
        };
    }
    
    private Expression ParseFunctionCall(string functionName)
    {
        Consume(TokenType.LeftParenthesis);
        
        var arguments = new List<Expression>();
        
        if (_currentToken.Type != TokenType.RightParenthesis)
        {
            do
            {
                arguments.Add(ParseExpression());
                
                if (_currentToken.Type == TokenType.Comma)
                {
                    Consume(TokenType.Comma);
                }
                else
                {
                    break;
                }
            } while (true);
        }
        
        Consume(TokenType.RightParenthesis);
        
        return new FunctionExpression
        {
            FunctionName = functionName,
            Arguments = arguments
        };
    }
    
    #endregion
    
    #region Helper Methods
    
    private void Consume(TokenType expectedType)
    {
        if (_currentToken.Type != expectedType)
        {
            throw new InvalidOperationException(
                $"Expected {expectedType}, got {_currentToken.Type} ('{_currentToken.Lexeme}') at position {_currentToken.Position}");
        }
        
        _currentToken = _lexer.NextToken();
    }
    
    private string ConsumeIdentifier()
    {
        if (_currentToken.Type != TokenType.Identifier)
        {
            throw new InvalidOperationException(
                $"Expected identifier, got {_currentToken.Type} ('{_currentToken.Lexeme}') at position {_currentToken.Position}");
        }
        
        var identifier = _currentToken.Lexeme;
        _currentToken = _lexer.NextToken();
        return identifier;
    }
    
    private bool IsJoinToken(TokenType type)
    {
        return type is TokenType.Join or TokenType.InnerJoin or TokenType.LeftJoin or 
               TokenType.RightJoin or TokenType.FullJoin;
    }
    
    #endregion
}

