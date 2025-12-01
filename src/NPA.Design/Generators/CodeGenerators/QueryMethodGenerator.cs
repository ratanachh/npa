using System;
using System.Linq;
using System.Text;
using NPA.Design.Models;
using NPA.Design.Generators.Helpers;
using NPA.Design.Generators.Builders;
using NPA.Design.Services;
using NPA.Design.Shared;

namespace NPA.Design.Generators.CodeGenerators;

/// <summary>
/// Generates query method bodies for repositories ([Query] and [NamedQuery] attributes).
/// </summary>
internal static class QueryMethodGenerator
{
    /// <summary>
    /// Generates method body for [Query] attribute methods.
    /// </summary>
    public static string GenerateQueryMethodBody(MethodInfo method, RepositoryInfo info, MethodAttributeInfo attrs)
    {
        var sb = new StringBuilder();
        var isAsync = method.ReturnType.StartsWith("System.Threading.Tasks.Task");
        var paramObj = SqlQueryBuilder.GenerateParameterObject(method.Parameters);

        // Use native SQL if NativeQuery is true, otherwise convert CPQL to SQL
        string sql;
        if (attrs.NativeQuery)
        {
            // Native SQL - use as-is without conversion
            sql = attrs.QuerySql ?? string.Empty;
        }
        else
        {
            // Convert CPQL to SQL using entity metadata dictionary
            sql = info.EntitiesMetadata.Count > 0
                ? CpqlToSqlConverter.ConvertToSql(attrs.QuerySql ?? string.Empty, info.EntitiesMetadata)
                : CpqlToSqlConverter.ConvertToSql(attrs.QuerySql ?? string.Empty);
        }

        if (attrs.HasMultiMapping)
        {
            // Multi-mapping query using Dapper
            var innerType = TypeHelper.GetInnerType(method.ReturnType);
            var isCollection = method.ReturnType.Contains("IEnumerable") || method.ReturnType.Contains("List") ||
                             method.ReturnType.Contains("ICollection") || method.ReturnType.Contains("[]");

            sb.AppendLine($"            var sql = @\"{sql}\";");
            sb.AppendLine($"            var splitOn = \"{attrs.SplitOn ?? "Id"}\";");
            sb.AppendLine();

            if (isCollection)
            {
                // Collection result with multi-mapping
                sb.AppendLine($"            var lookup = new Dictionary<object, {innerType}>();");
                sb.AppendLine();

                if (isAsync)
                {
                    sb.AppendLine($"            await _connection.QueryAsync<{innerType}, dynamic, {innerType}>(");
                    sb.AppendLine($"                sql,");
                    sb.AppendLine($"                (main, related) => {{");
                    sb.AppendLine($"                    var key = main.{attrs.KeyProperty ?? "Id"};");
                    sb.AppendLine($"                    if (!lookup.TryGetValue(key, out var existing))");
                    sb.AppendLine($"                    {{");
                    sb.AppendLine($"                        lookup[key] = main;");
                    sb.AppendLine($"                    }}");
                    sb.AppendLine($"                    // Note: Relationship population should be customized based on your entities");
                    sb.AppendLine($"                    return main;");
                    sb.AppendLine($"                }},");
                    sb.AppendLine($"                {paramObj},");
                    sb.AppendLine($"                splitOn: splitOn);");
                    sb.AppendLine();

                    var conversion = TypeHelper.GetCollectionConversion(method.ReturnType);
                    if (!string.IsNullOrEmpty(conversion))
                    {
                        sb.AppendLine($"            return lookup.Values.{conversion};");
                    }
                    else
                    {
                        sb.AppendLine($"            return lookup.Values;");
                    }
                }
                else
                {
                    sb.AppendLine($"            _connection.Query<{innerType}, dynamic, {innerType}>(");
                    sb.AppendLine($"                sql,");
                    sb.AppendLine($"                (main, related) => {{");
                    sb.AppendLine($"                    var key = main.{attrs.KeyProperty ?? "Id"};");
                    sb.AppendLine($"                    if (!lookup.TryGetValue(key, out var existing))");
                    sb.AppendLine($"                    {{");
                    sb.AppendLine($"                        lookup[key] = main;");
                    sb.AppendLine($"                    }}");
                    sb.AppendLine($"                    // Note: Relationship population should be customized based on your entities");
                    sb.AppendLine($"                    return main;");
                    sb.AppendLine($"                }},");
                    sb.AppendLine($"                {paramObj},");
                    sb.AppendLine($"                splitOn: splitOn);");
                    sb.AppendLine();

                    var conversion = TypeHelper.GetCollectionConversion(method.ReturnType);
                    if (!string.IsNullOrEmpty(conversion))
                    {
                        sb.AppendLine($"            return lookup.Values.{conversion};");
                    }
                    else
                    {
                        sb.AppendLine($"            return lookup.Values;");
                    }
                }
            }
            else
            {
                // Single result with multi-mapping
                if (isAsync)
                {
                    sb.AppendLine($"            var result = await _connection.QueryAsync<{innerType}, dynamic, {innerType}>(");
                    sb.AppendLine($"                sql,");
                    sb.AppendLine($"                (main, related) => {{");
                    sb.AppendLine($"                    // Note: Relationship population should be customized based on your entities");
                    sb.AppendLine($"                    return main;");
                    sb.AppendLine($"                }},");
                    sb.AppendLine($"                {paramObj},");
                    sb.AppendLine($"                splitOn: splitOn);");
                    sb.AppendLine();
                    sb.AppendLine($"            return result.FirstOrDefault();");
                }
                else
                {
                    sb.AppendLine($"            var result = _connection.Query<{innerType}, dynamic, {innerType}>(");
                    sb.AppendLine($"                sql,");
                    sb.AppendLine($"                (main, related) => {{");
                    sb.AppendLine($"                    // Note: Relationship population should be customized based on your entities");
                    sb.AppendLine($"                    return main;");
                    sb.AppendLine($"                }},");
                    sb.AppendLine($"                {paramObj},");
                    sb.AppendLine($"                splitOn: splitOn);");
                    sb.AppendLine();
                    sb.AppendLine($"            return result.FirstOrDefault();");
                }
            }
        }
        else if (method.ReturnType.Contains("IEnumerable") || method.ReturnType.Contains("ICollection") ||
                 method.ReturnType.Contains("List") || method.ReturnType.Contains("[]") ||
                 method.ReturnType.Contains("HashSet") || method.ReturnType.Contains("ISet") ||
                 method.ReturnType.Contains("IReadOnly"))
        {
            // Returns collection
            sb.AppendLine($"            var sql = @\"{sql}\";");
            var conversion = TypeHelper.GetCollectionConversion(method.ReturnType);

            if (isAsync)
            {
                if (!string.IsNullOrEmpty(conversion))
                {
                    sb.AppendLine($"            var result = await _connection.QueryAsync<{TypeHelper.GetInnerType(method.ReturnType)}>(sql, {paramObj});");
                    sb.AppendLine($"            return result.{conversion};");
                }
                else
                {
                    sb.AppendLine($"            return await _connection.QueryAsync<{TypeHelper.GetInnerType(method.ReturnType)}>(sql, {paramObj});");
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(conversion))
                {
                    sb.AppendLine($"            var result = _connection.Query<{TypeHelper.GetInnerType(method.ReturnType)}>(sql, {paramObj});");
                    sb.AppendLine($"            return result.{conversion};");
                }
                else
                {
                    sb.AppendLine($"            return _connection.Query<{TypeHelper.GetInnerType(method.ReturnType)}>(sql, {paramObj});");
                }
            }
        }
        else if (method.ReturnType.Contains("int") || method.ReturnType.Contains("long"))
        {
            // Returns scalar (count, affected rows, etc.)
            // Detect if it's INSERT/UPDATE/DELETE based on SQL query
            sb.AppendLine($"            var sql = @\"{sql}\";");

            var isModification = sql.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) ||
                                sql.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) ||
                                sql.TrimStart().StartsWith("DELETE", StringComparison.OrdinalIgnoreCase);

