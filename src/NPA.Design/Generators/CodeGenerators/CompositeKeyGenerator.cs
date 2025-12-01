using System.Linq;
using System.Text;
using NPA.Design.Models;
using NPA.Design.Generators.Helpers;

namespace NPA.Design.Generators.CodeGenerators;

/// <summary>
/// Generates composite key methods for repositories.
/// </summary>
internal static class CompositeKeyGenerator
{
    /// <summary>
    /// Generates composite key method overloads.
    /// </summary>
    public static string GenerateCompositeKeyMethods(RepositoryInfo info)
    {
        // Validate that composite key properties exist
        if (info.CompositeKeyProperties == null || info.CompositeKeyProperties.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var entityType = info.EntityType;

        sb.AppendLine("        #region Composite Key Methods");
        sb.AppendLine();

        // GetByIdAsync(CompositeKey)
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Gets an entity by its composite key asynchronously.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <param name=\"key\">The composite key.</param>");
        sb.AppendLine("        /// <returns>The entity if found; otherwise, null.</returns>");
        sb.AppendLine($"        public async Task<{entityType}?> GetByIdAsync(NPA.Core.Core.CompositeKey key)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (key == null) throw new ArgumentNullException(nameof(key));");
        sb.AppendLine($"            return await _entityManager.FindAsync<{entityType}>(key);");
        sb.AppendLine("        }");
        sb.AppendLine();

        // DeleteAsync(CompositeKey)
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Deletes an entity by its composite key asynchronously.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <param name=\"key\">The composite key.</param>");
        sb.AppendLine($"        public async Task DeleteAsync(NPA.Core.Core.CompositeKey key)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (key == null) throw new ArgumentNullException(nameof(key));");
        sb.AppendLine($"            await _entityManager.RemoveAsync<{entityType}>(key);");
        sb.AppendLine("        }");
        sb.AppendLine();

        // ExistsAsync(CompositeKey)
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Checks if an entity exists by its composite key asynchronously.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <param name=\"key\">The composite key.");
        sb.AppendLine("        /// <returns>True if the entity exists; otherwise, false.</returns>");
        sb.AppendLine($"        public async Task<bool> ExistsAsync(NPA.Core.Core.CompositeKey key)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (key == null) throw new ArgumentNullException(nameof(key));");
        sb.AppendLine("            var entity = await GetByIdAsync(key);");
        sb.AppendLine("            return entity != null;");
        sb.AppendLine("        }");
        sb.AppendLine();

        // FindByCompositeKey (individual parameters)
        var keyParams = string.Join(", ", info.CompositeKeyProperties.Select((prop, i) => $"object {StringHelper.ToCamelCase(prop)}"));

        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Finds an entity by its composite key components asynchronously.");
        sb.AppendLine("        /// </summary>");
        foreach (var prop in info.CompositeKeyProperties)
        {
            sb.AppendLine($"        /// <param name=\"{StringHelper.ToCamelCase(prop)}\">The {prop} component of the composite key.</param>");
        }
        sb.AppendLine("        /// <returns>The entity if found; otherwise, null.</returns>");
        sb.AppendLine($"        public async Task<{entityType}?> FindByCompositeKeyAsync({keyParams})");
        sb.AppendLine("        {");
        sb.AppendLine("            var key = new NPA.Core.Core.CompositeKey();");
        foreach (var prop in info.CompositeKeyProperties)
        {
            sb.AppendLine($"            key.SetValue(\"{prop}\", {StringHelper.ToCamelCase(prop)});");
        }
        sb.AppendLine("            return await GetByIdAsync(key);");
        sb.AppendLine("        }");
        sb.AppendLine();

        sb.AppendLine("        #endregion");

        return sb.ToString();
    }
}

