namespace NPA.Core.Annotations;

/// <summary>
/// Specifies pagination for a query method.
/// </summary>
/// <example>
/// <code>
/// [Paginated(PageSize = 20)]
/// Task&lt;IEnumerable&lt;User&gt;&gt; GetUsersAsync(int page);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PaginatedAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the default page size.
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum allowed page size.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating whether to include total count in results.
    /// </summary>
    public bool IncludeTotalCount { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaginatedAttribute"/> class.
    /// </summary>
    public PaginatedAttribute()
    {
    }
}
