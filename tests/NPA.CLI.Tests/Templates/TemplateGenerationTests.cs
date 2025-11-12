using NPA.CLI.Templates;
using Xunit;

namespace NPA.CLI.Tests.Templates;

public class TemplateGenerationTests
{
    [Fact]
    public void RepositoryInterfaceTemplate_GeneratesValidCode()
    {
        // Arrange
        var template = new RepositoryInterfaceTemplate();

        // Act
        var code = template.Generate("Customer", "MyApp.Repositories");

        // Assert
        Assert.Contains("interface ICustomerRepository", code);
        Assert.Contains("namespace MyApp.Repositories", code);
        Assert.Contains("[Repository]", code);
        Assert.Contains("IRepository<Customer, long>", code);
    }

    [Fact]
    public void RepositoryImplementationTemplate_GeneratesValidCode()
    {
        // Arrange
        var template = new RepositoryImplementationTemplate();

        // Act
        var code = template.Generate("Order", "MyApp.Repositories");

        // Assert
        Assert.Contains("class OrderRepository", code);
        Assert.Contains("IOrderRepository", code);
        Assert.Contains("IEntityManager", code);
        Assert.Contains("namespace MyApp.Repositories", code);
    }

    [Fact]
    public void EntityTemplate_GeneratesValidCode()
    {
        // Arrange
        var template = new EntityTemplate();

        // Act
        var code = template.Generate("Product", "MyApp.Entities", "products");

        // Assert
        Assert.Contains("class Product", code);
        Assert.Contains("namespace MyApp.Entities", code);
        Assert.Contains("[Entity]", code);
        Assert.Contains("[Table(\"products\")]", code);
        Assert.Contains("[Id]", code);
        Assert.Contains("[GeneratedValue(GenerationType.Identity)]", code);
    }

    [Fact]
    public void MigrationTemplate_GeneratesValidCode()
    {
        // Arrange
        var template = new MigrationTemplate();

        // Act
        var code = template.Generate("CreateUserTable", "MyApp.Migrations");

        // Assert
        Assert.Contains("Migration", code);
        Assert.Contains("namespace MyApp.Migrations", code);
        Assert.Contains("IMigration", code);
        Assert.Contains("Version", code);
        Assert.Contains("UpAsync", code);
        Assert.Contains("DownAsync", code);
    }

    [Fact]
    public void TestTemplate_GeneratesValidCode()
    {
        // Arrange
        var template = new TestTemplate();

        // Act
        var code = template.Generate("UserService", "MyApp.Tests");

        // Assert
        Assert.Contains("class UserServiceTests", code);
        Assert.Contains("namespace MyApp.Tests", code);
        Assert.Contains("[Fact]", code);
        Assert.Contains("Assert.True", code);
    }
}
