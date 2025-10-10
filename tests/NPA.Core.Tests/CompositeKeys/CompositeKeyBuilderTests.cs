using NPA.Core.Core;
using NPA.Core.Metadata;
using Xunit;

namespace NPA.Core.Tests.CompositeKeys;

/// <summary>
/// Tests for the CompositeKeyBuilder class.
/// </summary>
public class CompositeKeyBuilderTests
{
    [Fact]
    public void CompositeKeyBuilder_Create_ShouldReturnBuilder()
    {
        // Act
        var builder = CompositeKeyBuilder.Create();

        // Assert
        Assert.NotNull(builder);
    }

    [Fact]
    public void CompositeKeyBuilder_WithKey_ShouldAddKey()
    {
        // Arrange
        var builder = CompositeKeyBuilder.Create();

        // Act
        var result = builder
            .WithKey("OrderId", 1L)
            .WithKey("ProductId", 100L);

        // Assert
        Assert.Same(builder, result); // Fluent API
    }

    [Fact]
    public void CompositeKeyBuilder_Build_ShouldCreateCompositeKey()
    {
        // Arrange
        var builder = CompositeKeyBuilder.Create()
            .WithKey("OrderId", 1L)
            .WithKey("ProductId", 100L);

        // Act
        var compositeKey = builder.Build();

        // Assert
        Assert.NotNull(compositeKey);
        Assert.Equal(2, compositeKey.Values.Count);
        Assert.Equal(1L, compositeKey.GetValue("OrderId"));
        Assert.Equal(100L, compositeKey.GetValue("ProductId"));
    }

    [Fact]
    public void CompositeKeyBuilder_Build_NoKeys_ShouldThrow()
    {
        // Arrange
        var builder = CompositeKeyBuilder.Create();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void CompositeKeyBuilder_WithKey_NullPropertyName_ShouldThrow()
    {
        // Arrange
        var builder = CompositeKeyBuilder.Create();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithKey(null!, 1L));
    }

    [Fact]
    public void CompositeKeyBuilder_WithKey_NullValue_ShouldThrow()
    {
        // Arrange
        var builder = CompositeKeyBuilder.Create();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithKey("OrderId", null!));
    }

    [Fact]
    public void CompositeKeyBuilder_FromEntity_ShouldExtractKeyValues()
    {
        // Arrange
        var entity = new OrderItemTestEntity
        {
            OrderId = 1L,
            ProductId = 100L,
            Quantity = 5,
            UnitPrice = 29.99m
        };

        var metadata = new EntityMetadata
        {
            EntityType = typeof(OrderItemTestEntity),
            CompositeKeyMetadata = new CompositeKeyMetadata
            {
                KeyProperties = new List<PropertyMetadata>
                {
                    new PropertyMetadata { PropertyName = "OrderId", PropertyType = typeof(long), ColumnName = "order_id" },
                    new PropertyMetadata { PropertyName = "ProductId", PropertyType = typeof(long), ColumnName = "product_id" }
                }
            }
        };

        // Act
        var builder = CompositeKeyBuilder.FromEntity(entity, metadata);
        var compositeKey = builder.Build();

        // Assert
        Assert.NotNull(compositeKey);
        Assert.Equal(2, compositeKey.Values.Count);
        Assert.Equal(1L, compositeKey.GetValue("OrderId"));
        Assert.Equal(100L, compositeKey.GetValue("ProductId"));
    }

    [Fact]
    public void CompositeKeyBuilder_FromEntity_NoCompositeKey_ShouldThrow()
    {
        // Arrange
        var entity = new OrderItemTestEntity();
        var metadata = new EntityMetadata
        {
            EntityType = typeof(OrderItemTestEntity),
            CompositeKeyMetadata = null // No composite key
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            CompositeKeyBuilder.FromEntity(entity, metadata));
    }

    [Fact]
    public void CompositeKeyBuilder_FluentAPI_ShouldChainCorrectly()
    {
        // Arrange & Act
        var compositeKey = CompositeKeyBuilder.Create()
            .WithKey("Part1", "A")
            .WithKey("Part2", 10)
            .WithKey("Part3", DateTime.Parse("2025-01-10"))
            .Build();

        // Assert
        Assert.Equal(3, compositeKey.Values.Count);
        Assert.Equal("A", compositeKey.GetValue("Part1"));
        Assert.Equal(10, compositeKey.GetValue("Part2"));
        Assert.IsType<DateTime>(compositeKey.GetValue("Part3"));
    }
}