            if (isModification)
            {
                // INSERT/UPDATE/DELETE - returns affected row count
                if (isAsync)
                {
                    sb.AppendLine($"            return await _connection.ExecuteAsync(sql, {paramObj});");
                }
                else
                {
                    sb.AppendLine($"            return _connection.Execute(sql, {paramObj});");
                }
            }
            else
            {
                // SELECT COUNT, SUM, etc. - returns scalar value
                if (isAsync)
                {
                    sb.AppendLine($"            return await _connection.ExecuteScalarAsync<{TypeHelper.GetInnerType(method.ReturnType)}>(sql, {paramObj});");
                }
                else
                {
                    sb.AppendLine($"            return _connection.ExecuteScalar<{TypeHelper.GetInnerType(method.ReturnType)}>(sql, {paramObj});");
                }
            }
        }
        else if (method.ReturnType.Contains("bool"))
        {
            // Returns boolean (exists check)
            sb.AppendLine($"            var sql = @\"{sql}\";");
            if (isAsync)
            {
                sb.AppendLine($"            var result = await _connection.ExecuteScalarAsync<int>(sql, {paramObj});");
            }
            else
            {
                sb.AppendLine($"            var result = _connection.ExecuteScalar<int>(sql, {paramObj});");
            }
            sb.AppendLine($"            return result > 0;");
        }
        else
        {
            // Returns single entity or nullable
            sb.AppendLine($"            var sql = @\"{sql}\";");
            if (isAsync)
            {
                sb.AppendLine($"            return await _connection.QueryFirstOrDefaultAsync<{TypeHelper.GetInnerType(method.ReturnType)}>(sql, {paramObj});");
            }
            else
            {
                sb.AppendLine($"            return _connection.QueryFirstOrDefault<{TypeHelper.GetInnerType(method.ReturnType)}>(sql, {paramObj});");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates method body for [NamedQuery] attribute methods.
    /// </summary>
    public static string GenerateNamedQueryMethodBody(MethodInfo method, RepositoryInfo info, string namedQueryName)
    {
        // Find the named query from entity metadata
        var namedQuery = info.EntityMetadata?.NamedQueries
            ?.FirstOrDefault(nq => nq.Name == namedQueryName);

        if (namedQuery == null)
        {
            // Fallback to convention-based if named query not found
            // This shouldn't happen but provides a safety net
            // Fallback - will be replaced when ConventionBasedQueryGenerator is extracted
            return ConventionBasedQueryGenerator.GenerateSimpleConventionBody(method, info);
        }

        var sb = new StringBuilder();

        // Generate comment indicating this uses a named query
        sb.AppendLine($"            // Using named query: {namedQueryName}");
        sb.AppendLine();

        // Determine the SQL to use
        string sql;
        if (namedQuery.NativeQuery)
        {
            // Native SQL - use as-is without conversion
            sql = namedQuery.Query;
        }
        else
        {
            // Convert CPQL to SQL using entity metadata dictionary
            sql = info.EntitiesMetadata.Count > 0
                ? CpqlToSqlConverter.ConvertToSql(namedQuery.Query, info.EntitiesMetadata)
                : CpqlToSqlConverter.ConvertToSql(namedQuery.Query);
        }

        // Generate parameter object
        var paramObj = SqlQueryBuilder.GenerateParameterObject(method.Parameters);

        // Execute query based on return type (same logic as GenerateQueryMethodBody)
        var isAsync = method.ReturnType.StartsWith("System.Threading.Tasks.Task");

        if (method.ReturnType.Contains("IEnumerable") || method.ReturnType.Contains("ICollection") ||
            method.ReturnType.Contains("List") || method.ReturnType.Contains("[]") ||
            method.ReturnType.Contains("HashSet") || method.ReturnType.Contains("ISet") ||
            method.ReturnType.Contains("IReadOnly"))
        {
            // Returns collection
            sb.AppendLine($"            var sql = @\"{sql}\";");
            if (isAsync)
            {
                sb.AppendLine($"            return await _connection.QueryAsync<{TypeHelper.GetInnerType(method.ReturnType)}>(sql, {paramObj});");
            }
            else
            {
                sb.AppendLine($"            return _connection.Query<{TypeHelper.GetInnerType(method.ReturnType)}>(sql, {paramObj});");
            }
        }
        else if (method.ReturnType.Contains(info.EntityType) || method.ReturnType.EndsWith("?"))
        {
            // Returns single entity
            sb.AppendLine($"            var sql = @\"{sql}\";");
            if (isAsync)
            {
                sb.AppendLine($"            return await _connection.QueryFirstOrDefaultAsync<{TypeHelper.GetInnerType(method.ReturnType)}>(sql, {paramObj});");
            }
            else
            {
                sb.AppendLine($"            return _connection.QueryFirstOrDefault<{TypeHelper.GetInnerType(method.ReturnType)}>(sql, {paramObj});");
            }
        }
        else if (method.ReturnType.Contains("int") || method.ReturnType.Contains("long"))
        {
            // Returns scalar (count, affected rows, etc.)
            sb.AppendLine($"            var sql = @\"{sql}\";");

            var isModification = sql.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) ||
                                sql.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) ||
                                sql.TrimStart().StartsWith("DELETE", StringComparison.OrdinalIgnoreCase);

            if (isModification)
            {
                // INSERT/UPDATE/DELETE - returns affected row count
                if (isAsync)
                {
                    sb.AppendLine($"            return await _connection.ExecuteAsync(sql, {paramObj});");
                }
                else
                {
                    sb.AppendLine($"            return _connection.Execute(sql, {paramObj});");
                }
            }
            else
            {
                // SELECT COUNT, SUM, etc. - returns scalar value
                if (isAsync)
                {
                    sb.AppendLine($"            return await _connection.ExecuteScalarAsync<{TypeHelper.GetInnerType(method.ReturnType)}>(sql, {paramObj});");
                }
                else
                {
                    sb.AppendLine($"            return _connection.ExecuteScalar<{TypeHelper.GetInnerType(method.ReturnType)}>(sql, {paramObj});");
                }
            }
        }
        else if (method.ReturnType.Contains("bool"))
        {
            // Returns boolean (exists check)
            sb.AppendLine($"            var sql = @\"{sql}\";");
            if (isAsync)
            {
                sb.AppendLine($"            var result = await _connection.ExecuteScalarAsync<int>(sql, {paramObj});");
            }
            else
            {
                sb.AppendLine($"            var result = _connection.ExecuteScalar<int>(sql, {paramObj});");
            }
            sb.AppendLine($"            return result > 0;");
        }
        else
        {
            // Returns single entity or nullable
            sb.AppendLine($"            var sql = @\"{sql}\";");
            if (isAsync)
            {
                sb.AppendLine($"            return await _connection.QueryFirstOrDefaultAsync<{TypeHelper.GetInnerType(method.ReturnType)}>(sql, {paramObj});");
            }
            else
            {
                sb.AppendLine($"            return _connection.QueryFirstOrDefault<{TypeHelper.GetInnerType(method.ReturnType)}>(sql, {paramObj});");
            }
        }

        return sb.ToString();
    }
}

