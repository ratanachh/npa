using FluentAssertions;
using NPA.Core.Annotations;
using NPA.Core.Metadata;
using Xunit;

namespace NPA.Core.Tests.Relationships;

public class RelationshipMetadataTests
{
    private readonly MetadataProvider _metadataProvider = new();

    [Entity]
    [Table("users")]
    private class User
    {
        [Id]
        public int Id { get; set; }

        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [OneToMany("User", Cascade = CascadeType.All)]
        public ICollection<Order> Orders { get; set; } = new List<Order>();

        [ManyToMany]
        [JoinTable("user_roles", 
            JoinColumns = new[] { "user_id" }, 
            InverseJoinColumns = new[] { "role_id" })]
        public ICollection<Role> Roles { get; set; } = new List<Role>();
    }

    [Entity]
    [Table("orders")]
    private class Order
    {
        [Id]
        public int Id { get; set; }

        [ManyToOne(Fetch = FetchType.Eager)]
        [JoinColumn("user_id")]
        public User User { get; set; } = null!;

        [OneToMany("Order", Cascade = CascadeType.All, OrphanRemoval = true)]
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    [Entity]
    [Table("order_items")]
    private class OrderItem
    {
        [Id]
        public int Id { get; set; }

        [ManyToOne]
        [JoinColumn("order_id", Nullable = false)]
        public Order Order { get; set; } = null!;
    }

    [Entity]
    [Table("roles")]
    private class Role
    {
        [Id]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [ManyToMany("Roles")]
        public ICollection<User> Users { get; set; } = new List<User>();
    }

    [Fact]
    public void GetEntityMetadata_UserEntity_ShouldDetectOneToManyRelationship()
    {
        // Act
        var metadata = _metadataProvider.GetEntityMetadata<User>();

        // Assert
        metadata.Relationships.Should().ContainKey("Orders");
        var relationship = metadata.Relationships["Orders"];
        relationship.RelationshipType.Should().Be(RelationshipType.OneToMany);
        relationship.TargetEntityType.Should().Be(typeof(Order));
        relationship.MappedBy.Should().Be("User");
        relationship.CascadeType.Should().Be(CascadeType.All);
        relationship.FetchType.Should().Be(FetchType.Lazy);
        relationship.IsOwner.Should().BeFalse(); // Non-owning side due to mappedBy
    }

    [Fact]
    public void GetEntityMetadata_UserEntity_ShouldDetectManyToManyRelationship()
    {
        // Act
        var metadata = _metadataProvider.GetEntityMetadata<User>();

        // Assert
        metadata.Relationships.Should().ContainKey("Roles");
        var relationship = metadata.Relationships["Roles"];
        relationship.RelationshipType.Should().Be(RelationshipType.ManyToMany);
        relationship.TargetEntityType.Should().Be(typeof(Role));
        relationship.IsOwner.Should().BeTrue(); // Owning side (no mappedBy)
        relationship.JoinTable.Should().NotBeNull();
        relationship.JoinTable!.Name.Should().Be("user_roles");
        relationship.JoinTable.JoinColumns.Should().ContainSingle().Which.Should().Be("user_id");
        relationship.JoinTable.InverseJoinColumns.Should().ContainSingle().Which.Should().Be("role_id");
    }

    [Fact]
    public void GetEntityMetadata_OrderEntity_ShouldDetectManyToOneRelationship()
    {
        // Act
        var metadata = _metadataProvider.GetEntityMetadata<Order>();

        // Assert
        metadata.Relationships.Should().ContainKey("User");
        var relationship = metadata.Relationships["User"];
        relationship.RelationshipType.Should().Be(RelationshipType.ManyToOne);
        relationship.TargetEntityType.Should().Be(typeof(User));
        relationship.FetchType.Should().Be(FetchType.Eager);
        relationship.IsOwner.Should().BeTrue();
        relationship.JoinColumn.Should().NotBeNull();
        relationship.JoinColumn!.Name.Should().Be("user_id");
        relationship.JoinColumn.ReferencedColumnName.Should().Be("id");
    }

