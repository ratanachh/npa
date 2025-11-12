namespace NPA.CLI.Services;

/// <summary>
/// Project options for scaffolding.
/// </summary>
public class ProjectOptions
{
    public string DatabaseProvider { get; set; } = "sqlserver";
    public string ConnectionString { get; set; } = string.Empty;
    public bool IncludeSamples { get; set; } = false;
}

/// <summary>
/// Service for creating and managing projects.
/// </summary>
public interface IProjectTemplateService
{
    /// <summary>
    /// Creates a new project from a template.
    /// </summary>
    Task CreateProjectAsync(string template, string projectName, string outputPath, ProjectOptions options);

    /// <summary>
    /// Gets available project templates.
    /// </summary>
    Task<IEnumerable<string>> GetAvailableTemplatesAsync();
}
