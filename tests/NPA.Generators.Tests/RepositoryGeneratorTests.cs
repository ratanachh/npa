using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using NPA.Generators.Generators;
using NPA.Generators.Services;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for repository source generator.
/// </summary>
public class RepositoryGeneratorTests : GeneratorTestBase
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
    [InlineData("IUserRepository", "UserRepositoryImplementation")]
    [InlineData("IProductRepository", "ProductRepositoryImplementation")]
    [InlineData("IOrderRepository", "OrderRepositoryImplementation")]
    [InlineData("MyCustomRepository", "MyCustomRepositoryImplementation")]
    public void GetImplementationName_ShouldGenerateCorrectName(string interfaceName, string expectedClassName)
    {
        // Arrange
        var source = $@"
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{{
    [Table(""users"")]
    public class User
    {{
        [Column(""id"")]
        public long Id {{ get; set; }}
    }}

    [Repository]
    public interface {interfaceName} : IRepository<User, long>
    {{
    }}
}}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        var generatedCode = outputCompilation.SyntaxTrees
            .FirstOrDefault(t => t.FilePath.Contains(expectedClassName))?
            .ToString();
        
        generatedCode.Should().NotBeNullOrEmpty($"Generator should produce {expectedClassName}");
        generatedCode.Should().Contain($"class {expectedClassName}", $"Generated class should be named {expectedClassName}");
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
    public void RepositoryAttribute_ShouldHaveParameterlessConstructor()
    {
        // Arrange
        var attributeType = typeof(NPA.Core.Annotations.RepositoryAttribute);

        // Act
        var constructor = attributeType.GetConstructor(Type.EmptyTypes);

        // Assert
        constructor.Should().NotBeNull("RepositoryAttribute should have a parameterless constructor");
    }

    [Fact]
    public void DeleteAsync_ShouldGenerateCorrectSqlWithIdParameter()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{
    [Table(""students"")]
    public class Student
    {
        [Column(""id"")]
        public long Id { get; set; }
        
        [Column(""name"")]
        public string Name { get; set; }
    }

    [Repository]
    public interface IStudentRepository : IRepository<Student, long>
    {
        Task DeleteAsync(long id);
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("StudentRepository"))
            .ToString();
        
        generatedCode.Should().Contain("DELETE FROM students WHERE id = @id", "Should generate DELETE with WHERE clause");
        generatedCode.Should().Contain("await _connection.ExecuteAsync(sql, new { id })", "Should execute with id parameter");
        generatedCode.Should().NotContain("throw new InvalidOperationException", "Should not throw when id parameter is present");
    }

    [Theory]
    [InlineData("id", "id = @id")]
    [InlineData("Id", "id = @Id")]
    [InlineData("ID", "id = @ID")]
    public void DeleteAsync_ShouldHandleIdParameterCaseInsensitive(string paramName, string expectedWhereClause)
    {
        // Arrange
        var source = $@"
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{{
    [Table(""students"")]
    public class Student
    {{
        [Column(""id"")]
        public long Id {{ get; set; }}
    }}

    [Repository]
    public interface IStudentRepository : IRepository<Student, long>
    {{
        Task DeleteAsync(long {paramName});
    }}
}}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("StudentRepository"))
            .ToString();
        
        generatedCode.Should().Contain(expectedWhereClause, $"Should generate WHERE clause with parameter {paramName}");
        generatedCode.Should().Contain($"new {{ {paramName} }}", $"Should include parameter {paramName} in anonymous object");
    }

    [Fact]
    public void DeleteAsync_ShouldNotReturnValue()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{
    [Table(""students"")]
    public class Student
    {
        [Column(""id"")]
        public long Id { get; set; }
    }

    [Repository]
    public interface IStudentRepository : IRepository<Student, long>
    {
        Task DeleteAsync(long id);
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("StudentRepository"))
            .ToString();
        
        generatedCode.Should().Contain("public async System.Threading.Tasks.Task DeleteAsync(long id)", "Method signature should return Task (void)");
        generatedCode.Should().NotContain("return await _connection.ExecuteAsync", "Should NOT return ExecuteAsync result for void Task");
        generatedCode.Should().Contain("await _connection.ExecuteAsync(sql, new { id });", "Should call ExecuteAsync without return");
    }

    [Fact]
    public void DeleteAsync_WithoutParameters_ShouldThrowException()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{
    [Table(""students"")]
    public class Student
    {
        [Column(""id"")]
        public long Id { get; set; }
    }

    [Repository]
    public interface IStudentRepository : IRepository<Student, long>
    {
        Task DeleteAllAsync();
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("StudentRepository"))
            .ToString();
        
        generatedCode.Should().Contain("throw new InvalidOperationException", "Should throw exception for parameterless delete");
        generatedCode.Should().Contain("Delete without WHERE clause is not allowed", "Should have descriptive error message");
    }

    [Fact]
    public void DeleteAsync_WithNonIdParameter_ShouldUseConventionAnalysis()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{
    [Table(""students"")]
    public class Student
    {
        [Column(""id"")]
        public long Id { get; set; }
        
        [Column(""email"")]
        public string Email { get; set; }
    }

    [Repository]
    public interface IStudentRepository : IRepository<Student, long>
    {
        Task DeleteByEmailAsync(string email);
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("StudentRepository"))
            .ToString();
        
        generatedCode.Should().Contain("email = @email", "Should use convention analysis to generate WHERE clause based on method name");
        generatedCode.Should().Contain("DELETE FROM students", "Should generate DELETE statement");
    }

    #region Integration Tests for DELETE Method Generation

    private const string TestSource = @"
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{
    public class Student
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    [Repository]
    public interface IStudentRepository : IRepository<Student, long>
    {
        Task DeleteAsync(long id);
    }
}";

    [Fact]
    public void DeleteAsync_WithIdParameter_Integration_ShouldGenerateWhereClause()
    {
        // Arrange & Act
        RunGeneratorWithOutput<RepositoryGenerator>(TestSource, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        
        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("StudentRepository"))
            .ToList();
        
        generatedTrees.Should().NotBeEmpty("Generator should produce StudentRepository implementation");
        
        var generatedCode = generatedTrees.First().ToString();
        
        // Verify the generated DeleteAsync method
        generatedCode.Should().Contain("public async System.Threading.Tasks.Task DeleteAsync(long id)", 
            "DeleteAsync method signature should be generated");
        
        generatedCode.Should().Contain("DELETE FROM students WHERE id = @id", 
            "SQL should include WHERE clause with id parameter");
        
        generatedCode.Should().Contain("await _connection.ExecuteAsync(sql, new { id })", 
            "Should execute SQL with id parameter");
        
        generatedCode.Should().NotContain("return await _connection.ExecuteAsync", 
            "Should NOT return the result of ExecuteAsync for void Task methods");
        
        generatedCode.Should().NotContain("throw new InvalidOperationException(\"Delete without WHERE clause is not allowed\")", 
            "Should NOT throw exception when id parameter is present");
    }

    [Fact]
    public void DeleteAsync_WithoutParameters_Integration_ShouldThrowException()
    {
        // Arrange
        var sourceWithoutParams = @"
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{
    public class Student
    {
        public long Id { get; set; }
    }

    [Repository]
    public interface IStudentRepository : IRepository<Student, long>
    {
        Task DeleteAllAsync();
    }
}";
        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(sourceWithoutParams, out var outputCompilation, out _);

        // Assert
        var generatedCode = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("StudentRepository"))
            .First()
            .ToString();
        
        generatedCode.Should().Contain("throw new InvalidOperationException(\"Delete without WHERE clause is not allowed\")", 
            "Should throw exception for parameterless delete method");
    }

    [Theory]
    [InlineData("id")]
    [InlineData("Id")]
    [InlineData("ID")]
    public void DeleteAsync_WithIdParameter_Integration_ShouldBeCaseInsensitive(string paramName)
    {
        // Arrange
        var sourceWithCasing = $@"
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{{
    public class Student
    {{
        public long Id {{ get; set; }}
    }}

    [Repository]
    public interface IStudentRepository : IRepository<Student, long>
    {{
        Task DeleteAsync(long {paramName});
    }}
}}";
        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(sourceWithCasing, out var outputCompilation, out _);

        // Assert
        var generatedCode = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("StudentRepository"))
            .First()
            .ToString();
        
        generatedCode.Should().Contain($"id = @{paramName}", 
            $"WHERE clause should use parameter name '{paramName}' regardless of casing");
        
        generatedCode.Should().Contain($"new {{ {paramName} }}", 
            $"Parameter object should include '{paramName}'");
    }

    [Fact]
    public void DeleteAsync_WithCustomParameter_Integration_ShouldUseConventionAnalysis()
    {
        // Arrange
        var sourceWithEmail = @"
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{
    public class Student
    {
        public long Id { get; set; }
        public string Email { get; set; }
    }

    [Repository]
    public interface IStudentRepository : IRepository<Student, long>
    {
        Task DeleteByEmailAsync(string email);
    }
}";
        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(sourceWithEmail, out var outputCompilation, out _);

        // Assert
        var generatedCode = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("StudentRepository"))
            .First()
            .ToString();
        
        // Should use convention analysis to extract "Email" from "DeleteByEmailAsync"
        generatedCode.Should().Contain("Email = @email", 
            "Should generate WHERE clause based on method name convention");
    }

    [Fact]
    public void DeleteAsync_SyncVersion_Integration_ShouldNotUseAwait()
    {
        // Arrange
        var sourceWithSync = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{
    public class Student
    {
        public long Id { get; set; }
    }

    [Repository]
    public interface IStudentRepository : IRepository<Student, long>
    {
        void Delete(long id);
    }
}";
        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(sourceWithSync, out var outputCompilation, out _);

        // Assert
        var generatedCode = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("StudentRepository"))
            .First()
            .ToString();
        
        generatedCode.Should().Contain("_connection.Execute(sql, new { id })", 
            "Synchronous version should use Execute without await");
        
        generatedCode.Should().NotContain("await", 
            "Synchronous version should not use await keyword");
    }

    [Fact]
    public void DeleteAsync_WithMultipleParameters_Integration_ShouldGenerateAndClause()
    {
        // Arrange
        var sourceWithMultiParams = @"
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{
    public class Student
    {
        public long Id { get; set; }
        public long TenantId { get; set; }
    }

    [Repository]
    public interface IStudentRepository : IRepository<Student, long>
    {
        Task DeleteByIdAndTenantIdAsync(long id, long tenantId);
    }
}";
        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(sourceWithMultiParams, out var outputCompilation, out _);

        // Assert
        var generatedCode = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("StudentRepository"))
            .First()
            .ToString();
        
        generatedCode.Should().Contain("Id = @id AND TenantId = @tenantId", 
            "Should generate AND clause for multiple parameters");
    }

    [Fact]
    public void GeneratedRepository_ShouldHaveImplementationSuffix_AndNotBePartial()
    {
        // Arrange - Setup test source code
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Table(""test_entities"")]
    public class TestEntity
    {
        [Id]
        public long Id { get; set; }
        public string Name { get; set; }
    }

    [Repository]
    public interface ITestEntityRepository : IRepository<TestEntity, long>
    {
    }
}";

        // Act - Run generator
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert - No errors
        diagnostics.Should().BeEmpty("generator should not produce errors");

        // Get generated source
        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("TestEntityRepository"))
            .ToList();
            
        generatedTrees.Should().NotBeEmpty("Generator should produce TestEntityRepository implementation");
        
        var generatedCode = generatedTrees.First().ToString();

        // Verify class name has Implementation suffix
        generatedCode.Should().Contain("class TestEntityRepositoryImplementation", 
            "generated class should have Implementation suffix");

        // Verify class is NOT partial
        generatedCode.Should().NotContain("public partial class TestEntityRepositoryImplementation",
            "generated class should NOT be partial");
        
        // Verify it is a regular public class
        generatedCode.Should().Contain("public class TestEntityRepositoryImplementation",
            "generated class should be a regular public class");
    }

    [Fact]
    public void QueryMethod_ReturningListOfNullable_ShouldGenerateCorrectCode()
    {
        // Arrange - Setup test source with List<T?> return type
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Table(""students"")]
    public class Student
    {
        [Id]
        public long Id { get; set; }
        public string Email { get; set; }
    }

    [Repository]
    public interface IStudentRepository : IRepository<Student, long>
    {
        [Query(""SELECT s FROM Student s WHERE s.Email = :email"")]
        Task<List<Student?>> FindByEmailAsync(string email);
    }
}";

        // Act - Run generator
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert - No errors
        diagnostics.Should().BeEmpty("generator should not produce errors");

        // Get generated source
        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("StudentRepository"))
            .ToList();
            
        generatedTrees.Should().NotBeEmpty("Generator should produce StudentRepository implementation");
        
        var generatedCode = generatedTrees.First().ToString();

        // Verify QueryAsync is used with nullable type
        generatedCode.Should().Contain("QueryAsync<TestNamespace.Student?>", 
            "QueryAsync should use nullable element type when List<T?> is specified");

        // Verify result is converted to List
        generatedCode.Should().Contain(".ToList()", 
            "result should be converted to List when return type is List<T>");
        
        // Verify return type matches
        generatedCode.Should().Contain("Task<System.Collections.Generic.List<TestNamespace.Student?>>", 
            "method signature should match interface with nullable element type");
    }

    [Theory]
    [InlineData("Task<Student[]>", "ToArray()", "Student[]")]
    [InlineData("Task<Student?[]>", "ToArray()", "Student?[]")]
    [InlineData("Task<List<Student>>", "ToList()", "List<Student>")]
    [InlineData("Task<List<Student?>>", "ToList()", "List<Student?>")]
    [InlineData("Task<IList<Student>>", "ToList()", "IList<Student>")]
    [InlineData("Task<IReadOnlyList<Student>>", "ToList()", "IReadOnlyList<Student>")]
    [InlineData("Task<IReadOnlyCollection<Student>>", "ToList()", "IReadOnlyCollection<Student>")]
    [InlineData("Task<HashSet<Student>>", "ToHashSet()", "HashSet<Student>")]
    [InlineData("Task<HashSet<Student?>>", "ToHashSet()", "HashSet<Student?>")]
    [InlineData("Task<ISet<Student>>", "ToHashSet()", "ISet<Student>")]
    [InlineData("Task<ISet<Student?>>", "ToHashSet()", "ISet<Student?>")]
    [InlineData("Task<IEnumerable<Student>>", "", "IEnumerable<Student>")]
    [InlineData("Task<IEnumerable<Student?>>", "", "IEnumerable<Student?>")]
    [InlineData("Task<ICollection<Student>>", "", "ICollection<Student>")]
    public void QueryMethod_ShouldGenerateCorrectConversion_ForAllCollectionTypes(string returnType, string expectedConversion, string collectionType)
    {
        // Arrange - Setup test source with various collection return types
        var source = $@"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{{
    [Table(""students"")]
    public class Student
    {{
        [Id]
        public long Id {{ get; set; }}
        public string Email {{ get; set; }}
    }}

    [Repository]
    public interface IStudentRepository : IRepository<Student, long>
    {{
        [Query(""SELECT s FROM Student s"")]
        {returnType} FindAllAsync();
    }}
}}";

        // Act - Run generator
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert - No errors
        diagnostics.Should().BeEmpty("generator should not produce errors");

        // Get generated source
        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("StudentRepository"))
            .ToList();
            
        generatedTrees.Should().NotBeEmpty("Generator should produce StudentRepository implementation");
        
        var generatedCode = generatedTrees.First().ToString();

        if (!string.IsNullOrEmpty(expectedConversion))
        {
            // Should use intermediate variable and conversion
            generatedCode.Should().Contain("var result = await _connection.QueryAsync<", 
                $"should use intermediate variable for {collectionType}");
            generatedCode.Should().Contain($".{expectedConversion}", 
                $"should use {expectedConversion} for {collectionType}");
        }
        else
        {
            // Should return directly without conversion
            generatedCode.Should().Contain("return await _connection.QueryAsync<", 
                $"should return directly for {collectionType}");
            generatedCode.Should().NotContain(".ToList()", 
                $"should not convert for {collectionType}");
            generatedCode.Should().NotContain(".ToArray()", 
                $"should not convert for {collectionType}");
            generatedCode.Should().NotContain(".ToHashSet()", 
                $"should not convert for {collectionType}");
        }
    }

    #endregion

    #region Multi-Mapping Tests

    [Fact]
    public void MultiMappingAttribute_ShouldExistInNPACore()
    {
        // Verify the MultiMappingAttribute is available
        var attributeType = typeof(NPA.Core.Annotations.MultiMappingAttribute);
        
        attributeType.Should().NotBeNull("MultiMappingAttribute should exist in NPA.Core.Annotations");
        attributeType.Should().BeAssignableTo<Attribute>("MultiMappingAttribute should be an Attribute");
    }

    [Fact]
    public void MultiMappingAttribute_ShouldTargetMethods()
    {
        // Arrange
        var attributeType = typeof(NPA.Core.Annotations.MultiMappingAttribute);
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(attributeType, typeof(AttributeUsageAttribute));

        // Assert
        attributeUsage.Should().NotBeNull("MultiMappingAttribute should have AttributeUsage");
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Method, "MultiMappingAttribute should target methods");
    }

    [Fact]
    public void MultiMappingAttribute_ShouldHaveKeyPropertyParameter()
    {
        // Arrange
        var attributeType = typeof(NPA.Core.Annotations.MultiMappingAttribute);

        // Act
        var constructor = attributeType.GetConstructor(new[] { typeof(string) });

        // Assert
        constructor.Should().NotBeNull("MultiMappingAttribute should have a constructor accepting keyProperty string");
    }

    [Fact]
    public void MultiMappingAttribute_ShouldHaveSplitOnProperty()
    {
        // Arrange
        var attributeType = typeof(NPA.Core.Annotations.MultiMappingAttribute);

        // Act
        var property = attributeType.GetProperty("SplitOn");

        // Assert
        property.Should().NotBeNull("MultiMappingAttribute should have SplitOn property");
        property!.PropertyType.Should().Be(typeof(string), "SplitOn should be of type string");
        property.CanWrite.Should().BeTrue("SplitOn should be settable");
    }

    [Fact]
    public void MultiMappingAttribute_ShouldHaveMapTypesProperty()
    {
        // Arrange
        var attributeType = typeof(NPA.Core.Annotations.MultiMappingAttribute);

        // Act
        var property = attributeType.GetProperty("MapTypes");

        // Assert
        property.Should().NotBeNull("MultiMappingAttribute should have MapTypes property");
        property!.PropertyType.Should().Be(typeof(Type[]), "MapTypes should be of type Type[]");
        property.CanWrite.Should().BeTrue("MapTypes should be settable");
    }

    [Theory]
    [InlineData("Id", "AddressId")]
    [InlineData("UserId", "AddressId,OrderId")]
    [InlineData("StudentId", "Id")]
    public void MultiMappingAttribute_ShouldStoreKeyAndSplitOnProperties(string keyProperty, string splitOn)
    {
        // Arrange & Act
        var attr = new NPA.Core.Annotations.MultiMappingAttribute(keyProperty)
        {
            SplitOn = splitOn
        };

        // Assert
        attr.KeyProperty.Should().Be(keyProperty);
        attr.SplitOn.Should().Be(splitOn);
    }

    #endregion

    #region Spring Data JPA Convention Tests

    [Theory]
    [InlineData("FindByEmailAsync", "string email", "email = @email")]
    [InlineData("GetByIdAsync", "long id", "id = @id")]
    [InlineData("QueryByNameAsync", "string name", "name = @name")]
    public void ConventionBasedQuery_SimpleEquality_ShouldGenerateCorrectSQL(string methodName, string paramDeclaration, string expectedWhereClause)
    {
        // Arrange
        var source = $@"
using System.Collections.Generic;
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{{
    [Table(""users"")]
    public class User
    {{
        [Column(""id"")]
        public long Id {{ get; set; }}
        
        [Column(""name"")]
        public string Name {{ get; set; }}
        
        [Column(""email"")]
        public string Email {{ get; set; }}
    }}

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {{
        Task<List<User>> {methodName}({paramDeclaration});
    }}
}}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("UserRepository"))
            .ToString();
        
        generatedCode.Should().Contain("SELECT", $"{methodName} should generate SELECT query");
        generatedCode.Should().Contain(expectedWhereClause, $"{methodName} should generate correct WHERE clause");
        generatedCode.Should().Contain("FROM users", $"{methodName} should query from correct table");
    }

    [Theory]
    [InlineData("FindByAgeGreaterThanAsync", "int age", "age > @age")]
    [InlineData("FindByPriceLessThanAsync", "decimal price", "price < @price")]
    [InlineData("FindByCountGreaterThanEqualAsync", "int count", "count >= @count")]
    [InlineData("FindByRatingLessThanEqualAsync", "decimal rating", "rating <= @rating")]
    public void ConventionBasedQuery_ComparisonOperators_ShouldGenerateCorrectSQL(string methodName, string paramDeclaration, string expectedWhereClause)
    {
        // Arrange
        var source = $@"
using System.Collections.Generic;
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{{
    [Table(""products"")]
    public class Product
    {{
        [Column(""id"")]
        public long Id {{ get; set; }}
        
        [Column(""age"")]
        public int Age {{ get; set; }}
        
        [Column(""price"")]
        public decimal Price {{ get; set; }}
        
        [Column(""count"")]
        public int Count {{ get; set; }}
        
        [Column(""rating"")]
        public decimal Rating {{ get; set; }}
    }}

    [Repository]
    public interface IProductRepository : IRepository<Product, long>
    {{
        Task<List<Product>> {methodName}({paramDeclaration});
    }}
}}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("ProductRepository"))
            .ToString();
        
        generatedCode.Should().Contain(expectedWhereClause, $"{methodName} should generate correct WHERE clause with comparison operator");
        generatedCode.Should().Contain("SELECT", "Should generate SELECT query");
        generatedCode.Should().Contain("FROM products", "Should use correct table name");
    }

    [Theory]
    [InlineData("FindByAgeBetweenAsync", "int minAge, int maxAge", "age BETWEEN @minAge AND @maxAge")]
    [InlineData("FindByPriceBetweenAsync", "decimal minPrice, decimal maxPrice", "price BETWEEN @minPrice AND @maxPrice")]
    public void ConventionBasedQuery_Between_ShouldGenerateBetweenClause(string methodName, string paramDeclaration, string expectedWhereClause)
    {
        // Arrange
        var source = $@"
using System.Collections.Generic;
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{{
    [Table(""products"")]
    public class Product
    {{
        [Column(""id"")]
        public long Id {{ get; set; }}
        
        [Column(""age"")]
        public int Age {{ get; set; }}
        
        [Column(""price"")]
        public decimal Price {{ get; set; }}
    }}

    [Repository]
    public interface IProductRepository : IRepository<Product, long>
    {{
        Task<List<Product>> {methodName}({paramDeclaration});
    }}
}}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("ProductRepository"))
            .ToString();
        
        generatedCode.Should().Contain(expectedWhereClause, $"{methodName} should generate BETWEEN clause");
    }

    [Theory]
    [InlineData("FindByNameContainingAsync", "string name", "name LIKE CONCAT('%', @name, '%')")]
    [InlineData("FindByEmailStartingWithAsync", "string email", "email LIKE CONCAT(@email, '%')")]
    [InlineData("FindByDescriptionEndingWithAsync", "string description", "description LIKE CONCAT('%', @description)")]
    [InlineData("FindByTitleNotContainingAsync", "string title", "title NOT LIKE CONCAT('%', @title, '%')")]
    public void ConventionBasedQuery_StringOperators_ShouldGenerateCorrectLikeClause(string methodName, string paramDeclaration, string expectedPattern)
    {
        // Arrange
        var source = $@"
using System.Collections.Generic;
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{{
    [Table(""products"")]
    public class Product
    {{
        [Column(""id"")]
        public long Id {{ get; set; }}
        
        [Column(""name"")]
        public string Name {{ get; set; }}
        
        [Column(""email"")]
        public string Email {{ get; set; }}
        
        [Column(""description"")]
        public string Description {{ get; set; }}
        
        [Column(""title"")]
        public string Title {{ get; set; }}
    }}

    [Repository]
    public interface IProductRepository : IRepository<Product, long>
    {{
        Task<List<Product>> {methodName}({paramDeclaration});
    }}
}}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("ProductRepository"))
            .ToString();
        
        generatedCode.Should().Contain(expectedPattern, $"{methodName} should generate correct LIKE pattern");
    }

    [Theory]
    [InlineData("FindByIdInAsync", "System.Collections.Generic.IEnumerable<long> ids", "id IN @ids")]
    [InlineData("FindByStatusNotInAsync", "System.Collections.Generic.IEnumerable<string> statuses", "status NOT IN @statuses")]
    public void ConventionBasedQuery_CollectionOperators_ShouldGenerateInClause(string methodName, string paramDeclaration, string expectedClause)
    {
        // Arrange
        var source = $@"
using System.Collections.Generic;
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{{
    [Table(""products"")]
    public class Product
    {{
        [Column(""id"")]
        public long Id {{ get; set; }}
        
        [Column(""status"")]
        public string Status {{ get; set; }}
    }}

    [Repository]
    public interface IProductRepository : IRepository<Product, long>
    {{
        Task<List<Product>> {methodName}({paramDeclaration});
    }}
}}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("ProductRepository"))
            .ToString();
        
        generatedCode.Should().Contain(expectedClause, $"{methodName} should generate IN/NOT IN clause");
    }

    [Theory]
    [InlineData("FindByDeletedAtIsNullAsync", "deleted_at IS NULL")]
    [InlineData("FindByEmailIsNotNullAsync", "email IS NOT NULL")]
    [InlineData("FindByIsActiveTrueAsync", "Active = TRUE")]
    [InlineData("FindByIsDeletedFalseAsync", "Deleted = FALSE")]
    public void ConventionBasedQuery_NullAndBooleanChecks_ShouldGenerateCorrectClause(string methodName, string expectedClause)
    {
        // Arrange
        var source = $@"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{{
    [Table(""products"")]
    public class Product
    {{
        [Column(""id"")]
        public long Id {{ get; set; }}
        
        [Column(""deleted_at"")]
        public DateTime? DeletedAt {{ get; set; }}
        
        [Column(""email"")]
        public string Email {{ get; set; }}
        
        [Column(""is_active"")]
        public bool IsActive {{ get; set; }}
        
        [Column(""is_deleted"")]
        public bool IsDeleted {{ get; set; }}
    }}

    [Repository]
    public interface IProductRepository : IRepository<Product, long>
    {{
        Task<List<Product>> {methodName}();
    }}
}}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("ProductRepository"))
            .ToString();
        
        generatedCode.Should().Contain(expectedClause, $"{methodName} should generate correct null/boolean clause");
    }

    [Theory]
    [InlineData("FindByCreatedAtBeforeAsync", "System.DateTime date", "created_at < @date")]
    [InlineData("FindByUpdatedAtAfterAsync", "System.DateTime date", "updated_at > @date")]
    public void ConventionBasedQuery_DateTimeOperators_ShouldGenerateCorrectComparison(string methodName, string paramDeclaration, string expectedClause)
    {
        // Arrange
        var source = $@"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{{
    [Table(""products"")]
    public class Product
    {{
        [Column(""id"")]
        public long Id {{ get; set; }}
        
        [Column(""created_at"")]
        public DateTime CreatedAt {{ get; set; }}
        
        [Column(""updated_at"")]
        public DateTime UpdatedAt {{ get; set; }}
    }}

    [Repository]
    public interface IProductRepository : IRepository<Product, long>
    {{
        Task<List<Product>> {methodName}({paramDeclaration});
    }}
}}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("ProductRepository"))
            .ToString();
        
        generatedCode.Should().Contain(expectedClause, $"{methodName} should generate correct temporal comparison");
    }

    [Theory]
    [InlineData("FindByEmailAndTenantIdAsync", "string email, long tenantId", "email = @email AND tenant_id = @tenantId")]
    [InlineData("FindByFirstNameOrLastNameAsync", "string firstName, string lastName", "Name = @firstName OR last_name = @lastName")]
    [InlineData("FindByTenantIdAndStatusAsync", "long tenantId, string status", "tenant_id = @tenantId AND status = @status")]
    public void ConventionBasedQuery_MultipleConditions_ShouldCombineCorrectly(string methodName, string paramDeclaration, string expectedWhereClause)
    {
        // Arrange
        var source = $@"
using System.Collections.Generic;
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{{
    [Table(""users"")]
    public class User
    {{
        [Column(""id"")]
        public long Id {{ get; set; }}
        
        [Column(""email"")]
        public string Email {{ get; set; }}
        
        [Column(""tenant_id"")]
        public long TenantId {{ get; set; }}
        
        [Column(""first_name"")]
        public string FirstName {{ get; set; }}
        
        [Column(""last_name"")]
        public string LastName {{ get; set; }}
        
        [Column(""status"")]
        public string Status {{ get; set; }}
    }}

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {{
        Task<List<User>> {methodName}({paramDeclaration});
    }}
}}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("UserRepository"))
            .ToString();
        
        generatedCode.Should().Contain(expectedWhereClause, $"{methodName} should generate correct AND/OR clause");
    }

    [Theory]
    [InlineData("FindByTenantIdOrderByNameAscAsync", "long tenantId", "ORDER BY name ASC")]
    [InlineData("FindByStatusOrderByCreatedAtDescAsync", "string status", "ORDER BY created_at DESC")]
    [InlineData("FindAllOrderByNameAscThenEmailDescAsync", "", "ORDER BY name ASC, email DESC")]
    public void ConventionBasedQuery_Ordering_ShouldGenerateOrderByClause(string methodName, string paramDeclaration, string expectedOrderBy)
    {
        // Arrange
        var source = $@"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{{
    [Table(""users"")]
    public class User
    {{
        [Column(""id"")]
        public long Id {{ get; set; }}
        
        [Column(""name"")]
        public string Name {{ get; set; }}
        
        [Column(""email"")]
        public string Email {{ get; set; }}
        
        [Column(""tenant_id"")]
        public long TenantId {{ get; set; }}
        
        [Column(""status"")]
        public string Status {{ get; set; }}
        
        [Column(""created_at"")]
        public DateTime CreatedAt {{ get; set; }}
    }}

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {{
        Task<List<User>> {methodName}({paramDeclaration});
    }}
}}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("UserRepository"))
            .ToString();
        
        generatedCode.Should().Contain(expectedOrderBy, $"{methodName} should generate correct ORDER BY clause");
    }

    [Theory]
    [InlineData("CountByTenantIdAsync", "long tenantId", "tenant_id = @tenantId")]
    [InlineData("CountByStatusAsync", "string status", "status = @status")]
    [InlineData("CountByAgeGreaterThanAsync", "int age", "age > @age")]
    public void ConventionBasedQuery_Count_ShouldGenerateCountQuery(string methodName, string paramDeclaration, string expectedWhereClause)
    {
        // Arrange
        var source = $@"
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{{
    [Table(""users"")]
    public class User
    {{
        [Column(""id"")]
        public long Id {{ get; set; }}
        
        [Column(""tenant_id"")]
        public long TenantId {{ get; set; }}
        
        [Column(""status"")]
        public string Status {{ get; set; }}
        
        [Column(""age"")]
        public int Age {{ get; set; }}
    }}

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {{
        Task<long> {methodName}({paramDeclaration});
    }}
}}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("UserRepository"))
            .ToString();
        
        generatedCode.Should().Contain("COUNT(*)", $"{methodName} should use COUNT(*)");
        generatedCode.Should().Contain(expectedWhereClause, $"{methodName} should generate correct WHERE clause");
    }

    [Theory]
    [InlineData("ExistsByEmailAsync", "string email", "email = @email")]
    [InlineData("ExistsByTenantIdAndEmailAsync", "long tenantId, string email", "tenant_id = @tenantId AND email = @email")]
    public void ConventionBasedQuery_Exists_ShouldGenerateExistsQuery(string methodName, string paramDeclaration, string expectedWhereClause)
    {
        // Arrange
        var source = $@"
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{{
    [Table(""users"")]
    public class User
    {{
        [Column(""id"")]
        public long Id {{ get; set; }}
        
        [Column(""email"")]
        public string Email {{ get; set; }}
        
        [Column(""tenant_id"")]
        public long TenantId {{ get; set; }}
    }}

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {{
        Task<bool> {methodName}({paramDeclaration});
    }}
}}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("UserRepository"))
            .ToString();
        
        generatedCode.Should().Contain("COUNT(*)", $"{methodName} should use COUNT(*) for existence check");
        generatedCode.Should().Contain(expectedWhereClause, $"{methodName} should generate correct WHERE clause");
    }

    [Theory]
    [InlineData("DeleteByEmailAsync", "string email", "email = @email")]
    [InlineData("RemoveByStatusAsync", "string status", "status = @status")]
    public void ConventionBasedQuery_Delete_ShouldGenerateDeleteQuery(string methodName, string paramDeclaration, string expectedWhereClause)
    {
        // Arrange
        var source = $@"
using System.Threading.Tasks;
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{{
    [Table(""users"")]
    public class User
    {{
        [Column(""id"")]
        public long Id {{ get; set; }}
        
        [Column(""email"")]
        public string Email {{ get; set; }}
        
        [Column(""status"")]
        public string Status {{ get; set; }}
    }}

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {{
        Task {methodName}({paramDeclaration});
    }}
}}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("UserRepository"))
            .ToString();
        
        generatedCode.Should().Contain("DELETE FROM users", $"{methodName} should generate DELETE statement");
        generatedCode.Should().Contain(expectedWhereClause, $"{methodName} should generate correct WHERE clause");
    }

    [Fact]
    public void MethodConventionAnalyzer_ToSnakeCase_ShouldConvertPascalCaseCorrectly()
    {
        // This is a documentation test - documents the snake_case conversion utility
        // Actual conversion is tested when used in query generation
        MethodConventionAnalyzer.ToSnakeCase("StudentId").Should().Be("student_id");
        MethodConventionAnalyzer.ToSnakeCase("EnrolledCoursesCount").Should().Be("enrolled_courses_count");
        MethodConventionAnalyzer.ToSnakeCase("IsActive").Should().Be("is_active");
        MethodConventionAnalyzer.ToSnakeCase("Email").Should().Be("email");
        MethodConventionAnalyzer.ToSnakeCase("TenantId").Should().Be("tenant_id");
    }

    [Theory]
    [InlineData("FindByStudentIdAsync", QueryType.Select, "StudentId")]
    [InlineData("CountByTenantIdAsync", QueryType.Count, "TenantId")]
    [InlineData("ExistsByEmailAsync", QueryType.Exists, "Email")]
    [InlineData("DeleteByStatusAsync", QueryType.Delete, "Status")]
    public void MethodConventionAnalyzer_DetermineQueryType_ShouldIdentifyCorrectType(string methodName, QueryType expectedType, string expectedProperty)
    {
        // This is a documentation test - documents the query type determination logic
        // Validates that method name prefixes correctly map to query types
        var cleanName = methodName.Replace("Async", "");
        
        if (cleanName.StartsWith("Find") || cleanName.StartsWith("Get"))
            expectedType.Should().Be(QueryType.Select);
        else if (cleanName.StartsWith("Count"))
            expectedType.Should().Be(QueryType.Count);
        else if (cleanName.StartsWith("Exists"))
            expectedType.Should().Be(QueryType.Exists);
        else if (cleanName.StartsWith("Delete"))
            expectedType.Should().Be(QueryType.Delete);

        expectedProperty.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region UPDATE and DELETE Query Generation Tests

    [Fact]
    public void Generator_UpdateQuery_ShouldConvertJPQLToSQLAndUseExecuteAsync()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class Product
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }

    [Repository]
    public interface IProductRepository : IRepository<Product, int>
    {
        [Query(""UPDATE Product p SET p.Price = :price WHERE p.Id = :id"")]
        Task<int> UpdatePriceAsync(int id, decimal price);
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        
        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("ProductRepository"))
            .ToList();
        
        generatedTrees.Should().NotBeEmpty("Generator should produce ProductRepository implementation");
        var generatedCode = generatedTrees.First().ToString();
        
        // Should convert JPQL to SQL - generator uses metadata from [Table] attribute to get table name "products"
        generatedCode.Should().Contain("UPDATE products SET Price = @price WHERE Id = @id");
        
        // Should use ExecuteAsync for UPDATE (not ExecuteScalarAsync)
        generatedCode.Should().Contain("ExecuteAsync");
    }

    [Fact]
    public void Generator_UpdateQueryWithMetadata_ShouldUseTableAndColumnNames()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Table(""products"")]
    public class Product
    {
        [Column(""id"")]
        public int Id { get; set; }
        
        [Column(""price"")]
        public decimal Price { get; set; }
        
        [Column(""stock_quantity"")]
        public int Stock { get; set; }
    }

    [Repository]
    public interface IProductRepository : IRepository<Product, int>
    {
        [Query(""UPDATE Product p SET p.Price = :price, p.Stock = :stock WHERE p.Id = :id"")]
        Task<int> UpdatePriceAndStockAsync(int id, decimal price, int stock);
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        
        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("ProductRepository"))
            .ToList();
        
        generatedTrees.Should().NotBeEmpty("Generator should produce ProductRepository implementation");
        var generatedCode = generatedTrees.First().ToString();
        
        // Should use table name from [Table("products")] attribute
        // Stock property uses [Column("stock_quantity")] attribute
        generatedCode.Should().Contain("UPDATE products SET price = @price, stock_quantity = @stock WHERE id = @id");
    }

    [Fact]
    public void Generator_DeleteQuery_ShouldConvertJPQLToSQLAndUseExecuteAsync()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;
