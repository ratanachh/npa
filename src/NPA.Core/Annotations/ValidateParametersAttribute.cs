namespace NPA.Core.Annotations;

/// <summary>
/// Indicates that method parameters should be validated before execution.
/// The generator will add null checks, range checks, and other validations.
/// </summary>
/// <example>
/// <code>
/// [ValidateParameters]
/// Task&lt;User?&gt; GetByIdAsync([Range(1, int.MaxValue)] int id);
/// 
/// [ValidateParameters(ThrowOnNull = true)]
/// Task&lt;void&gt; UpdateAsync([NotNull] User user);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ValidateParametersAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether to throw ArgumentNullException for null reference parameters.
    /// Default is true.
    /// </summary>
    public bool ThrowOnNull { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate string parameters are not empty.
    /// Default is false.
    /// </summary>
    public bool ValidateStringsNotEmpty { get; set; }

    /// <summary>
    /// Gets or sets whether to validate collection parameters are not empty.
    /// Default is false.
    /// </summary>
    public bool ValidateCollectionsNotEmpty { get; set; }

    /// <summary>
    /// Gets or sets whether to validate numeric parameters are positive.
    /// Default is false.
    /// </summary>
    public bool ValidatePositive { get; set; }

    /// <summary>
    /// Gets or sets a custom validation error message pattern.
    /// Use {paramName} as a placeholder for the parameter name.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateParametersAttribute"/> class.
    /// </summary>
    public ValidateParametersAttribute()
    {
    }
}
