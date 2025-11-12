namespace NPA.Core.Configuration;

/// <summary>
/// Configuration options for database connection pooling.
/// These settings are translated to database-specific connection string parameters.
/// </summary>
public class ConnectionPoolOptions
{
    /// <summary>
    /// Gets or sets whether connection pooling is enabled.
    /// Default: true (pooling enabled for better performance).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum number of connections to maintain in the pool.
    /// Default: 5 connections.
    /// Set to 0 for lazy initialization (connections created on demand).
    /// </summary>
    public int MinPoolSize { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum number of connections allowed in the pool.
    /// Default: 100 connections.
    /// Increase for high-traffic scenarios, decrease for resource-constrained environments.
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the timeout for establishing a new connection.
    /// Default: 30 seconds.
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum lifetime of a connection before it's recycled.
    /// Default: null (unlimited lifetime).
    /// Setting this helps with load balancing and prevents stale connections.
    /// Recommended: 15-30 minutes for production.
    /// </summary>
    public TimeSpan? ConnectionLifetime { get; set; } = null;

    /// <summary>
    /// Gets or sets how long a connection can be idle before being removed from the pool.
    /// Default: 5 minutes.
    /// Set to null to disable idle timeout.
    /// </summary>
    public TimeSpan? IdleTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets whether to reset connection state when returning to the pool.
    /// Default: true (ensures clean state for next use).
    /// Disabling may improve performance but can cause unexpected behavior.
    /// </summary>
    public bool ResetOnReturn { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate connections before use.
    /// Default: true (pings connection to ensure it's alive).
    /// </summary>
    public bool ValidateOnAcquire { get; set; } = true;
}
