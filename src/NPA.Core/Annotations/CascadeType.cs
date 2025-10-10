namespace NPA.Core.Annotations;

/// <summary>
/// Defines the cascade operations that should be applied to related entities.
/// Similar to JPA CascadeType.
/// </summary>
[Flags]
public enum CascadeType
{
    /// <summary>
    /// No cascade operations.
    /// </summary>
    None = 0,

    /// <summary>
    /// Cascade persist (save) operations.
    /// When an entity is persisted, cascade to related entities.
    /// </summary>
    Persist = 1 << 0,

    /// <summary>
    /// Cascade merge (update) operations.
    /// When an entity is merged, cascade to related entities.
    /// </summary>
    Merge = 1 << 1,

    /// <summary>
    /// Cascade remove (delete) operations.
    /// When an entity is removed, cascade to related entities.
    /// </summary>
    Remove = 1 << 2,

    /// <summary>
    /// Cascade refresh operations.
    /// When an entity is refreshed, cascade to related entities.
    /// </summary>
    Refresh = 1 << 3,

    /// <summary>
    /// Cascade detach operations.
    /// When an entity is detached, cascade to related entities.
    /// </summary>
    Detach = 1 << 4,

    /// <summary>
    /// Cascade all operations (Persist, Merge, Remove, Refresh, Detach).
    /// </summary>
    All = Persist | Merge | Remove | Refresh | Detach
}

