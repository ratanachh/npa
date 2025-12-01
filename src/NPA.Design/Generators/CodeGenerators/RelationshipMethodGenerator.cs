using System.Collections.Generic;
using System.Linq;
using System.Text;
using NPA.Design.Models;
using NPA.Design.Generators.Helpers;

namespace NPA.Design.Generators.CodeGenerators;

/// <summary>
/// Generates relationship-aware methods for repositories (GetByIdWith, Load methods).
/// </summary>
internal static class RelationshipMethodGenerator
{
    /// <summary>
    /// Generates relationship-aware methods like GetByIdWith{Property}Async and Load{Property}Async.
    /// </summary>
    public static string GenerateRelationshipAwareMethods(RepositoryInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("        #region Relationship-Aware Methods");
        sb.AppendLine();

        var entityName = info.EntityType.Split('.').Last();

        foreach (var relationship in info.Relationships)
        {
            var relatedTypeName = relationship.TargetEntityType;
            var relatedTypeFullName = relationship.TargetEntityFullType;
            var propertyName = relationship.PropertyName;

            // Generate GetByIdWith{Property}Async method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets a {entityName} by its ID with {propertyName} loaded asynchronously.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"id\">The {entityName} identifier.</param>");
            sb.AppendLine($"        /// <returns>The {entityName} with {propertyName} loaded if found; otherwise, null.</returns>");
            sb.AppendLine($"        public async Task<{info.EntityType}?> GetByIdWith{propertyName}Async({info.KeyType} id)");
            sb.AppendLine("        {");

            if (relationship.IsCollection)
            {
                GenerateOneToManyLoadSQL(sb, info, relationship, entityName, relatedTypeName);
            }
            else if (relationship.Type == RelationshipType.ManyToOne)
            {
                GenerateManyToOneLoadSQL(sb, info, relationship, entityName, relatedTypeName);
            }
            else if (relationship.Type == RelationshipType.OneToOne)
            {
                GenerateOneToOneLoadSQL(sb, info, relationship, entityName, relatedTypeName);
            }

            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate Load{Property}Async for lazy loading
            if (relationship.FetchType == FetchType.Lazy)
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Loads {propertyName} for an existing {entityName} entity asynchronously.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        /// <param name=\"entity\">The {entityName} entity.</param>");

                if (relationship.IsCollection)
                {
                    sb.AppendLine($"        /// <returns>A collection of {relatedTypeName} entities.</returns>");
                    sb.AppendLine($"        public async Task<IEnumerable<{relatedTypeFullName}>> Load{propertyName}Async({info.EntityType} entity)");
                }
                else
                {
                    sb.AppendLine($"        /// <returns>The loaded {relatedTypeName} entity if found; otherwise, null.</returns>");
                    // Remove trailing ? if already present to avoid double nullable marker
                    var returnType = relatedTypeFullName.TrimEnd('?');
                    sb.AppendLine($"        public async Task<{returnType}?> Load{propertyName}Async({info.EntityType} entity)");
                }

                sb.AppendLine("        {");
                sb.AppendLine("            if (entity == null) throw new ArgumentNullException(nameof(entity));");
                sb.AppendLine();

                if (relationship.IsCollection)
                {
                    GenerateLazyLoadCollectionSQL(sb, info, relationship, relatedTypeName);
                }
                else
                {
                    GenerateLazyLoadSingleSQL(sb, info, relationship, relatedTypeName);
                }

                sb.AppendLine("        }");
                sb.AppendLine();
            }
        }

