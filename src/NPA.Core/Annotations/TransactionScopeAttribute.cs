namespace NPA.Core.Annotations;

/// <summary>
/// Controls transaction behavior for a generated repository method.
/// The generator will wrap the method with transaction management code.
/// </summary>
/// <example>
/// <code>
/// [TransactionScope(IsolationLevel = IsolationLevel.ReadCommitted)]
/// Task&lt;void&gt; UpdateUserAndOrdersAsync(int userId, Order[] orders);
/// 
/// [TransactionScope(Required = false)]  // Don't use transaction
/// Task&lt;IEnumerable&lt;User&gt;&gt; GetAllAsync();
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class TransactionScopeAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether a transaction is required for this method.
    /// Default is true.
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Gets or sets the transaction isolation level.
    /// Default is ReadCommitted.
    /// </summary>
    public System.Data.IsolationLevel IsolationLevel { get; set; } = System.Data.IsolationLevel.ReadCommitted;

    /// <summary>
    /// Gets or sets the transaction timeout in seconds.
    /// Default is 30 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to automatically roll back on exception.
    /// Default is true.
    /// </summary>
    public bool AutoRollbackOnError { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use an existing ambient transaction.
    /// Default is true (join existing if available).
    /// </summary>
    public bool JoinAmbientTransaction { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionScopeAttribute"/> class.
    /// </summary>
    public TransactionScopeAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionScopeAttribute"/> class with isolation level.
    /// </summary>
    /// <param name="isolationLevel">The transaction isolation level.</param>
    public TransactionScopeAttribute(System.Data.IsolationLevel isolationLevel)
    {
        IsolationLevel = isolationLevel;
    }
}
