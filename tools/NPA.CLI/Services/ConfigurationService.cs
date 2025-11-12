using System.Text.Json;

namespace NPA.CLI.Services;

/// <summary>
/// Implementation of configuration service.
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private const string DefaultConfigFileName = "npa.config.json";

    public async Task<NpaConfiguration> LoadConfigurationAsync(string? configPath = null)
    {
        var path = configPath ?? DefaultConfigFileName;

        if (!File.Exists(path))
        {
            return new NpaConfiguration();
        }

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<NpaConfiguration>(json) ?? new NpaConfiguration();
    }

    public async Task SaveConfigurationAsync(NpaConfiguration config, string? configPath = null)
    {
        var path = configPath ?? DefaultConfigFileName;
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    public Task<bool> ValidateConfigurationAsync(NpaConfiguration config)
    {
        var isValid = !string.IsNullOrWhiteSpace(config.ConnectionString) &&
                      !string.IsNullOrWhiteSpace(config.DatabaseProvider);

        return Task.FromResult(isValid);
    }

    public async Task InitializeConfigurationAsync(string outputPath)
    {
        var config = new NpaConfiguration
        {
            ConnectionString = "Server=localhost;Database=MyDatabase;Trusted_Connection=true;",
            DatabaseProvider = "sqlserver",
            MigrationsNamespace = "Migrations",
            EntitiesNamespace = "Entities",
            RepositoriesNamespace = "Repositories"
        };

        var configPath = Path.Combine(outputPath, DefaultConfigFileName);
        await SaveConfigurationAsync(config, configPath);
    }
}
