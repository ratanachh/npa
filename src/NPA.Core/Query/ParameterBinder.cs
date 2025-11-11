using System.Text.RegularExpressions;

namespace NPA.Core.Query;

/// <summary>
/// Binds parameters to SQL queries safely.
/// </summary>
public class ParameterBinder : IParameterBinder
{
    private static readonly Regex ParameterPattern = new(@"@(\w+)", RegexOptions.Compiled);

    /// <inheritdoc />
    public object BindParameters(Dictionary<string, object?> parameters)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        // Return as anonymous object for Dapper
        var properties = parameters.ToDictionary(
            kvp => kvp.Key.TrimStart(':'), // Remove ':' prefix from parameter names
            kvp => kvp.Value);

        return CreateAnonymousObject(properties);
    }

    /// <inheritdoc />
    public object BindParametersByIndex(Dictionary<int, object?> parameters)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        // Convert indexed parameters to named parameters
        var namedParameters = new Dictionary<string, object?>();
        foreach (var kvp in parameters.OrderBy(x => x.Key))
        {
            namedParameters[$"p{kvp.Key}"] = kvp.Value;
        }

        return CreateAnonymousObject(namedParameters);
    }

    /// <inheritdoc />
    public bool ValidateParameters(string sql, IEnumerable<string> parameterNames)
    {
        if (string.IsNullOrEmpty(sql))
            return false;

        if (parameterNames == null)
            return true;

        var sqlParameters = new HashSet<string>();
        var matches = ParameterPattern.Matches(sql);
        
        foreach (Match match in matches)
        {
            sqlParameters.Add(match.Groups[1].Value);
        }

        var providedParameters = new HashSet<string>(parameterNames);
        return providedParameters.IsSubsetOf(sqlParameters);
    }

    /// <inheritdoc />
    public object? SanitizeParameter(object? value)
    {
        if (value == null)
            return null;

        // Enhanced SQL injection prevention
        if (value is string stringValue)
        {
            // Use parameterized queries instead of string manipulation for security
            // This method should only be used as a last resort
            return SanitizeStringValue(stringValue);
        }

        return value;
    }

    private static string SanitizeStringValue(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Comprehensive SQL injection prevention
        return input.Replace("'", "''")           // Escape single quotes
                   .Replace("\\", "\\\\")         // Escape backslashes
                   .Replace("\0", "\\0")          // Escape null characters
                   .Replace("\n", "\\n")          // Escape newlines
                   .Replace("\r", "\\r")          // Escape carriage returns
                   .Replace("\x1a", "\\Z")        // Escape Ctrl+Z
                   .Replace("--", "")             // Remove SQL comments
                   .Replace("/*", "")             // Remove block comment start
                   .Replace("*/", "")             // Remove block comment end
                   .Replace("xp_", "x_p_")        // Prevent xp_cmdshell calls
                   .Replace("sp_", "s_p_");       // Prevent stored procedure calls
    }

    private object CreateAnonymousObject(Dictionary<string, object?> properties)
    {
        // Create a dynamic object for Dapper parameter binding
        var expando = new System.Dynamic.ExpandoObject() as IDictionary<string, object?>;
        
        foreach (var property in properties)
        {
            expando[property.Key] = property.Value;
        }

        return expando;
    }
}
