using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NPA.Monitoring;

namespace NPA.Profiler.Profiling;

/// <summary>
/// Main profiler that intercepts and profiles database operations.
/// </summary>
public class NpaProfiler : IDisposable
{
    private readonly PerformanceMonitor? _performanceMonitor;
    private ProfilingSession? _currentSession;
    private readonly Stack<QueryProfile> _queryStack = new();
    private bool _isEnabled;

    public NpaProfiler(PerformanceMonitor? performanceMonitor = null)
    {
        _performanceMonitor = performanceMonitor;
    }

    public ProfilingSession? CurrentSession => _currentSession;
    public bool IsEnabled => _isEnabled;

    /// <summary>
    /// Starts a new profiling session.
    /// </summary>
    public ProfilingSession StartSession()
    {
        if (_currentSession != null)
        {
            throw new InvalidOperationException("A profiling session is already active. Stop the current session before starting a new one.");
        }

        _currentSession = new ProfilingSession();
        _currentSession.Start();
        _isEnabled = true;

        return _currentSession;
    }

    /// <summary>
    /// Stops the current profiling session and returns it.
    /// </summary>
    public ProfilingSession? StopSession()
    {
        if (_currentSession == null)
        {
            return null;
        }

        _currentSession.Stop();
        _isEnabled = false;

        var session = _currentSession;
        _currentSession = null;

        return session;
    }

    /// <summary>
    /// Begins profiling a query execution.
    /// </summary>
    public IDisposable? BeginProfileQuery(
        string sql,
        QueryType queryType = QueryType.Other,
        string? entityType = null,
        Dictionary<string, object?>? parameters = null,
        [CallerMemberName] string? callerMember = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
    {
        if (!_isEnabled || _currentSession == null)
        {
            return null;
        }

        var profile = new QueryProfile
        {
            Sql = sql,
            QueryType = queryType,
            EntityType = entityType,
            Parameters = parameters ?? new Dictionary<string, object?>(),
            CallerMember = callerMember,
            CallerFilePath = callerFilePath,
            CallerLineNumber = callerLineNumber,
            StackTrace = Environment.StackTrace
        };

        _queryStack.Push(profile);

        return new QueryProfileScope(this, profile);
    }

    /// <summary>
    /// Records the completion of a query execution.
    /// </summary>
    internal void EndProfileQuery(QueryProfile profile, TimeSpan duration, int rowsAffected, bool fromCache)
    {
        if (!_isEnabled || _currentSession == null)
        {
            return;
        }

        profile.Duration = duration;
        profile.RowsAffected = rowsAffected;
        profile.FromCache = fromCache;

        _currentSession.AddQuery(profile);

        // Also report to performance monitor if available
        _performanceMonitor?.RecordMetric(
            profile.QueryType.ToString(),
            duration,
            rowsAffected);

        if (_queryStack.Count > 0 && _queryStack.Peek() == profile)
        {
            _queryStack.Pop();
        }
    }

    /// <summary>
    /// Wraps a database connection to enable profiling.
    /// </summary>
    public IDbConnection ProfileConnection(IDbConnection connection)
    {
        if (!_isEnabled)
        {
            return connection;
        }

        return new ProfiledDbConnection(connection, this);
    }

    public void Dispose()
    {
        StopSession();
    }

    /// <summary>
    /// Scope that automatically records query timing.
    /// </summary>
    internal class QueryProfileScope : IDisposable
    {
        private readonly NpaProfiler _profiler;
        private readonly QueryProfile _profile;
        private readonly Stopwatch _stopwatch;
        private int _rowsAffected;
        private bool _fromCache;

        public QueryProfileScope(NpaProfiler profiler, QueryProfile profile)
        {
            _profiler = profiler;
            _profile = profile;
            _stopwatch = Stopwatch.StartNew();
        }

        public void RecordResult(int rowsAffected, bool fromCache = false)
        {
            _rowsAffected = rowsAffected;
            _fromCache = fromCache;
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _profiler.EndProfileQuery(_profile, _stopwatch.Elapsed, _rowsAffected, _fromCache);
        }
    }
}

/// <summary>
/// Profiled database connection wrapper.
/// </summary>
internal class ProfiledDbConnection : IDbConnection
{
    private readonly IDbConnection _connection;
    private readonly NpaProfiler _profiler;

    public ProfiledDbConnection(IDbConnection connection, NpaProfiler profiler)
    {
        _connection = connection;
        _profiler = profiler;
    }

