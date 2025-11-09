using FluentAssertions;
using NPA.Core.Annotations;
using Xunit;

namespace NPA.Core.Tests.Annotations;

public class CustomGeneratorAttributesTests
{
    [Fact]
    public void GeneratedMethodAttribute_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var attr = new GeneratedMethodAttribute();

        // Assert
        attr.IncludeNullCheck.Should().BeTrue();
        attr.GenerateAsync.Should().BeFalse();
        attr.GenerateSync.Should().BeFalse();
        attr.CustomSql.Should().BeNull();
        attr.IncludeLogging.Should().BeFalse();
        attr.IncludeErrorHandling.Should().BeFalse();
        attr.Description.Should().BeNull();
    }

    [Fact]
    public void GeneratedMethodAttribute_WithCustomValues_ShouldSetCorrectly()
    {
        // Arrange & Act
        var attr = new GeneratedMethodAttribute
        {
            IncludeNullCheck = false,
            GenerateAsync = true,
            CustomSql = "SELECT * FROM users",
            IncludeLogging = true,
            Description = "Custom method"
        };

        // Assert
        attr.IncludeNullCheck.Should().BeFalse();
        attr.GenerateAsync.Should().BeTrue();
        attr.CustomSql.Should().Be("SELECT * FROM users");
        attr.IncludeLogging.Should().BeTrue();
        attr.Description.Should().Be("Custom method");
    }

    [Fact]
    public void IgnoreInGenerationAttribute_DefaultConstructor_ShouldHaveNullReason()
    {
        // Arrange & Act
        var attr = new IgnoreInGenerationAttribute();

        // Assert
        attr.Reason.Should().BeNull();
    }

    [Fact]
    public void IgnoreInGenerationAttribute_WithReason_ShouldSetCorrectly()
    {
        // Arrange & Act
        var attr = new IgnoreInGenerationAttribute("Temporary property");

        // Assert
        attr.Reason.Should().Be("Temporary property");
    }

    [Fact]
    public void CustomImplementationAttribute_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var attr = new CustomImplementationAttribute();

        // Assert
        attr.GeneratePartialStub.Should().BeTrue();
        attr.ImplementationHint.Should().BeNull();
        attr.Required.Should().BeTrue();
    }

    [Fact]
    public void CustomImplementationAttribute_WithHint_ShouldSetCorrectly()
    {
        // Arrange & Act
        var attr = new CustomImplementationAttribute("Implement complex business logic here");

        // Assert
        attr.ImplementationHint.Should().Be("Implement complex business logic here");
        attr.Required.Should().BeTrue();
    }

    [Fact]
    public void CustomImplementationAttribute_WithCustomValues_ShouldSetCorrectly()
    {
        // Arrange & Act
        var attr = new CustomImplementationAttribute
        {
            GeneratePartialStub = false,
            Required = false,
            ImplementationHint = "Optional implementation"
        };

        // Assert
        attr.GeneratePartialStub.Should().BeFalse();
        attr.Required.Should().BeFalse();
        attr.ImplementationHint.Should().Be("Optional implementation");
    }

    [Fact]
    public void CacheResultAttribute_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var attr = new CacheResultAttribute();

        // Assert
        attr.Duration.Should().Be(300);
        attr.KeyPattern.Should().BeNull();
        attr.Region.Should().BeNull();
        attr.CacheNulls.Should().BeFalse();
        attr.Priority.Should().Be(0);
        attr.SlidingExpiration.Should().BeFalse();
    }

    [Fact]
    public void CacheResultAttribute_WithDuration_ShouldSetCorrectly()
    {
        // Arrange & Act
        var attr = new CacheResultAttribute(600);

        // Assert
        attr.Duration.Should().Be(600);
    }

    [Fact]
    public void CacheResultAttribute_WithCustomValues_ShouldSetCorrectly()
    {
        // Arrange & Act
        var attr = new CacheResultAttribute
        {
            Duration = 120,
            KeyPattern = "user:id:{id}",
            Region = "Users",
            CacheNulls = true,
            Priority = 10,
            SlidingExpiration = true
        };

        // Assert
        attr.Duration.Should().Be(120);
        attr.KeyPattern.Should().Be("user:id:{id}");
        attr.Region.Should().Be("Users");
        attr.CacheNulls.Should().BeTrue();
        attr.Priority.Should().Be(10);
        attr.SlidingExpiration.Should().BeTrue();
    }

    [Fact]
    public void ValidateParametersAttribute_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var attr = new ValidateParametersAttribute();

        // Assert
        attr.ThrowOnNull.Should().BeTrue();
        attr.ValidateStringsNotEmpty.Should().BeFalse();
        attr.ValidateCollectionsNotEmpty.Should().BeFalse();
        attr.ValidatePositive.Should().BeFalse();
        attr.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateParametersAttribute_WithCustomValues_ShouldSetCorrectly()
    {
        // Arrange & Act
        var attr = new ValidateParametersAttribute
        {
            ThrowOnNull = false,
            ValidateStringsNotEmpty = true,
            ValidateCollectionsNotEmpty = true,
            ValidatePositive = true,
            ErrorMessage = "Invalid parameter: {paramName}"
        };

        // Assert
        attr.ThrowOnNull.Should().BeFalse();
        attr.ValidateStringsNotEmpty.Should().BeTrue();
        attr.ValidateCollectionsNotEmpty.Should().BeTrue();
        attr.ValidatePositive.Should().BeTrue();
        attr.ErrorMessage.Should().Be("Invalid parameter: {paramName}");
    }

    [Fact]
    public void RetryOnFailureAttribute_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var attr = new RetryOnFailureAttribute();

        // Assert
        attr.MaxAttempts.Should().Be(3);
        attr.DelayMilliseconds.Should().Be(100);
        attr.ExponentialBackoff.Should().BeTrue();
        attr.MaxDelayMilliseconds.Should().Be(30000);
        attr.RetryOn.Should().BeNull();
        attr.LogRetries.Should().BeTrue();
    }

    [Fact]
    public void RetryOnFailureAttribute_WithMaxAttempts_ShouldSetCorrectly()
    {
        // Arrange & Act
        var attr = new RetryOnFailureAttribute(5);

        // Assert
        attr.MaxAttempts.Should().Be(5);
    }

    [Fact]
    public void RetryOnFailureAttribute_WithCustomValues_ShouldSetCorrectly()
    {
        // Arrange & Act
        var attr = new RetryOnFailureAttribute
        {
            MaxAttempts = 10,
            DelayMilliseconds = 500,
            ExponentialBackoff = false,
            MaxDelayMilliseconds = 60000,
            LogRetries = false,
            RetryOn = new[] { typeof(TimeoutException), typeof(InvalidOperationException) }
        };

        // Assert
        attr.MaxAttempts.Should().Be(10);
        attr.DelayMilliseconds.Should().Be(500);
        attr.ExponentialBackoff.Should().BeFalse();
        attr.MaxDelayMilliseconds.Should().Be(60000);
        attr.LogRetries.Should().BeFalse();
        attr.RetryOn.Should().HaveCount(2);
        attr.RetryOn.Should().Contain(typeof(TimeoutException));
        attr.RetryOn.Should().Contain(typeof(InvalidOperationException));
    }

    [Fact]
    public void TransactionScopeAttribute_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var attr = new TransactionScopeAttribute();

        // Assert
        attr.Required.Should().BeTrue();
        attr.IsolationLevel.Should().Be(System.Data.IsolationLevel.ReadCommitted);
        attr.TimeoutSeconds.Should().Be(30);
        attr.AutoRollbackOnError.Should().BeTrue();
        attr.JoinAmbientTransaction.Should().BeTrue();
    }

    [Fact]
    public void TransactionScopeAttribute_WithIsolationLevel_ShouldSetCorrectly()
    {
        // Arrange & Act
        var attr = new TransactionScopeAttribute(System.Data.IsolationLevel.Serializable);

        // Assert
        attr.IsolationLevel.Should().Be(System.Data.IsolationLevel.Serializable);
    }

    [Fact]
    public void TransactionScopeAttribute_WithCustomValues_ShouldSetCorrectly()
    {
        // Arrange & Act
        var attr = new TransactionScopeAttribute
        {
            Required = false,
            IsolationLevel = System.Data.IsolationLevel.ReadUncommitted,
            TimeoutSeconds = 60,
            AutoRollbackOnError = false,
            JoinAmbientTransaction = false
        };

        // Assert
        attr.Required.Should().BeFalse();
        attr.IsolationLevel.Should().Be(System.Data.IsolationLevel.ReadUncommitted);
        attr.TimeoutSeconds.Should().Be(60);
        attr.AutoRollbackOnError.Should().BeFalse();
        attr.JoinAmbientTransaction.Should().BeFalse();
    }

    [Fact]
    public void GeneratedMethodAttribute_CanBeAppliedToMethod()
    {
        // This test verifies the AttributeUsage is correct
        var attributeUsage = typeof(GeneratedMethodAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().Be(AttributeTargets.Method);
        attributeUsage.AllowMultiple.Should().BeFalse();
    }

    [Fact]
    public void IgnoreInGenerationAttribute_CanBeAppliedToMultipleTargets()
    {
        // This test verifies the AttributeUsage is correct
        var attributeUsage = typeof(IgnoreInGenerationAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        attributeUsage.Should().NotBeNull();
        var validTargets = AttributeTargets.Property | AttributeTargets.Method | 
                          AttributeTargets.Class | AttributeTargets.Field;
        attributeUsage!.ValidOn.Should().Be(validTargets);
        attributeUsage.AllowMultiple.Should().BeFalse();
    }
}
