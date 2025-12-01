using System;
using System.Text;

namespace NPA.Design.Generators.Helpers;

/// <summary>
/// Helper class for string manipulation utilities.
/// </summary>
internal static class StringHelper
{
    /// <summary>
    /// Converts PascalCase to camelCase.
    /// </summary>
    public static string ToCamelCase(string pascalCase)
    {
        if (string.IsNullOrEmpty(pascalCase) || char.IsLower(pascalCase[0]))
            return pascalCase;

        return char.ToLower(pascalCase[0]) + pascalCase.Substring(1);
    }

    /// <summary>
    /// Converts input to PascalCase (handles snake_case and other formats).
    /// </summary>
    public static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Handle single character input
        if (input.Length == 1)
        {
            return char.ToUpper(input[0]).ToString();
        }

        // If already PascalCase (starts with upper) and no underscores, just return
        if (char.IsUpper(input[0]) && !input.Contains("_")) return input;

        var parts = input.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length > 0)
            {
                sb.Append(char.ToUpper(part[0]));
                if (part.Length > 1)
                    sb.Append(part.Substring(1).ToLower());
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Gets the implementation name from an interface name.
    /// IUserRepository -> UserRepositoryImplementation
    /// </summary>
    public static string GetImplementationName(string interfaceName)
    {
        // IUserRepository -> UserRepositoryImplementation
        if (interfaceName.StartsWith("I") && interfaceName.Length > 1 && char.IsUpper(interfaceName[1]))
            return interfaceName.Substring(1) + "Implementation";

        return interfaceName + "Implementation";
    }

    /// <summary>
    /// Simple pluralization helper. Handles common cases but is not comprehensive.
    /// For accurate pluralization, consider using a dedicated library or providing explicit table names via [Table] attribute.
    /// </summary>
    public static string Pluralize(string word)
    {
        if (string.IsNullOrEmpty(word))
            return word;

        // Handle common irregular plurals
        var irregularPlurals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "person", "people" },
            { "child", "children" },
            { "man", "men" },
            { "woman", "women" },
            { "foot", "feet" },
            { "tooth", "teeth" },
            { "mouse", "mice" },
            { "goose", "geese" },
            { "ox", "oxen" }
        };

        if (irregularPlurals.TryGetValue(word, out var plural))
            return plural;

        // Handle words ending in 'y' preceded by a consonant
        if (word.Length > 1 && word.EndsWith("y", StringComparison.OrdinalIgnoreCase))
        {
            var secondLast = word[word.Length - 2];
            if (!IsVowel(secondLast))
            {
                return word.Substring(0, word.Length - 1) + "ies";
            }
        }

        // Handle words ending in 's', 'x', 'z', 'ch', 'sh'
        if (word.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("z", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
        {
            return word + "es";
        }

        // Default: add 's'
        return word + "s";
    }

    private static bool IsVowel(char c)
    {
        return c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u' ||
               c == 'A' || c == 'E' || c == 'I' || c == 'O' || c == 'U';
    }
}

