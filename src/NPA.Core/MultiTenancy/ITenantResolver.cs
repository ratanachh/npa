namespace NPA.Core.MultiTenancy;

/// <summary>
/// Resolves the tenant from various sources (HTTP headers, claims, host, etc.).
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Resolves the tenant identifier from the current context.
    /// </summary>
    /// <returns>The tenant ID, or null if resolution fails</returns>
    Task<string?> ResolveAsync();
}
