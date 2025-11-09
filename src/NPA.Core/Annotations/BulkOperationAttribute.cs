namespace NPA.Core.Annotations;

/// <summary>
/// Specifies that a repository method performs a bulk operation.
/// The method signature determines the operation type (insert, update, delete).
/// </summary>
/// <example>
/// <code>
/// [BulkOperation]
/// Task&lt;int&gt; BulkInsertAsync(IEnumerable&lt;User&gt; users);
/// 
/// [BulkOperation(BatchSize = 1000)]
/// Task&lt;int&gt; BulkUpdateAsync(IEnumerable&lt;User&gt; users);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class BulkOperationAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the batch size for bulk operations.
    /// Default is 1000.
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    public int? CommandTimeout { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use transactions for bulk operations.
    /// Default is true.
    /// </summary>
    public bool UseTransaction { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkOperationAttribute"/> class.
    /// </summary>
    public BulkOperationAttribute()
    {
    }
}
