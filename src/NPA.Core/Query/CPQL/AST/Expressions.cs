namespace NPA.Core.Query.CPQL.AST;

/// <summary>
/// Base class for all expressions.
/// </summary>
public abstract class Expression
{
}

/// <summary>
/// Represents a property access expression (e.g., u.Username or Username).
/// </summary>
public sealed class PropertyExpression : Expression
{
    /// <summary>Gets or sets the entity alias (optional).</summary>
    public string? EntityAlias { get; set; }
    
    /// <summary>Gets or sets the property name.</summary>
    public string PropertyName { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public override string ToString()
    {
        return EntityAlias != null ? $"{EntityAlias}.{PropertyName}" : PropertyName;
    }
}

/// <summary>
/// Represents a literal value expression.
/// </summary>
public sealed class LiteralExpression : Expression
{
    /// <summary>Gets or sets the literal value.</summary>
    public object? Value { get; set; }
    
    /// <inheritdoc />
    public override string ToString()
    {
        return Value?.ToString() ?? "NULL";
    }
}

/// <summary>
/// Represents a parameter expression (e.g., :username).
/// </summary>
public sealed class ParameterExpression : Expression
{
    /// <summary>Gets or sets the parameter name.</summary>
    public string ParameterName { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public override string ToString()
    {
        return $":{ParameterName}";
    }
}

/// <summary>
/// Represents a binary expression (e.g., a + b, a = b).
/// </summary>
public sealed class BinaryExpression : Expression
{
    /// <summary>Gets or sets the left operand.</summary>
    public Expression Left { get; set; } = null!;
    
    /// <summary>Gets or sets the binary operator.</summary>
    public BinaryOperator Operator { get; set; }
    
    /// <summary>Gets or sets the right operand.</summary>
    public Expression Right { get; set; } = null!;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryExpression"/> class.
    /// </summary>
    public BinaryExpression() { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryExpression"/> class with specified operands and operator.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="op">The binary operator.</param>
    /// <param name="right">The right operand.</param>
    public BinaryExpression(Expression left, BinaryOperator op, Expression right)
    {
        Left = left;
        Operator = op;
        Right = right;
    }
    
    /// <inheritdoc />
    public override string ToString()
    {
        return $"({Left} {Operator} {Right})";
    }
}

/// <summary>
/// Binary operators.
/// </summary>
public enum BinaryOperator
{
    /// <summary>Addition (+).</summary>
    Add,
    /// <summary>Subtraction (-).</summary>
    Subtract,
    /// <summary>Multiplication (*).</summary>
    Multiply,
    /// <summary>Division (/).</summary>
    Divide,
    /// <summary>Modulo (%).</summary>
    Modulo,
    
    /// <summary>Equal (=).</summary>
    Equal,
    /// <summary>Not equal (&lt;&gt; or !=).</summary>
    NotEqual,
    /// <summary>Less than (&lt;).</summary>
    LessThan,
    /// <summary>Less than or equal (&lt;=).</summary>
    LessThanOrEqual,
    /// <summary>Greater than (&gt;).</summary>
    GreaterThan,
    /// <summary>Greater than or equal (&gt;=).</summary>
    GreaterThanOrEqual,
    /// <summary>LIKE operator.</summary>
    Like,
    /// <summary>IN operator.</summary>
    In,
    /// <summary>BETWEEN operator.</summary>
    Between,
    /// <summary>IS operator.</summary>
    Is,
    
    /// <summary>AND logical operator.</summary>
    And,
    /// <summary>OR logical operator.</summary>
    Or
}

/// <summary>
/// Represents a unary expression (e.g., -a, NOT b).
/// </summary>
public sealed class UnaryExpression : Expression
{
    /// <summary>Gets or sets the unary operator.</summary>
    public UnaryOperator Operator { get; set; }
    
    /// <summary>Gets or sets the operand.</summary>
    public Expression Operand { get; set; } = null!;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UnaryExpression"/> class.
    /// </summary>
    public UnaryExpression() { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UnaryExpression"/> class with specified operator and operand.
    /// </summary>
    /// <param name="op">The unary operator.</param>
    /// <param name="operand">The operand.</param>
    public UnaryExpression(UnaryOperator op, Expression operand)
    {
        Operator = op;
        Operand = operand;
    }
    
    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Operator} {Operand}";
    }
}

/// <summary>
/// Unary operators.
/// </summary>
public enum UnaryOperator
{
    /// <summary>Unary plus (+).</summary>
    Plus,
    /// <summary>Unary minus (-).</summary>
    Minus,
    /// <summary>Logical NOT.</summary>
    Not
}

/// <summary>
/// Represents a function call expression (e.g., UPPER(u.Username)).
/// </summary>
public sealed class FunctionExpression : Expression
{
    /// <summary>Gets or sets the function name.</summary>
    public string FunctionName { get; set; } = string.Empty;
    
    /// <summary>Gets or sets the list of function arguments.</summary>
    public List<Expression> Arguments { get; set; } = new();
    
    /// <inheritdoc />
    public override string ToString()
    {
        return $"{FunctionName}({string.Join(", ", Arguments)})";
    }
}

/// <summary>
/// Represents an aggregate function expression (e.g., COUNT(u.Id), SUM(o.Total)).
/// </summary>
public sealed class AggregateExpression : Expression
{
    /// <summary>Gets or sets the aggregate function name.</summary>
    public string FunctionName { get; set; } = string.Empty;
    
    /// <summary>Gets or sets the aggregate argument expression.</summary>
    public Expression Argument { get; set; } = null!;
    
    /// <summary>Gets or sets whether DISTINCT should be applied.</summary>
    public bool IsDistinct { get; set; }
    
    /// <inheritdoc />
    public override string ToString()
    {
        var distinct = IsDistinct ? "DISTINCT " : "";
        return $"{FunctionName}({distinct}{Argument})";
    }
}

/// <summary>
/// Represents a subquery expression.
/// </summary>
public sealed class SubqueryExpression : Expression
{
    /// <summary>Gets or sets the subquery.</summary>
    public SelectQuery Query { get; set; } = null!;
    
    /// <inheritdoc />
    public override string ToString()
    {
        return "(subquery)";
    }
}

/// <summary>
/// Represents a wildcard expression (SELECT *).
/// </summary>
public sealed class WildcardExpression : Expression
{
    /// <summary>Gets or sets the entity alias (optional, for alias.*).</summary>
    public string? EntityAlias { get; set; }
    
    /// <inheritdoc />
    public override string ToString()
    {
        return EntityAlias != null ? $"{EntityAlias}.*" : "*";
    }
}
