namespace NPA.CLI.Services;

/// <summary>
/// Configuration for NPA.
/// </summary>
public class NpaConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseProvider { get; set; } = "sqlserver";
    public string MigrationsNamespace { get; set; } = "Migrations";
    public string EntitiesNamespace { get; set; } = "Entities";
    public string RepositoriesNamespace { get; set; } = "Repositories";
}

/// <summary>
/// Service for managing configuration.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Loads configuration from file.
    /// </summary>
    Task<NpaConfiguration> LoadConfigurationAsync(string? configPath = null);

    /// <summary>
    /// Saves configuration to file.
    /// </summary>
    Task SaveConfigurationAsync(NpaConfiguration config, string? configPath = null);

    /// <summary>
    /// Validates configuration.
    /// </summary>
    Task<bool> ValidateConfigurationAsync(NpaConfiguration config);

    /// <summary>
    /// Initializes a new configuration file.
    /// </summary>
    Task InitializeConfigurationAsync(string outputPath);
}
