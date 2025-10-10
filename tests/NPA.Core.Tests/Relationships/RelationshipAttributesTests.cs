using FluentAssertions;
using NPA.Core.Annotations;
using Xunit;

namespace NPA.Core.Tests.Relationships;

public class RelationshipAttributesTests
{
    [Fact]
    public void OneToManyAttribute_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var attribute = new OneToManyAttribute();

        // Assert
        attribute.MappedBy.Should().Be(string.Empty);
        attribute.Cascade.Should().Be(CascadeType.None);
        attribute.Fetch.Should().Be(FetchType.Lazy);
        attribute.OrphanRemoval.Should().BeFalse();
    }

    [Fact]
    public void OneToManyAttribute_WithMappedBy_ShouldSetProperty()
    {
        // Arrange & Act
        var attribute = new OneToManyAttribute("User");

        // Assert
        attribute.MappedBy.Should().Be("User");
    }

    [Fact]
    public void OneToManyAttribute_WithCascadeAll_ShouldSetProperty()
    {
        // Arrange & Act
        var attribute = new OneToManyAttribute
        {
            Cascade = CascadeType.All,
            OrphanRemoval = true
        };

        // Assert
        attribute.Cascade.Should().Be(CascadeType.All);
        attribute.OrphanRemoval.Should().BeTrue();
    }

    [Fact]
    public void ManyToOneAttribute_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var attribute = new ManyToOneAttribute();

        // Assert
        attribute.Cascade.Should().Be(CascadeType.None);
        attribute.Fetch.Should().Be(FetchType.Eager);
        attribute.Optional.Should().BeTrue();
    }

    [Fact]
    public void ManyToOneAttribute_WithRequiredRelationship_ShouldSetProperty()
    {
        // Arrange & Act
        var attribute = new ManyToOneAttribute
        {
            Optional = false,
            Fetch = FetchType.Lazy
        };

        // Assert
        attribute.Optional.Should().BeFalse();
        attribute.Fetch.Should().Be(FetchType.Lazy);
    }

    [Fact]
    public void ManyToManyAttribute_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var attribute = new ManyToManyAttribute();

        // Assert
        attribute.MappedBy.Should().Be(string.Empty);
        attribute.Cascade.Should().Be(CascadeType.None);
        attribute.Fetch.Should().Be(FetchType.Lazy);
    }

    [Fact]
    public void ManyToManyAttribute_WithMappedBy_ShouldSetProperty()
    {
        // Arrange & Act
        var attribute = new ManyToManyAttribute("Roles");

        // Assert
        attribute.MappedBy.Should().Be("Roles");
    }

    [Fact]
    public void JoinColumnAttribute_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var attribute = new JoinColumnAttribute();

        // Assert
        attribute.Name.Should().Be(string.Empty);
        attribute.ReferencedColumnName.Should().Be("id");
        attribute.Unique.Should().BeFalse();
        attribute.Nullable.Should().BeTrue();
        attribute.Insertable.Should().BeTrue();
        attribute.Updatable.Should().BeTrue();
    }

    [Fact]
    public void JoinColumnAttribute_WithName_ShouldSetProperty()
    {
        // Arrange & Act
        var attribute = new JoinColumnAttribute("user_id");

        // Assert
        attribute.Name.Should().Be("user_id");
    }

    [Fact]
    public void JoinColumnAttribute_WithAllProperties_ShouldSetCorrectly()
    {
        // Arrange & Act
        var attribute = new JoinColumnAttribute("order_id")
        {
            ReferencedColumnName = "id",
            Unique = true,
            Nullable = false,
            Insertable = true,
            Updatable = false
        };

        // Assert
        attribute.Name.Should().Be("order_id");
        attribute.ReferencedColumnName.Should().Be("id");
        attribute.Unique.Should().BeTrue();
        attribute.Nullable.Should().BeFalse();
        attribute.Insertable.Should().BeTrue();
        attribute.Updatable.Should().BeFalse();
    }

    [Fact]
    public void JoinTableAttribute_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var attribute = new JoinTableAttribute();

        // Assert
        attribute.Name.Should().Be(string.Empty);
        attribute.Schema.Should().Be(string.Empty);
        attribute.JoinColumns.Should().BeEmpty();
        attribute.InverseJoinColumns.Should().BeEmpty();
    }

    [Fact]
    public void JoinTableAttribute_WithName_ShouldSetProperty()
    {
        // Arrange & Act
        var attribute = new JoinTableAttribute("user_roles");

        // Assert
        attribute.Name.Should().Be("user_roles");
    }

    [Fact]
    public void JoinTableAttribute_WithAllProperties_ShouldSetCorrectly()
    {
        // Arrange & Act
        var attribute = new JoinTableAttribute("user_roles")
        {
            Schema = "public",
            JoinColumns = new[] { "user_id" },
            InverseJoinColumns = new[] { "role_id" }
        };

        // Assert
        attribute.Name.Should().Be("user_roles");
        attribute.Schema.Should().Be("public");
        attribute.JoinColumns.Should().ContainSingle().Which.Should().Be("user_id");
        attribute.InverseJoinColumns.Should().ContainSingle().Which.Should().Be("role_id");
    }

    [Fact]
    public void CascadeType_All_ShouldIncludeAllOperations()
    {
        // Arrange & Act
        var cascadeAll = CascadeType.All;

        // Assert
        cascadeAll.Should().HaveFlag(CascadeType.Persist);
        cascadeAll.Should().HaveFlag(CascadeType.Merge);
        cascadeAll.Should().HaveFlag(CascadeType.Remove);
        cascadeAll.Should().HaveFlag(CascadeType.Refresh);
        cascadeAll.Should().HaveFlag(CascadeType.Detach);
    }

    [Fact]
    public void CascadeType_None_ShouldNotIncludeAnyOperations()
    {
        // Arrange & Act
        var cascadeNone = CascadeType.None;

        // Assert
        cascadeNone.Should().NotHaveFlag(CascadeType.Persist);
        cascadeNone.Should().NotHaveFlag(CascadeType.Merge);
        cascadeNone.Should().NotHaveFlag(CascadeType.Remove);
        cascadeNone.Should().NotHaveFlag(CascadeType.Refresh);
        cascadeNone.Should().NotHaveFlag(CascadeType.Detach);
    }

    [Fact]
    public void CascadeType_CanCombineFlags()
    {
        // Arrange & Act
        var cascade = CascadeType.Persist | CascadeType.Merge;

        // Assert
        cascade.Should().HaveFlag(CascadeType.Persist);
        cascade.Should().HaveFlag(CascadeType.Merge);
        cascade.Should().NotHaveFlag(CascadeType.Remove);
    }

    [Fact]
    public void FetchType_Values_ShouldBeCorrect()
    {
        // Arrange & Act
        var eager = FetchType.Eager;
        var lazy = FetchType.Lazy;

        // Assert
        eager.Should().Be(FetchType.Eager);
        lazy.Should().Be(FetchType.Lazy);
    }
}

