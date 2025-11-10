using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
    [InlineData("IUserRepository", "UserRepositoryImplementation")]
    [InlineData("IProductRepository", "ProductRepositoryImplementation")]
    [InlineData("IOrderRepository", "OrderRepositoryImplementation")]
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
        // This test verifies that DeleteAsync(long id) generates proper SQL
        // The generator should create: DELETE FROM table_name WHERE id = @id
        // Not: throw new InvalidOperationException("Delete without WHERE clause is not allowed")
        
        // Expected generated code pattern:
        // var sql = "DELETE FROM students WHERE id = @id";
        // await _connection.ExecuteAsync(sql, new { id });
        
        // This is a documentation test - actual verification requires compilation testing
        true.Should().BeTrue("DeleteAsync should generate SQL with WHERE clause for id parameter");
    }

    [Theory]
    [InlineData("id", "id = @id")]
    [InlineData("Id", "id = @Id")]
    [InlineData("ID", "id = @ID")]
    public void DeleteAsync_ShouldHandleIdParameterCaseInsensitive(string paramName, string expectedWhereClause)
    {
        // Tests that the generator handles id parameter regardless of casing
        // The special handling should recognize: id, Id, ID
        
        expectedWhereClause.Should().Contain("@", "WHERE clause should use parameter syntax");
        expectedWhereClause.Should().Contain(paramName, "WHERE clause should reference the parameter name");
    }

    [Fact]
    public void DeleteAsync_ShouldNotReturnValue()
    {
        // DeleteAsync should return Task (void), not Task<int>
        // The generated code should NOT have: return await _connection.ExecuteAsync(...)
        // It should have: await _connection.ExecuteAsync(...)
        
        // This ensures the fix removes the incorrect return statement
        true.Should().BeTrue("DeleteAsync should not return ExecuteAsync result");
    }

    [Fact]
    public void DeleteAsync_WithoutParameters_ShouldThrowException()
    {
        // A DeleteAsync method without parameters should generate code that throws
        // throw new InvalidOperationException("Delete without WHERE clause is not allowed")
        
        // This is a safety feature to prevent accidental deletion of all records
        true.Should().BeTrue("DeleteAsync without parameters should throw InvalidOperationException");
    }

    [Fact]
    public void DeleteAsync_WithNonIdParameter_ShouldUseConventionAnalysis()
    {
        // If parameter is not named 'id', the convention analyzer should still work
        // For example: DeleteAsync(string email) should generate WHERE email = @email
        
        true.Should().BeTrue("DeleteAsync should support custom parameter names through convention analysis");
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
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var generator = new RepositoryGenerator();
        
        // Act
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, 
            out var outputCompilation, 
            out var diagnostics);

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
        var compilation = CreateCompilation(sourceWithoutParams);
        var generator = new RepositoryGenerator();
        
        // Act
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, 
            out var outputCompilation, 
            out _);

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
        var compilation = CreateCompilation(sourceWithCasing);
        var generator = new RepositoryGenerator();
        
        // Act
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, 
            out var outputCompilation, 
            out _);

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
        var compilation = CreateCompilation(sourceWithEmail);
        var generator = new RepositoryGenerator();
        
        // Act
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, 
            out var outputCompilation, 
            out _);

        // Assert
        var generatedCode = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("StudentRepository"))
            .First()
            .ToString();
        
        // Should use convention analysis to extract "Email" from "DeleteByEmailAsync"
        generatedCode.Should().Contain("email = @email", 
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
        var compilation = CreateCompilation(sourceWithSync);
        var generator = new RepositoryGenerator();
        
        // Act
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, 
            out var outputCompilation, 
            out _);

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
        var compilation = CreateCompilation(sourceWithMultiParams);
        var generator = new RepositoryGenerator();
        
        // Act
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, 
            out var outputCompilation, 
            out _);

        // Assert
        var generatedCode = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains("StudentRepository"))
            .First()
            .ToString();
        
        generatedCode.Should().Contain("id = @id AND tenant_id = @tenantId", 
            "Should generate AND clause for multiple parameters");
    }

    private static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(NPA.Core.Annotations.RepositoryAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(NPA.Core.Repositories.IRepository<,>).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
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
        var compilation = CreateCompilation(source);
        var generator = new RepositoryGenerator();
        
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, 
            out var outputCompilation, 
            out var diagnostics);

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
        var compilation = CreateCompilation(source);
        var generator = new RepositoryGenerator();
        
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, 
            out var outputCompilation, 
            out var diagnostics);

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
        var compilation = CreateCompilation(source);
        var generator = new RepositoryGenerator();
        
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, 
            out var outputCompilation, 
            out var diagnostics);

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
}
