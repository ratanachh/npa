using System.Text;
using NPA.Design.Models;
using NPA.Design.Generators.Helpers;
using NPA.Design.Generators.Builders;

namespace NPA.Design.Generators.CodeGenerators;

/// <summary>
/// Generates stored procedure method bodies for repositories.
/// </summary>
internal static class StoredProcedureGenerator
{
    /// <summary>
    /// Generates the method body for stored procedure calls.
    /// </summary>
    public static string GenerateStoredProcedureMethodBody(MethodInfo method, string entityType, MethodAttributeInfo attrs)
    {
        var sb = new StringBuilder();
        var isAsync = method.ReturnType.StartsWith("System.Threading.Tasks.Task");
        var procName = attrs.Schema != null ? $"{attrs.Schema}.{attrs.ProcedureName}" : attrs.ProcedureName;
        var paramObj = SqlQueryBuilder.GenerateParameterObject(method.Parameters);

        if (method.ReturnType.Contains("IEnumerable") || method.ReturnType.Contains("ICollection") ||
            method.ReturnType.Contains("List") || method.ReturnType.Contains("[]") ||
            method.ReturnType.Contains("HashSet") || method.ReturnType.Contains("ISet") ||
            method.ReturnType.Contains("IReadOnly"))
        {
            // Returns collection
            var conversion = TypeHelper.GetCollectionConversion(method.ReturnType);

            if (isAsync)
            {
                if (!string.IsNullOrEmpty(conversion))
                {
                    sb.AppendLine($"            var result = await _connection.QueryAsync<{TypeHelper.GetInnerType(method.ReturnType)}>(\"{procName}\", {paramObj}, commandType: CommandType.StoredProcedure);");
                    sb.AppendLine($"            return result.{conversion};");
                }
                else
                {
                    sb.AppendLine($"            return await _connection.QueryAsync<{TypeHelper.GetInnerType(method.ReturnType)}>(\"{procName}\", {paramObj}, commandType: CommandType.StoredProcedure);");
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(conversion))
                {
                    sb.AppendLine($"            var result = _connection.Query<{TypeHelper.GetInnerType(method.ReturnType)}>(\"{procName}\", {paramObj}, commandType: CommandType.StoredProcedure);");
                    sb.AppendLine($"            return result.{conversion};");
                }
                else
                {
                    sb.AppendLine($"            return _connection.Query<{TypeHelper.GetInnerType(method.ReturnType)}>(\"{procName}\", {paramObj}, commandType: CommandType.StoredProcedure);");
                }
            }
        }
        else if (method.ReturnType.Contains("int") || method.ReturnType.Contains("long") || method.ReturnType.Contains("bool"))
        {
            // Returns scalar or execution result
            if (isAsync)
            {
                sb.AppendLine($"            return await _connection.ExecuteAsync(\"{procName}\", {paramObj}, commandType: CommandType.StoredProcedure);");
            }
            else
            {
                sb.AppendLine($"            return _connection.Execute(\"{procName}\", {paramObj}, commandType: CommandType.StoredProcedure);");
            }
        }
        else
        {
            // Returns single entity
            if (isAsync)
            {
                sb.AppendLine($"            return await _connection.QueryFirstOrDefaultAsync<{TypeHelper.GetInnerType(method.ReturnType)}>(\"{procName}\", {paramObj}, commandType: CommandType.StoredProcedure);");
            }
            else
            {
                sb.AppendLine($"            return _connection.QueryFirstOrDefault<{TypeHelper.GetInnerType(method.ReturnType)}>(\"{procName}\", {paramObj}, commandType: CommandType.StoredProcedure);");
            }
        }

        return sb.ToString();
    }
}

