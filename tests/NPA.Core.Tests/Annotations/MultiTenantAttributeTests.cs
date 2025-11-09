using FluentAssertions;
using NPA.Core.Annotations;
using Xunit;

namespace NPA.Core.Tests.Annotations;

public class MultiTenantAttributeTests
{
    [Fact]
    public void MultiTenantAttribute_ShouldHaveDefaultValues()
    {
        // Act
        var attribute = new MultiTenantAttribute();

        // Assert
        attribute.TenantIdProperty.Should().Be("TenantId");
        attribute.EnforceTenantIsolation.Should().BeTrue();
        attribute.AllowCrossTenantQueries.Should().BeFalse();
        attribute.ValidateTenantOnWrite.Should().BeTrue();
        attribute.AutoPopulateTenantId.Should().BeTrue();
    }

    [Fact]
    public void MultiTenantAttribute_ShouldAcceptCustomTenantIdProperty()
    {
        // Act
        var attribute = new MultiTenantAttribute("CustomTenantId");

        // Assert
        attribute.TenantIdProperty.Should().Be("CustomTenantId");
    }

    [Fact]
    public void MultiTenantAttribute_ShouldAllowSettingProperties()
    {
        // Act
        var attribute = new MultiTenantAttribute
        {
            EnforceTenantIsolation = false,
            AllowCrossTenantQueries = true,
            ValidateTenantOnWrite = false,
            AutoPopulateTenantId = false
        };

        // Assert
        attribute.EnforceTenantIsolation.Should().BeFalse();
        attribute.AllowCrossTenantQueries.Should().BeTrue();
        attribute.ValidateTenantOnWrite.Should().BeFalse();
        attribute.AutoPopulateTenantId.Should().BeFalse();
    }

    [Fact]
    public void MultiTenantAttribute_ShouldHaveCorrectAttributeUsage()
    {
        // Arrange
        var attributeType = typeof(MultiTenantAttribute);

        // Act
        var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            attributeType, typeof(AttributeUsageAttribute))!;

        // Assert
        usage.Should().NotBeNull();
        usage.ValidOn.Should().Be(AttributeTargets.Class);
        usage.AllowMultiple.Should().BeFalse();
        usage.Inherited.Should().BeTrue();
    }
}
