using System.Collections.Generic;
using System.Linq;
using System.Text;
using NPA.Design.Models;
using NPA.Design.Generators.Helpers;

namespace NPA.Design.Generators.CodeGenerators;

/// <summary>
/// Generates orphan removal support for repository UpdateAsync method.
/// </summary>
internal static class OrphanRemovalGenerator
{
    /// <summary>
    /// Generates orphan removal override for UpdateAsync method.
    /// </summary>
    public static string GenerateOrphanRemovalUpdateOverride(RepositoryInfo info)
    {
        var sb = new StringBuilder();
        var orphanRemovalRelationships = info.OrphanRemovalRelationships;

        sb.AppendLine("        #region Orphan Removal Support");
        sb.AppendLine();
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Updates an entity with orphan removal support.");
        sb.AppendLine($"        /// Automatically deletes orphaned child entities that are no longer referenced.");
        sb.AppendLine($"        /// Orphan removal enabled for: {string.Join(", ", orphanRemovalRelationships.Select(r => r.PropertyName))}");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public override async Task UpdateAsync({info.EntityType} entity)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            if (entity == null) throw new ArgumentNullException(nameof(entity));");
        sb.AppendLine();
        
        var keyPropertyName = MetadataHelper.GetKeyPropertyName(info);
        
        // Load existing entity with relationships to compare
        sb.AppendLine($"            // Load existing entity to detect orphaned relationships");
        sb.AppendLine($"            var existing = await GetByIdAsync(entity.{keyPropertyName});");
        sb.AppendLine($"            if (existing == null)");
        sb.AppendLine($"                throw new InvalidOperationException($\"{info.EntityType} with id {{entity.{keyPropertyName}}} not found\");");
        sb.AppendLine();

        // Process each orphan removal relationship
        // Note: ManyToOne relationships are NOT supported for orphan removal because:
        // - They are the inverse side of OneToMany (the "many" side)
        // - Removing the relationship only sets the FK to null, doesn't delete the parent
        // - The parent entity should not be deleted when a child removes the reference
        foreach (var rel in orphanRemovalRelationships)
        {
            if (rel.Type == RelationshipType.ManyToMany)
            {
                // ManyToMany: Collection orphan removal (uses join table)
                GenerateManyToManyOrphanRemoval(sb, info, rel, keyPropertyName);
            }
            else if (rel.IsCollection)
            {
                // OneToMany: Collection orphan removal
                GenerateOneToManyOrphanRemoval(sb, info, rel, keyPropertyName);
            }
            else if (rel.Type == RelationshipType.OneToOne)
            {
                // OneToOne: Single entity orphan removal
                GenerateOneToOneOrphanRemoval(sb, info, rel, keyPropertyName);
            }
            // ManyToOne is explicitly NOT supported - see comment above
        }

        sb.AppendLine($"            // Update the main entity");
        sb.AppendLine($"            await base.UpdateAsync(entity);");
        sb.AppendLine($"        }}");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();

        return sb.ToString();
    }

