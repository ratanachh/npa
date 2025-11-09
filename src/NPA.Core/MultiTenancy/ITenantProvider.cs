namespace NPA.Core.MultiTenancy;

/// <summary>
/// Provides information about the current tenant.
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// Gets the current tenant identifier.
    /// </summary>
    /// <returns>The tenant ID, or null if no tenant context is set</returns>
    string? GetCurrentTenantId();

    /// <summary>
    /// Gets the current tenant context with additional metadata.
    /// </summary>
    /// <returns>The tenant context, or null if no tenant is set</returns>
    TenantContext? GetCurrentTenant();

    /// <summary>
    /// Sets the current tenant for the operation scope.
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    void SetCurrentTenant(string tenantId);

    /// <summary>
    /// Clears the current tenant context.
    /// </summary>
    void ClearCurrentTenant();
}
