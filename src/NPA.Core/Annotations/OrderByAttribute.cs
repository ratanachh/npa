namespace NPA.Core.Annotations;

/// <summary>
/// Specifies ordering for a query method.
/// Can be used on methods or parameters to define sort order.
/// </summary>
/// <example>
/// <code>
/// [OrderBy("CreatedAt", Direction = SortDirection.Descending)]
/// Task&lt;IEnumerable&lt;User&gt;&gt; GetRecentUsersAsync();
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class OrderByAttribute : Attribute
{
    /// <summary>
    /// Gets the property name to order by.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets or sets the sort direction.
    /// </summary>
    public SortDirection Direction { get; set; } = SortDirection.Ascending;

    /// <summary>
    /// Gets or sets the order priority when multiple OrderBy attributes are used.
    /// Lower values are applied first.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderByAttribute"/> class.
    /// </summary>
    /// <param name="propertyName">The property name to order by.</param>
    public OrderByAttribute(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        
        PropertyName = propertyName;
    }
}

/// <summary>
/// Specifies the sort direction for ordering.
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// Ascending order (A-Z, 0-9, oldest first).
    /// </summary>
    Ascending,
    
    /// <summary>
    /// Descending order (Z-A, 9-0, newest first).
    /// </summary>
    Descending
}
