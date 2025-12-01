using Xunit;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NPA.Core.Annotations; // Reference the real attributes
using NPA.Generators.Generators;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for the EntityMetadataGenerator source generator.
/// </summary>
public class EntityMetadataGeneratorTests : GeneratorTestBase
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
        var result = RunGenerator<EntityMetadataGenerator>(source);

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
        var result = RunGenerator<EntityMetadataGenerator>(source);

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
        var result = RunGenerator<EntityMetadataGenerator>(source);

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
        var result = RunGenerator<EntityMetadataGenerator>(source);

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
        var result = RunGenerator<EntityMetadataGenerator>(source);

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
        var result = RunGenerator<EntityMetadataGenerator>(source);

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
        var result = RunGenerator<EntityMetadataGenerator>(source);

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
        var result = RunGenerator<EntityMetadataGenerator>(source);

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
        var result = RunGenerator<EntityMetadataGenerator>(source);

        // Assert
        var sourceText = result.GeneratedSources[0].SourceText.ToString();
        sourceText.Should().Contain("public IEnumerable<EntityMetadata> GetAllMetadata()");
        sourceText.Should().Contain("return _metadata.Values;");
    }

    [Fact]
    public void EntityMetadataGenerator_ShouldGenerateJoinColumn_ForManyToOneRelationship()
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
    [Table(""orders"")]
    public class Order
    {
        [Id]
        public long Id { get; set; }

        [Column(""user_id"")]
        public long? UserId { get; set; }

        [ManyToOne]
        [JoinColumn(""user_id"")]
        public User? User { get; set; }
    }
}";

        // Act
        var result = RunGenerator<EntityMetadataGenerator>(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var sourceText = result.GeneratedSources[0].SourceText.ToString();
        
        // Verify JoinColumn is generated
        sourceText.Should().Contain("JoinColumn = new JoinColumnMetadata");
        sourceText.Should().Contain("Name = \"user_id\"");
        sourceText.Should().Contain("RelationshipType.ManyToOne");
    }

    [Fact]
    public void EntityMetadataGenerator_ShouldGenerateJoinColumn_WithDefaultName_WhenNotSpecified()
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
    public class Order
    {
        [Id]
        public long Id { get; set; }

        [ManyToOne]
        public User? Customer { get; set; }
    }
}";

        // Act
        var result = RunGenerator<EntityMetadataGenerator>(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var sourceText = result.GeneratedSources[0].SourceText.ToString();
        
        // Verify JoinColumn is generated with default name
        sourceText.Should().Contain("JoinColumn = new JoinColumnMetadata");
        sourceText.Should().Contain("Name = \"customer_id\"");
    }

    [Fact]
    public void EntityMetadataGenerator_ShouldGenerateMappedBy_ForOneToManyRelationship()
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

        [OneToMany(MappedBy = ""User"")]
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }

    [Entity]
    public class Order
    {
        [Id]
        public long Id { get; set; }

        [ManyToOne]
        [JoinColumn(""user_id"")]
        public User? User { get; set; }
    }
}";

        // Act
        var result = RunGenerator<EntityMetadataGenerator>(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var sourceText = result.GeneratedSources[0].SourceText.ToString();
        
        // Verify MappedBy is generated
        sourceText.Should().Contain("MappedBy = \"User\"");
        sourceText.Should().Contain("RelationshipType.OneToMany");
    }

    [Fact]
    public void EntityMetadataGenerator_ShouldGenerateJoinColumn_ForOneToOneRelationship()
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

        [OneToOne]
        [JoinColumn(""profile_id"")]
        public UserProfile? Profile { get; set; }
    }

    [Entity]
    public class UserProfile
    {
        [Id]
        public long Id { get; set; }

        [OneToOne(MappedBy = ""Profile"")]
        public User? User { get; set; }
    }
}";

        // Act
        var result = RunGenerator<EntityMetadataGenerator>(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var sourceText = result.GeneratedSources[0].SourceText.ToString();
        
        // Verify JoinColumn is generated for owner side
        sourceText.Should().Contain("RelationshipType.OneToOne");
        sourceText.Should().Contain("JoinColumn = new JoinColumnMetadata");
        sourceText.Should().Contain("Name = \"profile_id\"");
        sourceText.Should().Contain("MappedBy = \"Profile\"");
    }

    [Fact]
    public void EntityMetadataGenerator_ShouldGenerateJoinColumn_WithAllAttributes()
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
    public class Order
    {
        [Id]
        public long Id { get; set; }

        [ManyToOne]
        [JoinColumn(""user_id"", ReferencedColumnName = ""id"", Unique = true, Nullable = false, Insertable = true, Updatable = true)]
        public User? User { get; set; }
    }
}";

        // Act
        var result = RunGenerator<EntityMetadataGenerator>(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var sourceText = result.GeneratedSources[0].SourceText.ToString();
        
        // Verify all JoinColumn properties are generated
        sourceText.Should().Contain("Name = \"user_id\"");
        sourceText.Should().Contain("ReferencedColumnName = \"id\"");
        sourceText.Should().Contain("Unique = true");
        sourceText.Should().Contain("Nullable = false");
        sourceText.Should().Contain("Insertable = true");
        sourceText.Should().Contain("Updatable = true");
    }

    [Fact]
    public void EntityMetadataGenerator_ShouldNotGenerateJoinColumn_ForManyToManyRelationship()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using System.Collections.Generic;

namespace TestNamespace
{
    [Entity]
    public class Student
    {
        [Id]
        public long Id { get; set; }

        [ManyToMany]
        [JoinTable(""student_courses"", JoinColumns = new[] { ""student_id"" }, InverseJoinColumns = new[] { ""course_id"" })]
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }

    [Entity]
    public class Course
    {
        [Id]
        public long Id { get; set; }

        [ManyToMany(MappedBy = ""Courses"")]
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}";

        // Act
        var result = RunGenerator<EntityMetadataGenerator>(source);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        var sourceText = result.GeneratedSources[0].SourceText.ToString();
        
        // Verify ManyToMany doesn't use JoinColumn but uses JoinTable
        sourceText.Should().Contain("RelationshipType.ManyToMany");
        sourceText.Should().Contain("JoinTable = new JoinTableMetadata");
    }
}