    private static void GenerateOneToManyOrphanRemoval(StringBuilder sb, RepositoryInfo info, RelationshipMetadata rel, string keyPropertyName)
    {
        // Determine FK column name from MappedBy or convention
        var fkColumn = rel.MappedBy != null
            ? $"{info.EntityType.Split('.').Last()}Id"
            : $"{rel.TargetEntityType}Id";
        
        var relatedTableName = MetadataHelper.GetTableNameFromMetadata(info, rel.TargetEntityType) ?? StringHelper.Pluralize(rel.TargetEntityType.Split('.').Last());
        var relatedKeyPropertyName = MetadataHelper.GetKeyPropertyName(info, rel.TargetEntityType);
        
        sb.AppendLine($"            // Orphan removal for {rel.PropertyName} collection (OneToMany)");
        sb.AppendLine($"            if (entity.{rel.PropertyName} != null)");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                var currentItems = entity.{rel.PropertyName}.ToList();");
        sb.AppendLine($"                ");
        sb.AppendLine($"                // Load existing items from database");
        sb.AppendLine($"                var fkColumnName = \"{fkColumn}\";");
        sb.AppendLine($"                var sql = $\"SELECT * FROM {relatedTableName} WHERE {{fkColumnName}} = @ParentId\";");
        sb.AppendLine($"                var existingItems = (await _connection.QueryAsync<{rel.TargetEntityFullType}>(sql, new {{ ParentId = entity.{keyPropertyName} }})).ToList();");
        sb.AppendLine($"                ");
        sb.AppendLine($"                // Identify orphaned items (in existing but not in current)");
        sb.AppendLine($"                var currentIds = currentItems.Where(i => i.{relatedKeyPropertyName} != default).Select(i => i.{relatedKeyPropertyName}).ToHashSet();");
        sb.AppendLine($"                ");
        sb.AppendLine($"                // Delete orphaned items");
        sb.AppendLine($"                foreach (var existingItem in existingItems)");
        sb.AppendLine($"                {{");
        sb.AppendLine($"                    if (!currentIds.Contains(existingItem.{relatedKeyPropertyName}))");
        sb.AppendLine($"                    {{");
        sb.AppendLine($"                        await _entityManager.RemoveAsync(existingItem);");
        sb.AppendLine($"                    }}");
        sb.AppendLine($"                }}");
        sb.AppendLine($"            }}");
        sb.AppendLine($"            else");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                // Collection is null - delete all existing items (orphan removal)");
        sb.AppendLine($"                var fkColumnName = \"{fkColumn}\";");
        sb.AppendLine($"                var sql = $\"SELECT * FROM {relatedTableName} WHERE {{fkColumnName}} = @ParentId\";");
        sb.AppendLine($"                var existingItems = (await _connection.QueryAsync<{rel.TargetEntityFullType}>(sql, new {{ ParentId = entity.{keyPropertyName} }})).ToList();");
        sb.AppendLine($"                ");
        sb.AppendLine($"                foreach (var existingItem in existingItems)");
        sb.AppendLine($"                {{");
        sb.AppendLine($"                    await _entityManager.RemoveAsync(existingItem);");
        sb.AppendLine($"                }}");
        sb.AppendLine($"            }}");
        sb.AppendLine();
    }

    private static void GenerateOneToOneOrphanRemoval(StringBuilder sb, RepositoryInfo info, RelationshipMetadata rel, string keyPropertyName)
    {
        var relatedKeyPropertyName = MetadataHelper.GetKeyPropertyName(info, rel.TargetEntityType);
        var relatedTableName = MetadataHelper.GetTableNameFromMetadata(info, rel.TargetEntityType) ?? StringHelper.Pluralize(rel.TargetEntityType.Split('.').Last());
        
        // Determine FK column - for OneToOne, it could be on either side
        var fkColumn = rel.JoinColumn?.Name;
        if (string.IsNullOrEmpty(fkColumn))
        {
            // If owner side, FK is on target entity pointing to this entity
            if (rel.IsOwner)
            {
                fkColumn = $"{info.EntityType.Split('.').Last()}Id";
            }
            else
            {
                // Inverse side - FK is on this entity pointing to target
                fkColumn = $"{rel.TargetEntityType}Id";
            }
        }
        
        sb.AppendLine($"            // Orphan removal for {rel.PropertyName} (OneToOne)");
        sb.AppendLine($"            ");
        if (rel.IsOwner)
        {
            // Owner side: FK is on target entity
            sb.AppendLine($"            // Load existing related entity (owner side - FK on target)");
            sb.AppendLine($"            var fkColumnName = \"{fkColumn}\";");
            sb.AppendLine($"            var existingSql = $\"SELECT * FROM {relatedTableName} WHERE {{fkColumnName}} = @ParentId\";");
            sb.AppendLine($"            var existingRelated = await _connection.QueryFirstOrDefaultAsync<{rel.TargetEntityFullType}>(existingSql, new {{ ParentId = entity.{keyPropertyName} }});");
        }
        else
        {
            // Inverse side: FK is on current entity, query target entity using FK value
            // Note: existing.{rel.PropertyName} may be null because GetByIdAsync doesn't eagerly load relationships
            // So we need to query the current entity's table to get the FK value, then query the target entity
            // Use metadata if available, otherwise attempt simple pluralization
            // Note: Simple pluralization may fail for irregular nouns (e.g., Person -> People).
            // For accurate table names, use [Table] attribute on entity classes.
            var entityName = info.EntityType.Split('.').Last();
            var currentTableName = MetadataHelper.GetTableNameFromMetadata(info, info.EntityType) 
                ?? StringHelper.Pluralize(entityName);
            var relatedKeyType = MetadataHelper.GetRelatedEntityKeyType(info, rel.TargetEntityType);
            var defaultKeyValue = relatedKeyType == "Guid" ? "Guid.Empty" : relatedKeyType == "int" ? "0" : relatedKeyType == "long" ? "0L" : $"default({relatedKeyType})";
            var keyColumnName = MetadataHelper.GetKeyColumnName(info);
            var relatedKeyColumnName = MetadataHelper.GetKeyColumnName(info, rel.TargetEntityType);
            sb.AppendLine($"            // Load existing related entity (inverse side - FK on current entity)");
            sb.AppendLine($"            // Query current entity's table to get FK value, then query target entity");
            sb.AppendLine($"            var fkColumnName = \"{fkColumn}\";");
            sb.AppendLine($"            var fkValueSql = $\"SELECT {{fkColumnName}} FROM {currentTableName} WHERE {keyColumnName} = @ParentId\";");
            sb.AppendLine($"            var fkValue = await _connection.QueryFirstOrDefaultAsync<{relatedKeyType}>(fkValueSql, new {{ ParentId = entity.{keyPropertyName} }});");
            // Check if FK value is valid (not null for reference types, not default for value types)
            // For reference types (string), null check is sufficient. For value types, check against default.
            var isReferenceType = relatedKeyType == "string";
            var fkValueCheck = isReferenceType ? "fkValue != null" : $"fkValue != null && fkValue != {defaultKeyValue}";
            sb.AppendLine($"            var existingRelated = {fkValueCheck} ? await _connection.QueryFirstOrDefaultAsync<{rel.TargetEntityFullType}>($\"SELECT * FROM {relatedTableName} WHERE {relatedKeyColumnName} = @FkValue\", new {{ FkValue = fkValue }}) : null;");
        }
        sb.AppendLine($"            ");
        sb.AppendLine($"            // Check if relationship was cleared or replaced");
        sb.AppendLine($"            if (existingRelated != null)");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                if (entity.{rel.PropertyName} == null)");
        sb.AppendLine($"                {{");
        sb.AppendLine($"                    // Relationship cleared - delete orphan (orphan removal)");
        sb.AppendLine($"                    await _entityManager.RemoveAsync(existingRelated);");
        sb.AppendLine($"                }}");
        sb.AppendLine($"                else if (entity.{rel.PropertyName}.{relatedKeyPropertyName} != existingRelated.{relatedKeyPropertyName})");
        sb.AppendLine($"                {{");
        sb.AppendLine($"                    // Relationship replaced - delete old orphan (orphan removal)");
        sb.AppendLine($"                    await _entityManager.RemoveAsync(existingRelated);");
        sb.AppendLine($"                }}");
        sb.AppendLine($"            }}");
        sb.AppendLine();
    }

