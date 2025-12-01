using System.Collections.Generic;
using System.Linq;
using System.Text;
using NPA.Design.Models;
using NPA.Design.Generators.Helpers;

namespace NPA.Design.Generators.CodeGenerators;

/// <summary>
/// Generates cascade operation methods for repositories (AddWithCascade, UpdateWithCascade, DeleteWithCascade).
/// </summary>
internal static class CascadeOperationGenerator
{
    /// <summary>
    /// Generates cascade operation overrides for AddAsync, UpdateAsync, and DeleteAsync.
    /// </summary>
    public static string GenerateCascadeOperationOverrides(RepositoryInfo info)
    {
        var sb = new StringBuilder();

        sb.AppendLine("        #region Cascade Operations");
        sb.AppendLine();

        // Check for Persist cascades - affects AddAsync
        var persistCascades = info.CascadeRelationships.Where(r => (r.CascadeTypes & CascadeType.Persist) != 0).ToList();
        if (persistCascades.Any())
        {
            sb.AppendLine(GenerateCascadeAddMethod(info, persistCascades));
        }

        // Check for Merge cascades - affects UpdateAsync
        var mergeCascades = info.CascadeRelationships.Where(r => (r.CascadeTypes & CascadeType.Merge) != 0).ToList();
        if (mergeCascades.Any())
        {
            sb.AppendLine(GenerateCascadeUpdateMethod(info, mergeCascades));
        }

        // Check for Remove cascades - affects DeleteAsync
        var removeCascades = info.CascadeRelationships.Where(r => (r.CascadeTypes & CascadeType.Remove) != 0).ToList();
        if (removeCascades.Any())
        {
            sb.AppendLine(GenerateCascadeDeleteMethod(info, removeCascades));
        }

        sb.AppendLine("        #endregion");
        sb.AppendLine();

        return sb.ToString();
    }

    private static string GenerateCascadeAddMethod(RepositoryInfo info, List<RelationshipMetadata> cascades)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Adds an entity with cascade persist support.");
        sb.AppendLine($"        /// Automatically persists related entities marked with CascadeType.Persist.");
        sb.AppendLine($"        /// Cascades: {string.Join(", ", cascades.Select(c => c.PropertyName))}");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public async Task<{info.EntityType}> AddWithCascadeAsync({info.EntityType} entity)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            if (entity == null) throw new ArgumentNullException(nameof(entity));");
        sb.AppendLine();

        // Generate cascade logic for each relationship with Persist
        foreach (var cascade in cascades)
        {
            if (cascade.IsCollection)
            {
                // Collection cascade (OneToMany) - Persist children after parent
                sb.AppendLine($"            // Cascade persist {cascade.PropertyName} collection (children persisted after parent)");
                sb.AppendLine($"            var {cascade.PropertyName.ToLower()}ToPersist = entity.{cascade.PropertyName}?.ToList() ?? new List<{cascade.TargetEntityFullType}>();");
                sb.AppendLine();
            }
            else
            {
                // Single entity cascade (ManyToOne, OneToOne) - Persist parent first
                var fkColumnName = cascade.JoinColumn?.Name ?? $"{cascade.TargetEntityType.Split('.').Last()}Id";
                var hasFkProperty = MetadataHelper.HasProperty(info, fkColumnName);

                sb.AppendLine($"            // Cascade persist {cascade.PropertyName} (parent persisted first)");
                sb.AppendLine($"            if (entity.{cascade.PropertyName} != null)");
                sb.AppendLine($"            {{");
                var relatedKeyPropertyName = MetadataHelper.GetKeyPropertyName(info, cascade.TargetEntityType);
                sb.AppendLine($"                // Check if entity is transient (Id is default value)");
                sb.AppendLine($"                if (entity.{cascade.PropertyName}.{relatedKeyPropertyName} == default)");
                sb.AppendLine($"                {{");
                sb.AppendLine($"                    // Persist the related entity first");
                sb.AppendLine($"                    await _entityManager.PersistAsync(entity.{cascade.PropertyName});");
                sb.AppendLine($"                    ");
                if (hasFkProperty)
                {
                    var fkPropertyName = MetadataHelper.GetPropertyNameForColumn(info, fkColumnName);
                    sb.AppendLine($"                    // Update FK on main entity (if FK property exists)");
                    sb.AppendLine($"                    entity.{fkPropertyName} = entity.{cascade.PropertyName}.{relatedKeyPropertyName};");
                }
                else
                {
                    sb.AppendLine($"                    // Note: FK property doesn't exist - FK is managed automatically via @JoinColumn");
                }
                sb.AppendLine($"                }}");
                sb.AppendLine($"            }}");
                sb.AppendLine();
            }
        }

        sb.AppendLine($"            // Persist the main entity");
        sb.AppendLine($"            var result = await AddAsync(entity);");
        sb.AppendLine();

        // Get the key property name for the main entity
        var keyPropertyName = MetadataHelper.GetKeyPropertyName(info);

