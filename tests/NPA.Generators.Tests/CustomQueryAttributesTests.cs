using FluentAssertions;
using NPA.Core.Annotations;
using Xunit;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for Phase 4.1 custom query attributes.
/// </summary>
public class CustomQueryAttributesTests
{
    [Fact]
    public void QueryAttribute_ShouldStoreSQL()
    {
        // Arrange
        var sql = "SELECT * FROM users WHERE email = @email";

        // Act
        var attr = new QueryAttribute(sql);

        // Assert
        attr.Sql.Should().Be(sql);
        attr.Buffered.Should().BeTrue("default value should be true");
        attr.CommandTimeout.Should().BeNull("default value should be null");
    }

    [Fact]
    public void QueryAttribute_ShouldAllowConfiguration()
    {
        // Arrange
        var sql = "SELECT * FROM users";
        
        // Act
        var attr = new QueryAttribute(sql)
        {
            Buffered = false,
            CommandTimeout = 30
        };

        // Assert
        attr.Buffered.Should().BeFalse();
        attr.CommandTimeout.Should().Be(30);
    }

    [Fact]
    public void QueryAttribute_ShouldThrowOnNullOrEmptySQL()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new QueryAttribute(null!));
        Assert.Throws<ArgumentException>(() => new QueryAttribute(""));
        Assert.Throws<ArgumentException>(() => new QueryAttribute("   "));
    }

    [Fact]
    public void StoredProcedureAttribute_ShouldStoreProcedureName()
    {
        // Arrange
        var procedureName = "sp_GetUsers";

        // Act
        var attr = new StoredProcedureAttribute(procedureName);

        // Assert
        attr.ProcedureName.Should().Be(procedureName);
        attr.Schema.Should().BeNull();
        attr.CommandTimeout.Should().BeNull();
    }

    [Fact]
    public void StoredProcedureAttribute_ShouldAllowSchemaConfiguration()
    {
        // Arrange
        var procedureName = "GetUsers";
        var schema = "dbo";

        // Act
        var attr = new StoredProcedureAttribute(procedureName)
        {
            Schema = schema,
            CommandTimeout = 60
        };

        // Assert
        attr.ProcedureName.Should().Be(procedureName);
        attr.Schema.Should().Be(schema);
        attr.CommandTimeout.Should().Be(60);
    }

    [Fact]
    public void StoredProcedureAttribute_ShouldThrowOnNullOrEmptyName()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new StoredProcedureAttribute(null!));
        Assert.Throws<ArgumentException>(() => new StoredProcedureAttribute(""));
        Assert.Throws<ArgumentException>(() => new StoredProcedureAttribute("   "));
    }

    [Fact]
    public void MultiMappingAttribute_ShouldStoreKeyProperty()
    {
        // Arrange
        var keyProperty = "UserId";

        // Act
        var attr = new MultiMappingAttribute(keyProperty);

        // Assert
        attr.KeyProperty.Should().Be(keyProperty);
        attr.SplitOn.Should().BeNull();
        attr.MapTypes.Should().BeNull();
    }

    [Fact]
    public void MultiMappingAttribute_ShouldAllowSplitOnConfiguration()
    {
        // Arrange
        var keyProperty = "Id";
        var splitOn = "AddressId,OrderId";

        // Act
        var attr = new MultiMappingAttribute(keyProperty)
        {
            SplitOn = splitOn
        };

        // Assert
        attr.KeyProperty.Should().Be(keyProperty);
        attr.SplitOn.Should().Be(splitOn);
    }

    [Fact]
    public void MultiMappingAttribute_ShouldThrowOnNullOrEmptyKey()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new MultiMappingAttribute(null!));
        Assert.Throws<ArgumentException>(() => new MultiMappingAttribute(""));
        Assert.Throws<ArgumentException>(() => new MultiMappingAttribute("   "));
    }

    [Fact]
    public void BulkOperationAttribute_ShouldHaveDefaultValues()
    {
        // Act
        var attr = new BulkOperationAttribute();

        // Assert
        attr.BatchSize.Should().Be(1000, "default batch size should be 1000");
        attr.UseTransaction.Should().BeTrue("default should use transactions");
        attr.CommandTimeout.Should().BeNull();
    }

    [Fact]
    public void BulkOperationAttribute_ShouldAllowConfiguration()
    {
        // Act
        var attr = new BulkOperationAttribute
        {
            BatchSize = 500,
            UseTransaction = false,
            CommandTimeout = 120
        };

        // Assert
        attr.BatchSize.Should().Be(500);
        attr.UseTransaction.Should().BeFalse();
        attr.CommandTimeout.Should().Be(120);
    }

    [Fact]
    public void QueryAttribute_ShouldTargetMethods()
    {
        // Arrange
        var attributeType = typeof(QueryAttribute);
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(attributeType, typeof(AttributeUsageAttribute));

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Method);
    }

    [Fact]
    public void StoredProcedureAttribute_ShouldTargetMethods()
    {
        // Arrange
        var attributeType = typeof(StoredProcedureAttribute);
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(attributeType, typeof(AttributeUsageAttribute));

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Method);
    }

    [Fact]
    public void MultiMappingAttribute_ShouldTargetMethods()
    {
        // Arrange
        var attributeType = typeof(MultiMappingAttribute);
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(attributeType, typeof(AttributeUsageAttribute));

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Method);
    }

    [Fact]
    public void BulkOperationAttribute_ShouldTargetMethods()
    {
        // Arrange
        var attributeType = typeof(BulkOperationAttribute);
        var attributeUsage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(attributeType, typeof(AttributeUsageAttribute));

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().HaveFlag(AttributeTargets.Method);
    }
}
