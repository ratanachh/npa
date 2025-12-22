using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using NPA.Design.Models;
using NPA.Design.Generators.Helpers;
using NPA.Design.Generators.Analyzers;

namespace NPA.Design.Generators.CodeGenerators;

/// <summary>
/// Generates relationship query methods for repository implementations.
/// </summary>
internal static class RelationshipQueryGenerator
{
    public static string GenerateRelationshipQueryMethods(RepositoryInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("        #region Relationship Query Methods");
        sb.AppendLine();

        var entityName = info.EntityType.Split('.').Last();
        var tableName = info.EntityMetadata?.TableName ?? entityName;

        foreach (var relationship in info.Relationships)
        {
            // Generate FindBy methods for ManyToOne relationships (find by parent)
            if (relationship.Type == Models.RelationshipType.ManyToOne)
            {
                GenerateFindByParentMethod(sb, info, relationship, tableName);
                GenerateCountByParentMethod(sb, info, relationship, tableName);
                // Generate property-based queries (e.g., FindByCustomerNameAsync)
                GeneratePropertyBasedQueries(sb, info, relationship, tableName);
                // Generate advanced filters (date ranges, amount filters)
                GenerateAdvancedFilters(sb, info, relationship, tableName);
                // Generate complex filters (OR/AND combinations)
                GenerateComplexFilters(sb, info, relationship, tableName);
            }

            // Generate Has/Count methods for OneToMany relationships (check if parent has children)
            if (relationship.Type == Models.RelationshipType.OneToMany && !string.IsNullOrEmpty(relationship.MappedBy))
            {
                GenerateHasChildrenMethod(sb, info, relationship);
                GenerateCountChildrenMethod(sb, info, relationship);
                // Generate aggregate methods for numeric properties
                GenerateAggregateMethods(sb, info, relationship);
                // Generate GROUP BY aggregate methods
                GenerateGroupByAggregateMethods(sb, info, relationship);
                // Generate multi-entity GROUP BY aggregate methods (with JOINs)
                GenerateMultiEntityGroupByAggregateMethods(sb, info, relationship);
                // Generate subquery-based filters
                GenerateSubqueryFilters(sb, info, relationship);
                // Generate inverse relationship queries (FindWith/Without/WithCount)
                GenerateInverseRelationshipQueries(sb, info, relationship);
            }
        }

        // Generate multi-level navigation queries (e.g., OrderItem → Order → Customer)
        GenerateMultiLevelNavigationQueries(sb, info, tableName);

        sb.AppendLine("        #endregion");
        sb.AppendLine();

        return sb.ToString();
    }

