using Microsoft.Extensions.Logging;
using NPA.Core.MultiTenancy;

namespace NPA.Extensions.MultiTenancy;

/// <summary>
/// High-level service for managing tenants and tenant context.
/// </summary>
public class TenantManager
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ITenantStore _tenantStore;
    private readonly ILogger<TenantManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantManager"/> class.
    /// </summary>
    public TenantManager(
        ITenantProvider tenantProvider,
        ITenantStore tenantStore,
        ILogger<TenantManager> logger)
    {
        _tenantProvider = tenantProvider;
        _tenantStore = tenantStore;
        _logger = logger;
    }

    /// <summary>
    /// Creates and registers a new tenant.
    /// </summary>
    /// <param name="tenantId">The unique tenant identifier</param>
    /// <param name="name">The tenant name</param>
    /// <param name="isolationStrategy">The isolation strategy</param>
    /// <param name="connectionString">Optional connection string for database-per-tenant</param>
    /// <param name="schema">Optional schema for schema-per-tenant</param>
    /// <returns>The created tenant context</returns>
    public async Task<TenantContext> CreateTenantAsync(
        string tenantId,
        string name,
        TenantIsolationStrategy isolationStrategy = TenantIsolationStrategy.Discriminator,
        string? connectionString = null,
        string? schema = null)
    {
        var tenant = new TenantContext
        {
            TenantId = tenantId,
            Name = name,
            IsolationStrategy = isolationStrategy,
            ConnectionString = connectionString,
            Schema = schema,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _tenantStore.RegisterAsync(tenant);
        _logger.LogInformation("Tenant '{TenantId}' created with strategy {Strategy}", tenantId, isolationStrategy);

        return tenant;
    }

    /// <summary>
    /// Sets the current tenant context for the operation scope.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    public async Task SetCurrentTenantAsync(string tenantId)
    {
        var tenant = await _tenantStore.GetByIdAsync(tenantId);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant '{tenantId}' not found");
        }

        if (!tenant.IsActive)
        {
            throw new InvalidOperationException($"Tenant '{tenantId}' is not active");
        }

        if (_tenantProvider is AsyncLocalTenantProvider asyncProvider)
        {
            asyncProvider.SetTenantContext(tenant);
        }
        else
        {
            _tenantProvider.SetCurrentTenant(tenantId);
        }

        _logger.LogDebug("Current tenant set to '{TenantId}'", tenantId);
    }

    /// <summary>
    /// Executes an action within a specific tenant context.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="action">The action to execute</param>
    public async Task ExecuteInTenantContextAsync(string tenantId, Func<Task> action)
    {
        var previousTenant = _tenantProvider.GetCurrentTenantId();

        try
        {
            await SetCurrentTenantAsync(tenantId);
            await action();
        }
        finally
        {
            if (previousTenant != null)
            {
                await SetCurrentTenantAsync(previousTenant);
            }
            else
            {
                _tenantProvider.ClearCurrentTenant();
            }
        }
    }

    /// <summary>
    /// Executes a function within a specific tenant context and returns the result.
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="func">The function to execute</param>
    /// <returns>The function result</returns>
    public async Task<T> ExecuteInTenantContextAsync<T>(string tenantId, Func<Task<T>> func)
    {
        var previousTenant = _tenantProvider.GetCurrentTenantId();

        try
        {
            await SetCurrentTenantAsync(tenantId);
            return await func();
        }
        finally
        {
            if (previousTenant != null)
            {
                await SetCurrentTenantAsync(previousTenant);
            }
            else
            {
                _tenantProvider.ClearCurrentTenant();
            }
        }
    }

    /// <summary>
    /// Gets all registered tenants.
    /// </summary>
    public Task<IEnumerable<TenantContext>> GetAllTenantsAsync()
    {
        return _tenantStore.GetAllAsync();
    }

    /// <summary>
    /// Gets a tenant by ID.
    /// </summary>
    public Task<TenantContext?> GetTenantAsync(string tenantId)
    {
        return _tenantStore.GetByIdAsync(tenantId);
    }

    /// <summary>
    /// Deactivates a tenant (soft delete).
    /// </summary>
    public async Task DeactivateTenantAsync(string tenantId)
    {
        var tenant = await _tenantStore.GetByIdAsync(tenantId);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant '{tenantId}' not found");
        }

        tenant.IsActive = false;
        await _tenantStore.UpdateAsync(tenant);

        _logger.LogInformation("Tenant '{TenantId}' deactivated", tenantId);
    }

    /// <summary>
    /// Removes a tenant completely.
    /// </summary>
    public async Task RemoveTenantAsync(string tenantId)
    {
        await _tenantStore.RemoveAsync(tenantId);
        _logger.LogWarning("Tenant '{TenantId}' removed", tenantId);
    }
}
