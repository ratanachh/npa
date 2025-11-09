namespace NPA.Core.Annotations;

/// <summary>
/// Indicates that a method will have a custom implementation provided by the developer.
/// The generator will create a partial method declaration that you can implement.
/// </summary>
/// <example>
/// <code>
/// public interface IUserRepository
/// {
///     [CustomImplementation]
///     Task&lt;User?&gt; FindByComplexCriteriaAsync(SearchCriteria criteria);
/// }
/// 
/// // In your partial class:
/// public partial class UserRepository
/// {
///     public partial Task&lt;User?&gt; FindByComplexCriteriaAsync(SearchCriteria criteria)
///     {
///         // Your custom implementation
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class CustomImplementationAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether to generate a partial method stub for implementation.
    /// Default is true.
    /// </summary>
    public bool GeneratePartialStub { get; set; } = true;

    /// <summary>
    /// Gets or sets a description of what the custom implementation should do.
    /// This will be included as a comment in the generated code.
    /// </summary>
    public string? ImplementationHint { get; set; }

    /// <summary>
    /// Gets or sets whether the custom implementation is required.
    /// If true, the generator will produce a compile error if not implemented.
    /// Default is true.
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomImplementationAttribute"/> class.
    /// </summary>
    public CustomImplementationAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomImplementationAttribute"/> class with an implementation hint.
    /// </summary>
    /// <param name="implementationHint">A hint about what the implementation should do.</param>
    public CustomImplementationAttribute(string implementationHint)
    {
        ImplementationHint = implementationHint;
    }
}
