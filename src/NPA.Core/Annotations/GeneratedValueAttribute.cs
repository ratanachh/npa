namespace NPA.Core.Annotations;

/// <summary>
/// Specifies how the primary key value is generated.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class GeneratedValueAttribute : Attribute
{
    /// <summary>
    /// Gets the generation strategy.
    /// </summary>
    public GenerationType Strategy { get; }

    /// <summary>
    /// Gets or sets the name of the generator (for Sequence and Table strategies).
    /// </summary>
    public string? Generator { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratedValueAttribute"/> class with the specified strategy.
    /// </summary>
    /// <param name="strategy">The generation strategy to use.</param>
    public GeneratedValueAttribute(GenerationType strategy)
    {
        Strategy = strategy;
    }
}
