using NPA.Core.Query.CPQL.AST;

namespace NPA.Core.Query.CPQL.SqlGeneration;

/// <summary>
/// Interface for generating SQL JOIN clauses.
/// </summary>
public interface IJoinGenerator
{
    /// <summary>
    /// Generates JOIN SQL for a list of join clauses.
    /// </summary>
    /// <param name="joins">The join clauses.</param>
    /// <returns>The SQL JOIN string.</returns>
    string Generate(List<AST.JoinClause> joins);
}

