using NPA.Core.Annotations;
using Xunit;

namespace NPA.Core.Tests.Annotations;

public class OneToOneAttributeTests
{
    [Fact]
    public void OneToOneAttribute_DefaultValues_AreCorrect()
    {
        // Arrange
        var attribute = new OneToOneAttribute();

        // Assert
        Assert.Null(attribute.MappedBy);
        Assert.Equal(CascadeType.None, attribute.Cascade);
        Assert.Equal(FetchType.Eager, attribute.Fetch); // Default for OneToOne is Eager
        Assert.True(attribute.Optional);
    }

    [Fact]
    public void OneToOneAttribute_Properties_CanBeSet()
    {
        // Arrange
        var attribute = new OneToOneAttribute
        {
            MappedBy = "User",
            Cascade = CascadeType.All,
            Fetch = FetchType.Lazy,
            Optional = false
        };

        // Assert
        Assert.Equal("User", attribute.MappedBy);
        Assert.Equal(CascadeType.All, attribute.Cascade);
        Assert.Equal(FetchType.Lazy, attribute.Fetch);
        Assert.False(attribute.Optional);
    }
}
