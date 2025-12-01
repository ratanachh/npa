using Xunit;
using System.Linq;
using Microsoft.CodeAnalysis;
using NPA.Generators.Generators;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for bidirectional relationship validation method generation.
/// Bidirectional Relationship Management - Validation
/// </summary>
public class BidirectionalValidationTests : GeneratorTestBase
{
    [Fact]
    public void ValidationMethod_ShouldBeGenerated()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using System.Collections.Generic;

namespace TestNamespace;

[Entity]
[Table(""customers"")]
public class Customer
{
    [Id]
    public int Id { get; set; }
    
    [OneToMany(MappedBy = ""Customer"")]
    public List<Order> Orders { get; set; }
}

[Entity]
[Table(""orders"")]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    [Column(""customer_id"")]
    public int CustomerId { get; set; }
    
    [ManyToOne]
    [JoinColumn(""customer_id"")]
    public Customer Customer { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<BidirectionalRelationshipGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var orderHelper = outputCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.Contains("OrderRelationshipHelper"));
        Assert.NotNull(orderHelper);
        var code = orderHelper.ToString();

        // Verify validation method exists
        Assert.Contains("ValidateRelationshipConsistency", code);
        Assert.Contains("Validation Methods", code);
        Assert.Contains("InvalidOperationException", code);
    }

    [Fact]
    public void ValidationMethod_ShouldCheckForeignKeyConsistency()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;

namespace TestNamespace;

[Entity]
[Table(""orders"")]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    [Column(""customer_id"")]
    public int CustomerId { get; set; }
    
    [ManyToOne]
    [JoinColumn(""customer_id"")]
    public Customer Customer { get; set; }
}

[Entity]
[Table(""customers"")]
public class Customer
{
    [Id]
    public int Id { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<BidirectionalRelationshipGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        var orderHelper = outputCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.Contains("OrderRelationshipHelper"));
        Assert.NotNull(orderHelper);
        var code = orderHelper.ToString();

        // Verify FK validation logic
        Assert.Contains("CustomerId", code);
        Assert.Contains("expectedFk", code);
        Assert.Contains("does not match", code);
    }

    [Fact]
    public void ValidationMethod_ShouldCheckNullConsistency()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;

namespace TestNamespace;

[Entity]
[Table(""orders"")]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    [Column(""customer_id"")]
    public int CustomerId { get; set; }
    
    [ManyToOne]
    [JoinColumn(""customer_id"")]
    public Customer Customer { get; set; }
}

[Entity]
public class Customer
{
    [Id]
    public int Id { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<BidirectionalRelationshipGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        var orderHelper = outputCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.Contains("OrderRelationshipHelper"));
        Assert.NotNull(orderHelper);
        var code = orderHelper.ToString();

        // Verify null validation logic
        Assert.Contains("Customer is null", code);
        Assert.Contains("else if", code);
    }

    [Fact]
    public void ValidationMethod_ShouldHandleMultipleRelationships()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;

namespace TestNamespace;

[Entity]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    [Column(""customer_id"")]
    public int CustomerId { get; set; }
    
    [Column(""shipper_id"")]
    public int ShipperId { get; set; }
    
    [ManyToOne]
    [JoinColumn(""customer_id"")]
    public Customer Customer { get; set; }
    
    [ManyToOne]
    [JoinColumn(""shipper_id"")]
    public Shipper Shipper { get; set; }
}

[Entity]
public class Customer
{
    [Id]
    public int Id { get; set; }
}

[Entity]
public class Shipper
{
    [Id]
    public int Id { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<BidirectionalRelationshipGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        var orderHelper = outputCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.Contains("OrderRelationshipHelper"));
        Assert.NotNull(orderHelper);
        var code = orderHelper.ToString();

        // Verify both relationships are validated
        Assert.Contains("Validate Customer consistency", code);
        Assert.Contains("Validate Shipper consistency", code);
    }
}
