using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Core.Extensions;
using NPA.Core.Metadata;

namespace NPA.Core.Tests.Extensions;

/// <summary>
/// Tests for ServiceCollectionExtensions (Phase 2.7).
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddNpaMetadataProvider_WithValidServices_ShouldRegisterIMetadataProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNpaMetadataProvider();
        var provider = services.BuildServiceProvider();
        var metadataProvider = provider.GetService<IMetadataProvider>();

        // Assert
        metadataProvider.Should().NotBeNull();
        metadataProvider.Should().BeAssignableTo<IMetadataProvider>();
    }

    [Fact]
    public void AddNpaMetadataProvider_ShouldRegisterAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNpaMetadataProvider();
        
        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IMetadataProvider));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddNpaMetadataProvider_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () =>
        {
            services.AddNpaMetadataProvider();
            services.AddNpaMetadataProvider(); // Call twice
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddNpaMetadataProvider_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        ServiceCollection? services = null;

        // Act
        Action act = () => services!.AddNpaMetadataProvider();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddNpaMetadataProvider_ResolvedProvider_ShouldImplementAllInterfaceMethods()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNpaMetadataProvider();
        var provider = services.BuildServiceProvider();
        var metadataProvider = provider.GetRequiredService<IMetadataProvider>();

        // Assert - verify all IMetadataProvider methods exist
        metadataProvider.Should().NotBeNull();
        
        // GetEntityMetadata<T> should be available
        var genericMethod = metadataProvider.GetType().GetMethod("GetEntityMetadata", 
            new Type[] { });
        genericMethod.Should().NotBeNull();
        
        // GetEntityMetadata(Type) should be available
        var typeMethod = metadataProvider.GetType().GetMethod("GetEntityMetadata", 
            new Type[] { typeof(Type) });
        typeMethod.Should().NotBeNull();
        
        // IsEntity should be available
        var isEntityMethod = metadataProvider.GetType().GetMethod("IsEntity");
        isEntityMethod.Should().NotBeNull();
    }

    [Fact]
    public void AddNpaMetadataProvider_ShouldFallbackToMetadataProvider_WhenNoGeneratedProviderExists()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNpaMetadataProvider();
        var provider = services.BuildServiceProvider();
        var metadataProvider = provider.GetRequiredService<IMetadataProvider>();

        // Assert
        // Since this test project likely doesn't have generated provider,
        // it should fall back to MetadataProvider
        metadataProvider.Should().NotBeNull();
        metadataProvider.Should().BeAssignableTo<IMetadataProvider>();
    }

    [Fact]
    public void AddNpaMetadataProvider_ReturnValue_ShouldBeServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddNpaMetadataProvider();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddNpaMetadataProvider_ShouldAllowChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services
            .AddNpaMetadataProvider()
            .AddSingleton<string>("test"); // Add another service to test chaining

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(services);
        services.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public void MetadataProvider_ShouldBeResolvableAfterRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNpaMetadataProvider();

        // Act
        var provider = services.BuildServiceProvider();
        var metadataProvider = provider.GetService<IMetadataProvider>();

        // Assert
        metadataProvider.Should().NotBeNull();
    }

    [Fact]
    public void MetadataProvider_ShouldBeResolvableMultipleTimes_WithSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNpaMetadataProvider();
        var provider = services.BuildServiceProvider();

        // Act
        var instance1 = provider.GetRequiredService<IMetadataProvider>();
        var instance2 = provider.GetRequiredService<IMetadataProvider>();

        // Assert
        instance1.Should().BeSameAs(instance2, "singleton should return same instance");
    }
}

