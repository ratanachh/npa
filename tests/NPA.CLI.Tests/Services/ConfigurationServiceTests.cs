using NPA.CLI.Services;
using Xunit;

namespace NPA.CLI.Tests.Services;

public class ConfigurationServiceTests
{
    private readonly ConfigurationService _service;
    private readonly string _testDir;

    public ConfigurationServiceTests()
    {
        _service = new ConfigurationService();
        _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public async Task LoadConfigurationAsync_ReturnsDefaultWhenFileNotExists()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDir, "nonexistent.json");

        // Act
        var config = await _service.LoadConfigurationAsync(nonExistentPath);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(string.Empty, config.ConnectionString);
        Assert.Equal("sqlserver", config.DatabaseProvider);
    }

    [Fact]
    public async Task SaveAndLoadConfigurationAsync_RoundTrip()
    {
        // Arrange
        var configPath = Path.Combine(_testDir, "test.config.json");
        var config = new NpaConfiguration
        {
            ConnectionString = "Server=localhost;Database=Test;",
            DatabaseProvider = "postgresql",
            MigrationsNamespace = "TestMigrations",
            EntitiesNamespace = "TestEntities",
            RepositoriesNamespace = "TestRepositories"
        };

        try
        {
            // Act
            await _service.SaveConfigurationAsync(config, configPath);
            var loaded = await _service.LoadConfigurationAsync(configPath);

            // Assert
            Assert.Equal(config.ConnectionString, loaded.ConnectionString);
            Assert.Equal(config.DatabaseProvider, loaded.DatabaseProvider);
            Assert.Equal(config.MigrationsNamespace, loaded.MigrationsNamespace);
            Assert.Equal(config.EntitiesNamespace, loaded.EntitiesNamespace);
            Assert.Equal(config.RepositoriesNamespace, loaded.RepositoriesNamespace);
        }
        finally
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }

    [Fact]
    public async Task ValidateConfigurationAsync_ValidConfig_ReturnsTrue()
    {
        // Arrange
        var config = new NpaConfiguration
        {
            ConnectionString = "Server=localhost;Database=Test;",
            DatabaseProvider = "sqlserver"
        };

        // Act
        var isValid = await _service.ValidateConfigurationAsync(config);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_EmptyConnectionString_ReturnsFalse()
    {
        // Arrange
        var config = new NpaConfiguration
        {
            ConnectionString = "",
            DatabaseProvider = "sqlserver"
        };

        // Act
        var isValid = await _service.ValidateConfigurationAsync(config);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_EmptyProvider_ReturnsFalse()
    {
        // Arrange
        var config = new NpaConfiguration
        {
            ConnectionString = "Server=localhost;Database=Test;",
            DatabaseProvider = ""
        };

        // Act
        var isValid = await _service.ValidateConfigurationAsync(config);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task InitializeConfigurationAsync_CreatesConfigFile()
    {
        // Arrange
        var configPath = Path.Combine(_testDir, "npa.config.json");

        try
        {
            // Act
            await _service.InitializeConfigurationAsync(_testDir);

            // Assert
            Assert.True(File.Exists(configPath));
            
            var config = await _service.LoadConfigurationAsync(configPath);
            Assert.NotNull(config);
            Assert.NotEmpty(config.ConnectionString);
            Assert.Equal("sqlserver", config.DatabaseProvider);
        }
        finally
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }

    private void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }
}
