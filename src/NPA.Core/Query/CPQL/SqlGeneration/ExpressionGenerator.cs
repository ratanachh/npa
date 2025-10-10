using System.Text;
using NPA.Core.Query.CPQL.AST;

namespace NPA.Core.Query.CPQL.SqlGeneration;

/// <summary>
/// Generates SQL from CPQL expressions.
/// </summary>
public sealed class ExpressionGenerator : IExpressionGenerator
{
    private readonly IEntityMapper _entityMapper;
    private readonly IFunctionRegistry _functionRegistry;
    private readonly string _dialect;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionGenerator"/> class.
    /// </summary>
    /// <param name="entityMapper">The entity mapper.</param>
    /// <param name="functionRegistry">The function registry.</param>
    /// <param name="dialect">The database dialect.</param>
    public ExpressionGenerator(IEntityMapper entityMapper, IFunctionRegistry functionRegistry, string dialect = "default")
    {
        _entityMapper = entityMapper ?? throw new ArgumentNullException(nameof(entityMapper));
        _functionRegistry = functionRegistry ?? throw new ArgumentNullException(nameof(functionRegistry));
        _dialect = dialect;
    }
    
    /// <inheritdoc />
    public string Generate(Expression expression)
    {
        return expression switch
        {
            PropertyExpression prop => GeneratePropertyExpression(prop),
            LiteralExpression literal => GenerateLiteralExpression(literal),
            ParameterExpression param => GenerateParameterExpression(param),
            BinaryExpression binary => GenerateBinaryExpression(binary),
            UnaryExpression unary => GenerateUnaryExpression(unary),
            FunctionExpression func => GenerateFunctionExpression(func),
            AggregateExpression agg => GenerateAggregateExpression(agg),
            WildcardExpression wildcard => GenerateWildcardExpression(wildcard),
            SubqueryExpression subquery => throw new NotSupportedException("Subqueries are not yet supported"),
            _ => throw new NotSupportedException($"Expression type {expression.GetType().Name} is not supported")
        };
    }
    
    private string GeneratePropertyExpression(PropertyExpression prop)
    {
        return _entityMapper.GetColumnName(prop.EntityAlias, prop.PropertyName);
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
    
    private string GenerateBinaryExpression(BinaryExpression binary)
    {
        var left = Generate(binary.Left);
        var right = Generate(binary.Right);
        
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
    
    private string GenerateUnaryExpression(UnaryExpression unary)
    {
        var operand = Generate(unary.Operand);
        
        var op = unary.Operator switch
        {
            UnaryOperator.Plus => "+",
            UnaryOperator.Minus => "-",
            UnaryOperator.Not => "NOT",
            _ => throw new NotSupportedException($"Unary operator {unary.Operator} is not supported")
        };
        
        return $"{op} {operand}";
    }
    
    private string GenerateFunctionExpression(FunctionExpression func)
    {
        var sqlFunctionName = _functionRegistry.GetSqlFunction(func.FunctionName, _dialect);
        
        // Special handling for NOW() which doesn't take arguments in SQL
        if (sqlFunctionName.EndsWith("()"))
        {
            return sqlFunctionName;
        }
        
        var arguments = func.Arguments.Select(Generate);
        return $"{sqlFunctionName}({string.Join(", ", arguments)})";
    }
    
    private string GenerateAggregateExpression(AggregateExpression agg)
    {
        var functionName = agg.FunctionName.ToUpperInvariant();
        var argument = Generate(agg.Argument);
        var distinct = agg.IsDistinct ? "DISTINCT " : "";
        
        return $"{functionName}({distinct}{argument})";
    }
    
    private string GenerateWildcardExpression(WildcardExpression wildcard)
    {
        return wildcard.EntityAlias != null ? $"{wildcard.EntityAlias}.*" : "*";
    }
}

