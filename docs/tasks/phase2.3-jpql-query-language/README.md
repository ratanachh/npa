# Phase 2.3: JPQL-like Query Language

## 📋 Task Overview

**Objective**: Implement a JPQL-like query language that provides object-oriented querying capabilities while generating efficient SQL using Dapper.

**Priority**: High  
**Estimated Time**: 4-5 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.2 (Relationship Mapping, Composite Key Support)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] JPQLParser class is complete
- [ ] SqlGenerator class is implemented
- [ ] Query language supports all basic operations
- [ ] SQL generation is optimized
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

## 📝 Detailed Requirements

### 1. JPQL Parser
- **Purpose**: Parse JPQL-like queries into abstract syntax tree
- **Features**:
  - SELECT clause parsing
  - FROM clause parsing with entity resolution
  - WHERE clause parsing with expressions
  - ORDER BY clause parsing
  - GROUP BY clause parsing
  - HAVING clause parsing
  - JOIN clause parsing
  - Subquery support

### 2. SQL Generator
- **Purpose**: Convert parsed JPQL to database-specific SQL
- **Features**:
  - Entity name to table name mapping
  - Property name to column name mapping
  - JOIN generation
  - WHERE clause generation
  - ORDER BY clause generation
  - GROUP BY clause generation
  - HAVING clause generation
  - Parameter placeholder generation

### 3. Query Language Features
- **Basic Queries**: SELECT, FROM, WHERE
- **Joins**: INNER JOIN, LEFT JOIN, RIGHT JOIN, FULL OUTER JOIN
- **Aggregates**: COUNT, SUM, AVG, MIN, MAX
- **Grouping**: GROUP BY, HAVING
- **Sorting**: ORDER BY with multiple columns
- **Functions**: String, date, numeric functions
- **Subqueries**: Nested queries
- **Parameters**: Named and positional parameters

### 4. Entity Resolution
- **Entity Name Mapping**: Map entity names to table names
- **Property Mapping**: Map property names to column names
- **Relationship Navigation**: Navigate entity relationships
- **Alias Support**: Support for entity aliases

### 5. Performance Optimization
- **Query Optimization**: Optimize generated SQL
- **Index Hints**: Suggest index usage
- **Join Optimization**: Optimize join operations
- **Parameter Binding**: Efficient parameter binding

## 🏗️ Implementation Plan

### Step 1: Create Parser Infrastructure
1. Create `JPQLParser` class
2. Create `QueryAST` classes
3. Create `Lexer` class
4. Create `Parser` class

### Step 2: Implement Query Parsing
1. Implement SELECT clause parsing
2. Implement FROM clause parsing
3. Implement WHERE clause parsing
4. Implement ORDER BY clause parsing
5. Implement GROUP BY clause parsing

### Step 3: Implement SQL Generation
1. Create `SqlGenerator` class
2. Implement entity resolution
3. Implement property mapping
4. Implement JOIN generation
5. Implement WHERE clause generation

### Step 4: Add Advanced Features
1. Implement subquery support
2. Implement function support
3. Implement parameter support
4. Implement alias support

### Step 5: Add Performance Optimization
1. Implement query optimization
2. Add index hints
3. Optimize join operations
4. Optimize parameter binding

### Step 6: Create Unit Tests
1. Test parser functionality
2. Test SQL generation
3. Test entity resolution
4. Test performance optimization

### Step 7: Add Documentation
1. XML documentation comments
2. Usage examples
3. JPQL syntax guide
4. Best practices

## 📁 File Structure

