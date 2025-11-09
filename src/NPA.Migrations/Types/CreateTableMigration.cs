using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPA.Migrations.Types;

/// <summary>
/// Migration for creating a new database table.
/// </summary>
public class CreateTableMigration : Migration
{
    private readonly string _tableName;
    private readonly List<ColumnDefinition> _columns;
    private readonly List<string> _primaryKeys;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateTableMigration"/> class.
    /// </summary>
    /// <param name="tableName">Name of the table to create.</param>
    /// <param name="version">Migration version.</param>
    public CreateTableMigration(string tableName, long version)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

        _tableName = tableName;
        _columns = new List<ColumnDefinition>();
        _primaryKeys = new List<string>();
        Version = version;
    }

    /// <inheritdoc/>
    public override string Name => $"CreateTable_{_tableName}";

    /// <inheritdoc/>
    public override long Version { get; }

    /// <inheritdoc/>
    public override DateTime CreatedAt { get; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public override string Description => $"Create table {_tableName}";

    /// <summary>
    /// Adds a column to the table.
    /// </summary>
    /// <param name="name">Column name.</param>
    /// <param name="type">SQL data type.</param>
    /// <param name="nullable">Whether the column is nullable.</param>
    /// <param name="defaultValue">Default value expression.</param>
    /// <returns>This migration for fluent chaining.</returns>
    public CreateTableMigration AddColumn(
        string name,
        string type,
        bool nullable = true,
        string? defaultValue = null)
    {
        _columns.Add(new ColumnDefinition
        {
            Name = name,
            Type = type,
            Nullable = nullable,
            DefaultValue = defaultValue
        });
        return this;
    }

    /// <summary>
    /// Adds a primary key column.
    /// </summary>
    /// <param name="name">Column name.</param>
    /// <param name="type">SQL data type.</param>
    /// <param name="identity">Whether it's an identity column.</param>
    /// <returns>This migration for fluent chaining.</returns>
    public CreateTableMigration AddPrimaryKey(
        string name,
        string type = "INT",
        bool identity = true)
    {
        var column = new ColumnDefinition
        {
            Name = name,
            Type = type,
            Nullable = false,
            Identity = identity
        };

        _columns.Add(column);
        _primaryKeys.Add(name);
        return this;
    }

    /// <summary>
    /// Adds multiple columns as composite primary key.
    /// </summary>
    /// <param name="columnNames">Column names that form the primary key.</param>
    /// <returns>This migration for fluent chaining.</returns>
    public CreateTableMigration AddCompositePrimaryKey(params string[] columnNames)
    {
        _primaryKeys.AddRange(columnNames);
        return this;
    }

    /// <inheritdoc/>
    public override async Task UpAsync(IDbConnection connection)
    {
        if (!_columns.Any())
            throw new InvalidOperationException($"Cannot create table {_tableName} without columns.");

        var sql = GenerateCreateTableSql();
        await ExecuteSqlAsync(connection, sql);
    }

    /// <inheritdoc/>
    public override async Task DownAsync(IDbConnection connection)
    {
        var sql = $"DROP TABLE IF EXISTS {_tableName}";
        await ExecuteSqlAsync(connection, sql);
    }

    /// <summary>
    /// Generates the CREATE TABLE SQL statement.
    /// </summary>
    /// <returns>SQL statement.</returns>
    private string GenerateCreateTableSql()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE {_tableName} (");

        var columnDefinitions = new List<string>();

        foreach (var column in _columns)
        {
            var columnDef = new StringBuilder();
            columnDef.Append($"    {column.Name} {column.Type}");

            if (column.Identity)
                columnDef.Append(" IDENTITY(1,1)");

            if (!column.Nullable)
                columnDef.Append(" NOT NULL");

            if (!string.IsNullOrEmpty(column.DefaultValue))
                columnDef.Append($" DEFAULT {column.DefaultValue}");

            columnDefinitions.Add(columnDef.ToString());
        }

        sb.AppendLine(string.Join(",\n", columnDefinitions));

        // Add primary key constraint if specified
        if (_primaryKeys.Any())
        {
            var pkName = $"PK_{_tableName}";
            var pkColumns = string.Join(", ", _primaryKeys);
            sb.AppendLine($"    ,CONSTRAINT {pkName} PRIMARY KEY ({pkColumns})");
        }

        sb.AppendLine(")");

        return sb.ToString();
    }

    /// <summary>
    /// Column definition for table creation.
    /// </summary>
    private class ColumnDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Nullable { get; set; } = true;
        public string? DefaultValue { get; set; }
        public bool Identity { get; set; }
    }
}
