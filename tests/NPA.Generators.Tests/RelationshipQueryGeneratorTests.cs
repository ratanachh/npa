using Xunit;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for relationship query methods generation.
/// Relationship Query Methods
/// </summary>
public class RelationshipQueryGeneratorTests : GeneratorTestBase
{
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
}
