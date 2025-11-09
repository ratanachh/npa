namespace NPA.Core.Annotations;

/// <summary>
/// Indicates that a method call should be automatically retried on failure.
/// The generator will wrap the method with retry logic using exponential backoff.
/// </summary>
/// <example>
/// <code>
/// [RetryOnFailure(MaxAttempts = 3, DelayMilliseconds = 100)]
/// Task&lt;User?&gt; GetByIdAsync(int id);
/// 
/// [RetryOnFailure(MaxAttempts = 5, RetryOn = typeof(TimeoutException))]
/// Task&lt;void&gt; UpdateAsync(User user);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RetryOnFailureAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// Default is 3.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial delay in milliseconds before the first retry.
    /// Default is 100ms.
    /// </summary>
    public int DelayMilliseconds { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to use exponential backoff for retry delays.
    /// If true, delay doubles on each retry.
    /// Default is true.
    /// </summary>
    public bool ExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum delay in milliseconds between retries.
    /// Default is 30000ms (30 seconds).
    /// </summary>
    public int MaxDelayMilliseconds { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the specific exception types to retry on.
    /// If not specified, retries on all exceptions.
    /// </summary>
    /// <example>
    /// RetryOn = new[] { typeof(TimeoutException), typeof(DbException) }
    /// </example>
    public Type[]? RetryOn { get; set; }

    /// <summary>
    /// Gets or sets whether to log retry attempts.
    /// Default is true.
    /// </summary>
    public bool LogRetries { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryOnFailureAttribute"/> class.
    /// </summary>
    public RetryOnFailureAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryOnFailureAttribute"/> class with max attempts.
    /// </summary>
    /// <param name="maxAttempts">The maximum number of retry attempts.</param>
    public RetryOnFailureAttribute(int maxAttempts)
    {
        MaxAttempts = maxAttempts;
    }
}
