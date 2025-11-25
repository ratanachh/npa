using FluentAssertions;
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for Phase 7.5: Orphan Removal support in repository generator.
/// </summary>
public class RepositoryGeneratorOrphanRemovalTests : GeneratorTestBase
{
    [Fact]
    public void OrphanRemoval_OneToMany_ShouldOverrideUpdateAsync()
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
        public string Name { get; set; }
        
        [OneToMany(MappedBy = ""Customer"", OrphanRemoval = true)]
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
        diagnostics.Should().BeEmpty("Should not have compilation errors");
        
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should override UpdateAsync
        generatedCode.Should().Contain("public override async Task UpdateAsync(Customer entity)",
            "Should override UpdateAsync for orphan removal");

        // Should have orphan removal region
        generatedCode.Should().Contain("#region Orphan Removal Support (Phase 7.5)",
            "Should have orphan removal region");

        // Should load existing entity
        generatedCode.Should().Contain("var existing = await GetByIdAsync(entity.Id)",
            "Should load existing entity to compare");

        // Should detect orphans for OneToMany
        generatedCode.Should().Contain("Orphan removal for Orders collection (OneToMany)",
            "Should handle OneToMany orphan removal");

        // Should load existing items
        generatedCode.Should().Contain("SELECT * FROM Order WHERE",
            "Should query existing items from database");

        // Should identify orphaned items
        generatedCode.Should().Contain("Identify orphaned items (in existing but not in current)",
            "Should compare existing vs current items");

        // Should delete orphaned items
        generatedCode.Should().Contain("Delete orphaned items",
            "Should delete items not in current collection");

        // Should handle null collection
        generatedCode.Should().Contain("Collection is null - delete all existing items (orphan removal)",
            "Should handle null collection by deleting all");
    }

    [Fact]
    public void OrphanRemoval_OneToOne_ShouldOverrideUpdateAsync()
    {
        // Arrange - User with OneToOne OrphanRemoval=true
        var source = @"using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    public class User
    {
        [Id]
        public int Id { get; set; }
        public string Username { get; set; }
        
        [OneToOne(OrphanRemoval = true)]
        [JoinColumn(""profile_id"")]
        public UserProfile Profile { get; set; }
    }

    [Entity]
    public class UserProfile
    {
        [Id]
        public int Id { get; set; }
        public string Bio { get; set; }
    }

    [Repository]
    public interface IUserRepository : IRepository<User, int>
    {
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Should not have compilation errors");
        
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("UserRepositoryImplementation"))
            .ToString();

        // Should override UpdateAsync
        generatedCode.Should().Contain("public override async Task UpdateAsync(User entity)",
            "Should override UpdateAsync for orphan removal");

        // Should handle OneToOne orphan removal
        generatedCode.Should().Contain("Orphan removal for Profile (OneToOne)",
            "Should handle OneToOne orphan removal");

        // Should check if relationship was cleared
        generatedCode.Should().Contain("Relationship cleared - delete orphan (orphan removal)",
            "Should delete when relationship is cleared");

        // Should check if relationship was replaced
        generatedCode.Should().Contain("Relationship replaced - delete old orphan (orphan removal)",
            "Should delete when relationship is replaced");
    }

    [Fact]
    public void OrphanRemoval_MultipleRelationships_ShouldHandleAll()
    {
        // Arrange - Entity with multiple orphan removal relationships
        var source = @"using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    public class Company
    {
        [Id]
        public int Id { get; set; }
        public string Name { get; set; }
        
        [OneToMany(MappedBy = ""Company"", OrphanRemoval = true)]
        public ICollection<Employee> Employees { get; set; }
        
        [OneToOne(OrphanRemoval = true)]
        [JoinColumn(""address_id"")]
        public Address Address { get; set; }
    }

    [Entity]
    public class Employee
    {
        [Id]
        public int Id { get; set; }
        
        [ManyToOne]
        [JoinColumn(""company_id"")]
        public Company Company { get; set; }
    }

    [Entity]
    public class Address
    {
        [Id]
        public int Id { get; set; }
        public string Street { get; set; }
    }

    [Repository]
    public interface ICompanyRepository : IRepository<Company, int>
    {
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Should not have compilation errors");
        
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CompanyRepositoryImplementation"))
            .ToString();

        // Should handle both relationships
        generatedCode.Should().Contain("Orphan removal for Employees collection (OneToMany)",
            "Should handle OneToMany orphan removal");
        
        generatedCode.Should().Contain("Orphan removal for Address (OneToOne)",
            "Should handle OneToOne orphan removal");
    }

    [Fact]
    public void OrphanRemoval_WithoutOrphanRemoval_ShouldNotOverrideUpdateAsync()
    {
        // Arrange - Customer without OrphanRemoval
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
        
        [OneToMany(MappedBy = ""Customer"")]
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
        diagnostics.Should().BeEmpty("Should not have compilation errors");
        
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should NOT override UpdateAsync
        generatedCode.Should().NotContain("public override async Task UpdateAsync(Customer entity)",
            "Should not override UpdateAsync when OrphanRemoval is false");

        // Should NOT have orphan removal region
        generatedCode.Should().NotContain("#region Orphan Removal Support (Phase 7.5)",
            "Should not have orphan removal region when not needed");
    }

    [Fact]
    public void OrphanRemoval_OneToMany_ShouldHandleCollectionClear()
    {
        // Arrange
        var source = @"using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    public class Order
    {
        [Id]
        public int Id { get; set; }
        
        [OneToMany(MappedBy = ""Order"", OrphanRemoval = true)]
        public ICollection<OrderItem> Items { get; set; }
    }

    [Entity]
    public class OrderItem
    {
        [Id]
        public int Id { get; set; }
        
        [ManyToOne]
        [JoinColumn(""order_id"")]
        public Order Order { get; set; }
    }

    [Repository]
    public interface IOrderRepository : IRepository<Order, int>
    {
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Should not have compilation errors");
        
        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should handle null collection (collection clear scenario)
        generatedCode.Should().Contain("Collection is null - delete all existing items (orphan removal)",
            "Should delete all items when collection is null");
    }
}

