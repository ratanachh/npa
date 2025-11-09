using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace NPA.Migrations;

/// <summary>
/// Base class for all database migrations.
/// Provides common functionality and logging.
/// </summary>
public abstract class Migration : IMigration
{
    /// <summary>
    /// Gets the logger for this migration.
    /// </summary>
    protected ILogger? Logger { get; set; }

    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract long Version { get; }

    /// <inheritdoc/>
    public abstract DateTime CreatedAt { get; }

    /// <inheritdoc/>
    public abstract string Description { get; }

    /// <inheritdoc/>
    public abstract Task UpAsync(IDbConnection connection);

    /// <inheritdoc/>
    public abstract Task DownAsync(IDbConnection connection);

    /// <summary>
    /// Sets the logger for this migration.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public void SetLogger(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">Message to log.</param>
    protected virtual void LogInfo(string message)
    {
        Logger?.LogInformation("[Migration {Name}] {Message}", Name, message);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">Message to log.</param>
    protected virtual void LogWarning(string message)
    {
        Logger?.LogWarning("[Migration {Name}] {Message}", Name, message);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">Message to log.</param>
    /// <param name="exception">Optional exception.</param>
    protected virtual void LogError(string message, Exception? exception = null)
    {
        Logger?.LogError(exception, "[Migration {Name}] {Message}", Name, message);
    }

    /// <summary>
    /// Executes SQL command asynchronously.
    /// </summary>
    /// <param name="connection">Database connection.</param>
    /// <param name="sql">SQL command to execute.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <returns>Number of affected rows.</returns>
    protected virtual async Task<int> ExecuteSqlAsync(
        IDbConnection connection,
        string sql,
        IDbTransaction? transaction = null)
    {
        LogInfo($"Executing SQL: {TruncateSql(sql)}");
        return await connection.ExecuteAsync(sql, transaction: transaction);
    }

    /// <summary>
    /// Executes a scalar SQL query asynchronously.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="connection">Database connection.</param>
    /// <param name="sql">SQL query to execute.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <returns>Scalar result.</returns>
    protected virtual async Task<T?> ExecuteScalarAsync<T>(
        IDbConnection connection,
        string sql,
        IDbTransaction? transaction = null)
    {
        LogInfo($"Executing scalar SQL: {TruncateSql(sql)}");
        return await connection.ExecuteScalarAsync<T>(sql, transaction: transaction);
    }

    /// <summary>
    /// Checks if a table exists in the database.
    /// </summary>
    /// <param name="connection">Database connection.</param>
    /// <param name="tableName">Table name to check.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <returns>True if table exists, false otherwise.</returns>
    protected virtual async Task<bool> TableExistsAsync(
        IDbConnection connection,
        string tableName,
        IDbTransaction? transaction = null)
    {
        var sql = GetTableExistsSql(tableName);
        var result = await connection.ExecuteScalarAsync<int>(sql, transaction: transaction);
        return result > 0;
    }

    /// <summary>
    /// Gets the SQL to check if a table exists (database-specific).
    /// Override this for different database providers.
    /// </summary>
    /// <param name="tableName">Table name.</param>
    /// <returns>SQL query string.</returns>
    protected virtual string GetTableExistsSql(string tableName)
    {
        // Default SQL Server syntax
        return $@"
            SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_NAME = '{tableName}'";
    }

    /// <summary>
    /// Truncates SQL for logging (prevents log spam with large SQL statements).
    /// </summary>
    /// <param name="sql">SQL to truncate.</param>
    /// <param name="maxLength">Maximum length (default 200).</param>
    /// <returns>Truncated SQL.</returns>
    protected virtual string TruncateSql(string sql, int maxLength = 200)
    {
        if (string.IsNullOrEmpty(sql))
            return string.Empty;

        var cleaned = sql.Trim().Replace("\n", " ").Replace("\r", "");
        while (cleaned.Contains("  "))
            cleaned = cleaned.Replace("  ", " ");

        return cleaned.Length > maxLength
            ? cleaned.Substring(0, maxLength) + "..."
            : cleaned;
    }

    /// <summary>
    /// Generates a version number based on current timestamp.
    /// Format: YYYYMMDDHHMMSS
    /// </summary>
    /// <returns>Version number.</returns>
    protected static long GenerateVersion()
    {
        var now = DateTime.UtcNow;
        return long.Parse($"{now:yyyyMMddHHmmss}");
    }

    /// <summary>
    /// Generates a version number from a specific date.
    /// </summary>
    /// <param name="date">Date to generate version from.</param>
    /// <returns>Version number.</returns>
    protected static long GenerateVersion(DateTime date)
    {
        return long.Parse($"{date:yyyyMMddHHmmss}");
    }
}