        // Now persist collections (children after parent)
        foreach (var cascade in cascades.Where(c => c.IsCollection))
        {
            // Determine FK column name from MappedBy or convention
            var fkColumn = cascade.MappedBy != null
                ? $"{info.EntityType.Split('.').Last()}Id"
                : $"{cascade.TargetEntityType.Split('.').Last()}Id";
            
            // Check if FK property exists on the related entity
            var hasFkProperty = MetadataHelper.HasProperty(info, fkColumn, cascade.TargetEntityType);
            var fkPropertyName = hasFkProperty ? MetadataHelper.GetPropertyNameForColumn(info, fkColumn, cascade.TargetEntityType) : null;
            var ownerPropertyName = cascade.MappedBy ?? info.EntityType.Split('.').Last();

            sb.AppendLine($"            // Persist {cascade.PropertyName} collection after parent");
            sb.AppendLine($"            if ({cascade.PropertyName.ToLower()}ToPersist.Any())");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                foreach (var item in {cascade.PropertyName.ToLower()}ToPersist)");
            sb.AppendLine($"                {{");
            sb.AppendLine($"                    // Set FK to parent");
            if (hasFkProperty && fkPropertyName != null)
            {
                sb.AppendLine($"                    item.{fkPropertyName} = result.{keyPropertyName};");
            }
            else
            {
                // Use navigation property if FK property doesn't exist
                sb.AppendLine($"                    item.{ownerPropertyName} = result;");
            }
            sb.AppendLine($"                    await _entityManager.PersistAsync(item);");
            sb.AppendLine($"                }}");
            sb.AppendLine($"            }}");
            sb.AppendLine();
        }

        sb.AppendLine($"            return result;");
        sb.AppendLine($"        }}");
        sb.AppendLine();