```
src/NPA.Core/Query/
├── JPQL/
│   ├── JPQLParser.cs
│   ├── QueryAST.cs
│   ├── Lexer.cs
│   ├── Parser.cs
│   ├── EntityResolver.cs
│   └── FunctionRegistry.cs
├── SqlGeneration/
│   ├── SqlGenerator.cs
│   ├── EntityMapper.cs
│   ├── JoinGenerator.cs
│   ├── WhereClauseGenerator.cs
│   └── OrderByGenerator.cs
└── Optimization/
    ├── QueryOptimizer.cs
    ├── IndexHintGenerator.cs
    └── JoinOptimizer.cs

tests/NPA.Core.Tests/Query/
├── JPQL/
│   ├── JPQLParserTests.cs
│   ├── LexerTests.cs
│   ├── ParserTests.cs
│   └── EntityResolverTests.cs
├── SqlGeneration/
│   ├── SqlGeneratorTests.cs
│   ├── EntityMapperTests.cs
│   ├── JoinGeneratorTests.cs
│   └── WhereClauseGeneratorTests.cs
└── Optimization/
    ├── QueryOptimizerTests.cs
    ├── IndexHintGeneratorTests.cs
    └── JoinOptimizerTests.cs
```

## 💻 Code Examples

### JPQL Parser
```csharp
public class JPQLParser
{
    private readonly IEntityResolver _entityResolver;
    private readonly IFunctionRegistry _functionRegistry;
    
    public JPQLParser(IEntityResolver entityResolver, IFunctionRegistry functionRegistry)
    {
        _entityResolver = entityResolver ?? throw new ArgumentNullException(nameof(entityResolver));
        _functionRegistry = functionRegistry ?? throw new ArgumentNullException(nameof(functionRegistry));
    }
    
    public QueryAST Parse(string jpql)
    {
        if (string.IsNullOrEmpty(jpql))
            throw new ArgumentException("JPQL cannot be null or empty", nameof(jpql));
        
        var lexer = new Lexer(jpql);
        var parser = new Parser(lexer, _entityResolver, _functionRegistry);
        return parser.Parse();
    }
}

public class Parser
{
    private readonly Lexer _lexer;
    private readonly IEntityResolver _entityResolver;
    private readonly IFunctionRegistry _functionRegistry;
    private Token _currentToken;
    
    public Parser(Lexer lexer, IEntityResolver entityResolver, IFunctionRegistry functionRegistry)
    {
        _lexer = lexer ?? throw new ArgumentNullException(nameof(lexer));
        _entityResolver = entityResolver ?? throw new ArgumentNullException(nameof(entityResolver));
        _functionRegistry = functionRegistry ?? throw new ArgumentNullException(nameof(functionRegistry));
        _currentToken = _lexer.NextToken();
    }
    
    public QueryAST Parse()
    {
        var query = new SelectQuery();
        
        // Parse SELECT clause
        if (_currentToken.Type == TokenType.Select)
        {
            query.SelectClause = ParseSelectClause();
        }
        
        // Parse FROM clause
        if (_currentToken.Type == TokenType.From)
        {
            query.FromClause = ParseFromClause();
        }
        
        // Parse WHERE clause
        if (_currentToken.Type == TokenType.Where)
        {
            query.WhereClause = ParseWhereClause();
        }
        
        // Parse ORDER BY clause
        if (_currentToken.Type == TokenType.OrderBy)
        {
            query.OrderByClause = ParseOrderByClause();
        }
        
        // Parse GROUP BY clause
        if (_currentToken.Type == TokenType.GroupBy)
        {
            query.GroupByClause = ParseGroupByClause();
        }
        
        // Parse HAVING clause
        if (_currentToken.Type == TokenType.Having)
        {
            query.HavingClause = ParseHavingClause();
        }
        
        return query;
    }
    
    private SelectClause ParseSelectClause()
    {
        Consume(TokenType.Select);
        
        var selectClause = new SelectClause();
        
        if (_currentToken.Type == TokenType.Distinct)
        {
            selectClause.IsDistinct = true;
            Consume(TokenType.Distinct);
        }
        
        do
        {
            var selectItem = ParseSelectItem();
            selectClause.Items.Add(selectItem);
            
            if (_currentToken.Type == TokenType.Comma)
            {
                Consume(TokenType.Comma);
            }
            else
            {
                break;
            }
        } while (true);
        
        return selectClause;
    }
    
    private FromClause ParseFromClause()
    {
        Consume(TokenType.From);
        
        var fromClause = new FromClause();
        
        do
        {
            var fromItem = ParseFromItem();
            fromClause.Items.Add(fromItem);
            
            if (_currentToken.Type == TokenType.Comma)
            {
                Consume(TokenType.Comma);
            }
            else
            {
                break;
            }
        } while (true);
        
        return fromClause;
    }
    
    private WhereClause ParseWhereClause()
    {
        Consume(TokenType.Where);
        
        var whereClause = new WhereClause();
        whereClause.Condition = ParseExpression();
        
        return whereClause;
    }
    
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
        
        if (_currentToken.Type == TokenType.Equal)
        {
            Consume(TokenType.Equal);
            var right = ParseRelationalExpression();
            return new BinaryExpression(left, BinaryOperator.Equal, right);
        }
        else if (_currentToken.Type == TokenType.NotEqual)
        {
            Consume(TokenType.NotEqual);
            var right = ParseRelationalExpression();
            return new BinaryExpression(left, BinaryOperator.NotEqual, right);
        }
        
        return left;
    }
    
    private Expression ParseRelationalExpression()
    {
        var left = ParseAdditiveExpression();
        
        if (_currentToken.Type == TokenType.LessThan)
        {
            Consume(TokenType.LessThan);
            var right = ParseAdditiveExpression();
            return new BinaryExpression(left, BinaryOperator.LessThan, right);
        }
        else if (_currentToken.Type == TokenType.LessThanOrEqual)
        {
            Consume(TokenType.LessThanOrEqual);
            var right = ParseAdditiveExpression();
            return new BinaryExpression(left, BinaryOperator.LessThanOrEqual, right);
        }
        else if (_currentToken.Type == TokenType.GreaterThan)
        {
            Consume(TokenType.GreaterThan);
            var right = ParseAdditiveExpression();
            return new BinaryExpression(left, BinaryOperator.GreaterThan, right);
        }
        else if (_currentToken.Type == TokenType.GreaterThanOrEqual)
        {
            Consume(TokenType.GreaterThanOrEqual);
            var right = ParseAdditiveExpression();
            return new BinaryExpression(left, BinaryOperator.GreaterThanOrEqual, right);
        }
        
        return left;
    }
    
    private Expression ParseAdditiveExpression()
    {
        var left = ParseMultiplicativeExpression();
        
        while (_currentToken.Type == TokenType.Plus || _currentToken.Type == TokenType.Minus)
        {
            var operator = _currentToken.Type == TokenType.Plus ? BinaryOperator.Add : BinaryOperator.Subtract;
            Consume(_currentToken.Type);
            var right = ParseMultiplicativeExpression();
            left = new BinaryExpression(left, operator, right);
        }
        
        return left;
    }
    
    private Expression ParseMultiplicativeExpression()
    {
        var left = ParseUnaryExpression();
        
        while (_currentToken.Type == TokenType.Multiply || _currentToken.Type == TokenType.Divide || _currentToken.Type == TokenType.Modulo)
        {
            var operator = _currentToken.Type switch
            {
                TokenType.Multiply => BinaryOperator.Multiply,
                TokenType.Divide => BinaryOperator.Divide,
                TokenType.Modulo => BinaryOperator.Modulo,
                _ => throw new InvalidOperationException()
            };
            Consume(_currentToken.Type);
            var right = ParseUnaryExpression();
            left = new BinaryExpression(left, operator, right);
        }
        
        return left;
    }
    
    private Expression ParseUnaryExpression()
    {
        if (_currentToken.Type == TokenType.Plus)
        {
            Consume(TokenType.Plus);
            return new UnaryExpression(UnaryOperator.Plus, ParsePrimaryExpression());
        }
        else if (_currentToken.Type == TokenType.Minus)
        {
            Consume(TokenType.Minus);
            return new UnaryExpression(UnaryOperator.Minus, ParsePrimaryExpression());
        }
        else if (_currentToken.Type == TokenType.Not)
        {
            Consume(TokenType.Not);
            return new UnaryExpression(UnaryOperator.Not, ParsePrimaryExpression());
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
                return ParseStringLiteral();
            case TokenType.NumberLiteral:
                return ParseNumberLiteral();
            case TokenType.LeftParenthesis:
                return ParseParenthesizedExpression();
            case TokenType.Parameter:
                return ParseParameterExpression();
            default:
                throw new InvalidOperationException($"Unexpected token: {_currentToken.Type}");
        }
    }
    
    private void Consume(TokenType expectedType)
    {
        if (_currentToken.Type != expectedType)
            throw new InvalidOperationException($"Expected {expectedType}, got {_currentToken.Type}");
        
        _currentToken = _lexer.NextToken();
    }
}
```

