using Xunit;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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

        [Column(""username"", IsNullable = false, Length = 50)]
        public string Username { get; set; } = string.Empty;

        [Column(""email"", IsNullable = false, IsUnique = true)]
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
        sourceText.Should().Contain("public sealed class GeneratedMetadataProvider : NPA.Core.Metadata.IMetadataProvider");
        sourceText.Should().Contain("UserMetadata");
        sourceText.Should().Contain("typeof(TestNamespace.User)");
        sourceText.Should().Contain("TableName = \"users\"");
        sourceText.Should().Contain("PrimaryKeyProperty = \"Id\"");
        sourceText.Should().Contain("public EntityMetadata GetEntityMetadata<T>()");
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
    public class User
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
    }
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
        // Create a syntax tree from the source
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Create compilation with necessary references
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Create the generator
        var generator = new EntityMetadataGenerator();

        // Run the generator
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        // Get the results
        var runResult = driver.GetRunResult();
        
        return runResult.Results[0];
    }
}

