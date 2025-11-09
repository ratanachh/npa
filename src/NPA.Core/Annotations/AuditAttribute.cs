namespace NPA.Core.Annotations;

/// <summary>
/// Indicates that a method should generate audit log entries.
/// The generator will create audit records tracking who, when, and what was modified.
/// </summary>
/// <example>
/// <code>
/// [Audit]  // Use default settings
/// Task CreateAsync(User user);
/// 
/// [Audit(IncludeOldValue = true, IncludeNewValue = true)]
/// Task UpdateAsync(User user);
/// 
/// [Audit(Category = "Security", Severity = AuditSeverity.High)]
/// Task DeleteAsync(int id);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class AuditAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether to include the old value in the audit log (for update operations).
    /// Default is false.
    /// </summary>
    public bool IncludeOldValue { get; set; }

    /// <summary>
    /// Gets or sets whether to include the new value in the audit log.
    /// Default is true.
    /// </summary>
    public bool IncludeNewValue { get; set; } = true;

    /// <summary>
    /// Gets or sets the audit category for filtering and reporting.
    /// Default is "Data".
    /// </summary>
    public string Category { get; set; } = "Data";

    /// <summary>
    /// Gets or sets the audit severity level.
    /// Default is Normal.
    /// </summary>
    public AuditSeverity Severity { get; set; } = AuditSeverity.Normal;

    /// <summary>
    /// Gets or sets whether to include parameter values in the audit log.
    /// Default is true.
    /// </summary>
    public bool IncludeParameters { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to capture the user/principal who performed the action.
    /// Default is true.
    /// </summary>
    public bool CaptureUser { get; set; } = true;

    /// <summary>
    /// Gets or sets the description of the audited action.
    /// If not specified, a description will be generated from the method name.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether to include the IP address of the caller.
    /// Default is false.
    /// </summary>
    public bool CaptureIpAddress { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditAttribute"/> class.
    /// </summary>
    public AuditAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditAttribute"/> class with a category.
    /// </summary>
    /// <param name="category">The audit category.</param>
    public AuditAttribute(string category)
    {
        Category = category;
    }
}

/// <summary>
/// Defines severity levels for audit entries.
/// </summary>
public enum AuditSeverity
{
    /// <summary>
    /// Low severity - informational only.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal severity - standard operations.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High severity - sensitive operations.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical severity - security-sensitive operations.
    /// </summary>
    Critical = 3
}
