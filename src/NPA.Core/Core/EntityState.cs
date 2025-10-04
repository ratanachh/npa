namespace NPA.Core.Core;

/// <summary>
/// Represents the state of an entity within the persistence context.
/// </summary>
public enum EntityState
{
    /// <summary>
    /// The entity is not tracked by the persistence context.
    /// </summary>
    Detached,

    /// <summary>
    /// The entity is tracked and unchanged.
    /// </summary>
    Unchanged,

    /// <summary>
    /// The entity is tracked and has been added to the context.
    /// </summary>
    Added,

    /// <summary>
    /// The entity is tracked and has been modified.
    /// </summary>
    Modified,

    /// <summary>
    /// The entity is tracked and marked for deletion.
    /// </summary>
    Deleted
}
