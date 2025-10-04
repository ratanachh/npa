using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace NPA.Core.Tests.Core;

/// <summary>
/// Mock database connection for unit testing using in-memory SQLite.
/// </summary>
public class MockDbConnection : IDbConnection
{
    private readonly List<MockCommand> _executedCommands = new();
    private ConnectionState _state = ConnectionState.Closed;
    private string _connectionString = string.Empty;

    public IReadOnlyList<MockCommand> ExecutedCommands => _executedCommands.AsReadOnly();

    public string ConnectionString
    {
        get => _connectionString;
        set => _connectionString = value ?? string.Empty;
    }

    public int ConnectionTimeout { get; set; } = 30;
    public string Database { get; set; } = "TestDB";
    public ConnectionState State => _state;

    public IDbTransaction BeginTransaction() => new MockTransaction(this);
    public IDbTransaction BeginTransaction(IsolationLevel il) => new MockTransaction(this);
    public void ChangeDatabase(string databaseName) => Database = databaseName;
    public void Close() => _state = ConnectionState.Closed;
    public IDbCommand CreateCommand() => new MockCommand(this);
    public void Open() => _state = ConnectionState.Open;
    public void Dispose() => Close();

    internal void AddExecutedCommand(MockCommand command)
    {
        _executedCommands.Add(command);
    }

    public void Reset()
    {
        _executedCommands.Clear();
        _state = ConnectionState.Closed;
    }
}

/// <summary>
/// Mock database command for unit testing.
/// </summary>
public class MockCommand : IDbCommand
{
    private readonly MockDbConnection _connection;
    private readonly List<IDataParameter> _parameters = new();

    public MockCommand(MockDbConnection connection)
    {
        _connection = connection;
    }

    public string CommandText { get; set; } = string.Empty;
    public int CommandTimeout { get; set; } = 30;
    public CommandType CommandType { get; set; } = CommandType.Text;
    public IDbConnection? Connection
    {
        get => _connection;
        set { /* Mock implementation - connection is set in constructor */ }
    }
    public IDataParameterCollection Parameters => new MockParameterCollection(_parameters);
    public IDbTransaction? Transaction { get; set; }
    public UpdateRowSource UpdatedRowSource { get; set; } = UpdateRowSource.None;

    public void Cancel() { }
    public IDbDataParameter CreateParameter() => new MockParameter();
    public int ExecuteNonQuery() => 1; // Mock return value
    public IDataReader ExecuteReader() => new MockDataReader();
    public IDataReader ExecuteReader(CommandBehavior behavior) => new MockDataReader();
    public object? ExecuteScalar() => 123L; // Mock return value for ID generation
    public void Prepare() { }
    public void Dispose() { }

    internal void AddParameter(IDataParameter parameter)
    {
        _parameters.Add(parameter);
    }
}

/// <summary>
/// Mock database parameter for unit testing.
/// </summary>
public class MockParameter : IDbDataParameter
{
    public DbType DbType { get; set; }
    public ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public bool IsNullable { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public byte Precision { get; set; }
    public byte Scale { get; set; }
    public int Size { get; set; }
    public string SourceColumn { get; set; } = string.Empty;
    public bool SourceColumnNullMapping { get; set; }
    public DataRowVersion SourceVersion { get; set; }
    public object? Value { get; set; }
}

/// <summary>
/// Mock parameter collection for unit testing.
/// </summary>
public class MockParameterCollection : IDataParameterCollection
{
    private readonly List<IDataParameter> _parameters;

    public MockParameterCollection(List<IDataParameter> parameters)
    {
        _parameters = parameters;
    }

    public object this[string parameterName]
    {
        get => _parameters.FirstOrDefault(p => p.ParameterName == parameterName)?.Value!;
        set
        {
            var parameter = _parameters.FirstOrDefault(p => p.ParameterName == parameterName);
            if (parameter != null)
                parameter.Value = value;
        }
    }

    public object this[int index]
    {
        get => _parameters[index].Value!;
        set => _parameters[index].Value = value;
    }

    public bool IsFixedSize => false;
    public bool IsReadOnly => false;
    public bool IsSynchronized => false;
    public int Count => _parameters.Count;
    public object SyncRoot => new object();

    public int Add(object? value)
    {
        if (value is IDataParameter parameter)
        {
            _parameters.Add(parameter);
            return _parameters.Count - 1;
        }
        return -1;
    }

