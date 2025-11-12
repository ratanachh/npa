using NPA.CLI.Services;
using Xunit;

namespace NPA.CLI.Tests.Services;

public class CodeGenerationServiceTests
{
    private readonly CodeGenerationService _service;

    public CodeGenerationServiceTests()
    {
        _service = new CodeGenerationService();
    }

    [Fact]
    public async Task GenerateRepositoryInterfaceAsync_ReturnsValidCode()
    {
        // Arrange
        var entityName = "User";
        var namespaceName = "MyApp.Repositories";

        // Act
        var code = await _service.GenerateRepositoryInterfaceAsync(entityName, namespaceName);

        // Assert
        Assert.NotNull(code);
        Assert.Contains("interface IUserRepository", code);
        Assert.Contains("namespace MyApp.Repositories", code);
        Assert.Contains("[Repository]", code);
    }

    [Fact]
    public async Task GenerateRepositoryImplementationAsync_ReturnsValidCode()
    {
        // Arrange
        var entityName = "Product";
        var namespaceName = "MyApp.Repositories";

        // Act
        var code = await _service.GenerateRepositoryImplementationAsync(entityName, namespaceName);

        // Assert
        Assert.NotNull(code);
        Assert.Contains("class ProductRepository", code);
        Assert.Contains("namespace MyApp.Repositories", code);
        Assert.Contains("IEntityManager", code);
    }

    [Fact]
    public async Task GenerateEntityAsync_ReturnsValidCode()
    {
        // Arrange
        var entityName = "Order";
        var namespaceName = "MyApp.Entities";
        var tableName = "orders";

        // Act
        var code = await _service.GenerateEntityAsync(entityName, namespaceName, tableName);

        // Assert
        Assert.NotNull(code);
        Assert.Contains("class Order", code);
        Assert.Contains("namespace MyApp.Entities", code);
        Assert.Contains("[Entity]", code);
        Assert.Contains("[Table(\"orders\")]", code);
    }

    [Fact]
    public async Task GenerateMigrationAsync_ReturnsValidCode()
    {
        // Arrange
        var migrationName = "AddUserTable";
        var namespaceName = "MyApp.Migrations";

        // Act
        var code = await _service.GenerateMigrationAsync(migrationName, namespaceName);

        // Assert
        Assert.NotNull(code);
        Assert.Contains("Migration", code);
        Assert.Contains("namespace MyApp.Migrations", code);
        Assert.Contains("IMigration", code);
        Assert.Contains("Version", code);
    }

    [Fact]
    public async Task GenerateTestAsync_ReturnsValidCode()
    {
        // Arrange
        var className = "UserRepository";
        var namespaceName = "MyApp.Tests";

        // Act
        var code = await _service.GenerateTestAsync(className, namespaceName);

        // Assert
        Assert.NotNull(code);
        Assert.Contains("class UserRepositoryTests", code);
        Assert.Contains("namespace MyApp.Tests", code);
        Assert.Contains("[Fact]", code);
    }
}