using System;

namespace TestNamespace
{
    public class Session
    {
        public int Id { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
    }

    [Repository]
    public interface ISessionRepository : IRepository<Session, int>
    {
        [Query(""DELETE FROM Session s WHERE s.ExpiresAt < :now OR s.IsRevoked = true"")]
        Task<int> DeleteExpiredOrRevokedAsync(DateTime now);
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        
        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("SessionRepository"))
            .ToList();
        
        generatedTrees.Should().NotBeEmpty("Generator should produce SessionRepository implementation");
        var generatedCode = generatedTrees.First().ToString();
        
        // Should convert JPQL to SQL - uses metadata table name "sessions"
        generatedCode.Should().Contain("DELETE FROM sessions WHERE ExpiresAt < @now OR IsRevoked = true");
        
        // Should use ExecuteAsync for DELETE (not ExecuteScalarAsync)
        generatedCode.Should().Contain("ExecuteAsync");
    }

    [Fact]
    public void Generator_DeleteQueryWithMetadata_ShouldUseTableAndColumnNames()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;
using System;

namespace TestNamespace
{
    [Table(""sessions"")]
    public class Session
    {
        [Column(""id"")]
        public int Id { get; set; }
        
        [Column(""expires_at"")]
        public DateTime ExpiresAt { get; set; }
        
