using FluentAssertions;
using NPA.Core.Annotations;
using NPA.Core.Metadata;
using NPA.Core.Tests.TestEntities;
using Xunit;

namespace NPA.Core.Tests.Metadata;

/// <summary>
/// Unit tests for the MetadataProvider class.
/// </summary>
public class MetadataProviderTests
{
    private readonly MetadataProvider _metadataProvider = new();

    [Fact]
    public void IsEntity_WithEntityType_ShouldReturnTrue()
    {
        // Act
        var result = _metadataProvider.IsEntity(typeof(User));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEntity_WithNonEntityType_ShouldReturnFalse()
    {
        // Act
        var result = _metadataProvider.IsEntity(typeof(string));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEntity_WithNullType_ShouldReturnFalse()
    {
        // Act
        var result = _metadataProvider.IsEntity(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetEntityMetadata_WithValidEntityType_ShouldReturnMetadata()
    {
        // Act
        var metadata = _metadataProvider.GetEntityMetadata<User>();

        // Assert
        metadata.Should().NotBeNull();
        metadata.EntityType.Should().Be(typeof(User));
        metadata.TableName.Should().Be("users");
        metadata.SchemaName.Should().BeNull();
        metadata.PrimaryKeyProperty.Should().Be("Id");
        metadata.Properties.Should().HaveCount(5); // Id, Username, Email, CreatedAt, IsActive
    }

    [Fact]
    public void GetEntityMetadata_WithNonEntityType_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _metadataProvider.GetEntityMetadata(typeof(string));
        action.Should().Throw<ArgumentException>()
            .WithMessage("*is not marked as an entity*");
    }

    [Fact]
    public void GetEntityMetadata_WithNullType_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _metadataProvider.GetEntityMetadata(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetEntityMetadata_WithEntityWithoutId_ShouldThrowException()
    {
        // Arrange
        var entityType = typeof(EntityWithoutId);

        // Act & Assert
        var action = () => _metadataProvider.GetEntityMetadata(entityType);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*must have at least one property marked with [Id] attribute*");
    }

    [Fact]
    public void GetEntityMetadata_ShouldCacheResults()
    {
        // Act
        var metadata1 = _metadataProvider.GetEntityMetadata<User>();
        var metadata2 = _metadataProvider.GetEntityMetadata<User>();

        // Assert
        metadata1.Should().BeSameAs(metadata2);
    }

    [Fact]
    public void GetEntityMetadata_WithCompositeKey_ShouldReturnMetadata()
    {
        // Act
        var metadata = _metadataProvider.GetEntityMetadata<OrderItem>();

        // Assert
        metadata.Should().NotBeNull();
        metadata.EntityType.Should().Be(typeof(OrderItem));
        metadata.TableName.Should().Be("order_items");
        metadata.PrimaryKeyProperty.Should().Be("OrderId"); // First Id property (for backward compatibility)
        metadata.Properties.Should().HaveCount(4); // OrderId, ProductId, Quantity, UnitPrice
        
        var orderIdProperty = metadata.Properties["OrderId"];
        var productIdProperty = metadata.Properties["ProductId"];

        orderIdProperty.IsPrimaryKey.Should().BeTrue();
        productIdProperty.IsPrimaryKey.Should().BeTrue();
    }

    [Fact]
    public void GetEntityMetadata_ShouldMapColumnAttributesCorrectly()
    {
        // Act
        var metadata = _metadataProvider.GetEntityMetadata<User>();
        var usernameProperty = metadata.Properties["Username"];

        // Assert
        usernameProperty.ColumnName.Should().Be("username");
        usernameProperty.IsNullable.Should().BeTrue(); // Default value
    }

    [Fact]
    public void GetEntityMetadata_ShouldUseDefaultColumnNames()
    {
        // Act
        var metadata = _metadataProvider.GetEntityMetadata<User>();
        var emailProperty = metadata.Properties["Email"];

        // Assert
        emailProperty.ColumnName.Should().Be("email");
        emailProperty.IsNullable.Should().BeTrue(); // Default value
        emailProperty.IsUnique.Should().BeFalse(); // Default value
    }

    [Fact]
    public void GetEntityMetadata_ShouldConvertCamelCaseToSnakeCase()
    {
        // Act
        var metadata = _metadataProvider.GetEntityMetadata<User>();
        var createdAtProperty = metadata.Properties["CreatedAt"];

        // Assert
        createdAtProperty.ColumnName.Should().Be("created_at");
    }
}

/// <summary>
/// Test entity without Id attribute for testing error scenarios.
/// </summary>
[Entity]
[Table("test_entity")]
public class EntityWithoutId
{
    public string Name { get; set; } = string.Empty;
}
