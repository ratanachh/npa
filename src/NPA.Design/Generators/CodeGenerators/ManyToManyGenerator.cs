using System.Linq;
using System.Text;
using NPA.Design.Models;
using NPA.Design.Generators.Helpers;

namespace NPA.Design.Generators.CodeGenerators;

/// <summary>
/// Generates many-to-many relationship methods for repositories.
/// </summary>
internal static class ManyToManyGenerator
{
    /// <summary>
    /// Generates many-to-many relationship methods.
    /// </summary>
    public static string GenerateManyToManyMethods(RepositoryInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("        #region Many-to-Many Relationship Methods");
        sb.AppendLine();

        foreach (var relationship in info.ManyToManyRelationships)
        {
            var entityName = info.EntityType.Split('.').Last();
            var relatedName = relationship.CollectionElementType.Split('.').Last();
            var joinTable = string.IsNullOrEmpty(relationship.JoinTableSchema)
                ? relationship.JoinTableName
                : $"{relationship.JoinTableSchema}.{relationship.JoinTableName}";

            // Determine key columns with defaults if not specified
            var ownerKeyColumn = relationship.JoinColumns.FirstOrDefault() ?? $"{entityName}Id";
            var targetKeyColumn = relationship.InverseJoinColumns.FirstOrDefault() ?? $"{relatedName}Id";

            // Get{Related}Async method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets all {relationship.PropertyName} for a {entityName} asynchronously.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{StringHelper.ToCamelCase(entityName)}Id\">The {entityName} identifier.</param>");
            sb.AppendLine($"        /// <returns>A collection of {relatedName} entities.</returns>");
            sb.AppendLine($"        public async Task<IEnumerable<{relationship.CollectionElementType}>> Get{relationship.PropertyName}Async({info.KeyType} {StringHelper.ToCamelCase(entityName)}Id)");
            sb.AppendLine("        {");
            // Get the related entity's key column name from metadata (for SQL, not property name)
            var relatedEntityTypeName = relationship.CollectionElementType.Split('.').Last();
            var relatedKeyColumnName = MetadataHelper.GetKeyColumnName(info, relatedEntityTypeName);
            sb.AppendLine($"            var sql = @\"");
            sb.AppendLine($"                SELECT r.*");
            sb.AppendLine($"                FROM {joinTable} jt");
            sb.AppendLine($"                INNER JOIN {relatedName} r ON jt.{targetKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE jt.{ownerKeyColumn} = @{entityName}Id\";");
            sb.AppendLine();
            sb.AppendLine($"            return await _connection.QueryAsync<{relationship.CollectionElementType}>(");
            sb.AppendLine($"                sql,");
            sb.AppendLine($"                new {{ {entityName}Id = {StringHelper.ToCamelCase(entityName)}Id }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Add{Related}Async method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Adds a relationship between a {entityName} and a {relatedName} asynchronously.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{StringHelper.ToCamelCase(entityName)}Id\">The {entityName} identifier.</param>");
            sb.AppendLine($"        /// <param name=\"{StringHelper.ToCamelCase(relatedName)}Id\">The {relatedName} identifier.</param>");
            sb.AppendLine($"        public async Task Add{relatedName}Async({info.KeyType} {StringHelper.ToCamelCase(entityName)}Id, {info.KeyType} {StringHelper.ToCamelCase(relatedName)}Id)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"INSERT INTO {joinTable} ({ownerKeyColumn}, {targetKeyColumn}) VALUES (@{entityName}Id, @{relatedName}Id)\";");
            sb.AppendLine();
            sb.AppendLine("            await _connection.ExecuteAsync(");
            sb.AppendLine("                sql,");
            sb.AppendLine($"                new {{ {entityName}Id = {StringHelper.ToCamelCase(entityName)}Id, {relatedName}Id = {StringHelper.ToCamelCase(relatedName)}Id }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Remove{Related}Async method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Removes a relationship between a {entityName} and a {relatedName} asynchronously.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{StringHelper.ToCamelCase(entityName)}Id\">The {entityName} identifier.</param>");
            sb.AppendLine($"        /// <param name=\"{StringHelper.ToCamelCase(relatedName)}Id\">The {relatedName} identifier.</param>");
            sb.AppendLine($"        public async Task Remove{relatedName}Async({info.KeyType} {StringHelper.ToCamelCase(entityName)}Id, {info.KeyType} {StringHelper.ToCamelCase(relatedName)}Id)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"DELETE FROM {joinTable} WHERE {ownerKeyColumn} = @{entityName}Id AND {targetKeyColumn} = @{relatedName}Id\";");
            sb.AppendLine();
            sb.AppendLine("            await _connection.ExecuteAsync(");
            sb.AppendLine("                sql,");
            sb.AppendLine($"                new {{ {entityName}Id = {StringHelper.ToCamelCase(entityName)}Id, {relatedName}Id = {StringHelper.ToCamelCase(relatedName)}Id }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Has{Related}Async method (existence check)
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Checks if a relationship exists between a {entityName} and a {relatedName} asynchronously.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{StringHelper.ToCamelCase(entityName)}Id\">The {entityName} identifier.</param>");
            sb.AppendLine($"        /// <param name=\"{StringHelper.ToCamelCase(relatedName)}Id\">The {relatedName} identifier.</param>");
            sb.AppendLine($"        /// <returns>True if the relationship exists; otherwise, false.</returns>");
            sb.AppendLine($"        public async Task<bool> Has{relatedName}Async({info.KeyType} {StringHelper.ToCamelCase(entityName)}Id, {info.KeyType} {StringHelper.ToCamelCase(relatedName)}Id)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"SELECT COUNT(1) FROM {joinTable} WHERE {ownerKeyColumn} = @{entityName}Id AND {targetKeyColumn} = @{relatedName}Id\";");
            sb.AppendLine();
            sb.AppendLine("            var count = await _connection.ExecuteScalarAsync<int>(");
            sb.AppendLine("                sql,");
            sb.AppendLine($"                new {{ {entityName}Id = {StringHelper.ToCamelCase(entityName)}Id, {relatedName}Id = {StringHelper.ToCamelCase(relatedName)}Id }});");
            sb.AppendLine();
            sb.AppendLine("            return count > 0;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        sb.AppendLine("        #endregion");

        return sb.ToString();
    }
}