    public string ConnectionString
    {
        get => _connection.ConnectionString;
#pragma warning disable CS8767 // Nullability mismatch
        set => _connection.ConnectionString = value!;
#pragma warning restore CS8767
    }

    public int ConnectionTimeout => _connection.ConnectionTimeout;
    public string Database => _connection.Database;
    public ConnectionState State => _connection.State;

    public IDbTransaction BeginTransaction() => _connection.BeginTransaction();
    public IDbTransaction BeginTransaction(IsolationLevel il) => _connection.BeginTransaction(il);
    public void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);
    public void Close() => _connection.Close();

    public IDbCommand CreateCommand()
    {
        var command = _connection.CreateCommand();
        return new ProfiledDbCommand(command, _profiler);
    }

    public void Open() => _connection.Open();
    public void Dispose() => _connection.Dispose();
}

/// <summary>
/// Profiled database command wrapper.
/// </summary>
internal class ProfiledDbCommand : IDbCommand
{
    private readonly IDbCommand _command;
    private readonly NpaProfiler _profiler;

    public ProfiledDbCommand(IDbCommand command, NpaProfiler profiler)
    {
        _command = command;
        _profiler = profiler;
    }

    public string CommandText
    {
        get => _command.CommandText;
#pragma warning disable CS8767 // Nullability mismatch
        set => _command.CommandText = value!;
#pragma warning restore CS8767
    }

    public int CommandTimeout
    {
        get => _command.CommandTimeout;
        set => _command.CommandTimeout = value;
    }

    public CommandType CommandType
    {
        get => _command.CommandType;
        set => _command.CommandType = value;
    }

    public IDbConnection? Connection
    {
        get => _command.Connection;
        set => _command.Connection = value;
    }

    public IDataParameterCollection Parameters => _command.Parameters;
    public IDbTransaction? Transaction
    {
        get => _command.Transaction;
        set => _command.Transaction = value;
    }

    public UpdateRowSource UpdatedRowSource
    {
        get => _command.UpdatedRowSource;
        set => _command.UpdatedRowSource = value;
    }

    public void Cancel() => _command.Cancel();
    public IDbDataParameter CreateParameter() => _command.CreateParameter();

    public int ExecuteNonQuery()
    {
        var queryType = DetermineQueryType(CommandText);
        var parameters = ExtractParameters();

        using var scope = _profiler.BeginProfileQuery(CommandText, queryType, parameters: parameters);
        var result = _command.ExecuteNonQuery();
        
        if (scope is NpaProfiler.QueryProfileScope profileScope)
        {
            profileScope.RecordResult(result);
        }

        return result;
    }

    public IDataReader ExecuteReader() => ExecuteReader(CommandBehavior.Default);

    public IDataReader ExecuteReader(CommandBehavior behavior)
    {
        var queryType = DetermineQueryType(CommandText);
        var parameters = ExtractParameters();

        using var scope = _profiler.BeginProfileQuery(CommandText, queryType, parameters: parameters);
        var reader = _command.ExecuteReader(behavior);

        if (scope is NpaProfiler.QueryProfileScope profileScope)
        {
            profileScope.RecordResult(reader.RecordsAffected);
        }

        return reader;
    }

    public object? ExecuteScalar()
    {
        var queryType = DetermineQueryType(CommandText);
        var parameters = ExtractParameters();

        using var scope = _profiler.BeginProfileQuery(CommandText, queryType, parameters: parameters);
        var result = _command.ExecuteScalar();

        if (scope is NpaProfiler.QueryProfileScope profileScope)
        {
            profileScope.RecordResult(1);
        }

        return result;
    }

    public void Prepare() => _command.Prepare();
    public void Dispose() => _command.Dispose();

    private QueryType DetermineQueryType(string sql)
    {
        var trimmed = sql.TrimStart().ToUpperInvariant();
        if (trimmed.StartsWith("SELECT")) return QueryType.Select;
        if (trimmed.StartsWith("INSERT")) return QueryType.Insert;
        if (trimmed.StartsWith("UPDATE")) return QueryType.Update;
        if (trimmed.StartsWith("DELETE")) return QueryType.Delete;
        return QueryType.Other;
    }

    private Dictionary<string, object?> ExtractParameters()
    {
        var parameters = new Dictionary<string, object?>();
        foreach (IDbDataParameter param in _command.Parameters)
        {
            parameters[param.ParameterName] = param.Value;
        }
        return parameters;
    }
}
