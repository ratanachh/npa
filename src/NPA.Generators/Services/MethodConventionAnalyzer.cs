using Microsoft.CodeAnalysis;
using NPA.Generators.Models;

namespace NPA.Generators.Services;

/// <summary>
/// Analyzes repository method names to determine query intent and generate appropriate SQL.
/// </summary>
public static class MethodConventionAnalyzer
{
    /// <summary>
    /// Analyzes a method to determine its query convention.
    /// </summary>
    public static MethodConvention AnalyzeMethod(IMethodSymbol method)
    {
        var methodName = method.Name.Replace("Async", ""); // Remove Async suffix
        var parameters = method.Parameters;
        var returnType = method.ReturnType;

        // Check for Distinct modifier
        var hasDistinct = methodName.Contains("Distinct");

        // Determine query type from method name prefix
        var queryType = DetermineQueryType(methodName);
        
        // Get the prefix string for extracting properties
        var prefix = GetMethodPrefix(methodName, queryType);
        
        // Extract result limit (First/Top keywords)
        var limit = ExtractResultLimit(methodName, prefix);
        
        // Extract property names and ordering from method name
        var (propertyNames, separators, orderByProperties) = ExtractPropertiesAndOrdering(methodName, prefix);

        // Determine if it returns a collection or single result
        var returnsCollection = IsCollectionReturnType(returnType);

        return new MethodConvention
        {
            QueryType = queryType,
            PropertyNames = propertyNames,
            PropertySeparators = separators,
            OrderByProperties = orderByProperties,
            ReturnsCollection = returnsCollection,
            HasDistinct = hasDistinct,
            Limit = limit,
            Parameters = parameters.Select(p => new ParameterInfo
            {
                Name = p.Name,
                Type = p.Type.ToDisplayString()
            }).ToList()
        };
    }

    private static QueryType DetermineQueryType(string methodName)
    {
        if (methodName.StartsWith("Find") || methodName.StartsWith("Get") || methodName.StartsWith("Query") || methodName.StartsWith("Search") || methodName.StartsWith("Read") || methodName.StartsWith("Stream"))
            return QueryType.Select;
        
        if (methodName.StartsWith("Count"))
            return QueryType.Count;
        
        if (methodName.StartsWith("Exists") || methodName.StartsWith("Has") || methodName.StartsWith("Is") || methodName.StartsWith("Contains"))
            return QueryType.Exists;
        
        if (methodName.StartsWith("Delete") || methodName.StartsWith("Remove"))
            return QueryType.Delete;
        
        if (methodName.StartsWith("Update") || methodName.StartsWith("Modify"))
            return QueryType.Update;
        
        if (methodName.StartsWith("Insert") || methodName.StartsWith("Add") || methodName.StartsWith("Save") || methodName.StartsWith("Create"))
            return QueryType.Insert;

        return QueryType.Unknown;
    }

    private static string GetMethodPrefix(string methodName, QueryType queryType)
    {
        // Define known prefixes for each query type
        var prefixes = queryType switch
        {
            QueryType.Select => new[] { "Find", "Get", "Query", "Search", "Read", "Stream" },
            QueryType.Count => new[] { "Count" },
            QueryType.Exists => new[] { "Exists", "Has", "Is", "Contains" },
            QueryType.Delete => new[] { "Delete", "Remove" },
            QueryType.Update => new[] { "Update", "Modify" },
            QueryType.Insert => new[] { "Insert", "Add", "Save", "Create" },
            _ => Array.Empty<string>()
        };

        foreach (var prefix in prefixes)
        {
            if (!methodName.StartsWith(prefix)) continue;
            
            // Check for "By" after the prefix
            if (methodName.Length > prefix.Length + 2 && 
                methodName.Substring(prefix.Length, 2) == "By")
            {
                return prefix + "By";
            }
            return prefix;
        }

        return string.Empty;
    }

