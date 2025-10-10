using NPA.Core.Core;
using NPA.Core.Metadata;
using Xunit;

namespace NPA.Core.Tests.CompositeKeys;

/// <summary>
/// Tests for the CompositeKeyMetadata class.
/// </summary>
public class CompositeKeyMetadataTests
{
    [Fact]
    public void CompositeKeyMetadata_IsCompositeKey_MultipleProperties_ShouldReturnTrue()
    {
        // Arrange
        var metadata = new CompositeKeyMetadata
        {
            KeyProperties = new List<PropertyMetadata>
            {
                new PropertyMetadata { PropertyName = "OrderId", PropertyType = typeof(long), ColumnName = "order_id", IsPrimaryKey = true },
                new PropertyMetadata { PropertyName = "ProductId", PropertyType = typeof(long), ColumnName = "product_id", IsPrimaryKey = true }
            }
        };

        // Act & Assert
        Assert.True(metadata.IsCompositeKey);
        Assert.Equal(2, metadata.Count);
    }

    [Fact]
    public void CompositeKeyMetadata_KeyNames_ShouldReturnPropertyNames()
    {
        // Arrange
        var metadata = new CompositeKeyMetadata
        {
            KeyProperties = new List<PropertyMetadata>
            {
                new PropertyMetadata { PropertyName = "OrderId", PropertyType = typeof(long), ColumnName = "order_id" },
                new PropertyMetadata { PropertyName = "ProductId", PropertyType = typeof(long), ColumnName = "product_id" }
            }
        };

        // Act
        var keyNames = metadata.KeyNames;

        // Assert
        Assert.Equal(2, keyNames.Count);
        Assert.Contains("OrderId", keyNames);
        Assert.Contains("ProductId", keyNames);
    }

    [Fact]
    public void CompositeKeyMetadata_KeyColumns_ShouldReturnColumnNames()
    {
        // Arrange
        var metadata = new CompositeKeyMetadata
        {
            KeyProperties = new List<PropertyMetadata>
            {
                new PropertyMetadata { PropertyName = "OrderId", PropertyType = typeof(long), ColumnName = "order_id" },
                new PropertyMetadata { PropertyName = "ProductId", PropertyType = typeof(long), ColumnName = "product_id" }
            }
        };

        // Act
        var keyColumns = metadata.KeyColumns;

        // Assert
        Assert.Equal(2, keyColumns.Length);
        Assert.Contains("order_id", keyColumns);
        Assert.Contains("product_id", keyColumns);
    }

    [Fact]
    public void CompositeKeyMetadata_CreateCompositeKey_ShouldExtractValues()
    {
        // Arrange
        var metadata = new CompositeKeyMetadata
        {
            KeyProperties = new List<PropertyMetadata>
            {
                new PropertyMetadata { PropertyName = "OrderId", PropertyType = typeof(long), ColumnName = "order_id" },
                new PropertyMetadata { PropertyName = "ProductId", PropertyType = typeof(long), ColumnName = "product_id" }
            }
        };

        var entity = new OrderItemTestEntity
        {
            OrderId = 1L,
            ProductId = 100L,
            Quantity = 5
        };

        // Act
        var compositeKey = metadata.CreateCompositeKey(entity);

        // Assert
        Assert.NotNull(compositeKey);
        Assert.Equal(2, compositeKey.Values.Count);
        Assert.Equal(1L, compositeKey.GetValue("OrderId"));
        Assert.Equal(100L, compositeKey.GetValue("ProductId"));
    }

    [Fact]
    public void CompositeKeyMetadata_GenerateWhereClause_ShouldProduceCorrectSQL()
    {
        // Arrange
        var metadata = new CompositeKeyMetadata
        {
            KeyProperties = new List<PropertyMetadata>
            {
                new PropertyMetadata { PropertyName = "OrderId", PropertyType = typeof(long), ColumnName = "order_id" },
                new PropertyMetadata { PropertyName = "ProductId", PropertyType = typeof(long), ColumnName = "product_id" }
            }
        };

        // Act
        var whereClause = metadata.GenerateWhereClause();

        // Assert
        Assert.Contains("order_id = @OrderId", whereClause);
        Assert.Contains("product_id = @ProductId", whereClause);
        Assert.Contains(" AND ", whereClause);
    }

    [Fact]
    public void CompositeKeyMetadata_ExtractParameters_ShouldCreateDictionary()
    {
        // Arrange
        var metadata = new CompositeKeyMetadata
        {
            KeyProperties = new List<PropertyMetadata>
            {
                new PropertyMetadata { PropertyName = "OrderId", PropertyType = typeof(long), ColumnName = "order_id" },
                new PropertyMetadata { PropertyName = "ProductId", PropertyType = typeof(long), ColumnName = "product_id" }
            }
        };

        var compositeKey = new CompositeKey();
        compositeKey.SetValue("OrderId", 1L);
        compositeKey.SetValue("ProductId", 100L);

        // Act
        var parameters = metadata.ExtractParameters(compositeKey);

        // Assert
        Assert.Equal(2, parameters.Count);
        Assert.Equal(1L, parameters["OrderId"]);
        Assert.Equal(100L, parameters["ProductId"]);
    }

    [Fact]
    public void CompositeKeyMetadata_Validate_ValidKey_ShouldReturnTrue()
    {
        // Arrange
        var metadata = new CompositeKeyMetadata
        {
            KeyProperties = new List<PropertyMetadata>
            {
                new PropertyMetadata { PropertyName = "OrderId", PropertyType = typeof(long), ColumnName = "order_id" },
                new PropertyMetadata { PropertyName = "ProductId", PropertyType = typeof(long), ColumnName = "product_id" }
            }
        };

        var compositeKey = new CompositeKey();
        compositeKey.SetValue("OrderId", 1L);
        compositeKey.SetValue("ProductId", 100L);

        // Act
        var isValid = metadata.Validate(compositeKey);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void CompositeKeyMetadata_Validate_MissingKey_ShouldReturnFalse()
    {
        // Arrange
        var metadata = new CompositeKeyMetadata
        {
            KeyProperties = new List<PropertyMetadata>
            {
                new PropertyMetadata { PropertyName = "OrderId", PropertyType = typeof(long), ColumnName = "order_id" },
                new PropertyMetadata { PropertyName = "ProductId", PropertyType = typeof(long), ColumnName = "product_id" }
            }
        };

        var compositeKey = new CompositeKey();
        compositeKey.SetValue("OrderId", 1L);
        // Missing ProductId

        // Act
        var isValid = metadata.Validate(compositeKey);

        // Assert
        Assert.False(isValid);
    }
}

