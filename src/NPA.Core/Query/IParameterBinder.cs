namespace NPA.Core.Query;

/// <summary>
/// Binds parameters to SQL queries safely.
/// </summary>
public interface IParameterBinder
{
    /// <summary>
    /// Binds parameters to a SQL query.
    /// </summary>
    /// <param name="parameters">The parameters to bind.</param>
    /// <returns>An object containing the bound parameters for Dapper.</returns>
    object BindParameters(Dictionary<string, object?> parameters);

    /// <summary>
    /// Binds parameters by index to a SQL query.
    /// </summary>
    /// <param name="parameters">The parameters to bind, indexed by position.</param>
    /// <returns>An object containing the bound parameters for Dapper.</returns>
    object BindParametersByIndex(Dictionary<int, object?> parameters);

    /// <summary>
    /// Validates parameter names against the SQL query.
    /// </summary>
    /// <param name="sql">The SQL query string.</param>
    /// <param name="parameterNames">The parameter names to validate.</param>
    /// <returns>True if all parameters are valid; otherwise, false.</returns>
    bool ValidateParameters(string sql, IEnumerable<string> parameterNames);

    /// <summary>
    /// Sanitizes a parameter value to prevent SQL injection.
    /// </summary>
    /// <param name="value">The parameter value to sanitize.</param>
    /// <returns>The sanitized parameter value.</returns>
    object? SanitizeParameter(object? value);
}
