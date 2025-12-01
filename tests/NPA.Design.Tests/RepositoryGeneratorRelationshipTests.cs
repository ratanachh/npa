using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;
using NPA.Design.Generators;

namespace NPA.Design.Tests;

/// <summary>
/// Tests for Relationship-Aware Repository Generation.
/// Verifies that the generator creates relationship-specific methods
/// like GetByIdWith{Property}Async and Load{Property}Async.
/// </summary>
public class RepositoryGeneratorRelationshipTests : GeneratorTestBase
{
    #region ManyToOne Relationship Tests

    [Fact]
    public void ManyToOne_OwnerSide_ShouldGenerateGetByIdWithRelationshipMethod()
    {
        // Arrange - Order has ManyToOne to Customer (owner side)
        var source = @"
using NPA.Core.Annotations;
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
    public interface IOrderRepository : IRepository<Order, int>
    {
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty("Generator should not produce diagnostics");

        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should generate GetByIdWithCustomerAsync method
        generatedCode.Should().Contain("Task<TestNamespace.Order?> GetByIdWithCustomerAsync(int id)",
            "Should generate GetByIdWith{Property}Async method for ManyToOne relationship");

        // Should use LEFT JOIN
        generatedCode.Should().Contain("LEFT JOIN customers",
            "Should use LEFT JOIN to load related Customer");

        // Should use Dapper multi-mapping
        generatedCode.Should().Contain("QueryAsync<TestNamespace.Order, TestNamespace.Customer, TestNamespace.Order>",
            "Should use Dapper multi-mapping to load Order and Customer");

        // Should set the navigation property
        generatedCode.Should().Contain("entity.Customer = related",
            "Should set the Customer navigation property");
    }

    [Fact]
    public void ManyToOne_ShouldGenerateLoadRelationshipMethod()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
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
    }

    [Entity]
    [Table(""orders"")]
    public class Order
    {
        [Id]
        [Column(""id"")]
        public int Id { get; set; }
        
        [Column(""customer_id"")]
        public int CustomerId { get; set; }
        
        [ManyToOne]
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
        diagnostics.Should().BeEmpty();

        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should generate LoadCustomerAsync method
        generatedCode.Should().Contain("Task<TestNamespace.Customer?> LoadCustomerAsync(TestNamespace.Order entity)",
            "Should generate Load{Property}Async method for lazy loading");

        // Should use direct property access (not reflection)
        // Should use direct property access (not reflection)
        generatedCode.Should().Contain("entity.CustomerId",
            "Should use direct property access for foreign key");

        // Should query the related entity
        generatedCode.Should().Contain("SELECT * FROM customers WHERE Id = @Id",
            "Should query Customer by foreign key");
    }

    #endregion

    #region OneToMany Relationship Tests

    [Fact]
    public void OneToMany_InverseSide_ShouldGenerateGetByIdWithCollectionMethod()
    {
        // Arrange - Customer has OneToMany to Orders (inverse side)
        var source = @"
using NPA.Core.Annotations;
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
        
        [OneToMany(MappedBy = ""Customer"")]
        public List<Order> Orders { get; set; }
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
        diagnostics.Should().BeEmpty();

        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should generate GetByIdWithOrdersAsync method
        generatedCode.Should().Contain("Task<TestNamespace.Customer?> GetByIdWithOrdersAsync(int id)",
            "Should generate GetByIdWith{Property}Async method for OneToMany collection");

        // Should query the main entity first
        // Should use LEFT JOIN
        generatedCode.Should().Contain("LEFT JOIN orders",
            "Should use LEFT JOIN to load related Orders");
    }

    [Fact]
    public void OneToMany_ShouldGenerateLoadCollectionMethod()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
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
        
