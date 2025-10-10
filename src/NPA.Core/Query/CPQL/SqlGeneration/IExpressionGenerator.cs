using NPA.Core.Query.CPQL.AST;

namespace NPA.Core.Query.CPQL.SqlGeneration;

/// <summary>
/// Interface for generating SQL from expressions.
/// </summary>
public interface IExpressionGenerator
{
    /// <summary>
    /// Generates SQL for an expression.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns>The SQL string.</returns>
    string Generate(Expression expression);
}

