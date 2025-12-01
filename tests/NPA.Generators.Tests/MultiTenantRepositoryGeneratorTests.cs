using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using FluentAssertions;
using NPA.Generators.Generators;

namespace NPA.Generators.Tests;

public class MultiTenantRepositoryGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void Generate_ShouldIncludeTenantProviderParameter_WhenEntityIsMultiTenant()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{
    [Entity, Table(""Products""), MultiTenant]
    public class Product
    {
        [Id]
        public int Id { get; set; }
        
        public string TenantId { get; set; }
        public string Name { get; set; }
    }

    [Repository(typeof(Product))]
    public interface IProductRepository : IRepository<Product, int>
    {
    }
}";

        // Act
        var (diagnostics, output) = GetGeneratedOutput(source);

        // Assert
        diagnostics.Should().BeEmpty();
        output.Should().Contain("ITenantProvider? tenantProvider = null");
        output.Should().Contain("base(connection, entityManager, metadataProvider, tenantProvider)");
        output.Should().Contain("using NPA.Core.MultiTenancy;");
    }

    [Fact]
    public void Generate_ShouldIncludeMultiTenantDocumentation_WhenEntityIsMultiTenant()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{
    [Entity, Table(""Orders""), MultiTenant(tenantIdProperty: ""OrganizationId"", AllowCrossTenantQueries = true)]
    public class Order
    {
        [Id]
        public int Id { get; set; }
        
        public string OrganizationId { get; set; }
        public string OrderNumber { get; set; }
    }

    [Repository(typeof(Order))]
    public interface IOrderRepository : IRepository<Order, int>
    {
    }
}";

        // Act
        var (diagnostics, output) = GetGeneratedOutput(source);

        // Assert
        diagnostics.Should().BeEmpty();
        output.Should().Contain("This repository supports multi-tenancy with automatic tenant filtering");
        output.Should().Contain("Tenant property: OrganizationId");
        output.Should().Contain("Cross-tenant queries: Allowed");
    }

    [Fact]
    public void Generate_ShouldNotIncludeTenantProvider_WhenEntityIsNotMultiTenant()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{
    [Entity, Table(""Categories"")]
    public class Category
    {
        [Id]
        public int Id { get; set; }
        
        public string Name { get; set; }
    }

    [Repository(typeof(Category))]
    public interface ICategoryRepository : IRepository<Category, int>
    {
    }
}";

        // Act
        var (diagnostics, output) = GetGeneratedOutput(source);

        // Assert
        diagnostics.Should().BeEmpty();
        output.Should().NotContain("ITenantProvider");
        output.Should().NotContain("using NPA.Core.MultiTenancy;");
        output.Should().NotContain("multi-tenancy");
    }

    [Fact]
    public void Generate_ShouldIndicateCrossTenantNotAllowed_WhenAllowCrossTenantQueriesIsFalse()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{
    [Entity, Table(""SecureData""), MultiTenant(AllowCrossTenantQueries = false)]
    public class SecureData
    {
        [Id]
        public int Id { get; set; }
        
        public string TenantId { get; set; }
        public string Data { get; set; }
    }

    [Repository(typeof(SecureData))]
    public interface ISecureDataRepository : IRepository<SecureData, int>
    {
    }
}";

        // Act
        var (diagnostics, output) = GetGeneratedOutput(source);

        // Assert
        diagnostics.Should().BeEmpty();
        output.Should().Contain("Cross-tenant queries: Not allowed");
    }

    [Fact]
    public void Generate_ShouldUseDefaultTenantProperty_WhenNotSpecified()
    {
        // Arrange
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace TestNamespace
{
    [Entity, Table(""Users""), MultiTenant]
    public class User
    {
        [Id]
        public int Id { get; set; }
        
        public string TenantId { get; set; }
        public string Username { get; set; }
    }

    [Repository(typeof(User))]
    public interface IUserRepository : IRepository<User, int>
    {
    }
}";

        // Act
        var (diagnostics, output) = GetGeneratedOutput(source);

        // Assert
        diagnostics.Should().BeEmpty();
        output.Should().Contain("Tenant property: TenantId");
    }

    private (ImmutableArray<Diagnostic> Diagnostics, string Output) GetGeneratedOutput(string source)
    {
        var result = RunGenerator<RepositoryGenerator>(source, includeAnnotationSource: true);
        var output = result.GeneratedSources.Length > 0
            ? string.Join("\n\n", result.GeneratedSources.Select(s => s.SourceText.ToString()))
            : string.Empty;
        return (result.Diagnostics, output);
    }
}
