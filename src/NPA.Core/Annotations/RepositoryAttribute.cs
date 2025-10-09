namespace NPA.Core.Annotations;

/// <summary>
/// Marks an interface for automatic repository implementation generation.
/// The source generator will create a concrete implementation with method bodies.
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class RepositoryAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the entity type this repository manages.
    /// </summary>
    public Type? EntityType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to generate default CRUD methods.
    /// </summary>
    public bool GenerateDefaultMethods { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryAttribute"/> class.
    /// </summary>
    public RepositoryAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryAttribute"/> class with the specified entity type.
    /// </summary>
    /// <param name="entityType">The entity type this repository manages.</param>
    public RepositoryAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}

