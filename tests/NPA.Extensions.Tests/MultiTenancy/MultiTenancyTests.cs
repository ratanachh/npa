using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Core.MultiTenancy;
using NPA.Extensions.MultiTenancy;
using Xunit;

namespace NPA.Extensions.Tests.MultiTenancy;

public class MultiTenancyTests
{
    private readonly IServiceProvider _serviceProvider;

    public MultiTenancyTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMultiTenancy();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void TenantProvider_ShouldReturnNullWhenNoTenantSet()
    {
        // Arrange
        var provider = _serviceProvider.GetRequiredService<ITenantProvider>();

        // Act
        var tenantId = provider.GetCurrentTenantId();

        // Assert
        tenantId.Should().BeNull();
    }

    [Fact]
    public void TenantProvider_ShouldSetAndGetTenant()
    {
        // Arrange
        var provider = _serviceProvider.GetRequiredService<ITenantProvider>();

        // Act
        provider.SetCurrentTenant("tenant1");
        var tenantId = provider.GetCurrentTenantId();

        // Assert
        tenantId.Should().Be("tenant1");
    }

    [Fact]
    public void TenantProvider_ShouldClearTenant()
    {
        // Arrange
        var provider = _serviceProvider.GetRequiredService<ITenantProvider>();
        provider.SetCurrentTenant("tenant1");

        // Act
        provider.ClearCurrentTenant();
        var tenantId = provider.GetCurrentTenantId();

        // Assert
        tenantId.Should().BeNull();
    }

