using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NPA.Design.Models;
using NPA.Design.Generators.Helpers;
using NPA.Design.Services;

namespace NPA.Design.Generators.Builders;

/// <summary>
/// Builds SQL query clauses (WHERE, ORDER BY) and parameter objects for method generation.
/// </summary>
internal static class SqlQueryBuilder
{
    /// <summary>
    /// Builds a WHERE clause from property names, separators, and parameters.
    /// </summary>
    public static string BuildWhereClause(List<string> propertyNames, List<string> separators, List<ParameterInfo> parameters, EntityMetadataInfo? entityMetadata)
    {
        if (propertyNames.Count == 0)
            return string.Empty;

        var clauses = new List<string>();
        var paramIndex = 0;

        for (int i = 0; i < propertyNames.Count; i++)
        {
            var propExpression = propertyNames[i];

            // Check if property has a keyword (format: "Property:Keyword")
            if (propExpression.Contains(":"))
            {
                var parts = propExpression.Split(':');
                var propertyName = parts[0];
                var keyword = parts[1];
                var columnName = MetadataHelper.GetColumnNameForProperty(propertyName, entityMetadata);

                switch (keyword)
                {
                    case "GreaterThan":
                    case "IsGreaterThan":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} > @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "GreaterThanEqual":
                    case "IsGreaterThanEqual":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} >= @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "LessThan":
                    case "IsLessThan":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} < @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "LessThanEqual":
                    case "IsLessThanEqual":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} <= @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "Between":
                    case "IsBetween":
                        if (paramIndex + 1 < parameters.Count)
                        {
                            clauses.Add($"{columnName} BETWEEN @{parameters[paramIndex].Name} AND @{parameters[paramIndex + 1].Name}");
                            paramIndex += 2;
                        }
                        break;
                    case "Like":
                    case "IsLike":
                    case "Containing":
                    case "IsContaining":
                    case "Contains":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} LIKE CONCAT('%', @{parameters[paramIndex].Name}, '%')");
                            paramIndex++;
                        }
                        break;
                    case "NotLike":
                    case "IsNotLike":
                    case "NotContaining":
                    case "IsNotContaining":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} NOT LIKE CONCAT('%', @{parameters[paramIndex].Name}, '%')");
                            paramIndex++;
                        }
                        break;
                    case "Regex":
                    case "Matches":
                    case "IsMatches":
                    case "MatchesRegex":
                        if (paramIndex < parameters.Count)
                        {
                            // MySQL uses REGEXP, PostgreSQL uses ~, SQL Server doesn't have native regex
                            // Using MySQL syntax by default - providers can override
                            clauses.Add($"{columnName} REGEXP @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "StartingWith":
                    case "IsStartingWith":
                    case "StartsWith":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} LIKE CONCAT(@{parameters[paramIndex].Name}, '%')");
                            paramIndex++;
                        }
                        break;
                    case "EndingWith":
                    case "IsEndingWith":
                    case "EndsWith":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} LIKE CONCAT('%', @{parameters[paramIndex].Name})");
                            paramIndex++;
                        }
                        break;
                    case "In":
                    case "IsIn":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} IN @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "NotIn":
                    case "IsNotIn":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} NOT IN @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "IsNull":
                    case "Null":
                        clauses.Add($"{columnName} IS NULL");
                        // No parameter consumed
                        break;
                    case "IsNotNull":
                    case "NotNull":
                        clauses.Add($"{columnName} IS NOT NULL");
                        // No parameter consumed
                        break;
                    case "Is":
                    case "Equals":
                        // Synonyms for equality - handle NULL specially
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} = @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "Not":
                    case "IsNot":
                        // Inequality operator
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} <> @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "True":
                    case "IsTrue":
                        clauses.Add($"{columnName} = TRUE");
                        // No parameter consumed
                        break;
                    case "False":
                    case "IsFalse":
                        clauses.Add($"{columnName} = FALSE");
                        // No parameter consumed
                        break;
                    case "Before":
                    case "IsBefore":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} < @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "After":
                    case "IsAfter":
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} > @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                    case "IgnoreCase":
                    case "IgnoringCase":
                        // Apply to the previous clause only if it's an equality operator
                        if (clauses.Count > 0 && paramIndex > 0)
                        {
                            var lastClause = clauses[clauses.Count - 1];
                            // Only apply IgnoreCase if the last clause is an equality check (contains " = " or " = TRUE" or " = FALSE")
                            if (lastClause.Contains(" = ") || lastClause.Contains(" = TRUE") || lastClause.Contains(" = FALSE"))
                            {
                                clauses[clauses.Count - 1] = $"LOWER({columnName}) = LOWER(@{parameters[paramIndex - 1].Name})";
                            }
                            // If not equality, ignore the IgnoreCase keyword (don't apply to comparisons like >, <, LIKE, etc.)
                        }
                        break;
                    case "AllIgnoreCase":
                    case "AllIgnoringCase":
                        // This would require tracking all properties and applying LOWER to all comparisons
                        // For now, treat same as IgnoreCase - only apply to equality operators
                        if (clauses.Count > 0 && paramIndex > 0)
                        {
                            var lastClause = clauses[clauses.Count - 1];
                            // Only apply AllIgnoreCase if the last clause is an equality check
                            if (lastClause.Contains(" = ") || lastClause.Contains(" = TRUE") || lastClause.Contains(" = FALSE"))
                            {
                                clauses[clauses.Count - 1] = $"LOWER({columnName}) = LOWER(@{parameters[paramIndex - 1].Name})";
                            }
                        }
                        break;
                    default:
                        // Default to equality
                        if (paramIndex < parameters.Count)
                        {
                            clauses.Add($"{columnName} = @{parameters[paramIndex].Name}");
                            paramIndex++;
                        }
                        break;
                }
            }
            else
            {
                // Simple property without keyword - use equality
                var columnName = MetadataHelper.GetColumnNameForProperty(propExpression, entityMetadata);
                if (paramIndex < parameters.Count)
                {
                    clauses.Add($"{columnName} = @{parameters[paramIndex].Name}");
                    paramIndex++;
                }
            }
        }

        // Join clauses with appropriate separators (AND or OR)
        if (clauses.Count == 0)
            return string.Empty;
        if (clauses.Count == 1)
            return clauses[0];

        var result = new StringBuilder();
        result.Append(clauses[0]);

        for (int i = 1; i < clauses.Count; i++)
        {
            // Use separator if available, otherwise default to AND
            var separator = i - 1 < separators.Count ? separators[i - 1].ToUpper() : "AND";
            result.Append($" {separator} {clauses[i]}");
        }

        return result.ToString();
    }

    /// <summary>
    /// Builds an ORDER BY clause from order by properties.
    /// </summary>
    public static string BuildOrderByClause(List<OrderByInfo> orderByProperties, EntityMetadataInfo? entityMetadata)
    {
        if (orderByProperties.Count == 0)
            return string.Empty;

        var clauses = new List<string>();
        foreach (var orderBy in orderByProperties)
        {
            var columnName = MetadataHelper.GetColumnNameForProperty(orderBy.PropertyName, entityMetadata);
            var direction = orderBy.Direction.Equals("Desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            clauses.Add($"{columnName} {direction}");
        }

        return string.Join(", ", clauses);
    }

    /// <summary>
    /// Generates a parameter object string for Dapper queries.
    /// </summary>
    public static string GenerateParameterObject(List<ParameterInfo> parameters)
    {
        if (parameters.Count == 0)
            return "null";

        var props = string.Join(", ", parameters.Select(p => p.Name));
        return $"new {{ {props} }}";
    }
}