### SQL Generator
```csharp
public class SqlGenerator
{
    private readonly IEntityMapper _entityMapper;
    private readonly IJoinGenerator _joinGenerator;
    private readonly IWhereClauseGenerator _whereClauseGenerator;
    private readonly IOrderByGenerator _orderByGenerator;
    
    public SqlGenerator(
        IEntityMapper entityMapper,
        IJoinGenerator joinGenerator,
        IWhereClauseGenerator whereClauseGenerator,
        IOrderByGenerator orderByGenerator)
    {
        _entityMapper = entityMapper ?? throw new ArgumentNullException(nameof(entityMapper));
        _joinGenerator = joinGenerator ?? throw new ArgumentNullException(nameof(joinGenerator));
        _whereClauseGenerator = whereClauseGenerator ?? throw new ArgumentNullException(nameof(whereClauseGenerator));
        _orderByGenerator = orderByGenerator ?? throw new ArgumentNullException(nameof(orderByGenerator));
    }
    
    public string Generate(QueryAST query)
    {
        var sql = new StringBuilder();
        
        // Generate SELECT clause
        sql.AppendLine(GenerateSelectClause(query.SelectClause));
        
        // Generate FROM clause
        sql.AppendLine(GenerateFromClause(query.FromClause));
        
        // Generate JOIN clauses
        if (query.FromClause?.Items?.Any() == true)
        {
            var joins = _joinGenerator.GenerateJoins(query.FromClause.Items);
            if (!string.IsNullOrEmpty(joins))
            {
                sql.AppendLine(joins);
            }
        }
        
        // Generate WHERE clause
        if (query.WhereClause != null)
        {
            var whereClause = _whereClauseGenerator.Generate(query.WhereClause);
            if (!string.IsNullOrEmpty(whereClause))
            {
                sql.AppendLine($"WHERE {whereClause}");
            }
        }
        
        // Generate GROUP BY clause
        if (query.GroupByClause != null)
        {
            sql.AppendLine(GenerateGroupByClause(query.GroupByClause));
        }
        
        // Generate HAVING clause
        if (query.HavingClause != null)
        {
            var havingClause = _whereClauseGenerator.Generate(query.HavingClause);
            if (!string.IsNullOrEmpty(havingClause))
            {
                sql.AppendLine($"HAVING {havingClause}");
            }
        }
        
        // Generate ORDER BY clause
        if (query.OrderByClause != null)
        {
            var orderByClause = _orderByGenerator.Generate(query.OrderByClause);
            if (!string.IsNullOrEmpty(orderByClause))
            {
                sql.AppendLine($"ORDER BY {orderByClause}");
            }
        }
        
        return sql.ToString().Trim();
    }
    
    private string GenerateSelectClause(SelectClause selectClause)
    {
        if (selectClause == null || !selectClause.Items.Any())
            return "SELECT *";
        
        var items = selectClause.Items.Select(GenerateSelectItem);
        var distinct = selectClause.IsDistinct ? "DISTINCT " : "";
        
        return $"SELECT {distinct}{string.Join(", ", items)}";
    }
    
    private string GenerateSelectItem(SelectItem selectItem)
    {
        if (selectItem.Expression is PropertyExpression propertyExpression)
        {
            var columnName = _entityMapper.GetColumnName(propertyExpression.EntityAlias, propertyExpression.PropertyName);
            var alias = !string.IsNullOrEmpty(selectItem.Alias) ? $" AS {selectItem.Alias}" : "";
            return $"{columnName}{alias}";
        }
        else if (selectItem.Expression is FunctionExpression functionExpression)
        {
            return GenerateFunctionExpression(functionExpression);
        }
        else if (selectItem.Expression is AggregateExpression aggregateExpression)
        {
            return GenerateAggregateExpression(aggregateExpression);
        }
        
        return selectItem.Expression.ToString();
    }
    
    private string GenerateFromClause(FromClause fromClause)
    {
        if (fromClause == null || !fromClause.Items.Any())
            throw new InvalidOperationException("FROM clause is required");
        
        var items = fromClause.Items.Select(GenerateFromItem);
        return $"FROM {string.Join(", ", items)}";
    }
    
    private string GenerateFromItem(FromItem fromItem)
    {
        var tableName = _entityMapper.GetTableName(fromItem.EntityName);
        var alias = !string.IsNullOrEmpty(fromItem.Alias) ? $" AS {fromItem.Alias}" : "";
        return $"{tableName}{alias}";
    }
    
    private string GenerateGroupByClause(GroupByClause groupByClause)
    {
        if (groupByClause == null || !groupByClause.Items.Any())
            return "";
        
        var items = groupByClause.Items.Select(item => _entityMapper.GetColumnName(item.EntityAlias, item.PropertyName));
        return $"GROUP BY {string.Join(", ", items)}";
    }
    
    private string GenerateFunctionExpression(FunctionExpression functionExpression)
    {
        var functionName = functionExpression.FunctionName.ToUpper();
        var arguments = functionExpression.Arguments.Select(GenerateExpression);
        
        return functionName switch
        {
            "COUNT" => $"COUNT({string.Join(", ", arguments)})",
            "SUM" => $"SUM({string.Join(", ", arguments)})",
            "AVG" => $"AVG({string.Join(", ", arguments)})",
            "MIN" => $"MIN({string.Join(", ", arguments)})",
            "MAX" => $"MAX({string.Join(", ", arguments)})",
            "UPPER" => $"UPPER({string.Join(", ", arguments)})",
            "LOWER" => $"LOWER({string.Join(", ", arguments)})",
            "LENGTH" => $"LEN({string.Join(", ", arguments)})",
            "SUBSTRING" => $"SUBSTRING({string.Join(", ", arguments)})",
            _ => throw new NotSupportedException($"Function {functionName} is not supported")
        };
    }
    
    private string GenerateAggregateExpression(AggregateExpression aggregateExpression)
    {
        var functionName = aggregateExpression.FunctionName.ToUpper();
        var argument = GenerateExpression(aggregateExpression.Argument);
        var distinct = aggregateExpression.IsDistinct ? "DISTINCT " : "";
        
        return $"{functionName}({distinct}{argument})";
    }
    
    private string GenerateExpression(Expression expression)
    {
        return expression switch
        {
            PropertyExpression prop => _entityMapper.GetColumnName(prop.EntityAlias, prop.PropertyName),
            LiteralExpression literal => GenerateLiteral(literal),
            ParameterExpression param => $"@{param.ParameterName}",
            BinaryExpression binary => GenerateBinaryExpression(binary),
            UnaryExpression unary => GenerateUnaryExpression(unary),
            FunctionExpression func => GenerateFunctionExpression(func),
            _ => expression.ToString()
        };
    }
    
    private string GenerateLiteral(LiteralExpression literal)
    {
        return literal.Value switch
        {
            string str => $"'{str.Replace("'", "''")}'",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            _ => literal.Value?.ToString() ?? "NULL"
        };
    }
    
    private string GenerateBinaryExpression(BinaryExpression binary)
    {
        var left = GenerateExpression(binary.Left);
        var right = GenerateExpression(binary.Right);
        var operator = binary.Operator switch
        {
            BinaryOperator.Equal => "=",
            BinaryOperator.NotEqual => "<>",
            BinaryOperator.LessThan => "<",
            BinaryOperator.LessThanOrEqual => "<=",
            BinaryOperator.GreaterThan => ">",
            BinaryOperator.GreaterThanOrEqual => ">=",
            BinaryOperator.And => "AND",
            BinaryOperator.Or => "OR",
            BinaryOperator.Add => "+",
            BinaryOperator.Subtract => "-",
            BinaryOperator.Multiply => "*",
            BinaryOperator.Divide => "/",
            BinaryOperator.Modulo => "%",
            _ => throw new NotSupportedException($"Operator {binary.Operator} is not supported")
        };
        
        return $"({left} {operator} {right})";
    }
    
    private string GenerateUnaryExpression(UnaryExpression unary)
    {
        var operand = GenerateExpression(unary.Operand);
        var operator = unary.Operator switch
        {
            UnaryOperator.Plus => "+",
            UnaryOperator.Minus => "-",
            UnaryOperator.Not => "NOT",
            _ => throw new NotSupportedException($"Operator {unary.Operator} is not supported")
        };
        
        return $"{operator} {operand}";
    }
}
```

