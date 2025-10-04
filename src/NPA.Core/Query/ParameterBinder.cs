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

        // Basic SQL injection prevention
        if (value is string stringValue)
        {
            // Remove potential SQL injection characters
            return stringValue.Replace("'", "''")
                             .Replace("--", "")
                             .Replace("/*", "")
                             .Replace("*/", "");
        }

        return value;
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
