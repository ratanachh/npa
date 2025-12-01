using System.Collections.Generic;
using System.Linq;
using System.Text;
using NPA.Design.Models;
using NPA.Design.Generators.Helpers;

namespace NPA.Design.Generators.CodeGenerators;

/// <summary>
/// Generates eager loading overrides for repositories.
/// </summary>
internal static class EagerLoadingGenerator
{
    /// <summary>
    /// Generates eager loading overrides for GetByIdAsync, GetAllAsync, and GetByIdsAsync.
    /// </summary>
    public static string GenerateEagerLoadingOverrides(RepositoryInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("        #region Eager Loading Overrides");
        sb.AppendLine();

        var entityName = info.EntityType.Split('.').Last();
        var eagerRelationships = info.EagerRelationships;

        if (eagerRelationships.Count == 0)
        {
            sb.AppendLine("        #endregion");
            return sb.ToString();
        }

        // Check if we have only single-entity relationships or also collections
        var hasSingleOnly = eagerRelationships.All(r => !r.IsCollection);
        var hasCollections = eagerRelationships.Any(r => r.IsCollection);

        if (hasSingleOnly)
        {
            // Simple case: only ManyToOne or OneToOne relationships
            GenerateSimpleEagerGetByIdOverride(sb, info, entityName, eagerRelationships);
        }
        else
        {
            // Complex case: has collections - need multiple queries or careful JOIN
            GenerateComplexEagerGetByIdOverride(sb, info, entityName, eagerRelationships);
        }

        // Generate GetAllAsync override
        GenerateEagerGetAllOverride(sb, info, entityName, eagerRelationships);

        // Generate GetByIdsAsync for batch loading
        GenerateBatchLoadingMethod(sb, info, entityName, eagerRelationships);

        sb.AppendLine("        #endregion");
        return sb.ToString();
    }

