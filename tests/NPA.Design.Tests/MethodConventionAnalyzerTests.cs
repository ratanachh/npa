using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using NPA.Design.Services;
using NPA.Design.Generators;

namespace NPA.Design.Tests;

/// <summary>
/// Tests for MethodConventionAnalyzer utility methods.
/// </summary>
public class MethodConventionAnalyzerTests
{
    #region ToSnakeCase Tests
    
    [Theory]
    [InlineData("Email", "email")]
    [InlineData("FirstName", "first_name")]
    [InlineData("UserId", "user_id")]
    [InlineData("CreatedAt", "created_at")]
    [InlineData("IsActive", "is_active")]
    [InlineData("HTTPResponse", "h_t_t_p_response")]
    [InlineData("ID", "i_d")]
    public void ToSnakeCase_ShouldConvertCorrectly(string input, string expected)
    {
        // Act
        var result = MethodConventionAnalyzer.ToSnakeCase(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToSnakeCase_ShouldHandleEmptyString()
    {
        // Act
        var result = MethodConventionAnalyzer.ToSnakeCase("");

        // Assert
        result.Should().Be("");
    }

    [Fact]
    public void ToSnakeCase_ShouldHandleSingleCharacter()
    {
        // Act
        var result = MethodConventionAnalyzer.ToSnakeCase("A");

        // Assert
        result.Should().Be("a");
    }

    [Fact]
    public void ToSnakeCase_ShouldHandleAllLowercase()
    {
        // Act
        var result = MethodConventionAnalyzer.ToSnakeCase("email");

        // Assert
        result.Should().Be("email");
    }

    [Fact]
    public void ToSnakeCase_ShouldHandleConsecutiveUppercase()
    {
        // Act
        var result = MethodConventionAnalyzer.ToSnakeCase("XMLParser");

        // Assert
        result.Should().Be("x_m_l_parser");
    }

    [Fact]
    public void ToSnakeCase_ShouldHandleNumbersAndUnderscores()
    {
        // Act
        var result = MethodConventionAnalyzer.ToSnakeCase("User123Name");

        // Assert
        result.Should().Be("user123_name");
    }
    
    #endregion
    
    #region Convention-Based Query Generation Tests

    [Theory]
    [InlineData("FindByEmailAsync")]
    [InlineData("GetByIdAsync")]
    [InlineData("QueryByNameAsync")]
    public void ConventionBasedQuery_SimpleEquality_ShouldGenerateCorrectSQL(string methodName)
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
        
        [Column(""name"")]
        public string Name {{ get; set; }}
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
        generatedCode.Should().Contain("WHERE");
        generatedCode.Should().Contain("@");
    }

    [Theory]
    [InlineData("FindByEmailAndStatusAsync")]
    [InlineData("FindByFirstNameOrLastNameAsync")]
    public void ConventionBasedQuery_MultipleConditions_ShouldCombineCorrectly(string methodName)
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
        public string Status {{ get; set; }}
        public string FirstName {{ get; set; }}
        public string LastName {{ get; set; }}
    }}

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {{
        Task<IEnumerable<User>> {methodName}(string param1, string param2);
    }}
}}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("WHERE");
        if (methodName.Contains("And"))
            generatedCode.Should().Contain("AND");
        if (methodName.Contains("Or"))
            generatedCode.Should().Contain("OR");
    }

    [Fact]
    public void ConventionBasedQuery_CountMethod_ShouldGenerateCountQuery()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    public class User
    {
        [Id]
        public long Id { get; set; }
        public string Status { get; set; }
    }

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {
        Task<int> CountByStatusAsync(string status);
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("COUNT");
    }

    [Fact]
    public void ConventionBasedQuery_ExistsMethod_ShouldGenerateExistsQuery()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    public class User
    {
        [Id]
        public long Id { get; set; }
        public string Email { get; set; }
    }

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {
        Task<bool> ExistsByEmailAsync(string email);
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("COUNT");
        generatedCode.Should().Contain("return");
    }

    [Fact]
    public void ConventionBasedQuery_DeleteMethod_ShouldGenerateDeleteQuery()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    public class User
    {
        [Id]
        public long Id { get; set; }
        public string Email { get; set; }
    }

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {
        Task DeleteByEmailAsync(string email);
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("DELETE FROM");
        generatedCode.Should().Contain("WHERE");
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