    private static int? ExtractResultLimit(string methodName, string prefix)
    {
        // After the prefix, check for First or Top keywords
        var afterPrefix = methodName.Substring(prefix.Length);
        
        // Skip "Distinct" if present (it's a separate modifier)
        if (afterPrefix.StartsWith("Distinct"))
        {
            afterPrefix = afterPrefix.Substring(8); // Skip "Distinct"
        }
        
        // Check for "First" or "Top" followed by optional number
        if (afterPrefix.StartsWith("First"))
        {
            afterPrefix = afterPrefix.Substring(5); // Skip "First"
            return ExtractNumber(afterPrefix, defaultValue: 1);
        }
        
        if (afterPrefix.StartsWith("Top"))
        {
            afterPrefix = afterPrefix.Substring(3); // Skip "Top"
            return ExtractNumber(afterPrefix, defaultValue: 1);
        }
        
        return null;
    }
    
    private static int ExtractNumber(string text, int defaultValue)
    {
        // Extract leading digits
        int number = 0;
        int digitCount = 0;
        
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsDigit(text[i]))
            {
                number = number * 10 + (text[i] - '0');
                digitCount++;
            }
            else
            {
                break;
            }
        }
        
        return digitCount > 0 ? number : defaultValue;
    }

    private static (List<string> properties, List<string> separators, List<OrderByInfo> ordering) ExtractPropertiesAndOrdering(string methodName, string prefix)
    {
        var properties = new List<string>();
        var separators = new List<string>();
        var ordering = new List<OrderByInfo>();
        
        var afterPrefix = methodName.Substring(prefix.Length);
        
        // Remove "Distinct" if present (it's handled separately as a modifier)
        if (afterPrefix.StartsWith("Distinct"))
        {
            afterPrefix = afterPrefix.Substring(8); // Skip "Distinct"
        }
        
        // Remove "First" or "Top" with optional number (handled separately as Limit)
        if (afterPrefix.StartsWith("First"))
        {
            afterPrefix = afterPrefix.Substring(5); // Skip "First"
            // Skip any digits
            while (afterPrefix.Length > 0 && char.IsDigit(afterPrefix[0]))
            {
                afterPrefix = afterPrefix.Substring(1);
            }
        }
        else if (afterPrefix.StartsWith("Top"))
        {
            afterPrefix = afterPrefix.Substring(3); // Skip "Top"
            // Skip any digits
            while (afterPrefix.Length > 0 && char.IsDigit(afterPrefix[0]))
            {
                afterPrefix = afterPrefix.Substring(1);
            }
        }
        
        // Check if there's an "OrderBy" clause
        var orderByIndex = afterPrefix.IndexOf("OrderBy", StringComparison.Ordinal);
        
        string propertyPart;
        if (orderByIndex >= 0)
        {
            propertyPart = afterPrefix.Substring(0, orderByIndex);
            var orderPart = afterPrefix.Substring(orderByIndex + 7); // Skip "OrderBy"
            ordering = ParseOrderBy(orderPart);
        }
        else
        {
            propertyPart = afterPrefix;
        }
        
        // Parse property names with NPA keywords
        if (!string.IsNullOrEmpty(propertyPart))
        {
            (properties, separators) = ParsePropertyExpressions(propertyPart);
        }
        
        return (properties, separators, ordering);
    }
    
    /// <summary>
    /// Parses property expressions with NPA keywords like LessThan, GreaterThan, Between, Like, etc.
    /// Supports all Spring Data NPA keywords with synonyms (Is prefix, EndsWith vs EndingWith, etc.)
    /// Returns properties and the separators between them.
    /// </summary>
    private static (List<string> properties, List<string> separators) ParsePropertyExpressions(string propertyPart)
    {
        var properties = new List<string>();
        var separators = new List<string>();
        
        // NPA operator keywords (NOT including And/Or which are separators)
        // Ordered by length (longest first) to avoid partial matches
        var operatorKeywords = new[]
        {
            // Comparison (with synonyms)
            "IsGreaterThanEqual", "GreaterThanEqual", "IsLessThanEqual", "LessThanEqual",
            "IsGreaterThan", "GreaterThan", "IsLessThan", "LessThan",
            
            // String operations (with synonyms)
            "IsStartingWith", "StartingWith", "StartsWith",
            "IsEndingWith", "EndingWith", "EndsWith",
            "IsNotContaining", "NotContaining", "IsContaining", "Containing", "Contains",
            "IsNotLike", "NotLike", "IsLike", "Like",
            
            // Pattern matching (regex)
            "MatchesRegex", "IsMatches", "Matches", "Regex",
            
            // Null checks (with synonyms)
            "IsNotNull", "NotNull", "IsNull", "Null",
            
            // Collection operations (with synonyms)
            "IsNotIn", "NotIn", "IsIn", "In",
            
            // Other operations
            "IsBetween", "Between",
            "IgnoringCase", "IgnoreCase", "AllIgnoringCase", "AllIgnoreCase",
            "IsAfter", "After", "IsBefore", "Before",
            "IsTrue", "True", "IsFalse", "False",
            "IsNot", "Not", "Equals", "Is"
        };
        
        // Separator keywords
        var separatorKeywords = new[] { "And", "Or" };
        
        var remaining = propertyPart;
        var currentProperty = new System.Text.StringBuilder();
        
        while (remaining.Length > 0)
        {
            var foundOperator = false;
            var foundSeparator = false;
            
            // Check for operator keywords first
            foreach (var keyword in operatorKeywords)
            {
                if (!remaining.StartsWith(keyword)) continue;
                
                // Append keyword to current property
                if (currentProperty.Length > 0)
                {
                    properties.Add(currentProperty + ":" + keyword);
                    currentProperty.Clear();
                }
                    
                remaining = remaining.Substring(keyword.Length);
                foundOperator = true;
                break;
            }
            
            if (foundOperator)
                continue;
            
            // Check for separator keywords (And/Or)
            foreach (var separator in separatorKeywords)
            {
                if (!remaining.StartsWith(separator)) continue;
                
                // Save current property if any
                if (currentProperty.Length > 0)
                {
                    properties.Add(currentProperty.ToString());
                    currentProperty.Clear();
                }
                
                // Track the separator
                separators.Add(separator);
                    
                remaining = remaining.Substring(separator.Length);
                foundSeparator = true;
                break;
            }
            
            if (foundSeparator)
                continue;
            
            // No keyword found, consume one character
            currentProperty.Append(remaining[0]);
            remaining = remaining.Substring(1);
        }
        
        // Add final property if any
        if (currentProperty.Length > 0)
        {
            properties.Add(currentProperty.ToString());
        }
        
        return (properties, separators);
    }
    
    /// <summary>
    /// Parses the OrderBy part of a method name.
    /// Handles patterns like "NameDesc", "EmailAsc", "NameDescThenEmailAsc".
    /// </summary>
    private static List<OrderByInfo> ParseOrderBy(string orderPart)
    {
        var result = new List<OrderByInfo>();
        
        if (string.IsNullOrEmpty(orderPart))
            return result;
        
        // Split on "Then" for multiple order clauses
        var orderClauses = orderPart.Split(new[] { "Then" }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var clause in orderClauses)
        {
            var orderInfo = new OrderByInfo();
            
            // Check for "Desc" or "Asc" suffix
            if (clause.EndsWith("Desc", StringComparison.Ordinal))
            {
                orderInfo.PropertyName = clause.Substring(0, clause.Length - 4);
                orderInfo.Direction = "Desc";
            }
            else if (clause.EndsWith("Asc", StringComparison.Ordinal))
            {
                orderInfo.PropertyName = clause.Substring(0, clause.Length - 3);
                orderInfo.Direction = "Asc";
            }
            else
            {
                // Default to Asc if no direction specified
                orderInfo.PropertyName = clause;
                orderInfo.Direction = "Asc";
            }
            
            if (!string.IsNullOrEmpty(orderInfo.PropertyName))
            {
                result.Add(orderInfo);
            }
        }
        
        return result;
    }
    
    private static bool IsCollectionReturnType(ITypeSymbol returnType)
    {
        var typeString = returnType.ToDisplayString();
        
        // Handle Task<T>
        if (!typeString.StartsWith("System.Threading.Tasks.Task<"))
            return typeString.Contains("IEnumerable<") ||
                   typeString.Contains("ICollection<") ||
                   typeString.Contains("IList<") ||
                   typeString.Contains("List<") ||
                   typeString.Contains("[]");
        
        var innerType = returnType is INamedTypeSymbol { TypeArguments.Length: > 0 } namedType
            ? namedType.TypeArguments[0]
            : null;
            
        if (innerType != null)
        {
            typeString = innerType.ToDisplayString();
        }

        return typeString.Contains("IEnumerable<") ||
               typeString.Contains("ICollection<") ||
               typeString.Contains("IList<") ||
               typeString.Contains("List<") ||
               typeString.Contains("[]");
    }

    /// <summary>
    /// Converts a Pascal case property name to snake_case for SQL.
    /// </summary>
    public static string ToSnakeCase(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLower(text[0]));

        for (var i = 1; i < text.Length; i++)
        {
            if (char.IsUpper(text[i]))
            {
                result.Append('_');
                result.Append(char.ToLower(text[i]));
            }
            else
            {
                result.Append(text[i]);
            }
        }

        return result.ToString();
    }
}