    private static void GenerateSimpleEagerGetByIdOverride(StringBuilder sb, RepositoryInfo info, string entityName, List<RelationshipMetadata> eagerRelationships)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets a {entityName} by its ID with eager relationships loaded asynchronously.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <param name=\"id\">The {entityName} identifier.</param>");
        sb.AppendLine($"        /// <returns>The {entityName} with eager relationships loaded if found; otherwise, null.</returns>");
        sb.AppendLine($"        public override async Task<{info.EntityType}?> GetByIdAsync({info.KeyType} id)");
        sb.AppendLine("        {");

        // Get actual table name from metadata
        var entityTableName = info.EntityMetadata?.TableName ?? entityName;

        // Build SQL with JOINs for all eager relationships
        var keyPropertyName = MetadataHelper.GetKeyPropertyName(info);
        var sqlBuilder = new StringBuilder($"SELECT e.*");
        var joins = new List<string>();
        var splitOns = new List<string> { keyPropertyName };
        var typeParams = new List<string> { info.EntityType };
        var aliases = new Dictionary<string, string> { { entityName, "e" } };
        int aliasCounter = 0;

        foreach (var relationship in eagerRelationships)
        {
            if (relationship.IsCollection) continue; // Skip collections in simple case

            var alias = $"r{aliasCounter++}";
            var relatedTypeName = relationship.TargetEntityType;
            var relatedTableName = MetadataHelper.GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? StringHelper.Pluralize(relationship.TargetEntityType.Split('.').Last());
            var foreignKeyColumn = relationship.JoinColumn?.Name ?? $"{relationship.TargetEntityType.Split('.').Last()}Id";

            var relatedKeyPropertyName = MetadataHelper.GetKeyPropertyName(info, relationship.TargetEntityType);
            aliases[relatedTypeName] = alias;
            sqlBuilder.Append($", {alias}.*");
            joins.Add($"LEFT JOIN {relatedTableName} {alias} ON e.{foreignKeyColumn} = {alias}.{relatedKeyPropertyName}");
            splitOns.Add(relatedKeyPropertyName);
            typeParams.Add(relationship.TargetEntityFullType);
        }

        sqlBuilder.Append($" FROM {entityTableName} e");
        foreach (var join in joins)
        {
            sqlBuilder.Append($" {join}");
        }
        sqlBuilder.Append($" WHERE e.{keyPropertyName} = @Id");

        sb.AppendLine($"            var sql = @\"{sqlBuilder}\";");
        sb.AppendLine();

        // Generate Dapper query with multi-mapping
        if (eagerRelationships.Count == 1)
        {
            var rel = eagerRelationships[0];
            sb.AppendLine($"            var result = await _connection.QueryAsync<{info.EntityType}, {rel.TargetEntityFullType}, {info.EntityType}>(");
            sb.AppendLine($"                sql,");
            sb.AppendLine($"                (entity, related) => {{ entity.{rel.PropertyName} = related; return entity; }},");
            sb.AppendLine($"                new {{ Id = id }},");
            sb.AppendLine($"                splitOn: \"{string.Join(",", splitOns.Skip(1))}\");");
        }
        else if (eagerRelationships.Count == 2)
        {
            var rel1 = eagerRelationships[0];
            var rel2 = eagerRelationships[1];
            sb.AppendLine($"            var result = await _connection.QueryAsync<{info.EntityType}, {rel1.TargetEntityFullType}, {rel2.TargetEntityFullType}, {info.EntityType}>(");
            sb.AppendLine($"                sql,");
            sb.AppendLine($"                (entity, related1, related2) => {{ entity.{rel1.PropertyName} = related1; entity.{rel2.PropertyName} = related2; return entity; }},");
            sb.AppendLine($"                new {{ Id = id }},");
            sb.AppendLine($"                splitOn: \"{string.Join(",", splitOns.Skip(1))}\");");
        }
        else
        {
            // Fall back to multiple queries for more than 2 relationships
            sb.AppendLine($"            var entity = await base.GetByIdAsync(id);");
            sb.AppendLine($"            if (entity == null) return null;");
            sb.AppendLine();
            foreach (var rel in eagerRelationships)
            {
                sb.AppendLine($"            entity.{rel.PropertyName} = await GetByIdWith{rel.PropertyName}Async(id);");
            }
        }

        sb.AppendLine("            return result.FirstOrDefault();");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateComplexEagerGetByIdOverride(StringBuilder sb, RepositoryInfo info, string entityName, List<RelationshipMetadata> eagerRelationships)
    {
        // When we have collections, we need to use separate queries to avoid cartesian product
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets a {entityName} by its ID with eager relationships loaded asynchronously.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <param name=\"id\">The {entityName} identifier.</param>");
        sb.AppendLine($"        /// <returns>The {entityName} with eager relationships loaded if found; otherwise, null.</returns>");
        sb.AppendLine($"        public override async Task<{info.EntityType}?> GetByIdAsync({info.KeyType} id)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var entity = await base.GetByIdAsync(id);");
        sb.AppendLine($"            if (entity == null) return null;");
        sb.AppendLine();

        // Load each eager relationship
        foreach (var rel in eagerRelationships)
        {
            if (rel.IsCollection)
            {
                var foreignKeyColumn = rel.JoinColumn?.Name ?? $"{entityName}Id";
                var relatedTableName = MetadataHelper.GetTableNameFromMetadata(info, rel.TargetEntityType) ?? StringHelper.Pluralize(rel.TargetEntityType.Split('.').Last());
                sb.AppendLine($"            // Load {rel.PropertyName} collection");
                sb.AppendLine($"            var {rel.PropertyName.ToLower()}Sql = @\"SELECT * FROM {relatedTableName} WHERE {foreignKeyColumn} = @Id\";");
                sb.AppendLine($"            entity.{rel.PropertyName} = (await _connection.QueryAsync<{rel.TargetEntityFullType}>({rel.PropertyName.ToLower()}Sql, new {{ Id = id }})).ToList();");
                sb.AppendLine();
            }
            else
            {
                var foreignKeyColumn = rel.JoinColumn?.Name ?? $"{rel.TargetEntityType.Split('.').Last()}Id";
                var hasFkProperty = MetadataHelper.HasProperty(info, foreignKeyColumn);
                var isFkNullable = MetadataHelper.IsPropertyNullable(info, foreignKeyColumn);
                var relatedKeyType = MetadataHelper.GetRelatedEntityKeyType(info, rel.TargetEntityType);
                var nullCheck = isFkNullable ? "!= null" : $"!= default({relatedKeyType})";

                var relatedKeyPropertyName = MetadataHelper.GetKeyPropertyName(info, rel.TargetEntityType);
                var relatedTableName = MetadataHelper.GetTableNameFromMetadata(info, rel.TargetEntityType) ?? StringHelper.Pluralize(rel.TargetEntityType.Split('.').Last());
                sb.AppendLine($"            // Load {rel.PropertyName}");
                sb.AppendLine($"            var {rel.PropertyName.ToLower()}Sql = @\"SELECT r.* FROM {relatedTableName} r WHERE r.{relatedKeyPropertyName} = @ForeignKeyId\";");
                
                if (hasFkProperty)
                {
                    var foreignKeyProperty = MetadataHelper.GetPropertyNameForColumn(info, foreignKeyColumn);
                    sb.AppendLine($"            var {rel.PropertyName.ToLower()}FkValue = entity.{foreignKeyProperty};");
                    sb.AppendLine($"            if ({rel.PropertyName.ToLower()}FkValue {nullCheck})");
                }
                else
                {
                    // Use navigation property's key if FK property doesn't exist
                    var defaultKeyValue = relatedKeyType == "Guid" ? "Guid.Empty" : relatedKeyType == "int" ? "0" : relatedKeyType == "long" ? "0L" : $"default({relatedKeyType})";
                    sb.AppendLine($"            var {rel.PropertyName.ToLower()}FkValue = entity.{rel.PropertyName}?.{relatedKeyPropertyName} ?? {defaultKeyValue};");
                    sb.AppendLine($"            if (entity.{rel.PropertyName} != null)");
                }
                
                sb.AppendLine($"            {{");
                sb.AppendLine($"                entity.{rel.PropertyName} = (await _connection.QueryAsync<{rel.TargetEntityFullType}>({rel.PropertyName.ToLower()}Sql, new {{ ForeignKeyId = {rel.PropertyName.ToLower()}FkValue }})).FirstOrDefault();");
                sb.AppendLine($"            }}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("            return entity;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateEagerGetAllOverride(StringBuilder sb, RepositoryInfo info, string entityName, List<RelationshipMetadata> eagerRelationships)
    {
        // GetAllAsync with eager loading
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets all {entityName} entities with eager relationships loaded asynchronously.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <returns>A collection of {entityName} entities with eager relationships loaded.</returns>");
        sb.AppendLine($"        public override async Task<IEnumerable<{info.EntityType}>> GetAllAsync()");
        sb.AppendLine("        {");
        sb.AppendLine($"            var entities = (await base.GetAllAsync()).ToList();");
        sb.AppendLine($"            if (!entities.Any()) return entities;");
        sb.AppendLine();

        // Load relationships for all entities
        foreach (var rel in eagerRelationships)
        {
            if (rel.IsCollection)
            {
                var foreignKeyColumn = rel.JoinColumn?.Name ?? $"{entityName}Id";
                var foreignKeyProperty = MetadataHelper.GetPropertyNameForColumn(info, foreignKeyColumn, rel.TargetEntityType);
                var relatedTableName = MetadataHelper.GetTableNameFromMetadata(info, rel.TargetEntityType) ?? StringHelper.Pluralize(rel.TargetEntityType.Split('.').Last());

                var keyPropertyName = MetadataHelper.GetKeyPropertyName(info);
                sb.AppendLine($"            // Load {rel.PropertyName} for all entities");
                sb.AppendLine($"            var ids = entities.Select(e => e.{keyPropertyName}).ToArray();");
                sb.AppendLine($"            var {rel.PropertyName.ToLower()}Sql = @\"SELECT * FROM {relatedTableName} WHERE {foreignKeyColumn} IN @Ids\";");
                sb.AppendLine($"            var all{rel.PropertyName} = (await _connection.QueryAsync<{rel.TargetEntityFullType}>({rel.PropertyName.ToLower()}Sql, new {{ Ids = ids }})).ToList();");
                sb.AppendLine($"            var {rel.PropertyName.ToLower()}ByEntity = all{rel.PropertyName}.GroupBy(r => r.{foreignKeyProperty}).ToDictionary(g => g.Key, g => g.ToList());");
                sb.AppendLine($"            foreach (var entity in entities)");
                sb.AppendLine($"            {{");
                sb.AppendLine($"                if ({rel.PropertyName.ToLower()}ByEntity.TryGetValue(entity.{keyPropertyName}, out var items))");
                sb.AppendLine($"                    entity.{rel.PropertyName} = items;");
                sb.AppendLine($"            }}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("            return entities;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateBatchLoadingMethod(StringBuilder sb, RepositoryInfo info, string entityName, List<RelationshipMetadata> eagerRelationships)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets multiple {entityName} entities by their IDs with eager relationships loaded asynchronously.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <param name=\"ids\">The collection of {entityName} identifiers.</param>");
        sb.AppendLine($"        /// <returns>A collection of {entityName} entities with eager relationships loaded.</returns>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> GetByIdsAsync(IEnumerable<{info.KeyType}> ids)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var idArray = ids.ToArray();");
        sb.AppendLine($"            if (!idArray.Any()) return Enumerable.Empty<{info.EntityType}>();");
        sb.AppendLine();

        // Get actual table name from metadata
        var entityTableName = info.EntityMetadata?.TableName ?? entityName;

        var keyPropertyName = MetadataHelper.GetKeyPropertyName(info);
        sb.AppendLine($"            var sql = @\"SELECT * FROM {entityTableName} WHERE {keyPropertyName} IN @Ids\";");
        sb.AppendLine($"            var entities = (await _connection.QueryAsync<{info.EntityType}>(sql, new {{ Ids = idArray }})).ToList();");
        sb.AppendLine($"            if (!entities.Any()) return entities;");
        sb.AppendLine();

        // Load relationships for all entities
        foreach (var rel in eagerRelationships)
        {
            if (rel.IsCollection)
            {
                var foreignKeyColumn = rel.JoinColumn?.Name ?? $"{entityName}Id";
                var foreignKeyProperty = MetadataHelper.GetPropertyNameForColumn(info, foreignKeyColumn, rel.TargetEntityType);
                var relatedTableName = MetadataHelper.GetTableNameFromMetadata(info, rel.TargetEntityType) ?? StringHelper.Pluralize(rel.TargetEntityType.Split('.').Last());

                sb.AppendLine($"            // Load {rel.PropertyName} for all entities");
                sb.AppendLine($"            var {rel.PropertyName.ToLower()}Sql = @\"SELECT * FROM {relatedTableName} WHERE {foreignKeyColumn} IN @Ids\";");
                sb.AppendLine($"            var all{rel.PropertyName} = (await _connection.QueryAsync<{rel.TargetEntityFullType}>({rel.PropertyName.ToLower()}Sql, new {{ Ids = idArray }})).ToList();");
                sb.AppendLine($"            var {rel.PropertyName.ToLower()}ByEntity = all{rel.PropertyName}.GroupBy(r => r.{foreignKeyProperty}).ToDictionary(g => g.Key, g => g.ToList());");
                sb.AppendLine($"            foreach (var entity in entities)");
                sb.AppendLine($"            {{");
                sb.AppendLine($"                if ({rel.PropertyName.ToLower()}ByEntity.TryGetValue(entity.{keyPropertyName}, out var items))");
                sb.AppendLine($"                    entity.{rel.PropertyName} = items;");
                sb.AppendLine($"            }}");
                sb.AppendLine();
            }
            else
            {
                var foreignKeyColumn = rel.JoinColumn?.Name ?? $"{rel.TargetEntityType}Id";
                var foreignKeyProperty = MetadataHelper.GetPropertyNameForColumn(info, foreignKeyColumn);
                var relatedTableName = MetadataHelper.GetTableNameFromMetadata(info, rel.TargetEntityType) ?? StringHelper.Pluralize(rel.TargetEntityType.Split('.').Last());
                var isFkNullable = MetadataHelper.IsPropertyNullable(info, foreignKeyColumn);
                var relatedKeyType = MetadataHelper.GetRelatedEntityKeyType(info, rel.TargetEntityType);
                var fkPropertyType = MetadataHelper.GetForeignKeyPropertyType(info, foreignKeyColumn);
                var nullCheck = isFkNullable ? "!= null" : $"!= default({relatedKeyType})";
                var fkValueCheck = isFkNullable ? "fkValue != null && " : "";
                // Cast is needed if FK type differs from related entity's key type, regardless of nullability
                var needsCast = fkPropertyType != null && fkPropertyType != relatedKeyType;
                var fkValueCast = needsCast ? $"({relatedKeyType})fkValue" : "fkValue";

                sb.AppendLine($"            // Load {rel.PropertyName} for all entities");
                sb.AppendLine($"            var {rel.PropertyName.ToLower()}Ids = entities.Select(e => e.{foreignKeyProperty}).Where(v => v {nullCheck}).Distinct().ToArray();");
                sb.AppendLine($"            if ({rel.PropertyName.ToLower()}Ids.Any())");
                sb.AppendLine($"            {{");
                var relatedKeyPropertyName = MetadataHelper.GetKeyPropertyName(info, rel.TargetEntityType);
                sb.AppendLine($"                var {rel.PropertyName.ToLower()}Sql = @\"SELECT * FROM {relatedTableName} WHERE {relatedKeyPropertyName} IN @Ids\";");
                sb.AppendLine($"                var all{rel.PropertyName} = (await _connection.QueryAsync<{rel.TargetEntityFullType}>({rel.PropertyName.ToLower()}Sql, new {{ Ids = {rel.PropertyName.ToLower()}Ids }})).ToDictionary(r => r.{relatedKeyPropertyName});");
                sb.AppendLine($"                foreach (var entity in entities)");
                sb.AppendLine($"                {{");
                sb.AppendLine($"                    var fkValue = entity.{foreignKeyProperty};");
                sb.AppendLine($"                    if ({fkValueCheck}all{rel.PropertyName}.TryGetValue({fkValueCast}, out var related))");
                sb.AppendLine($"                        entity.{rel.PropertyName} = related;");
                sb.AppendLine($"                }}");
                sb.AppendLine($"            }}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("            return entities;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }
}