### Usage Examples
```csharp
// Basic queries
var users = await entityManager
    .CreateQuery<User>("SELECT u FROM User u")
    .GetResultListAsync();

var activeUsers = await entityManager
    .CreateQuery<User>("SELECT u FROM User u WHERE u.IsActive = :active")
    .SetParameter("active", true)
    .GetResultListAsync();

// Joins
var ordersWithUsers = await entityManager
    .CreateQuery<Order>("SELECT o FROM Order o JOIN o.User u WHERE u.Username = :username")
    .SetParameter("username", "john")
    .GetResultListAsync();

// Aggregates
var userCount = await entityManager
    .CreateQuery<int>("SELECT COUNT(u) FROM User u WHERE u.IsActive = :active")
    .SetParameter("active", true)
    .GetSingleResultAsync();

// Group by
var ordersByUser = await entityManager
    .CreateQuery<object>("SELECT u.Username, COUNT(o) FROM User u LEFT JOIN u.Orders o GROUP BY u.Username")
    .GetResultListAsync();

// Order by
var usersByCreatedDate = await entityManager
    .CreateQuery<User>("SELECT u FROM User u ORDER BY u.CreatedAt DESC")
    .GetResultListAsync();

// Functions
var upperCaseNames = await entityManager
    .CreateQuery<string>("SELECT UPPER(u.Username) FROM User u")
    .GetResultListAsync();

// Subqueries
var usersWithOrders = await entityManager
    .CreateQuery<User>("SELECT u FROM User u WHERE u.Id IN (SELECT o.User.Id FROM Order o)")
    .GetResultListAsync();
```

