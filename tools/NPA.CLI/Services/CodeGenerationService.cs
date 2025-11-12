using NPA.CLI.Templates;

namespace NPA.CLI.Services;

/// <summary>
/// Implementation of code generation service.
/// </summary>
public class CodeGenerationService : ICodeGenerationService
{
    public Task<string> GenerateRepositoryInterfaceAsync(string entityName, string namespaceName)
    {
        var template = new RepositoryInterfaceTemplate();
        var code = template.Generate(entityName, namespaceName);
        return Task.FromResult(code);
    }

    public Task<string> GenerateRepositoryImplementationAsync(string entityName, string namespaceName)
    {
        var template = new RepositoryImplementationTemplate();
        var code = template.Generate(entityName, namespaceName);
        return Task.FromResult(code);
    }

    public Task<string> GenerateEntityAsync(string entityName, string namespaceName, string tableName)
    {
        var template = new EntityTemplate();
        var code = template.Generate(entityName, namespaceName, tableName);
        return Task.FromResult(code);
    }

    public Task<string> GenerateMigrationAsync(string migrationName, string namespaceName)
    {
        var template = new MigrationTemplate();
        var code = template.Generate(migrationName, namespaceName);
        return Task.FromResult(code);
    }

    public Task<string> GenerateTestAsync(string className, string namespaceName)
    {
        var template = new TestTemplate();
        var code = template.Generate(className, namespaceName);
        return Task.FromResult(code);
    }
}
