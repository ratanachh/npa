namespace NPA.Core.Annotations;

/// <summary>
/// Marks an entity as multi-tenant, automatically filtering queries by tenant.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class MultiTenantAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MultiTenantAttribute"/> class.
    /// </summary>
    /// <param name="tenantIdProperty">
    /// The name of the property that stores the tenant ID. Defaults to "TenantId".
    /// </param>
    public MultiTenantAttribute(string tenantIdProperty = "TenantId")
    {
        TenantIdProperty = tenantIdProperty;
    }

    /// <summary>
    /// Gets the name of the property that stores the tenant ID.
    /// </summary>
    public string TenantIdProperty { get; }

    /// <summary>
    /// Gets or sets whether to enforce tenant isolation in queries.
    /// When true, all queries automatically filter by current tenant.
    /// </summary>
    public bool EnforceTenantIsolation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to allow cross-tenant queries with explicit bypass.
    /// When false, cross-tenant queries throw an exception.
    /// </summary>
    public bool AllowCrossTenantQueries { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to validate tenant on insert/update.
    /// When true, ensures TenantId is set before persisting.
    /// </summary>
    public bool ValidateTenantOnWrite { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to auto-populate TenantId from current context on insert.
    /// </summary>
    public bool AutoPopulateTenantId { get; set; } = true;
}
