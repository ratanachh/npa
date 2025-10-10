namespace NPA.Core.Query.CPQL;

/// <summary>
/// Default implementation of function registry.
/// </summary>
public sealed class FunctionRegistry : IFunctionRegistry
{
    private readonly Dictionary<string, Dictionary<string, string>> _functions = new(StringComparer.OrdinalIgnoreCase);
    
    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionRegistry"/> class with default functions.
    /// </summary>
    public FunctionRegistry()
    {
        RegisterDefaultFunctions();
    }
    
    /// <inheritdoc />
    public bool IsRegistered(string functionName)
    {
        return _functions.ContainsKey(functionName);
    }
    
    /// <inheritdoc />
    public string GetSqlFunction(string functionName, string dialect)
    {
        if (!_functions.TryGetValue(functionName, out var dialectMappings))
        {
            // If not registered, return the function name as-is
            return functionName;
        }
        
        if (dialectMappings.TryGetValue(dialect, out var sqlFunction))
        {
            return sqlFunction;
        }
        
        // Return default if dialect-specific mapping not found
        if (dialectMappings.TryGetValue("default", out var defaultFunction))
        {
            return defaultFunction;
        }
        
        return functionName;
    }
    
    /// <summary>
    /// Registers a function with SQL mappings for different dialects.
    /// </summary>
    /// <param name="functionName">The CPQL function name.</param>
    /// <param name="dialectMappings">The SQL function names for different dialects.</param>
    public void RegisterFunction(string functionName, Dictionary<string, string> dialectMappings)
    {
        _functions[functionName] = dialectMappings;
    }
    
    private void RegisterDefaultFunctions()
    {
        // Aggregate functions (standard across databases)
        RegisterFunction("COUNT", new Dictionary<string, string> { { "default", "COUNT" } });
        RegisterFunction("SUM", new Dictionary<string, string> { { "default", "SUM" } });
        RegisterFunction("AVG", new Dictionary<string, string> { { "default", "AVG" } });
        RegisterFunction("MIN", new Dictionary<string, string> { { "default", "MIN" } });
        RegisterFunction("MAX", new Dictionary<string, string> { { "default", "MAX" } });
        
        // String functions
        RegisterFunction("UPPER", new Dictionary<string, string> { { "default", "UPPER" } });
        RegisterFunction("LOWER", new Dictionary<string, string> { { "default", "LOWER" } });
        RegisterFunction("TRIM", new Dictionary<string, string> { { "default", "TRIM" } });
        RegisterFunction("CONCAT", new Dictionary<string, string> { { "default", "CONCAT" } });
        
        RegisterFunction("LENGTH", new Dictionary<string, string>
        {
            { "default", "LENGTH" },
            { "SqlServer", "LEN" },
            { "MySql", "LENGTH" },
            { "PostgreSql", "LENGTH" }
        });
        
        RegisterFunction("SUBSTRING", new Dictionary<string, string>
        {
            { "default", "SUBSTRING" },
            { "SqlServer", "SUBSTRING" },
            { "MySql", "SUBSTRING" },
            { "PostgreSql", "SUBSTRING" }
        });
        
        // Date functions
        RegisterFunction("YEAR", new Dictionary<string, string> { { "default", "YEAR" } });
        RegisterFunction("MONTH", new Dictionary<string, string> { { "default", "MONTH" } });
        RegisterFunction("DAY", new Dictionary<string, string> { { "default", "DAY" } });
        RegisterFunction("HOUR", new Dictionary<string, string> { { "default", "HOUR" } });
        RegisterFunction("MINUTE", new Dictionary<string, string> { { "default", "MINUTE" } });
        RegisterFunction("SECOND", new Dictionary<string, string> { { "default", "SECOND" } });
        
        RegisterFunction("NOW", new Dictionary<string, string>
        {
            { "default", "NOW()" },
            { "SqlServer", "GETDATE()" },
            { "MySql", "NOW()" },
            { "PostgreSql", "NOW()" }
        });
    }
}