    public static void GenerateFindByParentMethod(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string tableName)
    {
        var targetEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        var foreignKeyColumn = relationship.JoinColumn?.Name ?? $"{targetEntitySimpleName}Id";
        var paramName = StringHelper.ToCamelCase(targetEntitySimpleName) + "Id";
        var keyColumnName = MetadataHelper.GetKeyColumnName(info);
        var relatedKeyType = MetadataHelper.GetRelatedEntityKeyType(info, relationship.TargetEntityType);

        // Generate method without pagination (backward compatibility)
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}IdAsync({relatedKeyType} {paramName})");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = \"SELECT * FROM {tableName} WHERE {foreignKeyColumn} = @{paramName} ORDER BY {keyColumnName}\";");
        sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {paramName} }});");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate method with pagination
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} with pagination support.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <param name=\"{paramName}\">The {relationship.PropertyName} identifier.</param>");
        sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
        sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}IdAsync({relatedKeyType} {paramName}, int skip, int take)");
        sb.AppendLine("        {");
        // Use database-specific pagination syntax (will need provider-specific handling, but for now use OFFSET/FETCH which works in SQL Server, PostgreSQL, SQLite)
        sb.AppendLine($"            var sql = $\"SELECT * FROM {tableName} WHERE {foreignKeyColumn} = @{paramName} ORDER BY {keyColumnName} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
        sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {paramName}, skip, take }});");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate method with pagination and sorting
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} with pagination and sorting support.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <param name=\"{paramName}\">The {relationship.PropertyName} identifier.</param>");
        sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
        sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
        sb.AppendLine("        /// <param name=\"orderBy\">Property name to order by. Defaults to primary key if null or empty.</param>");
        sb.AppendLine("        /// <param name=\"ascending\">Sort direction. True for ascending, false for descending.</param>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}IdAsync({relatedKeyType} {paramName}, int skip, int take, string? orderBy = null, bool ascending = true)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var orderByColumn = GetColumnNameForProperty(orderBy, \"{keyColumnName}\");");
        sb.AppendLine($"            var direction = ascending ? \"ASC\" : \"DESC\";");
        sb.AppendLine($"            var sql = $\"SELECT * FROM {tableName} WHERE {foreignKeyColumn} = @{paramName} ORDER BY {{orderByColumn}} {{direction}} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
        sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {paramName}, skip, take }});");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    public static void GenerateCountByParentMethod(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string tableName)
    {
        var targetEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        var foreignKeyColumn = relationship.JoinColumn?.Name ?? $"{targetEntitySimpleName}Id";
        var paramName = StringHelper.ToCamelCase(targetEntitySimpleName) + "Id";
        var relatedKeyType = MetadataHelper.GetRelatedEntityKeyType(info, relationship.TargetEntityType);

        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Counts {info.EntityType} entities by {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<int> CountBy{relationship.PropertyName}IdAsync({relatedKeyType} {paramName})");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = \"SELECT COUNT(*) FROM {tableName} WHERE {foreignKeyColumn} = @{paramName}\";");
        sb.AppendLine($"            return await _connection.ExecuteScalarAsync<int>(sql, new {{ {paramName} }});");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    public static void GenerateHasChildrenMethod(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        var childTableName = MetadataHelper.GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relationship.TargetEntityType;
        var parentEntityName = info.EntityType.Split('.').Last();
        // Get FK column from inverse ManyToOne relationship
        var foreignKeyColumn = GetForeignKeyColumnForOneToMany(info, relationship, parentEntityName);

        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Checks if the entity has any {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<bool> Has{relationship.PropertyName}Async({info.KeyType} id)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = \"SELECT COUNT(*) FROM {childTableName} WHERE {foreignKeyColumn} = @id\";");
        sb.AppendLine($"            var count = await _connection.ExecuteScalarAsync<int>(sql, new {{ id }});");
        sb.AppendLine($"            return count > 0;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    public static void GenerateCountChildrenMethod(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        var childTableName = MetadataHelper.GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relationship.TargetEntityType;
        var parentEntityName = info.EntityType.Split('.').Last();
        // Get FK column from inverse ManyToOne relationship
        var foreignKeyColumn = GetForeignKeyColumnForOneToMany(info, relationship, parentEntityName);

        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Counts the number of {relationship.PropertyName} for the entity.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<int> Count{relationship.PropertyName}Async({info.KeyType} id)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = \"SELECT COUNT(*) FROM {childTableName} WHERE {foreignKeyColumn} = @id\";");
        sb.AppendLine($"            return await _connection.ExecuteScalarAsync<int>(sql, new {{ id }});");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates property-based query methods for ManyToOne relationships.
    /// For example, FindByCustomerNameAsync, FindByCustomerEmailAsync, etc.
    /// </summary>
    public static void GeneratePropertyBasedQueries(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string tableName)
    {
        // Get related entity metadata
        var relatedEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(relatedEntitySimpleName, out var relatedMetadata))
        {
            return; // Can't generate property-based queries without metadata
        }

        if (relatedMetadata.Properties == null)
        {
            return;
        }

        var targetEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        var foreignKeyColumn = relationship.JoinColumn?.Name ?? $"{targetEntitySimpleName}Id";
        var relatedTableName = MetadataHelper.GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relatedEntitySimpleName;
        var keyColumnName = MetadataHelper.GetKeyColumnName(info);
        // Use column name instead of property name for the JOIN condition
        var relatedKeyColumnName = MetadataHelper.GetKeyColumnName(info, relationship.TargetEntityType);

        // Generate query methods for each property of the related entity (excluding primary key and relationships)
        foreach (var property in relatedMetadata.Properties)
        {
            // Skip primary key, relationships, and complex types
            if (property.IsPrimaryKey)
                continue;

            // Skip if property type is a collection or complex object (likely a relationship)
            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") || 
                property.TypeName.Contains("IEnumerable") || !TypeHelper.IsSimpleType(property.TypeName))
                continue;

            var propertyParamName = StringHelper.ToCamelCase(property.Name);
            var methodName = $"FindBy{relationship.PropertyName}{property.Name}Async";

            // Generate method without pagination (backward compatibility)
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName}.{property.Name}.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName})");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
            sb.AppendLine($"                INNER JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE r.{property.ColumnName} = @{propertyParamName}");
            sb.AppendLine($"                ORDER BY e.{keyColumnName}\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {propertyParamName} }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate method with pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName}.{property.Name} with pagination support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{propertyParamName}\">The {property.Name} value.</param>");
            sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
            sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName}, int skip, int take)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
            sb.AppendLine($"                INNER JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE r.{property.ColumnName} = @{propertyParamName}");
            sb.AppendLine($"                ORDER BY e.{keyColumnName} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {propertyParamName}, skip, take }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate method with pagination and sorting
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName}.{property.Name} with pagination and sorting support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{propertyParamName}\">The {property.Name} value.</param>");
            sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
            sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
            sb.AppendLine("        /// <param name=\"orderBy\">Property name to order by. Defaults to primary key if null or empty.</param>");
            sb.AppendLine("        /// <param name=\"ascending\">Sort direction. True for ascending, false for descending.</param>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName}, int skip, int take, string? orderBy = null, bool ascending = true)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var orderByColumn = GetColumnNameForProperty(orderBy, \"{keyColumnName}\");");
            sb.AppendLine($"            var direction = ascending ? \"ASC\" : \"DESC\";");
            sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
            sb.AppendLine($"                INNER JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE r.{property.ColumnName} = @{propertyParamName}");
            sb.AppendLine($"                ORDER BY e.{{orderByColumn}} {{direction}} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {propertyParamName}, skip, take }});");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates advanced filter methods for ManyToOne relationships.
    /// For example, FindByCustomerAndDateRangeAsync, FindCustomerOrdersAboveAmountAsync, etc.
    /// </summary>
    public static void GenerateAdvancedFilters(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string tableName)
    {
        // Get related entity metadata
        var relatedEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(relatedEntitySimpleName, out var relatedMetadata))
        {
            return; // Can't generate advanced filters without metadata
        }

        // Get current entity metadata for date/amount filters on the current entity
        if (info.EntityMetadata?.Properties == null)
        {
            return;
        }

        var targetEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        var foreignKeyColumn = relationship.JoinColumn?.Name ?? $"{targetEntitySimpleName}Id";
        var relatedTableName = MetadataHelper.GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? relatedEntitySimpleName;
        var keyColumnName = MetadataHelper.GetKeyColumnName(info);
        var relatedKeyColumnName = MetadataHelper.GetKeyColumnName(info, relationship.TargetEntityType);
        var relatedKeyType = MetadataHelper.GetRelatedEntityKeyType(info, relationship.TargetEntityType);
        var relatedKeyParamName = StringHelper.ToCamelCase(targetEntitySimpleName) + "Id";

        // Generate date range filters for DateTime properties on the current entity
        foreach (var property in info.EntityMetadata.Properties)
        {
            if (property.IsPrimaryKey)
                continue;

            if (!TypeHelper.IsDateTimeType(property.TypeName))
                continue;

            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") ||
                property.TypeName.Contains("IEnumerable"))
                continue;

            var propertyParamName = StringHelper.ToCamelCase(property.Name);
            var propertyColumnName = property.ColumnName;

            // Generate date range filter with relationship (without pagination)
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName} and {property.Name} date range.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}And{property.Name}RangeAsync({relatedKeyType} {relatedKeyParamName}, DateTime start{property.Name}, DateTime end{property.Name})");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
            sb.AppendLine($"                INNER JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE e.{foreignKeyColumn} = @{relatedKeyParamName}");
            sb.AppendLine($"                    AND e.{propertyColumnName} >= @start{property.Name}");
            sb.AppendLine($"                    AND e.{propertyColumnName} <= @end{property.Name}");
            sb.AppendLine($"                ORDER BY e.{keyColumnName}\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {relatedKeyParamName}, start{property.Name}, end{property.Name} }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate date range filter with pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} and {property.Name} date range with pagination support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{relatedKeyParamName}\">The {relationship.PropertyName} identifier.</param>");
            sb.AppendLine($"        /// <param name=\"start{property.Name}\">Start date.</param>");
            sb.AppendLine($"        /// <param name=\"end{property.Name}\">End date.</param>");
            sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
            sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}And{property.Name}RangeAsync({relatedKeyType} {relatedKeyParamName}, DateTime start{property.Name}, DateTime end{property.Name}, int skip, int take)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
            sb.AppendLine($"                INNER JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE e.{foreignKeyColumn} = @{relatedKeyParamName}");
            sb.AppendLine($"                    AND e.{propertyColumnName} >= @start{property.Name}");
            sb.AppendLine($"                    AND e.{propertyColumnName} <= @end{property.Name}");
            sb.AppendLine($"                ORDER BY e.{keyColumnName} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {relatedKeyParamName}, start{property.Name}, end{property.Name}, skip, take }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate date range filter with pagination and sorting
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} and {property.Name} date range with pagination and sorting support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{relatedKeyParamName}\">The {relationship.PropertyName} identifier.</param>");
            sb.AppendLine($"        /// <param name=\"start{property.Name}\">Start date.</param>");
            sb.AppendLine($"        /// <param name=\"end{property.Name}\">End date.</param>");
            sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
            sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
            sb.AppendLine("        /// <param name=\"orderBy\">Property name to order by. Defaults to primary key if null or empty.</param>");
            sb.AppendLine("        /// <param name=\"ascending\">Sort direction. True for ascending, false for descending.</param>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}And{property.Name}RangeAsync({relatedKeyType} {relatedKeyParamName}, DateTime start{property.Name}, DateTime end{property.Name}, int skip, int take, string? orderBy = null, bool ascending = true)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var orderByColumn = GetColumnNameForProperty(orderBy, \"{keyColumnName}\");");
            sb.AppendLine($"            var direction = ascending ? \"ASC\" : \"DESC\";");
            sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
            sb.AppendLine($"                INNER JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE e.{foreignKeyColumn} = @{relatedKeyParamName}");
            sb.AppendLine($"                    AND e.{propertyColumnName} >= @start{property.Name}");
            sb.AppendLine($"                    AND e.{propertyColumnName} <= @end{property.Name}");
            sb.AppendLine($"                ORDER BY e.{{orderByColumn}} {{direction}} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {relatedKeyParamName}, start{property.Name}, end{property.Name}, skip, take }});");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        // Generate amount/quantity filters for numeric properties on the current entity
        foreach (var property in info.EntityMetadata.Properties)
        {
            if (property.IsPrimaryKey)
                continue;

            if (!TypeHelper.IsNumericType(property.TypeName))
                continue;

            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") ||
                property.TypeName.Contains("IEnumerable"))
                continue;

            var propertyParamName = StringHelper.ToCamelCase(property.Name);
            var propertyColumnName = property.ColumnName;
            var returnType = property.TypeName.TrimEnd('?');

            // Generate minimum amount filter with relationship (without pagination)
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName} with {property.Name} greater than or equal to the specified value.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> Find{relationship.PropertyName}{property.Name}AboveAsync({relatedKeyType} {relatedKeyParamName}, {returnType} min{property.Name})");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
            sb.AppendLine($"                INNER JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE e.{foreignKeyColumn} = @{relatedKeyParamName}");
            sb.AppendLine($"                    AND e.{propertyColumnName} >= @min{property.Name}");
            sb.AppendLine($"                ORDER BY e.{keyColumnName}\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {relatedKeyParamName}, min{property.Name} }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate minimum amount filter with pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} with {property.Name} greater than or equal to the specified value, with pagination support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{relatedKeyParamName}\">The {relationship.PropertyName} identifier.</param>");
            sb.AppendLine($"        /// <param name=\"min{property.Name}\">Minimum {property.Name} value.</param>");
            sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
            sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> Find{relationship.PropertyName}{property.Name}AboveAsync({relatedKeyType} {relatedKeyParamName}, {returnType} min{property.Name}, int skip, int take)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
            sb.AppendLine($"                INNER JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE e.{foreignKeyColumn} = @{relatedKeyParamName}");
            sb.AppendLine($"                    AND e.{propertyColumnName} >= @min{property.Name}");
            sb.AppendLine($"                ORDER BY e.{keyColumnName} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {relatedKeyParamName}, min{property.Name}, skip, take }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate minimum amount filter with pagination and sorting
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} with {property.Name} greater than or equal to the specified value, with pagination and sorting support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{relatedKeyParamName}\">The {relationship.PropertyName} identifier.</param>");
            sb.AppendLine($"        /// <param name=\"min{property.Name}\">Minimum {property.Name} value.</param>");
            sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
            sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
            sb.AppendLine("        /// <param name=\"orderBy\">Property name to order by. Defaults to primary key if null or empty.</param>");
            sb.AppendLine("        /// <param name=\"ascending\">Sort direction. True for ascending, false for descending.</param>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> Find{relationship.PropertyName}{property.Name}AboveAsync({relatedKeyType} {relatedKeyParamName}, {returnType} min{property.Name}, int skip, int take, string? orderBy = null, bool ascending = true)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var orderByColumn = GetColumnNameForProperty(orderBy, \"{keyColumnName}\");");
            sb.AppendLine($"            var direction = ascending ? \"ASC\" : \"DESC\";");
            sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
            sb.AppendLine($"                INNER JOIN {relatedTableName} r ON e.{foreignKeyColumn} = r.{relatedKeyColumnName}");
            sb.AppendLine($"                WHERE e.{foreignKeyColumn} = @{relatedKeyParamName}");
            sb.AppendLine($"                    AND e.{propertyColumnName} >= @min{property.Name}");
            sb.AppendLine($"                ORDER BY e.{{orderByColumn}} {{direction}} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {relatedKeyParamName}, min{property.Name}, skip, take }});");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates subquery-based filter methods for OneToMany relationships.
    /// For example, FindWithMinimumItemsAsync - finds entities with at least N child entities.
    /// </summary>
    public static void GenerateSubqueryFilters(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Get child entity metadata
        var childEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(childEntitySimpleName, out var childMetadata))
        {
            return; // Can't generate subquery filters without metadata
        }

        var childTableName = MetadataHelper.GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? childEntitySimpleName;
        var parentEntityName = info.EntityType.Split('.').Last();
        var tableName = MetadataHelper.GetTableNameFromMetadata(info, info.EntityType) ?? parentEntityName;
        var keyColumnName = MetadataHelper.GetKeyColumnName(info);
        
        // For OneToMany, the JoinColumn is on the inverse ManyToOne relationship
        var foreignKeyColumn = GetForeignKeyColumnForOneToMany(info, relationship, parentEntityName);

        // Generate FindWithMinimum{Property}Async - finds parents with at least N children (without pagination)
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities that have at least the specified number of {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindWithMinimum{relationship.PropertyName}Async(int minCount)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
        sb.AppendLine($"                WHERE (");
        sb.AppendLine($"                    SELECT COUNT(*)");
        sb.AppendLine($"                    FROM {childTableName} c");
        sb.AppendLine($"                    WHERE c.{foreignKeyColumn} = e.{keyColumnName}");
        sb.AppendLine($"                ) >= @minCount");
        sb.AppendLine($"                ORDER BY e.{keyColumnName}\";");
        sb.AppendLine("            return await _connection.QueryAsync<" + info.EntityType + ">(sql, new { minCount });");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate FindWithMinimum{Property}Async with pagination
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds {info.EntityType} entities that have at least the specified number of {relationship.PropertyName}, with pagination support.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <param name=\"minCount\">Minimum number of {relationship.PropertyName}.</param>");
        sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
        sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindWithMinimum{relationship.PropertyName}Async(int minCount, int skip, int take)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
        sb.AppendLine($"                WHERE (");
        sb.AppendLine($"                    SELECT COUNT(*)");
        sb.AppendLine($"                    FROM {childTableName} c");
        sb.AppendLine($"                    WHERE c.{foreignKeyColumn} = e.{keyColumnName}");
        sb.AppendLine($"                ) >= @minCount");
        sb.AppendLine($"                ORDER BY e.{keyColumnName} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
        sb.AppendLine("            return await _connection.QueryAsync<" + info.EntityType + ">(sql, new { minCount, skip, take });");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate FindWithMinimum{Property}Async with pagination and sorting
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds {info.EntityType} entities that have at least the specified number of {relationship.PropertyName}, with pagination and sorting support.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <param name=\"minCount\">Minimum number of {relationship.PropertyName}.</param>");
        sb.AppendLine("        /// <param name=\"skip\">Number of records to skip.</param>");
        sb.AppendLine("        /// <param name=\"take\">Number of records to take.</param>");
        sb.AppendLine("        /// <param name=\"orderBy\">Property name to order by. Defaults to primary key if null or empty.</param>");
        sb.AppendLine("        /// <param name=\"ascending\">Sort direction. True for ascending, false for descending.</param>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindWithMinimum{relationship.PropertyName}Async(int minCount, int skip, int take, string? orderBy = null, bool ascending = true)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var orderByColumn = GetColumnNameForProperty(orderBy, \"{keyColumnName}\");");
        sb.AppendLine($"            var direction = ascending ? \"ASC\" : \"DESC\";");
        sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
        sb.AppendLine($"                WHERE (");
        sb.AppendLine($"                    SELECT COUNT(*)");
        sb.AppendLine($"                    FROM {childTableName} c");
        sb.AppendLine($"                    WHERE c.{foreignKeyColumn} = e.{keyColumnName}");
        sb.AppendLine($"                ) >= @minCount");
        sb.AppendLine($"                ORDER BY e.{{orderByColumn}} {{direction}} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
        sb.AppendLine("            return await _connection.QueryAsync<" + info.EntityType + ">(sql, new { minCount, skip, take });");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates inverse relationship query methods for OneToMany relationships.
    /// For example, FindWithOrdersAsync, FindWithoutOrdersAsync, FindWithOrderCountAsync.
    /// These methods are generated on the parent entity (e.g., Customer) to find entities based on their child relationships.
    /// </summary>
    public static void GenerateInverseRelationshipQueries(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Get child entity metadata
        var childEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(childEntitySimpleName, out var childMetadata))
        {
            return; // Can't generate inverse queries without metadata
        }

        var childTableName = MetadataHelper.GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? childEntitySimpleName;
        var parentEntityName = info.EntityType.Split('.').Last();
        var tableName = MetadataHelper.GetTableNameFromMetadata(info, info.EntityType) ?? parentEntityName;
        var keyColumnName = MetadataHelper.GetKeyColumnName(info);
        
        // For OneToMany, the JoinColumn is on the inverse ManyToOne relationship
        var foreignKeyColumn = GetForeignKeyColumnForOneToMany(info, relationship, parentEntityName);

        // Generate FindWith{Property}Async - finds parents that have at least one child
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities that have at least one {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindWith{relationship.PropertyName}Async()");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = @\"SELECT DISTINCT e.* FROM {tableName} e");
        sb.AppendLine($"                INNER JOIN {childTableName} c ON c.{foreignKeyColumn} = e.{keyColumnName}");
        sb.AppendLine($"                ORDER BY e.{keyColumnName}\";");
        sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql);");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate FindWithout{Property}Async - finds parents that have no children
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities that have no {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindWithout{relationship.PropertyName}Async()");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
        sb.AppendLine($"                WHERE NOT EXISTS (");
        sb.AppendLine($"                    SELECT 1");
        sb.AppendLine($"                    FROM {childTableName} c");
        sb.AppendLine($"                    WHERE c.{foreignKeyColumn} = e.{keyColumnName}");
        sb.AppendLine($"                )");
        sb.AppendLine($"                ORDER BY e.{keyColumnName}\";");
        sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql);");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate FindWith{Property}CountAsync - finds parents with at least N children
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities that have at least the specified number of {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <param name=\"minCount\">Minimum number of {relationship.PropertyName}.</param>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> FindWith{relationship.PropertyName}CountAsync(int minCount)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = @\"SELECT e.* FROM {tableName} e");
        sb.AppendLine($"                WHERE (");
        sb.AppendLine($"                    SELECT COUNT(*)");
        sb.AppendLine($"                    FROM {childTableName} c");
        sb.AppendLine($"                    WHERE c.{foreignKeyColumn} = e.{keyColumnName}");
        sb.AppendLine($"                ) >= @minCount");
        sb.AppendLine($"                ORDER BY e.{keyColumnName}\";");
        sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ minCount }});");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Represents a navigation path through multiple relationships.
    /// </summary>
    private class NavigationPath
    {
        public List<Models.RelationshipMetadata> Relationships { get; set; } = new();
        public List<string> EntityNames { get; set; } = new();
        public List<string> TableNames { get; set; } = new();
        public List<string> FkColumns { get; set; } = new();
        public List<string> KeyColumns { get; set; } = new();
        public List<string?> JoinTableNames { get; set; } = new(); // For ManyToMany relationships
        public List<string?> JoinTableOwnerFkColumns { get; set; } = new(); // FK column in join table pointing to owner
        public List<string?> JoinTableTargetFkColumns { get; set; } = new(); // FK column in join table pointing to target
        public string PathDescription => string.Join(" → ", EntityNames);
    }

    /// <summary>
    /// Generates multi-level navigation queries.
    /// For example, FindByCustomerNameAsync on OrderItemRepository navigates: OrderItem → Order → Customer
    /// Supports 2+ level navigation through ManyToOne, OneToOne, and ManyToMany relationships.
    /// </summary>
    public static void GenerateMultiLevelNavigationQueries(StringBuilder sb, RepositoryInfo info, string tableName)
    {
        if (info.EntitiesMetadata == null || info.EntitiesMetadata.Count == 0)
            return; // Can't generate multi-level queries without metadata

        if (info.Compilation == null)
            return; // Need compilation to extract relationships

        var entityName = info.EntityType.Split('.').Last();
        var keyColumnName = MetadataHelper.GetKeyColumnName(info);

        // Find all valid navigation paths (2+ levels)
        var navigationPaths = FindNavigationPaths(info, entityName, tableName, maxDepth: 5);

        // Generate queries for each navigation path
        foreach (var path in navigationPaths)
        {
            if (path.Relationships.Count < 2)
                continue; // Skip single-level paths (handled elsewhere)

            var targetEntityName = path.EntityNames.Last();
            var targetMetadata = info.EntitiesMetadata.TryGetValue(targetEntityName, out var metadata) ? metadata : null;
            if (targetMetadata?.Properties == null)
                continue;

            // Generate property-based queries for target entity properties
            foreach (var property in targetMetadata.Properties)
            {
                if (property.IsPrimaryKey)
                    continue;

                if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") ||
                    property.TypeName.Contains("IEnumerable") || !TypeHelper.IsSimpleType(property.TypeName))
                    continue;

                GenerateNavigationPathQuery(sb, info, path, property, tableName, keyColumnName);
            }
        }
    }

    /// <summary>
    /// Finds all valid navigation paths from the current entity to other entities.
    /// </summary>
    private static List<NavigationPath> FindNavigationPaths(RepositoryInfo info, string currentEntityName, string currentTableName, int maxDepth = 5)
    {
        var paths = new List<NavigationPath>();
        var visited = new HashSet<string>(); // Track visited entities to avoid cycles

        void FindPathsRecursive(NavigationPath currentPath, string entityName, string tableName, int depth)
        {
            if (depth > maxDepth)
                return;

            if (visited.Contains(entityName))
                return; // Avoid cycles

            visited.Add(entityName);

            // Get relationships for current entity
            var relationships = GetRelationshipsForEntity(info, entityName);
            
            foreach (var rel in relationships)
            {
                // Only support navigation through ManyToOne, OneToOne, and ManyToMany (for now)
                if (rel.Type != Models.RelationshipType.ManyToOne &&
                    rel.Type != Models.RelationshipType.OneToOne &&
                    rel.Type != Models.RelationshipType.ManyToMany)
                    continue;

                var targetEntityName = rel.TargetEntityType.Split('.').Last();
                
                // Skip if target is the starting entity (avoid cycles)
                if (targetEntityName == currentEntityName)
                    continue;

                // Skip if we don't have metadata for target
                if (!info.EntitiesMetadata.ContainsKey(targetEntityName))
                    continue;

                // Create new path
                var newPath = new NavigationPath
                {
                    Relationships = new List<Models.RelationshipMetadata>(currentPath.Relationships) { rel },
                    EntityNames = new List<string>(currentPath.EntityNames) { targetEntityName },
                    TableNames = new List<string>(currentPath.TableNames),
                    FkColumns = new List<string>(currentPath.FkColumns),
                    KeyColumns = new List<string>(currentPath.KeyColumns),
                    JoinTableNames = new List<string?>(currentPath.JoinTableNames),
                    JoinTableOwnerFkColumns = new List<string?>(currentPath.JoinTableOwnerFkColumns),
                    JoinTableTargetFkColumns = new List<string?>(currentPath.JoinTableTargetFkColumns)
                };

                // Add table name, FK column, and key column for this relationship
                var targetTableName = MetadataHelper.GetTableNameFromMetadata(info, targetEntityName) ?? targetEntityName;
                newPath.TableNames.Add(targetTableName);

                // Determine FK column and key column based on relationship type
                string fkColumn;
                string keyColumn;
                string? joinTableName = null;
                string? joinTableOwnerFkColumn = null;
                string? joinTableTargetFkColumn = null;
                
                if (rel.Type == Models.RelationshipType.ManyToOne)
                {
                    // ManyToOne: FK is always on the source entity (current entity)
                    // Join: source.FK = target.Key
                    fkColumn = rel.JoinColumn?.Name ?? $"{targetEntityName}Id";
                    keyColumn = MetadataHelper.GetKeyColumnName(info, targetEntityName);
                }
                else if (rel.Type == Models.RelationshipType.OneToOne)
                {
                    // OneToOne: FK location depends on ownership
                    if (rel.IsOwner)
                    {
                        // Owner side: FK is on the target entity pointing back to source
                        // Join: source.Key = target.FK
                        var sourceKeyColumn = MetadataHelper.GetKeyColumnName(info, entityName);
                        var targetFkColumn = rel.JoinColumn?.Name ?? $"{entityName}Id";
                        fkColumn = sourceKeyColumn; // Source key
                        keyColumn = targetFkColumn; // Target FK (on target table)
                    }
                    else
                    {
                        // Inverse side: FK is on the source entity (like ManyToOne)
                        // Join: source.FK = target.Key
                        fkColumn = rel.JoinColumn?.Name ?? $"{targetEntityName}Id";
                        keyColumn = MetadataHelper.GetKeyColumnName(info, targetEntityName);
                    }
                }
                else if (rel.Type == Models.RelationshipType.ManyToMany)
                {
                    // ManyToMany: Requires join table
                    if (rel.JoinTable == null)
                    {
                        // Skip if no join table metadata
                        continue;
                    }
                    
                    // Store join table information
                    joinTableName = string.IsNullOrEmpty(rel.JoinTable.Schema)
                        ? rel.JoinTable.Name
                        : $"{rel.JoinTable.Schema}.{rel.JoinTable.Name}";
                    
                    // Get FK column names from join table
                    joinTableOwnerFkColumn = rel.JoinTable.JoinColumns?.FirstOrDefault() 
                        ?? $"{entityName}Id";
                    joinTableTargetFkColumn = rel.JoinTable.InverseJoinColumns?.FirstOrDefault() 
                        ?? $"{targetEntityName}Id";
                    
                    // For ManyToMany, we'll use special SQL generation
                    // Store source key for first join: source.Key = joinTable.ownerFK
                    fkColumn = MetadataHelper.GetKeyColumnName(info, entityName);
                    keyColumn = joinTableOwnerFkColumn;
                }
                else
                {
                    continue;
                }

                // Store FK and key columns
                newPath.FkColumns.Add(fkColumn);
                newPath.KeyColumns.Add(keyColumn);
                
                // Store join table info for ManyToMany
                newPath.JoinTableNames.Add(joinTableName);
                newPath.JoinTableOwnerFkColumns.Add(joinTableOwnerFkColumn);
                newPath.JoinTableTargetFkColumns.Add(joinTableTargetFkColumn);

                // If path has 2+ levels, add it to results
                if (newPath.Relationships.Count >= 2)
                {
                    paths.Add(newPath);
                }

                // Continue recursively
                FindPathsRecursive(newPath, targetEntityName, targetTableName, depth + 1);
            }

            visited.Remove(entityName); // Backtrack
        }

        // Start from current entity
        var initialPath = new NavigationPath
        {
            EntityNames = new List<string> { currentEntityName },
            TableNames = new List<string> { currentTableName },
            JoinTableNames = new List<string?>(),
            JoinTableOwnerFkColumns = new List<string?>(),
            JoinTableTargetFkColumns = new List<string?>()
        };

        FindPathsRecursive(initialPath, currentEntityName, currentTableName, 1);

        return paths;
    }

    /// <summary>
    /// Gets relationships for an entity by extracting them from the compilation.
    /// </summary>
    public static List<Models.RelationshipMetadata> GetRelationshipsForEntity(RepositoryInfo info, string entityName)
    {
        if (info.Compilation == null)
            return new List<Models.RelationshipMetadata>();

        // If this is the current entity, use existing relationships
        if (entityName == info.EntityType.Split('.').Last())
        {
            return info.Relationships;
        }

        // Otherwise, extract relationships from the entity
        // Try to find the entity type in metadata
        if (info.EntitiesMetadata.TryGetValue(entityName, out var metadata))
        {
            // Try to find the full type name
            var entityFullType = metadata.Name; // EntityMetadataInfo.Name should contain full type name
            if (string.IsNullOrEmpty(entityFullType))
            {
                // Fallback: try to construct from namespace
                entityFullType = entityName;
            }

            return EntityAnalyzer.ExtractRelationships(info.Compilation, entityFullType);
        }

        return new List<Models.RelationshipMetadata>();
    }

    /// <summary>
    /// Generates query methods for a specific navigation path.
    /// </summary>
    private static void GenerateNavigationPathQuery(StringBuilder sb, RepositoryInfo info, NavigationPath path, 
        PropertyMetadataInfo property, string tableName, string keyColumnName)
    {
        var entityName = info.EntityType.Split('.').Last();
        var targetEntityName = path.EntityNames.Last();
        var pathDescription = path.PathDescription;
        
        // Build method name from path
        var methodNameParts = new List<string> { "FindBy" };
        for (int i = 1; i < path.EntityNames.Count; i++)
        {
            methodNameParts.Add(path.EntityNames[i]);
        }
        methodNameParts.Add(property.Name);
        var methodName = string.Join("", methodNameParts) + "Async";

        var propertyParamName = StringHelper.ToCamelCase(property.Name);

        // Build SQL with multiple JOINs
        var sqlBuilder = new StringBuilder();
        sqlBuilder.AppendLine($"SELECT e.* FROM {tableName} e");
        
        // Add JOINs for each level
        for (int i = 0; i < path.Relationships.Count; i++)
        {
            var rel = path.Relationships[i];
            var fkColumn = path.FkColumns[i];
            var keyColumn = path.KeyColumns[i];
            
            // Determine source and target aliases
            string sourceAlias;
            if (i == 0)
            {
                sourceAlias = "e"; // Base table
            }
            else
            {
                sourceAlias = $"r{i}"; // Previous intermediate table
            }
            
            if (rel.Type == Models.RelationshipType.ManyToMany)
            {
                // ManyToMany: Need two joins through join table
                var joinTableName = path.JoinTableNames[i] ?? "";
                var joinTableOwnerFk = path.JoinTableOwnerFkColumns[i] ?? "";
                var joinTableTargetFk = path.JoinTableTargetFkColumns[i] ?? "";
                var targetTableName = path.TableNames[i + 1];
                var targetAlias = $"r{i + 1}";
                var joinTableAlias = $"jt{i + 1}";
                
                // First join: source -> join table
                // source.Key = joinTable.ownerFK
                sqlBuilder.AppendLine($"                INNER JOIN {joinTableName} {joinTableAlias} ON {sourceAlias}.{fkColumn} = {joinTableAlias}.{joinTableOwnerFk}");
                
                // Second join: join table -> target
                // joinTable.targetFK = target.Key
                var targetKeyColumn = MetadataHelper.GetKeyColumnName(info, path.EntityNames[i + 1]);
                sqlBuilder.AppendLine($"                INNER JOIN {targetTableName} {targetAlias} ON {joinTableAlias}.{joinTableTargetFk} = {targetAlias}.{targetKeyColumn}");
            }
            else if (rel.Type == Models.RelationshipType.OneToOne && rel.IsOwner)
            {
                // OneToOne owner: FK is on target, join is reversed
                // source.Key = target.FK
                var targetTableName = path.TableNames[i + 1];
                var targetAlias = $"r{i + 1}";
                var joinCondition = $"{sourceAlias}.{fkColumn} = {targetAlias}.{keyColumn}";
                sqlBuilder.AppendLine($"                INNER JOIN {targetTableName} {targetAlias} ON {joinCondition}");
            }
            else
            {
                // ManyToOne or OneToOne inverse: Standard join
                // source.FK = target.Key
                var targetTableName = path.TableNames[i + 1];
                var targetAlias = $"r{i + 1}";
                var joinCondition = $"{sourceAlias}.{fkColumn} = {targetAlias}.{keyColumn}";
                sqlBuilder.AppendLine($"                INNER JOIN {targetTableName} {targetAlias} ON {joinCondition}");
            }
        }

        // Add WHERE clause
        var lastTableAlias = path.Relationships.Count > 0 ? $"r{path.Relationships.Count}" : "e";
        sqlBuilder.AppendLine($"                WHERE {lastTableAlias}.{property.ColumnName} = @{propertyParamName}");

        // Generate method without pagination
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities by navigating through {pathDescription} to {targetEntityName}.{property.Name}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName})");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = @\"{sqlBuilder.ToString().TrimEnd()}");
        sb.AppendLine($"                ORDER BY e.{keyColumnName}\";");
        sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {propertyParamName} }});");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate method with pagination
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds {info.EntityType} entities by navigating through {pathDescription} to {targetEntityName}.{property.Name}, with pagination support.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName}, int skip, int take)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = @\"{sqlBuilder.ToString().TrimEnd()}");
        sb.AppendLine($"                ORDER BY e.{keyColumnName} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
        sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {propertyParamName}, skip, take }});");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate method with pagination and sorting
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds {info.EntityType} entities by navigating through {pathDescription} to {targetEntityName}.{property.Name}, with pagination and sorting support.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName}, int skip, int take, string? orderBy = null, bool ascending = true)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var orderByColumn = GetColumnNameForProperty(orderBy, \"{keyColumnName}\");");
        sb.AppendLine($"            var direction = ascending ? \"ASC\" : \"DESC\";");
        sb.AppendLine($"            var sql = $\"{sqlBuilder.ToString().TrimEnd()}");
        sb.AppendLine($"                ORDER BY e.{{orderByColumn}} {{direction}} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
        sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {propertyParamName}, skip, take }});");
        sb.AppendLine("        }");
        sb.AppendLine();
    }


    /// <summary>
    /// Generates complex filter queries with OR/AND combinations for ManyToOne relationships.
    /// For example, FindByCustomerOrSupplierAsync, FindByCustomerAndStatusAsync.
    /// </summary>
    public static void GenerateComplexFilters(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string tableName)
    {
        var entityName = info.EntityType.Split('.').Last();
        var keyColumnName = MetadataHelper.GetKeyColumnName(info);
        var targetEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        var foreignKeyColumn = relationship.JoinColumn?.Name ?? $"{targetEntitySimpleName}Id";
        var relatedKeyType = MetadataHelper.GetRelatedEntityKeyType(info, relationship.TargetEntityType);
        var paramName = StringHelper.ToCamelCase(targetEntitySimpleName) + "Id";

        // Generate OR combinations: FindBy{Property1}Or{Property2}Async (requires at least 2 relationships)
        if (info.Relationships.Count >= 2)
        {
            foreach (var otherRel in info.Relationships)
        {
            if (otherRel == relationship || otherRel.Type != Models.RelationshipType.ManyToOne)
                continue;

            var otherEntitySimpleName = otherRel.TargetEntityType.Split('.').Last();
            var otherFkColumn = otherRel.JoinColumn?.Name ?? $"{otherEntitySimpleName}Id";
            var otherKeyType = MetadataHelper.GetRelatedEntityKeyType(info, otherRel.TargetEntityType);
            var otherParamName = StringHelper.ToCamelCase(otherEntitySimpleName) + "Id";

            // Generate FindBy{Property1}Or{Property2}Async
            var orMethodName = $"FindBy{relationship.PropertyName}Or{otherRel.PropertyName}Async";
            
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName} or {otherRel.PropertyName}.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        /// <param name=\"{paramName}\">The {relationship.PropertyName} identifier (nullable).</param>");
            sb.AppendLine($"        /// <param name=\"{otherParamName}\">The {otherRel.PropertyName} identifier (nullable).</param>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {orMethodName}({relatedKeyType}? {paramName}, {otherKeyType}? {otherParamName})");
            sb.AppendLine("        {");
            sb.AppendLine($"            var conditions = new List<string>();");
            sb.AppendLine($"            var parameters = new Dictionary<string, object>();");
            sb.AppendLine();
            sb.AppendLine($"            if ({paramName}.HasValue)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                conditions.Add(\"{foreignKeyColumn} = @{paramName}\");");
            sb.AppendLine($"                parameters.Add(\"{paramName}\", {paramName}.Value);");
            sb.AppendLine($"            }}");
            sb.AppendLine();
            sb.AppendLine($"            if ({otherParamName}.HasValue)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                conditions.Add(\"{otherFkColumn} = @{otherParamName}\");");
            sb.AppendLine($"                parameters.Add(\"{otherParamName}\", {otherParamName}.Value);");
            sb.AppendLine($"            }}");
            sb.AppendLine();
            sb.AppendLine($"            if (conditions.Count == 0)");
            sb.AppendLine($"                return Enumerable.Empty<{info.EntityType}>();");
            sb.AppendLine();
            sb.AppendLine($"            var whereClause = string.Join(\" OR \", conditions);");
            sb.AppendLine($"            var sql = $\"SELECT * FROM {tableName} WHERE {{whereClause}} ORDER BY {keyColumnName}\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, parameters);");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate FindBy{Property1}Or{Property2}Async with pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} or {otherRel.PropertyName}, with pagination support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {orMethodName}({relatedKeyType}? {paramName}, {otherKeyType}? {otherParamName}, int skip, int take)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var conditions = new List<string>();");
            sb.AppendLine($"            var parameters = new Dictionary<string, object> {{ {{ \"skip\", skip }}, {{ \"take\", take }} }};");
            sb.AppendLine();
            sb.AppendLine($"            if ({paramName}.HasValue)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                conditions.Add(\"{foreignKeyColumn} = @{paramName}\");");
            sb.AppendLine($"                parameters.Add(\"{paramName}\", {paramName}.Value);");
            sb.AppendLine($"            }}");
            sb.AppendLine();
            sb.AppendLine($"            if ({otherParamName}.HasValue)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                conditions.Add(\"{otherFkColumn} = @{otherParamName}\");");
            sb.AppendLine($"                parameters.Add(\"{otherParamName}\", {otherParamName}.Value);");
            sb.AppendLine($"            }}");
            sb.AppendLine();
            sb.AppendLine($"            if (conditions.Count == 0)");
            sb.AppendLine($"                return Enumerable.Empty<{info.EntityType}>();");
            sb.AppendLine();
            sb.AppendLine($"            var whereClause = string.Join(\" OR \", conditions);");
            sb.AppendLine($"            var sql = $\"SELECT * FROM {tableName} WHERE {{whereClause}} ORDER BY {keyColumnName} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, parameters);");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate FindBy{Property1}Or{Property2}Async with pagination and sorting
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} or {otherRel.PropertyName}, with pagination and sorting support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {orMethodName}({relatedKeyType}? {paramName}, {otherKeyType}? {otherParamName}, int skip, int take, string? orderBy = null, bool ascending = true)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var orderByColumn = GetColumnNameForProperty(orderBy, \"{keyColumnName}\");");
            sb.AppendLine($"            var direction = ascending ? \"ASC\" : \"DESC\";");
            sb.AppendLine($"            var conditions = new List<string>();");
            sb.AppendLine($"            var parameters = new Dictionary<string, object> {{ {{ \"skip\", skip }}, {{ \"take\", take }} }};");
            sb.AppendLine();
            sb.AppendLine($"            if ({paramName}.HasValue)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                conditions.Add(\"{foreignKeyColumn} = @{paramName}\");");
            sb.AppendLine($"                parameters.Add(\"{paramName}\", {paramName}.Value);");
            sb.AppendLine($"            }}");
            sb.AppendLine();
            sb.AppendLine($"            if ({otherParamName}.HasValue)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                conditions.Add(\"{otherFkColumn} = @{otherParamName}\");");
            sb.AppendLine($"                parameters.Add(\"{otherParamName}\", {otherParamName}.Value);");
            sb.AppendLine($"            }}");
            sb.AppendLine();
            sb.AppendLine($"            if (conditions.Count == 0)");
            sb.AppendLine($"                return Enumerable.Empty<{info.EntityType}>();");
            sb.AppendLine();
            sb.AppendLine($"            var whereClause = string.Join(\" OR \", conditions);");
            sb.AppendLine($"            var sql = $\"SELECT * FROM {tableName} WHERE {{whereClause}} ORDER BY {{orderByColumn}} {{direction}} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, parameters);");
            sb.AppendLine("        }");
            sb.AppendLine();
            }
        }

        // Generate AND combinations with entity properties: FindBy{Property}And{PropertyName}Async
        if (info.EntityMetadata?.Properties != null)
        {
            foreach (var property in info.EntityMetadata.Properties)
            {
                if (property.IsPrimaryKey)
                    continue;

                if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") ||
                    property.TypeName.Contains("IEnumerable") || !TypeHelper.IsSimpleType(property.TypeName))
                    continue;

                var propertyParamName = StringHelper.ToCamelCase(property.Name);
                var andMethodName = $"FindBy{relationship.PropertyName}And{property.Name}Async";

                // Generate FindBy{Property}And{PropertyName}Async
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName} and {property.Name}.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        /// <param name=\"{paramName}\">The {relationship.PropertyName} identifier.</param>");
                sb.AppendLine($"        /// <param name=\"{propertyParamName}\">The {property.Name} value.</param>");
                sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {andMethodName}({relatedKeyType} {paramName}, {property.TypeName} {propertyParamName})");
                sb.AppendLine("        {");
                sb.AppendLine($"            var sql = @\"SELECT * FROM {tableName}");
                sb.AppendLine($"                WHERE {foreignKeyColumn} = @{paramName} AND {property.ColumnName} = @{propertyParamName}");
                sb.AppendLine($"                ORDER BY {keyColumnName}\";");
                sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {paramName}, {propertyParamName} }});");
                sb.AppendLine("        }");
                sb.AppendLine();

                // Generate FindBy{Property}And{PropertyName}Async with pagination
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} and {property.Name}, with pagination support.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {andMethodName}({relatedKeyType} {paramName}, {property.TypeName} {propertyParamName}, int skip, int take)");
                sb.AppendLine("        {");
                sb.AppendLine($"            var sql = @\"SELECT * FROM {tableName}");
                sb.AppendLine($"                WHERE {foreignKeyColumn} = @{paramName} AND {property.ColumnName} = @{propertyParamName}");
                sb.AppendLine($"                ORDER BY {keyColumnName} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
                sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {paramName}, {propertyParamName}, skip, take }});");
                sb.AppendLine("        }");
                sb.AppendLine();

                // Generate FindBy{Property}And{PropertyName}Async with pagination and sorting
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} and {property.Name}, with pagination and sorting support.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        public async Task<IEnumerable<{info.EntityType}>> {andMethodName}({relatedKeyType} {paramName}, {property.TypeName} {propertyParamName}, int skip, int take, string? orderBy = null, bool ascending = true)");
                sb.AppendLine("        {");
                sb.AppendLine($"            var orderByColumn = GetColumnNameForProperty(orderBy, \"{keyColumnName}\");");
                sb.AppendLine($"            var direction = ascending ? \"ASC\" : \"DESC\";");
                sb.AppendLine($"            var sql = @\"SELECT * FROM {tableName}");
                sb.AppendLine($"                WHERE {foreignKeyColumn} = @{paramName} AND {property.ColumnName} = @{propertyParamName}");
                sb.AppendLine($"                ORDER BY {{orderByColumn}} {{direction}} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY\";");
                sb.AppendLine($"            return await _connection.QueryAsync<{info.EntityType}>(sql, new {{ {paramName}, {propertyParamName}, skip, take }});");
                sb.AppendLine("        }");
                sb.AppendLine();
            }
        }
    }

    /// <summary>
    /// Checks if a type is a DateTime type.
    /// </summary>

    /// <summary>
    /// Generates aggregate methods for OneToMany relationships.
    /// For example, GetTotalOrderAmountAsync, GetAverageOrderAmountAsync, etc.
    /// </summary>
    public static void GenerateAggregateMethods(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Get child entity metadata
        var childEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(childEntitySimpleName, out var childMetadata))
        {
            return; // Can't generate aggregate methods without metadata
        }

        if (childMetadata.Properties == null)
        {
            return;
        }

        var childTableName = MetadataHelper.GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? childEntitySimpleName;
        var parentEntityName = info.EntityType.Split('.').Last();
        
        // For OneToMany, the JoinColumn is on the inverse ManyToOne relationship
        // Find the FK column from the child entity's ManyToOne relationship
        var foreignKeyColumn = GetForeignKeyColumnForOneToMany(info, relationship, parentEntityName);

        // Generate aggregate methods for each numeric property of the child entity
        foreach (var property in childMetadata.Properties)
        {
            // Skip primary key, relationships, and non-numeric types
            if (property.IsPrimaryKey)
                continue;

            if (!TypeHelper.IsNumericType(property.TypeName))
                continue;

            // Skip if property type is a collection or complex object
            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") || 
                property.TypeName.Contains("IEnumerable"))
                continue;

            var propertyName = property.Name;
            var propertyColumnName = property.ColumnName;
            var returnType = property.TypeName.TrimEnd('?'); // Remove nullable marker for return type

            // Generate SUM method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the sum of {relationship.PropertyName}.{propertyName} for the entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<{returnType}> GetTotal{relationship.PropertyName}{propertyName}Async({info.KeyType} id)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"SELECT COALESCE(SUM({propertyColumnName}), 0) FROM {childTableName} WHERE {foreignKeyColumn} = @id\";");
            sb.AppendLine($"            return await _connection.ExecuteScalarAsync<{returnType}>(sql, new {{ id }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate AVG method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the average of {relationship.PropertyName}.{propertyName} for the entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<{returnType}?> GetAverage{relationship.PropertyName}{propertyName}Async({info.KeyType} id)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"SELECT AVG({propertyColumnName}) FROM {childTableName} WHERE {foreignKeyColumn} = @id\";");
            sb.AppendLine($"            return await _connection.ExecuteScalarAsync<{returnType}?>(sql, new {{ id }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate MIN method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the minimum {relationship.PropertyName}.{propertyName} for the entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<{returnType}?> GetMin{relationship.PropertyName}{propertyName}Async({info.KeyType} id)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"SELECT MIN({propertyColumnName}) FROM {childTableName} WHERE {foreignKeyColumn} = @id\";");
            sb.AppendLine($"            return await _connection.ExecuteScalarAsync<{returnType}?>(sql, new {{ id }});");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate MAX method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the maximum {relationship.PropertyName}.{propertyName} for the entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<{returnType}?> GetMax{relationship.PropertyName}{propertyName}Async({info.KeyType} id)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"SELECT MAX({propertyColumnName}) FROM {childTableName} WHERE {foreignKeyColumn} = @id\";");
            sb.AppendLine($"            return await _connection.ExecuteScalarAsync<{returnType}?>(sql, new {{ id }});");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates GROUP BY aggregate methods for OneToMany relationships.
    /// For example, GetOrderCountsByCustomerAsync, GetTotalOrderAmountsByCustomerAsync, etc.
    /// These methods return Dictionary&lt;ParentKeyType, AggregateType&gt; grouped by parent entity.
    /// </summary>
    public static void GenerateGroupByAggregateMethods(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Get child entity metadata
        var childEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(childEntitySimpleName, out var childMetadata))
        {
            return; // Can't generate GROUP BY methods without metadata
        }

        if (childMetadata.Properties == null)
        {
            return;
        }

        var childTableName = MetadataHelper.GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? childEntitySimpleName;
        var parentEntityName = info.EntityType.Split('.').Last();
        
        // For OneToMany, the JoinColumn is on the inverse ManyToOne relationship
        var foreignKeyColumn = GetForeignKeyColumnForOneToMany(info, relationship, parentEntityName);
        
        // Get parent entity key type and column name
        var parentKeyType = info.KeyType;
        var parentKeyColumnName = MetadataHelper.GetKeyColumnName(info, info.EntityType);

        // Generate COUNT method (always available, doesn't require numeric property)
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets the count of {relationship.PropertyName} grouped by parent entity.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<Dictionary<{parentKeyType}, int>> Get{relationship.PropertyName}CountsBy{parentEntityName}Async()");
        sb.AppendLine("        {");
        sb.AppendLine($"            var sql = $\"SELECT {foreignKeyColumn} AS Key, COUNT(*) AS Value FROM {childTableName} GROUP BY {foreignKeyColumn}\";");
        sb.AppendLine($"            var results = await _connection.QueryAsync<({parentKeyType} Key, int Value)>(sql);");
        sb.AppendLine("            return results.ToDictionary(r => r.Key, r => r.Value);");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate aggregate methods for each numeric property of the child entity
        foreach (var property in childMetadata.Properties)
        {
            // Skip primary key, relationships, and non-numeric types
            if (property.IsPrimaryKey)
                continue;

            if (!TypeHelper.IsNumericType(property.TypeName))
                continue;

            // Skip if property type is a collection or complex object
            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") || 
                property.TypeName.Contains("IEnumerable"))
                continue;

            var propertyName = property.Name;
            var propertyColumnName = property.ColumnName;
            var returnType = property.TypeName.TrimEnd('?'); // Remove nullable marker for return type

            // Generate SUM GROUP BY method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the sum of {relationship.PropertyName}.{propertyName} grouped by parent entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<Dictionary<{parentKeyType}, {returnType}>> GetTotal{relationship.PropertyName}{propertyName}By{parentEntityName}Async()");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"SELECT {foreignKeyColumn} AS Key, COALESCE(SUM({propertyColumnName}), 0) AS Value FROM {childTableName} GROUP BY {foreignKeyColumn}\";");
            sb.AppendLine($"            var results = await _connection.QueryAsync<({parentKeyType} Key, {returnType} Value)>(sql);");
            sb.AppendLine("            return results.ToDictionary(r => r.Key, r => r.Value);");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate AVG GROUP BY method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the average of {relationship.PropertyName}.{propertyName} grouped by parent entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<Dictionary<{parentKeyType}, {returnType}?>> GetAverage{relationship.PropertyName}{propertyName}By{parentEntityName}Async()");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"SELECT {foreignKeyColumn} AS Key, AVG({propertyColumnName}) AS Value FROM {childTableName} GROUP BY {foreignKeyColumn}\";");
            sb.AppendLine($"            var results = await _connection.QueryAsync<({parentKeyType} Key, {returnType}? Value)>(sql);");
            sb.AppendLine("            return results.ToDictionary(r => r.Key, r => r.Value);");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate MIN GROUP BY method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the minimum {relationship.PropertyName}.{propertyName} grouped by parent entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<Dictionary<{parentKeyType}, {returnType}?>> GetMin{relationship.PropertyName}{propertyName}By{parentEntityName}Async()");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"SELECT {foreignKeyColumn} AS Key, MIN({propertyColumnName}) AS Value FROM {childTableName} GROUP BY {foreignKeyColumn}\";");
            sb.AppendLine($"            var results = await _connection.QueryAsync<({parentKeyType} Key, {returnType}? Value)>(sql);");
            sb.AppendLine("            return results.ToDictionary(r => r.Key, r => r.Value);");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate MAX GROUP BY method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the maximum {relationship.PropertyName}.{propertyName} grouped by parent entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<Dictionary<{parentKeyType}, {returnType}?>> GetMax{relationship.PropertyName}{propertyName}By{parentEntityName}Async()");
            sb.AppendLine("        {");
            sb.AppendLine($"            var sql = $\"SELECT {foreignKeyColumn} AS Key, MAX({propertyColumnName}) AS Value FROM {childTableName} GROUP BY {foreignKeyColumn}\";");
            sb.AppendLine($"            var results = await _connection.QueryAsync<({parentKeyType} Key, {returnType}? Value)>(sql);");
            sb.AppendLine("            return results.ToDictionary(r => r.Key, r => r.Value);");
            sb.AppendLine("        }");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates multi-entity GROUP BY aggregate methods with JOINs for OneToMany relationships.
    /// For example, GetCustomerOrderSummaryAsync that returns CustomerId, CustomerName, OrderCount, TotalAmount.
    /// These methods JOIN the parent table with the child table and return tuples with parent properties and aggregates.
    /// </summary>
    public static void GenerateMultiEntityGroupByAggregateMethods(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Get child entity metadata
        var childEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(childEntitySimpleName, out var childMetadata))
        {
            return; // Can't generate methods without metadata
        }

        if (childMetadata.Properties == null || info.EntityMetadata?.Properties == null)
        {
            return;
        }

        var childTableName = MetadataHelper.GetTableNameFromMetadata(info, relationship.TargetEntityType) ?? childEntitySimpleName;
        var parentTableName = info.EntityMetadata.TableName ?? info.EntityType.Split('.').Last();
        var parentEntityName = info.EntityType.Split('.').Last();
        
        // For OneToMany, the JoinColumn is on the inverse ManyToOne relationship
        var foreignKeyColumn = GetForeignKeyColumnForOneToMany(info, relationship, parentEntityName);
        
        // Get parent entity key type and column name
        var parentKeyType = info.KeyType;
        var parentKeyColumnName = MetadataHelper.GetKeyColumnName(info, info.EntityType);

        // Find the inverse ManyToOne relationship to get the correct FK column
        // The FK column is on the child table pointing to the parent
        var parentKeyColumn = parentKeyColumnName;

        // Generate summary method with COUNT and all numeric aggregates
        // First, find all numeric properties in the child entity
        var numericProperties = childMetadata.Properties
            .Where(p => !p.IsPrimaryKey && TypeHelper.IsNumericType(p.TypeName) && 
                       !p.TypeName.Contains("ICollection") && !p.TypeName.Contains("List") && 
                       !p.TypeName.Contains("IEnumerable"))
            .ToList();

        if (numericProperties.Count == 0)
        {
            return; // No numeric properties to aggregate
        }

        // Get parent entity simple properties (for inclusion in result)
        var parentSimpleProperties = info.EntityMetadata.Properties
            .Where(p => !p.IsPrimaryKey && TypeHelper.IsSimpleType(p.TypeName) && 
                       !p.TypeName.Contains("ICollection") && !p.TypeName.Contains("List") && 
                       !p.TypeName.Contains("IEnumerable"))
            .Take(5) // Limit to 5 properties to avoid overly complex tuples
            .ToList();

        // Build tuple type for return value
        var tupleElements = new List<string>();
        tupleElements.Add($"{parentKeyType} {parentEntityName}Id");
        
        foreach (var prop in parentSimpleProperties)
        {
            tupleElements.Add($"{prop.TypeName} {prop.Name}");
        }
        
        var countPropertyName = $"{relationship.PropertyName}Count";
        tupleElements.Add($"int {countPropertyName}");
        
        foreach (var prop in numericProperties)
        {
            var returnType = prop.TypeName.TrimEnd('?');
            tupleElements.Add($"{returnType} Total{prop.Name}");
        }

        var tupleType = $"({string.Join(", ", tupleElements)})";

        // Generate method name
        var methodName = $"Get{parentEntityName}{relationship.PropertyName}SummaryAsync";

        // Generate method
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets a summary of {relationship.PropertyName} grouped by {parentEntityName}, including parent entity properties and aggregates.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public async Task<IEnumerable<{tupleType}>> {methodName}()");
        sb.AppendLine("        {");
        
        // Build SELECT clause
        var selectParts = new List<string>();
        selectParts.Add($"p.{parentKeyColumn} AS {parentEntityName}Id");
        
        foreach (var prop in parentSimpleProperties)
        {
            selectParts.Add($"p.{prop.ColumnName} AS {prop.Name}");
        }
        
        selectParts.Add($"COUNT(c.{foreignKeyColumn}) AS {countPropertyName}");
        
        foreach (var prop in numericProperties)
        {
            selectParts.Add($"COALESCE(SUM(c.{prop.ColumnName}), 0) AS Total{prop.Name}");
        }

        var selectClause = string.Join(", ", selectParts);

        // Build SQL with JOIN
        sb.AppendLine($"            var sql = @\"SELECT {selectClause}");
        sb.AppendLine($"                FROM {parentTableName} p");
        sb.AppendLine($"                LEFT JOIN {childTableName} c ON p.{parentKeyColumn} = c.{foreignKeyColumn}");
        sb.AppendLine($"                GROUP BY p.{parentKeyColumn}");
        
        // Add parent properties to GROUP BY
        foreach (var prop in parentSimpleProperties)
        {
            sb.AppendLine($", p.{prop.ColumnName}");
        }
        
        sb.AppendLine("\";");
        sb.AppendLine($"            return await _connection.QueryAsync<{tupleType}>(sql);");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    /// <summary>
    /// Checks if a type is a simple type that can be used in WHERE clauses.
    /// </summary>

    // Generate separate interface for relationship query methods
    public static string GeneratePartialInterface(RepositoryInfo info)
    {
        var sb = new StringBuilder();

        // Create interface name with Partial suffix (e.g., IOrderRepositoryPartial)
        var partialInterfaceName = info.InterfaceName + "Partial";

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// This code was generated by NPA.Design.RepositoryGenerator");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
        sb.AppendLine($"namespace {info.Namespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Extended interface for {info.InterfaceName} with relationship query methods.");
        sb.AppendLine($"    /// This interface is automatically implemented by the generated repository.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public interface {partialInterfaceName}");
        sb.AppendLine("    {");

        var entityName = info.EntityType.Split('.').Last();

        foreach (var relationship in info.Relationships)
        {
            // Generate GetByIdWith{Property}Async signature
            GenerateGetByIdWithPropertySignature(sb, info, relationship, entityName);

            // Generate Load{Property}Async signature for lazy loading
            if (relationship.FetchType == Models.FetchType.Lazy)
            {
                GenerateLoadPropertySignature(sb, info, relationship, entityName);
            }

            // Generate FindBy method signatures for ManyToOne relationships
            if (relationship.Type == Models.RelationshipType.ManyToOne)
            {
                GenerateFindByParentSignature(sb, info, relationship);
                GenerateCountByParentSignature(sb, info, relationship);
                GeneratePropertyBasedQuerySignatures(sb, info, relationship);
                GenerateAdvancedFilterSignatures(sb, info, relationship);
                GenerateComplexFilterSignatures(sb, info, relationship);
            }

            // Generate Has/Count method signatures for OneToMany relationships
            if (relationship.Type == Models.RelationshipType.OneToMany && !string.IsNullOrEmpty(relationship.MappedBy))
            {
                GenerateHasChildrenSignature(sb, info, relationship);
                GenerateCountChildrenSignature(sb, info, relationship);
                GenerateAggregateMethodSignatures(sb, info, relationship);
                GenerateGroupByAggregateMethodSignatures(sb, info, relationship);
                GenerateMultiEntityGroupByAggregateMethodSignatures(sb, info, relationship);
                GenerateSubqueryFilterSignatures(sb, info, relationship);
                GenerateInverseRelationshipQuerySignatures(sb, info, relationship);
            }
        }

        // Generate multi-level navigation query signatures
        GenerateMultiLevelNavigationQuerySignatures(sb, info);

        // Generate cascade operation method signatures if applicable
        var persistCascades = info.CascadeRelationships.Where(r => (r.CascadeTypes & Models.CascadeType.Persist) != 0).ToList();
        if (persistCascades.Any())
        {
            GenerateAddWithCascadeSignature(sb, info, persistCascades, entityName);
        }

        var mergeCascades = info.CascadeRelationships.Where(r => (r.CascadeTypes & Models.CascadeType.Merge) != 0).ToList();
        if (mergeCascades.Any())
        {
            GenerateUpdateWithCascadeSignature(sb, info, mergeCascades, entityName);
        }

        var removeCascades = info.CascadeRelationships.Where(r => (r.CascadeTypes & Models.CascadeType.Remove) != 0).ToList();
        if (removeCascades.Any())
        {
            GenerateDeleteWithCascadeSignature(sb, info, removeCascades, entityName);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public static void GenerateGetByIdWithPropertySignature(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string entityName)
    {
        var propertyName = relationship.PropertyName;
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets a {entityName} by its ID with {propertyName} loaded asynchronously.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <param name=\"id\">The {entityName} identifier.</param>");
        sb.AppendLine($"        /// <returns>The {entityName} with {propertyName} loaded if found; otherwise, null.</returns>");
        sb.AppendLine($"        Task<{info.EntityType}?> GetByIdWith{propertyName}Async({info.KeyType} id);");
        sb.AppendLine();
    }

    public static void GenerateLoadPropertySignature(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship, string entityName)
    {
        var propertyName = relationship.PropertyName;
        var relatedTypeName = relationship.TargetEntityType;
        var relatedTypeFullName = relationship.TargetEntityFullType;

        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Loads {propertyName} for an existing {entityName} entity asynchronously.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        /// <param name=\"entity\">The {entityName} entity.</param>");

        if (relationship.IsCollection)
        {
            sb.AppendLine($"        /// <returns>A collection of {relatedTypeName} entities.</returns>");
            sb.AppendLine($"        Task<IEnumerable<{relatedTypeFullName}>> Load{propertyName}Async({info.EntityType} entity);");
        }
        else
        {
            sb.AppendLine($"        /// <returns>The loaded {relatedTypeName} entity if found; otherwise, null.</returns>");
            var returnType = relatedTypeFullName.TrimEnd('?');
            sb.AppendLine($"        Task<{returnType}?> Load{propertyName}Async({info.EntityType} entity);");
        }
        sb.AppendLine();
    }

    public static void GenerateFindByParentSignature(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        var targetEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        var paramName = StringHelper.ToCamelCase(targetEntitySimpleName) + "Id";
        var relatedKeyType = MetadataHelper.GetRelatedEntityKeyType(info, relationship.TargetEntityType);

        // Signature without pagination
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}IdAsync({relatedKeyType} {paramName});");
        sb.AppendLine();

        // Signature with pagination
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} with pagination support.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}IdAsync({relatedKeyType} {paramName}, int skip, int take);");
        sb.AppendLine();

        // Signature with pagination and sorting
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} with pagination and sorting support.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}IdAsync({relatedKeyType} {paramName}, int skip, int take, string? orderBy = null, bool ascending = true);");
        sb.AppendLine();
    }

    public static void GenerateCountByParentSignature(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        var targetEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        var paramName = StringHelper.ToCamelCase(targetEntitySimpleName) + "Id";
        var relatedKeyType = MetadataHelper.GetRelatedEntityKeyType(info, relationship.TargetEntityType);

        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Counts {info.EntityType} entities by {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<int> CountBy{relationship.PropertyName}IdAsync({relatedKeyType} {paramName});");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates property-based query method signatures for the interface.
    /// </summary>
    public static void GeneratePropertyBasedQuerySignatures(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Get related entity metadata
        var relatedEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(relatedEntitySimpleName, out var relatedMetadata))
        {
            return; // Can't generate signatures without metadata
        }

        if (relatedMetadata.Properties == null)
        {
            return;
        }

        // Generate signatures for each property of the related entity
        foreach (var property in relatedMetadata.Properties)
        {
            // Skip primary key, relationships, and complex types
            if (property.IsPrimaryKey)
                continue;

            // Skip if property type is a collection or complex object
            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") || 
                property.TypeName.Contains("IEnumerable") || !TypeHelper.IsSimpleType(property.TypeName))
                continue;

            var propertyParamName = StringHelper.ToCamelCase(property.Name);
            var methodName = $"FindBy{relationship.PropertyName}{property.Name}Async";

            // Signature without pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName}.{property.Name}.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName});");
            sb.AppendLine();

            // Signature with pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName}.{property.Name} with pagination support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName}, int skip, int take);");
            sb.AppendLine();

            // Signature with pagination and sorting
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName}.{property.Name} with pagination and sorting support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName}, int skip, int take, string? orderBy = null, bool ascending = true);");
            sb.AppendLine();
        }
    }

    public static void GenerateHasChildrenSignature(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Checks if the entity has any {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<bool> Has{relationship.PropertyName}Async({info.KeyType} id);");
        sb.AppendLine();
    }

    public static void GenerateCountChildrenSignature(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Counts the number of {relationship.PropertyName} for the entity.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<int> Count{relationship.PropertyName}Async({info.KeyType} id);");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates aggregate method signatures for the interface.
    /// </summary>
    public static void GenerateAggregateMethodSignatures(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Get child entity metadata
        var childEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(childEntitySimpleName, out var childMetadata))
        {
            return; // Can't generate signatures without metadata
        }

        if (childMetadata.Properties == null)
        {
            return;
        }

        // Generate signatures for each numeric property of the child entity
        foreach (var property in childMetadata.Properties)
        {
            // Skip primary key, relationships, and non-numeric types
            if (property.IsPrimaryKey)
                continue;

            if (!TypeHelper.IsNumericType(property.TypeName))
                continue;

            // Skip if property type is a collection or complex object
            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") || 
                property.TypeName.Contains("IEnumerable"))
                continue;

            var propertyName = property.Name;
            var returnType = property.TypeName.TrimEnd('?'); // Remove nullable marker for return type

            // Generate SUM signature
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the sum of {relationship.PropertyName}.{propertyName} for the entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<{returnType}> GetTotal{relationship.PropertyName}{propertyName}Async({info.KeyType} id);");
            sb.AppendLine();

            // Generate AVG signature
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the average of {relationship.PropertyName}.{propertyName} for the entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<{returnType}?> GetAverage{relationship.PropertyName}{propertyName}Async({info.KeyType} id);");
            sb.AppendLine();

            // Generate MIN signature
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the minimum {relationship.PropertyName}.{propertyName} for the entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<{returnType}?> GetMin{relationship.PropertyName}{propertyName}Async({info.KeyType} id);");
            sb.AppendLine();

            // Generate MAX signature
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the maximum {relationship.PropertyName}.{propertyName} for the entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<{returnType}?> GetMax{relationship.PropertyName}{propertyName}Async({info.KeyType} id);");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates GROUP BY aggregate method signatures for the interface.
    /// </summary>
    public static void GenerateGroupByAggregateMethodSignatures(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Get child entity metadata
        var childEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(childEntitySimpleName, out var childMetadata))
        {
            return; // Can't generate signatures without metadata
        }

        if (childMetadata.Properties == null)
        {
            return;
        }

        var parentEntityName = info.EntityType.Split('.').Last();
        var parentKeyType = info.KeyType;

        // Generate COUNT signature (always available)
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets the count of {relationship.PropertyName} grouped by parent entity.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<Dictionary<{parentKeyType}, int>> Get{relationship.PropertyName}CountsBy{parentEntityName}Async();");
        sb.AppendLine();

        // Generate signatures for each numeric property of the child entity
        foreach (var property in childMetadata.Properties)
        {
            // Skip primary key, relationships, and non-numeric types
            if (property.IsPrimaryKey)
                continue;

            if (!TypeHelper.IsNumericType(property.TypeName))
                continue;

            // Skip if property type is a collection or complex object
            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") || 
                property.TypeName.Contains("IEnumerable"))
                continue;

            var propertyName = property.Name;
            var returnType = property.TypeName.TrimEnd('?'); // Remove nullable marker for return type

            // Generate SUM GROUP BY signature
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the sum of {relationship.PropertyName}.{propertyName} grouped by parent entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<Dictionary<{parentKeyType}, {returnType}>> GetTotal{relationship.PropertyName}{propertyName}By{parentEntityName}Async();");
            sb.AppendLine();

            // Generate AVG GROUP BY signature
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the average of {relationship.PropertyName}.{propertyName} grouped by parent entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<Dictionary<{parentKeyType}, {returnType}?>> GetAverage{relationship.PropertyName}{propertyName}By{parentEntityName}Async();");
            sb.AppendLine();

            // Generate MIN GROUP BY signature
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the minimum {relationship.PropertyName}.{propertyName} grouped by parent entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<Dictionary<{parentKeyType}, {returnType}?>> GetMin{relationship.PropertyName}{propertyName}By{parentEntityName}Async();");
            sb.AppendLine();

            // Generate MAX GROUP BY signature
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Gets the maximum {relationship.PropertyName}.{propertyName} grouped by parent entity.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<Dictionary<{parentKeyType}, {returnType}?>> GetMax{relationship.PropertyName}{propertyName}By{parentEntityName}Async();");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates advanced filter method signatures for the interface.
    /// </summary>
    public static void GenerateAdvancedFilterSignatures(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Get current entity metadata for date/amount filters
        if (info.EntityMetadata?.Properties == null)
        {
            return;
        }

        var relatedKeyType = MetadataHelper.GetRelatedEntityKeyType(info, relationship.TargetEntityType);
        var targetEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        var relatedKeyParamName = StringHelper.ToCamelCase(targetEntitySimpleName) + "Id";

        // Generate date range filter signatures for DateTime properties
        foreach (var property in info.EntityMetadata.Properties)
        {
            if (property.IsPrimaryKey)
                continue;

            if (!TypeHelper.IsDateTimeType(property.TypeName))
                continue;

            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") ||
                property.TypeName.Contains("IEnumerable"))
                continue;

            // Signature without pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName} and {property.Name} date range.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}And{property.Name}RangeAsync({relatedKeyType} {relatedKeyParamName}, DateTime start{property.Name}, DateTime end{property.Name});");
            sb.AppendLine();

            // Signature with pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} and {property.Name} date range with pagination support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}And{property.Name}RangeAsync({relatedKeyType} {relatedKeyParamName}, DateTime start{property.Name}, DateTime end{property.Name}, int skip, int take);");
            sb.AppendLine();

            // Signature with pagination and sorting
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} and {property.Name} date range with pagination and sorting support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindBy{relationship.PropertyName}And{property.Name}RangeAsync({relatedKeyType} {relatedKeyParamName}, DateTime start{property.Name}, DateTime end{property.Name}, int skip, int take, string? orderBy = null, bool ascending = true);");
            sb.AppendLine();
        }

        // Generate amount/quantity filter signatures for numeric properties
        foreach (var property in info.EntityMetadata.Properties)
        {
            if (property.IsPrimaryKey)
                continue;

            if (!TypeHelper.IsNumericType(property.TypeName))
                continue;

            if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") ||
                property.TypeName.Contains("IEnumerable"))
                continue;

            var returnType = property.TypeName.TrimEnd('?');

            // Signature without pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName} with {property.Name} greater than or equal to the specified value.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> Find{relationship.PropertyName}{property.Name}AboveAsync({relatedKeyType} {relatedKeyParamName}, {returnType} min{property.Name});");
            sb.AppendLine();

            // Signature with pagination
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} with {property.Name} greater than or equal to the specified value, with pagination support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> Find{relationship.PropertyName}{property.Name}AboveAsync({relatedKeyType} {relatedKeyParamName}, {returnType} min{property.Name}, int skip, int take);");
            sb.AppendLine();

            // Signature with pagination and sorting
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} with {property.Name} greater than or equal to the specified value, with pagination and sorting support.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> Find{relationship.PropertyName}{property.Name}AboveAsync({relatedKeyType} {relatedKeyParamName}, {returnType} min{property.Name}, int skip, int take, string? orderBy = null, bool ascending = true);");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates complex filter query method signatures for the interface.
    /// </summary>
    public static void GenerateComplexFilterSignatures(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        var targetEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        var relatedKeyType = MetadataHelper.GetRelatedEntityKeyType(info, relationship.TargetEntityType);
        var paramName = StringHelper.ToCamelCase(targetEntitySimpleName) + "Id";

        // Generate OR combination signatures (requires at least 2 relationships)
        if (info.Relationships.Count >= 2)
        {
            foreach (var otherRel in info.Relationships)
            {
                if (otherRel == relationship || otherRel.Type != Models.RelationshipType.ManyToOne)
                    continue;

                var otherEntitySimpleName = otherRel.TargetEntityType.Split('.').Last();
                var otherKeyType = MetadataHelper.GetRelatedEntityKeyType(info, otherRel.TargetEntityType);
                var otherParamName = StringHelper.ToCamelCase(otherEntitySimpleName) + "Id";
                var orMethodName = $"FindBy{relationship.PropertyName}Or{otherRel.PropertyName}Async";

                // Signature without pagination
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName} or {otherRel.PropertyName}.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {orMethodName}({relatedKeyType}? {paramName}, {otherKeyType}? {otherParamName});");
                sb.AppendLine();

                // Signature with pagination
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} or {otherRel.PropertyName}, with pagination support.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {orMethodName}({relatedKeyType}? {paramName}, {otherKeyType}? {otherParamName}, int skip, int take);");
                sb.AppendLine();

                // Signature with pagination and sorting
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} or {otherRel.PropertyName}, with pagination and sorting support.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {orMethodName}({relatedKeyType}? {paramName}, {otherKeyType}? {otherParamName}, int skip, int take, string? orderBy = null, bool ascending = true);");
                sb.AppendLine();
            }
        }

        // Generate AND combination signatures with entity properties
        if (info.EntityMetadata?.Properties != null)
        {
            foreach (var property in info.EntityMetadata.Properties)
            {
                if (property.IsPrimaryKey)
                    continue;

                if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") ||
                    property.TypeName.Contains("IEnumerable") || !TypeHelper.IsSimpleType(property.TypeName))
                    continue;

                var propertyParamName = StringHelper.ToCamelCase(property.Name);
                var andMethodName = $"FindBy{relationship.PropertyName}And{property.Name}Async";

                // Signature without pagination
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds all {info.EntityType} entities by {relationship.PropertyName} and {property.Name}.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {andMethodName}({relatedKeyType} {paramName}, {property.TypeName} {propertyParamName});");
                sb.AppendLine();

                // Signature with pagination
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} and {property.Name}, with pagination support.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {andMethodName}({relatedKeyType} {paramName}, {property.TypeName} {propertyParamName}, int skip, int take);");
                sb.AppendLine();

                // Signature with pagination and sorting
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds {info.EntityType} entities by {relationship.PropertyName} and {property.Name}, with pagination and sorting support.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {andMethodName}({relatedKeyType} {paramName}, {property.TypeName} {propertyParamName}, int skip, int take, string? orderBy = null, bool ascending = true);");
                sb.AppendLine();
            }
        }
    }

    /// <summary>
    /// Generates multi-entity GROUP BY aggregate method signatures for the interface.
    /// </summary>
    public static void GenerateMultiEntityGroupByAggregateMethodSignatures(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Get child entity metadata
        var childEntitySimpleName = relationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata == null || !info.EntitiesMetadata.TryGetValue(childEntitySimpleName, out var childMetadata))
        {
            return; // Can't generate signatures without metadata
        }

        if (childMetadata.Properties == null || info.EntityMetadata?.Properties == null)
        {
            return;
        }

        var parentEntityName = info.EntityType.Split('.').Last();
        var parentKeyType = info.KeyType;

        // Find numeric properties in child entity
        var numericProperties = childMetadata.Properties
            .Where(p => !p.IsPrimaryKey && TypeHelper.IsNumericType(p.TypeName) && 
                       !p.TypeName.Contains("ICollection") && !p.TypeName.Contains("List") && 
                       !p.TypeName.Contains("IEnumerable"))
            .ToList();

        if (numericProperties.Count == 0)
        {
            return; // No numeric properties to aggregate
        }

        // Get parent entity simple properties
        var parentSimpleProperties = info.EntityMetadata.Properties
            .Where(p => !p.IsPrimaryKey && TypeHelper.IsSimpleType(p.TypeName) && 
                       !p.TypeName.Contains("ICollection") && !p.TypeName.Contains("List") && 
                       !p.TypeName.Contains("IEnumerable"))
            .Take(5)
            .ToList();

        // Build tuple type
        var tupleElements = new List<string>();
        tupleElements.Add($"{parentKeyType} {parentEntityName}Id");
        
        foreach (var prop in parentSimpleProperties)
        {
            tupleElements.Add($"{prop.TypeName} {prop.Name}");
        }
        
        var countPropertyName = $"{relationship.PropertyName}Count";
        tupleElements.Add($"int {countPropertyName}");
        
        foreach (var prop in numericProperties)
        {
            var returnType = prop.TypeName.TrimEnd('?');
            tupleElements.Add($"{returnType} Total{prop.Name}");
        }

        var tupleType = $"({string.Join(", ", tupleElements)})";
        var methodName = $"Get{parentEntityName}{relationship.PropertyName}SummaryAsync";

        // Generate signature
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Gets a summary of {relationship.PropertyName} grouped by {parentEntityName}, including parent entity properties and aggregates.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{tupleType}>> {methodName}();");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates subquery filter method signatures for the interface.
    /// </summary>
    public static void GenerateSubqueryFilterSignatures(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Signature without pagination
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities that have at least the specified number of {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindWithMinimum{relationship.PropertyName}Async(int minCount);");
        sb.AppendLine();

        // Signature with pagination
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds {info.EntityType} entities that have at least the specified number of {relationship.PropertyName}, with pagination support.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindWithMinimum{relationship.PropertyName}Async(int minCount, int skip, int take);");
        sb.AppendLine();

        // Signature with pagination and sorting
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds {info.EntityType} entities that have at least the specified number of {relationship.PropertyName}, with pagination and sorting support.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindWithMinimum{relationship.PropertyName}Async(int minCount, int skip, int take, string? orderBy = null, bool ascending = true);");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates inverse relationship query method signatures for the interface.
    /// </summary>
    public static void GenerateInverseRelationshipQuerySignatures(StringBuilder sb, RepositoryInfo info, Models.RelationshipMetadata relationship)
    {
        // Signature for FindWith{Property}Async
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities that have at least one {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindWith{relationship.PropertyName}Async();");
        sb.AppendLine();

        // Signature for FindWithout{Property}Async
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities that have no {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindWithout{relationship.PropertyName}Async();");
        sb.AppendLine();

        // Signature for FindWith{Property}CountAsync
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Finds all {info.EntityType} entities that have at least the specified number of {relationship.PropertyName}.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> FindWith{relationship.PropertyName}CountAsync(int minCount);");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates multi-level navigation query method signatures for the interface.
    /// </summary>
    public static void GenerateMultiLevelNavigationQuerySignatures(StringBuilder sb, RepositoryInfo info)
    {
        if (info.EntitiesMetadata == null || info.EntitiesMetadata.Count == 0)
            return;

        if (info.Compilation == null)
            return; // Need compilation to extract relationships

        var entityName = info.EntityType.Split('.').Last();
        var tableName = info.EntityMetadata?.TableName ?? entityName;

        // Find all valid navigation paths (2+ levels) - same logic as implementation
        var navigationPaths = FindNavigationPaths(info, entityName, tableName, maxDepth: 5);

        // Generate signatures for each navigation path
        foreach (var path in navigationPaths)
        {
            if (path.Relationships.Count < 2)
                continue; // Skip single-level paths (handled elsewhere)

            var targetEntityName = path.EntityNames.Last();
            var targetMetadata = info.EntitiesMetadata.TryGetValue(targetEntityName, out var metadata) ? metadata : null;
            if (targetMetadata?.Properties == null)
                continue;

            var pathDescription = path.PathDescription;

            // Generate property-based query signatures for target entity properties
            foreach (var property in targetMetadata.Properties)
            {
                if (property.IsPrimaryKey)
                    continue;

                if (property.TypeName.Contains("ICollection") || property.TypeName.Contains("List") ||
                    property.TypeName.Contains("IEnumerable") || !TypeHelper.IsSimpleType(property.TypeName))
                    continue;

                // Build method name from path
                var methodNameParts = new List<string> { "FindBy" };
                for (int i = 1; i < path.EntityNames.Count; i++)
                {
                    methodNameParts.Add(path.EntityNames[i]);
                }
                methodNameParts.Add(property.Name);
                var methodName = string.Join("", methodNameParts) + "Async";
                var propertyParamName = StringHelper.ToCamelCase(property.Name);

                // Signature without pagination
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds all {info.EntityType} entities by navigating through {pathDescription} to {targetEntityName}.{property.Name}.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName});");
                sb.AppendLine();

                // Signature with pagination
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds {info.EntityType} entities by navigating through {pathDescription} to {targetEntityName}.{property.Name}, with pagination support.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName}, int skip, int take);");
                sb.AppendLine();

                // Signature with pagination and sorting
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Finds {info.EntityType} entities by navigating through {pathDescription} to {targetEntityName}.{property.Name}, with pagination and sorting support.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        Task<IEnumerable<{info.EntityType}>> {methodName}({property.TypeName} {propertyParamName}, int skip, int take, string? orderBy = null, bool ascending = true);");
                sb.AppendLine();
            }
        }
    }

    public static void GenerateAddWithCascadeSignature(StringBuilder sb, RepositoryInfo info, List<Models.RelationshipMetadata> cascades, string entityName)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Adds a {entityName} with cascade persist support.");
        sb.AppendLine($"        /// Automatically persists related entities marked with CascadeType.Persist.");
        sb.AppendLine($"        /// Cascades: {string.Join(", ", cascades.Select(c => c.PropertyName))}");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task<{info.EntityType}> AddWithCascadeAsync({info.EntityType} entity);");
        sb.AppendLine();
    }

    public static void GenerateUpdateWithCascadeSignature(StringBuilder sb, RepositoryInfo info, List<Models.RelationshipMetadata> cascades, string entityName)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Updates a {entityName} with cascade merge support.");
        sb.AppendLine($"        /// Automatically updates related entities marked with CascadeType.Merge.");
        sb.AppendLine($"        /// Cascades: {string.Join(", ", cascades.Select(c => c.PropertyName))}");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task UpdateWithCascadeAsync({info.EntityType} entity);");
        sb.AppendLine();
    }

    public static void GenerateDeleteWithCascadeSignature(StringBuilder sb, RepositoryInfo info, List<Models.RelationshipMetadata> cascades, string entityName)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Deletes a {entityName} with cascade remove support.");
        sb.AppendLine($"        /// Automatically deletes related entities marked with CascadeType.Remove.");
        sb.AppendLine($"        /// Cascades: {string.Join(", ", cascades.Select(c => c.PropertyName))}");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        Task DeleteWithCascadeAsync({info.KeyType} id);");
        sb.AppendLine();
    }

    public static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

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
                    sb.Append(part.Substring(1));
            }
        }
        return sb.ToString();
    }


    /// <summary>
    /// Gets the foreign key column name for a OneToMany relationship.
    /// The JoinColumn is defined on the inverse ManyToOne relationship, not on the OneToMany.
    /// </summary>
    public static string GetForeignKeyColumnForOneToMany(RepositoryInfo info, Models.RelationshipMetadata oneToManyRelationship, string parentEntityName)
    {
        // If JoinColumn is specified on the OneToMany (shouldn't normally happen, but check first)
        if (oneToManyRelationship.JoinColumn != null && !string.IsNullOrEmpty(oneToManyRelationship.JoinColumn.Name))
        {
            return oneToManyRelationship.JoinColumn.Name;
        }

        // The JoinColumn is on the child entity's ManyToOne relationship
        // We need to find the child entity's ManyToOne relationship that points back to the parent
        // The MappedBy property tells us the property name on the child entity
        if (string.IsNullOrEmpty(oneToManyRelationship.MappedBy))
        {
            // No MappedBy means we can't determine the inverse relationship
            // Fall back to default naming convention
            return $"{parentEntityName}Id";
        }

        // Extract relationships from child entity to get the JoinColumn from ManyToOne
        if (info.Compilation != null)
        {
            // Try both full type and simple type name
            var childEntityFullType = oneToManyRelationship.TargetEntityFullType;
            var childEntitySimpleNameForExtraction = oneToManyRelationship.TargetEntityType.Split('.').Last();
            
            // Try full type first, then simple name
            var childRelationships = EntityAnalyzer.ExtractRelationships(info.Compilation, childEntityFullType);
            if (childRelationships.Count == 0)
            {
                // Try with just the simple name (might need namespace)
                childRelationships = EntityAnalyzer.ExtractRelationships(info.Compilation, childEntitySimpleNameForExtraction);
            }
            
            // Find the ManyToOne relationship that matches MappedBy
            var inverseManyToOne = childRelationships.FirstOrDefault(r => 
                r.Type == Models.RelationshipType.ManyToOne && 
                r.PropertyName.Equals(oneToManyRelationship.MappedBy, StringComparison.OrdinalIgnoreCase));
            
            if (inverseManyToOne != null && inverseManyToOne.JoinColumn != null && !string.IsNullOrEmpty(inverseManyToOne.JoinColumn.Name))
            {
                return inverseManyToOne.JoinColumn.Name;
            }
        }

        // Fallback: Try to find the FK property on the child entity
        // The FK property name is typically {MappedBy}Id (e.g., "CustomerId" if MappedBy is "Customer")
        // NOTE: We only look for the FK property (ending with "Id"), NOT the navigation property name
        var childEntitySimpleName = oneToManyRelationship.TargetEntityType.Split('.').Last();
        if (info.EntitiesMetadata != null && info.EntitiesMetadata.TryGetValue(childEntitySimpleName, out var childMetadata))
        {
            if (childMetadata.Properties != null)
            {
                // Look for a property that matches the FK naming pattern (e.g., "CustomerId")
                // Do NOT match the navigation property name (e.g., "Customer") as that's not the FK column
                var fkPropertyName = $"{oneToManyRelationship.MappedBy}Id";
                var fkProperty = childMetadata.Properties.FirstOrDefault(p => 
                    p.Name.Equals(fkPropertyName, StringComparison.OrdinalIgnoreCase));

                if (fkProperty != null)
                {
                    // Use the column name from the FK property
                    return fkProperty.ColumnName;
                }
            }
        }

        // Fall back to default naming convention
        return $"{parentEntityName}Id";
    }
}