    [Fact]
    public void GetEntityMetadata_OrderEntity_ShouldDetectOneToManyWithOrphanRemoval()
    {
        // Act
        var metadata = _metadataProvider.GetEntityMetadata<Order>();

        // Assert
        metadata.Relationships.Should().ContainKey("Items");
        var relationship = metadata.Relationships["Items"];
        relationship.RelationshipType.Should().Be(RelationshipType.OneToMany);
        relationship.TargetEntityType.Should().Be(typeof(OrderItem));
        relationship.OrphanRemoval.Should().BeTrue();
        relationship.CascadeType.Should().Be(CascadeType.All);
    }

    [Fact]
    public void GetEntityMetadata_OrderItemEntity_ShouldDetectRequiredManyToOne()
    {
        // Act
        var metadata = _metadataProvider.GetEntityMetadata<OrderItem>();

        // Assert
        metadata.Relationships.Should().ContainKey("Order");
        var relationship = metadata.Relationships["Order"];
        relationship.RelationshipType.Should().Be(RelationshipType.ManyToOne);
        relationship.JoinColumn.Should().NotBeNull();
        relationship.JoinColumn!.Name.Should().Be("order_id");
        relationship.JoinColumn.Nullable.Should().BeFalse();
    }

    [Fact]
    public void GetEntityMetadata_RoleEntity_ShouldDetectManyToManyInverseSide()
    {
        // Act
        var metadata = _metadataProvider.GetEntityMetadata<Role>();

        // Assert
        metadata.Relationships.Should().ContainKey("Users");
        var relationship = metadata.Relationships["Users"];
        relationship.RelationshipType.Should().Be(RelationshipType.ManyToMany);
        relationship.TargetEntityType.Should().Be(typeof(User));
        relationship.MappedBy.Should().Be("Roles");
        relationship.IsOwner.Should().BeFalse(); // Non-owning side due to mappedBy
        relationship.JoinTable.Should().BeNull(); // Join table only on owning side
    }

    [Entity]
    [Table("products")]
    private class Product
    {
        [Id]
        public int Id { get; set; }

        [ManyToOne]
        public Category Category { get; set; } = null!;
    }

    [Entity]
    [Table("categories")]
    private class Category
    {
        [Id]
        public int Id { get; set; }
    }

    [Entity]
    [Table("students")]
    private class Student
    {
        [Id]
        public int Id { get; set; }

        [ManyToMany]
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }

    [Entity]
    [Table("courses")]
    private class Course
    {
        [Id]
        public int Id { get; set; }

        [ManyToMany("Courses")]
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }

    [Fact]
    public void GetEntityMetadata_WithDefaultJoinColumn_ShouldGenerateCorrectName()
    {
        // Act
        var metadata = _metadataProvider.GetEntityMetadata<Product>();

        // Assert
        var relationship = metadata.Relationships["Category"];
        relationship.JoinColumn.Should().NotBeNull();
        relationship.JoinColumn!.Name.Should().Be("category_id");
    }

    [Fact]
    public void GetEntityMetadata_WithDefaultJoinTable_ShouldGenerateCorrectName()
    {
        // Act
        var metadata = _metadataProvider.GetEntityMetadata<Student>();

        // Assert
        var relationship = metadata.Relationships["Courses"];
        relationship.JoinTable.Should().NotBeNull();
        relationship.JoinTable!.Name.Should().Be("student_course");
        relationship.JoinTable.JoinColumns.Should().ContainSingle().Which.Should().Be("student_id");
        relationship.JoinTable.InverseJoinColumns.Should().ContainSingle().Which.Should().Be("course_id");
    }

    [Fact]
    public void JoinTableMetadata_FullName_WithSchema_ShouldReturnSchemaAndName()
    {
        // Arrange
        var joinTable = new JoinTableMetadata
        {
            Name = "user_roles",
            Schema = "public"
        };

        // Act & Assert
        joinTable.FullName.Should().Be("public.user_roles");
    }

    [Fact]
    public void JoinTableMetadata_FullName_WithoutSchema_ShouldReturnNameOnly()
    {
        // Arrange
        var joinTable = new JoinTableMetadata
        {
            Name = "user_roles",
            Schema = ""
        };

        // Act & Assert
        joinTable.FullName.Should().Be("user_roles");
    }
}

