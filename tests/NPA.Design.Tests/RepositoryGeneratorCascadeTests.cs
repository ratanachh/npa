using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;
using NPA.Design.Generators;

namespace NPA.Design.Tests;

/// <summary>
/// Tests: Cascade Operations Enhancement.
/// Verifies that the generator creates cascade-aware methods
/// like AddWithCascadeAsync, UpdateWithCascadeAsync, and DeleteWithCascadeAsync.
/// </summary>
public class RepositoryGeneratorCascadeTests : GeneratorTestBase
{

    [Fact]
    public void CascadeAll_ShouldGenerateAllThreeCascadeMethods()
    {
        // Arrange - Customer with CascadeType.All on Orders
        var source = @"using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    [Table(""customers"")]
    public class Customer
    {
        [Id]
        [Column(""id"")]
        public int Id { get; set; }
        
        [Column(""name"")]
        public string Name { get; set; }
        
        [OneToMany(MappedBy = ""Customer"", Cascade = CascadeType.All, OrphanRemoval = true)]
        public ICollection<Order> Orders { get; set; }
    }

    [Entity]
    [Table(""orders"")]
    public class Order
    {
        [Id]
        [Column(""id"")]
        public int Id { get; set; }
        
        [ManyToOne]
        [JoinColumn(""customer_id"")]
        public Customer Customer { get; set; }
    }

    [Repository]
    public interface ICustomerRepository : IRepository<Customer, int>
    {
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");

        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should have all three cascade methods for CascadeType.All
        generatedCode.Should().Contain("public async Task<TestNamespace.Customer> AddWithCascadeAsync(TestNamespace.Customer entity)",
            "Should generate AddWithCascadeAsync for CascadeType.Persist");

        generatedCode.Should().Contain("public async Task UpdateWithCascadeAsync(TestNamespace.Customer entity)",
            "Should generate UpdateWithCascadeAsync for CascadeType.Merge");

        generatedCode.Should().Contain("public async Task DeleteWithCascadeAsync(int id)",
            "Should generate DeleteWithCascadeAsync for CascadeType.Remove");
    }

    [Fact]
    public void CascadePersist_ShouldHandleCollections()
    {
        // Arrange - Customer with Orders collection
        var source = @"using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    public class Customer
    {
        [Id]
        public int Id { get; set; }
        
        [OneToMany(MappedBy = ""Customer"", Cascade = CascadeType.Persist)]
        public ICollection<Order> Orders { get; set; }
    }

    [Entity]
    public class Order
    {
        [Id]
        public int Id { get; set; }
        
        [ManyToOne]
        [JoinColumn(""customer_id"")]
        public Customer Customer { get; set; }
    }

    [Repository]
    public interface ICustomerRepository : IRepository<Customer, int>
    {
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should handle Orders collection (child-after strategy)
        generatedCode.Should().Contain("Cascade persist Orders collection (children persisted after parent)",
            "Should use child-after strategy for OneToMany relationships");

        generatedCode.Should().Contain("var ordersToPersist = entity.Orders?.ToList()",
            "Should collect Orders before persisting parent");

        generatedCode.Should().Contain("await _entityManager.PersistAsync(item)",
            "Should persist each Order after parent");
    }

    [Fact]
    public void OrphanRemoval_ShouldImplementOrphanDetection()
    {
        // Arrange - Customer with OrphanRemoval=true
        var source = @"using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    public class Customer
    {
        [Id]
        public int Id { get; set; }
        
        [OneToMany(MappedBy = ""Customer"", Cascade = CascadeType.Merge, OrphanRemoval = true)]
        public ICollection<Order> Orders { get; set; }
    }

    [Entity]
    public class Order
    {
        [Id]
        public int Id { get; set; }
        
        [ManyToOne]
        [JoinColumn(""customer_id"")]
        public Customer Customer { get; set; }
    }

    [Repository]
    public interface ICustomerRepository : IRepository<Customer, int>
    {
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should have orphan removal logic
        generatedCode.Should().Contain("Load existing items to detect orphans (OrphanRemoval=true)",
            "Should query existing items for orphan detection");

        generatedCode.Should().Contain("Delete orphaned items",
            "Should delete items not in current collection");

        generatedCode.Should().Contain("await _entityManager.RemoveAsync(existing)",
            "Should use EntityManager to remove orphans");
    }

    [Fact]
    public void CascadeDelete_ShouldDeleteChildrenFirst()
    {
        // Arrange - Customer with CascadeType.Remove
        var source = @"using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    public class Customer
    {
        [Id]
        public int Id { get; set; }
        
        [OneToMany(MappedBy = ""Customer"", Cascade = CascadeType.Remove)]
        public ICollection<Order> Orders { get; set; }
    }

    [Entity]
    public class Order
    {
        [Id]
        public int Id { get; set; }
        
        [ManyToOne]
        [JoinColumn(""customer_id"")]
        public Customer Customer { get; set; }
    }

    [Repository]
    public interface ICustomerRepository : IRepository<Customer, int>
    {
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should delete children before parent  
        generatedCode.Should().Contain("delete children first",
            "Should use children-first deletion strategy");

        generatedCode.Should().Contain("SELECT * FROM orders WHERE",
            "Should query related Orders from orders table before deleting");
    }

    [Fact]
    public void CascadeParentFirst_ShouldPersistManyToOneFirst()
    {
        // Arrange - Order with cascade on Customer (ManyToOne)
        var source = @"using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    public class Customer
    {
        [Id]
        public int Id { get; set; }
    }

    [Entity]
    public class Order
    {
        [Id]
        public int Id { get; set; }
        
        [ManyToOne(Cascade = CascadeType.Persist)]
        [JoinColumn(""customer_id"")]
        public Customer Customer { get; set; }
    }

    [Repository]
    public interface IOrderRepository : IRepository<Order, int>
    {
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        generatedCode.Should().Contain("AddWithCascadeAsync",
            "Should generate AddWithCascadeAsync");

        // Should handle Customer (parent-first)
        generatedCode.Should().Contain("Cascade persist Customer (parent persisted first)",
            "Should use parent-first strategy for ManyToOne");
    }

    [Fact]
    public void TransientEntityDetection_ShouldCheckDefaultId()
    {
        // Arrange - Order with cascade on Customer
        var source = @"using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    public class Customer
    {
        [Id]
        public int Id { get; set; }
    }

    [Entity]
    public class Order
    {
        [Id]
        public int Id { get; set; }
        
        [ManyToOne(Cascade = CascadeType.Persist)]
        [JoinColumn(""customer_id"")]
        public Customer Customer { get; set; }
    }

    [Repository]
    public interface IOrderRepository : IRepository<Order, int>
    {
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should check if Customer is transient before persisting
        generatedCode.Should().Contain("Check if entity is transient (Id is default value)",
            "Should check for transient entities");

        generatedCode.Should().Contain("entity.Customer.Id == default",
            "Should use direct property access to check if Id is default");

        generatedCode.Should().Contain("await _entityManager.PersistAsync(entity.Customer)",
            "Should persist transient Customer before main entity");
    }

    [Fact]
    public void NoCascade_ShouldNotGenerateCascadeMethods()
    {
        // Arrange - Order with CascadeType.None
        var source = @"using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    public class Customer
    {
        [Id]
        public int Id { get; set; }
    }

    [Entity]
    public class Order
    {
        [Id]
        public int Id { get; set; }
        
        [ManyToOne(Cascade = CascadeType.None)]
        [JoinColumn(""customer_id"")]
        public Customer Customer { get; set; }
    }

    [Repository]
    public interface IOrderRepository : IRepository<Order, int>
    {
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should not generate cascade methods
        generatedCode.Should().NotContain("AddWithCascadeAsync",
            "Should not generate AddWithCascadeAsync when CascadeType is None");

        generatedCode.Should().NotContain("UpdateWithCascadeAsync",
            "Should not generate UpdateWithCascadeAsync when CascadeType is None");

        generatedCode.Should().NotContain("DeleteWithCascadeAsync",
            "Should not generate DeleteWithCascadeAsync when CascadeType is None");
    }
}