        [OneToMany(MappedBy = ""Customer"")]
        public List<Order> Orders { get; set; }
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
        diagnostics.Should().BeEmpty();

        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("CustomerRepositoryImplementation"))
            .ToString();

        // Should generate LoadOrdersAsync method
        generatedCode.Should().Contain("Task<IEnumerable<TestNamespace.Order>> LoadOrdersAsync(TestNamespace.Customer entity)",
            "Should generate Load{Property}Async method for collection");

        // Should read the primary key to query related items
        generatedCode.Should().Contain("entity.Id",
            "Should use entity's Id to query related Orders");
    }

    #endregion

    #region OneToOne Relationship Tests

    [Fact]
    public void OneToOne_OwnerSide_ShouldGenerateRelationshipMethods()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    [Table(""users"")]
    public class User
    {
        [Id]
        [Column(""id"")]
        public int Id { get; set; }
        
        [OneToOne]
        [JoinColumn(""profile_id"")]
        public UserProfile Profile { get; set; }
    }

    [Entity]
    [Table(""user_profiles"")]
    public class UserProfile
    {
        [Id]
        [Column(""id"")]
        public int Id { get; set; }
        
        [Column(""bio"")]
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
        diagnostics.Should().BeEmpty();

        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("UserRepositoryImplementation"))
            .ToString();

        // Should generate GetByIdWithProfileAsync
        generatedCode.Should().Contain("Task<TestNamespace.User?> GetByIdWithProfileAsync(int id)",
            "Should generate GetByIdWith{Property}Async for OneToOne");

        // Should generate LoadProfileAsync
        generatedCode.Should().Contain("Task<TestNamespace.UserProfile?> LoadProfileAsync(TestNamespace.User entity)",
            "Should generate Load{Property}Async for OneToOne");

        // Should use LEFT JOIN for OneToOne
        generatedCode.Should().Contain("LEFT JOIN user_profiles",
            "Should use LEFT JOIN for OneToOne relationship");
    }

    #endregion

    #region Owner vs Inverse Detection Tests

    [Fact]
    public void Relationship_OwnerSide_ShouldHaveJoinColumn()
    {
        // Arrange - Verify owner side has foreign key column
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    [Table(""departments"")]
    public class Department
    {
        [Id]
        [Column(""id"")]
        public int Id { get; set; }
        
        [OneToMany(MappedBy = ""Department"")]
        public List<Employee> Employees { get; set; }
    }

    [Entity]
    [Table(""employees"")]
    public class Employee
    {
        [Id]
        [Column(""id"")]
        public int Id { get; set; }
        
        [ManyToOne]
        [JoinColumn(""department_id"")]
        public Department Department { get; set; }
    }

    [Repository]
    public interface IEmployeeRepository : IRepository<Employee, int>
    {
    }

    [Repository]
    public interface IDepartmentRepository : IRepository<Department, int>
    {
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var employeeRepo = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("EmployeeRepositoryImplementation"))
            .ToString();

        var departmentRepo = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("DepartmentRepositoryImplementation"))
            .ToString();

        // Employee (owner) should have GetByIdWithDepartmentAsync
        employeeRepo.Should().Contain("GetByIdWithDepartmentAsync",
            "Owner side should generate relationship methods");

        // Department (inverse) should have GetByIdWithEmployeesAsync
        departmentRepo.Should().Contain("GetByIdWithEmployeesAsync",
            "Inverse side should also generate relationship methods");
    }

    #endregion

    #region Multiple Relationships Tests

    [Fact]
    public void MultipleRelationships_ShouldGenerateMethodsForEach()
    {
        // Arrange - Entity with multiple relationships
        var source = @"
using NPA.Core.Annotations;
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
    }

    [Entity]
    [Table(""products"")]
    public class Product
    {
        [Id]
        [Column(""id"")]
        public int Id { get; set; }
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
        
        [OneToMany(MappedBy = ""Order"")]
        public List<OrderItem> Items { get; set; }
    }

    [Entity]
    [Table(""order_items"")]
    public class OrderItem
    {
        [Id]
        [Column(""id"")]
        public int Id { get; set; }
        
        [ManyToOne]
        [JoinColumn(""order_id"")]
        public Order Order { get; set; }
        
        [ManyToOne]
        [JoinColumn(""product_id"")]
        public Product Product { get; set; }
    }

    [Repository]
    public interface IOrderRepository : IRepository<Order, int>
    {
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should generate methods for Customer relationship
        generatedCode.Should().Contain("GetByIdWithCustomerAsync",
            "Should generate method for Customer relationship");
        generatedCode.Should().Contain("LoadCustomerAsync",
            "Should generate lazy load method for Customer");

        // Should generate methods for Items relationship
        generatedCode.Should().Contain("GetByIdWithItemsAsync",
            "Should generate method for Items collection");
        generatedCode.Should().Contain("LoadItemsAsync",
            "Should generate lazy load method for Items");
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void NoRelationships_ShouldNotGenerateRelationshipMethods()
    {
        // Arrange - Entity with no relationships
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Entity]
    [Table(""products"")]
    public class Product
    {
        [Id]
        [Column(""id"")]
        public int Id { get; set; }
        
        [Column(""name"")]
        public string Name { get; set; }
    }

    [Repository]
    public interface IProductRepository : IRepository<Product, int>
    {
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("ProductRepositoryImplementation"))
            .ToString();

        // Should not have relationship-specific methods
        generatedCode.Should().NotContain("GetByIdWith",
            "Should not generate GetByIdWith methods when no relationships exist");
        generatedCode.Should().NotContain("Load",
            "Should not generate Load methods when no relationships exist");

        // But should still have base CRUD methods
        generatedCode.Should().Contain("class ProductRepositoryImplementation",
            "Should still generate the repository class");
    }

    [Fact]
    public void NullableRelationship_ShouldHandleNullCase()
    {
        // Arrange - Nullable relationship
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
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
    }

    [Entity]
    [Table(""orders"")]
    public class Order
    {
        [Id]
        [Column(""id"")]
        public int Id { get; set; }
        
        [ManyToOne]
        [JoinColumn(""customer_id"", Nullable = true)]
        public Customer? Customer { get; set; }
    }

    [Repository]
    public interface IOrderRepository : IRepository<Order, int>
    {
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should still generate the relationship methods
        generatedCode.Should().Contain("GetByIdWithCustomerAsync",
            "Should generate method even for nullable relationships");

        // Should use LEFT JOIN (which handles nulls)
        generatedCode.Should().Contain("LEFT JOIN",
            "Should use LEFT JOIN to properly handle nullable relationships");
    }

    #endregion

    #region Region Comment Tests

    [Fact]
    public void RelationshipMethods_ShouldBeInCorrectRegion()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
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
    public interface IOrderRepository : IRepository<Order, int>
    {
    }
}";

        // Act
        RunGeneratorWithOutput<RepositoryGenerator>(source, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();

        var generatedCode = outputCompilation.SyntaxTrees
            .First(t => t.FilePath.Contains("OrderRepositoryImplementation"))
            .ToString();

        // Should have the relationship-aware region
        generatedCode.Should().Contain("#region Relationship-Aware Methods",
            "Relationship methods should be in their own region");

        // Methods should be within the region
        var regionStart = generatedCode.IndexOf("#region Relationship-Aware Methods");
        var getByIdWithMethod = generatedCode.IndexOf("GetByIdWithCustomerAsync");
        var loadMethod = generatedCode.IndexOf("LoadCustomerAsync");
        var regionEnd = generatedCode.IndexOf("#endregion", regionStart);

        regionStart.Should().BeLessThan(getByIdWithMethod,
            "GetByIdWith method should be inside the region");
        regionStart.Should().BeLessThan(loadMethod,
            "Load method should be inside the region");
        getByIdWithMethod.Should().BeLessThan(regionEnd,
            "GetByIdWith method should be before region end");
        loadMethod.Should().BeLessThan(regionEnd,
            "Load method should be before region end");
    }

    #endregion
}
