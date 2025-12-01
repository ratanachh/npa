using System.Linq;
using System.Text;
using NPA.Design.Models;
using NPA.Design.Generators.Helpers;
using NPA.Design.Services;

namespace NPA.Design.Generators.CodeGenerators;

/// <summary>
/// Generates method implementations for repository interfaces.
/// </summary>
internal static class MethodGenerator
{
    /// <summary>
    /// Generates a complete method implementation including signature and body.
    /// </summary>
    public static string GenerateMethodImplementation(MethodInfo method, RepositoryInfo info)
    {
        var sb = new StringBuilder();

        // Add XML documentation
        sb.AppendLine("        /// <inheritdoc />");

        // Method signature - add async if return type is Task
        var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"));
        var isAsync = method.ReturnType.StartsWith("System.Threading.Tasks.Task");
        var asyncModifier = isAsync ? "async " : "";

        sb.AppendLine($"        public {asyncModifier}{method.ReturnType} {method.Name}({parameters})");
        sb.AppendLine("        {");

        // Generate implementation based on attributes or conventions
        var implementation = GenerateMethodBody(method, info);
        sb.Append(implementation);

        sb.AppendLine("        }");

        return sb.ToString();
    }

    /// <summary>
    /// Generates the method body by routing to appropriate generators based on attributes or conventions.
    /// </summary>
    public static string GenerateMethodBody(MethodInfo method, RepositoryInfo info)
    {
        var sb = new StringBuilder();
        var attrs = method.Attributes;

        // Priority 1: Check if method name matches a NamedQuery (auto-detection)
        var namedQueryName = TryFindMatchingNamedQuery(method, info);
        if (namedQueryName != null)
        {
            // Use the matched named query (highest priority)
            sb.Append(QueryMethodGenerator.GenerateNamedQueryMethodBody(method, info, namedQueryName));
        }
        // Priority 2: Explicit [NamedQuery] attribute on method
        else if (attrs.HasNamedQuery && !string.IsNullOrEmpty(attrs.NamedQueryName))
        {
            sb.Append(QueryMethodGenerator.GenerateNamedQueryMethodBody(method, info, attrs.NamedQueryName!));
        }
        // Priority 3: [Query] attribute
        else if (attrs.HasQuery)
        {
            sb.Append(QueryMethodGenerator.GenerateQueryMethodBody(method, info, attrs));
        }
        // Priority 4: [StoredProcedure] attribute
        else if (attrs.HasStoredProcedure)
        {
            sb.Append(StoredProcedureGenerator.GenerateStoredProcedureMethodBody(method, info.EntityType, attrs));
        }
        // Priority 5: [BulkOperation] attribute
        else if (attrs.HasBulkOperation)
        {
            sb.Append(BulkOperationGenerator.GenerateBulkOperationMethodBody(method, info.EntityType, attrs));
        }
        // Priority 6: Convention-based generation
        else if (method.Symbol != null)
        {
            // Use convention-based generation
            var convention = MethodConventionAnalyzer.AnalyzeMethod(method.Symbol);
            sb.Append(ConventionBasedQueryGenerator.GenerateConventionBasedMethodBody(method, info, convention));
        }
        else
        {
            // Fallback to simple conventions
            sb.Append(ConventionBasedQueryGenerator.GenerateSimpleConventionBody(method, info));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Tries to find a matching named query based on method name conventions.
    /// Looks for patterns like: EntityName.MethodName or just MethodName
    /// </summary>
    public static string? TryFindMatchingNamedQuery(MethodInfo method, RepositoryInfo info)
    {
        // Get entity metadata to access named queries
        var entityMetadata = info.EntityMetadata;
        if (entityMetadata == null || entityMetadata.NamedQueries == null || !entityMetadata.NamedQueries.Any())
        {
            return null;
        }

        var methodName = method.Name;
        var entityName = info.EntityType;

        // Extract simple entity name without namespace
        var simpleEntityName = entityName.Contains(".")
            ? entityName.Substring(entityName.LastIndexOf('.') + 1)
            : entityName;

        // Try different naming conventions:
        // 1. EntityName.MethodName (e.g., "Order.FindRecentOrdersAsync")
        var fullName = $"{simpleEntityName}.{methodName}";
        if (entityMetadata.NamedQueries.Any(nq => nq.Name == fullName))
        {
            return fullName;
        }

        // 2. Just MethodName (e.g., "FindRecentOrdersAsync")
        if (entityMetadata.NamedQueries.Any(nq => nq.Name == methodName))
        {
            return methodName;
        }

        // 3. Try without "Async" suffix if present
        if (methodName.EndsWith("Async"))
        {
            var nameWithoutAsync = methodName.Substring(0, methodName.Length - 5);

            // EntityName.MethodNameWithoutAsync
            var fullNameWithoutAsync = $"{simpleEntityName}.{nameWithoutAsync}";
            if (entityMetadata.NamedQueries.Any(nq => nq.Name == fullNameWithoutAsync))
            {
                return fullNameWithoutAsync;
            }

            // Just MethodNameWithoutAsync
            if (entityMetadata.NamedQueries.Any(nq => nq.Name == nameWithoutAsync))
            {
                return nameWithoutAsync;
            }
        }

        return null;
    }
}

