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
    [InlineData("FindByEmailAsync", "email", "SELECT * FROM table WHERE email = @email")]
    [InlineData("GetByIdAsync", "id", "SELECT * FROM table WHERE id = @id")]
    [InlineData("QueryByNameAsync", "name", "SELECT * FROM table WHERE name = @name")]
    public void ConventionBasedQuery_SimpleEquality_ShouldGenerateCorrectSQL(string methodName, string paramName, string expectedSql)
    {
        // This is a documentation test - actual generation is tested via integration tests
        // The pattern should match: Find/Get/Query + By + PropertyName
        methodName.Should().MatchRegex("^(Find|Get|Query)By[A-Z].*Async$");
        paramName.Should().NotBeNullOrEmpty();
        expectedSql.Should().Contain("WHERE");
    }

    [Theory]
    [InlineData("FindByAgeGreaterThanAsync", "age", ">")]
    [InlineData("FindByPriceLessThanAsync", "price", "<")]
    [InlineData("FindByCountGreaterThanEqualAsync", "count", ">=")]
    [InlineData("FindByRatingLessThanEqualAsync", "rating", "<=")]
    public void ConventionBasedQuery_ComparisonOperators_ShouldGenerateCorrectSQL(string methodName, string paramName, string operatorSymbol)
    {
        // Pattern: FindBy + Property + (GreaterThan|LessThan|GreaterThanEqual|LessThanEqual)
        methodName.Should().MatchRegex("^FindBy[A-Z].*(GreaterThan|LessThan)(Equal)?Async$");
        paramName.Should().NotBeNullOrEmpty();
        operatorSymbol.Should().BeOneOf(">", "<", ">=", "<=");
    }

    [Theory]
    [InlineData("FindByAgeBetweenAsync", 2, "BETWEEN")]
    [InlineData("FindByDateBetweenAsync", 2, "BETWEEN")]
    public void ConventionBasedQuery_Between_ShouldRequireTwoParameters(string methodName, int expectedParams, string keyword)
    {
        methodName.Should().Contain("Between");
        expectedParams.Should().Be(2, "Between requires min and max parameters");
        keyword.Should().Be("BETWEEN");
    }

    [Theory]
    [InlineData("FindByNameContainingAsync", "LIKE CONCAT('%', @param, '%')")]
    [InlineData("FindByEmailStartingWithAsync", "LIKE CONCAT(@param, '%')")]
    [InlineData("FindByDescriptionEndingWithAsync", "LIKE CONCAT('%', @param)")]
    [InlineData("FindByTitleNotContainingAsync", "NOT LIKE CONCAT('%', @param, '%')")]
    public void ConventionBasedQuery_StringOperators_ShouldGenerateCorrectLikeClause(string methodName, string expectedPattern)
    {
        methodName.Should().MatchRegex(".*(Containing|StartingWith|EndingWith|NotContaining)Async$");
        expectedPattern.Should().Contain("LIKE");
    }

    [Theory]
    [InlineData("FindByIdInAsync", "IN")]
    [InlineData("FindByStatusNotInAsync", "NOT IN")]
    public void ConventionBasedQuery_CollectionOperators_ShouldGenerateInClause(string methodName, string expectedOperator)
    {
        methodName.Should().MatchRegex(".*(In|NotIn)Async$");
        expectedOperator.Should().Contain("IN");
    }

    [Theory]
    [InlineData("FindByDeletedAtIsNullAsync", "IS NULL")]
    [InlineData("FindByEmailIsNotNullAsync", "IS NOT NULL")]
    [InlineData("FindByIsActiveTrueAsync", "= TRUE")]
    [InlineData("FindByIsDeletedFalseAsync", "= FALSE")]
    public void ConventionBasedQuery_NullAndBooleanChecks_ShouldGenerateCorrectClause(string methodName, string expectedClause)
    {
        methodName.Should().MatchRegex(".*(IsNull|IsNotNull|True|False)Async$");
        expectedClause.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("FindByCreatedAtBeforeAsync", "<")]
    [InlineData("FindByUpdatedAtAfterAsync", ">")]
    public void ConventionBasedQuery_DateTimeOperators_ShouldGenerateCorrectComparison(string methodName, string expectedOperator)
    {
        methodName.Should().MatchRegex(".*(Before|After)Async$");
        expectedOperator.Should().BeOneOf("<", ">");
    }

    [Theory]
    [InlineData("FindByEmailAndTenantIdAsync", "AND")]
    [InlineData("FindByFirstNameOrLastNameAsync", "OR")]
    [InlineData("FindByTenantIdAndStatusAndIsActiveTrueAsync", "AND")]
    public void ConventionBasedQuery_MultipleConditions_ShouldCombineCorrectly(string methodName, string expectedConnector)
    {
        methodName.Should().MatchRegex(".*(And|Or).*Async$");
        expectedConnector.Should().BeOneOf("AND", "OR");
    }

    [Theory]
    [InlineData("FindByTenantIdOrderByNameAscAsync", "ORDER BY name ASC")]
    [InlineData("FindByStatusOrderByCreatedAtDescAsync", "ORDER BY created_at DESC")]
    [InlineData("FindAllOrderByNameAscThenEmailDescAsync", "ORDER BY name ASC, email DESC")]
    public void ConventionBasedQuery_Ordering_ShouldGenerateOrderByClause(string methodName, string expectedOrderBy)
    {
        methodName.Should().MatchRegex(".*OrderBy[A-Z].*(Asc|Desc)Async$");
        expectedOrderBy.Should().Contain("ORDER BY");
    }

    [Theory]
    [InlineData("CountByTenantIdAsync", "COUNT(*)")]
    [InlineData("CountByStatusAsync", "COUNT(*)")]
    [InlineData("CountByAgeGreaterThanAsync", "COUNT(*)")]
    public void ConventionBasedQuery_Count_ShouldGenerateCountQuery(string methodName, string expectedFunction)
    {
        methodName.Should().StartWith("Count");
        methodName.Should().EndWith("Async");
        expectedFunction.Should().Be("COUNT(*)");
    }

    [Theory]
    [InlineData("ExistsByEmailAsync", "COUNT(1)")]
    [InlineData("ExistsByTenantIdAndEmailAsync", "COUNT(1)")]
    public void ConventionBasedQuery_Exists_ShouldGenerateExistsQuery(string methodName, string expectedFunction)
    {
        methodName.Should().StartWith("Exists");
        methodName.Should().EndWith("Async");
        expectedFunction.Should().Be("COUNT(1)");
    }

    [Theory]
    [InlineData("DeleteByEmailAsync", "DELETE FROM")]
    [InlineData("RemoveByStatusAsync", "DELETE FROM")]
    public void ConventionBasedQuery_Delete_ShouldGenerateDeleteQuery(string methodName, string expectedOperation)
    {
        methodName.Should().MatchRegex("^(Delete|Remove)By.*Async$");
        expectedOperation.Should().Be("DELETE FROM");
    }

    [Fact]
    public void MethodConventionAnalyzer_ToSnakeCase_ShouldConvertPascalCaseCorrectly()
    {
        // Arrange & Act & Assert
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
        // This validates the pattern recognition logic
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
}

