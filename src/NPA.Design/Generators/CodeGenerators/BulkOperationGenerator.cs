using System.Linq;
using System.Text;
using NPA.Design.Models;
using NPA.Design.Generators.Helpers;

namespace NPA.Design.Generators.CodeGenerators;

/// <summary>
/// Generates bulk operation method bodies for repositories (BulkInsert, BulkUpdate, BulkDelete).
/// </summary>
internal static class BulkOperationGenerator
{
    /// <summary>
    /// Generates the method body for bulk operations.
    /// </summary>
    public static string GenerateBulkOperationMethodBody(MethodInfo method, string entityType, MethodAttributeInfo attrs)
    {
        var sb = new StringBuilder();
        var isAsync = method.ReturnType.StartsWith("System.Threading.Tasks.Task");
        var entityParam = method.Parameters.FirstOrDefault(p => p.Type.Contains("IEnumerable"));

        if (entityParam != null)
        {
            var collectionType = TypeHelper.GetInnerType(entityParam.Type);

            if (method.Name.Contains("Insert") || method.Name.Contains("Add") || method.Name.Contains("Create"))
            {
                if (isAsync)
                {
                    sb.AppendLine($"            return await _entityManager.BulkInsertAsync({entityParam.Name});");
                }
                else
                {
                    sb.AppendLine($"            return _entityManager.BulkInsert({entityParam.Name});");
                }
            }
            else if (method.Name.Contains("Update") || method.Name.Contains("Modify"))
            {
                if (isAsync)
                {
                    sb.AppendLine($"            return await _entityManager.BulkUpdateAsync({entityParam.Name});");
                }
                else
                {
                    sb.AppendLine($"            return _entityManager.BulkUpdate({entityParam.Name});");
                }
            }
            else if (method.Name.Contains("Delete") || method.Name.Contains("Remove"))
            {
                if (isAsync)
                {
                    sb.AppendLine($"            return await _entityManager.BulkDeleteAsync({entityParam.Name});");
                }
                else
                {
                    sb.AppendLine($"            return _entityManager.BulkDelete({entityParam.Name});");
                }
            }
            else
            {
                sb.AppendLine($"            throw new NotImplementedException(\"Bulk operation type not recognized from method name\");");
            }
        }
        else
        {
            sb.AppendLine($"            throw new NotImplementedException(\"Bulk operation requires an IEnumerable parameter\");");
        }

        return sb.ToString();
    }
}

