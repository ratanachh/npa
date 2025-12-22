using FluentAssertions;
using Xunit;

namespace NPA.Design.Tests;

/// <summary>
/// Tests for incremental generator optimizations (Phase 4.5).
/// Verifies that the generator properly implements value equality and caching.
/// </summary>
public class IncrementalGeneratorOptimizationTests : GeneratorTestBase
{
    [Fact]
    public void RepositoryInfoComparer_ShouldExist()
    {
        // Arrange & Act
        var comparerType = GetComparerType();

        // Assert
        comparerType.Should().NotBeNull("RepositoryInfoComparer should exist for incremental caching");
    }

    [Fact]
    public void RepositoryInfoComparer_ShouldImplementIEqualityComparer()
    {
        // Arrange
        var comparerType = GetComparerType();
        var repositoryInfoType = GetRepositoryInfoType();

        // Act
        var implementsInterface = comparerType!.GetInterfaces()
            .Any(i => i.IsGenericType && 
                     i.GetGenericTypeDefinition() == typeof(IEqualityComparer<>) &&
                     i.GetGenericArguments()[0].Name == repositoryInfoType!.Name);

        // Assert
        implementsInterface.Should().BeTrue("RepositoryInfoComparer should implement IEqualityComparer<RepositoryInfo>");
    }

    [Fact]
    public void RepositoryInfoComparer_Equals_ShouldReturnTrueForIdenticalInfo()
    {
        // Arrange
        var comparer = CreateComparer();
        var info1 = CreateRepositoryInfo("IUserRepository", "TestNamespace", "User", "int");
        var info2 = CreateRepositoryInfo("IUserRepository", "TestNamespace", "User", "int");

        // Act
        var result = InvokeEquals(comparer, info1, info2);

        // Assert
        result.Should().BeTrue("identical RepositoryInfo objects should be equal");
    }

    [Fact]
    public void RepositoryInfoComparer_Equals_ShouldReturnFalseForDifferentInterfaceName()
    {
        // Arrange
        var comparer = CreateComparer();
        var info1 = CreateRepositoryInfo("IUserRepository", "TestNamespace", "User", "int");
        var info2 = CreateRepositoryInfo("IProductRepository", "TestNamespace", "User", "int");

        // Act
        var result = InvokeEquals(comparer, info1, info2);

        // Assert
        result.Should().BeFalse("RepositoryInfo with different interface names should not be equal");
    }

    [Fact]
    public void RepositoryInfoComparer_Equals_ShouldReturnFalseForDifferentNamespace()
    {
        // Arrange
        var comparer = CreateComparer();
        var info1 = CreateRepositoryInfo("IUserRepository", "Namespace1", "User", "int");
        var info2 = CreateRepositoryInfo("IUserRepository", "Namespace2", "User", "int");

        // Act
        var result = InvokeEquals(comparer, info1, info2);

        // Assert
        result.Should().BeFalse("RepositoryInfo with different namespaces should not be equal");
    }

    [Fact]
    public void RepositoryInfoComparer_Equals_ShouldReturnFalseForDifferentEntityType()
    {
        // Arrange
        var comparer = CreateComparer();
        var info1 = CreateRepositoryInfo("IUserRepository", "TestNamespace", "User", "int");
        var info2 = CreateRepositoryInfo("IUserRepository", "TestNamespace", "Product", "int");

        // Act
        var result = InvokeEquals(comparer, info1, info2);

        // Assert
        result.Should().BeFalse("RepositoryInfo with different entity types should not be equal");
    }

    [Fact]
    public void RepositoryInfoComparer_Equals_ShouldReturnFalseForDifferentKeyType()
    {
        // Arrange
        var comparer = CreateComparer();
        var info1 = CreateRepositoryInfo("IUserRepository", "TestNamespace", "User", "int");
        var info2 = CreateRepositoryInfo("IUserRepository", "TestNamespace", "User", "Guid");

        // Act
        var result = InvokeEquals(comparer, info1, info2);

        // Assert
        result.Should().BeFalse("RepositoryInfo with different key types should not be equal");
    }

    [Fact]
    public void RepositoryInfoComparer_GetHashCode_ShouldReturnSameHashForIdenticalInfo()
    {
        // Arrange
        var comparer = CreateComparer();
        var info1 = CreateRepositoryInfo("IUserRepository", "TestNamespace", "User", "int");
        var info2 = CreateRepositoryInfo("IUserRepository", "TestNamespace", "User", "int");

        // Act
        var hash1 = InvokeGetHashCode(comparer, info1);
        var hash2 = InvokeGetHashCode(comparer, info2);

        // Assert
        hash1.Should().Be(hash2, "identical RepositoryInfo objects should have the same hash code");
    }

    [Fact]
    public void RepositoryInfoComparer_GetHashCode_ShouldReturnDifferentHashForDifferentInfo()
    {
        // Arrange
        var comparer = CreateComparer();
        var info1 = CreateRepositoryInfo("IUserRepository", "TestNamespace", "User", "int");
        var info2 = CreateRepositoryInfo("IProductRepository", "TestNamespace", "Product", "Guid");

        // Act
        var hash1 = InvokeGetHashCode(comparer, info1);
        var hash2 = InvokeGetHashCode(comparer, info2);

        // Assert
        hash1.Should().NotBe(hash2, "different RepositoryInfo objects should typically have different hash codes");
    }

    [Fact]
    public void RepositoryInfoComparer_Equals_ShouldHandleNullValues()
    {
        // Arrange
        var comparer = CreateComparer();
        var info = CreateRepositoryInfo("IUserRepository", "TestNamespace", "User", "int");

        // Act
        var result1 = InvokeEquals(comparer, null, null);
        var result2 = InvokeEquals(comparer, info, null);
        var result3 = InvokeEquals(comparer, null, info);

        // Assert
        result1.Should().BeTrue("null should equal null");
        result2.Should().BeFalse("non-null should not equal null");
        result3.Should().BeFalse("null should not equal non-null");
    }

    // Helper methods

    private static Type? GetComparerType()
    {
        return GetGeneratorType("NPA.Design.Comparers.RepositoryInfoComparer");
    }

    private static Type? GetRepositoryInfoType()
    {
        return GetGeneratorType("NPA.Design.Models.RepositoryInfo");
    }

    private static object CreateComparer()
    {
        var comparerType = GetComparerType();
        return Activator.CreateInstance(comparerType!)!;
    }

    private static object CreateRepositoryInfo(string interfaceName, string @namespace, string entityType, string keyType)
    {
        var instance = CreateRepositoryInfo(entityType, keyType, interfaceName, @namespace);
        SetPropertyValue(instance, "HasCompositeKey", false);
        return instance;
    }

    private static bool InvokeEquals(object comparer, object? x, object? y)
    {
        var equalsMethod = comparer.GetType().GetMethod("Equals", new[] { GetRepositoryInfoType()!, GetRepositoryInfoType()! });
        return (bool)equalsMethod!.Invoke(comparer, new[] { x, y })!;
    }

    private static int InvokeGetHashCode(object comparer, object obj)
    {
        var getHashCodeMethod = comparer.GetType().GetMethod("GetHashCode", new[] { GetRepositoryInfoType()! });
        return (int)getHashCodeMethod!.Invoke(comparer, new[] { obj })!;
    }
}
