using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using NPA.Core.Metadata;

namespace NPA.Core.Query;

/// <summary>
/// Represents a query that can be executed against the database.
/// </summary>
/// <typeparam name="T">The type of entity to return.</typeparam>
public sealed class Query<T> : IQuery<T>
{
    private readonly IDbConnection _connection;
    private readonly IQueryParser _parser;
    private readonly ISqlGenerator _sqlGenerator;
    private readonly IParameterBinder _parameterBinder;
    private readonly IMetadataProvider _metadataProvider;
    private readonly ILogger<Query<T>>? _logger;
    private readonly Dictionary<string, object?> _parameters;
    private readonly Dictionary<int, object?> _indexedParameters;
    private readonly string _cpql;
    private string? _sql;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Query{T}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="parser">The query parser.</param>
    /// <param name="sqlGenerator">The SQL generator.</param>
    /// <param name="parameterBinder">The parameter binder.</param>
    /// <param name="metadataProvider">The metadata provider.</param>
    /// <param name="cpql">The CPQL query string.</param>
    /// <param name="logger">The logger (optional).</param>
    public Query(
        IDbConnection connection,
        IQueryParser parser,
        ISqlGenerator sqlGenerator,
        IParameterBinder parameterBinder,
        IMetadataProvider metadataProvider,
        string cpql,
        ILogger<Query<T>>? logger = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        _parameterBinder = parameterBinder ?? throw new ArgumentNullException(nameof(parameterBinder));
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
        _cpql = cpql ?? throw new ArgumentNullException(nameof(cpql));
        _logger = logger;
        _parameters = new Dictionary<string, object?>();
        _indexedParameters = new Dictionary<int, object?>();
    }

    /// <inheritdoc />
    public IQuery<T> SetParameter(string name, object? value)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Parameter name cannot be null or empty", nameof(name));

        _logger?.LogDebug("Setting parameter {ParameterName} = {ParameterValue}", name, value);
        _parameters[name] = _parameterBinder.SanitizeParameter(value);
        return this;
    }

    /// <inheritdoc />
    public IQuery<T> SetParameter(int index, object? value)
    {
        ThrowIfDisposed();
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Parameter index cannot be negative");

        _logger?.LogDebug("Setting parameter at index {ParameterIndex} = {ParameterValue}", index, value);
        _indexedParameters[index] = _parameterBinder.SanitizeParameter(value);
        return this;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> GetResultListAsync()
    {
        ThrowIfDisposed();
        _logger?.LogDebug("Executing query to get result list");

        var sql = GetSql();
        var boundParameters = GetBoundParameters();

        try
        {
            var results = await _connection.QueryAsync<T>(sql, boundParameters);
            _logger?.LogDebug("Query executed successfully, returned {ResultCount} results", results.Count());
            return results;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing query: {Sql}", sql);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetSingleResultAsync()
    {
        ThrowIfDisposed();
        _logger?.LogDebug("Executing query to get single result");

        var sql = GetSql();
        var boundParameters = GetBoundParameters();

        try
        {
            var result = await _connection.QueryFirstOrDefaultAsync<T>(sql, boundParameters);
            _logger?.LogDebug("Query executed successfully, returned {HasResult} result", result != null ? "one" : "no");
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing query: {Sql}", sql);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<T> GetSingleResultRequiredAsync()
    {
        ThrowIfDisposed();
        _logger?.LogDebug("Executing query to get single required result");

        var result = await GetSingleResultAsync();
        if (result == null)
        {
            var message = "Query returned no results, but a single result was required";
            _logger?.LogWarning(message);
            throw new InvalidOperationException(message);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<int> ExecuteUpdateAsync()
    {
        ThrowIfDisposed();
        _logger?.LogDebug("Executing update query");

        var sql = GetSql();
        var boundParameters = GetBoundParameters();

        try
        {
            var affectedRows = await _connection.ExecuteAsync(sql, boundParameters);
            _logger?.LogDebug("Update query executed successfully, affected {AffectedRows} rows", affectedRows);
            return affectedRows;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing update query: {Sql}", sql);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<object?> ExecuteScalarAsync()
    {
        ThrowIfDisposed();
        _logger?.LogDebug("Executing scalar query");

        var sql = GetSql();
        var boundParameters = GetBoundParameters();

        try
        {
            var result = await _connection.ExecuteScalarAsync(sql, boundParameters);
            _logger?.LogDebug("Scalar query executed successfully, returned {HasResult} result", result != null ? "a" : "no");
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing scalar query: {Sql}", sql);
            throw;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _parameters.Clear();
            _indexedParameters.Clear();
            _disposed = true;
        }
    }

    private string GetSql()
    {
        if (_sql == null)
        {
            _logger?.LogDebug("Parsing CPQL: {Cpql}", _cpql);
            var parsedQuery = _parser.Parse(_cpql);
            
            var entityMetadata = _metadataProvider.GetEntityMetadata<T>();
            _sql = _sqlGenerator.Generate(parsedQuery, entityMetadata);
            
            _logger?.LogDebug("Generated SQL: {Sql}", _sql);
        }

        return _sql;
    }

    private object GetBoundParameters()
    {
        if (_indexedParameters.Count > 0)
        {
            return _parameterBinder.BindParametersByIndex(_indexedParameters);
        }

        return _parameterBinder.BindParameters(_parameters);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Query<T>));
    }
}
