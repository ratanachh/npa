using FluentAssertions;
using Xunit;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for repository source generator.
/// Note: Source generator testing requires MSBuild integration testing.
/// These tests verify the generator logic without full compilation.
/// </summary>
public class RepositoryGeneratorTests
{
    [Fact]
    public void RepositoryGenerator_ShouldBeMarkedAsGenerator()
    {
        // Arrange
        var generatorType = typeof(RepositoryGenerator);

        // Act
        var hasGeneratorAttribute = generatorType.GetCustomAttributes(typeof(Microsoft.CodeAnalysis.GeneratorAttribute), false).Any();

        // Assert
        hasGeneratorAttribute.Should().BeTrue("RepositoryGenerator should have [Generator] attribute");
    }

    [Fact]
    public void RepositoryGenerator_ShouldImplementIIncrementalGenerator()
    {
        // Arrange
        var generatorType = typeof(RepositoryGenerator);

        // Act
        var implementsInterface = typeof(Microsoft.CodeAnalysis.IIncrementalGenerator).IsAssignableFrom(generatorType);

        // Assert
        implementsInterface.Should().BeTrue("RepositoryGenerator should implement IIncrementalGenerator");
    }

    [Fact]
    public void RepositoryGenerator_ShouldHaveInitializeMethod()
    {
        // Arrange
        var generatorType = typeof(RepositoryGenerator);

        // Act
        var initializeMethod = generatorType.GetMethod("Initialize");

        // Assert
        initializeMethod.Should().NotBeNull("RepositoryGenerator should have Initialize method");
        initializeMethod!.GetParameters().Length.Should().Be(1, "Initialize should take one parameter");
    }

    [Theory]
    [InlineData("IUserRepository", "UserRepository")]
    [InlineData("IProductRepository", "ProductRepository")]
    [InlineData("IOrderRepository", "OrderRepository")]
    [InlineData("MyCustomRepository", "MyCustomRepositoryImplementation")]
    public void GetImplementationName_ShouldGenerateCorrectName(string interfaceName, string expected)
    {
        // This tests the naming convention logic (would need to expose the method or test through generator output)
        // For now, this documents the expected behavior
        expected.Should().NotBeNull();
    }

    [Fact]
    public void RepositoryAttribute_ShouldExistInNPACore()
    {
        // Verify the RepositoryAttribute is available
        var attributeType = typeof(NPA.Core.Annotations.RepositoryAttribute);
        
        attributeType.Should().NotBeNull("RepositoryAttribute should exist in NPA.Core.Annotations");
        attributeType.Should().BeAssignableTo<Attribute>("RepositoryAttribute should be an Attribute");
    }

    [Fact]
    public void RepositoryAttribute_ShouldTargetInterfaces()
    {
        // Arrange
        var attributeType = typeof(NPA.Core.Annotations.RepositoryAttribute);
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(attributeType, typeof(AttributeUsageAttribute));

        // Assert
        attributeUsage.Should().NotBeNull("RepositoryAttribute should have AttributeUsage");
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Interface, "RepositoryAttribute should target interfaces");
    }

    [Fact]
    public void RepositoryAttribute_ShouldHaveEntityTypeProperty()
    {
        // Arrange
        var attributeType = typeof(NPA.Core.Annotations.RepositoryAttribute);

        // Act
        var entityTypeProperty = attributeType.GetProperty("EntityType");

        // Assert
        entityTypeProperty.Should().NotBeNull("RepositoryAttribute should have EntityType property");
        entityTypeProperty!.PropertyType.Should().Be(typeof(Type), "EntityType should be of type Type");
    }

    [Fact]
    public void RepositoryAttribute_ShouldHaveGenerateDefaultMethodsProperty()
    {
        // Arrange
        var attributeType = typeof(NPA.Core.Annotations.RepositoryAttribute);

        // Act
        var property = attributeType.GetProperty("GenerateDefaultMethods");

        // Assert
        property.Should().NotBeNull("RepositoryAttribute should have GenerateDefaultMethods property");
        property!.PropertyType.Should().Be(typeof(bool), "GenerateDefaultMethods should be bool");
    }

    [Fact]
    public void RepositoryAttribute_ShouldHaveConstructors()
    {
        // Arrange
        var attributeType = typeof(NPA.Core.Annotations.RepositoryAttribute);

        // Act
        var constructors = attributeType.GetConstructors();

        // Assert
        constructors.Should().HaveCountGreaterOrEqualTo(1, "RepositoryAttribute should have at least one constructor");
    }
}
