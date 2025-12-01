using FluentAssertions;
using Xunit;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NPA.Generators.Generators;

namespace NPA.Generators.Tests;

public class ManyToManyRepositoryGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void DetectManyToManyRelationships_FindsRelationships()
    {
        // Arrange
        var sourceCode = @"
using NPA.Core.Annotations;
using System.Collections.Generic;

namespace TestApp.Entities
{
    public class User
    {
        [Id]
        public int Id { get; set; }
        
        [ManyToMany(MappedBy = ""Users"")]
        [JoinTable(""UserRoles"", 
            JoinColumns = new[] { ""UserId"" }, 
            InverseJoinColumns = new[] { ""RoleId"" })]
        public ICollection<Role> Roles { get; set; } = new List<Role>();
    }

    public class Role
    {
        [Id]
        public int Id { get; set; }
        
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}

namespace TestApp.Repositories
{
    using NPA.Core.Repositories;
    using TestApp.Entities;

    [Repository(typeof(User))]
    public interface IUserRepository : IRepository<User, int>
    {
    }
}";

        var detectMethod = GetDetectManyToManyRelationshipsMethod();

        // Act
        var compilation = CreateCompilation(sourceCode);
        var result = (System.Collections.IList)detectMethod.Invoke(null, new object[] { compilation, "TestApp.Entities.User" })!;

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        
        var relationship = result[0];
        var propertyName = GetPropertyValue(relationship, "PropertyName");
        propertyName.Should().Be("Roles");
        
        var joinTableName = GetPropertyValue(relationship, "JoinTableName");
        joinTableName.Should().Be("UserRoles");
        
        var joinColumns = (string[])GetPropertyValue(relationship, "JoinColumns")!;
        joinColumns.Should().ContainSingle().Which.Should().Be("UserId");
        
        var inverseJoinColumns = (string[])GetPropertyValue(relationship, "InverseJoinColumns")!;
        inverseJoinColumns.Should().ContainSingle().Which.Should().Be("RoleId");
    }

    [Fact]
    public void DetectManyToManyRelationships_ExtractsCollectionElementType()
    {
        // Arrange
        var sourceCode = @"
using NPA.Core.Annotations;
using System.Collections.Generic;

namespace TestApp.Entities
{
    public class Student
    {
        [Id]
        public int Id { get; set; }
        
        [ManyToMany]
        [JoinTable(""StudentCourses"")]
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }

    public class Course
    {
        [Id]
        public int Id { get; set; }
    }
}";

        var detectMethod = GetDetectManyToManyRelationshipsMethod();

        // Act
        var compilation = CreateCompilation(sourceCode);
        var result = (System.Collections.IList)detectMethod.Invoke(null, new object[] { compilation, "TestApp.Entities.Student" })!;

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        
        var relationship = result[0];
        var collectionElementType = GetPropertyValue(relationship, "CollectionElementType");
        collectionElementType.Should().Be("TestApp.Entities.Course");
    }

    [Fact]
    public void DetectManyToManyRelationships_HandlesMultipleRelationships()
    {
        // Arrange
        var sourceCode = @"
using NPA.Core.Annotations;
using System.Collections.Generic;

namespace TestApp.Entities
{
    public class User
    {
        [Id]
        public int Id { get; set; }
        
        [ManyToMany]
        [JoinTable(""UserRoles"")]
        public ICollection<Role> Roles { get; set; } = new List<Role>();
        
        [ManyToMany]
        [JoinTable(""UserGroups"")]
        public ICollection<Group> Groups { get; set; } = new List<Group>();
    }

    public class Role
    {
        [Id]
        public int Id { get; set; }
    }
    
    public class Group
    {
        [Id]
        public int Id { get; set; }
    }
}";

        var detectMethod = GetDetectManyToManyRelationshipsMethod();

        // Act
        var compilation = CreateCompilation(sourceCode);
        var result = (System.Collections.IList)detectMethod.Invoke(null, new object[] { compilation, "TestApp.Entities.User" })!;

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(2);
    }

    [Fact]
    public void GenerateManyToManyMethods_IncludesGetMethod()
    {
        // Arrange
        var generateMethod = GetGenerateManyToManyMethodsMethod();
        var repositoryInfo = CreateRepositoryInfoWithManyToMany();

        // Act
        var result = (string)generateMethod.Invoke(null, new object[] { repositoryInfo })!;

        // Assert
        result.Should().Contain("public async Task<IEnumerable<TestApp.Entities.Role>> GetRolesAsync(int userId)");
        result.Should().Contain("SELECT r.*");
        result.Should().Contain("FROM UserRoles jt");
        result.Should().Contain("INNER JOIN Role r ON jt.RoleId = r.Id");
        result.Should().Contain("WHERE jt.UserId = @UserId");
    }

    [Fact]
    public void GenerateManyToManyMethods_IncludesAddMethod()
    {
        // Arrange
        var generateMethod = GetGenerateManyToManyMethodsMethod();
        var repositoryInfo = CreateRepositoryInfoWithManyToMany();

        // Act
        var result = (string)generateMethod.Invoke(null, new object[] { repositoryInfo })!;

        // Assert
        result.Should().Contain("public async Task AddRoleAsync(int userId, int roleId)");
        result.Should().Contain("INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)");
    }

    [Fact]
    public void GenerateManyToManyMethods_IncludesRemoveMethod()
    {
        // Arrange
        var generateMethod = GetGenerateManyToManyMethodsMethod();
        var repositoryInfo = CreateRepositoryInfoWithManyToMany();

        // Act
        var result = (string)generateMethod.Invoke(null, new object[] { repositoryInfo })!;

        // Assert
        result.Should().Contain("public async Task RemoveRoleAsync(int userId, int roleId)");
        result.Should().Contain("DELETE FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId");
    }

    [Fact]
    public void GenerateManyToManyMethods_IncludesExistenceCheck()
    {
        // Arrange
        var generateMethod = GetGenerateManyToManyMethodsMethod();
        var repositoryInfo = CreateRepositoryInfoWithManyToMany();

        // Act
        var result = (string)generateMethod.Invoke(null, new object[] { repositoryInfo })!;

        // Assert
        result.Should().Contain("public async Task<bool> HasRoleAsync(int userId, int roleId)");
        result.Should().Contain("SELECT COUNT(1) FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId");
        result.Should().Contain("return count > 0;");
    }

    [Fact]
    public void GenerateManyToManyMethods_IncludesXmlDocumentation()
    {
        // Arrange
        var generateMethod = GetGenerateManyToManyMethodsMethod();
        var repositoryInfo = CreateRepositoryInfoWithManyToMany();

        // Act
        var result = (string)generateMethod.Invoke(null, new object[] { repositoryInfo })!;

        // Assert
        result.Should().Contain("/// <summary>");
        result.Should().Contain("/// Gets all Roles for a User asynchronously.");
        result.Should().Contain("/// Adds a relationship between a User and a Role asynchronously.");
        result.Should().Contain("/// Removes a relationship between a User and a Role asynchronously.");
        result.Should().Contain("/// Checks if a relationship exists between a User and a Role asynchronously.");
    }

    [Fact]
    public void GenerateManyToManyMethods_HandlesSchemaQualifiedJoinTable()
    {
        // Arrange
        var generateMethod = GetGenerateManyToManyMethodsMethod();
        var repositoryInfo = CreateRepositoryInfoWithSchema();

        // Act
        var result = (string)generateMethod.Invoke(null, new object[] { repositoryInfo })!;

        // Assert
        result.Should().Contain("FROM dbo.UserRoles jt");
    }

    [Fact]
    public void GenerateManyToManyMethods_UsesDefaultColumnNames()
    {
        // Arrange
        var generateMethod = GetGenerateManyToManyMethodsMethod();
        var repositoryInfo = CreateRepositoryInfoWithoutColumnNames();

        // Act
        var result = (string)generateMethod.Invoke(null, new object[] { repositoryInfo })!;

        // Assert
        result.Should().Contain("WHERE jt.UserId = @UserId");
        result.Should().Contain("INNER JOIN Role r ON jt.RoleId = r.Id");
    }

    // Helper methods
    private MethodInfo GetDetectManyToManyRelationshipsMethod()
    {
        var generatorType = typeof(RepositoryGenerator);
        var method = generatorType.GetMethod("DetectManyToManyRelationships", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull("DetectManyToManyRelationships method should exist");
        return method!;
    }

    private MethodInfo GetGenerateManyToManyMethodsMethod()
    {
        var generatorType = typeof(RepositoryGenerator);
        var method = generatorType.GetMethod("GenerateManyToManyMethods", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull("GenerateManyToManyMethods method should exist");
        return method!;
    }

    private object CreateRepositoryInfoWithManyToMany()
    {
        var assembly = typeof(RepositoryGenerator).Assembly;
        var repositoryInfoType = assembly.GetType("NPA.Generators.RepositoryInfo");
        var relationshipInfoType = assembly.GetType("NPA.Generators.ManyToManyRelationshipInfo");
        
        var repositoryInfo = Activator.CreateInstance(repositoryInfoType!)!;
        SetPropertyValue(repositoryInfo, "EntityType", "TestApp.Entities.User");
        SetPropertyValue(repositoryInfo, "KeyType", "int");
        
        var relationship = Activator.CreateInstance(relationshipInfoType!)!;
        SetPropertyValue(relationship, "PropertyName", "Roles");
        SetPropertyValue(relationship, "CollectionElementType", "TestApp.Entities.Role");
        SetPropertyValue(relationship, "JoinTableName", "UserRoles");
        SetPropertyValue(relationship, "JoinTableSchema", "");
        SetPropertyValue(relationship, "JoinColumns", new[] { "UserId" });
        SetPropertyValue(relationship, "InverseJoinColumns", new[] { "RoleId" });
        
        var relationships = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(relationshipInfoType!))!;
        relationships.Add(relationship);
        
        SetPropertyValue(repositoryInfo, "ManyToManyRelationships", relationships);
        
        return repositoryInfo;
    }

    private object CreateRepositoryInfoWithSchema()
    {
        var assembly = typeof(RepositoryGenerator).Assembly;
        var repositoryInfoType = assembly.GetType("NPA.Generators.RepositoryInfo");
        var relationshipInfoType = assembly.GetType("NPA.Generators.ManyToManyRelationshipInfo");
        
        var repositoryInfo = Activator.CreateInstance(repositoryInfoType!)!;
        SetPropertyValue(repositoryInfo, "EntityType", "TestApp.Entities.User");
        SetPropertyValue(repositoryInfo, "KeyType", "int");
        
        var relationship = Activator.CreateInstance(relationshipInfoType!)!;
        SetPropertyValue(relationship, "PropertyName", "Roles");
        SetPropertyValue(relationship, "CollectionElementType", "TestApp.Entities.Role");
        SetPropertyValue(relationship, "JoinTableName", "UserRoles");
        SetPropertyValue(relationship, "JoinTableSchema", "dbo");
        SetPropertyValue(relationship, "JoinColumns", new[] { "UserId" });
        SetPropertyValue(relationship, "InverseJoinColumns", new[] { "RoleId" });
        
        var relationships = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(relationshipInfoType!))!;
        relationships.Add(relationship);
        
        SetPropertyValue(repositoryInfo, "ManyToManyRelationships", relationships);
        
        return repositoryInfo;
    }

    private object CreateRepositoryInfoWithoutColumnNames()
    {
        var assembly = typeof(RepositoryGenerator).Assembly;
        var repositoryInfoType = assembly.GetType("NPA.Generators.RepositoryInfo");
        var relationshipInfoType = assembly.GetType("NPA.Generators.ManyToManyRelationshipInfo");
        
        var repositoryInfo = Activator.CreateInstance(repositoryInfoType!)!;
        SetPropertyValue(repositoryInfo, "EntityType", "TestApp.Entities.User");
        SetPropertyValue(repositoryInfo, "KeyType", "int");
        
        var relationship = Activator.CreateInstance(relationshipInfoType!)!;
        SetPropertyValue(relationship, "PropertyName", "Roles");
        SetPropertyValue(relationship, "CollectionElementType", "TestApp.Entities.Role");
        SetPropertyValue(relationship, "JoinTableName", "UserRoles");
        SetPropertyValue(relationship, "JoinTableSchema", "");
        SetPropertyValue(relationship, "JoinColumns", Array.Empty<string>());
        SetPropertyValue(relationship, "InverseJoinColumns", Array.Empty<string>());
        
        var relationships = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(relationshipInfoType!))!;
        relationships.Add(relationship);
        
        SetPropertyValue(repositoryInfo, "ManyToManyRelationships", relationships);
        
        return repositoryInfo;
    }

    private void SetPropertyValue(object obj, string propertyName, object? value)
    {
        var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        property.Should().NotBeNull($"Property {propertyName} should exist");
        property!.SetValue(obj, value);
    }

    private object? GetPropertyValue(object obj, string propertyName)
    {
        var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        property.Should().NotBeNull($"Property {propertyName} should exist");
        return property!.GetValue(obj);
    }

}
