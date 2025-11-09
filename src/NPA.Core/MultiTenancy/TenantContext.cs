namespace NPA.Core.MultiTenancy;

/// <summary>
/// Represents the context of a tenant including metadata.
/// </summary>
public class TenantContext
{
    /// <summary>
    /// Gets or sets the unique tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the tenant's connection string (for database-per-tenant strategy).
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the tenant's database schema (for schema-per-tenant strategy).
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// Gets or sets the isolation strategy for this tenant.
    /// </summary>
    public TenantIsolationStrategy IsolationStrategy { get; set; } = TenantIsolationStrategy.Discriminator;

    /// <summary>
    /// Gets or sets additional metadata for the tenant.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets whether the tenant is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when the tenant was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Defines the tenant isolation strategies.
/// </summary>
public enum TenantIsolationStrategy
{
    /// <summary>
    /// Single database with TenantId discriminator column in shared tables.
    /// Most cost-effective, easiest to maintain.
    /// </summary>
    Discriminator = 0,

    /// <summary>
    /// Separate schema per tenant in the same database.
    /// Good isolation with shared infrastructure.
    /// </summary>
    Schema = 1,

    /// <summary>
    /// Separate database per tenant.
    /// Maximum isolation and performance, higher maintenance cost.
    /// </summary>
    Database = 2
}
