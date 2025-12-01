using Xunit;
using System.Linq;
using Microsoft.CodeAnalysis;
using NPA.Generators.Generators;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for bidirectional relationship synchronization helper generation.
/// Bidirectional Relationship Management
/// </summary>
public class BidirectionalRelationshipGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void BidirectionalOneToMany_ShouldGenerateHelperMethods()
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
    
    [Column(""name"")]
    public string Name { get; set; }
    
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
        
        var generatedTrees = outputCompilation.SyntaxTrees.Where(t => t.FilePath.Contains("RelationshipHelper")).ToList();
        Assert.NotEmpty(generatedTrees);
        
        // Check CustomerRelationshipHelper
        var customerHelper = generatedTrees.FirstOrDefault(t => t.FilePath.Contains("CustomerRelationshipHelper"));
        Assert.NotNull(customerHelper);
        var customerCode = customerHelper.ToString();
        
        Assert.Contains("class CustomerRelationshipHelper", customerCode);
        Assert.Contains("AddToOrders", customerCode);
        Assert.Contains("RemoveFromOrders", customerCode);
        
        // Check OrderRelationshipHelper  
        var orderHelper = generatedTrees.FirstOrDefault(t => t.FilePath.Contains("OrderRelationshipHelper"));
        Assert.NotNull(orderHelper);
        var orderCode = orderHelper.ToString();
        
        Assert.Contains("class OrderRelationshipHelper", orderCode);
        Assert.Contains("SetCustomer", orderCode);
    }

    [Fact]
    public void BidirectionalOneToOne_ShouldGenerateHelperMethods()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;

namespace TestNamespace;

[Entity]
[Table(""users"")]
public class User
{
    [Id]
    public int Id { get; set; }
    
    [OneToOne(MappedBy = ""User"")]
    public UserProfile Profile { get; set; }
}

[Entity]
[Table(""user_profiles"")]
public class UserProfile
{
    [Id]
    public int Id { get; set; }
    
    [Column(""user_id"")]
    public int UserId { get; set; }
    
    [OneToOne]
    [JoinColumn(""user_id"")]
    public User User { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<BidirectionalRelationshipGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var generatedTrees = outputCompilation.SyntaxTrees.Where(t => t.FilePath.Contains("RelationshipHelper")).ToList();
        Assert.NotEmpty(generatedTrees);
        
        // Check UserProfileRelationshipHelper
        var profileHelper = generatedTrees.FirstOrDefault(t => t.FilePath.Contains("UserProfileRelationshipHelper"));
        Assert.NotNull(profileHelper);
        var profileCode = profileHelper.ToString();
        
        Assert.Contains("class UserProfileRelationshipHelper", profileCode);
        Assert.Contains("SetUser", profileCode);
    }

    [Fact]
    public void NoBidirectionalRelationships_ShouldNotGenerateHelpers()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;

namespace TestNamespace;

[Entity]
[Table(""products"")]
public class Product
{
    [Id]
    public int Id { get; set; }
    
    [Column(""name"")]
    public string Name { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<BidirectionalRelationshipGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var generatedTrees = outputCompilation.SyntaxTrees.Where(t => t.FilePath.Contains("RelationshipHelper")).ToList();
        Assert.Empty(generatedTrees); // No helpers should be generated
    }

    [Fact]
    public void HelperMethods_ShouldInitializeCollections()
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
    
    [ManyToOne]
    [JoinColumn(""customer_id"")]
    public Customer Customer { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<BidirectionalRelationshipGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        var customerHelper = outputCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.Contains("CustomerRelationshipHelper"));
        Assert.NotNull(customerHelper);
        var code = customerHelper.ToString();
        
        // Verify collection initialization
        Assert.Contains("Orders ??=", code);
        Assert.Contains("new System.Collections.Generic.List", code);
    }

    [Fact]
    public void HelperMethods_ShouldSetForeignKey()
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
        var orderHelper = outputCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.Contains("OrderRelationshipHelper"));
        Assert.NotNull(orderHelper);
        var code = orderHelper.ToString();
        
        // Verify FK synchronization
        Assert.Contains("CustomerId", code);
        Assert.Contains("value?.Id ?? 0", code);
    }

    [Fact]
    public void SetMethod_WithNullableProperty_ShouldNotUseNullForgivingOperator()
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
    
    [ManyToOne]
    [JoinColumn(""customer_id"")]
    public Customer? Customer { get; set; } // Nullable property
}
";

        // Act
        RunGeneratorWithOutput<BidirectionalRelationshipGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var orderHelper = outputCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.Contains("OrderRelationshipHelper"));
        Assert.NotNull(orderHelper);
        var code = orderHelper.ToString();
        
        // Verify nullable property assignment (no null-forgiving operator)
        Assert.Contains("entity.Customer = value;", code);
        Assert.DoesNotContain("entity.Customer = value!;", code);
    }

    [Fact]
    public void SetMethod_WithNonNullableProperty_ShouldUseNullForgivingOperator()
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
    
    [ManyToOne]
    [JoinColumn(""customer_id"")]
    public Customer Customer { get; set; } = null!; // Non-nullable property
}
";

        // Act
        RunGeneratorWithOutput<BidirectionalRelationshipGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var orderHelper = outputCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.Contains("OrderRelationshipHelper"));
        Assert.NotNull(orderHelper);
        var code = orderHelper.ToString();
        
        // Verify non-nullable property assignment (with null-forgiving operator)
        Assert.Contains("entity.Customer = value!;", code);
    }

    [Fact]
    public void RemoveFromMethod_WithNullableOwnerSideProperty_ShouldAssignNull()
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
    
    [ManyToOne]
    [JoinColumn(""customer_id"")]
    public Customer? Customer { get; set; } // Nullable - can be set to null
}
";

        // Act
        RunGeneratorWithOutput<BidirectionalRelationshipGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var customerHelper = outputCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.Contains("CustomerRelationshipHelper"));
        Assert.NotNull(customerHelper);
        var code = customerHelper.ToString();
        
        // Verify null assignment for nullable property
        Assert.Contains("item.Customer = null;", code);
    }

    [Fact]
    public void RemoveFromMethod_WithNonNullableOwnerSideProperty_ShouldSkipNullAssignment()
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
    public Customer Customer { get; set; } = null!; // Non-nullable - cannot be set to null
}
";

        // Act
        RunGeneratorWithOutput<BidirectionalRelationshipGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var customerHelper = outputCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.Contains("CustomerRelationshipHelper"));
        Assert.NotNull(customerHelper);
        var code = customerHelper.ToString();
        
        // Verify null assignment is skipped for non-nullable property
        Assert.DoesNotContain("item.Customer = null;", code);
        Assert.Contains("Customer is non-nullable, skipping null assignment", code);
        // FK should still be cleared
        Assert.Contains("item.CustomerId = 0;", code);
    }

    [Fact]
    public void SetMethod_ShouldHandleInverseCollectionProperty()
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
    
    [ManyToOne]
    [JoinColumn(""customer_id"")]
    public Customer Customer { get; set; } = null!;
}
";

        // Act
        RunGeneratorWithOutput<BidirectionalRelationshipGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var orderHelper = outputCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.Contains("OrderRelationshipHelper"));
        Assert.NotNull(orderHelper);
        var code = orderHelper.ToString();
        
        // Verify inverse collection property is used
        Assert.Contains("oldValue.Orders", code);
        Assert.Contains("value.Orders", code);
        Assert.Contains("Orders.Contains", code);
        Assert.Contains("Orders.Remove", code);
        Assert.Contains("Orders.Add", code);
    }
}
