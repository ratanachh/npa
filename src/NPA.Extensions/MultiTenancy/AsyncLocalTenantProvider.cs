using NPA.Core.MultiTenancy;

namespace NPA.Extensions.MultiTenancy;

/// <summary>
/// Thread-safe, async-local implementation of <see cref="ITenantProvider"/>.
/// Stores tenant context in AsyncLocal for proper async flow.
/// </summary>
public class AsyncLocalTenantProvider : ITenantProvider
{
    private static readonly AsyncLocal<TenantContext?> _tenantContext = new();

    /// <inheritdoc />
    public string? GetCurrentTenantId()
    {
        return _tenantContext.Value?.TenantId;
    }

    /// <inheritdoc />
    public TenantContext? GetCurrentTenant()
    {
        return _tenantContext.Value;
    }

    /// <inheritdoc />
    public void SetCurrentTenant(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
        }

        _tenantContext.Value = new TenantContext
        {
            TenantId = tenantId
        };
    }

    /// <inheritdoc />
    public void ClearCurrentTenant()
    {
        _tenantContext.Value = null;
    }

    /// <summary>
    /// Sets the full tenant context (used by tenant store during resolution).
    /// </summary>
    /// <param name="context">The tenant context</param>
    public void SetTenantContext(TenantContext context)
    {
        _tenantContext.Value = context ?? throw new ArgumentNullException(nameof(context));
    }
}
