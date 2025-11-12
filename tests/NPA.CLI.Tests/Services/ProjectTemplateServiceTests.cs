using NPA.CLI.Services;
using Xunit;

namespace NPA.CLI.Tests.Services;

public class ProjectTemplateServiceTests
{
    private readonly ProjectTemplateService _service;
    private readonly string _testDir;

    public ProjectTemplateServiceTests()
    {
        _service = new ProjectTemplateService();
        _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    }

    [Fact]
    public async Task GetAvailableTemplatesAsync_ReturnsTemplates()
    {
        // Act
        var templates = await _service.GetAvailableTemplatesAsync();

        // Assert
        Assert.NotNull(templates);
        Assert.Contains("console", templates);
        Assert.Contains("webapi", templates);
        Assert.Contains("classlib", templates);
    }

    [Fact]
    public async Task CreateProjectAsync_Console_CreatesProjectStructure()
    {
        // Arrange
        var projectName = "TestConsoleApp";
        var projectPath = Path.Combine(_testDir, projectName);
        var options = new ProjectOptions
        {
            DatabaseProvider = "sqlserver",
            ConnectionString = "Server=localhost;Database=Test;",
            IncludeSamples = false
        };

        try
        {
            // Act
            await _service.CreateProjectAsync("console", projectName, projectPath, options);

            // Assert
            Assert.True(Directory.Exists(projectPath));
            Assert.True(File.Exists(Path.Combine(projectPath, $"{projectName}.csproj")));
            Assert.True(File.Exists(Path.Combine(projectPath, "Program.cs")));
            Assert.True(Directory.Exists(Path.Combine(projectPath, "Entities")));
            Assert.True(Directory.Exists(Path.Combine(projectPath, "Repositories")));
        }
        finally
        {
            if (Directory.Exists(projectPath))
            {
                Directory.Delete(projectPath, true);
            }
        }
    }

    [Fact]
    public async Task CreateProjectAsync_WebApi_CreatesProjectStructure()
    {
        // Arrange
        var projectName = "TestWebApi";
        var projectPath = Path.Combine(_testDir, projectName);
        var options = new ProjectOptions
        {
            DatabaseProvider = "postgresql",
            ConnectionString = "Host=localhost;Database=Test;",
            IncludeSamples = false
        };

        try
        {
            // Act
            await _service.CreateProjectAsync("webapi", projectName, projectPath, options);

            // Assert
            Assert.True(Directory.Exists(projectPath));
            Assert.True(File.Exists(Path.Combine(projectPath, $"{projectName}.csproj")));
            Assert.True(File.Exists(Path.Combine(projectPath, "Program.cs")));
            Assert.True(File.Exists(Path.Combine(projectPath, "appsettings.json")));
            Assert.True(Directory.Exists(Path.Combine(projectPath, "Controllers")));
            Assert.True(Directory.Exists(Path.Combine(projectPath, "Entities")));
            Assert.True(Directory.Exists(Path.Combine(projectPath, "Repositories")));
        }
        finally
        {
            if (Directory.Exists(projectPath))
            {
                Directory.Delete(projectPath, true);
            }
        }
    }

    [Fact]
    public async Task CreateProjectAsync_WithSamples_CreatesSampleFiles()
    {
        // Arrange
        var projectName = "TestWithSamples";
        var projectPath = Path.Combine(_testDir, projectName);
        var options = new ProjectOptions
        {
            DatabaseProvider = "sqlserver",
            ConnectionString = "Server=localhost;Database=Test;",
            IncludeSamples = true
        };

        try
        {
            // Act
            await _service.CreateProjectAsync("webapi", projectName, projectPath, options);

            // Assert
            Assert.True(File.Exists(Path.Combine(projectPath, "Entities", "User.cs")));
            Assert.True(File.Exists(Path.Combine(projectPath, "Controllers", "UsersController.cs")));
        }
        finally
        {
            if (Directory.Exists(projectPath))
            {
                Directory.Delete(projectPath, true);
            }
        }
    }

    [Fact]
    public async Task CreateProjectAsync_ClassLibrary_CreatesProjectStructure()
    {
        // Arrange
        var projectName = "TestClassLib";
        var projectPath = Path.Combine(_testDir, projectName);
        var options = new ProjectOptions
        {
            DatabaseProvider = "mysql",
            ConnectionString = "Server=localhost;Database=Test;",
            IncludeSamples = false
        };

        try
        {
            // Act
            await _service.CreateProjectAsync("classlib", projectName, projectPath, options);

            // Assert
            Assert.True(Directory.Exists(projectPath));
            Assert.True(File.Exists(Path.Combine(projectPath, $"{projectName}.csproj")));
            Assert.True(Directory.Exists(Path.Combine(projectPath, "Entities")));
            Assert.True(Directory.Exists(Path.Combine(projectPath, "Repositories")));
        }
        finally
        {
            if (Directory.Exists(projectPath))
            {
                Directory.Delete(projectPath, true);
            }
        }
    }

    [Fact]
    public async Task CreateProjectAsync_InvalidTemplate_ThrowsException()
    {
        // Arrange
        var projectName = "TestInvalid";
        var projectPath = Path.Combine(_testDir, projectName);
        var options = new ProjectOptions();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.CreateProjectAsync("invalidtemplate", projectName, projectPath, options));
    }

    private void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }
}
