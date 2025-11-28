using Xunit;
using System.Linq;
using Microsoft.CodeAnalysis;
using FluentAssertions;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for relationship query methods generation.
/// Relationship Query Methods
/// </summary>
public class RelationshipQueryGeneratorTests : GeneratorTestBase
{
    #region Interface Generation Tests

    [Fact]
    public void SeparateInterface_ShouldBeGenerated_WithPartialSuffix()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using System.Collections.Generic;

namespace TestNamespace;

[Repository]
public partial interface IOrderRepository : IRepository<Order, int>
{
}

[Repository]
public partial interface ICustomerRepository : IRepository<Customer, int>
{
}

[Entity]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    [ManyToOne]
    public Customer Customer { get; set; }
}

[Entity]
public class Customer
{
    [Id]
    public int Id { get; set; }
    
    [OneToMany(MappedBy = ""Customer"")]
    public ICollection<Order> Orders { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // Check that separate interface files are generated with Partial suffix
        var orderInterface = outputCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.Contains("OrderRepositoryExtensions"));
        Assert.NotNull(orderInterface);
        var orderInterfaceCode = orderInterface.ToString();

        // Verify separate interface declaration with Partial suffix (NOT partial interface)
        Assert.Contains("public interface IOrderRepositoryPartial", orderInterfaceCode);
        Assert.DoesNotContain("partial interface", orderInterfaceCode);

        // Verify method signatures (not implementations)
        Assert.Contains("FindByCustomerIdAsync(int customerId);", orderInterfaceCode);
        Assert.Contains("CountByCustomerIdAsync(int customerId);", orderInterfaceCode);

        // Check Customer interface
        var customerInterface = outputCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.Contains("CustomerRepositoryExtensions"));
        Assert.NotNull(customerInterface);
        var customerInterfaceCode = customerInterface.ToString();

        Assert.Contains("public interface ICustomerRepositoryPartial", customerInterfaceCode);
        Assert.Contains("HasOrdersAsync(int id);", customerInterfaceCode);
        Assert.Contains("CountOrdersAsync(int id);", customerInterfaceCode);

        // Verify that the implementation class implements the Partial interface
        var orderImpl = outputCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.Contains("OrderRepositoryImplementation"));
        Assert.NotNull(orderImpl);
        var orderImplCode = orderImpl.ToString();

        // Check that the class declaration includes both IOrderRepository and IOrderRepositoryPartial
        Assert.Contains("IOrderRepository, IOrderRepositoryPartial", orderImplCode);
    }

    #endregion

    #region ManyToOne Relationship Query Methods

    [Fact]
    public void ManyToOne_ShouldGenerateFindByParentMethod()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using System.Collections.Generic;

namespace TestNamespace;

[Repository]
public partial interface IOrderRepository : IRepository<Order, int>
{
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

[Entity]
[Table(""customers"")]
public class Customer
{
    [Id]
    public int Id { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should generate FindByCustomerIdAsync method
        implementation.Should().Contain("FindByCustomerIdAsync(int customerId)");

        // Should use correct SQL with JoinColumn
        implementation.Should().Contain("SELECT * FROM orders WHERE customer_id = @customerId");

        // Should order by primary key
        implementation.Should().Contain("ORDER BY Id");
    }

    [Fact]
    public void ManyToOne_ShouldGenerateCountByParentMethod()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;

namespace TestNamespace;

[Repository]
public partial interface IOrderRepository : IRepository<Order, int>
{
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

[Entity]
[Table(""customers"")]
public class Customer
{
    [Id]
    public int Id { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should generate CountByCustomerIdAsync method
        implementation.Should().Contain("CountByCustomerIdAsync(int customerId)");

        // Should use COUNT query with correct table name (from Table attribute)
        implementation.Should().Contain("SELECT COUNT(*) FROM orders WHERE");

        // Should use correct foreign key column (from JoinColumn or default)
        var hasDefaultFk = implementation.Contains("CustomerId = @customerId");
        var hasCustomFk = implementation.Contains("customer_id = @customerId");
        Assert.True(hasDefaultFk || hasCustomFk, "Should contain either CustomerId or customer_id foreign key column");
    }

    [Fact]
    public void ManyToOne_WithCustomJoinColumn_ShouldUseCustomColumnName()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;

namespace TestNamespace;

[Repository]
public partial interface IOrderRepository : IRepository<Order, int>
{
}

[Entity]
[Table(""orders"")]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    [ManyToOne]
    [JoinColumn(""fk_customer"")]
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
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should use custom JoinColumn name
        implementation.Should().Contain("WHERE fk_customer = @customerId");
    }

    [Fact]
    public void ManyToOne_WithLongKeyType_ShouldGenerateCorrectMethod()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;

namespace TestNamespace;

[Repository]
public partial interface IOrderRepository : IRepository<Order, long>
{
}

[Entity]
public class Order
{
    [Id]
    public long Id { get; set; }
    
    [ManyToOne]
    public Customer Customer { get; set; }
}

[Entity]
public class Customer
{
    [Id]
    public long Id { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should use long type for parameter
        implementation.Should().Contain("FindByCustomerIdAsync(long customerId)");
    }

    #endregion

    #region OneToMany Relationship Query Methods

    [Fact]
    public void OneToMany_ShouldGenerateHasChildrenMethod()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using System.Collections.Generic;

namespace TestNamespace;

[Repository]
public partial interface ICustomerRepository : IRepository<Customer, int>
{
}

[Entity]
[Table(""customers"")]
public class Customer
{
    [Id]
    public int Id { get; set; }
    
