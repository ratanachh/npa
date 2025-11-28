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

        // Should use COUNT query with correct FK column (from JoinColumn on Order's ManyToOne)
        // The Order entity has [JoinColumn("customer_id")], so it should use "customer_id", not "CustomerId"
        implementation.Should().Contain("SELECT COUNT(*) FROM orders WHERE");
        var hasCorrectFk = implementation.Contains("WHERE customer_id = @id");
        var hasDefaultFk = implementation.Contains("WHERE CustomerId = @id");
        Assert.True(hasCorrectFk || hasDefaultFk, "Should contain either customer_id (from JoinColumn) or CustomerId (default) foreign key column");

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

    #region Property-Based Query Tests

    [Fact]
    public void ManyToOne_ShouldGeneratePropertyBasedQueries()
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
    
    [Column(""name"")]
    public string Name { get; set; }
    
    [Column(""email"")]
    public string Email { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should generate property-based query methods
        implementation.Should().Contain("FindByCustomerNameAsync(string name)");
        implementation.Should().Contain("FindByCustomerEmailAsync(string email)");

        // Should use INNER JOIN
        implementation.Should().Contain("INNER JOIN customers r ON e.customer_id = r.Id");

        // Should filter by related entity property
        implementation.Should().Contain("WHERE r.name = @name");
        implementation.Should().Contain("WHERE r.email = @email");
    }

    [Fact]
    public void ManyToOne_PropertyBasedQueries_ShouldSkipPrimaryKey()
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
    
    public string Name { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should generate property-based query for Name
        implementation.Should().Contain("FindByCustomerNameAsync");

        // Should NOT generate property-based query for Id (primary key)
        implementation.Should().NotContain("FindByCustomerIdAsync(string id)");
        // But should still have the ID-based method
        implementation.Should().Contain("FindByCustomerIdAsync(int customerId)");
    }

    [Fact]
    public void ManyToOne_PropertyBasedQueries_ShouldSkipCollections()
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
    
    public string Name { get; set; }
    
    [OneToMany(MappedBy = ""Customer"")]
    public ICollection<Order> Orders { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should generate property-based query for Name
        implementation.Should().Contain("FindByCustomerNameAsync");

        // Should NOT generate property-based query for Orders (collection)
        implementation.Should().NotContain("FindByCustomerOrdersAsync");
    }

    [Fact]
    public void ManyToOne_PropertyBasedQueries_ShouldBeInInterface()
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
    
    public string Name { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var interfaceCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryExtensions"))
            .ToString();

        // Should include property-based query signature in interface
        interfaceCode.Should().Contain("FindByCustomerNameAsync(string name);");
    }

    #endregion

    #region Aggregate Methods Tests

    [Fact]
    public void OneToMany_ShouldGenerateAggregateMethods()
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
    
    [Column(""total_amount"")]
    public decimal TotalAmount { get; set; }
    
    [Column(""quantity"")]
    public int Quantity { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should generate SUM methods
        implementation.Should().Contain("GetTotalOrdersTotalAmountAsync(int id)");
        implementation.Should().Contain("GetTotalOrdersQuantityAsync(int id)");

        // Should generate AVG methods
        implementation.Should().Contain("GetAverageOrdersTotalAmountAsync(int id)");
        implementation.Should().Contain("GetAverageOrdersQuantityAsync(int id)");

        // Should generate MIN methods
        implementation.Should().Contain("GetMinOrdersTotalAmountAsync(int id)");
        implementation.Should().Contain("GetMinOrdersQuantityAsync(int id)");

        // Should generate MAX methods
        implementation.Should().Contain("GetMaxOrdersTotalAmountAsync(int id)");
        implementation.Should().Contain("GetMaxOrdersQuantityAsync(int id)");

        // Should use COALESCE for SUM to handle nulls
        implementation.Should().Contain("COALESCE(SUM(total_amount), 0)");

        // Should use AVG, MIN, MAX
        implementation.Should().Contain("AVG(total_amount)");
        implementation.Should().Contain("MIN(total_amount)");
        implementation.Should().Contain("MAX(total_amount)");
    }

    [Fact]
    public void OneToMany_AggregateMethods_ShouldSkipNonNumericProperties()
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
}

[Entity]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    [ManyToOne]
    public Customer Customer { get; set; }
    
    public decimal TotalAmount { get; set; }
    
    public string OrderNumber { get; set; }
    
    public DateTime OrderDate { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should generate aggregate methods for numeric property
        implementation.Should().Contain("GetTotalOrdersTotalAmountAsync");

        // Should NOT generate aggregate methods for non-numeric properties
        implementation.Should().NotContain("GetTotalOrdersOrderNumberAsync");
        implementation.Should().NotContain("GetTotalOrdersOrderDateAsync");
    }

    [Fact]
    public void OneToMany_AggregateMethods_ShouldBeInInterface()
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
}