    public void Clear() => _parameters.Clear();
    public bool Contains(string parameterName) => _parameters.Any(p => p.ParameterName == parameterName);
    public bool Contains(object? value) => _parameters.Contains(value as IDataParameter);
    public void CopyTo(Array array, int index) => _parameters.CopyTo((IDataParameter[])array, index);
    public System.Collections.IEnumerator GetEnumerator() => _parameters.GetEnumerator();
    public int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);
    public int IndexOf(object? value) => _parameters.IndexOf(value as IDataParameter);
    public void Insert(int index, object? value)
    {
        if (value is IDataParameter parameter)
            _parameters.Insert(index, parameter);
    }
    public void Remove(object? value)
    {
        if (value is IDataParameter parameter)
            _parameters.Remove(parameter);
    }
    public void RemoveAt(string parameterName)
    {
        var parameter = _parameters.FirstOrDefault(p => p.ParameterName == parameterName);
        if (parameter != null)
            _parameters.Remove(parameter);
    }
    public void RemoveAt(int index) => _parameters.RemoveAt(index);
}

/// <summary>
/// Mock database reader for unit testing.
/// </summary>
public class MockDataReader : IDataReader
{
    private readonly List<Dictionary<string, object?>> _data;
    private int _currentIndex = -1;

    public MockDataReader(List<Dictionary<string, object?>>? data = null)
    {
        _data = data ?? new List<Dictionary<string, object?>>();
    }

    public int Depth => 0;
    public bool IsClosed => false;
    public int RecordsAffected => _data.Count;
    public int FieldCount => _data.FirstOrDefault()?.Count ?? 0;

    public void Close() { }
    public void Dispose() { }
    public bool GetBoolean(int i) => Convert.ToBoolean(GetValue(i));
    public byte GetByte(int i) => Convert.ToByte(GetValue(i));
    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => 0;
    public char GetChar(int i) => Convert.ToChar(GetValue(i));
    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => 0;
    public IDataReader GetData(int i) => this;
    public string GetDataTypeName(int i) => GetValue(i)?.GetType().Name ?? "String";
    public DateTime GetDateTime(int i) => Convert.ToDateTime(GetValue(i));
    public decimal GetDecimal(int i) => Convert.ToDecimal(GetValue(i));
    public double GetDouble(int i) => Convert.ToDouble(GetValue(i));
    public Type GetFieldType(int i) => GetValue(i)?.GetType() ?? typeof(string);
    public float GetFloat(int i) => Convert.ToSingle(GetValue(i));
    public Guid GetGuid(int i) => (Guid)GetValue(i)!;
    public short GetInt16(int i) => Convert.ToInt16(GetValue(i));
    public int GetInt32(int i) => Convert.ToInt32(GetValue(i));
    public long GetInt64(int i) => Convert.ToInt64(GetValue(i));
    public string GetName(int i) => GetFieldName(i);
    public int GetOrdinal(string name) => GetFieldIndex(name);
    public string GetString(int i) => Convert.ToString(GetValue(i)) ?? string.Empty;
    public object GetValue(int i) => GetValue(GetFieldName(i));
    public int GetValues(object[] values) => 0;
    public bool IsDBNull(int i) => GetValue(i) == null;
    public bool NextResult() => false;
    public bool Read() => ++_currentIndex < _data.Count;

    public DataTable GetSchemaTable() => new DataTable();

    // IDataRecord indexers
    public object this[int i] => GetValue(i);
    public object this[string name] => GetValue(name) ?? string.Empty;

    private string GetFieldName(int index)
    {
        if (_data.Count == 0 || _currentIndex < 0 || _currentIndex >= _data.Count)
            return string.Empty;

        return _data[_currentIndex].Keys.ElementAt(index);
    }

    private int GetFieldIndex(string name)
    {
        if (_data.Count == 0 || _currentIndex < 0 || _currentIndex >= _data.Count)
            return -1;

        var keys = _data[_currentIndex].Keys.ToList();
        return keys.IndexOf(name);
    }

    private object? GetValue(string name)
    {
        if (_data.Count == 0 || _currentIndex < 0 || _currentIndex >= _data.Count)
            return null;

        return _data[_currentIndex].TryGetValue(name, out var value) ? value : null;
    }
}

/// <summary>
/// Mock database transaction for unit testing.
/// </summary>
public class MockTransaction : IDbTransaction
{
    private readonly MockDbConnection _connection;

    public MockTransaction(MockDbConnection connection)
    {
        _connection = connection;
    }

    public IDbConnection Connection => _connection;
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;

    public void Commit() { }
    public void Rollback() { }
    public void Dispose() { }
}