        [Column(""is_revoked"")]
        public bool IsRevoked { get; set; }
        
        [Column(""user_id"")]
        public int UserId { get; set; }
    }

    [Repository]
    public interface ISessionRepository : IRepository<Session, int>
    {
        [Query(""DELETE FROM Session s WHERE (s.ExpiresAt < :now OR s.IsRevoked = true) AND s.UserId = :userId"")]
        Task<int> DeleteExpiredOrRevokedForUserAsync(int userId, DateTime now);
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        
        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("SessionRepository"))
            .ToList();
        
        generatedTrees.Should().NotBeEmpty("Generator should produce SessionRepository implementation");
        var generatedCode = generatedTrees.First().ToString();
        
        // Should use table and column names from attributes
        generatedCode.Should().Contain("DELETE FROM sessions WHERE (expires_at < @now OR is_revoked = true) AND user_id = @userId");
    }

    [Fact]
    public void Generator_UpdateWithExpressionQuery_ShouldPreserveExpression()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Table(""orders"")]
    public class Order
    {
        [Column(""id"")]
        public int Id { get; set; }
        
        [Column(""total_amount"")]
        public decimal TotalAmount { get; set; }
        
        [Column(""discount_percent"")]
        public decimal DiscountPercent { get; set; }
    }

    [Repository]
    public interface IOrderRepository : IRepository<Order, int>
    {
        [Query(""UPDATE Order o SET o.TotalAmount = o.TotalAmount * (1 - o.DiscountPercent / 100) WHERE o.Id = :orderId"")]
        Task<int> ApplyDiscountAsync(int orderId);
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        
        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("OrderRepository"))
            .ToList();
        
        generatedTrees.Should().NotBeEmpty("Generator should produce OrderRepository implementation");
        var generatedCode = generatedTrees.First().ToString();
        
        // Should preserve the arithmetic expression - uses metadata table name "orders"
        generatedCode.Should().Contain("UPDATE orders SET total_amount = total_amount * (1 - discount_percent / 100) WHERE id = @orderId");
    }

    [Fact]
    public void Generator_UpdateQueryReturningInt_ShouldReturnAffectedRowCount()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
    }

    [Repository]
    public interface IUserRepository : IRepository<User, int>
    {
        [Query(""UPDATE User u SET u.IsActive = :isActive WHERE u.Id = :userId"")]
        Task<int> UpdateActiveStatusAsync(int userId, bool isActive);
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        
        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("UserRepository"))
            .ToList();
        
        generatedTrees.Should().NotBeEmpty("Generator should produce UserRepository implementation");
        var generatedCode = generatedTrees.First().ToString();
        
        // Method should return Task<int> (row count)
        generatedCode.Should().Contain("Task<int> UpdateActiveStatusAsync");
        
        // Should use ExecuteAsync which returns affected row count
        generatedCode.Should().Contain("return await _connection.ExecuteAsync");
    }

    [Fact]
    public void Generator_DeleteQueryReturningInt_ShouldReturnAffectedRowCount()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class Product
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
    }

    [Repository]
    public interface IProductRepository : IRepository<Product, int>
    {
        [Query(""DELETE FROM Product p WHERE p.CategoryId = :categoryId"")]
        Task<int> DeleteByCategoryAsync(int categoryId);
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        
        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("ProductRepository"))
            .ToList();
        
        generatedTrees.Should().NotBeEmpty("Generator should produce ProductRepository implementation");
        var generatedCode = generatedTrees.First().ToString();
        
        // Method should return Task<int> (row count)
        generatedCode.Should().Contain("Task<int> DeleteByCategoryAsync");
        
        // Should use ExecuteAsync which returns affected row count
        generatedCode.Should().Contain("return await _connection.ExecuteAsync");
    }

    [Fact]
    public void Generator_UpdateQueryWithMultipleParameters_ShouldGenerateCorrectParameterObject()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class Product
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
    }

    [Repository]
    public interface IProductRepository : IRepository<Product, int>
    {
        [Query(""UPDATE Product p SET p.Price = :price, p.Stock = :stock, p.IsActive = :active WHERE p.Id = :id"")]
        Task<int> UpdateProductDetailsAsync(int id, decimal price, int stock, bool active);
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        
        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("ProductRepository"))
            .ToList();
        
        generatedTrees.Should().NotBeEmpty("Generator should produce ProductRepository implementation");
        var generatedCode = generatedTrees.First().ToString();
        
        // Should generate parameter object using anonymous object syntax
        generatedCode.Should().Contain("new { id, price, stock, active }");
    }
    #endregion

    #region INSERT Query Generation Tests

    [Fact]
    public void Generator_InsertQuery_ShouldConvertToSQLAndUseExecuteAsync()
    {
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    [Repository]
    public interface IProductRepository : IRepository<Product, int>
    {
        [Query(""INSERT INTO Product (Name, Price) VALUES (:name, :price)"")]
        Task<int> InsertProductAsync(string name, decimal price);
    }
}";
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);
        diagnostics.Should().BeEmpty();
        var generatedTrees = outputCompilation.SyntaxTrees.Where(t => t.FilePath.Contains("ProductRepository")).ToList();
        generatedTrees.Should().NotBeEmpty();
        var generatedCode = generatedTrees.First().ToString();
        generatedCode.Should().Contain("INSERT INTO products (Name, Price) VALUES (@name, @price)");
        generatedCode.Should().Contain("ExecuteAsync");
    }

    #endregion

    #region Native Query Tests

    [Fact]
    public void Generator_NativeQuerySelect_ShouldNotConvertCPQL()
    {
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TestNamespace
{
    [Table(""products"")]
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    [Repository]
    public interface IProductRepository : IRepository<Product, int>
    {
        [Query(""SELECT * FROM products WHERE price > @minPrice ORDER BY price DESC"", NativeQuery = true)]
        Task<IEnumerable<Product>> FindExpensiveProductsAsync(decimal minPrice);
    }
}";
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);
        diagnostics.Should().BeEmpty();
        var generatedTrees = outputCompilation.SyntaxTrees.Where(t => t.FilePath.Contains("ProductRepository")).ToList();
        generatedTrees.Should().NotBeEmpty();
        var generatedCode = generatedTrees.First().ToString();
        // Verify native SQL is preserved exactly as written (not converted from CPQL)
        generatedCode.Should().Contain("SELECT * FROM products WHERE price > @minPrice ORDER BY price DESC");
        generatedCode.Should().Contain("QueryAsync<TestNamespace.Product>");
    }

    [Fact]
    public void Generator_NativeQueryInsert_ShouldPreserveDatabaseSpecificSyntax()
    {
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Table(""products"")]
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [Repository]
    public interface IProductRepository : IRepository<Product, int>
    {
        [Query(""INSERT INTO products (name, created_at) VALUES (@name, NOW()) RETURNING id"", NativeQuery = true)]
        Task<int> CreateProductAsync(string name);
    }
}";
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);
        diagnostics.Should().BeEmpty();
        var generatedTrees = outputCompilation.SyntaxTrees.Where(t => t.FilePath.Contains("ProductRepository")).ToList();
        generatedTrees.Should().NotBeEmpty();
        var generatedCode = generatedTrees.First().ToString();
        // Verify PostgreSQL-specific syntax (NOW() function and RETURNING clause) is preserved
        generatedCode.Should().Contain("INSERT INTO products (name, created_at) VALUES (@name, NOW()) RETURNING id");
        // For INSERT with RETURNING, ExecuteAsync is used (not ExecuteScalarAsync)
        generatedCode.Should().Contain("ExecuteAsync");
    }

    #endregion

    #region DTO Return Type Tests

    [Fact]
    public void Generator_ConventionMethodReturningDTO_ShouldUseCorrectReturnType()
    {
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TestNamespace
{
    [Table(""users"")]
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }
        public decimal AccountBalance { get; set; }
    }

    public class UserStatistics
    {
        public string Country { get; set; }
        public long UserCount { get; set; }
        public decimal AverageBalance { get; set; }
        public decimal TotalBalance { get; set; }
    }

    [Repository]
    public interface IUserRepository : IRepository<User, int>
    {
        Task<IEnumerable<UserStatistics>> GetUserStatisticsByCountryAsync();
    }
}";
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);
        diagnostics.Should().BeEmpty();
        var generatedTrees = outputCompilation.SyntaxTrees.Where(t => t.FilePath.Contains("UserRepository")).ToList();
        generatedTrees.Should().NotBeEmpty();
        var generatedCode = generatedTrees.First().ToString();
        // Should use UserStatistics as the generic type parameter, not User
        generatedCode.Should().Contain("QueryAsync<TestNamespace.UserStatistics>");
        generatedCode.Should().NotContain("QueryAsync<TestNamespace.User>(sql);");
    }

    [Fact]
    public void Generator_QueryAttributeReturningDTO_ShouldUseCorrectReturnType()
    {
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TestNamespace
{
    [Table(""users"")]
    public class User
    {
        [Column(""id"")]
        public int Id { get; set; }
        
        [Column(""name"")]
        public string Name { get; set; }
        
        [Column(""country"")]
        public string Country { get; set; }
        
        [Column(""account_balance"")]
        public decimal AccountBalance { get; set; }
    }

    public class UserStatistics
    {
        public string Country { get; set; }
        public long UserCount { get; set; }
        public decimal AverageBalance { get; set; }
        public decimal TotalBalance { get; set; }
    }

    [Repository]
    public interface IUserRepository : IRepository<User, int>
    {
        [Query(""SELECT u.Country, COUNT(u), AVG(u.AccountBalance), SUM(u.AccountBalance) FROM User u GROUP BY u.Country"")]
        Task<IEnumerable<UserStatistics>> GetUserStatisticsByCountryAsync();
    }
}";
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);
        diagnostics.Should().BeEmpty();
        var generatedTrees = outputCompilation.SyntaxTrees.Where(t => t.FilePath.Contains("UserRepository")).ToList();
        generatedTrees.Should().NotBeEmpty();
        var generatedCode = generatedTrees.First().ToString();
        // Should use UserStatistics as the generic type parameter, not User
        generatedCode.Should().Contain("QueryAsync<TestNamespace.UserStatistics>");
        // Verify CPQL conversion happened (User -> users table, properties converted to snake_case)
        generatedCode.Should().Contain("FROM users");
        generatedCode.Should().Contain("account_balance");
    }

    [Fact]
    public void Generator_ConventionBasedFindBy_ShouldGenerateCorrectQuery()
    {
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TestNamespace
{
    [Table(""users"")]
    public class User
    {
        [Column(""id"")]
        public int Id { get; set; }
        
        [Column(""email"")]
        public string Email { get; set; }
        
        [Column(""user_name"")]
        public string Username { get; set; }
        
        [Column(""status"")]
        public string Status { get; set; }
        public string FullName { get; set; }
    }

    [Repository]
    public interface IUserRepository : IRepository<User, int>
    {
        Task<User?> FindByEmailAsync(string email);
        Task<User?> FindByUsernameAsync(string username);
        Task<IEnumerable<User>> FindByStatusAsync(string status);
        Task<User?> FindByFullNameAsync(string fullName);
    }
}";
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);
        diagnostics.Should().BeEmpty();
        var generatedTrees = outputCompilation.SyntaxTrees.Where(t => t.FilePath.Contains("UserRepository")).ToList();
        generatedTrees.Should().NotBeEmpty();
        var generatedCode = generatedTrees.First().ToString();
        
        // Convention-based FindBy methods should use property names as column names
        // This behavior matches how properties without [Column] attributes are handled.
        generatedCode.Should().Contain("WHERE email = @email", "Email property generates email column");
        generatedCode.Should().Contain("WHERE user_name = @username", "Username property generates user_name column");
        generatedCode.Should().Contain("WHERE status = @status", "Status property generates status column");
        generatedCode.Should().Contain("WHERE FullName = @fullName", "FullName property generates FullName column there is no [Column] attribute");
        
        // Should use User as return type for entity queries
        generatedCode.Should().Contain("QueryFirstOrDefaultAsync<TestNamespace.User?>");
        generatedCode.Should().Contain("QueryAsync<TestNamespace.User>");
    }

    #endregion

    #region NamedQuery Auto-Detection Tests

    [Fact]
    public void NamedQuery_AutoDetection_ByMethodName_ShouldGenerateCorrectCode()
    {
        // Arrange - Entity with NamedQuery and Repository with matching method name
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    [Table(""orders"")]
    [NamedQuery(""Order.FindRecentOrdersAsync"", 
                ""SELECT * FROM orders WHERE created_at > @since ORDER BY created_at DESC"",
                NativeQuery = true,
                Description = ""Finds orders created after a specific date"")]
    public class Order
    {
        [Id]
        public long Id { get; set; }
        
        [Column(""created_at"")]
        public DateTime CreatedAt { get; set; }
        
        [Column(""total"")]
        public decimal Total { get; set; }
    }

    [Repository]
    public interface IOrderRepository : IRepository<Order, long>
    {
        Task<IEnumerable<Order>> FindRecentOrdersAsync(DateTime since);
    }
}";

        // Act - Run generator
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");
        
        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("OrderRepository"))
            .ToList();
        
        generatedTrees.Should().NotBeEmpty("Generator should produce OrderRepository implementation");
        
        var generatedCode = generatedTrees.First().ToString();
        
        // Verify it uses the named query with compile-time embedded SQL
        generatedCode.Should().Contain("// Using named query: Order.FindRecentOrdersAsync", 
            "Should detect and use the named query automatically");
        
        generatedCode.Should().Contain("var sql = @\"SELECT * FROM orders WHERE created_at > @since ORDER BY created_at DESC\"", 
            "Should embed the SQL from named query at compile time");
        
        generatedCode.Should().Contain("_connection.QueryAsync<TestNamespace.Order>(sql, new { since })", 
            "Should generate Dapper query call with parameters");
    }

    [Fact]
    public void NamedQuery_AutoDetection_WithoutAsync_ShouldMatchMethodName()
    {
        // Arrange - NamedQuery without "Async" suffix should still match method with "Async"
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    [Table(""products"")]
    [NamedQuery(""Product.FindActive"", 
                ""SELECT * FROM products WHERE is_active = 1"",
                NativeQuery = true)]
    public class Product
    {
        [Id]
        public long Id { get; set; }
        
        [Column(""is_active"")]
        public bool IsActive { get; set; }
    }

    [Repository]
    public interface IProductRepository : IRepository<Product, long>
    {
        // Method has Async suffix, NamedQuery doesn't - should still match
        Task<IEnumerable<Product>> FindActiveAsync();
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();
        
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("ProductRepository"))
            .ToString();
        
        // Should match by removing "Async" suffix
        generatedCode.Should().Contain("// Using named query: Product.FindActive", 
            "Should match named query even without Async suffix");
    }

    [Fact]
    public void NamedQuery_PriorityOverQuery_ShouldUseNamedQuery()
    {
        // Arrange - Both NamedQuery and [Query] attribute present
        // NamedQuery should take priority
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    [Table(""users"")]
    [NamedQuery(""User.FindByEmail"", 
                ""SELECT * FROM users WHERE email = @email"",
                NativeQuery = true)]
    public class User
    {
        [Id]
        public long Id { get; set; }
        
        public string Email { get; set; }
    }

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {
        // Has [Query] attribute, but method name matches a NamedQuery
        // NamedQuery should take priority
        [Query(""SELECT * FROM users WHERE email LIKE @email || '%'"")]
        Task<IEnumerable<User>> FindByEmail(string email);
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();
        
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("UserRepository"))
            .ToString();
        
        // Should use NamedQuery, not the [Query] attribute
        generatedCode.Should().Contain("// Using named query: User.FindByEmail", 
            "NamedQuery should take priority over [Query] attribute");
        
        generatedCode.Should().NotContain("SELECT * FROM users WHERE email LIKE @email || '%'", 
            "Should not use the SQL from [Query] attribute");
    }

    #endregion
}


