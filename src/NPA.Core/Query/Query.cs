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
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Parameter name cannot be null or empty", nameof(name));
        _parameters[name] = _parameterBinder.SanitizeParameter(value);
        return this;
    }

    /// <inheritdoc />
    public IQuery<T> SetParameter(int index, object? value)
    {
        ThrowIfDisposed();
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), "Parameter index cannot be negative");
        _indexedParameters[index] = _parameterBinder.SanitizeParameter(value);
        return this;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> GetResultListAsync()
    {
        ThrowIfDisposed();
        var sql = GetSql();
        var boundParameters = GetBoundParameters();
        LogExecutionDetails(sql, boundParameters);
        return await _connection.QueryAsync<T>(sql, boundParameters);
    }

    /// <inheritdoc />
    public IEnumerable<T> GetResultList()
    {
        ThrowIfDisposed();
        var sql = GetSql();
        var boundParameters = GetBoundParameters();
        LogExecutionDetails(sql, boundParameters);
        return _connection.Query<T>(sql, boundParameters);
    }

    /// <inheritdoc />
    public async Task<T?> GetSingleResultAsync()
    {
        ThrowIfDisposed();
        var sql = GetSql();
        var boundParameters = GetBoundParameters();
        LogExecutionDetails(sql, boundParameters);
        return await _connection.QueryFirstOrDefaultAsync<T>(sql, boundParameters);
    }

    /// <inheritdoc />
    public T? GetSingleResult()
    {
        ThrowIfDisposed();
        var sql = GetSql();
        var boundParameters = GetBoundParameters();
        LogExecutionDetails(sql, boundParameters);
        return _connection.QueryFirstOrDefault<T>(sql, boundParameters);
    }

    /// <inheritdoc />
    public async Task<T> GetSingleResultRequiredAsync()
    {
        var result = await GetSingleResultAsync();
        if (result == null) throw new InvalidOperationException("Query returned no results, but a single result was required");
        return result;
    }

    /// <inheritdoc />
    public T GetSingleResultRequired()
    {
        var result = GetSingleResult();
        if (result == null) throw new InvalidOperationException("Query returned no results, but a single result was required");
        return result;
    }

    /// <inheritdoc />
    public async Task<int> ExecuteUpdateAsync()
    {
        ThrowIfDisposed();
        var sql = GetSql();
        var boundParameters = GetBoundParameters();
        LogExecutionDetails(sql, boundParameters);
        return await _connection.ExecuteAsync(sql, boundParameters);
    }

    /// <inheritdoc />
    public int ExecuteUpdate()
    {
        ThrowIfDisposed();
        var sql = GetSql();
        var boundParameters = GetBoundParameters();
        LogExecutionDetails(sql, boundParameters);
        return _connection.Execute(sql, boundParameters);
    }

    /// <inheritdoc />
    public async Task<object?> ExecuteScalarAsync()
    {
        ThrowIfDisposed();
        var sql = GetSql();
        var boundParameters = GetBoundParameters();
        LogExecutionDetails(sql, boundParameters);
        return await _connection.ExecuteScalarAsync(sql, boundParameters);
    }

    /// <inheritdoc />
    public object? ExecuteScalar()
    {
        ThrowIfDisposed();
        var sql = GetSql();
        var boundParameters = GetBoundParameters();
        LogExecutionDetails(sql, boundParameters);
        return _connection.ExecuteScalar(sql, boundParameters);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _parameters.Clear();
        _indexedParameters.Clear();
        _disposed = true;
    }

    private string GetSql()
    {
        if (_sql != null) return _sql;

        _logger?.LogDebug("Parsing CPQL: {Cpql}", _cpql);
        var parsedQuery = _parser.Parse(_cpql);

        if (string.IsNullOrEmpty(parsedQuery.EntityName))
        {
            throw new InvalidOperationException("CPQL query must have a FROM clause specifying an entity.");
        }

        var entityMetadata = _metadataProvider.GetEntityMetadata(parsedQuery.EntityName);
        _sql = _sqlGenerator.Generate(parsedQuery, entityMetadata);
        
        _logger?.LogDebug("Generated SQL: {Sql}", _sql);

        return _sql;
    }

    private object GetBoundParameters()
    {
        return _indexedParameters.Count > 0 
            ? _parameterBinder.BindParametersByIndex(_indexedParameters) 
            : _parameterBinder.BindParameters(_parameters);
    }

    private void LogExecutionDetails(string sql, object boundParameters)
    {
        if (_logger?.IsEnabled(LogLevel.Debug) != true) return;
        
        _logger.LogDebug("Executing SQL: {Sql}", sql);
        if (boundParameters is IDictionary<string, object?> paramDict && paramDict.Count > 0)
        {
            foreach (var param in paramDict)
            {
                _logger.LogDebug("  Parameter: @{ParamName} = {ParamValue}", param.Key, param.Value ?? "NULL");
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Query<T>));
    }
}
