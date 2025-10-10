using System.Linq.Expressions;
using System.Text;
using NPA.Core.Metadata;

namespace NPA.Core.Repositories;

/// <summary>
/// Translates LINQ expressions to SQL WHERE clauses.
/// </summary>
public class ExpressionTranslator
{
    private readonly EntityMetadata _metadata;
    private readonly Dictionary<string, object> _parameters;
    private int _parameterIndex;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionTranslator"/> class.
    /// </summary>
    /// <param name="metadata">The entity metadata.</param>
    /// <param name="parameters">The parameter dictionary to populate.</param>
    public ExpressionTranslator(EntityMetadata metadata, Dictionary<string, object> parameters)
    {
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        _parameterIndex = 0;
    }
    
    /// <summary>
    /// Translates an expression to SQL.
    /// </summary>
    /// <param name="expression">The expression to translate.</param>
    /// <returns>The SQL string.</returns>
    public string Translate(Expression expression)
    {
        return expression switch
        {
            BinaryExpression binary => TranslateBinary(binary),
            MemberExpression member => TranslateMember(member),
            ConstantExpression constant => TranslateConstant(constant),
            MethodCallExpression method => TranslateMethodCall(method),
            UnaryExpression unary => TranslateUnary(unary),
            _ => throw new NotSupportedException($"Expression type {expression.NodeType} is not supported")
        };
    }
    
    private string TranslateBinary(BinaryExpression expression)
    {
        var left = Translate(expression.Left);
        var right = Translate(expression.Right);
        
        var op = expression.NodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            _ => throw new NotSupportedException($"Binary operator {expression.NodeType} is not supported")
        };
        
        return $"({left} {op} {right})";
    }
    
    private string TranslateMember(MemberExpression expression)
    {
        var propertyName = expression.Member.Name;
        var propertyMetadata = _metadata.Properties.Values
            .FirstOrDefault(p => p.PropertyName == propertyName);
        
        if (propertyMetadata == null)
            throw new InvalidOperationException($"Property {propertyName} not found in entity metadata");
        
        return propertyMetadata.ColumnName;
    }
    
    private string TranslateConstant(ConstantExpression expression)
    {
        var paramName = $"p{_parameterIndex++}";
        _parameters[paramName] = expression.Value ?? DBNull.Value;
        return $"@{paramName}";
    }
    
    private string TranslateMethodCall(MethodCallExpression expression)
    {
        // Support common string methods
        if (expression.Method.Name == "Contains")
        {
            if (expression.Object is MemberExpression member)
            {
                var columnName = TranslateMember(member);
                var value = GetExpressionValue(expression.Arguments[0]);
                var paramName = $"p{_parameterIndex++}";
                _parameters[paramName] = $"%{value}%";
                return $"{columnName} LIKE @{paramName}";
            }
        }
        else if (expression.Method.Name == "StartsWith")
        {
            if (expression.Object is MemberExpression member)
            {
                var columnName = TranslateMember(member);
                var value = GetExpressionValue(expression.Arguments[0]);
                var paramName = $"p{_parameterIndex++}";
                _parameters[paramName] = $"{value}%";
                return $"{columnName} LIKE @{paramName}";
            }
        }
        else if (expression.Method.Name == "EndsWith")
        {
            if (expression.Object is MemberExpression member)
            {
                var columnName = TranslateMember(member);
                var value = GetExpressionValue(expression.Arguments[0]);
                var paramName = $"p{_parameterIndex++}";
                _parameters[paramName] = $"%{value}";
                return $"{columnName} LIKE @{paramName}";
            }
        }
        
        throw new NotSupportedException($"Method {expression.Method.Name} is not supported");
    }
    
    private string TranslateUnary(UnaryExpression expression)
    {
        if (expression.NodeType == ExpressionType.Not)
        {
            return $"NOT {Translate(expression.Operand)}";
        }
        
        if (expression.NodeType == ExpressionType.Convert)
        {
            return Translate(expression.Operand);
        }
        
        throw new NotSupportedException($"Unary operator {expression.NodeType} is not supported");
    }
    
    private object? GetExpressionValue(Expression expression)
    {
        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }
        
        // Compile and execute the expression to get the value
        var lambda = Expression.Lambda(expression);
        return lambda.Compile().DynamicInvoke();
    }
}