        sb.AppendLine("        #endregion");
        return sb.ToString();
    }

    private static void GenerateManyToOneLoadSQL(StringBuilder sb, RepositoryInfo info, RelationshipMetadata relationship, string entityName, string relatedTypeName)
    {
        var foreignKeyColumn = relationship.JoinColumn?.Name ?? $"{relatedTypeName}Id";

        // Get actual table names from metadata
        var entityTableName = info.EntityMetadata?.TableName ?? entityName;
        var relatedTableName = MetadataHelper.GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relatedTypeName;
        var keyPropertyName = MetadataHelper.GetKeyPropertyName(info);
        var relatedKeyPropertyName = MetadataHelper.GetKeyPropertyName(info, relationship.TargetEntityType);

        sb.AppendLine($"            var sql = @\"SELECT e.*, r.* FROM {entityTableName} e LEFT JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyPropertyName} WHERE e.{keyPropertyName} = @Id\";");
        sb.AppendLine($"            var result = await _connection.QueryAsync<{info.EntityType}, {relationship.TargetEntityFullType}, {info.EntityType}>(sql, (entity, related) => {{ entity.{relationship.PropertyName} = related; return entity; }}, new {{ Id = id }}, splitOn: \"{relatedKeyPropertyName}\");");
        sb.AppendLine("            return result.FirstOrDefault();");
    }

    private static void GenerateOneToOneLoadSQL(StringBuilder sb, RepositoryInfo info, RelationshipMetadata relationship, string entityName, string relatedTypeName)
    {
        var foreignKeyColumn = relationship.JoinColumn?.Name ?? $"{relatedTypeName}Id";

        // Get actual table names from metadata
        var entityTableName = info.EntityMetadata?.TableName ?? entityName;
        var relatedTableName = MetadataHelper.GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relatedTypeName;
        var keyPropertyName = MetadataHelper.GetKeyPropertyName(info);
        var relatedKeyPropertyName = MetadataHelper.GetKeyPropertyName(info, relationship.TargetEntityType);

        sb.AppendLine($"            var sql = @\"SELECT e.*, r.* FROM {entityTableName} e LEFT JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyPropertyName} WHERE e.{keyPropertyName} = @Id\";");
        sb.AppendLine($"            var result = await _connection.QueryAsync<{info.EntityType}, {relationship.TargetEntityFullType}, {info.EntityType}>(sql, (entity, related) => {{ entity.{relationship.PropertyName} = related; return entity; }}, new {{ Id = id }}, splitOn: \"{relatedKeyPropertyName}\");");
        sb.AppendLine("            return result.FirstOrDefault();");
    }

    private static void GenerateOneToManyLoadSQL(StringBuilder sb, RepositoryInfo info, RelationshipMetadata relationship, string entityName, string relatedTypeName)
    {
        var foreignKeyColumn = relationship.JoinColumn?.Name ?? $"{entityName}Id";

        // Get actual table names from metadata
        var entityTableName = info.EntityMetadata?.TableName ?? entityName;
        var relatedTableName = MetadataHelper.GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relatedTypeName;

        var keyPropertyName = MetadataHelper.GetKeyPropertyName(info);
        sb.AppendLine($"            var entityDict = new Dictionary<{info.KeyType}, {info.EntityType}>();");
        sb.AppendLine($"            var sql = @\"SELECT e.*, r.* FROM {entityTableName} e LEFT JOIN {relatedTableName} r ON e.{keyPropertyName} = r.{foreignKeyColumn} WHERE e.{keyPropertyName} = @Id\";");
        sb.AppendLine($"            await _connection.QueryAsync<{info.EntityType}, {relationship.TargetEntityFullType}, {info.EntityType}>(sql, (entity, related) => {{");
        sb.AppendLine($"                if (!entityDict.TryGetValue(entity.{keyPropertyName}, out var existingEntity)) {{");
        sb.AppendLine($"                    existingEntity = entity;");
        sb.AppendLine($"                    existingEntity.{relationship.PropertyName} = new List<{relationship.TargetEntityFullType}>();");
        sb.AppendLine($"                    entityDict[entity.{keyPropertyName}] = existingEntity;");
        sb.AppendLine($"                }}");
        sb.AppendLine($"                if (related != null) ((List<{relationship.TargetEntityFullType}>)existingEntity.{relationship.PropertyName}).Add(related);");
        sb.AppendLine($"                return existingEntity;");
        sb.AppendLine($"            }}, new {{ Id = id }}, splitOn: \"{keyPropertyName}\");");
        sb.AppendLine("            return entityDict.Values.FirstOrDefault();");
    }

    private static void GenerateLazyLoadCollectionSQL(StringBuilder sb, RepositoryInfo info, RelationshipMetadata relationship, string relatedTypeName)
    {
        // Get actual table name from metadata
        var relatedTableName = MetadataHelper.GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relatedTypeName;
        var keyPropertyName = MetadataHelper.GetKeyPropertyName(info);
        var fkColumnName = relationship.JoinColumn?.Name ?? $"{info.EntityType.Split('.').Last()}Id";

        sb.AppendLine($"            var sql = @\"SELECT * FROM {relatedTableName} WHERE {fkColumnName} = @Id\";");
        sb.AppendLine($"            return await _connection.QueryAsync<{relationship.TargetEntityFullType}>(sql, new {{ Id = entity.{keyPropertyName} }});");
    }

    private static void GenerateLazyLoadSingleSQL(StringBuilder sb, RepositoryInfo info, RelationshipMetadata relationship, string relatedTypeName)
    {
        // Get actual table name from metadata
        var relatedTableName = MetadataHelper.GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relatedTypeName;
        var entityName = info.EntityType.Split('.').Last();

        sb.AppendLine($"            // Lazy load {relationship.PropertyName}");

        var keyPropertyName = MetadataHelper.GetKeyPropertyName(info);
        
        if (relationship.Type == RelationshipType.OneToOne && !string.IsNullOrEmpty(relationship.MappedBy))
        {
            // Owner side of OneToOne with MappedBy - query by owner's ID on inverse side's FK
            var inverseFkColumn = relationship.JoinColumn?.Name ?? $"{entityName}Id";
            sb.AppendLine($"            var sql = @\"SELECT * FROM {relatedTableName} WHERE {inverseFkColumn} = @Id\";");
            sb.AppendLine($"            var result = await _connection.QueryAsync<{relationship.TargetEntityFullType}>(sql, new {{ Id = entity.{keyPropertyName} }});");
        }
        else
        {
            // ManyToOne or inverse side of OneToOne - query by FK on current entity
            var fkColumnName = relationship.JoinColumn?.Name ?? $"{relatedTypeName}Id";
            var hasFkProperty = MetadataHelper.HasProperty(info, fkColumnName);
            var relatedKeyPropertyName = MetadataHelper.GetKeyPropertyName(info, relationship.TargetEntityType);
            sb.AppendLine($"            var sql = @\"SELECT * FROM {relatedTableName} WHERE {relatedKeyPropertyName} = @Id\";");
            
            if (hasFkProperty)
            {
                var fkPropertyName = MetadataHelper.GetPropertyNameForColumn(info, fkColumnName);
                sb.AppendLine($"            var result = await _connection.QueryAsync<{relationship.TargetEntityFullType}>(sql, new {{ Id = entity.{fkPropertyName} }});");
            }
            else
            {
                // Use navigation property's key if FK property doesn't exist
                var relatedKeyType = MetadataHelper.GetRelatedEntityKeyType(info, relationship.TargetEntityType);
                var defaultKeyValue = relatedKeyType == "Guid" ? "Guid.Empty" : relatedKeyType == "int" ? "0" : relatedKeyType == "long" ? "0L" : $"default({relatedKeyType})";
                sb.AppendLine($"            var result = await _connection.QueryAsync<{relationship.TargetEntityFullType}>(sql, new {{ Id = entity.{relationship.PropertyName}?.{relatedKeyPropertyName} ?? {defaultKeyValue} }});");
            }
        }

        sb.AppendLine("            return result.FirstOrDefault();");
    }
}

