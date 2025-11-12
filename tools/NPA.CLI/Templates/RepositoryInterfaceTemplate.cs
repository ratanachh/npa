namespace NPA.CLI.Templates;

/// <summary>
/// Template for generating repository interfaces.
/// </summary>
public class RepositoryInterfaceTemplate
{
    public string Generate(string entityName, string namespaceName)
    {
        return $@"using NPA.Core;
using NPA.Core.Attributes;
using {namespaceName.Replace(".Repositories", ".Entities")};

namespace {namespaceName};

/// <summary>
/// Repository interface for {entityName} entity.
/// </summary>
[Repository]
public partial interface I{entityName}Repository : IRepository<{entityName}, long>
{{
    // Add custom query methods here
    // Example:
    // Task<{entityName}?> FindByNameAsync(string name);
}}
";
    }
}
