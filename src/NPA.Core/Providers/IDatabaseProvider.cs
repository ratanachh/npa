using System.Data;
using NPA.Core.Metadata;

namespace NPA.Core.Providers;

/// <summary>
/// Defines the contract for database-specific operations and SQL generation.
/// </summary>
public interface IDatabaseProvider
{
    /// <summary>
    /// Generates INSERT SQL statement for the specified entity metadata.
    /// </summary>
    /// <param name="metadata">The entity metadata.</param>
    /// <returns>The generated INSERT SQL statement.</returns>
    string GenerateInsertSql(EntityMetadata metadata);

    /// <summary>
    /// Generates UPDATE SQL statement for the specified entity metadata.
    /// </summary>
    /// <param name="metadata">The entity metadata.</param>
    /// <returns>The generated UPDATE SQL statement.</returns>
    string GenerateUpdateSql(EntityMetadata metadata);

    /// <summary>
    /// Generates DELETE SQL statement for the specified entity metadata.
    /// </summary>
    /// <param name="metadata">The entity metadata.</param>
    /// <returns>The generated DELETE SQL statement.</returns>
    string GenerateDeleteSql(EntityMetadata metadata);

    /// <summary>
    /// Generates SELECT SQL statement for the specified entity metadata.
    /// </summary>
    /// <param name="metadata">The entity metadata.</param>
    /// <returns>The generated SELECT SQL statement.</returns>
    string GenerateSelectSql(EntityMetadata metadata);

    /// <summary>
    /// Generates SELECT BY ID SQL statement for the specified entity metadata.
    /// </summary>
    /// <param name="metadata">The entity metadata.</param>
    /// <returns>The generated SELECT BY ID SQL statement.</returns>
    string GenerateSelectByIdSql(EntityMetadata metadata);

    /// <summary>
    /// Generates COUNT SQL statement for the specified entity metadata.
    /// </summary>
    /// <param name="metadata">The entity metadata.</param>
    /// <returns>The generated COUNT SQL statement.</returns>
    string GenerateCountSql(EntityMetadata metadata);

    /// <summary>
    /// Resolves the database table name for the specified entity metadata.
    /// </summary>
    /// <param name="metadata">The entity metadata.</param>
    /// <returns>The resolved table name.</returns>
    string ResolveTableName(EntityMetadata metadata);

    /// <summary>
    /// Resolves the database column name for the specified property metadata.
    /// </summary>
    /// <param name="property">The property metadata.</param>
    /// <returns>The resolved column name.</returns>
    string ResolveColumnName(PropertyMetadata property);

    /// <summary>
    /// Gets the parameter placeholder for the specified parameter name.
    /// </summary>
    /// <param name="parameterName">The parameter name.</param>
    /// <returns>The parameter placeholder.</returns>
    string GetParameterPlaceholder(string parameterName);

    /// <summary>
    /// Converts a parameter value to the appropriate type for the database.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="targetType">The target type.</param>
    /// <returns>The converted value.</returns>
    object? ConvertParameterValue(object? value, Type targetType);

    /// <summary>
    /// Performs bulk insert operation for the specified entities asynchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="metadata">The entity metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of affected rows.</returns>
    Task<int> BulkInsertAsync<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk insert operation for the specified entities synchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="metadata">The entity metadata.</param>
    /// <returns>The number of affected rows.</returns>
    int BulkInsert<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata);

    /// <summary>
    /// Performs bulk update operation for the specified entities asynchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="entities">The entities to update.</param>
    /// <param name="metadata">The entity metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of affected rows.</returns>
    Task<int> BulkUpdateAsync<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk update operation for the specified entities synchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="entities">The entities to update.</param>
    /// <param name="metadata">The entity metadata.</param>
    /// <returns>The number of affected rows.</returns>
    int BulkUpdate<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata);

    /// <summary>
    /// Performs bulk delete operation for the specified entity IDs asynchronously.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="ids">The entity IDs to delete.</param>
    /// <param name="metadata">The entity metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of affected rows.</returns>
    Task<int> BulkDeleteAsync(IDbConnection connection, IEnumerable<object> ids, EntityMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk delete operation for the specified entity IDs synchronously.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="ids">The entity IDs to delete.</param>
    /// <param name="metadata">The entity metadata.</param>
    /// <returns>The number of affected rows.</returns>
    int BulkDelete(IDbConnection connection, IEnumerable<object> ids, EntityMetadata metadata);
}