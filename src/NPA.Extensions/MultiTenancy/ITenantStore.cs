using NPA.Core.MultiTenancy;

namespace NPA.Extensions.MultiTenancy;

/// <summary>
/// Manages tenant registration, lookup, and lifecycle.
/// </summary>
public interface ITenantStore
{
    /// <summary>
    /// Registers a new tenant.
    /// </summary>
    /// <param name="tenant">The tenant context to register</param>
    Task RegisterAsync(TenantContext tenant);

    /// <summary>
    /// Gets a tenant by ID.
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <returns>The tenant context, or null if not found</returns>
    Task<TenantContext?> GetByIdAsync(string tenantId);

    /// <summary>
    /// Gets all registered tenants.
    /// </summary>
    /// <returns>All tenants</returns>
    Task<IEnumerable<TenantContext>> GetAllAsync();

    /// <summary>
    /// Updates an existing tenant.
    /// </summary>
    /// <param name="tenant">The tenant to update</param>
    Task UpdateAsync(TenantContext tenant);

    /// <summary>
    /// Removes a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID to remove</param>
    Task RemoveAsync(string tenantId);

    /// <summary>
    /// Checks if a tenant exists.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>True if tenant exists</returns>
    Task<bool> ExistsAsync(string tenantId);
}

/// <summary>
/// In-memory implementation of <see cref="ITenantStore"/>.
/// Suitable for development and testing. Use a persistent store in production.
/// </summary>
public class InMemoryTenantStore : ITenantStore
{
    private readonly Dictionary<string, TenantContext> _tenants = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public Task RegisterAsync(TenantContext tenant)
    {
        if (tenant == null) throw new ArgumentNullException(nameof(tenant));
        if (string.IsNullOrWhiteSpace(tenant.TenantId))
            throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenant));

        lock (_lock)
        {
            if (_tenants.ContainsKey(tenant.TenantId))
                throw new InvalidOperationException($"Tenant '{tenant.TenantId}' already exists");

            _tenants[tenant.TenantId] = tenant;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<TenantContext?> GetByIdAsync(string tenantId)
    {
        lock (_lock)
        {
            return Task.FromResult(_tenants.TryGetValue(tenantId, out var tenant) ? tenant : null);
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<TenantContext>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult<IEnumerable<TenantContext>>(_tenants.Values.ToList());
        }
    }

    /// <inheritdoc />
    public Task UpdateAsync(TenantContext tenant)
    {
        if (tenant == null) throw new ArgumentNullException(nameof(tenant));

        lock (_lock)
        {
            if (!_tenants.ContainsKey(tenant.TenantId))
                throw new InvalidOperationException($"Tenant '{tenant.TenantId}' not found");

            _tenants[tenant.TenantId] = tenant;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string tenantId)
    {
        lock (_lock)
        {
            _tenants.Remove(tenantId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string tenantId)
    {
        lock (_lock)
        {
            return Task.FromResult(_tenants.ContainsKey(tenantId));
        }
    }
}
