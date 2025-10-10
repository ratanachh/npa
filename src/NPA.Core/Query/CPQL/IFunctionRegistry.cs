namespace NPA.Core.Query.CPQL;

/// <summary>
/// Interface for registering and resolving CPQL functions.
/// </summary>
public interface IFunctionRegistry
{
    /// <summary>
    /// Checks if a function is registered.
    /// </summary>
    /// <param name="functionName">The function name.</param>
    /// <returns>True if the function is registered; otherwise, false.</returns>
    bool IsRegistered(string functionName);
    
    /// <summary>
    /// Gets the SQL representation of a function for a specific database dialect.
    /// </summary>
    /// <param name="functionName">The function name.</param>
    /// <param name="dialect">The database dialect.</param>
    /// <returns>The SQL function name.</returns>
    string GetSqlFunction(string functionName, string dialect);
}