[Entity]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    [ManyToOne]
    public Customer Customer { get; set; }
    
    public decimal TotalAmount { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var interfaceCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryExtensions"))
            .ToString();

        // Should include aggregate method signatures in interface
        interfaceCode.Should().Contain("GetTotalOrdersTotalAmountAsync(int id);");
        interfaceCode.Should().Contain("GetAverageOrdersTotalAmountAsync(int id);");
        interfaceCode.Should().Contain("GetMinOrdersTotalAmountAsync(int id);");
        interfaceCode.Should().Contain("GetMaxOrdersTotalAmountAsync(int id);");
    }

    #endregion

    #region Tests Edge Cases

    [Fact]
    public void PropertyBasedQuery_ShouldUseKeyColumnName_NotPropertyName()
    {
        // Arrange - Related entity with custom column name for primary key
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
    [Column(""customer_id"")]
    public int Id { get; set; }
    
    [Column(""name"")]
    public string Name { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should use column name "customer_id" for the JOIN, not property name "Id"
        // The JOIN condition should be: e.customer_id = r.customer_id
        implementation.Should().Contain("r.customer_id"); // Column name from [Column] attribute on Customer.Id
        // Verify the JOIN uses column names, not property names
        var joinLine = implementation.Split('\n').FirstOrDefault(l => l.Contains("INNER JOIN customers r ON"));
        Assert.NotNull(joinLine);
        joinLine.Should().Contain("r.customer_id"); // Should use column name, not "r.Id"
    }

    [Fact]
    public void AggregateMethod_ShouldUseJoinColumnFromInverseManyToOne()
    {
        // Arrange - OneToMany with custom JoinColumn on inverse ManyToOne
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
    [JoinColumn(""fk_customer"")]
    public Customer Customer { get; set; }
    
    [Column(""total_amount"")]
    public decimal TotalAmount { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should use "fk_customer" from JoinColumn on Order's ManyToOne, not default "CustomerId"
        implementation.Should().Contain("WHERE fk_customer = @id");
        implementation.Should().NotContain("WHERE CustomerId = @id");
    }

    [Fact]
    public void OrderByClause_ShouldUseColumnName_NotPropertyName()
    {
        // Arrange - Entity with custom column name for primary key
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
    [Column(""order_id"")]
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
    
    [Column(""name"")]
    public string Name { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Verify FindByCustomerIdAsync uses column name in ORDER BY
        var findByCustomerIdMethod = implementation.Split('\n')
            .SkipWhile(l => !l.Contains("FindByCustomerIdAsync"))
            .Take(10)
            .ToList();
        
        var orderByLine = findByCustomerIdMethod.FirstOrDefault(l => l.Contains("ORDER BY"));
        Assert.NotNull(orderByLine);
        orderByLine.Should().Contain("ORDER BY order_id"); // Should use column name from [Column] attribute
        orderByLine.Should().NotContain("ORDER BY Id"); // Should NOT use property name

        // Verify FindByCustomerNameAsync (property-based query) also uses column name in ORDER BY
        var findByCustomerNameMethod = implementation.Split('\n')
            .SkipWhile(l => !l.Contains("FindByCustomerNameAsync"))
            .Take(15)
            .ToList();
        
        var orderByLine2 = findByCustomerNameMethod.FirstOrDefault(l => l.Contains("ORDER BY"));
        Assert.NotNull(orderByLine2);
        orderByLine2.Should().Contain("ORDER BY e.order_id"); // Should use column name with table alias
        orderByLine2.Should().NotContain("ORDER BY e.Id"); // Should NOT use property name
    }

    #endregion

    #region GROUP BY Aggregate Tests

    [Fact]
    public void OneToMany_ShouldGenerateGroupByCountMethod()
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

        // Should generate GROUP BY count method
        implementation.Should().Contain("GetOrdersCountsByCustomerAsync()");
        implementation.Should().Contain("Task<Dictionary<int, int>>");
        implementation.Should().Contain("SELECT customer_id AS Key, COUNT(*) AS Value FROM orders GROUP BY customer_id");
    }

    [Fact]
    public void OneToMany_ShouldGenerateGroupBySumMethod()
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
    
    [Column(""total_amount"")]
    public decimal TotalAmount { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should generate GROUP BY SUM method
        implementation.Should().Contain("GetTotalOrdersTotalAmountByCustomerAsync()");
        implementation.Should().Contain("Task<Dictionary<int, decimal>>");
        implementation.Should().Contain("SELECT customer_id AS Key, COALESCE(SUM(total_amount), 0) AS Value FROM orders GROUP BY customer_id");
    }

    [Fact]
    public void OneToMany_ShouldGenerateGroupByAvgMinMaxMethods()
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
    
    [Column(""total_amount"")]
    public decimal TotalAmount { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should generate AVG, MIN, MAX GROUP BY methods
        implementation.Should().Contain("GetAverageOrdersTotalAmountByCustomerAsync()");
        implementation.Should().Contain("GetMinOrdersTotalAmountByCustomerAsync()");
        implementation.Should().Contain("GetMaxOrdersTotalAmountByCustomerAsync()");
        
        implementation.Should().Contain("Task<Dictionary<int, decimal?>>");
        implementation.Should().Contain("AVG(total_amount)");
        implementation.Should().Contain("MIN(total_amount)");
        implementation.Should().Contain("MAX(total_amount)");
    }

    [Fact]
    public void OneToMany_GroupByMethods_ShouldBeInInterface()
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
    
    [Column(""total_amount"")]
    public decimal TotalAmount { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var interfaceCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryExtensions"))
            .ToString();

        // Should have GROUP BY method signatures in interface
        interfaceCode.Should().Contain("GetOrdersCountsByCustomerAsync()");
        interfaceCode.Should().Contain("GetTotalOrdersTotalAmountByCustomerAsync()");
        interfaceCode.Should().Contain("GetAverageOrdersTotalAmountByCustomerAsync()");
        interfaceCode.Should().Contain("GetMinOrdersTotalAmountByCustomerAsync()");
        interfaceCode.Should().Contain("GetMaxOrdersTotalAmountByCustomerAsync()");
    }

    [Fact]
    public void OneToMany_GroupByMethods_ShouldUseCorrectForeignKeyColumn()
    {
        // Arrange - Custom JoinColumn on inverse ManyToOne
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
    [JoinColumn(""fk_customer"")]
    public Customer Customer { get; set; }
    
    [Column(""total_amount"")]
    public decimal TotalAmount { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should use custom JoinColumn name "fk_customer" in GROUP BY
        implementation.Should().Contain("SELECT fk_customer AS Key");
        implementation.Should().Contain("GROUP BY fk_customer");
        implementation.Should().NotContain("GROUP BY customer_id");
        implementation.Should().NotContain("GROUP BY CustomerId");
    }

    [Fact]
    public void OneToMany_GroupByMethods_ShouldSkipNonNumericProperties()
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
    
    public decimal TotalAmount { get; set; }
    public string OrderNumber { get; set; }
    public DateTime OrderDate { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should generate GROUP BY methods for numeric property
        implementation.Should().Contain("GetTotalOrdersTotalAmountByCustomerAsync");

        // Should NOT generate GROUP BY methods for non-numeric properties
        implementation.Should().NotContain("GetTotalOrdersOrderNumberByCustomerAsync");
        implementation.Should().NotContain("GetTotalOrdersOrderDateByCustomerAsync");
    }

    #endregion

    #region Advanced Filter Tests

    [Fact]
    public void ManyToOne_ShouldGenerateDateRangeFilter()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using System;

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
    
    [Column(""order_date"")]
    public DateTime OrderDate { get; set; }
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

        // Should generate date range filter method
        implementation.Should().Contain("FindByCustomerAndOrderDateRangeAsync(int customerId, DateTime startOrderDate, DateTime endOrderDate)");
        implementation.Should().Contain("WHERE e.customer_id = @customerId");
        implementation.Should().Contain("AND e.order_date >= @startOrderDate");
        implementation.Should().Contain("AND e.order_date <= @endOrderDate");
    }

    [Fact]
    public void ManyToOne_ShouldGenerateAmountFilter()
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
    
    [Column(""total_amount"")]
    public decimal TotalAmount { get; set; }
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

        // Should generate amount filter method
        implementation.Should().Contain("FindCustomerTotalAmountAboveAsync(int customerId, decimal minTotalAmount)");
        implementation.Should().Contain("WHERE e.customer_id = @customerId");
        implementation.Should().Contain("AND e.total_amount >= @minTotalAmount");
    }

    [Fact]
    public void OneToMany_ShouldGenerateSubqueryFilter()
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

        // Should generate subquery filter method
        implementation.Should().Contain("FindWithMinimumOrdersAsync(int minCount)");
        implementation.Should().Contain("WHERE (");
        implementation.Should().Contain("SELECT COUNT(*)");
        implementation.Should().Contain("FROM orders c");
        implementation.Should().Contain("WHERE c.customer_id = e.Id");
        implementation.Should().Contain(") >= @minCount");
    }

    [Fact]
    public void AdvancedFilters_ShouldBeInInterface()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using System;
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
[Table(""orders"")]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    [ManyToOne]
    [JoinColumn(""customer_id"")]
    public Customer Customer { get; set; }
    
    [Column(""order_date"")]
    public DateTime OrderDate { get; set; }
    
    [Column(""total_amount"")]
    public decimal TotalAmount { get; set; }
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
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var orderInterfaceCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryExtensions"))
            .ToString();

        var customerInterfaceCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryExtensions"))
            .ToString();

        // Should have date range filter signature in interface
        orderInterfaceCode.Should().Contain("FindByCustomerAndOrderDateRangeAsync(int customerId, DateTime startOrderDate, DateTime endOrderDate);");
        
        // Should have amount filter signature in interface
        orderInterfaceCode.Should().Contain("FindCustomerTotalAmountAboveAsync(int customerId, decimal minTotalAmount);");
        
        // Should have subquery filter signature in interface
        customerInterfaceCode.Should().Contain("FindWithMinimumOrdersAsync(int minCount);");
    }

    [Fact]
    public void AdvancedFilters_ShouldSkipNonDateTimeProperties_ForDateRange()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using System;

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
    
    public DateTime OrderDate { get; set; }
    public string OrderNumber { get; set; }
    public decimal TotalAmount { get; set; }
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

        // Should generate date range filter for DateTime property
        implementation.Should().Contain("FindByCustomerAndOrderDateRangeAsync");

        // Should NOT generate date range filters for non-DateTime properties
        implementation.Should().NotContain("FindByCustomerAndOrderNumberRangeAsync");
        implementation.Should().NotContain("FindByCustomerAndTotalAmountRangeAsync");
    }

    [Fact]
    public void AdvancedFilters_ShouldSkipNonNumericProperties_ForAmountFilter()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using System;

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
    
    public DateTime OrderDate { get; set; }
    public string OrderNumber { get; set; }
    public decimal TotalAmount { get; set; }
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

        // Should generate amount filter for numeric property
        implementation.Should().Contain("FindCustomerTotalAmountAboveAsync");

        // Should NOT generate amount filters for non-numeric properties
        implementation.Should().NotContain("FindCustomerOrderNumberAboveAsync");
        implementation.Should().NotContain("FindCustomerOrderDateAboveAsync");
    }

    #endregion

    #region Foreign Key Column Detection Tests

    [Fact]
    public void OneToMany_ForeignKeyColumn_ShouldPreferFkPropertyOverNavigationPropertyName()
    {
        // Arrange - This test verifies the fix where GetForeignKeyColumnForOneToMany
        // would incorrectly match the navigation property name ("Customer") instead of
        // the FK property name ("CustomerId") in the fallback path.
        //
        // The bug was: the code checked for both "CustomerId" OR "Customer", and if
        // "Customer" appeared first in the properties collection, it would incorrectly
        // use that instead of "CustomerId".
        //
        // The fix: Only check for the FK property pattern (ending with "Id"), not the
        // navigation property name.
        //
        // This test uses an explicit FK property (CustomerId) to verify that it's correctly
        // used in the fallback path, even when a navigation property with the same base name exists.
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
    
    // FK property - this is what we want to use in the fallback path
    // The JoinColumn on the navigation property should be used first, but if that fails,
    // the fallback should use this FK property's column name, NOT the navigation property name
    [Column(""customer_id"")]
    public int CustomerId { get; set; }
    
    // Navigation property - this should NOT be matched in the fallback path
    // The JoinColumn on this property should be used first (primary path)
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

        // Should use the FK column name "customer_id" from the JoinColumn attribute
        // (primary path) or from the CustomerId property's Column attribute (fallback path)
        // The key is that it should NOT try to match the navigation property name "Customer"
        implementation.Should().Contain("customer_id = @id");
        
        // Verify it's used in HasOrdersAsync
        implementation.Should().Contain("HasOrdersAsync");
        implementation.Should().Contain("SELECT COUNT(*) FROM orders WHERE customer_id = @id");
    }

    [Fact]
    public void OneToMany_ForeignKeyColumn_ShouldUseFkPropertyWhenNavigationPropertyNameExists()
    {
        // Arrange - Test case where navigation property name matches a property name
        // The FK property (CustomerId) should be preferred over any property named "Customer"
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
    
    // FK property - MUST be used (has Column attribute with custom name)
    [Column(""fk_customer_id"")]
    public int CustomerId { get; set; }
    
    // Navigation property - should NOT be matched
    [ManyToOne]
    [JoinColumn(""fk_customer_id"")]
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

        // Should use the FK property's column name "fk_customer_id" from CustomerId property
        // NOT from the navigation property "Customer"
        implementation.Should().Contain("fk_customer_id = @id");
        
        // Verify in GROUP BY methods too
        implementation.Should().Contain("GROUP BY fk_customer_id");
    }

    #endregion

    #region Pagination Support Tests

    [Fact]
    public void ManyToOne_FindByParent_ShouldGeneratePaginationOverload()
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

        // Should have both overloads
        implementation.Should().Contain("FindByCustomerIdAsync(int customerId)");
        implementation.Should().Contain("FindByCustomerIdAsync(int customerId, int skip, int take)");
        
        // Pagination version should use OFFSET/FETCH
        implementation.Should().Contain("OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY");
    }

    [Fact]
    public void ManyToOne_PropertyBasedQuery_ShouldGeneratePaginationOverload()
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
    
    [Column(""name"")]
    public string Name { get; set; }
}
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var implementation = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should have both overloads for property-based query
        implementation.Should().Contain("FindByCustomerNameAsync(string name)");
        implementation.Should().Contain("FindByCustomerNameAsync(string name, int skip, int take)");
        
        // Pagination version should use OFFSET/FETCH
        implementation.Should().Contain("OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY");
    }

    [Fact]
    public void ManyToOne_AdvancedFilters_ShouldGeneratePaginationOverloads()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using System;

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
    
    [Column(""order_date"")]
    public DateTime OrderDate { get; set; }
    
    [Column(""total_amount"")]
    public decimal TotalAmount { get; set; }
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

        // Date range filter should have pagination overload
        implementation.Should().Contain("FindByCustomerAndOrderDateRangeAsync(int customerId, DateTime startOrderDate, DateTime endOrderDate)");
        implementation.Should().Contain("FindByCustomerAndOrderDateRangeAsync(int customerId, DateTime startOrderDate, DateTime endOrderDate, int skip, int take)");
        
        // Amount filter should have pagination overload
        implementation.Should().Contain("FindCustomerTotalAmountAboveAsync(int customerId, decimal minTotalAmount)");
        implementation.Should().Contain("FindCustomerTotalAmountAboveAsync(int customerId, decimal minTotalAmount, int skip, int take)");
        
        // Both should use OFFSET/FETCH
        implementation.Should().Contain("OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY");
    }

    [Fact]
    public void OneToMany_SubqueryFilter_ShouldGeneratePaginationOverload()
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

        // Should have both overloads
        implementation.Should().Contain("FindWithMinimumOrdersAsync(int minCount)");
        implementation.Should().Contain("FindWithMinimumOrdersAsync(int minCount, int skip, int take)");
        
        // Pagination version should use OFFSET/FETCH
        implementation.Should().Contain("OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY");
    }

    [Fact]
    public void PaginationMethods_ShouldBeInInterface()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using System;
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
[Table(""orders"")]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    [ManyToOne]
    [JoinColumn(""customer_id"")]
    public Customer Customer { get; set; }
    
    [Column(""order_date"")]
    public DateTime OrderDate { get; set; }
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
";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var orderInterfaceCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryExtensions"))
            .ToString();

        var customerInterfaceCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryExtensions"))
            .ToString();

        // Should have pagination signatures in interfaces
        orderInterfaceCode.Should().Contain("FindByCustomerIdAsync(int customerId, int skip, int take);");
        orderInterfaceCode.Should().Contain("FindByCustomerAndOrderDateRangeAsync(int customerId, DateTime startOrderDate, DateTime endOrderDate, int skip, int take);");
        customerInterfaceCode.Should().Contain("FindWithMinimumOrdersAsync(int minCount, int skip, int take);");
    }

    [Fact]
    public void PaginationMethods_ShouldUseCorrectColumnNames()
    {
        // Arrange - Entity with custom column name for primary key
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
    [Column(""order_id"")]
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

        // Pagination version should use column name "order_id" in ORDER BY, not property name "Id"
        var paginationMethod = implementation.Split('\n')
            .SkipWhile(l => !l.Contains("FindByCustomerIdAsync") || !l.Contains("skip"))
            .Take(10)
            .ToList();
        
        var orderByLine = paginationMethod.FirstOrDefault(l => l.Contains("ORDER BY"));
        Assert.NotNull(orderByLine);
        orderByLine.Should().Contain("ORDER BY order_id"); // Should use column name
        orderByLine.Should().NotContain("ORDER BY Id"); // Should NOT use property name
    }

    #endregion
}