    [Fact]
    public void TenantProvider_ShouldThrowWhenSettingNullTenant()
    {
        // Arrange
        var provider = _serviceProvider.GetRequiredService<ITenantProvider>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => provider.SetCurrentTenant(null!));
        Assert.Throws<ArgumentException>(() => provider.SetCurrentTenant(string.Empty));
        Assert.Throws<ArgumentException>(() => provider.SetCurrentTenant("  "));
    }

    [Fact]
    public async Task TenantStore_ShouldRegisterTenant()
    {
        // Arrange
        var store = _serviceProvider.GetRequiredService<ITenantStore>();
        var tenant = new TenantContext
        {
            TenantId = "tenant1",
            Name = "Tenant One"
        };

        // Act
        await store.RegisterAsync(tenant);
        var retrieved = await store.GetByIdAsync("tenant1");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.TenantId.Should().Be("tenant1");
        retrieved.Name.Should().Be("Tenant One");
    }

    [Fact]
    public async Task TenantStore_ShouldThrowWhenRegisteringDuplicateTenant()
    {
        // Arrange
        var store = _serviceProvider.GetRequiredService<ITenantStore>();
        var tenant1 = new TenantContext { TenantId = "tenant1", Name = "Tenant One" };
        var tenant2 = new TenantContext { TenantId = "tenant1", Name = "Duplicate" };

        // Act
        await store.RegisterAsync(tenant1);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.RegisterAsync(tenant2));
    }

    [Fact]
    public async Task TenantStore_ShouldUpdateTenant()
    {
        // Arrange
        var store = _serviceProvider.GetRequiredService<ITenantStore>();
        var tenant = new TenantContext { TenantId = "tenant1", Name = "Original" };
        await store.RegisterAsync(tenant);

        // Act
        tenant.Name = "Updated";
        await store.UpdateAsync(tenant);
        var retrieved = await store.GetByIdAsync("tenant1");

        // Assert
        retrieved!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task TenantStore_ShouldRemoveTenant()
    {
        // Arrange
        var store = _serviceProvider.GetRequiredService<ITenantStore>();
        var tenant = new TenantContext { TenantId = "tenant1", Name = "Tenant One" };
        await store.RegisterAsync(tenant);

        // Act
        await store.RemoveAsync("tenant1");
        var retrieved = await store.GetByIdAsync("tenant1");

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task TenantStore_ShouldCheckExistence()
    {
        // Arrange
        var store = _serviceProvider.GetRequiredService<ITenantStore>();
        var tenant = new TenantContext { TenantId = "tenant1", Name = "Tenant One" };

        // Act & Assert
        (await store.ExistsAsync("tenant1")).Should().BeFalse();
        await store.RegisterAsync(tenant);
        (await store.ExistsAsync("tenant1")).Should().BeTrue();
    }

    [Fact]
    public async Task TenantStore_ShouldGetAllTenants()
    {
        // Arrange
        var store = _serviceProvider.GetRequiredService<ITenantStore>();
        await store.RegisterAsync(new TenantContext { TenantId = "tenant1", Name = "One" });
        await store.RegisterAsync(new TenantContext { TenantId = "tenant2", Name = "Two" });
        await store.RegisterAsync(new TenantContext { TenantId = "tenant3", Name = "Three" });

        // Act
        var all = await store.GetAllAsync();

        // Assert
        all.Should().HaveCount(3);
    }

    [Fact]
    public async Task TenantManager_ShouldCreateTenant()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<TenantManager>();

        // Act
        var tenant = await manager.CreateTenantAsync(
            "tenant1",
            "Tenant One",
            TenantIsolationStrategy.Discriminator);

        // Assert
        tenant.Should().NotBeNull();
        tenant.TenantId.Should().Be("tenant1");
        tenant.Name.Should().Be("Tenant One");
        tenant.IsolationStrategy.Should().Be(TenantIsolationStrategy.Discriminator);
        tenant.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task TenantManager_ShouldSetCurrentTenant()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<TenantManager>();
        var provider = _serviceProvider.GetRequiredService<ITenantProvider>();
        await manager.CreateTenantAsync("tenant1", "Tenant One");

        // Act
        await manager.SetCurrentTenantAsync("tenant1");
        var currentId = provider.GetCurrentTenantId();

        // Assert
        currentId.Should().Be("tenant1");
    }

    [Fact]
    public async Task TenantManager_ShouldThrowWhenSettingNonExistentTenant()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<TenantManager>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            manager.SetCurrentTenantAsync("nonexistent"));
    }

    [Fact]
    public async Task TenantManager_ShouldDeactivateTenant()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<TenantManager>();
        await manager.CreateTenantAsync("tenant1", "Tenant One");

        // Act
        await manager.DeactivateTenantAsync("tenant1");
        var tenant = await manager.GetTenantAsync("tenant1");

        // Assert
        tenant!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task TenantManager_ShouldThrowWhenSettingInactiveTenant()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<TenantManager>();
        await manager.CreateTenantAsync("tenant1", "Tenant One");
        await manager.DeactivateTenantAsync("tenant1");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            manager.SetCurrentTenantAsync("tenant1"));
    }

    [Fact]
    public async Task TenantManager_ShouldExecuteInTenantContext()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<TenantManager>();
        var provider = _serviceProvider.GetRequiredService<ITenantProvider>();
        await manager.CreateTenantAsync("tenant1", "Tenant One");
        await manager.CreateTenantAsync("tenant2", "Tenant Two");

        string? capturedTenantId = null;

        // Act
        await manager.ExecuteInTenantContextAsync("tenant1", async () =>
        {
            await Task.Delay(1);
            capturedTenantId = provider.GetCurrentTenantId();
        });

        var currentTenant = provider.GetCurrentTenantId();

        // Assert
        capturedTenantId.Should().Be("tenant1");
        currentTenant.Should().BeNull(); // Context should be cleared after execution
    }

    [Fact]
    public async Task TenantManager_ShouldExecuteInTenantContextWithResult()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<TenantManager>();
        await manager.CreateTenantAsync("tenant1", "Tenant One");

        // Act
        var result = await manager.ExecuteInTenantContextAsync("tenant1", async () =>
        {
            await Task.Delay(1);
            return "success";
        });

        // Assert
        result.Should().Be("success");
    }

    [Fact]
    public async Task TenantManager_ShouldRestorePreviousTenantContext()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<TenantManager>();
        var provider = _serviceProvider.GetRequiredService<ITenantProvider>();
        await manager.CreateTenantAsync("tenant1", "Tenant One");
        await manager.CreateTenantAsync("tenant2", "Tenant Two");
        
        await manager.SetCurrentTenantAsync("tenant1");

        // Act
        await manager.ExecuteInTenantContextAsync("tenant2", async () =>
        {
            await Task.Delay(1);
            provider.GetCurrentTenantId().Should().Be("tenant2");
        });

        var restored = provider.GetCurrentTenantId();

        // Assert
        restored.Should().Be("tenant1"); // Should restore to tenant1
    }

    [Fact]
    public async Task AsyncLocalProvider_ShouldMaintainTenantAcrossAsyncCalls()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<TenantManager>();
        var provider = _serviceProvider.GetRequiredService<ITenantProvider>();
        await manager.CreateTenantAsync("tenant1", "Tenant One");
        await manager.SetCurrentTenantAsync("tenant1");

        // Act
        var tenantBeforeAwait = provider.GetCurrentTenantId();
        await Task.Delay(10);
        var tenantAfterAwait = provider.GetCurrentTenantId();

        // Assert
        tenantBeforeAwait.Should().Be("tenant1");
        tenantAfterAwait.Should().Be("tenant1");
    }

    [Fact]
    public async Task TenantContext_ShouldSupportDifferentIsolationStrategies()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<TenantManager>();

        // Act
        var discriminatorTenant = await manager.CreateTenantAsync(
            "tenant1", "Discriminator", TenantIsolationStrategy.Discriminator);
        
        var schemaTenant = await manager.CreateTenantAsync(
            "tenant2", "Schema", TenantIsolationStrategy.Schema, schema: "tenant2_schema");
        
        var databaseTenant = await manager.CreateTenantAsync(
            "tenant3", "Database", TenantIsolationStrategy.Database, 
            connectionString: "Server=localhost;Database=tenant3");

        // Assert
        discriminatorTenant.IsolationStrategy.Should().Be(TenantIsolationStrategy.Discriminator);
        schemaTenant.IsolationStrategy.Should().Be(TenantIsolationStrategy.Schema);
        schemaTenant.Schema.Should().Be("tenant2_schema");
        databaseTenant.IsolationStrategy.Should().Be(TenantIsolationStrategy.Database);
        databaseTenant.ConnectionString.Should().Be("Server=localhost;Database=tenant3");
    }

    [Fact]
    public async Task TenantContext_ShouldSupportMetadata()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<TenantManager>();
        var tenant = await manager.CreateTenantAsync("tenant1", "Tenant One");
        tenant.Metadata = new Dictionary<string, object>
        {
            { "Region", "US-West" },
            { "Plan", "Enterprise" },
            { "MaxUsers", 1000 }
        };

        var store = _serviceProvider.GetRequiredService<ITenantStore>();
        await store.UpdateAsync(tenant);

        // Act
        var retrieved = await store.GetByIdAsync("tenant1");

        // Assert
        retrieved!.Metadata.Should().NotBeNull();
        retrieved.Metadata!["Region"].Should().Be("US-West");
        retrieved.Metadata["Plan"].Should().Be("Enterprise");
        retrieved.Metadata["MaxUsers"].Should().Be(1000);
    }
}
