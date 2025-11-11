using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for regex pattern matching keywords (Regex, Matches, MatchesRegex).
/// </summary>
public class RegexPatternMatchingTests
{
    #region Integration Tests - Verify Generated SQL
    
    [Theory]
    [InlineData("FindByEmailRegexAsync")]
    [InlineData("FindByEmailMatchesAsync")]
    [InlineData("FindByEmailMatchesRegexAsync")]
    public void RegexKeywords_AllVariants_ShouldGenerateRegexpOperator(string methodName)
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
        public string Email {{ get; set; }}
    }}

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {{
        Task<IEnumerable<User>> {methodName}(string pattern);
    }}
}}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("REGEXP");
    }
    
    #endregion
    
    #region SQL Generation with Regex
    
    [Fact]
    public void RegexWithCombinations_ShouldGenerateAndClause()
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
        public string Email { get; set; }
        public string Status { get; set; }
    }

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {
        Task<IEnumerable<User>> FindByEmailRegexAndStatusAsync(string emailPattern, string status);
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("REGEXP");
        generatedCode.Should().Contain("AND");
    }
    
    #endregion
    
    #region Helper Methods
    
    private static GeneratorRunResult RunGenerator(string source)
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
}
