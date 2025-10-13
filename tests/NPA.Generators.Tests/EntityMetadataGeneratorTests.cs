using Xunit;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NPA.Core.Annotations; // Reference the real attributes

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for the EntityMetadataGenerator source generator.
/// </summary>
public class EntityMetadataGeneratorTests
{
    [Fact]
    public void EntityMetadataGenerator_ShouldGenerateMetadataProvider_WhenEntityExists()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using System.Collections.Generic;

namespace TestNamespace
{
    [Entity]
    [Table(""users"")]
    public class User
    {
        [Id]
        [GeneratedValue(GenerationType.Identity)]
        [Column(""id"")]
        public long Id { get; set; }

        [Column(""username"")]
        public string Username { get; set; } = string.Empty;

        [Column(""email"")]
        public string Email { get; set; } = string.Empty;
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        result.GeneratedSources.Should().HaveCount(1);
        
        var generatedSource = result.GeneratedSources[0];
        generatedSource.HintName.Should().Be("GeneratedMetadataProvider.g.cs");
        
        var sourceText = generatedSource.SourceText.ToString();
        
        // Verify the using statement and the class signature (the fix)
        sourceText.Should().Contain("using NPA.Core.Metadata;");
        sourceText.Should().Contain("public sealed class GeneratedMetadataProvider : IMetadataProvider");

        sourceText.Should().Contain("UserMetadata");
        sourceText.Should().Contain("typeof(TestNamespace.User)");
        sourceText.Should().Contain("TableName = \"users\"");
        sourceText.Should().Contain("PrimaryKeyProperty = \"Id\"");
        sourceText.Should().Contain("public EntityMetadata GetEntityMetadata(Type entityType)");
        sourceText.Should().Contain("public bool IsEntity(Type type)");
    }

    [Fact]
    public void EntityMetadataGenerator_ShouldNotGenerateCode_WhenNoEntityExists()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class NotAnEntity { }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        result.GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public void EntityMetadataGenerator_ShouldGenerateProperties_WithCorrectMetadata()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;

namespace TestNamespace
{
    [Entity]
    public class Product
    {
        [Id]
        public int Id { get; set; }

        [Column(""name"")]
        public string Name { get; set; } = string.Empty;

        [Column(""price"")]
        public decimal Price { get; set; }
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        result.GeneratedSources.Should().HaveCount(1);
        
        var sourceText = result.GeneratedSources[0].SourceText.ToString();
        sourceText.Should().Contain("PropertyName = \"Name\"");
        sourceText.Should().Contain("PropertyName = \"Price\"");
        sourceText.Should().Contain("ColumnName = \"name\"");
        sourceText.Should().Contain("ColumnName = \"price\"");
    }

    [Fact]
    public void EntityMetadataGenerator_ShouldHandleMultipleEntities()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;

namespace TestNamespace
{
    [Entity]
    public class User
    {
        [Id]
        public long Id { get; set; }
    }

    [Entity]
    public class Product
    {
        [Id]
        public int Id { get; set; }
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        result.GeneratedSources.Should().HaveCount(1);
        
        var sourceText = result.GeneratedSources[0].SourceText.ToString();
        sourceText.Should().Contain("UserMetadata");
        sourceText.Should().Contain("ProductMetadata");
        sourceText.Should().Contain("typeof(TestNamespace.User)");
        sourceText.Should().Contain("typeof(TestNamespace.Product)");
    }

    [Fact]
    public void EntityMetadataGenerator_ShouldHandleTableName()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;

namespace TestNamespace
{
    [Entity]
    [Table(""users"")]
    public class User
    {
        [Id]
        public long Id { get; set; }
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var sourceText = result.GeneratedSources[0].SourceText.ToString();
        sourceText.Should().Contain("TableName = \"users\"");
    }

    [Fact]
    public void EntityMetadataGenerator_ShouldDetectNullableProperties()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;

namespace TestNamespace
{
    [Entity]
    public class User
    {
        [Id]
        public long Id { get; set; }

        public string? NullableString { get; set; }
        
        public string NonNullableString { get; set; } = string.Empty;
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var sourceText = result.GeneratedSources[0].SourceText.ToString();
        
        // Check that nullable properties are correctly identified
        sourceText.Should().Contain("PropertyName = \"NullableString\"");
        sourceText.Should().Contain("IsNullable = true");
        sourceText.Should().Contain("PropertyName = \"NonNullableString\"");
    }

    [Fact]
    public void EntityMetadataGenerator_ShouldHandleRelationships()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using System.Collections.Generic;

namespace TestNamespace
{
    [Entity]
    public class User
    {
        [Id]
        public long Id { get; set; }

        [OneToMany]
        public List<Order> Orders { get; set; } = new();
    }

    [Entity]
    public class Order
    {
        [Id]
        public long Id { get; set; }

        [ManyToOne]
        public User User { get; set; } = null!;
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var sourceText = result.GeneratedSources[0].SourceText.ToString();
        sourceText.Should().Contain("RelationshipType.OneToMany");
        sourceText.Should().Contain("RelationshipType.ManyToOne");
    }

    [Fact]
    public void EntityMetadataGenerator_ShouldProvideGetMetadataMethod()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;

namespace TestNamespace
{
    [Entity]
    public class User
    {
        [Id]
        public long Id { get; set; }
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        var sourceText = result.GeneratedSources[0].SourceText.ToString();
        sourceText.Should().Contain("public EntityMetadata GetEntityMetadata(Type entityType)");
        sourceText.Should().Contain("_metadata.TryGetValue(entityType, out var metadata)");
    }

    [Fact]
    public void EntityMetadataGenerator_ShouldProvideGetAllMetadataMethod()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;

namespace TestNamespace
{
    [Entity]
    public class User
    {
        [Id]
        public long Id { get; set; }
    }
}";

        // Act
        var result = RunGenerator(source);

        // Assert
        var sourceText = result.GeneratedSources[0].SourceText.ToString();
        sourceText.Should().Contain("public IEnumerable<EntityMetadata> GetAllMetadata()");
        sourceText.Should().Contain("return _metadata.Values;");
    }

    private static GeneratorRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Add reference to the assembly containing the attributes
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(EntityAttribute).Assembly.Location), // Important!
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new EntityMetadataGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        var runResult = driver.GetRunResult();
        
        return runResult.Results[0];
    }
}
