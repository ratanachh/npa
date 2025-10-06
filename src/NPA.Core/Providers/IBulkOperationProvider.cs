using System.Data;
using NPA.Core.Metadata;

namespace NPA.Core.Providers;

/// <summary>
/// Defines the contract for bulk database operations.
/// </summary>
public interface IBulkOperationProvider
{
    /// <summary>
    /// Performs bulk insert operation for the specified entities.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="metadata">The entity metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of affected rows.</returns>
    Task<int> BulkInsertAsync<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk update operation for the specified entities.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="entities">The entities to update.</param>
    /// <param name="metadata">The entity metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of affected rows.</returns>
    Task<int> BulkUpdateAsync<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk delete operation for the specified entity IDs.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="ids">The entity IDs to delete.</param>
    /// <param name="metadata">The entity metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of affected rows.</returns>
    Task<int> BulkDeleteAsync(IDbConnection connection, IEnumerable<object> ids, EntityMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the maximum batch size supported by this provider.
    /// </summary>
    int MaxBatchSize { get; }

    /// <summary>
    /// Gets a value indicating whether this provider supports table-valued parameters.
    /// </summary>
    bool SupportsTableValuedParameters { get; }

    /// <summary>
    /// Creates a table-valued parameter for bulk operations.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities.</param>
    /// <param name="metadata">The entity metadata.</param>
    /// <param name="typeName">The table-valued parameter type name.</param>
    /// <returns>The table-valued parameter.</returns>
    object CreateTableValuedParameter<T>(IEnumerable<T> entities, EntityMetadata metadata, string typeName);
}