        return sb.ToString();
    }

    private static string GenerateCascadeUpdateMethod(RepositoryInfo info, List<RelationshipMetadata> cascades)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Updates an entity with cascade merge support.");
        sb.AppendLine($"        /// Automatically updates related entities marked with CascadeType.Merge.");
        sb.AppendLine($"        /// Cascades: {string.Join(", ", cascades.Select(c => c.PropertyName))}");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public async Task UpdateWithCascadeAsync({info.EntityType} entity)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            if (entity == null) throw new ArgumentNullException(nameof(entity));");
        sb.AppendLine();

        // Update single entity relationships first
        foreach (var cascade in cascades.Where(c => !c.IsCollection))
        {
            var relatedKeyPropertyName = MetadataHelper.GetKeyPropertyName(info, cascade.TargetEntityType);
            sb.AppendLine($"            // Cascade merge {cascade.PropertyName}");
            sb.AppendLine($"            if (entity.{cascade.PropertyName} != null)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                // Update if entity exists (has Id), persist if new");
            sb.AppendLine($"                if (entity.{cascade.PropertyName}.{relatedKeyPropertyName} != default)");
            sb.AppendLine($"                {{");
            sb.AppendLine($"                    await _entityManager.MergeAsync(entity.{cascade.PropertyName});");
            sb.AppendLine($"                }}");
            sb.AppendLine($"                else");
            sb.AppendLine($"                {{");
            sb.AppendLine($"                    await _entityManager.PersistAsync(entity.{cascade.PropertyName});");
            sb.AppendLine($"                }}");
            sb.AppendLine($"            }}");
            sb.AppendLine();
        }

        sb.AppendLine($"            // Update the main entity");
        sb.AppendLine($"            await UpdateAsync(entity);");
        sb.AppendLine();

        // Handle collection cascades
        foreach (var cascade in cascades.Where(c => c.IsCollection))
        {
            // Determine FK column name from MappedBy or convention
            var fkColumn = cascade.MappedBy != null
                ? $"{info.EntityType.Split('.').Last()}Id"
                : $"{cascade.TargetEntityType.Split('.').Last()}Id";
            
            // Check if FK property exists on the related entity
            var hasFkProperty = MetadataHelper.HasProperty(info, fkColumn, cascade.TargetEntityType);
            var fkPropertyName = hasFkProperty ? MetadataHelper.GetPropertyNameForColumn(info, fkColumn, cascade.TargetEntityType) : null;
            var ownerPropertyName = cascade.MappedBy ?? info.EntityType.Split('.').Last();

            var keyPropertyName = MetadataHelper.GetKeyPropertyName(info);
            var relatedKeyPropertyName = MetadataHelper.GetKeyPropertyName(info, cascade.TargetEntityType);
            
            sb.AppendLine($"            // Cascade merge {cascade.PropertyName} collection");
            sb.AppendLine($"            if (entity.{cascade.PropertyName} != null)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                var currentItems = entity.{cascade.PropertyName}.ToList();");
            sb.AppendLine($"                ");

            if (cascade.OrphanRemoval)
            {
                var relatedTableName = MetadataHelper.GetTableNameFromMetadata(info, cascade.TargetEntityType) ?? StringHelper.Pluralize(cascade.TargetEntityType.Split('.').Last());
                sb.AppendLine($"                // Load existing items to detect orphans (OrphanRemoval=true)");
                sb.AppendLine($"                var fkColumnName = \"{fkColumn}\";");
                sb.AppendLine($"                var sql = $\"SELECT * FROM {relatedTableName} WHERE {{fkColumnName}} = @ParentId\";");
                sb.AppendLine($"                var existingItems = (await _connection.QueryAsync<{cascade.TargetEntityFullType}>(sql, new {{ ParentId = entity.{keyPropertyName} }})).ToList();");
                sb.AppendLine($"                ");
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
                sb.AppendLine($"                ");
            }

            sb.AppendLine($"                // Update existing items or persist new ones");
            sb.AppendLine($"                foreach (var item in currentItems)");
            sb.AppendLine($"                {{");
            sb.AppendLine($"                    // Ensure FK is set");
            if (hasFkProperty && fkPropertyName != null)
            {
                sb.AppendLine($"                    item.{fkPropertyName} = entity.{keyPropertyName};");
            }
            else
            {
                // Use navigation property if FK property doesn't exist
                sb.AppendLine($"                    item.{ownerPropertyName} = entity;");
            }
            sb.AppendLine($"                    ");
            sb.AppendLine($"                    if (item.{relatedKeyPropertyName} != default)");
            sb.AppendLine($"                    {{");
            sb.AppendLine($"                        await _entityManager.MergeAsync(item);");
            sb.AppendLine($"                    }}");
            sb.AppendLine($"                    else");
            sb.AppendLine($"                    {{");
            sb.AppendLine($"                        await _entityManager.PersistAsync(item);");
            sb.AppendLine($"                    }}");
            sb.AppendLine($"                }}");
            sb.AppendLine($"            }}");
            sb.AppendLine();
        }

        sb.AppendLine($"        }}");
        sb.AppendLine();

        return sb.ToString();
    }

    private static string GenerateCascadeDeleteMethod(RepositoryInfo info, List<RelationshipMetadata> cascades)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Deletes an entity with cascade remove support.");
        sb.AppendLine($"        /// Automatically deletes related entities marked with CascadeType.Remove.");
        sb.AppendLine($"        /// Cascades: {string.Join(", ", cascades.Select(c => c.PropertyName))}");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public async Task DeleteWithCascadeAsync({info.KeyType} id)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            // Load entity to check relationships");
        sb.AppendLine($"            var entity = await GetByIdAsync(id);");
        sb.AppendLine($"            if (entity == null)");
        sb.AppendLine($"                throw new InvalidOperationException($\"{info.EntityType} with id {{id}} not found\");");
        sb.AppendLine();

        // Delete collections first (children before parent)
        foreach (var cascade in cascades.Where(c => c.IsCollection))
        {
            var fkColumn = cascade.MappedBy != null
                ? $"{info.EntityType.Split('.').Last()}Id".ToLower()
                : $"{cascade.TargetEntityType.Split('.').Last()}Id".ToLower();
            var relatedTableName = MetadataHelper.GetTableNameFromMetadata(info, cascade.TargetEntityType) ?? StringHelper.Pluralize(cascade.TargetEntityType.Split('.').Last());

            sb.AppendLine($"            // Cascade remove {cascade.PropertyName} collection (delete children first)");
            sb.AppendLine($"            var fkColumn{cascade.PropertyName} = \"{fkColumn}\";");
            sb.AppendLine($"            var sql{cascade.PropertyName} = $\"SELECT * FROM {relatedTableName} WHERE {{fkColumn{cascade.PropertyName}}} = @ParentId\";");
            sb.AppendLine($"            var {cascade.PropertyName.ToLower()}Items = await _connection.QueryAsync<{cascade.TargetEntityFullType}>(sql{cascade.PropertyName}, new {{ ParentId = id }});");
            sb.AppendLine($"            ");
            sb.AppendLine($"            foreach (var item in {cascade.PropertyName.ToLower()}Items)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                await _entityManager.RemoveAsync(item);");
            sb.AppendLine($"            }}");
            sb.AppendLine();
        }

        // Handle single entity cascades
        foreach (var cascade in cascades.Where(c => !c.IsCollection))
        {
            sb.AppendLine($"            // Cascade remove {cascade.PropertyName}");
            sb.AppendLine($"            if (entity.{cascade.PropertyName} != null)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                await _entityManager.RemoveAsync(entity.{cascade.PropertyName});");
            sb.AppendLine($"            }}");
            sb.AppendLine();
        }

        sb.AppendLine($"            // Delete the main entity");
        sb.AppendLine($"            await DeleteAsync(id);");
        sb.AppendLine($"        }}");
        sb.AppendLine();

        return sb.ToString();
    }
}

