namespace NPA.Core.Annotations;

/// <summary>
/// Indicates that a property, method, or class should be ignored during code generation.
/// This allows you to exclude certain members from generated repositories or metadata.
/// </summary>
/// <example>
/// <code>
/// public class User
/// {
///     public int Id { get; set; }
///     
///     [IgnoreInGeneration]
///     public string TemporaryData { get; set; }  // Won't be included in generated code
/// }
/// 
/// public interface IUserRepository
/// {
///     [IgnoreInGeneration]
///     Task&lt;void&gt; CustomMethod();  // Generator will skip this method
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = false)]
public sealed class IgnoreInGenerationAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the reason why this member is ignored.
    /// This is for documentation purposes.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IgnoreInGenerationAttribute"/> class.
    /// </summary>
    public IgnoreInGenerationAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IgnoreInGenerationAttribute"/> class with a reason.
    /// </summary>
    /// <param name="reason">The reason why this member is ignored.</param>
    public IgnoreInGenerationAttribute(string reason)
    {
        Reason = reason;
    }
}
