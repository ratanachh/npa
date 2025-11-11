using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace NPA.Generators.Tests;

public class OrderByParsingTests
{
    #region Method Analysis Tests (using IMethodSymbol)
    
    [Theory]
    [InlineData("FindByEmailOrderByName", "Name", "Asc")]
    [InlineData("FindByEmailOrderByNameDesc", "Name", "Desc")]
    [InlineData("FindByEmailOrderByNameAsc", "Name", "Asc")]
    [InlineData("GetByStatusOrderByCreatedAt", "CreatedAt", "Asc")]
    [InlineData("GetByStatusOrderByCreatedAtDesc", "CreatedAt", "Desc")]
    public void AnalyzeMethod_WithSingleOrderBy_ParsesCorrectly(
        string methodName, string expectedProperty, string expectedDirection)
    {
        // Arrange
        var method = CreateMethod(methodName);

        // Act
        var result = MethodConventionAnalyzer.AnalyzeMethod(method);

        // Assert
        result.OrderByProperties.Should().HaveCount(1);
        result.OrderByProperties[0].PropertyName.Should().Be(expectedProperty);
        result.OrderByProperties[0].Direction.Should().Be(expectedDirection);
    }

    [Fact]
    public void AnalyzeMethod_WithMultipleOrderBy_ParsesCorrectly()
    {
        // Arrange
        var method = CreateMethod("FindByEmailOrderByNameDescThenCreatedAtAsc");

        // Act
        var result = MethodConventionAnalyzer.AnalyzeMethod(method);

        // Assert
        result.OrderByProperties.Should().HaveCount(2);
        result.OrderByProperties[0].PropertyName.Should().Be("Name");
        result.OrderByProperties[0].Direction.Should().Be("Desc");
        result.OrderByProperties[1].PropertyName.Should().Be("CreatedAt");
        result.OrderByProperties[1].Direction.Should().Be("Asc");
    }

    [Fact]
    public void AnalyzeMethod_WithMultipleOrderByDefaultDirection_ParsesCorrectly()
    {
        // Arrange
        var method = CreateMethod("FindByStatusOrderByNameThenCreatedAt");

        // Act
        var result = MethodConventionAnalyzer.AnalyzeMethod(method);

        // Assert
        result.OrderByProperties.Should().HaveCount(2);
        result.OrderByProperties[0].PropertyName.Should().Be("Name");
        result.OrderByProperties[0].Direction.Should().Be("Asc");
        result.OrderByProperties[1].PropertyName.Should().Be("CreatedAt");
        result.OrderByProperties[1].Direction.Should().Be("Asc");
    }

    [Fact]
    public void AnalyzeMethod_WithThreeOrderByClauses_ParsesCorrectly()
    {
        // Arrange
        var method = CreateMethod("GetAllOrderByStatusDescThenNameAscThenCreatedAtDesc");

        // Act
        var result = MethodConventionAnalyzer.AnalyzeMethod(method);

        // Assert
        result.OrderByProperties.Should().HaveCount(3);
        result.OrderByProperties[0].PropertyName.Should().Be("Status");
        result.OrderByProperties[0].Direction.Should().Be("Desc");
        result.OrderByProperties[1].PropertyName.Should().Be("Name");
        result.OrderByProperties[1].Direction.Should().Be("Asc");
        result.OrderByProperties[2].PropertyName.Should().Be("CreatedAt");
        result.OrderByProperties[2].Direction.Should().Be("Desc");
    }

    [Fact]
    public void AnalyzeMethod_WithoutOrderBy_HasEmptyOrderByList()
    {
        // Arrange
        var method = CreateMethod("FindByEmail");

        // Act
        var result = MethodConventionAnalyzer.AnalyzeMethod(method);

        // Assert
        result.OrderByProperties.Should().BeEmpty();
        result.PropertyNames.Should().ContainSingle("Email");
    }

    [Fact]
    public void AnalyzeMethod_CombinesWhereAndOrderBy_ParsesBothCorrectly()
    {
        // Arrange
        var method = CreateMethod("FindByEmailAndStatusOrderByNameDescThenCreatedAt");

        // Act
        var result = MethodConventionAnalyzer.AnalyzeMethod(method);

        // Assert
        result.PropertyNames.Should().HaveCount(2);
        result.PropertyNames.Should().Contain("Email");
        result.PropertyNames.Should().Contain("Status");
        
        result.OrderByProperties.Should().HaveCount(2);
        result.OrderByProperties[0].PropertyName.Should().Be("Name");
        result.OrderByProperties[0].Direction.Should().Be("Desc");
        result.OrderByProperties[1].PropertyName.Should().Be("CreatedAt");
        result.OrderByProperties[1].Direction.Should().Be("Asc");
    }

    [Theory]
    [InlineData("CountByStatusOrderByName")]
    [InlineData("ExistsByEmailOrderByCreatedAt")]
    [InlineData("DeleteByIdOrderByName")]
    public void AnalyzeMethod_DifferentQueryTypesWithOrderBy_ParsesCorrectly(string methodName)
    {
        // Arrange
        var method = CreateMethod(methodName);

        // Act
        var result = MethodConventionAnalyzer.AnalyzeMethod(method);

        // Assert
        result.OrderByProperties.Should().NotBeEmpty();
    }

    [Fact]
    public void AnalyzeMethod_AsyncMethod_ParsesOrderByCorrectly()
    {
        // Arrange
        var method = CreateMethod("FindByEmailOrderByNameDescAsync");

        // Act
        var result = MethodConventionAnalyzer.AnalyzeMethod(method);

        // Assert
        result.OrderByProperties.Should().HaveCount(1);
        result.OrderByProperties[0].PropertyName.Should().Be("Name");
        result.OrderByProperties[0].Direction.Should().Be("Desc");
    }
    
    #endregion
    
    #region Integration Tests (verify generated SQL)
    
    [Fact]
    public void OrderByParsing_SingleAscending_ShouldGenerateOrderByClause()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    public class User
    {
        [Id]
        public long Id { get; set; }}
        public string Email { get; set; }
        public string Name { get; set; }
    }

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {
        Task<IEnumerable<User>> FindByEmailOrderByNameAsync(string email);
    }
}";

        // Act
        var result = RunRepositoryGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("ORDER BY");
    }
    
    [Fact]
    public void OrderByParsing_SingleDescending_ShouldGenerateOrderByDescClause()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    public class User
    {
        [Id]
        public long Id { get; set; }
        public string CreatedAt { get; set; }
    }

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {
        Task<IEnumerable<User>> FindAllOrderByCreatedAtDescAsync();
    }
}";

        // Act
        var result = RunRepositoryGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("ORDER BY");
        generatedCode.Should().Contain("DESC");
    }
    
    private static GeneratorRunResult RunRepositoryGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(NPA.Core.Annotations.EntityAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(NPA.Core.Repositories.IRepository<,>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new RepositoryGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        var runResult = driver.GetRunResult();
        
        return runResult.Results[0];
    }
    
    private static string GetGeneratedCode(GeneratorRunResult result)
    {
        if (result.GeneratedSources.Length == 0)
            return string.Empty;
            
        return result.GeneratedSources[0].SourceText.ToString();
    }
    
    #endregion

    private IMethodSymbol CreateMethod(string methodName)
    {
        var code = $@"
            using System.Collections.Generic;
            using System.Threading.Tasks;

            public interface ITestRepository
            {{
                Task<List<object>> {methodName}();
            }}
            ";

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var model = compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();
        var methodSyntax = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        return model.GetDeclaredSymbol(methodSyntax)!;
    }
}
