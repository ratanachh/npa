using System;

namespace NPA.Design.Generators.Helpers;

/// <summary>
/// Helper class for type-related utility methods.
/// </summary>
internal static class TypeHelper
{
    /// <summary>
    /// Extracts the inner type from Task&lt;T&gt;, IEnumerable&lt;T&gt;, arrays, etc.
    /// </summary>
    public static string GetInnerType(string typeString)
    {
        // Handle Task<T> first
        if (typeString.StartsWith("System.Threading.Tasks.Task<"))
        {
            var taskInner = ExtractFirstGenericArgument(typeString.Substring("System.Threading.Tasks.Task".Length));
            return GetInnerType(taskInner); // Recursively handle nested generics
        }

        // Handle arrays (T[] or T?[])
        if (typeString.Contains("[]"))
        {
            return typeString.Replace("[]", "");
        }

        // Handle IEnumerable<T>, ICollection<T>, List<T>, HashSet<T>, ISet<T>, etc.
        if (typeString.Contains("IEnumerable<") || typeString.Contains("ICollection<") ||
            typeString.Contains("IList<") || typeString.Contains("List<") ||
            typeString.Contains("HashSet<") || typeString.Contains("ISet<") ||
            typeString.Contains("IReadOnlyCollection<") || typeString.Contains("IReadOnlyList<"))
        {
            var collectionStart = typeString.IndexOf('<');
            if (collectionStart >= 0)
            {
                var innerType = ExtractFirstGenericArgument(typeString.Substring(collectionStart));
                // Don't trim '?' - preserve nullability of the element type
                return innerType;
            }
        }

        // No generic type found, return as is (preserve nullability)
        return typeString;
    }

    /// <summary>
    /// Determines what conversion method to use based on return type.
    /// Returns: empty string (no conversion), "ToList()", "ToArray()", "ToHashSet()"
    /// </summary>
    public static string GetCollectionConversion(string returnType)
    {
        // Determine what conversion method to use based on return type
        // Returns: empty string (no conversion), "ToList()", "ToArray()", "ToHashSet()"

        if (returnType.Contains("[]"))
            return "ToArray()";

        // List<T>, IList<T>, IReadOnlyList<T> all need ToList()
        if (returnType.Contains("List<") || returnType.Contains("System.Collections.Generic.List<") ||
            returnType.Contains("IList<") || returnType.Contains("System.Collections.Generic.IList<") ||
            returnType.Contains("IReadOnlyList<") || returnType.Contains("System.Collections.Generic.IReadOnlyList<"))
            return "ToList()";

        // IReadOnlyCollection<T> also needs ToList() (can't use ToHashSet for this)
        if (returnType.Contains("IReadOnlyCollection<") || returnType.Contains("System.Collections.Generic.IReadOnlyCollection<"))
            return "ToList()";

        if (returnType.Contains("HashSet<") || returnType.Contains("System.Collections.Generic.HashSet<"))
            return "ToHashSet()";

        if (returnType.Contains("ISet<") || returnType.Contains("System.Collections.Generic.ISet<"))
            return "ToHashSet()";

        // IEnumerable, ICollection - no conversion needed (QueryAsync returns IEnumerable)
        return string.Empty;
    }

    /// <summary>
    /// Extracts the first generic argument from a type string.
    /// </summary>
    public static string ExtractFirstGenericArgument(string text)
    {
        // Find the first < and matching >
        var startIndex = text.IndexOf('<');
        if (startIndex < 0)
            return text;

        var depth = 0;
        for (int i = startIndex; i < text.Length; i++)
        {
            if (text[i] == '<')
                depth++;
            else if (text[i] == '>')
            {
                depth--;
                if (depth == 0)
                {
                    return text.Substring(startIndex + 1, i - startIndex - 1);
                }
            }
        }

        return text;
    }

    /// <summary>
    /// Checks if a type name represents a DateTime or DateTimeOffset type.
    /// </summary>
    public static bool IsDateTimeType(string typeName)
    {
        var normalizedType = typeName.TrimEnd('?'); // Remove nullable marker
        return normalizedType == "DateTime" || normalizedType == "System.DateTime" || 
               normalizedType == "DateTimeOffset" || normalizedType == "System.DateTimeOffset";
    }

    /// <summary>
    /// Checks if a type is a simple type (primitive, string, DateTime, etc.).
    /// </summary>
    public static bool IsSimpleType(string typeName)
    {
        var simpleTypes = new[] { "string", "int", "long", "decimal", "double", "float", "bool", "DateTime", "Guid", "byte", "short", "char" };
        var normalizedType = typeName.TrimEnd('?'); // Remove nullable marker
        return simpleTypes.Contains(normalizedType) || normalizedType.StartsWith("System.");
    }

    /// <summary>
    /// Checks if a type is numeric and can be used in aggregate functions.
    /// </summary>
    public static bool IsNumericType(string typeName)
    {
        var numericTypes = new[] { "int", "long", "decimal", "double", "float", "byte", "short", "System.Int32", "System.Int64", "System.Decimal", "System.Double", "System.Single", "System.Byte", "System.Int16" };
        var normalizedType = typeName.TrimEnd('?'); // Remove nullable marker
        return numericTypes.Contains(normalizedType) || normalizedType.StartsWith("System.Int") || normalizedType.StartsWith("System.Decimal") || normalizedType.StartsWith("System.Double") || normalizedType.StartsWith("System.Single");
    }
}

