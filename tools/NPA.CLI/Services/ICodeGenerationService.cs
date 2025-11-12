namespace NPA.CLI.Services;

/// <summary>
/// Service for generating code from templates.
/// </summary>
public interface ICodeGenerationService
{
    /// <summary>
    /// Generates a repository interface.
    /// </summary>
    Task<string> GenerateRepositoryInterfaceAsync(string entityName, string namespaceName);

    /// <summary>
    /// Generates a repository implementation.
    /// </summary>
    Task<string> GenerateRepositoryImplementationAsync(string entityName, string namespaceName);

    /// <summary>
    /// Generates an entity class.
    /// </summary>
    Task<string> GenerateEntityAsync(string entityName, string namespaceName, string tableName);

    /// <summary>
    /// Generates a migration class.
    /// </summary>
    Task<string> GenerateMigrationAsync(string migrationName, string namespaceName);

    /// <summary>
    /// Generates a test class.
    /// </summary>
    Task<string> GenerateTestAsync(string className, string namespaceName);
}
