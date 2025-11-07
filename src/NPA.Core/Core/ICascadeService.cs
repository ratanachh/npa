using NPA.Core.Annotations;
using NPA.Core.Metadata;

namespace NPA.Core.Core;

/// <summary>
/// Service for handling cascade operations on related entities.
/// Automatically propagates state changes to related entities based on cascade configuration.
/// </summary>
public interface ICascadeService
{
    /// <summary>
    /// Processes cascade persist operations for the given entity.
    /// Recursively persists related entities when CascadeType.Persist is configured.
    /// </summary>
    /// <param name="entity">The entity to process cascade operations for.</param>
    /// <param name="metadata">The entity metadata.</param>
    /// <param name="entityManager">The entity manager to use for cascade operations.</param>
    /// <param name="visited">Set of already visited entities to prevent infinite recursion.</param>
    Task CascadePersistAsync(object entity, EntityMetadata metadata, IEntityManager entityManager, HashSet<object> visited);

    /// <summary>
    /// Processes cascade merge operations for the given entity.
    /// Recursively merges related entities when CascadeType.Merge is configured.
    /// </summary>
    /// <param name="entity">The entity to process cascade operations for.</param>
    /// <param name="metadata">The entity metadata.</param>
    /// <param name="entityManager">The entity manager to use for cascade operations.</param>
    /// <param name="visited">Set of already visited entities to prevent infinite recursion.</param>
    Task CascadeMergeAsync(object entity, EntityMetadata metadata, IEntityManager entityManager, HashSet<object> visited);

    /// <summary>
    /// Processes cascade remove operations for the given entity.
    /// Recursively removes related entities when CascadeType.Remove is configured.
    /// </summary>
    /// <param name="entity">The entity to process cascade operations for.</param>
    /// <param name="metadata">The entity metadata.</param>
    /// <param name="entityManager">The entity manager to use for cascade operations.</param>
    /// <param name="visited">Set of already visited entities to prevent infinite recursion.</param>
    Task CascadeRemoveAsync(object entity, EntityMetadata metadata, IEntityManager entityManager, HashSet<object> visited);

    /// <summary>
    /// Processes cascade detach operations for the given entity.
    /// Recursively detaches related entities when CascadeType.Detach is configured.
    /// </summary>
    /// <param name="entity">The entity to process cascade operations for.</param>
    /// <param name="metadata">The entity metadata.</param>
    /// <param name="entityManager">The entity manager to use for cascade operations.</param>
    /// <param name="visited">Set of already visited entities to prevent infinite recursion.</param>
    Task CascadeDetachAsync(object entity, EntityMetadata metadata, IEntityManager entityManager, HashSet<object> visited);
}
