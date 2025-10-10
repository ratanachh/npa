namespace NPA.Core.Annotations;

/// <summary>
/// Defines the loading strategy for relationship data.
/// Similar to JPA FetchType.
/// </summary>
public enum FetchType
{
    /// <summary>
    /// Load the relationship eagerly (immediately with the owning entity).
    /// Recommended for relationships that are frequently accessed together.
    /// </summary>
    Eager = 0,

    /// <summary>
    /// Load the relationship lazily (on-demand when accessed).
    /// Recommended for large collections or infrequently accessed relationships.
    /// </summary>
    Lazy = 1
}

