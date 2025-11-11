using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NPA.Core.Annotations;
using Xunit;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for Phase 4.1 custom query attributes.
/// </summary>
public class CustomQueryAttributesTests
{
    #region Attribute Property Tests
    
    [Fact]
    public void QueryAttribute_ShouldStoreSQL()
    {
        // Arrange
        var sql = "SELECT * FROM users WHERE email = @email";

        // Act
        var attr = new QueryAttribute(sql);

        // Assert
        attr.Sql.Should().Be(sql);
        attr.Buffered.Should().BeTrue("default value should be true");
        attr.CommandTimeout.Should().BeNull("default value should be null");
    }

    [Fact]
    public void QueryAttribute_ShouldAllowConfiguration()
    {
        // Arrange
        var sql = "SELECT * FROM users";
        
        // Act
        var attr = new QueryAttribute(sql)
        {
            Buffered = false,
            CommandTimeout = 30
        };

        // Assert
        attr.Buffered.Should().BeFalse();
        attr.CommandTimeout.Should().Be(30);
    }

    [Fact]
    public void QueryAttribute_ShouldThrowOnNullOrEmptySQL()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new QueryAttribute(null!));
        Assert.Throws<ArgumentException>(() => new QueryAttribute(""));
        Assert.Throws<ArgumentException>(() => new QueryAttribute("   "));
    }

    [Fact]
    public void StoredProcedureAttribute_ShouldStoreProcedureName()
    {
        // Arrange
        var procedureName = "sp_GetUsers";

        // Act
        var attr = new StoredProcedureAttribute(procedureName);

        // Assert
        attr.ProcedureName.Should().Be(procedureName);
        attr.Schema.Should().BeNull();
        attr.CommandTimeout.Should().BeNull();
    }

    [Fact]
    public void StoredProcedureAttribute_ShouldAllowSchemaConfiguration()
    {
        // Arrange
        var procedureName = "GetUsers";
        var schema = "dbo";

        // Act
        var attr = new StoredProcedureAttribute(procedureName)
        {
            Schema = schema,
            CommandTimeout = 60
        };

        // Assert
        attr.ProcedureName.Should().Be(procedureName);
        attr.Schema.Should().Be(schema);
        attr.CommandTimeout.Should().Be(60);
    }

    [Fact]
    public void StoredProcedureAttribute_ShouldThrowOnNullOrEmptyName()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new StoredProcedureAttribute(null!));
        Assert.Throws<ArgumentException>(() => new StoredProcedureAttribute(""));
        Assert.Throws<ArgumentException>(() => new StoredProcedureAttribute("   "));
    }

    [Fact]
    public void MultiMappingAttribute_ShouldStoreKeyProperty()
    {
        // Arrange
        var keyProperty = "UserId";

        // Act
        var attr = new MultiMappingAttribute(keyProperty);

        // Assert
        attr.KeyProperty.Should().Be(keyProperty);
        attr.SplitOn.Should().BeNull();
        attr.MapTypes.Should().BeNull();
    }

    [Fact]
    public void MultiMappingAttribute_ShouldAllowSplitOnConfiguration()
    {
        // Arrange
        var keyProperty = "Id";
        var splitOn = "AddressId,OrderId";

        // Act
        var attr = new MultiMappingAttribute(keyProperty)
        {
            SplitOn = splitOn
        };

        // Assert
        attr.KeyProperty.Should().Be(keyProperty);
        attr.SplitOn.Should().Be(splitOn);
    }

    [Fact]
    public void MultiMappingAttribute_ShouldThrowOnNullOrEmptyKey()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new MultiMappingAttribute(null!));
        Assert.Throws<ArgumentException>(() => new MultiMappingAttribute(""));
        Assert.Throws<ArgumentException>(() => new MultiMappingAttribute("   "));
    }

    [Fact]
    public void BulkOperationAttribute_ShouldHaveDefaultValues()
    {
        // Act
        var attr = new BulkOperationAttribute();

        // Assert
        attr.BatchSize.Should().Be(1000, "default batch size should be 1000");
        attr.UseTransaction.Should().BeTrue("default should use transactions");
        attr.CommandTimeout.Should().BeNull();
    }

    [Fact]
    public void BulkOperationAttribute_ShouldAllowConfiguration()
    {
        // Act
        var attr = new BulkOperationAttribute
        {
            BatchSize = 500,
            UseTransaction = false,
            CommandTimeout = 120
        };

        // Assert
        attr.BatchSize.Should().Be(500);
        attr.UseTransaction.Should().BeFalse();
        attr.CommandTimeout.Should().Be(120);
    }

    [Fact]
    public void QueryAttribute_ShouldTargetMethods()
    {
        // Arrange
        var attributeType = typeof(QueryAttribute);
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(attributeType, typeof(AttributeUsageAttribute));

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Method);
    }

    [Fact]
    public void StoredProcedureAttribute_ShouldTargetMethods()
    {
        // Arrange
        var attributeType = typeof(StoredProcedureAttribute);
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(attributeType, typeof(AttributeUsageAttribute));

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Method);
    }

    [Fact]
    public void MultiMappingAttribute_ShouldTargetMethods()
    {
        // Arrange
        var attributeType = typeof(MultiMappingAttribute);
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(attributeType, typeof(AttributeUsageAttribute));

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Method);
    }

    [Fact]
    public void BulkOperationAttribute_ShouldTargetMethods()
    {
        // Arrange
        var attributeType = typeof(BulkOperationAttribute);
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(attributeType, typeof(AttributeUsageAttribute));

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Method);
    }
    
    #endregion
    
    #region Integration Tests
    
    [Fact]
    public void QueryAttribute_ShouldGenerateQueryExecution()
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
    }

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {
        [Query(""SELECT * FROM users WHERE email = @email"")]
        Task<IEnumerable<User>> FindByCustomEmailAsync(string email);
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("SELECT * FROM users WHERE email = @email");
        generatedCode.Should().Contain("FindByCustomEmailAsync");
        generatedCode.Should().Contain("QueryAsync<");
    }
    
    [Fact]
    public void StoredProcedureAttribute_ShouldGenerateStoredProcedureCall()
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
    }

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {
        [StoredProcedure(""sp_GetUsersByRole"")]
        Task<IEnumerable<User>> GetUsersByRoleAsync(string role);
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("sp_GetUsersByRole");
        generatedCode.Should().Contain("GetUsersByRoleAsync");
        generatedCode.Should().Contain("CommandType.StoredProcedure");
    }
    
    [Fact]
    public void MultiMappingAttribute_ShouldGenerateMultiMappingCode()
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
        public Address Address { get; set; }
    }
    
    [Entity]
    public class Address
    {
        [Id]
        public long Id { get; set; }
        public string Street { get; set; }
    }

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {
        [Query(""SELECT * FROM users u INNER JOIN addresses a ON u.address_id = a.id"")]
        [MultiMapping(""Id"", SplitOn = ""Id"")]
        Task<IEnumerable<User>> GetUsersWithAddressesAsync();
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("GetUsersWithAddressesAsync");
        generatedCode.Should().Contain("splitOn:");
    }
    
    [Fact]
    public void BulkOperationAttribute_ShouldGenerateBulkOperationCode()
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
    }

    [Repository]
    public interface IUserRepository : IRepository<User, long>
    {
        [BulkOperation(BatchSize = 500)]
        Task BulkInsertAsync(IEnumerable<User> users);
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var generatedCode = GetGeneratedCode(result);
        generatedCode.Should().Contain("BulkInsertAsync");
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
