namespace NPA.Core.Annotations;

/// <summary>
/// Specifies that a query method uses Dapper multi-mapping to map results to multiple related entities.
/// </summary>
/// <example>
/// <code>
/// [MultiMapping("id", SplitOn = "user_id,address_id")]
/// [Query("SELECT u.*, a.*, o.* FROM users u LEFT JOIN addresses a ON u.id = a.user_id LEFT JOIN orders o ON u.id = o.user_id WHERE u.id = @id")]
/// Task&lt;User&gt; GetUserWithRelatedDataAsync(long id);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MultiMappingAttribute : Attribute
{
    /// <summary>
    /// Gets the property name to use as the key for grouping related entities.
    /// </summary>
    public string KeyProperty { get; }

    /// <summary>
    /// Gets or sets the column names to split on when mapping multiple types.
    /// Comma-separated list of column names (e.g., "user_id,address_id").
    /// </summary>
    public string? SplitOn { get; set; }

    /// <summary>
    /// Gets or sets the types to map to, in order.
    /// If not specified, will be inferred from method parameters and return type.
    /// </summary>
    public Type[]? MapTypes { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiMappingAttribute"/> class.
    /// </summary>
    /// <param name="keyProperty">The property name to use as grouping key.</param>
    public MultiMappingAttribute(string keyProperty)
    {
        if (string.IsNullOrWhiteSpace(keyProperty))
            throw new ArgumentException("Key property cannot be null or empty", nameof(keyProperty));
        
        KeyProperty = keyProperty;
    }
}
