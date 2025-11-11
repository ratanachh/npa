using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for result limiting keywords (First, Top).
/// </summary>
public class ResultLimitingTests
{
    #region Integration Tests - Verify Generated SQL
    
    [Theory]
    [InlineData("FindFirst5ByNameAsync", 5)]
    [InlineData("GetTop10ByStatusAsync", 10)]
    [InlineData("QueryFirst3ByEmailAsync", 3)]
    [InlineData("SearchTop20ByTenantIdAsync", 20)]
    public void ResultLimiting_WithNumber_ShouldGenerateFetchClause(string methodName, int expectedLimit)
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
        public string Status {{ get; set; }}
        public string Email {{ get; set; }}
        public string TenantId {{ get; set; }}
    }}

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {{
        Task<IEnumerable<User>> {methodName}(string value);
    }}
}}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("FETCH FIRST");
        generatedCode.Should().Contain(expectedLimit.ToString());
    }
    
    [Theory]
    [InlineData("FindFirstByNameAsync")]
    [InlineData("GetTopByStatusAsync")]
    public void ResultLimiting_WithoutNumber_ShouldDefaultToOne(string methodName)
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
        public string Status {{ get; set; }}
    }}

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {{
        Task<IEnumerable<User>> {methodName}(string value);
    }}
}}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("FETCH FIRST 1");
    }
    
    [Fact]
    public void ResultLimiting_WithOrderBy_ShouldCombineBoth()
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
        public string Name { get; set; }
        public string Status { get; set; }
    }

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {
        Task<IEnumerable<User>> FindFirst5ByStatusOrderByNameDescAsync(string status);
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("ORDER BY");
        generatedCode.Should().Contain("FETCH FIRST 5");
    }
    
    [Fact]
    public void ResultLimiting_WithDistinct_ShouldCombineBoth()
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
        public string TenantId { get; set; }
    }

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {
        Task<IEnumerable<User>> FindDistinctFirst10ByTenantIdAsync(string tenantId);
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("DISTINCT");
        generatedCode.Should().Contain("FETCH FIRST 10");
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