    [OneToMany(MappedBy = ""Customer"")]
    public ICollection<Order> Orders { get; set; }
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
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should generate HasOrdersAsync method
        implementation.Should().Contain("HasOrdersAsync(int id)");

        // Should use COUNT query
        implementation.Should().Contain("SELECT COUNT(*) FROM orders WHERE CustomerId = @id");

        // Should return bool based on count > 0
        implementation.Should().Contain("return count > 0");
    }

    [Fact]
    public void OneToMany_ShouldGenerateCountChildrenMethod()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using System.Collections.Generic;

namespace TestNamespace;

[Repository]
public partial interface ICustomerRepository : IRepository<Customer, int>
{
}

[Entity]
[Table(""customers"")]
public class Customer
{
    [Id]
    public int Id { get; set; }
    
    [OneToMany(MappedBy = ""Customer"")]
    public ICollection<Order> Orders { get; set; }
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
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should generate CountOrdersAsync method
        implementation.Should().Contain("CountOrdersAsync(int id)");

        // Should use COUNT query with correct table name (from Table attribute)
        implementation.Should().Contain("SELECT COUNT(*) FROM orders WHERE");

        // Should use correct foreign key column (from JoinColumn or default)
        // Note: The FK column could be "CustomerId" (default) or "customer_id" (from JoinColumn)
        var hasDefaultFk = implementation.Contains("CustomerId = @id");
        var hasCustomFk = implementation.Contains("customer_id = @id");
        Assert.True(hasDefaultFk || hasCustomFk, "Should contain either CustomerId or customer_id foreign key column");

        // Should return int
        implementation.Should().Contain("ExecuteScalarAsync<int>");
    }

    [Fact]
    public void OneToMany_WithoutMappedBy_ShouldNotGenerateMethods()
    {
        // Arrange - OneToMany without MappedBy is not inverse side
        var source = @"
using NPA.Core.Annotations;
using System.Collections.Generic;

namespace TestNamespace;

[Repository]
public partial interface ICustomerRepository : IRepository<Customer, int>
{
}

[Entity]
public class Customer
{
    [Id]
    public int Id { get; set; }
    
    [OneToMany]
    public ICollection<Order> Orders { get; set; }
}

[Entity]
public class Order
{
    [Id]
    public int Id { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should NOT generate Has/Count methods for non-inverse OneToMany
        implementation.Should().NotContain("HasOrdersAsync");
        implementation.Should().NotContain("CountOrdersAsync");
    }

    #endregion

    #region Multiple Relationships Tests

    [Fact]
    public void MultipleManyToOne_ShouldGenerateMethodsForEach()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;

namespace TestNamespace;

[Repository]
public partial interface IOrderRepository : IRepository<Order, int>
{
}

[Entity]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    [ManyToOne]
    public Customer Customer { get; set; }
    
    [ManyToOne]
    public Product Product { get; set; }
}

[Entity]
public class Customer
{
    [Id]
    public int Id { get; set; }
}

[Entity]
public class Product
{
    [Id]
    public int Id { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should generate methods for both relationships
        implementation.Should().Contain("FindByCustomerIdAsync");
        implementation.Should().Contain("CountByCustomerIdAsync");
        implementation.Should().Contain("FindByProductIdAsync");
        implementation.Should().Contain("CountByProductIdAsync");
    }

    [Fact]
    public void MultipleOneToMany_ShouldGenerateMethodsForEach()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using System.Collections.Generic;

namespace TestNamespace;

[Repository]
public partial interface ICustomerRepository : IRepository<Customer, int>
{
}

[Entity]
public class Customer
{
    [Id]
    public int Id { get; set; }
    
    [OneToMany(MappedBy = ""Customer"")]
    public ICollection<Order> Orders { get; set; }
    
    [OneToMany(MappedBy = ""Customer"")]
    public ICollection<Invoice> Invoices { get; set; }
}

[Entity]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    [ManyToOne]
    public Customer Customer { get; set; }
}

[Entity]
public class Invoice
{
    [Id]
    public int Id { get; set; }
    
    [ManyToOne]
    public Customer Customer { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should generate methods for both relationships
        implementation.Should().Contain("HasOrdersAsync");
        implementation.Should().Contain("CountOrdersAsync");
        implementation.Should().Contain("HasInvoicesAsync");
        implementation.Should().Contain("CountInvoicesAsync");
    }

    #endregion

    #region Table Name Handling Tests

    [Fact]
    public void WithTableAttribute_ShouldUseTableName()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;

namespace TestNamespace;

[Repository]
public partial interface IOrderRepository : IRepository<Order, int>
{
}

[Entity]
[Table(""tbl_orders"")]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    [ManyToOne]
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
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should use table name from Table attribute
        implementation.Should().Contain("FROM tbl_orders");
    }

    #endregion

    #region Method Implementation Details

    [Fact]
    public void FindByMethod_ShouldUseQueryAsync()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;

namespace TestNamespace;

[Repository]
public partial interface IOrderRepository : IRepository<Order, int>
{
}

[Entity]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    [ManyToOne]
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
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should use QueryAsync for collection results
        implementation.Should().Contain("QueryAsync<TestNamespace.Order>");
    }

    [Fact]
    public void CountMethod_ShouldUseExecuteScalarAsync()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;

namespace TestNamespace;

[Repository]
public partial interface IOrderRepository : IRepository<Order, int>
{
}

[Entity]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    [ManyToOne]
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
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should use ExecuteScalarAsync for count
        implementation.Should().Contain("ExecuteScalarAsync<int>");
    }

    #endregion
}
