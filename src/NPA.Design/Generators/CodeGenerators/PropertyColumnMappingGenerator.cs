using System.Text;
using NPA.Design.Models;

namespace NPA.Design.Generators.CodeGenerators;

/// <summary>
/// Generates property-to-column mapping helper for sorting support.
/// </summary>
internal static class PropertyColumnMappingGenerator
{
    /// <summary>
    /// Generates a static dictionary mapping property names to column names for sorting support.
    /// </summary>
    public static string GeneratePropertyColumnMapping(RepositoryInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("        #region Property-to-Column Mapping");
        sb.AppendLine();
        sb.AppendLine("        private static readonly Dictionary<string, string> _propertyColumnMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)");
        sb.AppendLine("        {");

        if (info.EntityMetadata?.Properties != null)
        {
            foreach (var property in info.EntityMetadata.Properties)
            {
                if (!string.IsNullOrEmpty(property.Name) && !string.IsNullOrEmpty(property.ColumnName))
                {
                    sb.AppendLine($"            {{ \"{property.Name}\", \"{property.ColumnName}\" }},");
                }
            }
        }

        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        private static string GetColumnNameForProperty(string? propertyName, string defaultColumnName)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (string.IsNullOrEmpty(propertyName))");
        sb.AppendLine("                return defaultColumnName;");
        sb.AppendLine();
        sb.AppendLine("            // Security: Only return column names that exist in the map to prevent SQL injection");
        sb.AppendLine("            // If property name is not found, return default column name instead of unsanitized input");
        sb.AppendLine("            return _propertyColumnMap.TryGetValue(propertyName, out var columnName) ? columnName : defaultColumnName;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();

        return sb.ToString();
    }
}