    private static void GenerateManyToManyOrphanRemoval(StringBuilder sb, RepositoryInfo info, RelationshipMetadata rel, string keyPropertyName)
    {
        // ManyToMany uses a join table, so we need to:
        // 1. Get current items from the collection
        // 2. Get existing items from join table
        // 3. Find items that were removed (in existing but not in current)
        // 4. Check if removed items are referenced by other entities
        // 5. Delete only if not referenced elsewhere (true orphan removal)
        
        var joinTable = rel.JoinTable;
        string joinTableName;
        string ownerKeyColumn;
        string targetKeyColumn;
        var relatedKeyPropertyName = MetadataHelper.GetKeyPropertyName(info, rel.TargetEntityType);
        var relatedKeyType = MetadataHelper.GetRelatedEntityKeyType(info, rel.TargetEntityType);
        
        if (joinTable == null)
        {
            // Fallback to convention-based join table name
            var entityName = info.EntityType.Split('.').Last();
            var relatedName = rel.TargetEntityType.Split('.').Last();
            joinTableName = $"{entityName}{relatedName}";
            ownerKeyColumn = $"{entityName}Id";
            targetKeyColumn = $"{relatedName}Id";
        }
        else
        {
            // Use join table metadata if available
            joinTableName = string.IsNullOrEmpty(joinTable.Schema)
                ? joinTable.Name
                : $"{joinTable.Schema}.{joinTable.Name}";
            ownerKeyColumn = joinTable.JoinColumns.FirstOrDefault() ?? $"{info.EntityType.Split('.').Last()}Id";
            targetKeyColumn = joinTable.InverseJoinColumns.FirstOrDefault() ?? $"{rel.TargetEntityType.Split('.').Last()}Id";
        }
        
        sb.AppendLine($"            // Orphan removal for {rel.PropertyName} collection (ManyToMany)");
        sb.AppendLine($"            // Note: ManyToMany orphan removal checks if entities are referenced elsewhere");
        sb.AppendLine($"            if (entity.{rel.PropertyName} != null)");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                var currentItems = entity.{rel.PropertyName}.ToList();");
        sb.AppendLine($"                ");
        sb.AppendLine($"                // Load existing relationships from join table");
        sb.AppendLine($"                var sql = $\"SELECT {targetKeyColumn} FROM {joinTableName} WHERE {ownerKeyColumn} = @ParentId\";");
        sb.AppendLine($"                var existingRelatedIds = (await _connection.QueryAsync<{relatedKeyType}>(sql, new {{ ParentId = entity.{keyPropertyName} }})).ToList();");
        sb.AppendLine($"                ");
        sb.AppendLine($"                var currentIds = currentItems.Where(i => i.{relatedKeyPropertyName} != default).Select(i => i.{relatedKeyPropertyName}).ToHashSet();");
        sb.AppendLine($"                ");
        sb.AppendLine($"                // Find removed items (in existing but not in current)");
        sb.AppendLine($"                var removedIds = existingRelatedIds.Except(currentIds).ToList();");
        sb.AppendLine($"                ");
        sb.AppendLine($"                // For each removed item, check if it's referenced by other entities");
        sb.AppendLine($"                foreach (var removedId in removedIds)");
        sb.AppendLine($"                {{");
        sb.AppendLine($"                    // Check if this entity is referenced by other entities in the join table");
        sb.AppendLine($"                    var checkSql = $\"SELECT COUNT(*) FROM {joinTableName} WHERE {targetKeyColumn} = @RemovedId AND {ownerKeyColumn} != @ParentId\";");
        sb.AppendLine($"                    var referenceCount = await _connection.QuerySingleAsync<int>(checkSql, new {{ RemovedId = removedId, ParentId = entity.{keyPropertyName} }});");
        sb.AppendLine($"                    ");
        sb.AppendLine($"                    // If not referenced elsewhere, delete the orphaned entity");
        sb.AppendLine($"                    if (referenceCount == 0)");
        sb.AppendLine($"                    {{");
        sb.AppendLine($"                        var orphanedEntity = await _entityManager.FindAsync<{rel.TargetEntityFullType}>(removedId);");
        sb.AppendLine($"                        if (orphanedEntity != null)");
        sb.AppendLine($"                        {{");
        sb.AppendLine($"                            await _entityManager.RemoveAsync(orphanedEntity);");
        sb.AppendLine($"                        }}");
        sb.AppendLine($"                    }}");
        sb.AppendLine($"                }}");
        sb.AppendLine($"            }}");
        sb.AppendLine($"            else");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                // Collection is null - check all existing relationships for orphan removal");
        sb.AppendLine($"                var sql = $\"SELECT {targetKeyColumn} FROM {joinTableName} WHERE {ownerKeyColumn} = @ParentId\";");
        sb.AppendLine($"                var existingRelatedIds = (await _connection.QueryAsync<{relatedKeyType}>(sql, new {{ ParentId = entity.{keyPropertyName} }})).ToList();");
        sb.AppendLine($"                ");
        sb.AppendLine($"                foreach (var relatedId in existingRelatedIds)");
        sb.AppendLine($"                {{");
        sb.AppendLine($"                    // Check if this entity is referenced by other entities");
        sb.AppendLine($"                    var checkSql = $\"SELECT COUNT(*) FROM {joinTableName} WHERE {targetKeyColumn} = @RelatedId AND {ownerKeyColumn} != @ParentId\";");
        sb.AppendLine($"                    var referenceCount = await _connection.QuerySingleAsync<int>(checkSql, new {{ RelatedId = relatedId, ParentId = entity.{keyPropertyName} }});");
        sb.AppendLine($"                    ");
        sb.AppendLine($"                    // If not referenced elsewhere, delete the orphaned entity");
        sb.AppendLine($"                    if (referenceCount == 0)");
        sb.AppendLine($"                    {{");
        sb.AppendLine($"                        var orphanedEntity = await _entityManager.FindAsync<{rel.TargetEntityFullType}>(relatedId);");
        sb.AppendLine($"                        if (orphanedEntity != null)");
        sb.AppendLine($"                        {{");
        sb.AppendLine($"                            await _entityManager.RemoveAsync(orphanedEntity);");
        sb.AppendLine($"                        }}");
        sb.AppendLine($"                    }}");
        sb.AppendLine($"                }}");
        sb.AppendLine($"            }}");
        sb.AppendLine();
    }
}

