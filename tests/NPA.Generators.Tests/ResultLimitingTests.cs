using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using NPA.Generators.Generators;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for result limiting keywords (First, Top).
/// </summary>
public class ResultLimitingTests : GeneratorTestBase
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
        var result = RunGenerator<RepositoryGenerator>(source, includeAnnotationSource: false);

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
        var result = RunGenerator<RepositoryGenerator>(source, includeAnnotationSource: false);

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
        var result = RunGenerator<RepositoryGenerator>(source, includeAnnotationSource: false);

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
        var result = RunGenerator<RepositoryGenerator>(source, includeAnnotationSource: false);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("DISTINCT");
        generatedCode.Should().Contain("FETCH FIRST 10");
    }
    
    #endregion
}