/// <summary>
/// Represents the analyzed convention of a repository method.
/// </summary>
public class MethodConvention
{
    /// <summary>
    /// Gets or sets the type of query operation.
    /// </summary>
    public QueryType QueryType { get; set; }
    
    /// <summary>
    /// Gets or sets the property names used in WHERE clause.
    /// </summary>
    public List<string> PropertyNames { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the separators between properties ("And" or "Or").
    /// The list has N-1 elements for N properties (separator[i] is between property[i] and property[i+1]).
    /// </summary>
    public List<string> PropertySeparators { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the ordering information.
    /// </summary>
    public List<OrderByInfo> OrderByProperties { get; set; } = new();
    
    /// <summary>
    /// Gets or sets a value indicating whether the method returns a collection.
    /// </summary>
    public bool ReturnsCollection { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the query should use DISTINCT.
    /// </summary>
    public bool HasDistinct { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum number of results to return (for First/Top queries).
    /// </summary>
    public int? Limit { get; set; }
    
    /// <summary>
    /// Gets or sets the method parameters.
    /// </summary>
    public List<ParameterInfo> Parameters { get; set; } = new();
}

/// <summary>
/// Represents ordering information for a query.
/// </summary>
public class OrderByInfo
{
    /// <summary>
    /// Gets or sets the property name to order by.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the sort direction ("Asc" or "Desc").
    /// </summary>
    public string Direction { get; set; } = "Asc";
}

/// <summary>
/// Specifies the type of query operation.
/// </summary>
public enum QueryType
{
    /// <summary>
    /// Unknown or unsupported query type.
    /// </summary>
    Unknown,
    
    /// <summary>
    /// SELECT query to retrieve data.
    /// </summary>
    Select,
    
    /// <summary>
    /// INSERT query to add new data.
    /// </summary>
    Insert,
    
    /// <summary>
    /// UPDATE query to modify existing data.
    /// </summary>
    Update,
    
    /// <summary>
    /// DELETE query to remove data.
    /// </summary>
    Delete,
    
    /// <summary>
    /// COUNT query to count records.
    /// </summary>
    Count,
    
    /// <summary>
    /// EXISTS query to check if records exist.
    /// </summary>
    Exists
}
