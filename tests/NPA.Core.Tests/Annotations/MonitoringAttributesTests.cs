using FluentAssertions;
using NPA.Core.Annotations;
using Xunit;

namespace NPA.Core.Tests.Annotations;

public class MonitoringAttributesTests
{
    #region PerformanceMonitorAttribute Tests

    [Fact]
    public void PerformanceMonitorAttribute_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var attribute = new PerformanceMonitorAttribute();

        // Assert
        attribute.IncludeParameters.Should().BeFalse();
        attribute.WarnThresholdMs.Should().Be(0);
        attribute.Category.Should().BeNull();
        attribute.TrackMemory.Should().BeFalse();
        attribute.TrackQueryCount.Should().BeFalse();
        attribute.MetricName.Should().BeNull();
    }

    [Fact]
    public void PerformanceMonitorAttribute_ConstructorWithThreshold_ShouldSetThreshold()
    {
        // Arrange & Act
        var attribute = new PerformanceMonitorAttribute(1000);

        // Assert
        attribute.WarnThresholdMs.Should().Be(1000);
    }

    [Fact]
    public void PerformanceMonitorAttribute_SetAllProperties_ShouldRetainValues()
    {
        // Arrange & Act
        var attribute = new PerformanceMonitorAttribute
        {
            IncludeParameters = true,
            WarnThresholdMs = 500,
            Category = "Database",
            TrackMemory = true,
            TrackQueryCount = true,
            MetricName = "CustomMetric"
        };

        // Assert
        attribute.IncludeParameters.Should().BeTrue();
        attribute.WarnThresholdMs.Should().Be(500);
        attribute.Category.Should().Be("Database");
        attribute.TrackMemory.Should().BeTrue();
        attribute.TrackQueryCount.Should().BeTrue();
        attribute.MetricName.Should().Be("CustomMetric");
    }

    [Fact]
    public void PerformanceMonitorAttribute_AttributeUsage_ShouldBeMethod()
    {
        // Arrange & Act
        var attributeUsage = typeof(PerformanceMonitorAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Assert
        attributeUsage.ValidOn.Should().Be(AttributeTargets.Method);
        attributeUsage.AllowMultiple.Should().BeFalse();
    }

    #endregion

    #region AuditAttribute Tests

    [Fact]
    public void AuditAttribute_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var attribute = new AuditAttribute();

        // Assert
        attribute.IncludeOldValue.Should().BeFalse();
        attribute.IncludeNewValue.Should().BeTrue();
        attribute.Category.Should().Be("Data");
        attribute.Severity.Should().Be(AuditSeverity.Normal);
        attribute.IncludeParameters.Should().BeTrue();
        attribute.CaptureUser.Should().BeTrue();
        attribute.Description.Should().BeNull();
        attribute.CaptureIpAddress.Should().BeFalse();
    }

    [Fact]
    public void AuditAttribute_ConstructorWithCategory_ShouldSetCategory()
    {
        // Arrange & Act
        var attribute = new AuditAttribute("Security");

        // Assert
        attribute.Category.Should().Be("Security");
    }

    [Fact]
    public void AuditAttribute_SetAllProperties_ShouldRetainValues()
    {
        // Arrange & Act
        var attribute = new AuditAttribute
        {
            IncludeOldValue = true,
            IncludeNewValue = false,
            Category = "Security",
            Severity = AuditSeverity.High,
            IncludeParameters = false,
            CaptureUser = false,
            Description = "Critical security action",
            CaptureIpAddress = true
        };

        // Assert
        attribute.IncludeOldValue.Should().BeTrue();
        attribute.IncludeNewValue.Should().BeFalse();
        attribute.Category.Should().Be("Security");
        attribute.Severity.Should().Be(AuditSeverity.High);
        attribute.IncludeParameters.Should().BeFalse();
        attribute.CaptureUser.Should().BeFalse();
        attribute.Description.Should().Be("Critical security action");
        attribute.CaptureIpAddress.Should().BeTrue();
    }

    [Fact]
    public void AuditAttribute_AttributeUsage_ShouldBeMethod()
    {
        // Arrange & Act
        var attributeUsage = typeof(AuditAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Assert
        attributeUsage.ValidOn.Should().Be(AttributeTargets.Method);
        attributeUsage.AllowMultiple.Should().BeFalse();
    }

    #endregion

    #region AuditSeverity Enum Tests

    [Fact]
    public void AuditSeverity_ShouldHaveCorrectValues()
    {
        // Assert
        ((int)AuditSeverity.Low).Should().Be(0);
        ((int)AuditSeverity.Normal).Should().Be(1);
        ((int)AuditSeverity.High).Should().Be(2);
        ((int)AuditSeverity.Critical).Should().Be(3);
    }

    [Fact]
    public void AuditSeverity_ShouldHaveAllExpectedMembers()
    {
        // Arrange & Act
        var severityValues = Enum.GetValues(typeof(AuditSeverity)).Cast<AuditSeverity>().ToList();

        // Assert
        severityValues.Should().HaveCount(4);
        severityValues.Should().Contain(AuditSeverity.Low);
        severityValues.Should().Contain(AuditSeverity.Normal);
        severityValues.Should().Contain(AuditSeverity.High);
        severityValues.Should().Contain(AuditSeverity.Critical);
    }

    #endregion
}
