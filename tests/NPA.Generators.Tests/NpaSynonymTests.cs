using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for NPA keyword synonyms to ensure all variants work correctly.
/// </summary>
public class NpaSynonymTests
{
    #region Subject Keyword Synonyms
    
    [Theory]
    [InlineData("FindByEmailAsync")]
    [InlineData("GetByEmailAsync")]
    [InlineData("QueryByEmailAsync")]
    [InlineData("SearchByEmailAsync")]
    [InlineData("ReadByEmailAsync")]
    [InlineData("StreamByEmailAsync")]
    public void SubjectKeywords_AllSelectSynonyms_ShouldGenerateSelectQuery(string methodName)
    {
        // Arrange
        var source = $@"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{{
    [Entity]
    [Table(""users"")]
    public class User
    {{
        [Id]
        public long Id {{ get; set; }}
        
        [Column(""email"")]
        public string Email {{ get; set; }}
    }}

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {{
        Task<IEnumerable<User>> {methodName}(string email);
    }}
}}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain($"public async System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<TestNamespace.User>> {methodName}");
        generatedCode.Should().Contain("SELECT");
        generatedCode.Should().Contain("WHERE email = @email");
    }
    
    #endregion
    
    #region Comparison Operator Synonyms
    
    [Theory]
    [InlineData("FindByAgeGreaterThanAsync", ">")]
    [InlineData("FindByAgeIsGreaterThanAsync", ">")]
    [InlineData("FindByPriceGreaterThanEqualAsync", ">=")]
    [InlineData("FindByPriceIsGreaterThanEqualAsync", ">=")]
    [InlineData("FindByStockLessThanAsync", "<")]
    [InlineData("FindByStockIsLessThanAsync", "<")]
    [InlineData("FindByQuantityLessThanEqualAsync", "<=")]
    [InlineData("FindByQuantityIsLessThanEqualAsync", "<=")]
    public void ComparisonOperators_WithAndWithoutIsPrefix_ShouldBothWork(
        string methodName, string expectedOperator)
    {
        // Arrange
        var propertyName = methodName.Replace("FindBy", "").Split(new[] { "GreaterThan", "LessThan", "IsGreater", "IsLess" }, StringSplitOptions.None)[0];
        var source = $@"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{{
    [Entity]
    public class Product
    {{
        [Id]
        public long Id {{ get; set; }}
        public int {propertyName} {{ get; set; }}
    }}

    [Repository]
    public interface IProductRepository : IRepository<Product, long>
    {{
        Task<IEnumerable<Product>> {methodName}(int value);
    }}
}}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain(expectedOperator);
    }
    
    #endregion
    
    #region Date/Time Operator Synonyms
    
    [Theory]
    [InlineData("FindByCreatedAtBeforeAsync", "<")]
    [InlineData("FindByCreatedAtIsBeforeAsync", "<")]
    [InlineData("FindByUpdatedAtAfterAsync", ">")]
    [InlineData("FindByUpdatedAtIsAfterAsync", ">")]
    public void DateTimeOperators_WithAndWithoutIsPrefix_ShouldBothWork(
        string methodName, string expectedOperator)
    {
        // Arrange
        var source = $@"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{{
    [Entity]
    public class Event
    {{
        [Id]
        public long Id {{ get; set; }}
        public DateTime CreatedAt {{ get; set; }}
        public DateTime UpdatedAt {{ get; set; }}
    }}

    [Repository]
    public interface IEventRepository : IRepository<Event, long>
    {{
        Task<IEnumerable<Event>> {methodName}(DateTime date);
    }}
}}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain(expectedOperator);
    }
    
    #endregion
    
    #region String Operator Synonyms
    
    [Theory]
    [InlineData("FindByNameContainingAsync")]
    [InlineData("FindByNameIsContainingAsync")]
    [InlineData("FindByNameContainsAsync")]
    public void ContainingOperator_AllVariants_ShouldWork(string methodName)
    {
        // Arrange
        var source = $@"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{{
    [Entity]
    public class Product
    {{
        [Id]
        public long Id {{ get; set; }}
        public string Name {{ get; set; }}
    }}

    [Repository]
    public interface IProductRepository : IRepository<Product, long>
    {{
        Task<IEnumerable<Product>> {methodName}(string name);
    }}
}}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("LIKE");
        generatedCode.Should().Contain("CONCAT");
    }
    
    #endregion
    
    #region Equality/Inequality Synonyms
    
    [Theory]
    [InlineData("FindByNameAsync")]
    [InlineData("FindByNameIsAsync")]
    [InlineData("FindByNameEqualsAsync")]
    public void EqualityOperators_AllVariants_ShouldMapToEquals(string methodName)
    {
        // Arrange
        var source = $@"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{{
    [Entity]
    public class User
    {{
        [Id]
        public long Id {{ get; set; }}
        public string Name {{ get; set; }}
    }}

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {{
        Task<IEnumerable<User>> {methodName}(string name);
    }}
}}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("= @name");
    }
    
    #endregion
    
    #region Helper Methods
    
    private static GeneratorRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Core.Annotations.EntityAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Core.Repositories.IRepository<,>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location)
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
}
