namespace NPA.Core.Metadata;

/// <summary>
/// Provides entity metadata for the persistence context.
/// </summary>
public interface IMetadataProvider
{
    /// <summary>
    /// Gets the metadata for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The entity metadata.</returns>
    EntityMetadata GetEntityMetadata<T>();

    /// <summary>
    /// Gets the metadata for the specified entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The entity metadata.</returns>
    EntityMetadata GetEntityMetadata(Type entityType);

    /// <summary>
    /// Gets the metadata for the specified entity name.
    /// </summary>
    /// <param name="entityName">The name of the entity.</param>
    /// <returns>The entity metadata.</returns>
    EntityMetadata GetEntityMetadata(string entityName);

    /// <summary>
    /// Checks if the specified type is an entity.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is an entity; otherwise, false.</returns>
    bool IsEntity(Type type);
}
