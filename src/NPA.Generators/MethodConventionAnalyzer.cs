using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace NPA.Generators;

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

        // Determine query type from method name prefix
        var queryType = DetermineQueryType(methodName);
        
        // Get the prefix string for extracting properties
        var prefix = GetMethodPrefix(methodName, queryType);
        
        // Extract property names and ordering from method name
        var (propertyNames, orderByProperties) = ExtractPropertiesAndOrdering(methodName, prefix);

        // Determine if it returns a collection or single result
        var returnsCollection = IsCollectionReturnType(returnType);

        return new MethodConvention
        {
            QueryType = queryType,
            PropertyNames = propertyNames,
            OrderByProperties = orderByProperties,
            ReturnsCollection = returnsCollection,
            Parameters = parameters.Select(p => new ParameterInfo
            {
                Name = p.Name,
                Type = p.Type.ToDisplayString()
            }).ToList()
        };
    }

    private static QueryType DetermineQueryType(string methodName)
    {
        if (methodName.StartsWith("Find") || methodName.StartsWith("Get") || methodName.StartsWith("Query") || methodName.StartsWith("Search"))
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
            QueryType.Select => new[] { "Find", "Get", "Query", "Search" },
            QueryType.Count => new[] { "Count" },
            QueryType.Exists => new[] { "Exists", "Has", "Is", "Contains" },
            QueryType.Delete => new[] { "Delete", "Remove" },
            QueryType.Update => new[] { "Update", "Modify" },
            QueryType.Insert => new[] { "Insert", "Add", "Save", "Create" },
            _ => new string[0]
        };

        foreach (var prefix in prefixes)
        {
            if (methodName.StartsWith(prefix))
            {
                // Check for "By" after the prefix
                if (methodName.Length > prefix.Length + 2 && 
                    methodName.Substring(prefix.Length, 2) == "By")
                {
                    return prefix + "By";
                }
                return prefix;
            }
        }

        return string.Empty;
    }

    private static (List<string> properties, List<OrderByInfo> ordering) ExtractPropertiesAndOrdering(string methodName, string prefix)
    {
        var properties = new List<string>();
        var ordering = new List<OrderByInfo>();
        
        var afterPrefix = methodName.Substring(prefix.Length);
        
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
        
        // Parse property names with Spring Data JPA keywords
        if (!string.IsNullOrEmpty(propertyPart))
        {
            properties = ParsePropertyExpressions(propertyPart);
        }
        
        return (properties, ordering);
    }
    
    /// <summary>
    /// Parses property expressions with Spring Data JPA keywords like LessThan, GreaterThan, Between, Like, etc.
    /// Supports: And, Or, LessThan, GreaterThan, Between, Like, NotLike, StartingWith, EndingWith, Containing,
    /// In, NotIn, IsNull, IsNotNull, True, False, Before, After, IgnoreCase
    /// </summary>
    private static List<string> ParsePropertyExpressions(string propertyPart)
    {
        var properties = new List<string>();
        
        // Spring Data JPA operator keywords (NOT including And/Or which are separators)
        var operatorKeywords = new[]
        {
            "GreaterThanEqual", "LessThanEqual", "GreaterThan", "LessThan",
            "StartingWith", "EndingWith", "NotContaining", "Containing", "NotLike", "Like",
            "IsNotNull", "IsNull", "NotIn", "In", "Between",
            "IgnoreCase", "AllIgnoreCase", "True", "False", "Before", "After"
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
                if (remaining.StartsWith(keyword))
                {
                    // Append keyword to current property
                    if (currentProperty.Length > 0)
                    {
                        properties.Add(currentProperty.ToString() + ":" + keyword);
                        currentProperty.Clear();
                    }
                    
                    remaining = remaining.Substring(keyword.Length);
                    foundOperator = true;
                    break;
                }
            }
            
            if (foundOperator)
                continue;
            
            // Check for separator keywords (And/Or)
            foreach (var separator in separatorKeywords)
            {
                if (remaining.StartsWith(separator))
                {
                    // Save current property if any
                    if (currentProperty.Length > 0)
                    {
                        properties.Add(currentProperty.ToString());
                        currentProperty.Clear();
                    }
                    
                    remaining = remaining.Substring(separator.Length);
                    foundSeparator = true;
                    break;
                }
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
        
        return properties;
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

    private static List<string> ExtractPropertyNames(string methodName, QueryType queryType)
    {
        var properties = new List<string>();

        // Remove the query type prefix
        var remainder = RemovePrefix(methodName, queryType);
        
        // Check for "By" keyword
        var byIndex = remainder.IndexOf("By");
        if (byIndex >= 0)
        {
            remainder = remainder.Substring(byIndex + 2); // Skip "By"
        }

        // Check for "And" and "Or" separators
        if (remainder.Contains("And"))
        {
            var parts = remainder.Split(new[] { "And" }, System.StringSplitOptions.RemoveEmptyEntries);
            properties.AddRange(parts);
        }
        else if (remainder.Contains("Or"))
        {
            var parts = remainder.Split(new[] { "Or" }, System.StringSplitOptions.RemoveEmptyEntries);
            properties.AddRange(parts);
        }
        else if (!string.IsNullOrEmpty(remainder))
        {
            properties.Add(remainder);
        }

        return properties;
    }

    private static string RemovePrefix(string methodName, QueryType queryType)
    {
        var prefixes = queryType switch
        {
            QueryType.Select => new[] { "FindBy", "Find", "GetBy", "Get", "QueryBy", "Query", "SearchBy", "Search" },
            QueryType.Count => new[] { "CountBy", "Count" },
            QueryType.Exists => new[] { "ExistsBy", "Exists", "HasBy", "Has", "IsBy", "Is", "ContainsBy", "Contains" },
            QueryType.Delete => new[] { "DeleteBy", "Delete", "RemoveBy", "Remove" },
            QueryType.Update => new[] { "UpdateBy", "Update", "ModifyBy", "Modify" },
            QueryType.Insert => new[] { "InsertBy", "Insert", "AddBy", "Add", "SaveBy", "Save", "CreateBy", "Create" },
            _ => new string[0]
        };

        foreach (var prefix in prefixes.OrderByDescending(p => p.Length))
        {
            if (methodName.StartsWith(prefix))
            {
                return methodName.Substring(prefix.Length);
            }
        }

        return methodName;
    }

    private static bool IsCollectionReturnType(ITypeSymbol returnType)
    {
        var typeString = returnType.ToDisplayString();
        
        // Handle Task<T>
        if (typeString.StartsWith("System.Threading.Tasks.Task<"))
        {
            var innerType = returnType is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0
                ? namedType.TypeArguments[0]
                : null;
            
            if (innerType != null)
            {
                typeString = innerType.ToDisplayString();
            }
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

        for (int i = 1; i < text.Length; i++)
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
    /// Gets or sets the ordering information.
    /// </summary>
    public List<OrderByInfo> OrderByProperties { get; set; } = new();
    
    /// <summary>
    /// Gets or sets a value indicating whether the method returns a collection.
    /// </summary>
    public bool ReturnsCollection { get; set; }
    
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
