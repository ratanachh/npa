using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Core.Metadata;
using System.Reflection;

namespace NPA.Core.Extensions;

/// <summary>
/// Extension methods for configuring NPA Core services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the NPA metadata provider to the service collection.
    /// Automatically uses generated metadata provider (from Phase 2.6 generator) if available for optimal performance (10-100x faster),
    /// otherwise falls back to reflection-based provider for backward compatibility.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    public static IServiceCollection AddNpaMetadataProvider(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        var generatedProviderType = FindGeneratedMetadataProvider();
        
        if (generatedProviderType != null)
        {
            // Fast path: Use generated metadata provider for 10-100x performance improvement
            services.AddSingleton(typeof(IMetadataProvider), generatedProviderType);
        }
        else
        {
            // Slow path: Use reflection-based metadata provider as fallback
            services.AddSingleton<IMetadataProvider, MetadataProvider>();
        }

        return services;
    }

    /// <summary>
    /// Finds the generated metadata provider type if it exists in any loaded assembly.
    /// The generator creates NPA.Generated.GeneratedMetadataProvider when [Entity] classes are present.
    /// </summary>
    /// <returns>The generated provider type if found; otherwise, null.</returns>
    private static Type? FindGeneratedMetadataProvider()
    {
        const string GeneratedProviderTypeName = "NPA.Generated.GeneratedMetadataProvider";
        
        // Strategy 1: Check entry assembly (most common case - ~95% of scenarios)
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            var type = entryAssembly.GetType(GeneratedProviderTypeName);
            if (IsValidGeneratedProvider(type))
                return type;
        }
        
        // Strategy 2: Check calling assembly (for library scenarios)
        var callingAssembly = Assembly.GetCallingAssembly();
        if (callingAssembly != null && callingAssembly != entryAssembly)
        {
            var type = callingAssembly.GetType(GeneratedProviderTypeName);
            if (IsValidGeneratedProvider(type))
                return type;
        }
        
        // Strategy 3: Scan all loaded non-system assemblies (fallback for complex scenarios)
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Skip system and framework assemblies for performance
            var assemblyName = assembly.FullName;
            if (assemblyName == null)
                continue;
                
            if (assemblyName.StartsWith("System.") ||
                assemblyName.StartsWith("Microsoft.") ||
                assemblyName.StartsWith("mscorlib") ||
                assemblyName.StartsWith("netstandard"))
                continue;
            
            var type = assembly.GetType(GeneratedProviderTypeName);
            if (IsValidGeneratedProvider(type))
                return type;
        }
        
        return null;
    }

    /// <summary>
    /// Validates that the type is a valid generated metadata provider.
    /// </summary>
    /// <param name="type">The type to validate.</param>
    /// <returns>True if the type is valid; otherwise, false.</returns>
    private static bool IsValidGeneratedProvider(Type? type)
    {
        if (type == null)
            return false;
        
        // Verify it implements IMetadataProvider
        if (!typeof(IMetadataProvider).IsAssignableFrom(type))
            return false;
        
        // Verify it's in the correct namespace
        if (type.Namespace != "NPA.Generated")
            return false;
        
        // Verify it has a parameterless constructor (for DI instantiation)
        var constructor = type.GetConstructor(Type.EmptyTypes);
        if (constructor == null)
            return false;
        
        return true;
    }
}

