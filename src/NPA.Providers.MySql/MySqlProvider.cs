using NPA.Core.Providers;
using MySqlConnector;
using System.Data;

namespace NPA.Providers.MySql;

/// <summary>
/// MySQL database provider implementation for NPA.
/// </summary>
public class MySqlProvider : IDatabaseProvider
{
    public string ProviderName => "MySQL";
    
    public string ParameterPrefix => "@";
    
    public char QuoteCharacter => '`';

    /// <summary>
    /// Creates a new MySQL database connection.
    /// </summary>
    /// <param name="connectionString">MySQL connection string</param>
    /// <returns>MySQL database connection</returns>
    public IDbConnection CreateConnection(string connectionString)
    {
        return new MySqlConnection(connectionString);
    }

    /// <summary>
    /// Generates MySQL-specific SQL for pagination.
    /// </summary>
    /// <param name="sql">Base SQL query</param>
    /// <param name="offset">Number of records to skip</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>SQL with MySQL LIMIT clause</returns>
    public string GeneratePaginationSql(string sql, int offset, int limit)
    {
        return $"{sql} LIMIT {offset}, {limit}";
    }

    /// <summary>
    /// Gets the MySQL-specific SQL for getting the last inserted ID.
    /// </summary>
    /// <returns>SQL command to get last insert ID</returns>
    public string GetLastInsertIdSql()
    {
        return "SELECT LAST_INSERT_ID()";
    }

    /// <summary>
    /// Converts a .NET type to MySQL column type.
    /// </summary>
    /// <param name="type">.NET type</param>
    /// <returns>MySQL column type</returns>
    public string GetColumnType(Type type)
    {
        // TODO: Implement complete type mapping
        return type.Name switch
        {
            nameof(Int32) => "INT",
            nameof(Int64) => "BIGINT",
            nameof(String) => "VARCHAR(255)",
            nameof(DateTime) => "DATETIME",
            nameof(Boolean) => "BOOLEAN",
            nameof(Decimal) => "DECIMAL(18,2)",
            _ => "TEXT"
        };
    }
}