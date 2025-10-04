namespace NPA.Core.Query;

/// <summary>
/// Represents a query that can be executed against the database.
/// </summary>
/// <typeparam name="T">The type of entity to return.</typeparam>
public interface IQuery<T> : IDisposable
{
    /// <summary>
    /// Sets a named parameter for the query.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>The query instance for method chaining.</returns>
    IQuery<T> SetParameter(string name, object? value);

    /// <summary>
    /// Sets a parameter by index for the query.
    /// </summary>
    /// <param name="index">The parameter index (0-based).</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>The query instance for method chaining.</returns>
    IQuery<T> SetParameter(int index, object? value);

    /// <summary>
    /// Executes the query and returns a list of results.
    /// </summary>
    /// <returns>A task representing the asynchronous operation. The task result contains the list of entities.</returns>
    Task<IEnumerable<T>> GetResultListAsync();

    /// <summary>
    /// Executes the query and returns a single result, or null if no results are found.
    /// </summary>
    /// <returns>A task representing the asynchronous operation. The task result contains the single entity, or null if not found.</returns>
    Task<T?> GetSingleResultAsync();

    /// <summary>
    /// Executes the query and returns a single result, throwing an exception if no results are found.
    /// </summary>
    /// <returns>A task representing the asynchronous operation. The task result contains the single entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no results are found.</exception>
    Task<T> GetSingleResultRequiredAsync();

    /// <summary>
    /// Executes an update or delete query and returns the number of affected rows.
    /// </summary>
    /// <returns>A task representing the asynchronous operation. The task result contains the number of affected rows.</returns>
    Task<int> ExecuteUpdateAsync();

    /// <summary>
    /// Executes a scalar query and returns the scalar value.
    /// </summary>
    /// <returns>A task representing the asynchronous operation. The task result contains the scalar value.</returns>
    Task<object?> ExecuteScalarAsync();
}