## 🧪 Test Cases

### Parser Tests
- [ ] Basic SELECT queries
- [ ] FROM clause parsing
- [ ] WHERE clause parsing
- [ ] ORDER BY clause parsing
- [ ] GROUP BY clause parsing
- [ ] JOIN clause parsing
- [ ] Subquery parsing
- [ ] Function parsing

### SQL Generation Tests
- [ ] Entity name mapping
- [ ] Property name mapping
- [ ] JOIN generation
- [ ] WHERE clause generation
- [ ] ORDER BY generation
- [ ] GROUP BY generation
- [ ] Function generation
- [ ] Parameter binding

### Integration Tests
- [ ] End-to-end query execution
- [ ] Performance testing
- [ ] Error handling
- [ ] Complex queries

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Basic JPQL syntax
- [ ] Query examples
- [ ] Function reference
- [ ] Performance considerations
- [ ] Best practices

### JPQL Syntax Guide
- [ ] SELECT clause
- [ ] FROM clause
- [ ] WHERE clause
- [ ] ORDER BY clause
- [ ] GROUP BY clause
- [ ] HAVING clause
- [ ] JOIN operations
- [ ] Functions
- [ ] Subqueries

## 🔍 Code Review Checklist

- [ ] Code follows .NET naming conventions
- [ ] All public members have XML documentation
- [ ] Error handling is appropriate
- [ ] Unit tests cover all scenarios
- [ ] Code is readable and maintainable
- [ ] Performance is optimized
- [ ] Memory usage is efficient
- [ ] Thread safety considerations

## 🚀 Next Steps

After completing this task:
1. Move to Phase 2.4: Repository Pattern Implementation
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on JPQL syntax
- [ ] Performance considerations for parsing
- [ ] Integration with existing features
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
