using System;
using System.Linq;
using System.Text;
using NPA.Design.Models;
using NPA.Design.Generators.Helpers;
using NPA.Design.Generators.Builders;
using NPA.Design.Services;

namespace NPA.Design.Generators.CodeGenerators;

/// <summary>
/// Generates method bodies based on naming conventions (e.g., FindBy, CountBy, etc.).
/// </summary>
internal static class ConventionBasedQueryGenerator
{
    /// <summary>
    /// Generates a method body based on the analyzed convention.
    /// </summary>
    public static string GenerateConventionBasedMethodBody(MethodInfo method, RepositoryInfo info, MethodConvention convention)
    {
        var sb = new StringBuilder();
        var isAsync = method.ReturnType.StartsWith("System.Threading.Tasks.Task");
        var tableName = GetTableName(info);

        switch (convention.QueryType)
        {
            case QueryType.Select:
                sb.Append(GenerateSelectQuery(method, info, tableName, convention, isAsync));
                break;
            case QueryType.Count:
                sb.Append(GenerateCountQuery(method, info, tableName, convention, isAsync));
                break;
            case QueryType.Exists:
                sb.Append(GenerateExistsQuery(method, info, tableName, convention, isAsync));
                break;
            case QueryType.Delete:
                sb.Append(GenerateDeleteQuery(method, info, tableName, convention, isAsync));
                break;
            case QueryType.Update:
            case QueryType.Insert:
                sb.Append(GenerateModificationQuery(method, info.EntityType, convention, isAsync));
                break;
            default:
                sb.AppendLine($"            throw new NotImplementedException(\"Method {method.Name} requires manual implementation\");");
                break;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates a simple convention-based method body as a fallback.
    /// </summary>
    public static string GenerateSimpleConventionBody(MethodInfo method, RepositoryInfo info)
    {
        var sb = new StringBuilder();

        // Determine the actual return type - use method return type if different from entity
        var returnType = TypeHelper.GetInnerType(method.ReturnType);
        var queryType = string.IsNullOrEmpty(returnType) ? info.EntityType : returnType;

        var tableName = GetTableName(info);
        var keyPropertyName = MetadataHelper.GetKeyPropertyName(info);
        var keyColumnName = MetadataHelper.GetKeyColumnName(info);

        // Simple convention analysis (fallback)
        if (method.Name.StartsWith("GetAll") || method.Name.StartsWith("FindAll"))
        {
            var columnList = MetadataHelper.BuildColumnList(info.EntityMetadata);
            sb.AppendLine($"            var sql = \"SELECT {columnList} FROM {tableName}\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{queryType}>(sql);");
        }
        else if (method.Name.StartsWith("GetById") || method.Name.StartsWith("FindById"))
        {
            var columnList = MetadataHelper.BuildColumnList(info.EntityMetadata);
            sb.AppendLine($"            var sql = \"SELECT {columnList} FROM {tableName} WHERE {keyColumnName} = @id\";");
            sb.AppendLine($"            return await _connection.QueryFirstOrDefaultAsync<{queryType}>(sql, new {{ id }});");
        }
        else
        {
            // Default implementation - throw not implemented
            sb.AppendLine($"            throw new NotImplementedException(\"Method {method.Name} requires manual implementation or a custom attribute\");");
        }

        return sb.ToString();
    }

    private static string GenerateSelectQuery(MethodInfo method, RepositoryInfo info, string tableName, MethodConvention convention, bool isAsync)
    {
        var sb = new StringBuilder();

        // Determine the actual return type - use method return type if different from entity
        var returnType = TypeHelper.GetInnerType(method.ReturnType);
        var queryType = string.IsNullOrEmpty(returnType) ? info.EntityType : returnType;

        var whereClause = SqlQueryBuilder.BuildWhereClause(convention.PropertyNames, convention.PropertySeparators, convention.Parameters, info.EntityMetadata);
        var orderByClause = SqlQueryBuilder.BuildOrderByClause(convention.OrderByProperties, info.EntityMetadata);
        var paramObj = SqlQueryBuilder.GenerateParameterObject(convention.Parameters);
        var hasParameters = !string.IsNullOrEmpty(paramObj) && paramObj != "null";

        // Build the full SQL query with optional DISTINCT and LIMIT
        var columnList = MetadataHelper.BuildColumnList(info.EntityMetadata);
        var selectClause = convention.HasDistinct ? $"SELECT DISTINCT {columnList}" : $"SELECT {columnList}";
        var sqlBuilder = new StringBuilder($"{selectClause} FROM {tableName}");

        if (!string.IsNullOrEmpty(whereClause))
        {
            sqlBuilder.Append($" WHERE {whereClause}");
        }

        if (!string.IsNullOrEmpty(orderByClause))
        {
            sqlBuilder.Append($" ORDER BY {orderByClause}");
        }

        // Add LIMIT clause if specified
        // Using ANSI SQL FETCH FIRST syntax for maximum compatibility
        if (convention.Limit.HasValue)
        {
            sqlBuilder.Append($" FETCH FIRST {convention.Limit.Value} ROWS ONLY");
        }

        var fullSql = sqlBuilder.ToString();
        sb.AppendLine($"            var sql = \"{fullSql}\";");

        if (!hasParameters)
        {
            // No parameters
            if (convention.ReturnsCollection)
            {
                if (isAsync)
                {
                    sb.AppendLine($"            return await _connection.QueryAsync<{queryType}>(sql);");
                }
                else
                {
                    sb.AppendLine($"            return _connection.Query<{queryType}>(sql);");
                }
            }
            else
            {
                if (isAsync)
                {
                    sb.AppendLine($"            return await _connection.QueryFirstOrDefaultAsync<{queryType}>(sql);");
                }
                else
                {
                    sb.AppendLine($"            return _connection.QueryFirstOrDefault<{queryType}>(sql);");
                }
            }
        }
        else
        {
            // With parameters
            if (convention.ReturnsCollection)
            {
                if (isAsync)
                {
                    sb.AppendLine($"            return await _connection.QueryAsync<{queryType}>(sql, {paramObj});");
                }
                else
                {
                    sb.AppendLine($"            return _connection.Query<{queryType}>(sql, {paramObj});");
                }
            }
            else
            {
                if (isAsync)
                {
                    sb.AppendLine($"            return await _connection.QueryFirstOrDefaultAsync<{queryType}>(sql, {paramObj});");
                }
                else
                {
                    sb.AppendLine($"            return _connection.QueryFirstOrDefault<{queryType}>(sql, {paramObj});");
                }
            }
        }

        return sb.ToString();
    }

    private static string GenerateCountQuery(MethodInfo method, RepositoryInfo info, string tableName, MethodConvention convention, bool isAsync)
    {
        var sb = new StringBuilder();
        var whereClause = SqlQueryBuilder.BuildWhereClause(convention.PropertyNames, convention.PropertySeparators, convention.Parameters, info.EntityMetadata);
        var paramObj = SqlQueryBuilder.GenerateParameterObject(convention.Parameters);

        // Handle DISTINCT for COUNT queries
        // COUNT(DISTINCT *) is invalid SQL - use primary key column instead
        string countExpression;
        if (convention.HasDistinct)
        {
            var keyColumnName = MetadataHelper.GetKeyColumnName(info);
            countExpression = $"COUNT(DISTINCT {keyColumnName})";
        }
        else
        {
            countExpression = "COUNT(*)";
        }

        if (string.IsNullOrEmpty(whereClause))
        {
            sb.AppendLine($"            var sql = \"SELECT {countExpression} FROM {tableName}\";");
            if (isAsync)
            {
                sb.AppendLine($"            return await _connection.ExecuteScalarAsync<int>(sql);");
            }
            else
            {
                sb.AppendLine($"            return _connection.ExecuteScalar<int>(sql);");
            }
        }
        else
        {
            sb.AppendLine($"            var sql = \"SELECT {countExpression} FROM {tableName} WHERE {whereClause}\";");
            if (isAsync)
            {
                sb.AppendLine($"            return await _connection.ExecuteScalarAsync<int>(sql, {paramObj});");
            }
            else
            {
                sb.AppendLine($"            return _connection.ExecuteScalar<int>(sql, {paramObj});");
            }
        }

        return sb.ToString();
    }

    private static string GenerateExistsQuery(MethodInfo method, RepositoryInfo info, string tableName, MethodConvention convention, bool isAsync)
    {
        var sb = new StringBuilder();
        var whereClause = SqlQueryBuilder.BuildWhereClause(convention.PropertyNames, convention.PropertySeparators, convention.Parameters, info.EntityMetadata);
        var paramObj = SqlQueryBuilder.GenerateParameterObject(convention.Parameters);

        sb.AppendLine($"            var sql = \"SELECT COUNT(*) FROM {tableName} WHERE {whereClause}\";");
        if (isAsync)
        {
            sb.AppendLine($"            var count = await _connection.ExecuteScalarAsync<int>(sql, {paramObj});");
        }
        else
        {
            sb.AppendLine($"            var count = _connection.ExecuteScalar<int>(sql, {paramObj});");
        }
        sb.AppendLine($"            return count > 0;");

        return sb.ToString();
    }

    private static string GenerateDeleteQuery(MethodInfo method, RepositoryInfo info, string tableName, MethodConvention convention, bool isAsync)
    {
        var sb = new StringBuilder();
        var whereClause = SqlQueryBuilder.BuildWhereClause(convention.PropertyNames, convention.PropertySeparators, convention.Parameters, info.EntityMetadata);
        var paramObj = SqlQueryBuilder.GenerateParameterObject(convention.Parameters);

        // Special handling for id parameter when convention doesn't extract it
        if (string.IsNullOrEmpty(whereClause) && convention.Parameters.Count > 0)
        {
            // Check if there's an 'id' parameter (case-insensitive)
            var idParam = convention.Parameters.FirstOrDefault(p =>
                p.Name.Equals("id", StringComparison.OrdinalIgnoreCase));

            if (idParam != null)
            {
                var keyColumnName = MetadataHelper.GetKeyColumnName(info);
                whereClause = $"{keyColumnName} = @{idParam.Name}";
                paramObj = $"new {{ {idParam.Name} }}";
            }
        }

        if (string.IsNullOrEmpty(whereClause))
        {
            sb.AppendLine($"            throw new InvalidOperationException(\"Delete without WHERE clause is not allowed\");");
        }
        else
        {
            sb.AppendLine($"            var sql = \"DELETE FROM {tableName} WHERE {whereClause}\";");
            if (isAsync)
            {
                sb.AppendLine($"            await _connection.ExecuteAsync(sql, {paramObj});");
            }
            else
            {
                sb.AppendLine($"            _connection.Execute(sql, {paramObj});");
            }
        }

        return sb.ToString();
    }

    private static string GenerateModificationQuery(MethodInfo method, string entityType, MethodConvention convention, bool isAsync)
    {
        var sb = new StringBuilder();
        var entityParam = convention.Parameters.FirstOrDefault();

        if (entityParam != null && entityParam.Type.Contains(entityType.Split('.').Last()))
        {
            if (convention.QueryType == QueryType.Insert)
            {
                if (isAsync)
                {
                    sb.AppendLine($"            await _entityManager.PersistAsync({entityParam.Name});");
                }
                else
                {
                    sb.AppendLine($"            _entityManager.Persist({entityParam.Name});");
                }
            }
            else
            {
                if (isAsync)
                {
                    sb.AppendLine($"            await _entityManager.MergeAsync({entityParam.Name});");
                }
                else
                {
                    sb.AppendLine($"            _entityManager.Merge({entityParam.Name});");
                }
            }
        }
        else
        {
            sb.AppendLine($"            throw new NotImplementedException(\"Method {method.Name} requires manual implementation\");");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets the table name for an entity, using metadata if available, otherwise using pluralization.
    /// </summary>
    private static string GetTableName(RepositoryInfo info)
    {
        // Try to get from metadata first
        var tableName = MetadataHelper.GetTableNameFromMetadata(info, info.EntityType);
        if (!string.IsNullOrEmpty(tableName))
        {
            return tableName!;
        }

        // Fallback: use pluralization
        var simpleName = info.EntityType.Split('.').Last();
        var pluralizedName = StringHelper.Pluralize(simpleName);
        return MethodConventionAnalyzer.ToSnakeCase(pluralizedName) ?? pluralizedName.ToLower();
    }
}

