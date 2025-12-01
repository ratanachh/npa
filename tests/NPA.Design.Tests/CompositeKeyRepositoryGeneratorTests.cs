using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using System.Linq;
using System.Reflection;
using NPA.Design.Generators;

namespace NPA.Design.Tests;

public class CompositeKeyRepositoryGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void DetectCompositeKey_WithTwoIdAttributes_ReturnsTrue()
    {
        // Arrange
        var code = @"
using NPA.Core.Annotations;

namespace Test
{
    [Table(""order_items"")]
    public class OrderItem
    {
        [Id]
        public int OrderId { get; set; }
        
        [Id]
        public int ProductId { get; set; }
        
        public int Quantity { get; set; }
    }
}";

        var compilation = CreateCompilation(code);
        var detectMethod = GetDetectCompositeKeyMethod();

        // Act
        var result = detectMethod.Invoke(null, new object[] { compilation, "Test.OrderItem" });
        var (hasCompositeKey, keyProperties) = ((bool, System.Collections.Generic.List<string>))result!;

        // Assert
        hasCompositeKey.Should().BeTrue();
        keyProperties.Should().HaveCount(2);
        keyProperties.Should().Contain("OrderId");
        keyProperties.Should().Contain("ProductId");
    }

    [Fact]
    public void DetectCompositeKey_WithSingleIdAttribute_ReturnsFalse()
    {
        // Arrange
        var code = @"
using NPA.Core.Annotations;

namespace Test
{
    [Table(""users"")]
    public class User
    {
        [Id]
        public int Id { get; set; }
        
        public string Name { get; set; }
    }
}";

        var compilation = CreateCompilation(code);
        var detectMethod = GetDetectCompositeKeyMethod();

        // Act
        var result = detectMethod.Invoke(null, new object[] { compilation, "Test.User" });
        var (hasCompositeKey, keyProperties) = ((bool, System.Collections.Generic.List<string>))result!;

        // Assert
        hasCompositeKey.Should().BeFalse();
        keyProperties.Should().BeEmpty();
    }

    [Fact]
    public void DetectCompositeKey_WithThreeIdAttributes_ReturnsTrue()
    {
        // Arrange
        var code = @"
using NPA.Core.Annotations;

namespace Test
{
    [Table(""complex_keys"")]
    public class ComplexKey
    {
        [Id]
        public int KeyPart1 { get; set; }
        
        [Id]
        public string KeyPart2 { get; set; }
        
        [Id]
        public System.Guid KeyPart3 { get; set; }
    }
}";

        var compilation = CreateCompilation(code);
        var detectMethod = GetDetectCompositeKeyMethod();

        // Act
        var result = detectMethod.Invoke(null, new object[] { compilation, "Test.ComplexKey" });
        var (hasCompositeKey, keyProperties) = ((bool, System.Collections.Generic.List<string>))result!;

        // Assert
        hasCompositeKey.Should().BeTrue();
        keyProperties.Should().HaveCount(3);
        keyProperties.Should().Contain("KeyPart1");
        keyProperties.Should().Contain("KeyPart2");
        keyProperties.Should().Contain("KeyPart3");
    }

    [Fact]
    public void DetectCompositeKey_WithNoIdAttributes_ReturnsFalse()
    {
        // Arrange
        var code = @"
using NPA.Core.Annotations;

namespace Test
{
    [Table(""products"")]
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";

        var compilation = CreateCompilation(code);
        var detectMethod = GetDetectCompositeKeyMethod();

        // Act
        var result = detectMethod.Invoke(null, new object[] { compilation, "Test.Product" });
        var (hasCompositeKey, keyProperties) = ((bool, System.Collections.Generic.List<string>))result!;

        // Assert
        hasCompositeKey.Should().BeFalse();
        keyProperties.Should().BeEmpty();
    }

    [Fact]
    public void ToCamelCase_ConvertsCorrectly()
    {
        // Arrange
        var toCamelCaseMethod = GetToCamelCaseMethod();

        // Act & Assert
        toCamelCaseMethod.Invoke(null, new object[] { "OrderId" }).Should().Be("orderId");
        toCamelCaseMethod.Invoke(null, new object[] { "ProductName" }).Should().Be("productName");
        toCamelCaseMethod.Invoke(null, new object[] { "ID" }).Should().Be("iD");
        toCamelCaseMethod.Invoke(null, new object[] { "name" }).Should().Be("name");
        toCamelCaseMethod.Invoke(null, new object[] { "" }).Should().Be("");
    }

    [Fact]
    public void GenerateCompositeKeyMethods_IncludesAllMethods()
    {
        // Arrange
        var generateMethod = GetGenerateCompositeKeyMethodsMethod();
        var repositoryInfo = CreateRepositoryInfo(
            "Test.OrderItem",
            hasCompositeKey: true,
            compositeKeyProperties: new System.Collections.Generic.List<string> { "OrderId", "ProductId" }
        );

        // Act
        var result = (string)generateMethod.Invoke(null, new object[] { repositoryInfo })!;

        // Assert
        result.Should().Contain("GetByIdAsync(NPA.Core.Core.CompositeKey key)");
        result.Should().Contain("DeleteAsync(NPA.Core.Core.CompositeKey key)");
        result.Should().Contain("ExistsAsync(NPA.Core.Core.CompositeKey key)");
        result.Should().Contain("FindByCompositeKeyAsync");
    }

    [Fact]
    public void GenerateCompositeKeyMethods_IncludesXmlDocumentation()
    {
        // Arrange
        var generateMethod = GetGenerateCompositeKeyMethodsMethod();
        var repositoryInfo = CreateRepositoryInfo(
            "Test.Entity",
            hasCompositeKey: true,
            compositeKeyProperties: new System.Collections.Generic.List<string> { "Id1", "Id2" }
        );

        // Act
        var result = (string)generateMethod.Invoke(null, new object[] { repositoryInfo })!;

        // Assert
        result.Should().Contain("/// <summary>");
        result.Should().Contain("/// Gets an entity by its composite key asynchronously.");
        result.Should().Contain("/// <param name=\"key\">The composite key.</param>");
    }

    private MethodInfo GetDetectCompositeKeyMethod()
    {
        var generatorType = typeof(RepositoryGenerator);
        var method = generatorType.GetMethod("DetectCompositeKey", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull("DetectCompositeKey method should exist");
        return method!;
    }

    private MethodInfo GetToCamelCaseMethod()
    {
        var generatorType = typeof(RepositoryGenerator);
        var method = generatorType.GetMethod("ToCamelCase", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull("ToCamelCase method should exist");
        return method!;
    }

    private MethodInfo GetGenerateCompositeKeyMethodsMethod()
    {
        var generatorType = typeof(RepositoryGenerator);
        var method = generatorType.GetMethod("GenerateCompositeKeyMethods", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull("GenerateCompositeKeyMethods method should exist");
        return method!;
    }

    private object CreateRepositoryInfo(string entityType, bool hasCompositeKey, System.Collections.Generic.List<string> compositeKeyProperties)
    {
        var generatorType = typeof(RepositoryGenerator);
        var repositoryInfoType = generatorType.Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "RepositoryInfo");
            
        repositoryInfoType.Should().NotBeNull("RepositoryInfo type should exist");
        
        var instance = Activator.CreateInstance(repositoryInfoType!)!;
        
        repositoryInfoType!.GetProperty("EntityType")!.SetValue(instance, entityType);
        repositoryInfoType.GetProperty("HasCompositeKey")!.SetValue(instance, hasCompositeKey);
        repositoryInfoType.GetProperty("CompositeKeyProperties")!.SetValue(instance, compositeKeyProperties);
        
        return instance;
    }
}
