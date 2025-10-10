using NPA.Core.Query.CPQL.AST;

namespace NPA.Core.Query.CPQL.SqlGeneration;

/// <summary>
/// Interface for generating SQL ORDER BY clauses.
/// </summary>
public interface IOrderByGenerator
{
    /// <summary>
    /// Generates ORDER BY SQL.
    /// </summary>
    /// <param name="orderBy">The ORDER BY clause.</param>
    /// <returns>The SQL ORDER BY string.</returns>
    string Generate(OrderByClause orderBy);
}

