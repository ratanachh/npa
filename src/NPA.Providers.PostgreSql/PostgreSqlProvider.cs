using NPA.Core.Providers;
using Npgsql;
using System.Data;

namespace NPA.Providers.PostgreSql;

/// <summary>
/// PostgreSQL database provider implementation for NPA.
/// </summary>
public class PostgreSqlProvider : IDatabaseProvider
{
    public string ProviderName => "PostgreSQL";
    
    public string ParameterPrefix => "@";
    
    public char QuoteCharacter => '"';

    /// <summary>
    /// Creates a new PostgreSQL database connection.
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string</param>
    /// <returns>PostgreSQL database connection</returns>
    public IDbConnection CreateConnection(string connectionString)
    {
        return new NpgsqlConnection(connectionString);
    }

    /// <summary>
    /// Generates PostgreSQL-specific SQL for pagination.
    /// </summary>
    /// <param name="sql">Base SQL query</param>
    /// <param name="offset">Number of records to skip</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>SQL with PostgreSQL LIMIT/OFFSET clause</returns>
    public string GeneratePaginationSql(string sql, int offset, int limit)
    {
        return $"{sql} LIMIT {limit} OFFSET {offset}";
    }

    /// <summary>
    /// Gets the PostgreSQL-specific SQL for getting the last inserted ID.
    /// </summary>
    /// <returns>SQL command to get last insert ID</returns>
    public string GetLastInsertIdSql()
    {
        return "SELECT lastval()";
    }

    /// <summary>
    /// Converts a .NET type to PostgreSQL column type.
    /// </summary>
    /// <param name="type">.NET type</param>
    /// <returns>PostgreSQL column type</returns>
    public string GetColumnType(Type type)
    {
        // TODO: Implement complete type mapping
        return type.Name switch
        {
            nameof(Int32) => "INTEGER",
            nameof(Int64) => "BIGINT",
            nameof(String) => "VARCHAR(255)",
            nameof(DateTime) => "TIMESTAMP",
            nameof(Boolean) => "BOOLEAN",
            nameof(Decimal) => "DECIMAL(18,2)",
            _ => "TEXT"
        };
    }
}