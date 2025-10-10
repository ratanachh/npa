using NPA.Core.Core;
using Xunit;

namespace NPA.Core.Tests.CompositeKeys;

/// <summary>
/// Tests for the CompositeKey class.
/// </summary>
public class CompositeKeyTests
{
    [Fact]
    public void CompositeKey_Create_ShouldInitializeEmpty()
    {
        // Arrange & Act
        var compositeKey = new CompositeKey();

        // Assert
        Assert.NotNull(compositeKey);
        Assert.NotNull(compositeKey.Values);
        Assert.Empty(compositeKey.Values);
    }

    [Fact]
    public void CompositeKey_SetValue_ShouldAddValue()
    {
        // Arrange
        var compositeKey = new CompositeKey();

        // Act
        compositeKey.SetValue("OrderId", 1L);
        compositeKey.SetValue("ProductId", 100L);

        // Assert
        Assert.Equal(2, compositeKey.Values.Count);
        Assert.Equal(1L, compositeKey.GetValue("OrderId"));
        Assert.Equal(100L, compositeKey.GetValue("ProductId"));
    }

    [Fact]
    public void CompositeKey_SetValue_NullValue_ShouldRemoveKey()
    {
        // Arrange
        var compositeKey = new CompositeKey();
        compositeKey.SetValue("OrderId", 1L);

        // Act
        compositeKey.SetValue("OrderId", null);

        // Assert
        Assert.Empty(compositeKey.Values);
    }

    [Fact]
    public void CompositeKey_GetValue_NonExistentKey_ShouldReturnNull()
    {
        // Arrange
        var compositeKey = new CompositeKey();

        // Act
        var value = compositeKey.GetValue("NonExistent");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void CompositeKey_Equals_SameValues_ShouldReturnTrue()
    {
        // Arrange
        var key1 = new CompositeKey();
        key1.SetValue("OrderId", 1L);
        key1.SetValue("ProductId", 100L);

        var key2 = new CompositeKey();
        key2.SetValue("OrderId", 1L);
        key2.SetValue("ProductId", 100L);

        // Act & Assert
        Assert.True(key1.Equals(key2));
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    [Fact]
    public void CompositeKey_Equals_DifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var key1 = new CompositeKey();
        key1.SetValue("OrderId", 1L);
        key1.SetValue("ProductId", 100L);

        var key2 = new CompositeKey();
        key2.SetValue("OrderId", 1L);
        key2.SetValue("ProductId", 200L);

        // Act & Assert
        Assert.False(key1.Equals(key2));
    }

    [Fact]
    public void CompositeKey_Equals_DifferentNumberOfKeys_ShouldReturnFalse()
    {
        // Arrange
        var key1 = new CompositeKey();
        key1.SetValue("OrderId", 1L);

        var key2 = new CompositeKey();
        key2.SetValue("OrderId", 1L);
        key2.SetValue("ProductId", 100L);

        // Act & Assert
        Assert.False(key1.Equals(key2));
    }

    [Fact]
    public void CompositeKey_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var compositeKey = new CompositeKey();
        compositeKey.SetValue("OrderId", 1L);
        compositeKey.SetValue("ProductId", 100L);

        // Act
        var result = compositeKey.ToString();

        // Assert
        Assert.Contains("OrderId", result);
        Assert.Contains("ProductId", result);
        Assert.Contains("1", result);
        Assert.Contains("100", result);
    }
}

